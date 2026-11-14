#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Timing;
using PlantsVsZombies.Modes;
using PlantKind = PlantsVsZombies.Simulation.PlantKind;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original lobber body clocks and independent butter immobilization.</summary>
    public sealed class AdventureButterAndReloadTests
    {
        /// <summary>Checks complete native shooting playback holds its counter and restarts a fresh twelve-FPS idle.</summary>
        /// <param name="kind">The original lobber species.</param>
        /// <param name="frames">The independently transcribed original source clip length.</param>
        [TestCase(PlantKind.CabbagePult, 38)]
        [TestCase(PlantKind.KernelPult, 34)]
        [TestCase(PlantKind.MelonPult, 38)]
        [TestCase(PlantKind.WinterMelon, 38)]
        public void CompletedShootingStartsTheOriginalFreshIdleClock(PlantKind kind, int frames)
        {
            var game = Board();
            var plant = Place(game, kind, 1, 2);
            plant.Timing = PlantTiming.FromRaw((PlantsVsZombies.Timing.PlantKind)(int)kind, 1, 0);
            var zombie = Spawn(game, ZombieKind.Normal, 1, 600f);
            zombie.Health = zombie.StartingHealth = 10000f;
            zombie.FrozenRemaining = 1000f;
            Tick(game, 1);
            Assert.That(plant.PresentationShotAge, Is.Zero);
            Assert.That(plant.PresentationLobberIdleAge, Is.EqualTo(-1f));
            var completion = (int)Math.Ceiling(frames * 100d / 35d);
            Tick(game, completion - 1);
            Assert.That(plant.PresentationLobberIdleAge, Is.EqualTo(-1f));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(1), "PC keeps the spent shooting counter until the body clip finishes.");
            Tick(game, 1);
            Assert.That(plant.PresentationLobberIdleAge, Is.Zero);
            Assert.That(plant.PresentationRate, Is.EqualTo(12f));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.Zero);
            Tick(game, 5);
            Assert.That(plant.PresentationLobberIdleAge, Is.EqualTo(.05f).Within(.00001f));
        }

        /// <summary>Checks PC 4768F9 preserves the static I, Zombie idle after a completed corn shot.</summary>
        [Test]
        public void IZombieRetainsTheOriginalStaticReloadedIdle()
        {
            var game = new ModeSession(61, randomSeed: 7529).Game;
            game.Plants.Clear();
            game.Zombies.Clear();
            var plant = Place(game, PlantKind.KernelPult, 1, 2);
            plant.PresentationShotAge = 34f / 35f;
            Invoke(game, "AdventurePresentationTick", .01f);
            Assert.That(plant.PresentationLobberIdleAge, Is.Zero);
            Assert.That(plant.PresentationRate, Is.Zero);
            for (var tick = 0; tick < 20; tick++)
            {
                Invoke(game, "AdventurePresentationTick", .01f);
            }

            Assert.That(plant.PresentationLobberIdleAge, Is.Zero);
        }

        /// <summary>Checks firing captures butter projectile metadata then restores the basket's ordinary kernel group.</summary>
        [Test]
        public void ButterFiringImmediatelyRestoresTheKernelBasket()
        {
            var game = Board();
            var plant = Place(game, PlantKind.KernelPult, 1, 2);
            plant.PresentationButter = true;
            plant._kernelWeaponPrepared = true;
            Invoke(game, "FireAdventureNativeTimedProjectile", plant);
            Assert.That(game.Peas[0].Butter, Is.True);
            Assert.That(game.Peas[0].Damage, Is.EqualTo(40f));
            Assert.That(plant.PresentationButter, Is.False);
            Assert.That(plant._kernelWeaponPrepared, Is.False);
        }

        /// <summary>Checks a real butter impact holds ordinary motion and playback without entering either cold state.</summary>
        [Test]
        public void ButterImpactImmobilizesWithoutIceOrChillAndExpiresExactly()
        {
            var game = Board();
            var zombie = Spawn(game, ZombieKind.Normal, 1, 500f);
            var projectile = new Pea { SourceKind = PlantKind.KernelPult, Butter = true, Row = 1,
                FlightRow = 1f, X = zombie.X, StartX = zombie.X, VelocityX = 0f, Damage = 40f };
            game.Peas.Add(projectile);
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(zombie.PresentationButter, Is.EqualTo(4f));
            Assert.That(zombie.FrozenRemaining, Is.Zero);
            Assert.That(zombie.ChillRemaining, Is.Zero);
            var x = zombie.X;
            var age = zombie.PresentationAnimationAge;
            Tick(game, 20);
            Assert.That(zombie.X, Is.EqualTo(x));
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(age));
            Tick(game, 380);
            Assert.That(zombie.PresentationButter, Is.Zero);
            Assert.That(zombie.FrozenRemaining, Is.Zero);
            Assert.That(zombie.ChillRemaining, Is.Zero);
        }

        /// <summary>Checks original butter admission rejects targets that PC ApplyButter cannot immobilize.</summary>
        /// <param name="kind">The original target species.</param>
        /// <param name="phase">The original movement phase.</param>
        /// <param name="headless">Whether the original head is absent.</param>
        /// <param name="allowed">Whether PC 54E470 admits butter.</param>
        [TestCase(ZombieKind.Normal, 0, false, true)]
        [TestCase(ZombieKind.Normal, 0, true, false)]
        [TestCase(ZombieKind.Zomboni, 0, false, false)]
        [TestCase(ZombieKind.Boss, 87, false, false)]
        [TestCase(ZombieKind.Pogo, 20, false, false)]
        [TestCase(ZombieKind.Bungee, 6, false, true)]
        [TestCase(ZombieKind.Bungee, 7, false, false)]
        public void ButterUsesTheOriginalHeadAndMovementGates(ZombieKind kind, int phase, bool headless, bool allowed)
        {
            var game = Board();
            var zombie = new Zombie { Kind = kind, PresentationNativePhase = phase, Health = 500f, Headless = headless };
            Assert.That(game.ApplyAdventureButter(zombie), Is.EqualTo(allowed));
            Assert.That(zombie.PresentationButter, Is.EqualTo(allowed ? 4f : 0f));
            Assert.That(zombie.FrozenRemaining, Is.Zero);
        }

        /// <summary>Checks fire removes actual cold counters while keeping the separate native butter counter.</summary>
        [Test]
        public void FireRemovesIceWithoutRemovingButter()
        {
            var game = Board();
            var zombie = Spawn(game, ZombieKind.Normal, 1, 500f);
            zombie.PresentationButter = 4f;
            zombie.FrozenRemaining = 2f;
            zombie.ChillRemaining = 10f;
            Invoke(game, "ApplyAdventureProjectileCold", new Pea { Fire = true }, zombie);
            Assert.That(zombie.PresentationButter, Is.EqualTo(4f));
            Assert.That(zombie.FrozenRemaining, Is.Zero);
            Assert.That(zombie.ChillRemaining, Is.Zero);
        }

        /// <summary>Creates a controlled night board without scheduled waves or graves.</summary>
        /// <param name="level">The original scene, defaulting to the controlled night board.</param>
        /// <returns>The playing production board.</returns>
        private static GameSimulation Board(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            game.Adventure._graves.Clear();
            return game;
        }

        /// <summary>Places a plant through its production initializer.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="kind">The native plant type.</param>
        /// <param name="row">The native board row.</param>
        /// <param name="column">The native board column.</param>
        /// <returns>The initialized live plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, column)!;
        }

        /// <summary>Creates a real actor at an original pixel position.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="kind">The native zombie type.</param>
        /// <param name="row">The native board row.</param>
        /// <param name="x">The original horizontal position.</param>
        /// <returns>The initialized live actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, row, AdventureZombieNativeRules.GridX(x), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(x);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Runs complete production updates at the original fixed rate.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="ticks">The number of hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Invokes an existing simulation entry point.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The original method name.</param>
        /// <param name="arguments">The original arguments.</param>
        /// <returns>The original method result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
