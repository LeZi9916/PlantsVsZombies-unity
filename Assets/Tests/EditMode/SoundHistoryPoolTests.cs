#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks retained consumed Foley history has independent prepared capacity and safe delayed reuse.</summary>
    public sealed class SoundHistoryPoolTests
    {
        /// <summary>Keeps small fixture requests small while production explicitly reserves its longer sound history.</summary>
        [Test]
        public void SoundCapacityIsIndependentAndSmallRequestsKeepTheirEventDefault()
        {
            var small = new SimulationCapacityRequirements(1, 2, 3, 4, 5);
            Assert.That(small.SoundEvents, Is.EqualTo(4));
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7123);
            Assert.That(game.GetCapacityRequirements().SoundEvents,
                Is.EqualTo(SimulationCapacityRequirements.MaximumSoundEvents));
        }

        /// <summary>Checks production preparation preserves every saved sound field and never constructs an entire history in one step.</summary>
        [Test]
        public void PreparationAdoptsRetainedHistoryIncrementallyWithoutChangingItsCheckpoint()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7123);
            var sounds = game.Presentation.ZombieSounds;
            for (var index = 0; index < 32; index++)
            {
                sounds.Add(new AdventureZombieSoundEvent
                {
                    Id = index + 1, Foley = 25, Tick = index, Pitch = .25f,
                    Resolved = true, SampleIndex = 2, ResolvedPitch = .75f
                });
            }
            var checkpoint = game.CaptureCheckpoint();
            var random = game.BattleRandomDrawCount;
            var requirements = new SimulationCapacityRequirements(0, 0, 0, 4, 0, soundEvents: 64);
            Prepare(game, requirements, 256);
            Assert.That(game.CheckpointState.LifetimePool.ActiveSoundEvents, Is.EqualTo(32));
            Assert.That(game.CheckpointState.LifetimePool.AvailableSoundEvents, Is.EqualTo(64));
            Assert.That(game.CaptureCheckpoint(), Is.EqualTo(checkpoint));
            Assert.That(game.BattleRandomDrawCount, Is.EqualTo(random));
        }

        /// <summary>Checks an oversized restored history still reaches finite preparation by reserving bounded inactive grace capacity.</summary>
        [Test]
        public void HistoryLargerThanItsTargetStillPreparesFiniteIdleGraceCapacity()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7123);
            for (var index = 0; index < 8200; index++)
            {
                game.Presentation.ZombieSounds.Add(new AdventureZombieSoundEvent { Id = index + 1, Resolved = true });
            }
            var requirements = new SimulationCapacityRequirements(0, 0, 0, 0, 0,
                soundEvents: SimulationCapacityRequirements.MaximumSoundEvents);
            Prepare(game, requirements, 9000);
            Assert.That(game.CheckpointState.LifetimePool.ActiveSoundEvents, Is.EqualTo(8200));
            Assert.That(game.CheckpointState.LifetimePool.AvailableSoundEvents, Is.EqualTo(256));
        }

        /// <summary>Checks real Foley requests, consumed 4096-entry history, burst admission and two-boundary grace recycle without GC.</summary>
        [Test]
        public void ConsumedHistoryAndBurstsRecycleWithoutManagedAllocations()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7123);
            var requirements = new SimulationCapacityRequirements(0, 0, 0, 0, 0,
                soundEvents: SimulationCapacityRequirements.MaximumSoundEvents);
            Prepare(game, requirements, 1000);
            var world = game.CheckpointState;
            var feedback = (ZombieFeedbackSystem)game.MemberOwner("RequestAdventureZombieFoley");
            var threshold = (int)typeof(GameSoundEffects).GetField("ConsumedSoundHistoryThreshold",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
            Emit(feedback, world, 1);
            world.Presentation.ZombieSounds.Clear();
            world.LifetimePool.CompleteFrame(world);
            world.LifetimePool.CompleteFrame(world);
            world.LifetimePool.CompleteFrame(world);
            var constructed = world.LifetimePool.CreatedCount;
            var read = 0;
            var serial = 0;
            var before = GC.GetAllocatedBytesForCurrentThread();
            Emit(feedback, world, threshold - 1);
            var first = world.Presentation.ZombieSounds[0];
            var firstId = first.Id;
            world.LifetimePool.CompleteFrame(world);
            Emit(feedback, world, 128);
            ConsumeLikeAudio(world, threshold, ref read, ref serial);
            var cleared = world.Presentation.ZombieSounds.Count == 0;
            world.LifetimePool.CompleteFrame(world);
            var retainedFirstBoundary = first.Id == firstId;
            Emit(feedback, world, 128);
            ConsumeLikeAudio(world, threshold, ref read, ref serial);
            world.LifetimePool.CompleteFrame(world);
            var retainedSecondBoundary = first.Id == firstId;
            Emit(feedback, world, 128);
            ConsumeLikeAudio(world, threshold, ref read, ref serial);
            world.LifetimePool.CompleteFrame(world);
            var recycledAfterGrace = first.Id == 0 && !first.Resolved;
            for (var cycle = 0; cycle < 3; cycle++)
            {
                Emit(feedback, world, threshold - world.Presentation.ZombieSounds.Count);
                ConsumeLikeAudio(world, threshold, ref read, ref serial);
                world.LifetimePool.CompleteFrame(world);
                Emit(feedback, world, 128);
                ConsumeLikeAudio(world, threshold, ref read, ref serial);
                world.LifetimePool.CompleteFrame(world);
                Emit(feedback, world, 128);
                ConsumeLikeAudio(world, threshold, ref read, ref serial);
                world.LifetimePool.CompleteFrame(world);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(cleared, Is.True);
            Assert.That(retainedFirstBoundary, Is.True);
            Assert.That(retainedSecondBoundary, Is.True);
            Assert.That(recycledAfterGrace, Is.True);
            Assert.That(serial, Is.GreaterThan(threshold * 3));
            Assert.That(world.LifetimePool.CreatedCount, Is.EqualTo(constructed));
            Assert.That(allocated, Is.Zero, "Advancing history, bursts, clearing and delayed recycling must stay inside prepared capacity.");
        }

        /// <summary>Uses the production request path while marking playback resolved without opening an audio device.</summary>
        /// <param name="feedback">The connected production Foley request owner.</param>
        /// <param name="world">The authoritative history receiving requests.</param>
        /// <param name="count">The number of requests in this burst.</param>
        private static void Emit(ZombieFeedbackSystem feedback, SimulationWorld world, int count)
        {
            for (var index = 0; index < count; index++)
            {
                feedback.RequestAdventureZombieFoley(-1);
                var sound = world.Presentation.ZombieSounds[world.Presentation.ZombieSounds.Count - 1];
                sound.Resolved = true;
                sound.SampleIndex = -1;
            }
        }

        /// <summary>Mirrors the unchanged audio reader's retention rule without changing the production clear threshold.</summary>
        /// <param name="world">The authoritative ordered event history.</param>
        /// <param name="threshold">The production audio consumer's actual constant.</param>
        /// <param name="read">The simulated consumer read index.</param>
        /// <param name="serial">The number of distinct consumed requests.</param>
        private static void ConsumeLikeAudio(SimulationWorld world, int threshold, ref int read, ref int serial)
        {
            while (read < world.Presentation.ZombieSounds.Count)
            {
                read++;
                serial++;
            }
            if (read >= threshold)
            {
                world.Presentation.ZombieSounds.Clear();
                read = 0;
            }
        }

        /// <summary>Runs bounded preparation calls and checks construction remains within the caller's per-step budget.</summary>
        /// <param name="game">The target match.</param>
        /// <param name="requirements">The explicit fixture capacity request.</param>
        /// <param name="limit">The maximum preparation calls before treating noncompletion as a failure.</param>
        private static void Prepare(GameSimulation game, SimulationCapacityRequirements requirements, int limit)
        {
            var complete = false;
            for (var step = 0; step < limit && !complete; step++)
            {
                var before = game.RuntimePoolStatistics.CreatedCount;
                complete = game.PrepareCapacityStep(requirements, 16);
                Assert.That(game.RuntimePoolStatistics.CreatedCount - before, Is.LessThanOrEqualTo(16));
            }
            Assert.That(complete, Is.True, "Active history exceeding a total target must never make incremental preparation spin forever.");
        }
    }
}
