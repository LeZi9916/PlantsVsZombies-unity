#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original PC planting displacement and production random-consumption boundaries.</summary>
    public sealed class AdventureRandomPlacementTests
    {
        /// <summary>Checks the exact signed planting endpoints and X-before-Y draws on awake and sleeping mushrooms.</summary>
        /// <param name="kind">The original mushroom species.</param>
        /// <param name="yBound">The native exclusive vertical bound.</param>
        [TestCase(PlantKind.PuffShroom, 6)]
        [TestCase(PlantKind.SeaShroom, 6)]
        [TestCase(PlantKind.SunShroom, 10)]
        public void PlantingSamplesPersistentOriginalOffsets(PlantKind kind, int yBound)
        {
            foreach (var level in new[] { 9, 19, 29, 39, 49 })
            {
                foreach (var last in new[] { false, true })
                {
                    var game = Board(level);
                    var random = AttachRandom(game, last);
                    var plant = (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 2)!;
                    Assert.That(plant.PositionOffsetX, Is.EqualTo(last ? 4 : -5));
                    Assert.That(plant.PositionOffsetY, Is.EqualTo(last ? yBound / 2 - 1 : -yBound / 2));
                    Assert.That(random.Bounds.Skip(random.Bounds.Count - 2), Is.EqualTo(new[] { 10, yBound }));
                    Assert.That(plant.Row, Is.EqualTo(1));
                    Assert.That(plant.Column, Is.EqualTo(2));
                    Assert.That(game.AdventurePlantAt(1, 2, PlantLayer.Main), Is.SameAs(plant));
                    game.Update(.1f);
                    Assert.That(plant.PositionOffsetX, Is.EqualTo(last ? 4 : -5));
                    Assert.That(plant.PositionOffsetY, Is.EqualTo(last ? yBound / 2 - 1 : -yBound / 2));
                }
            }
        }

        /// <summary>Checks ordinary plants retain exact cell alignment and consume no invented planting jitter.</summary>
        /// <param name="kind">An original species outside the three offset branches.</param>
        [TestCase(PlantKind.Peashooter)]
        [TestCase(PlantKind.Sunflower)]
        [TestCase(PlantKind.Wallnut)]
        [TestCase(PlantKind.FumeShroom)]
        [TestCase(PlantKind.PotatoMine)]
        [TestCase(PlantKind.Imitater)]
        public void OtherPlantsKeepTheirCellOrigin(PlantKind kind)
        {
            var game = Board(19);
            game.Adventure.ImitaterTarget = PlantKind.Peashooter;
            var random = AttachRandom(game, true);
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 2)!;
            Assert.That(plant.PositionOffsetX, Is.Zero);
            Assert.That(plant.PositionOffsetY, Is.Zero);
            Assert.That(random.Bounds.Contains(10), Is.False);
            Assert.That(random.Bounds.Contains(6), Is.False);
        }

        /// <summary>Checks displaced native attack, bite, projectile and pickup coordinates use the same persistent plant origin.</summary>
        /// <param name="kind">The original displaced mushroom.</param>
        [TestCase(PlantKind.PuffShroom)]
        [TestCase(PlantKind.SeaShroom)]
        public void MushroomCoordinatesFollowPlantingDisplacement(PlantKind kind)
        {
            var game = Board(39);
            var plant = new Plant { Kind = kind, Row = 1, Column = 2, PositionOffsetX = -5, PositionOffsetY = 2 };
            var bite = (global::PlantsVsZombies.BoardUI.NativeRect)Invoke(game, "AdventurePlantEatingRectangle", plant)!;
            Assert.That(bite.X, Is.EqualTo(205));
            Assert.That(bite.Y, Is.EqualTo(167));
            var shot = (Pea)Invoke(game, "FireAdventurePea", plant, 1, false, 20f)!;
            Assert.That(shot.X, Is.EqualTo(2f + (kind == PlantKind.SeaShroom ? 56f : 51f) / 80f).Within(.00001f));
            Assert.That(shot.PresentationLaunchY, Is.EqualTo(kind == PlantKind.SeaShroom ? 243.5f : 220.5f));
            var unshifted = new Plant { Kind = kind, Row = 1, Column = 2 };
            var zombie = new Zombie { Kind = ZombieKind.Normal, Row = 1, Health = 270f };
            // Place the body's left edge four pixels inside the unshifted range's far boundary.
            zombie.X = AdventureZombieNativeRules.GridX(40 + 2 * 80 + 60 + 230 - 4 - 36);
            Assert.That(AdventurePuffShroomRules.CanTarget(unshifted, zombie), Is.True);
            Assert.That(AdventurePuffShroomRules.CanTarget(plant, zombie), Is.False);
            plant.Kind = PlantKind.SunShroom;
            var sun = (global::PlantsVsZombies.Simulation.Sun)Invoke(game, "CreateAdventurePresentationSun", plant, 15)!;
            Assert.That(sun.PresentationPickup!.X, Is.EqualTo(195));
            Assert.That(sun.PresentationPickup.Y, Is.EqualTo(167));
        }

        /// <summary>Checks the catapult's one-hundred-pixel admission boundary follows the displaced native plant position.</summary>
        [Test]
        public void CatapultAcquiresTheDisplacedPlantAtItsOriginalDistanceBoundary()
        {
            var game = Board(39);
            var plant = new Plant { Kind = PlantKind.PuffShroom, Row = 1, Column = 2, Health = 300f,
                Layer = PlantLayer.Main, PositionOffsetX = -5 };
            SimulationFixture.Plants(game).Add(plant);
            var zombie = new Zombie { Kind = ZombieKind.Catapult, Row = 1, X = AdventureZombieNativeRules.GridX(294f) };
            Assert.That(Invoke(game, "FindAdventureCatapultTarget", zombie), Is.Null);
            zombie.X = AdventureZombieNativeRules.GridX(295f);
            Assert.That(Invoke(game, "FindAdventureCatapultTarget", zombie), Is.SameAs(plant));
        }

        /// <summary>Creates a production board with distant waves and its real native seed bank.</summary>
        /// <param name="level">The original terrain and day/night fixture.</param>
        /// <returns>The playing deterministic board.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Invokes a production collaborator through the existing test inspection boundary.</summary>
        /// <param name="game">The board owning the collaborator.</param>
        /// <param name="method">The original operation name.</param>
        /// <param name="arguments">The operation's arguments.</param>
        /// <returns>The operation's return value, or null for void.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Installs controlled first or last integer tickets without consuming constructor draws.</summary>
        /// <param name="game">The board whose subsequent draws are controlled.</param>
        /// <param name="last">Whether to return the last valid ticket.</param>
        /// <returns>The recording random stream.</returns>
        private static TicketRandom AttachRandom(GameSimulation game, bool last)
        {
            var random = new TicketRandom(last);
            SimulationInspection.GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
            return random;
        }

        /// <summary>Records exclusive integer bounds and provides stable native float tickets.</summary>
        private sealed class TicketRandom : Random
        {
            /// <summary>Stores whether the controlled stream returns each bound's last ticket.</summary>
            private readonly bool _last;
            /// <summary>Gets the integer draw bounds in call order.</summary>
            internal List<int> Bounds { get; } = new List<int>();

            /// <summary>Initializes the controlled endpoint stream.</summary>
            /// <param name="last">Whether bounded draws choose their last ticket.</param>
            internal TicketRandom(bool last)
            {
                _last = last;
            }

            /// <inheritdoc/>
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return _last ? maxValue - 1 : 0;
            }

            /// <inheritdoc/>
            public override int Next()
            {
                return 1;
            }

            /// <inheritdoc/>
            public override double NextDouble()
            {
                return .5;
            }
        }
    }
}
