#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC Gloom-shroom body overlap at acquisition and each scheduled area pulse.</summary>
    public sealed class AdventureGloomShroomTests
    {
        /// <summary>Checks both native rectangle edges, including touching and one-pixel gaps, through production actions.</summary>
        /// <param name="kind">The species selecting the original logical body rectangle.</param>
        /// <param name="bodyOffset">The body's horizontal offset from its native actor origin.</param>
        /// <param name="bodyWidth">The original logical body width, independent of visible artwork.</param>
        [TestCase(ZombieKind.Normal, 36, 42)]
        [TestCase(ZombieKind.Zomboni, 0, 153)]
        [TestCase(ZombieKind.Catapult, 0, 153)]
        [TestCase(ZombieKind.Gargantuar, -17, 125)]
        public void NativeEdgesAllowTouchingToTriggerButRequireOverlapToDamage(ZombieKind kind, int bodyOffset, int bodyWidth)
        {
            // PC 4765B0: a plant at x=280 uses [200,440]. PC 475AA0 admits overlap >= 0; 46D030 requires > 0.
            for (var side = 0; side < 2; side++)
            {
                for (var gap = -1; gap <= 1; gap++)
                {
                    var game = Board();
                    var plant = Place(game);
                    var bodyLeft = side == 0 ? 200 - bodyWidth - gap : 440 + gap;
                    var zombie = Target(game, kind, 2, bodyLeft - bodyOffset);
                    plant.ActionTimer = 0f;
                    Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
                    Assert.That(plant.UpgradeBurstTicks, Is.EqualTo(gap <= 0 ? 200 : 0),
                        $"{kind}, side={side}, gap={gap}: native target acquisition includes touching edges.");
                    plant.ActionTimer = 10f;
                    plant.UpgradeBurstTicks = 127;
                    Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
                    Assert.That(zombie.Health, Is.EqualTo(gap < 0 ? 250f : 270f),
                        $"{kind}, side={side}, gap={gap}: native area damage requires positive body overlap.");
                    Assert.That(game.Peas, Is.Empty);
                }
            }
        }

        /// <summary>Checks native row selection independently of the visual rectangle's vertical dimensions.</summary>
        /// <param name="rowOffset">The candidate's row relative to the mushroom.</param>
        /// <param name="admitted">Whether the native adjacent-row gate admits the candidate.</param>
        [TestCase(-2, false)]
        [TestCase(-1, true)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        public void OnlyTheSameAndAdjacentRowsTriggerAndReceiveDamage(int rowOffset, bool admitted)
        {
            var game = Board();
            var plant = Place(game);
            var zombie = Target(game, ZombieKind.Normal, 2 + rowOffset, 248);
            plant.ActionTimer = 0f;
            Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
            Assert.That(plant.UpgradeBurstTicks, Is.EqualTo(admitted ? 200 : 0));
            plant.ActionTimer = 10f;
            plant.UpgradeBurstTicks = 127;
            Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
            Assert.That(zombie.Health, Is.EqualTo(admitted ? 250f : 270f));
        }

        /// <summary>Checks each original pulse reacquires current bodies and bypasses a screen door without a projectile.</summary>
        [Test]
        public void ScheduledPulsesRecheckCurrentBodyOverlapAndPierceTheDoor()
        {
            var game = Board();
            var plant = Place(game);
            var zombie = Target(game, ZombieKind.ScreenDoor, 2, 403);
            zombie.ShieldHealth = 1100f;
            plant.ActionTimer = 10f;
            foreach (var remaining in new[] { 127, 99, 71, 43 })
            {
                plant.UpgradeBurstTicks = remaining;
                Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
                // Move from one-pixel overlap to touch, then a gap, and finally into range again.
                zombie.X = AdventureZombieNativeRules.GridX((remaining == 127 ? 404 : remaining == 99 ? 405 : 403) + .25f);
            }

            Assert.That(zombie.Health, Is.EqualTo(230f));
            Assert.That(zombie.ShieldHealth, Is.EqualTo(1100f));
            Assert.That(plant.ActionCount, Is.EqualTo(4));
            Assert.That(game.Peas, Is.Empty);
        }

        /// <summary>Creates an awake night board without imminent automatic waves.</summary>
        /// <returns>The isolated production simulation.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(game).Clear();
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Places a controlled awake Gloom-shroom at original cell origin (280,280).</summary>
        /// <param name="game">The board receiving the mushroom fixture.</param>
        /// <returns>The live mushroom using production upgrade actions.</returns>
        private static Plant Place(GameSimulation game)
        {
            var plant = new Plant { Id = 1, Kind = PlantKind.GloomShroom, Row = 2, Column = 3, Health = 300f };
            SimulationFixture.Plants(game).Add(plant);
            return plant;
        }

        /// <summary>Adds a live target at an explicit PC integer actor origin.</summary>
        /// <param name="game">The board receiving the target fixture.</param>
        /// <param name="kind">The species selecting its logical body geometry.</param>
        /// <param name="row">The target's native row.</param>
        /// <param name="nativeX">The native actor origin before its species-specific body offset.</param>
        /// <returns>The live production target.</returns>
        private static Zombie Target(GameSimulation game, ZombieKind kind, int row, int nativeX)
        {
            var zombie = new Zombie
            {
                Id = 1,
                Kind = kind,
                Row = row,
                X = AdventureZombieNativeRules.GridX(nativeX + .25f),
                Health = 270f,
                StartingHealth = 270f
            };
            SimulationFixture.Zombies(game).Add(zombie);
            return zombie;
        }

        /// <summary>Invokes a production simulation boundary without opening a scene.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The production method name.</param>
        /// <param name="arguments">The method's arguments.</param>
        private static void Invoke(GameSimulation game, string method, params object[] arguments)
        {
            SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
