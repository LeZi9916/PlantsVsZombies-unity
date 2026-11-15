#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects ownership boundaries, stable indexes and deterministic behavior dispatch.</summary>
    public sealed class SimulationArchitectureTests
    {
        /// <summary>Checks reference membership independently of native identifiers and snapshot membership.</summary>
        [Test]
        public void OrderedStoreKeepsIdentityAndNestedMutationSemantics()
        {
            var game = new GameSimulation();
            var store = SimulationFixture.Plants(game);
            var first = new Plant { Id = 7, Row = 0, Column = 0 };
            var second = new Plant { Id = 8, Row = 0, Column = 1 };
            store.Add(first);
            store.Add(second);
            var seen = new List<Plant>();
            foreach (var plant in new PooledSnapshot<Plant>(store))
            {
                seen.Add(plant);
                if (plant == first)
                {
                    store.Remove(second);
                    store.Add(new Plant { Id = 8, Row = 1, Column = 2 });
                }
            }
            Assert.That(seen, Is.EqualTo(new[] { first, second }));
            Assert.That(store.Contains(second), Is.False);
            Assert.That(store.FindById(8), Is.SameAs(store[1]));
            first.Id = 9;
            Assert.That(store.FindById(7), Is.Null);
            Assert.That(store.FindById(9), Is.SameAs(first));
        }
        /// <summary>Checks footprint, layer, dynamic availability and mutation invalidation.</summary>
        [Test]
        public void CellIndexPreservesLayerAndTwoCellRulesAcrossMutation()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 49 });
            var store = SimulationFixture.Plants(game);
            var cannon = new Plant { Id = 1, Kind = PlantKind.CobCannon, Row = 2, Column = 3, ColumnSpan = 2, Layer = PlantLayer.Main };
            var shell = new Plant { Id = 2, Kind = PlantKind.Pumpkin, Row = 2, Column = 3, Layer = PlantLayer.Armor };
            store.Add(cannon);
            store.Add(shell);
            Assert.That(game.AdventurePlantAt(2, 4, PlantLayer.Main), Is.SameAs(cannon));
            Assert.That(game.AdventurePlantAt(2, 3, PlantLayer.Armor), Is.SameAs(shell));
            cannon.BungeeState = 2;
            Assert.That(game.AdventurePlantAt(2, 4, PlantLayer.Main), Is.Null);
            cannon.BungeeState = 0;
            cannon.Column = 5;
            Assert.That(game.AdventurePlantAt(2, 4, PlantLayer.Main), Is.Null);
            Assert.That(game.AdventurePlantAt(2, 6, PlantLayer.Main), Is.SameAs(cannon));
            cannon.ColumnSpan = 1;
            Assert.That(game.AdventurePlantAt(2, 6, PlantLayer.Main), Is.Null);
            store.Remove(cannon);
            Assert.That(game.AdventurePlantAt(2, 5, PlantLayer.Main), Is.Null);
        }
        /// <summary>Checks lane changes preserve global tie order rather than move-to-lane order.</summary>
        [Test]
        public void LaneIndexUsesGlobalOrderAfterMovingOutAndBack()
        {
            var game = new GameSimulation();
            var store = SimulationFixture.Zombies(game);
            var first = new Zombie { Id = 1, Row = 2, X = 6f };
            var second = new Zombie { Id = 2, Row = 2, X = 6f };
            store.Add(first);
            store.Add(second);
            Assert.That(store.InRow(2), Is.EqualTo(new[] { first, second }));
            first.Row = 3;
            Assert.That(store.InRow(2), Is.EqualTo(new[] { second }));
            first.Row = 2;
            Assert.That(store.InRow(2), Is.EqualTo(new[] { first, second }));
        }
        /// <summary>Checks retained native IDs during replacement never revive the retired reference.</summary>
        [Test]
        public void ReplacementPreservesIdWithoutPreservingOldMembership()
        {
            var game = new GameSimulation();
            var store = SimulationFixture.Plants(game);
            var old = new Plant { Id = 17, Kind = PlantKind.Imitater };
            store.Add(old);
            Assert.That(store.FindById(17), Is.SameAs(old));
            store.Remove(old);
            var replacement = new Plant { Id = 18, Kind = PlantKind.Peashooter };
            store.Add(replacement);
            replacement.Id = 17;
            Assert.That(store.FindById(18), Is.Null);
            Assert.That(store.FindById(17), Is.SameAs(replacement));
            Assert.That(store.Contains(old), Is.False);
        }
        /// <summary>Checks specialty state is stored by the matching behavior and public reads are passive.</summary>
        [Test]
        public void BehaviorsOwnSpecialStateWithoutAllocatingDuringReadOnlyProbes()
        {
            var plant = new Plant { Kind = PlantKind.Chomper };
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                GC.KeepAlive(plant.ChomperPresentation);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            var state = new AdventureChomperState();
            plant.ChomperPresentation = state;
            Assert.That(((ChomperPlantBehavior)plant.Behavior).State, Is.SameAs(state));
            var zombie = new Zombie(ZombieKind.Gargantuar);
            zombie.GargantuarState = new AdventureGargantuarState { ActionTriggered = true };
            Assert.That(((GargantuarZombieBehavior)zombie.Behavior).State, Is.SameAs(zombie.GargantuarState));
            zombie.Kind = ZombieKind.Normal;
            Assert.That(zombie.GargantuarState, Is.Null);
            Assert.That(zombie.Behavior, Is.TypeOf<WalkingZombieBehavior>());
        }
        /// <summary>Checks public actor state is read-only and collections expose no mutation interface.</summary>
        [Test]
        public void PublicEntitiesAndCollectionsDoNotExposeUncontrolledWrites()
        {
            foreach (var type in new[] { typeof(Plant), typeof(Zombie) })
            {
                Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    Assert.That(property.SetMethod == null || !property.SetMethod.IsPublic, Is.True, property.Name);
                }
            }
            var game = new GameSimulation();
            Assert.That(game.Plants, Is.Not.InstanceOf<IList<Plant>>());
            Assert.That(game.Zombies, Is.Not.InstanceOf<IList<Zombie>>());
            Assert.That(game.Plants, Is.SameAs(game.Plants));
        }
        /// <summary>Checks the clock preserves stop timing and independent fractional remainders.</summary>
        [Test]
        public void FixedStepClockStopsAtTheSameTickAndRetainsRemainder()
        {
            var accumulated = 0d;
            var ticks = 0;
            FixedStepClock.Advance(ref accumulated, .035f, () => ticks < 2, _ => ticks++);
            Assert.That(ticks, Is.EqualTo(2));
            Assert.That(accumulated, Is.EqualTo(.015d).Within(.0000001d));
            FixedStepClock.Advance(ref accumulated, 0f, () => true, _ => ticks++);
            Assert.That(ticks, Is.EqualTo(3));
            Assert.That(accumulated, Is.EqualTo(.005d).Within(.0000001d));
        }
        /// <summary>Checks presentation queries never apply action transitions or consume randomness.</summary>
        [Test]
        public void RepeatedAnimationReadsPreserveNativeStateAndRandomPosition()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 });
            var zombie = new Zombie { PresentationAnimation = "held", PresentationAnimationAge = .35f, Eating = true };
            var draws = game.BattleRandomDrawCount;
            for (var i = 0; i < 20; i++)
            {
                Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_eat"));
                game.AdventurePresentationZombieRate(zombie);
            }
            Assert.That(zombie.PresentationAnimation, Is.EqualTo("held"));
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(.35f));
            Assert.That(game.BattleRandomDrawCount, Is.EqualTo(draws));
            Assert.That(SimulationFixture.SynchronizeZombieAnimation(game, zombie), Is.EqualTo("anim_eat"));
            Assert.That(zombie.PresentationAnimationAge, Is.Zero);
        }
        /// <summary>Checks production updates synchronize meal transitions without help from presentation queries.</summary>
        [Test]
        public void ProductionUpdatesSynchronizeEatingAndWalkingAnimationTransitions()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var plant = new Plant { Id = 1, Kind = PlantKind.Wallnut, Row = 2, Column = 4, Layer = PlantLayer.Main, Health = 4000f };
            SimulationFixture.Plants(game).Add(plant);
            var zombie = (Zombie)SimulationInspection.GetMethod("SpawnAdventureBoardZombie")
                .Invoke(game, new object[] { ZombieKind.Normal, 2, AdventureZombieNativeRules.GridX(350f), 0 })!;
            zombie.Age = .03f;
            zombie.PresentationBodyBlendTicks = 0;
            Assert.That(zombie.Eating, Is.False);

            game.Update(.01f);

            Assert.That(zombie.Eating, Is.True);
            Assert.That(zombie.PresentationAnimation, Is.EqualTo("anim_eat"));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(20));
            Assert.That(zombie.PresentationAnimationAge, Is.Zero);
            Assert.That(plant.Health, Is.EqualTo(3996f));
            SimulationFixture.Plants(game).Remove(plant);
            zombie.PresentationBodyBlendTicks = 0;

            game.Update(.04f);

            Assert.That(zombie.Eating, Is.False);
            Assert.That(zombie.PresentationAnimation, Does.StartWith("anim_walk"));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(20));
        }

        /// <summary>Checks nested scratch leases retain independent candidates and clear returned references.</summary>
        [Test]
        public void NestedScratchLeasesRemainIndependentAndReturnOnExceptions()
        {
            var pool = new ScratchBufferPool<Plant>();
            List<Plant> outerItems;
            using (var outer = pool.Rent())
            {
                outerItems = outer.Items;
                outer.Items.Add(new Plant());
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var nested = pool.Rent();
                    Assert.That(nested.Items, Is.Not.SameAs(outer.Items));
                    nested.Items.Add(new Plant());
                    throw new InvalidOperationException();
                });
                Assert.That(outer.Items.Count, Is.EqualTo(1));
            }
            using var reused = pool.Rent();
            Assert.That(reused.Items, Is.SameAs(outerItems));
            Assert.That(reused.Items, Is.Empty);
        }
        /// <summary>Checks indexed target selection against a stable full scan and verifies candidate reduction.</summary>
        [Test]
        public void LaneCandidatesPreserveNearestTargetAndReduceChecks()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 });
            var store = SimulationFixture.Zombies(game);
            for (var i = 0; i < 100; i++)
            {
                store.Add(new Zombie { Id = i + 1, Row = i % 5, X = 4f + i % 3, Health = 270f, StartingHealth = 270f });
            }
            var queries = (BoardQueries)game.MemberOwner("FindAdventureTarget");
            for (var row = 0; row < 5; row++)
            {
                Zombie? expected = null;
                foreach (var zombie in store)
                {
                    if (zombie.Row == row && (expected == null || zombie.X < expected.X))
                    {
                        expected = zombie;
                    }
                }
                Assert.That(queries.FindAdventureTarget(row, 0f, 20f, false), Is.SameAs(expected));
                Assert.That(store.InRow(row).Count, Is.EqualTo(20));
            }
        }

        /// <summary>Checks read-only dancer lists do not box an enumerator during linked movement queries.</summary>
        [Test]
        public void DancerGroupQueriesDoNotAllocateThroughReadOnlyCollections()
        {
            var game = new GameSimulation();
            var store = SimulationFixture.Zombies(game);
            var leader = new Zombie(ZombieKind.Dancing) { Id = 1, PresentationNativePhase = 44, DancerState = new AdventureDancerState() };
            var follower = new Zombie(ZombieKind.BackupDancer) { Id = 2, PresentationNativePhase = 44, DancerState = new AdventureDancerState { LeaderId = 1 } };
            leader.DancerState!.Followers[0] = 2;
            store.Add(leader);
            store.Add(follower);
            for (var i = 0; i < 100; i++)
            {
                AdventureDancerRules.CanWalk(follower, game.Zombies);
            }
            var walking = true;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                walking &= AdventureDancerRules.CanWalk(follower, game.Zombies);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(walking, Is.True);
            Assert.That(allocated, Is.Zero);
        }

        /// <summary>Checks warmed identity and lane queries allocate no managed memory.</summary>
        [Test]
        public void WarmedIndexesAndReadOnlyViewsDoNotAllocate()
        {
            var game = new GameSimulation();
            var store = SimulationFixture.Zombies(game);
            for (var i = 0; i < 100; i++)
            {
                store.Add(new Zombie { Id = i + 1, Row = i % 5 });
            }
            store.InRow(0);
            store.FindById(1);
            var alive = true;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var repeat = 0; repeat < 100; repeat++)
            {
                foreach (var zombie in game.Zombies)
                {
                    GC.KeepAlive(store.FindById(zombie.Id));
                    GC.KeepAlive(store.InRow(zombie.Row));
                    alive &= store.Contains(zombie);
                }
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(alive, Is.True);
            Assert.That(allocated, Is.Zero);
        }
    }
}
