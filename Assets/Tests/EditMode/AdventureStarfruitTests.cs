#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original PC Starfruit firing, straight star motion and native collision rules.</summary>
    public sealed class AdventureStarfruitTests
    {
        /// <summary>Checks five native top-left origins, pixel velocities, signed spin and fixed-height roof trajectories.</summary>
        /// <param name="level">The original dry, pool or roof board.</param>
        /// <param name="pitch">The board's original row spacing.</param>
        /// <param name="plantY">The original plant top in row two, column four.</param>
        [TestCase(11, 100f, 280f)]
        [TestCase(29, 85f, 250f)]
        [TestCase(41, 85f, 260f)]
        public void FiveStarsUseNativeLaunchVectorsAndStraightTrajectories(int level, float pitch, float plantY)
        {
            var game = Board(level);
            var plant = Plant(game);
            Invoke(game, "FireAdventureStarfruit", plant);
            Assert.That(game.Peas, Has.Count.EqualTo(5));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 3), Has.Count.EqualTo(1));
            var vx = new[] { -3.33f, 0f, 0f, 2.883864f, 2.883864f };
            var vy = new[] { 0f, 3.33f, -3.33f, 1.665f, -1.665f };
            for (var index = 0; index < 5; index++)
            {
                var shot = game.Peas[index];
                Assert.That(40f + shot.X * 80f, Is.EqualTo(385f).Within(.0001f));
                Assert.That(AdventureStarfruitRules.PositionY(shot, pitch), Is.EqualTo(plantY + 25f));
                Assert.That(shot.VelocityX * 80f / 100f, Is.EqualTo(vx[index]).Within(.00001f));
                Assert.That(shot.VelocityY * pitch / 100f, Is.EqualTo(vy[index]).Within(.00001f));
                Assert.That(Math.Abs(shot.PresentationSpin), Is.InRange(.05f, .1f));
                Assert.That(shot.PresentationRotation, Is.Zero);
                Assert.That(shot.PiercesShield, Is.EqualTo(index == 0));
                Assert.That(shot.Damage, Is.EqualTo(20f));
                Assert.That(shot.Splash, Is.False);
            }

            Move(game, 20);
            Assert.That(game.Peas, Has.Count.EqualTo(5));
            for (var index = 0; index < 5; index++)
            {
                var shot = game.Peas[index];
                Assert.That(40f + shot.X * 80f, Is.EqualTo(385f + vx[index] * 20f).Within(.001f));
                Assert.That(AdventureStarfruitRules.PositionY(shot, pitch), Is.EqualTo(plantY + 25f + vy[index] * 20f).Within(.001f),
                    "Star motion is a straight native line, including across a roof slope.");
            }
        }

        /// <summary>Checks native launch cadence, the thirty-ninth later update, target loss and recently eaten fallback.</summary>
        [Test]
        public void ScheduledBurstKeepsOriginalCountdownAndDoesNotReacquireItsTarget()
        {
            var game = Board();
            var plant = Plant(game);
            plant.StarfruitLaunchCountdown = 1;
            var target = Spawn(game, ZombieKind.Normal, 300f);
            Assert.That(Invoke(game, "FindAdventureStarfruitTarget", plant), Is.SameAs(target));
            Invoke(game, "AdvanceAdventureStarfruit", plant);
            Assert.That(plant.StarfruitShootingCountdown, Is.EqualTo(40));
            Assert.That(plant.StarfruitLaunchCountdown, Is.InRange(136, 150));
            var interval = plant.StarfruitLaunchCountdown;
            game.Zombies.Clear();
            for (var tick = 0; tick < 38; tick++)
            {
                Invoke(game, "AdvanceAdventureStarfruit", plant);
            }

            Assert.That(game.Peas, Is.Empty);
            Assert.That(plant.StarfruitLaunchCountdown, Is.EqualTo(interval - 38));
            Invoke(game, "AdvanceAdventureStarfruit", plant);
            Assert.That(game.Peas, Has.Count.EqualTo(5));
            Assert.That(plant.StarfruitShootingCountdown, Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureStarfruit", plant);
            Assert.That(game.Peas, Has.Count.EqualTo(5));
            Assert.That(plant.StarfruitShootingCountdown, Is.Zero);
            plant.StarfruitLaunchCountdown = 1;
            plant.RecentlyEatenTicks = 50;
            Invoke(game, "AdvanceAdventureStarfruit", plant);
            Assert.That(plant.StarfruitShootingCountdown, Is.EqualTo(40), "An actual recent bite admits the original attack without a target.");
        }

        /// <summary>Checks original same-row reverse targeting and the damage-mask gates through real target acquisition.</summary>
        [Test]
        public void TargetAcquisitionSkipsProtectedDiggerEmergenceAndUsesStationaryDizzyLead()
        {
            var game = Board();
            var plant = Plant(game);
            var zombie = Spawn(game, ZombieKind.Digger, 250f);
            foreach (var phase in new[] { 32, 33, 34, 35 })
            {
                zombie.PresentationNativePhase = phase;
                zombie.AbilityPhase = phase == 32 ? 0 : 1;
                Assert.That(Invoke(game, "FindAdventureStarfruitTarget", plant), Is.Null, "PC range bit one excludes digger phase " + phase + ".");
            }

            zombie.PresentationNativePhase = 36;
            zombie.Submerged = false;
            Assert.That(AdventureStarfruitRules.TargetNotWalking(zombie), Is.True);
            Assert.That(Invoke(game, "FindAdventureStarfruitTarget", plant), Is.SameAs(zombie));
            zombie.PresentationNativePhase = 37;
            Assert.That(AdventureStarfruitRules.TargetNotWalking(zombie), Is.False);
            zombie.X = AdventureZombieNativeRules.GridX(500f);
            Assert.That(Invoke(game, "FindAdventureStarfruitTarget", plant), Is.Null, "A same-row zombie ahead cannot trigger a five-way attack.");
        }

        /// <summary>Checks the original vertical and strict diagonal targeting windows independently of projectile visuals.</summary>
        [Test]
        public void TargetPathsRetainOriginalVerticalDiagonalAndReverseRules()
        {
            var plant = new Plant { Kind = PlantKind.Starfruit, Row = 2, Column = 4 };
            var zombie = new Zombie { Kind = ZombieKind.Normal, Row = 2, Health = 270f };
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(350, 280, 42, 115), 280, 0f, true), Is.True);
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(358, 280, 42, 115), 280, 0f, true), Is.False);
            zombie.Row = 1;
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(390, 180, 42, 115), 280, 0f, true), Is.True);
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(500, 180, 42, 115), 280, 0f, true), Is.True);
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(500, 180, 42, 115), 280, 2f, true), Is.False);
            Assert.That(AdventureStarfruitRules.CanTarget(plant, zombie, new PlantsVsZombies.BoardUI.NativeRect(700, 180, 42, 115), 280, 0f, true), Is.False);
        }

        /// <summary>Checks the original star ignores Torchwood and only its backward vector bypasses a screen door.</summary>
        [Test]
        public void BackwardStarIgnoresTorchwoodAndBypassesTheOriginalHeldShield()
        {
            var game = Board();
            var plant = Plant(game);
            Invoke(game, "PlaceAdventurePlant", PlantKind.Torchwood, 2, 3);
            Invoke(game, "FireAdventureStarfruit", plant);
            var shot = game.Peas[0];
            game.Peas.Clear();
            game.Peas.Add(shot);
            Move(game, 20);
            Assert.That(shot.Fire, Is.False);
            Assert.That(shot.Damage, Is.EqualTo(20f));
            Assert.That(shot.Splash, Is.False);
            var target = Spawn(game, ZombieKind.ScreenDoor, 240f);
            var health = target.Health;
            var shield = target.ShieldHealth;
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(game.Peas, Is.Empty);
            Assert.That(target.Health, Is.EqualTo(health - 20f));
            Assert.That(target.ShieldHealth, Is.EqualTo(shield));
            var impact = game.Presentation.Impacts[0];
            Assert.That(impact.NativeIcePoint, Is.True);
            Assert.That(impact.NativeParticleX, Is.EqualTo(40f + shot.X * 80f + 12f).Within(.001f));
            Assert.That(impact.NativeParticleY, Is.EqualTo(317f).Within(.001f));
        }

        /// <summary>Checks a downward star retains the native collision row until its top-left crosses the next grid boundary.</summary>
        [Test]
        public void VerticalStarUsesPixelGridRowInsteadOfRoundingFlightDisplacement()
        {
            var game = Board();
            Invoke(game, "FireAdventureStarfruit", Plant(game));
            var shot = game.Peas[1];
            game.Peas.Clear();
            game.Peas.Add(shot);
            Move(game, 22);
            Assert.That(shot.FlightRow, Is.GreaterThan(2.7f));
            Assert.That(shot.Row, Is.EqualTo(2), "PC maps y=378.26 to row two despite being nearer row three's center.");
            Move(game, 1);
            Assert.That(shot.Row, Is.EqualTo(3));
        }

        /// <summary>Checks the original twenty-five-update miner protection and the backward-star exception.</summary>
        /// <param name="index">The original backward or downward vector.</param>
        /// <param name="age">The projectile's age before the next native update.</param>
        /// <param name="hits">Whether the original miner gate admits the collision.</param>
        [TestCase(1, .23f, false)]
        [TestCase(1, .24f, true)]
        [TestCase(0, 0f, true)]
        public void FreshNonBackwardStarsWaitTwentyFiveUpdatesBeforeHittingDigger(int index, float age, bool hits)
        {
            var game = Board();
            Invoke(game, "FireAdventureStarfruit", Plant(game));
            var shot = game.Peas[index];
            shot.PresentationNativeAge = age;
            game.Peas.Clear();
            game.Peas.Add(shot);
            var zombie = Spawn(game, ZombieKind.Digger, 350f);
            zombie.PresentationNativePhase = 37;
            zombie.AbilityPhase = 2;
            zombie.Submerged = false;
            zombie.ArmorHealth = 0f;
            var health = zombie.Health;
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(zombie.Health, Is.EqualTo(hits ? health - 20f : health));
            Assert.That(game.Peas.Count, Is.EqualTo(hits ? 0 : 1));
        }

        /// <summary>Checks original star bounds keep exact boundary positions and reject the first point outside them.</summary>
        [Test]
        public void StarUsesOriginalNativeScreenBounds()
        {
            Assert.That(AdventureStarfruitRules.OffBoard(800f, 40f), Is.False);
            Assert.That(AdventureStarfruitRules.OffBoard(-40f, 600f), Is.False);
            Assert.That(AdventureStarfruitRules.OffBoard(800.01f, 300f), Is.True);
            Assert.That(AdventureStarfruitRules.OffBoard(-40.01f, 300f), Is.True);
            Assert.That(AdventureStarfruitRules.OffBoard(400f, 39.99f), Is.True);
            Assert.That(AdventureStarfruitRules.OffBoard(400f, 600.01f), Is.True);
        }

        /// <summary>Creates an active original board while postponing unrelated automatic waves.</summary>
        /// <param name="level">The original adventure board definition.</param>
        /// <returns>The deterministic production simulation.</returns>
        private static GameSimulation Board(int level = 11)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a production Starfruit at the shared original test cell.</summary>
        /// <param name="game">The board receiving the plant.</param>
        /// <returns>The original plant in row two, column four.</returns>
        private static Plant Plant(GameSimulation game)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Starfruit, 2, 4)!;
        }

        /// <summary>Creates a real production target at one native horizontal origin.</summary>
        /// <param name="game">The active board receiving the target.</param>
        /// <param name="kind">The original zombie kind.</param>
        /// <param name="nativeX">The original board-pixel zombie origin.</param>
        /// <returns>The production target in row two.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, float nativeX)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, 2, AdventureZombieNativeRules.GridX(nativeX), 0)!;
        }

        /// <summary>Advances only production projectiles at the original native update rate.</summary>
        /// <param name="game">The board containing the live projectiles.</param>
        /// <param name="ticks">The exact native update count.</param>
        private static void Move(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                Invoke(game, "AdvanceAdventureProjectiles", .01f, false);
            }
        }

        /// <summary>Invokes a private production operation without introducing runtime test hooks.</summary>
        /// <param name="game">The production board owning the operation.</param>
        /// <param name="name">The production operation name.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The original operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
