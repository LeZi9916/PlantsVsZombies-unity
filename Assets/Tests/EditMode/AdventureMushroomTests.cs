#nullable enable

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original sleep ownership and Doom cell cleanup.</summary>
    public sealed class AdventureMushroomTests
    {
        /// <summary>Checks every nocturnal species sleeps only on the original daytime backgrounds.</summary>
        /// <param name="level">The ground, pool, fog, roof or boss background.</param>
        /// <param name="asleep">Whether original StageIsNight permits sleeping.</param>
        [TestCase(9, true)]
        [TestCase(19, false)]
        [TestCase(29, true)]
        [TestCase(39, false)]
        [TestCase(49, true)]
        [TestCase(50, false)]
        public void AllMushroomsOwnIndependentSleepingAnimationsByBackground(int level, bool asleep)
        {
            foreach (var kind in new[] { PlantKind.PuffShroom, PlantKind.SunShroom, PlantKind.FumeShroom,
                PlantKind.HypnoShroom, PlantKind.ScaredyShroom, PlantKind.IceShroom, PlantKind.DoomShroom,
                PlantKind.SeaShroom, PlantKind.MagnetShroom, PlantKind.GloomShroom })
            {
                var game = Board(level);
                var plant = Place(game, kind, 1, 2);
                Assert.That(plant.Asleep, Is.EqualTo(asleep), kind.ToString());
                var state = plant.SleepingPresentation;
                if (!asleep)
                {
                    Assert.That(state, Is.Null);
                    continue;
                }

                Assert.That(state, Is.Not.Null);
                Assert.That(state!.Rate, Is.InRange(6f, 8f));
                Assert.That(state.AnimationSeconds * state.Rate / 17f, Is.InRange(0f, .9f));
                var seconds = state.AnimationSeconds;
                Tick(game, 20);
                Assert.That(state.AnimationSeconds, Is.EqualTo(seconds + .2f).Within(.00001f));
                Assert.That(plant.PresentationAwakeAge, Is.Zero);
                Assert.That(game.Peas, Is.Empty);
                Assert.That(plant.DoomShroomPresentation, Is.Null);
            }
        }

        /// <summary>Checks the free Z animation continues while a bungee owns the sleeping body and disappears on wake.</summary>
        [Test]
        public void SleepingClockRemainsIndependentOfCapturedBodyAndCoffeeWake()
        {
            var game = Board(49);
            var plant = Place(game, PlantKind.PuffShroom, 1, 2);
            var state = plant.SleepingPresentation!;
            var age = state.AnimationSeconds;
            plant.BungeeState = 2;
            Tick(game, 20);
            Assert.That(state.AnimationSeconds, Is.EqualTo(age + .2f).Within(.00001f));
            plant.BungeeState = 0;
            plant.WakeTimer = .01f;
            Tick(game, 1);
            Assert.That(plant.Asleep, Is.False);
            Assert.That(plant.SleepingPresentation, Is.Null);
            Assert.That(plant.AwakeByCoffee, Is.True);
        }

        /// <summary>Checks native Doom removes every own-cell layer and retains one terrain-independent crater.</summary>
        /// <param name="level">The original dry, water or roof background.</param>
        /// <param name="row">The original blast row.</param>
        /// <param name="column">The original sloped or flat cell.</param>
        [TestCase(9, 1, 2)]
        [TestCase(19, 1, 2)]
        [TestCase(29, 2, 2)]
        [TestCase(39, 2, 2)]
        [TestCase(49, 1, 2)]
        [TestCase(49, 1, 7)]
        [TestCase(50, 1, 2)]
        public void DoomExplosionClearsItsCellAcrossAllOriginalBackgrounds(int level, int row, int column)
        {
            var game = Board(level);
            var support = Place(game, game.Adventure.Level.IsWaterRow(row) ? PlantKind.LilyPad : PlantKind.FlowerPot, row, column);
            var doom = Place(game, PlantKind.DoomShroom, row, column);
            var shell = Place(game, PlantKind.Pumpkin, row, column);
            var neighbor = Place(game, PlantKind.Wallnut, row, column + 1);
            support.HasLadder = shell.HasLadder = true;
            doom.Asleep = false;
            doom.DoomShroomPresentation = new AdventureDoomShroomState { Countdown = 1 };
            Invoke(game, "AdvanceAdventureDoomShroom", doom);
            Assert.That(game.Plants, Has.No.Member(support));
            Assert.That(game.Plants, Has.No.Member(shell));
            Assert.That(game.Plants, Has.No.Member(doom));
            Assert.That(game.Plants, Has.Member(neighbor), "Native KillAllPlantsNearDoom removes only its own cell.");
            Assert.That(game.AdventureHasLadderAt(row, column), Is.False);
            Assert.That(game.Adventure.Craters, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.Craters[0].Remaining, Is.EqualTo(180f));
            Assert.That(Invoke(game, "CheckAdventurePlanting", PlantKind.FlowerPot, row, column, true), Is.EqualTo(AdventurePlantingFailure.Crater));
        }

        /// <summary>Checks the original eighteen-thousand-update crater expires on its exact native boundary.</summary>
        [Test]
        public void CraterOccupancyExpiresAfterExactlyEighteenThousandUpdates()
        {
            var game = Board(49);
            var doom = Place(game, PlantKind.DoomShroom, 1, 2);
            doom.Asleep = false;
            doom.DoomShroomPresentation = new AdventureDoomShroomState { Countdown = 1 };
            Invoke(game, "AdvanceAdventureDoomShroom", doom);
            Tick(game, 17999);
            Assert.That(game.Adventure.Craters, Has.Count.EqualTo(1));
            Tick(game, 1);
            Assert.That(game.Adventure.Craters, Is.Empty);
        }

        /// <summary>Creates a playing production board with no scheduled waves or initial occupants.</summary>
        /// <param name="level">The original adventure background.</param>
        /// <returns>The controlled production board.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            return game;
        }

        /// <summary>Places an original plant through its production initializer.</summary>
        /// <param name="game">The controlled board.</param>
        /// <param name="kind">The original plant identifier.</param>
        /// <param name="row">The zero-based row.</param>
        /// <param name="column">The zero-based column.</param>
        /// <returns>The live initialized plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row, int column)
        {
            if (kind == PlantKind.GloomShroom && game.AdventurePlantAt(row, column, PlantLayer.Main) == null)
            {
                Invoke(game, "PlaceAdventurePlant", PlantKind.FumeShroom, row, column);
            }

            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, column)!;
        }

        /// <summary>Advances the original fixed update loop.</summary>
        /// <param name="game">The playing board.</param>
        /// <param name="ticks">The exact number of hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Invokes an unchanged production action at a controlled native boundary.</summary>
        /// <param name="game">The controlled board.</param>
        /// <param name="method">The original action method name.</param>
        /// <param name="arguments">The action's original arguments.</param>
        /// <returns>The unmodified production result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
