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
    /// <summary>Checks original sleep ownership and independent animation clocks.</summary>
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
