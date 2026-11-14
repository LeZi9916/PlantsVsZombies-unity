#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the supplied PC giant's authored action events and retained flattened plants.</summary>
    public sealed class AdventureGargantuarTests
    {
        /// <summary>Checks crushing an awake explosive triggers its original immediate special rather than a flattened picture.</summary>
        [Test]
        public void CrushingAwakeExplosiveRunsItsOriginalSpecialImmediately()
        {
            var game = Board();
            var bomb = Place(game, PlantKind.CherryBomb);
            Invoke(game, "CrushAdventurePlantCell", 1, 3, true);
            Assert.That(game.Plants.Contains(bomb), Is.False);
            Assert.That(bomb.RequiredCherryBombPresentation.Detonated, Is.True);
            Assert.That(game.Presentation.SquishedPlants, Is.Empty);
        }

        /// <summary>Checks squishing awake Ice-shroom uses the same original sound, freeze and world particles as its ordinary special.</summary>
        [Test]
        public void SquishingAwakeIceShroomRunsItsCompleteOriginalSpecial()
        {
            var game = Board();
            var ice = Place(game, PlantKind.IceShroom);
            ice.Asleep = false;
            var giant = Spawn(game, 330f);
            Invoke(game, "CrushAdventurePlantCell", 1, 3, false);
            Assert.That(game.Plants.Contains(ice), Is.False);
            Assert.That(game.Presentation.SquishedPlants, Is.Empty);
            Assert.That(giant.FrozenRemaining, Is.GreaterThan(0f));
            Assert.That(Sounds(game, 18), Is.EqualTo(2), "The native special and the giant's first chill each request Frozen Foley.");
            Assert.That(game.Presentation.Explosions.Single().Effect, Is.EqualTo(AdventureExplosionEffect.IceTrap));
        }

        /// <summary>Checks drive-over respects PC Squish's triggered-Squash immunity while flattening its roof support.</summary>
        /// <param name="kind">The original crushing vehicle.</param>
        /// <param name="phase">The original look or prelaunch plant state.</param>
        [TestCase(ZombieKind.Zomboni, 3)]
        [TestCase(ZombieKind.Zomboni, 4)]
        [TestCase(ZombieKind.Catapult, 3)]
        [TestCase(ZombieKind.Catapult, 4)]
        public void VehiclesKeepTriggeredSquashWhileFlatteningItsSupport(ZombieKind kind, int phase)
        {
            var game = Board(49);
            var pot = Place(game, PlantKind.FlowerPot);
            var squash = Place(game, PlantKind.Squash);
            (squash.SquashPresentation ??= new AdventureSquashState()).Enter(phase, 80);
            Invoke(game, "SpawnAdventureBoardZombie", kind, 1, AdventureZombieNativeRules.GridX(250f), 0);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(game.Plants.Contains(squash), Is.True);
            Assert.That(game.Plants.Contains(pot), Is.False);
            Assert.That(game.Presentation.SquishedPlants.Single().Plant, Is.SameAs(pot));
        }

        /// <summary>Checks original own-cell Doom cleanup also removes previously squished allocated pictures.</summary>
        [Test]
        public void DoomSpecialClearsOnlyItsOwnPreviouslyCrushedPictures()
        {
            var game = Board();
            Place(game, PlantKind.Sunflower);
            Invoke(game, "PlaceAdventurePlant", PlantKind.Sunflower, 1, 4);
            Invoke(game, "CrushAdventurePlantCell", 1, 3, false);
            Invoke(game, "CrushAdventurePlantCell", 1, 4, false);
            var doom = Place(game, PlantKind.DoomShroom);
            doom.Asleep = false;
            doom.DoomShroomPresentation = new AdventureDoomShroomState { Countdown = 0 };
            Invoke(game, "AdvanceAdventureDoomShroom", doom);
            Assert.That(game.Presentation.SquishedPlants.Single().Plant.Column, Is.EqualTo(4));
            Assert.That(game.Adventure.Craters.Count, Is.EqualTo(1));
        }

        /// <summary>Creates a quiet production board with no scheduled enemies or preplanted objects.</summary>
        /// <param name="level">The original board background.</param>
        /// <returns>The initialized playing simulation.</returns>
        internal static GameSimulation Board(int level = 9)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            game.Adventure._graves.Clear();
            return game;
        }

        /// <summary>Creates a production plant at the controlled smash cell.</summary>
        /// <param name="game">The receiving board.</param>
        /// <param name="kind">The original plant type.</param>
        /// <returns>The initialized plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 3)!;
        }

        /// <summary>Creates a production giant at an exact original pixel position.</summary>
        /// <param name="game">The receiving board.</param>
        /// <param name="x">The original native horizontal position.</param>
        /// <returns>The initialized giant.</returns>
        private static Zombie Spawn(GameSimulation game, float x)
        {
            return (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Gargantuar, 1, AdventureZombieNativeRules.GridX(x), 0)!;
        }

        /// <summary>Advances complete production updates without replacing action or presentation clocks.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="count">The number of original hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int count)
        {
            for (var tick = 0; tick < count; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Counts actual production Foley requests for an original sound.</summary>
        /// <param name="game">The simulation containing sound requests.</param>
        /// <param name="id">The original Foley identifier.</param>
        /// <returns>The number of matching requests.</returns>
        private static int Sounds(GameSimulation game, int id)
        {
            return game.Presentation.ZombieSounds.Count(sound => sound.Foley == id);
        }

        /// <summary>Calls a production entry point without replacing its behavior.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The private entry point name.</param>
        /// <param name="arguments">The original call arguments.</param>
        /// <returns>The entry point result.</returns>
        internal static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
