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
    /// <summary>Checks original lobber body clocks, independent butter immobilization and contact-plant rules.</summary>
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

        /// <summary>Exercises an almost-eaten Squash through real ordinary zombie updates from its look into flight.</summary>
        /// <param name="level">The original day, night or roof scene.</param>
        /// <param name="alreadyEating">Whether the ordinary zombie has already latched its eating state.</param>
        [TestCase(9, false)]
        [TestCase(19, true)]
        [TestCase(49, true)]
        public void TriggeredSquashSurvivesOrdinaryZombieChewingUntilAirborne(int level, bool alreadyEating)
        {
            var game = Board(level);
            if (game.Adventure.Level.HasRoof)
            {
                Place(game, PlantKind.FlowerPot, 1, 4);
            }

            var plant = Place(game, PlantKind.Squash, 1, 4);
            plant.Health = 4f;
            var zombie = Spawn(game, ZombieKind.Normal, 1, 350f);
            zombie.Eating = alreadyEating;
            for (var tick = 0; tick < 126; tick++)
            {
                Tick(game, 1);
                Assert.That(game.Plants, Has.Member(plant), "Squash must survive triggered update " + tick);
                Assert.That(plant.Health, Is.EqualTo(4f));
            }

            Assert.That(plant.SquashPresentation!.NativePhase, Is.EqualTo(5));
        }

        /// <summary>Checks a damaged roof support can be eaten without deleting its already-airborne Squash.</summary>
        [Test]
        public void EatenFlowerPotKeepsTheAlreadyAirborneSquashAlive()
        {
            var game = Board(49);
            var pot = Place(game, PlantKind.FlowerPot, 1, 4);
            pot.Health = 4f;
            var squash = Place(game, PlantKind.Squash, 1, 4);
            squash.Health = 4f;
            var zombie = Spawn(game, ZombieKind.Normal, 1, 350f);
            zombie.Eating = true;
            Tick(game, 126);
            Assert.That(squash.SquashPresentation!.NativePhase, Is.EqualTo(5));
            Tick(game, 4);
            Assert.That(game.Plants, Has.No.Member(pot), "The ordinary zombie can now reach the worn support.");
            Assert.That(game.Plants, Has.Member(squash), "PC NotOnGround excludes the airborne Squash from support-stack cleanup.");
            Assert.That(squash.Health, Is.EqualTo(4f));
            Tick(game, 60);
            Assert.That(game.Zombies, Has.No.Member(zombie), "The surviving Squash still completes its original impact.");
            Assert.That(squash.SquashPresentation.NativePhase, Is.EqualTo(7));
        }

        /// <summary>Checks a Chomper finishes eating an eligible adjacent bungee before or during its grab of another plant.</summary>
        /// <param name="grabbing">Whether the bungee starts capturing its own target while the Chomper is biting.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ChomperCompletesItsBiteAgainstAnEligibleBungee(bool grabbing)
        {
            var game = Board();
            var plant = Place(game, PlantKind.Chomper, 1, 4);
            var cargo = grabbing ? Place(game, PlantKind.Wallnut, 1, 5) : null;
            var zombie = Spawn(game, ZombieKind.Bungee, 1, 440f);
            zombie.TargetColumn = 5;
            zombie.PresentationNativePhase = 6;
            zombie.PresentationAltitude = 0f;
            zombie.BungeeState!.Enter(6, grabbing ? 20 : 300);
            Tick(game, 1);
            Assert.That(plant.ChomperPresentation!.NativePhase, Is.EqualTo(10));
            Tick(game, 70);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(plant.ChomperPresentation.NativePhase, Is.EqualTo(11));
            if (cargo != null)
            {
                Assert.That(game.Plants, Has.Member(cargo));
                Assert.That(cargo.BungeeState, Is.Zero, "Swallowing the grabbing bungee restores the other captured plant.");
            }
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
