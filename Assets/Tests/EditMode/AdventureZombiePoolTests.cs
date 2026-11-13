#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original pool-lane eligibility, armored swimming bodies, and fifty-update pool ambushes.</summary>
    public sealed class AdventureZombiePoolTests
    {
        /// <summary>Checks the local PC pool eligibility and the first-five-wave restriction without banning legal air crossings.</summary>
        /// <param name="kind">The original zombie kind.</param>
        /// <param name="water">Whether the candidate row is water.</param>
        /// <param name="wave">The zero-based wave index.</param>
        /// <param name="expected">Whether the native water-specific gates permit this spawn.</param>
        [TestCase(ZombieKind.Normal, true, 4, false)]
        [TestCase(ZombieKind.Normal, true, 5, true)]
        [TestCase(ZombieKind.Conehead, true, 5, true)]
        [TestCase(ZombieKind.Buckethead, true, 5, true)]
        [TestCase(ZombieKind.Flag, true, 5, true)]
        [TestCase(ZombieKind.Balloon, true, 4, false)]
        [TestCase(ZombieKind.Balloon, true, 5, true)]
        [TestCase(ZombieKind.Snorkel, true, 0, true)]
        [TestCase(ZombieKind.Snorkel, false, 5, false)]
        [TestCase(ZombieKind.DolphinRider, true, 0, true)]
        [TestCase(ZombieKind.ScreenDoor, true, 5, false)]
        [TestCase(ZombieKind.Football, true, 5, false)]
        [TestCase(ZombieKind.Dancing, true, 5, false)]
        public void SpawnEligibilityMatchesNativePoolGates(ZombieKind kind, bool water, int wave, bool expected)
        {
            Assert.That(AdventureZombiePoolRules.CanSpawnInRow(kind, water, wave), Is.EqualTo(expected));
        }

        /// <summary>Checks the production row picker excludes ground-only enemies and retains legal later-wave swimming and aquatic lanes.</summary>
        [Test]
        public void ActualRowPickerUsesNativeGroundAquaticAndWaveGates()
        {
            var game = Fixture();
            var pick = typeof(GameSimulation).GetMethod("ChooseAdventureZombieRow", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var wave = 0; wave < 5; wave++)
            {
                game.Adventure.CurrentWave = wave;
                for (var attempt = 0; attempt < 16; attempt++)
                {
                    var row = (int)pick.Invoke(game, new object[] { ZombieKind.Normal })!;
                    Assert.That(game.Adventure.Level.IsWaterRow(row), Is.False);
                }
            }

            game.Adventure.CurrentWave = 5;
            var waterSpawns = 0;
            foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.ScreenDoor, ZombieKind.Football, ZombieKind.Snorkel, ZombieKind.DolphinRider })
            {
                for (var attempt = 0; attempt < 32; attempt++)
                {
                    var row = (int)pick.Invoke(game, new object[] { kind })!;
                    if (kind == ZombieKind.Normal)
                    {
                        waterSpawns += game.Adventure.Level.IsWaterRow(row) ? 1 : 0;
                    }
                    else
                    {
                        Assert.That(game.Adventure.Level.IsWaterRow(row), Is.EqualTo(AdventureZombiePoolRules.IsPoolOnly(kind)));
                    }
                }
            }

            Assert.That(waterSpawns, Is.GreaterThan(0), "The original allows normal swimming zombies from wave six onward.");
        }

        /// <summary>Checks original kinds and helmet health survive the normal water entrance and native swimming animation.</summary>
        /// <param name="kind">The original plain-body variant.</param>
        /// <param name="armor">Its unchanged native starting helmet health.</param>
        [TestCase(ZombieKind.Normal, 0f)]
        [TestCase(ZombieKind.Conehead, 370f)]
        [TestCase(ZombieKind.Buckethead, 1100f)]
        [TestCase(ZombieKind.Flag, 0f)]
        public void PlainWaterBodyKeepsKindAndArmorAndStartsSwimming(ZombieKind kind, float armor)
        {
            var game = Fixture();
            var zombie = Spawn(game, kind, 2, 9.5f);
            Assert.That(zombie.Kind, Is.EqualTo(kind));
            Assert.That(zombie.ArmorHealth, Is.EqualTo(armor));
            Assert.That(zombie.PoolState!.HasDuckyTube, Is.True);
            Assert.That(zombie.PresentationInPool, Is.False);
            zombie.X = AdventureZombieNativeRules.GridX(679f);
            game.Update(.01f);
            Assert.That(zombie.PresentationInPool, Is.True);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(1));
            for (var tick = 0; tick < 40; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(zombie.Kind, Is.EqualTo(kind));
            Assert.That(zombie.ArmorHealth, Is.EqualTo(armor));
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(zombie.PoolState.Altitude, Is.EqualTo(-40f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
            Assert.That(zombie.PresentationBiteBodyY, Is.EqualTo(40));
            Assert.That(zombie.PresentationBiteClipHeight, Is.EqualTo(33));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 19), Has.Count.EqualTo(1));
        }

        /// <summary>Checks flying balloons can cross a legal water lane without acquiring a tube or submerged body.</summary>
        [Test]
        public void AirborneBalloonRemainsAboveWaterWithoutDuckTube()
        {
            var game = Fixture();
            var zombie = Spawn(game, ZombieKind.Balloon, 2, 6f);
            game.Update(.1f);
            Assert.That(zombie.Airborne, Is.True);
            Assert.That(zombie.PresentationInPool, Is.False);
            Assert.That(zombie.PoolState!.HasDuckyTube, Is.False);
            Assert.That(zombie.PoolState.Altitude, Is.EqualTo(25f));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(25f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_idle"));
        }

        /// <summary>Checks the original pool rise curve retains native integer rounding before applying the body scale.</summary>
        /// <param name="remaining">The remaining native rise counter.</param>
        /// <param name="scale">The native body scale.</param>
        /// <param name="expected">The original resulting altitude.</param>
        [TestCase(50, 1f, -150f)]
        [TestCase(49, 1f, -148f)]
        [TestCase(25, 1f, -95f)]
        [TestCase(1, 1f, -42f)]
        [TestCase(0, 1f, -40f)]
        [TestCase(25, .5f, -47.5f)]
        public void PoolAmbushUsesOriginalFiftyTickAltitude(int remaining, float scale, float expected)
        {
            Assert.That(AdventureZombiePoolRules.RiseAltitude(remaining, scale), Is.EqualTo(expected));
        }

        /// <summary>Runs the production final ambush and all fifty emergence updates without a falling entrance or lost armor.</summary>
        [Test]
        public void FinalAmbushEmergesFromPoolWithSeaweedAndNativeSwimBody()
        {
            var game = Fixture();
            typeof(GameSimulation).GetMethod("SpawnScheduledFinalAmbush", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, null);
            Assert.That(game.Zombies, Has.Count.EqualTo(3));
            var zombies = game.Zombies.ToArray();
            var positions = new float[zombies.Length];
            for (var index = 0; index < zombies.Length; index++)
            {
                var zombie = zombies[index];
                positions[index] = zombie.X;
                Assert.That(zombie.Kind, Is.EqualTo(ZombieKind.Normal).Or.EqualTo(ZombieKind.Conehead).Or.EqualTo(ZombieKind.Buckethead));
                Assert.That(zombie.ArmorHealth, Is.EqualTo(AdventureZombieEquipmentRules.StartingArmor(zombie.Kind)));
                Assert.That(game.Adventure.Level.IsWaterRow(zombie.Row), Is.True);
                Assert.That(zombie.PresentationInPool, Is.True);
                Assert.That(zombie.PresentationNativePhase, Is.EqualTo(14));
                Assert.That(zombie.GraveRiseTicks, Is.EqualTo(50));
                Assert.That(zombie.DropRemaining, Is.Zero);
                Assert.That(zombie.Airborne, Is.False);
                Assert.That(zombie.PoolState!.HasDuckyTube, Is.True);
                Assert.That(zombie.PoolState.Altitude, Is.EqualTo(-150f));
                Assert.That(zombie.PoolState.SeaweedAge, Is.Zero);
                Assert.That(zombie.PoolState.SplashAge, Is.Zero);
                Assert.That(zombie.PoolState.SplashNativeX,
                    Is.EqualTo((int)AdventureZombieNativeRules.NativePositionX(zombie.X) + 23f));
                Assert.That(zombie.PoolState.SplashNativeY, Is.EqualTo(128f + zombie.Row * 85f));
                Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
                Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.False);
            }

            Assert.That(game.Presentation.PlantingEffects, Is.Empty, "Zombie splash particles belong to the actor layer instead of the global player-planting layer.");
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 52), Has.Count.EqualTo(3));
            for (var tick = 0; tick < 49; tick++)
            {
                game.Update(.01f);
            }

            foreach (var zombie in zombies)
            {
                Assert.That(zombie.GraveRiseTicks, Is.EqualTo(1));
                Assert.That(zombie.PoolState!.Altitude, Is.EqualTo(-42f));
            }

            game.Update(.01f);
            for (var index = 0; index < zombies.Length; index++)
            {
                var zombie = zombies[index];
                Assert.That(zombie.GraveRiseTicks, Is.Zero);
                Assert.That(zombie.PoolState!.Altitude, Is.EqualTo(-40f));
                Assert.That(zombie.X, Is.EqualTo(positions[index]), "The original emerging body cannot walk before its fiftieth update.");
                Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.True);
            }
        }

        /// <summary>Checks a dead pool actor retains its independent original effect clocks while pause still stops simulation.</summary>
        [Test]
        public void CorpseContinuesPoolEffectClocksWithoutAdvancingWhilePaused()
        {
            var game = Fixture();
            var zombie = Spawn(game, ZombieKind.Normal, 2, 6f);
            typeof(GameSimulation).GetMethod("InitializeAdventurePoolZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie, 6 });
            game.Update(.5f);
            Assert.That(zombie.GraveRiseTicks, Is.Zero);
            var pool = zombie.PoolState!;
            var seaweed = pool.SeaweedAge;
            var splash = pool.SplashAge;
            typeof(GameSimulation).GetMethod("HitAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie, 1000f, true, 0U });
            Assert.That(game.Zombies.Contains(zombie), Is.False);
            Assert.That(game.Presentation.Corpses, Has.Count.EqualTo(1));
            game.Update(.05f);
            Assert.That(pool.SeaweedAge, Is.EqualTo(seaweed + .05f).Within(.0001f));
            Assert.That(pool.SplashAge, Is.EqualTo(splash + .05f).Within(.0001f));
            Assert.That(game.Presentation.Corpses[0].Age, Is.EqualTo(.05f).Within(.0001f));
            game.TogglePause();
            game.Update(.1f);
            Assert.That(pool.SeaweedAge, Is.EqualTo(seaweed + .05f).Within(.0001f));
            Assert.That(pool.SplashAge, Is.EqualTo(splash + .05f).Within(.0001f));
            game.TogglePause();
            game.Update(.01f);
            Assert.That(pool.SeaweedAge, Is.EqualTo(seaweed + .06f).Within(.0001f));
            Assert.That(pool.SplashAge, Is.EqualTo(splash + .06f).Within(.0001f));
        }

        /// <summary>Checks Ice-shroom's original shared counter still suppresses the pool ambush.</summary>
        [Test]
        public void FrozenPoolDoesNotCreateAmbushZombies()
        {
            var game = Fixture();
            game.NotifyAdventureIceTrap();
            typeof(GameSimulation).GetMethod("SpawnScheduledFinalAmbush", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, null);
            Assert.That(game.Zombies, Is.Empty);
        }

        /// <summary>Creates a pool level with all scheduled waves held beyond the test.</summary>
        /// <returns>The isolated playing production board.</returns>
        private static GameSimulation Fixture()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a requested original zombie through the actual initialization path.</summary>
        /// <param name="game">The isolated board receiving the actor.</param>
        /// <param name="kind">The original zombie kind.</param>
        /// <param name="row">The requested lawn row.</param>
        /// <param name="x">The requested logical horizontal position.</param>
        /// <returns>The newly initialized live actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            return (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, row, x, 0 })!;
        }
    }
}
