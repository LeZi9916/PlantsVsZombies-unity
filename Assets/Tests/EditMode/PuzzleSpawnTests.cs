#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original puzzle placement order and randomized green-vase selection.</summary>
    public sealed class PuzzleSpawnTests
    {
        /// <summary>Checks PC 0x4338B0 maps ticket one to the next row of the same column.</summary>
        /// <remarks>This checks the initializer only; later tutorial or Crazy Dave mutations are outside its scope.</remarks>
        [Test]
        public void AdventureVaseTicketsUseOriginalColumnMajorCells()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 });
            var random = new VaseTicketRandom(false);
            SetRandom(game, random);
            Invoke(game, "InitializeVaseStage");
            var first = game.Adventure.Vases[0];
            Assert.That(first.Row, Is.EqualTo(1));
            Assert.That(first.Column, Is.EqualTo(6));
            Assert.That(first._seed, Is.EqualTo(PlantKind.Peashooter));
            Assert.That(random.Bounds.Count, Is.EqualTo(15), "The first original stage does not call the random green-vase selector.");
            foreach (var vase in game.Adventure.Vases)
            {
                Assert.That(vase.ShowsPlant, Is.False);
            }
        }

        /// <summary>Checks original stage transitions select green vases from all remaining plant contents.</summary>
        [Test]
        public void AdventureGreenVasesUseRandomSelectionWithoutReplacement()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 });
            game.Adventure.VaseStage = 1;
            var random = new VaseTicketRandom(true);
            SetRandom(game, random);
            Invoke(game, "InitializeVaseStage");
            var greenCount = 0;
            foreach (var vase in game.Adventure.Vases)
            {
                if (vase.ShowsPlant)
                {
                    greenCount++;
                    Assert.That(vase._seed, Is.EqualTo(PlantKind.Squash), "PC 0x433810 selects the last remaining plant interval for the injected tickets.");
                }
            }

            Assert.That(greenCount, Is.EqualTo(2));
            Assert.That(random.Bounds[random.Bounds.Count - 2], Is.EqualTo(13));
            Assert.That(random.Bounds[random.Bounds.Count - 1], Is.EqualTo(12));
        }

        /// <summary>Checks independent puzzles randomize their two green plant vases after all hidden contents are placed.</summary>
        [Test]
        public void IndependentGreenVasesUseTheOriginalRandomPlantPool()
        {
            var session = new ModeSession(51);
            var game = session.Game;
            var random = new VaseTicketRandom(true);
            SetRandom(game, random);
            Invoke(game, "InitializeModeVases");
            var greenCount = 0;
            foreach (var vase in game.Adventure.Vases)
            {
                if (vase.ShowsPlant)
                {
                    greenCount++;
                    Assert.That(vase._seed, Is.EqualTo(PlantKind.Squash));
                }
            }

            Assert.That(greenCount, Is.EqualTo(2));
            Assert.That(random.Bounds[random.Bounds.Count - 2], Is.EqualTo(15));
            Assert.That(random.Bounds[random.Bounds.Count - 1], Is.EqualTo(14));
        }

        /// <summary>Checks PC 0x435C3F creates the first fixed I, Zombie sunflower in zero-based row two.</summary>
        [Test]
        public void FirstIZombieFixedSunflowerUsesNativeRow()
        {
            var session = new ModeSession(61, randomSeed: 12345);
            var first = session.Game.Plants[0];
            Assert.That(first.Kind, Is.EqualTo(PlantKind.Sunflower));
            Assert.That(first.Row, Is.EqualTo(2));
            Assert.That(first.Column, Is.EqualTo(3));
        }

        /// <summary>Installs a deterministic source after constructing the explicit isolated fixture.</summary>
        /// <param name="game">The production simulation under test.</param>
        /// <param name="random">The deterministic ticket source.</param>
        private static void SetRandom(GameSimulation game, Random random)
        {
            typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
        }

        /// <summary>Invokes a production puzzle initializer without widening its runtime visibility.</summary>
        /// <param name="game">The production simulation under test.</param>
        /// <param name="method">The original private initializer name.</param>
        private static void Invoke(GameSimulation game, string method)
        {
            typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, Array.Empty<object>());
        }

        /// <summary>Supplies boundary tickets for placement and green-vase selection.</summary>
        private sealed class VaseTicketRandom : Random
        {
            /// <summary>Controls whether the final eligible interval is selected.</summary>
            private readonly bool _last;

            /// <summary>Retains every production exclusive random bound.</summary>
            private readonly List<int> _bounds = new List<int>();

            /// <summary>Gets the observed bounds in call order.</summary>
            public IReadOnlyList<int> Bounds => _bounds;

            /// <summary>Creates the deterministic boundary-ticket source.</summary>
            /// <param name="last">Whether to choose the last eligible interval instead of ticket one.</param>
            public VaseTicketRandom(bool last)
            {
                _last = last;
            }

            /// <summary>Records the exclusive bound and returns a valid deterministic ticket.</summary>
            /// <param name="maxValue">The production exclusive upper bound.</param>
            /// <returns>The last valid ticket, or ticket one when available.</returns>
            public override int Next(int maxValue)
            {
                _bounds.Add(maxValue);
                return _last ? maxValue - 1 : Math.Min(1, maxValue - 1);
            }
        }
    }
}
