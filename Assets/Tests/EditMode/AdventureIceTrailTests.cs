#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC ice rows through actual vehicle, planting, and Jalapeno updates.</summary>
    public sealed class AdventureIceTrailTests
    {
        /// <summary>Checks a moving vehicle extends one continuous row and refreshes its shared thirty-second lifetime.</summary>
        [Test]
        public void VehicleExtendsOneRowAndRefreshesTheWholeLifetime()
        {
            var game = Board(19);
            var vehicle = Spawn(game, 682f);
            game.Update(.01f);
            Assert.That(game.Adventure.IceTrails, Is.Empty, "PC waits until mPosX + 118 falls below the right board edge.");
            vehicle.X = AdventureZombieNativeRules.GridX(679f);
            game.Update(.01f);
            Assert.That(game.Adventure.IceTrails, Has.Count.EqualTo(1));
            var trail = game.Adventure.IceTrails[0];
            Assert.That(trail.MinX, Is.EqualTo(797));
            Assert.That(trail.RemainingTicks, Is.EqualTo(3000));
            Assert.That(trail.CoversColumn(8), Is.False, "PC IsIceAt waits until the native front reaches x750.");

            vehicle.X = AdventureZombieNativeRules.GridX(500.4f);
            game.Update(.01f);
            Assert.That(game.Adventure.IceTrails, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.IceTrails[0], Is.SameAs(trail));
            Assert.That(trail.MinX, Is.EqualTo(618));
            Assert.That(trail.CoversColumn(6), Is.False);
            Assert.That(trail.CoversColumn(7), Is.True);
            Assert.That(trail.CoversColumn(8), Is.True);
            Assert.That(trail.RemainingTicks, Is.EqualTo(3000));

            vehicle.X = AdventureZombieNativeRules.GridX(600f);
            game.Update(.01f);
            Assert.That(trail.MinX, Is.EqualTo(618), "A second or reversing vehicle cannot shorten existing ice.");
            Assert.That(trail.RemainingTicks, Is.EqualTo(3000));
            Assert.That(Invoke(game, "ChooseAdventureZombieRow", ZombieKind.Bobsled), Is.EqualTo(1));
            game.Zombies.Remove(vehicle);
            for (var tick = 0; tick < 2999; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Adventure.IceTrails, Has.Count.EqualTo(1));
            Assert.That(trail.RemainingTicks, Is.EqualTo(1));
            Assert.That(trail.Opacity, Is.EqualTo(25f / 255f));
            game.Update(.01f);
            Assert.That(game.Adventure.IceTrails, Is.Empty);
        }

        /// <summary>Checks the original ground and roof clamps on the continuous left edge.</summary>
        /// <param name="level">The board supplying the native terrain.</param>
        /// <param name="nativeX">The original vehicle position before its update.</param>
        /// <param name="edge">The independently checked PC ice clamp.</param>
        [TestCase(19, -100f, 25)]
        [TestCase(41, 100f, 500)]
        public void VehicleUsesOriginalGroundAndRoofClamps(int level, float nativeX, int edge)
        {
            var game = Board(level);
            Spawn(game, nativeX);
            game.Update(.01f);
            Assert.That(game.Adventure.IceTrails, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.IceTrails[0].MinX, Is.EqualTo(edge));
        }

        /// <summary>Checks a planted Jalapeno keeps the original twenty-update melt and final ten-update alpha fade.</summary>
        [Test]
        public void JalapenoMeltsTheRowInsteadOfDeletingItsIceImmediately()
        {
            var game = Board(19);
            var trail = new AdventureIceTrail { Id = 92, Row = 1, MinX = 337, RemainingTicks = 3000 };
            game.Adventure._ice.Add(trail);
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Jalapeno, 1, 2)!;
            plant.JalapenoPresentation!.Countdown = 1;
            game.Update(.01f);
            Assert.That(trail.RemainingTicks, Is.EqualTo(20));
            Assert.That(trail.CoversColumn(2), Is.False);
            Assert.That(trail.CoversColumn(3), Is.True);
            Assert.That(trail.Opacity, Is.EqualTo(1f));
            game.Update(.15f);
            Assert.That(trail.RemainingTicks, Is.EqualTo(5));
            Assert.That(trail.Opacity, Is.EqualTo(127f / 255f));
            Assert.That(trail.CoversColumn(3), Is.True);
            game.Update(.05f);
            Assert.That(game.Adventure.IceTrails, Is.Empty);
            Assert.That(trail.CoversColumn(3), Is.False);
        }

        /// <summary>Checks the original twelve-pixel planting probe and the far-right exclusion independently of artwork.</summary>
        /// <param name="minX">The ice row's native left edge.</param>
        /// <param name="column">The planting column being probed.</param>
        /// <param name="covered">The PC IsIceAt outcome.</param>
        [TestCase(107, 0, true)]
        [TestCase(108, 0, false)]
        [TestCase(108, 1, true)]
        [TestCase(750, 8, true)]
        [TestCase(751, 8, false)]
        public void PlantingUsesOriginalIceProbe(int minX, int column, bool covered)
        {
            var game = Board(19);
            game.Adventure._ice.Add(new AdventureIceTrail { Row = 1, MinX = minX, RemainingTicks = 3000 });
            Assert.That(Invoke(game, "CheckAdventurePlanting", PlantKind.Peashooter, 1, column, true),
                Is.EqualTo(covered ? AdventurePlantingFailure.Ice : AdventurePlantingFailure.None));
        }

        /// <summary>Creates a deterministic board with automatic wave generation postponed.</summary>
        /// <param name="level">The native terrain and adventure stage.</param>
        /// <returns>The active production simulation.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Adventure._graves.Clear();
            return game;
        }

        /// <summary>Spawns a vehicle at a fixed native origin through the production initializer.</summary>
        /// <param name="game">The playing board.</param>
        /// <param name="nativeX">The exact original horizontal position.</param>
        /// <returns>The live ice-producing vehicle.</returns>
        private static Zombie Spawn(GameSimulation game, float nativeX)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Zomboni, 1,
                AdventureZombieNativeRules.GridX(nativeX), 0)!;
        }

        /// <summary>Invokes an existing production operation without exposing a test-only runtime method.</summary>
        /// <param name="game">The board owning the operation.</param>
        /// <param name="name">The nonpublic operation name.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The production result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
