#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects real dense-board plant ticks and packet queries from captured-predicate allocations.</summary>
    public sealed class SimulationHotPathAllocationTests
    {
        /// <summary>Checks every plant is visited without allocating a closure, including plants that never face backward.</summary>
        [Test]
        public void DenseBackwardPlantTickAllocatesNoManagedMemory()
        {
            var game = CreateAdventure();
            for (var index = 0; index < 60; index++)
            {
                SimulationFixture.Plants(game).Add(new Plant
                {
                    Id = index + 1, Kind = PlantKind.Peashooter, Row = index % 5, Column = index % 9,
                    ModeFacesLeft = index % 2 == 0, _modeFireTimer = 100000f, Health = 300f
                });
            }
            var actions = (ModePlantActions)game.MemberOwner("TickModeBackwardPlants");
            actions.TickModeBackwardPlants(.01f);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var tick = 0; tick < 1000; tick++)
            {
                actions.TickModeBackwardPlants(.01f);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(game.Peas.Count, Is.Zero);
        }

        /// <summary>Checks the exact native row, strict x boundary and hypnotized exclusion of backward shooting.</summary>
        /// <param name="row">The target's native row.</param>
        /// <param name="x">The target's horizontal position.</param>
        /// <param name="hypnotized">Whether the target must be excluded.</param>
        /// <param name="expectedShots">The unchanged number of produced projectiles.</param>
        [TestCase(2, 3.5f, false, 0)]
        [TestCase(2, 3.499f, false, 2)]
        [TestCase(1, 3f, false, 0)]
        [TestCase(2, 3f, true, 0)]
        [TestCase(2, -1f, false, 2)]
        public void BackwardTargetQueryPreservesNativeRules(int row, float x, bool hypnotized, int expectedShots)
        {
            var game = CreateAdventure();
            var plant = new Plant
            {
                Id = 1, Kind = PlantKind.Peashooter, Row = 2, Column = 3,
                ModeFacesLeft = true, _modeFireTimer = 0f, Health = 300f
            };
            SimulationFixture.Plants(game).Add(plant);
            SimulationFixture.Zombies(game).Add(new Zombie
            {
                Id = 1, Kind = ZombieKind.Normal, Row = row, X = x, Hypnotized = hypnotized, Health = 270f
            });
            var actions = (ModePlantActions)game.MemberOwner("TickModeBackwardPlants");
            actions.TickModeBackwardPlants(.01f);
            Assert.That(game.Peas.Count, Is.EqualTo(expectedShots));
            Assert.That(plant.ActionTimer, Is.EqualTo(100000f));
            if (expectedShots == 2)
            {
                Assert.That(game.Peas[1].X, Is.EqualTo(game.Peas[0].X + .3f));
                Assert.That(plant._modeFireTimer, Is.EqualTo(1.45f));
            }
            else
            {
                Assert.That(plant._modeFireTimer, Is.EqualTo(-.01f));
            }
        }

        /// <summary>Checks the original Repeater exclusion leaves its action clocks untouched.</summary>
        [Test]
        public void BackwardRepeaterRetainsItsSeparateFiringPath()
        {
            var game = CreateAdventure();
            var plant = new Plant
            {
                Id = 1, Kind = PlantKind.Repeater, Row = 2, Column = 3,
                ModeFacesLeft = true, ActionTimer = 7f, _modeFireTimer = 3f, Health = 300f
            };
            SimulationFixture.Plants(game).Add(plant);
            var actions = (ModePlantActions)game.MemberOwner("TickModeBackwardPlants");
            actions.TickModeBackwardPlants(.01f);
            Assert.That(plant.ActionTimer, Is.EqualTo(7f));
            Assert.That(plant._modeFireTimer, Is.EqualTo(3f));
            Assert.That(game.Peas.Count, Is.Zero);
        }

        /// <summary>Checks coffee requirements remain live across wake clocks and effect-layer occupancy changes.</summary>
        [Test]
        public void CoffeePacketRequirementsPreserveLiveWakeAndLayerQueries()
        {
            var game = CreateAdventure();
            var mushroom = new Plant
            {
                Id = 1, Kind = PlantKind.FumeShroom, Row = 2, Column = 3,
                Asleep = true, WakeTimer = .5f, Health = 300f
            };
            var coffee = new Plant
            {
                Id = 2, Kind = PlantKind.CoffeeBean, Row = 2, Column = 3,
                Layer = PlantLayer.Effect, Health = 300f
            };
            SimulationFixture.Plants(game).Add(mushroom);
            var seeds = (SeedBankSystem)game.MemberOwner("AdventurePacketRequirements");
            Assert.That(seeds.AdventurePacketRequirements(PlantKind.CoffeeBean), Is.False);
            mushroom.WakeTimer = 0f;
            Assert.That(seeds.AdventurePacketRequirements(PlantKind.CoffeeBean), Is.True);
            SimulationFixture.Plants(game).Add(coffee);
            Assert.That(seeds.AdventurePacketRequirements(PlantKind.CoffeeBean), Is.False);
            SimulationFixture.Plants(game).Remove(coffee);
            Assert.That(seeds.AdventurePacketRequirements(PlantKind.CoffeeBean), Is.True);
            mushroom.Asleep = false;
            Assert.That(seeds.AdventurePacketRequirements(PlantKind.CoffeeBean), Is.False);
        }

        /// <summary>Checks changing coffee availability while overwriting a caller-owned packet context remains allocation-free.</summary>
        [Test]
        public void ChangingCoffeePacketContextAllocatesNoManagedMemory()
        {
            var game = CreateAdventure();
            var mushroom = new Plant
            {
                Id = 1, Kind = PlantKind.FumeShroom, Row = 2, Column = 3, Asleep = true, Health = 300f
            };
            SimulationFixture.Plants(game).Add(mushroom);
            var context = new AdventureSeedBankContext();
            game.AdventurePacketContext(PlantKind.CoffeeBean, destination: context);
            var satisfied = 0;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 1000; iteration++)
            {
                mushroom.Asleep = iteration % 2 == 0;
                game.AdventurePacketContext(PlantKind.CoffeeBean, destination: context);
                if (context.PlantingRequirementsMet)
                {
                    satisfied++;
                }
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(satisfied, Is.EqualTo(500));
        }

        /// <summary>Creates a normal board so direct collaborator checks use production indices and packet rules.</summary>
        /// <returns>The initialized original adventure simulation.</returns>
        private static GameSimulation CreateAdventure()
        {
            return new GameSimulation(new GameRules { AdventureLevel = 9 }, randomSeed: 7529);
        }
    }
}
