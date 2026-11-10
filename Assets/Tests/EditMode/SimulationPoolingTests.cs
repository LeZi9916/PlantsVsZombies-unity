#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks pooled gameplay state, safe mutation snapshots, and steady-state allocation budgets.</summary>
    public sealed class SimulationPoolingTests
    {
        /// <summary>Checks every projectile field against construction defaults after recycling.</summary>
        [Test]
        public void ProjectileResetRestoresEveryFieldAndRetainsHitStorage()
        {
            var pea = new Pea();
            DirtyScalarFields(pea);
            pea.SourceKind = PlantKind.WinterMelon;
            pea.SourceIsImitated = true;
            pea.PresentationSnowTrail = new AdventureSnowTrail();
            pea.HitIds.Add(42);
            var hitIds = pea.HitIds;

            pea.Reset();

            Assert.That(pea.HitIds, Is.SameAs(hitIds));
            Assert.That(pea.HitIds, Is.Empty);
            Assert.That(pea.SourceKind, Is.EqualTo(PlantKind.Peashooter));
            Assert.That(pea.SourceIsImitated, Is.False);
            AssertFieldsEqual(new Pea(), pea);
        }

        /// <summary>Checks that a collected native sun cannot transfer flight or fade state to the next spawn.</summary>
        [Test]
        public void SunResetRestoresEveryField()
        {
            var sun = new global::PlantsVsZombies.Simulation.Sun();
            DirtyScalarFields(sun);
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(global::PlantsVsZombies.Sun.SunKind.Large, 150f, 220f);

            sun.Reset();

            AssertFieldsEqual(new global::PlantsVsZombies.Simulation.Sun(), sun);
        }

        /// <summary>Checks nested snapshots preserve original membership and ordering while the live list mutates.</summary>
        [Test]
        public void SnapshotKeepsOriginalOrderAcrossNestedMutationAndEarlyExit()
        {
            var first = new Plant();
            var second = new Plant();
            var third = new Plant();
            var plants = new List<Plant> { first, second, third };
            var seen = new List<Plant>();
            foreach (var plant in new PooledSnapshot<Plant>(plants))
            {
                seen.Add(plant);
                if (plant == first)
                {
                    plants.Remove(second);
                    plants.Add(new Plant());
                    foreach (var nested in new PooledSnapshot<Plant>(plants))
                    {
                        Assert.That(nested, Is.SameAs(first));
                        break;
                    }
                }
            }

            Assert.That(seen, Is.EqualTo(new[] { first, second, third }));
        }

        /// <summary>Checks snapshot buffers are returned when the loop body throws.</summary>
        [Test]
        public void SnapshotReturnsItsBufferAfterAnException()
        {
            var plants = new List<Plant> { new Plant() };
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var plant in new PooledSnapshot<Plant>(plants))
                {
                    throw new InvalidOperationException();
                }
            });

            foreach (var plant in new PooledSnapshot<Plant>(plants))
            {
                Assert.That(plant, Is.SameAs(plants[0]));
            }
        }

        /// <summary>Checks retired projectiles and suns are actually reused by the gameplay spawn paths.</summary>
        [Test]
        public void GameplayReusesRetiredProjectilesAndCollectedSuns()
        {
            var game = CreateGame();
            Assert.That(game.TryPlant(PlantKind.Peashooter, 2, 0), Is.True);
            game.Zombies.Add(new Zombie { Row = 2, X = 9f, Health = 100000f });
            game.Update(2f);
            Assert.That(game.Peas.Count, Is.GreaterThan(0));
            var original = game.Peas[0];
            original.X = 11f;
            game.Update(.01f);
            Assert.That(game.Peas.Contains(original), Is.False);
            for (var i = 0; i < 200 && game.Peas.Count == 0; i++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Peas.Count, Is.GreaterThan(0));
            Assert.That(game.Peas[0], Is.SameAs(original));

            var sunGame = CreateGame(.02f);
            sunGame.Update(.02f);
            var sun = sunGame.Suns[0];
            sun.PresentationNative = true;
            sun.PresentationFromPlant = true;
            Assert.That(sunGame.CollectSun(sun), Is.True);
            sunGame.Update(.02f);
            Assert.That(sunGame.Suns[0], Is.SameAs(sun));
            Assert.That(sun.PresentationNative, Is.False);
            Assert.That(sun.PresentationFromPlant, Is.False);
            Assert.That(sun.Age, Is.LessThan(.03f));
        }

        /// <summary>Checks repeated snapshots, lookups, and warmed simulation ticks allocate no managed bytes.</summary>
        [Test]
        public void WarmSimulationAndQueriesDoNotAllocate()
        {
            var game = CreateGame();
            game.TryPlant(PlantKind.Peashooter, 2, 0);
            game.Zombies.Add(new Zombie { Row = 2, X = 9f, Health = 100000f });
            var plants = game.Plants;
            RunWarmTicks(game, plants, 200);

            var before = GC.GetAllocatedBytesForCurrentThread();
            RunWarmTicks(game, plants, 300);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero, "Warmed gameplay ticks and list snapshots must not allocate.");
        }

        /// <summary>Checks per-zombie simulation steps reuse normalized catalog asset names.</summary>
        [Test]
        public void ZombieAssetLookupsDoNotAllocate()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal };
            var normal = GameSimulation.AdventureZombiePresentationAsset(zombie);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                zombie.Kind = (ZombieKind)(i % 26);
                GC.KeepAlive(GameSimulation.AdventureZombiePresentationAsset(zombie));
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            zombie.Kind = ZombieKind.Normal;
            Assert.That(GameSimulation.AdventureZombiePresentationAsset(zombie), Is.SameAs(normal));
            zombie.Kind = (ZombieKind)(-1);
            Assert.That(GameSimulation.AdventureZombiePresentationAsset(zombie), Is.EqualTo("Zombie"));
        }

        /// <summary>Checks packet destinations clear all old UI flags and match independent snapshots.</summary>
        [Test]
        public void PacketContextReuseRestoresDefaultsAndOverwritesAllState()
        {
            var context = new AdventureSeedBankContext();
            DirtyScalarFields(context);
            context.Reset();
            AssertFieldsEqual(new AdventureSeedBankContext(), context);

            var game = new GameSimulation(new GameRules { AdventureLevel = 8 });
            var expected = game.AdventurePacketContext(PlantKind.SnowPea, false, 64, 1000);
            DirtyScalarFields(context);
            var actual = game.AdventurePacketContext(PlantKind.SnowPea, false, 64, 1000, context);

            Assert.That(actual, Is.SameAs(context));
            AssertFieldsEqual(expected, actual);
            Assert.That(actual.Picked, Is.False);
            Assert.That(actual.EasyPlantingCheat, Is.False);
            Assert.That(actual.SlotMachine, Is.False);
            Assert.That(actual.SlotMachineCountdown, Is.Zero);
        }

        /// <summary>Checks callers that omit a destination retain independent packet-state snapshots.</summary>
        [Test]
        public void PacketContextDefaultCallsKeepIndependentSnapshots()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 8 });
            var first = game.AdventurePacketContext(PlantKind.Peashooter);
            var second = game.AdventurePacketContext(PlantKind.Sunflower);
            Assert.That(second, Is.Not.SameAs(first));
            first.Picked = true;

            game.AdventurePacketContext(PlantKind.CherryBomb, destination: second);

            Assert.That(first.PacketKind, Is.EqualTo((int)PlantKind.Peashooter));
            Assert.That(first.Picked, Is.True);
            Assert.That(second.PacketKind, Is.EqualTo((int)PlantKind.CherryBomb));
            Assert.That(second.Picked, Is.False);
        }

        /// <summary>Checks immediately consumed packet contexts can be filled repeatedly without managed allocation.</summary>
        [Test]
        public void FillingReusablePacketContextDoesNotAllocate()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 8 });
            var context = new AdventureSeedBankContext();
            for (var i = 0; i < 8; i++)
            {
                game.AdventurePacketContext(PlantKind.Peashooter, destination: context);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                game.AdventurePacketContext(i % 2 == 0 ? PlantKind.Peashooter : PlantKind.Sunflower, destination: context);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        /// <summary>Runs the same representative hot-path workload during warmup and measurement.</summary>
        /// <param name="game">The active legacy simulation with a shooter and stationary target.</param>
        /// <param name="plants">The source used for mutation-safe snapshots.</param>
        /// <param name="count">The number of fixed simulation steps.</param>
        private static void RunWarmTicks(GameSimulation game, List<Plant> plants, int count)
        {
            for (var i = 0; i < count; i++)
            {
                game.Update(.01f);
                game.PlantAt(2, 0);
                game.CooldownRemaining(PlantKind.Peashooter);
                foreach (var plant in new PooledSnapshot<Plant>(plants))
                {
                    GC.KeepAlive(plant);
                }
            }
        }

        /// <summary>Creates a deterministic running match with no automatic zombie spawns.</summary>
        /// <param name="skySunInterval">The optional interval used to exercise sun creation.</param>
        /// <returns>A match that stays active while the test advances fixed steps.</returns>
        private static GameSimulation CreateGame(float skySunInterval = 0f)
        {
            return new GameSimulation(new GameRules
            {
                StartingSun = 1000,
                ZombieCount = 1,
                FirstZombieDelay = float.MaxValue,
                SkySunInterval = skySunInterval,
                ZombieSpeed = 0f
            });
        }

        /// <summary>Changes every mutable scalar field so reset coverage grows with the simulation entry.</summary>
        /// <param name="instance">The entry whose fields are dirtied.</param>
        private static void DirtyScalarFields(object instance)
        {
            foreach (var field in instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly)
                {
                    continue;
                }

                if (field.FieldType == typeof(int))
                {
                    field.SetValue(instance, 17);
                }
                else if (field.FieldType == typeof(float))
                {
                    field.SetValue(instance, 19f);
                }
                else if (field.FieldType == typeof(bool))
                {
                    field.SetValue(instance, true);
                }
                else if (field.FieldType.IsEnum)
                {
                    field.SetValue(instance, Enum.ToObject(field.FieldType, 1));
                }
            }
        }

        /// <summary>Compares every mutable public field with a fresh entry's defaults.</summary>
        /// <param name="expected">The fresh entry whose values define construction defaults.</param>
        /// <param name="actual">The recycled entry to inspect.</param>
        private static void AssertFieldsEqual(object expected, object actual)
        {
            foreach (var field in expected.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!field.IsInitOnly)
                {
                    Assert.That(field.GetValue(actual), Is.EqualTo(field.GetValue(expected)), field.Name);
                }
            }
        }
    }
}
