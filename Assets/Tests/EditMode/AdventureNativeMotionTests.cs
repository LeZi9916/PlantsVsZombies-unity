#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original pole-vault, water-contact, snorkel, and Threepeater motion branches.</summary>
    public sealed class AdventureNativeMotionTests
    {
        /// <summary>Checks the two native pool probes and the explicit right-bank cutoff.</summary>
        /// <param name="nativeX">The original zombie position in pixels.</param>
        /// <param name="inPool">Whether both original pool probes should pass.</param>
        [TestCase(680f, false)]
        [TestCase(679.9f, true)]
        [TestCase(-5f, true)]
        [TestCase(-6f, false)]
        public void PoolEntryUsesNativeProbes(float nativeX, bool inPool)
        {
            Assert.That(AdventureZombieMotionRules.IsPoolPosition(nativeX), Is.EqualTo(inPool));
        }

        /// <summary>Checks entry at integer mX below 680 followed by the original forty-tick sink.</summary>
        [Test]
        public void PoolEntrySinksBeforeSwitchingToSwim()
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.Normal, 2, 6f);
            zombie.PresentationInPool = false;
            zombie.PresentationAltitude = 0f;
            zombie.X = AdventureZombieNativeRules.GridX(680f);
            Invoke(game, "AdvanceAdventureZombiePool", zombie, .01f);
            Assert.That(zombie.PresentationInPool, Is.False);
            zombie.X = AdventureZombieNativeRules.GridX(679f);
            Invoke(game, "AdvanceAdventureZombiePool", zombie, .01f);
            Assert.That(zombie.PresentationInPool, Is.True);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(1));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Does.StartWith("anim_walk"));
            Assert.That(game.Presentation.ZombieSplashes, Is.Empty, "Plain swimmers must use one actor-local splash, without a second global effect.");
            Assert.That(zombie.PoolState!.SplashAge, Is.Zero);
            Assert.That(zombie.PoolState.SplashNativeX, Is.EqualTo(702f));
            Assert.That(AdventureZombieFoleyData.Definitions[19].Samples,
                Is.EqualTo(new[] { "plant_water", "zombie_entering_water" }));
            Assert.That(game.Presentation.ZombieSounds.Last().Foley, Is.EqualTo(19));
            for (var tick = 0; tick < 40; tick++)
            {
                Invoke(game, "AdvanceAdventureZombiePool", zombie, .01f);
                Assert.That(zombie.PoolState!.Altitude, Is.EqualTo(-(tick + 1)), "Pool state must advance exactly once per native tick.");
            }

            Assert.That(AdventureZombiePoolRules.BodyAltitude(zombie), Is.EqualTo(-40f));
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
            Assert.That(game.Presentation.ZombieSplashes, Is.Empty);
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 19), Is.EqualTo(1));
        }

        /// <summary>Checks continuous vault movement, the two timed sounds, and the original landing displacement.</summary>
        [Test]
        public void PoleVaultUsesAnimationDurationAndTimedFoley()
        {
            var game = Board(1);
            var zombie = Spawn(game, ZombieKind.PoleVaulter, 2, 6.5f);
            var target = new Plant { Kind = PlantKind.Wallnut, Row = 2, Column = 5, Health = 4000f };
            game.Plants.Add(target);
            var startX = zombie.X;
            Invoke(game, "BeginAdventurePoleVault", zombie, target);
            Assert.That(zombie.X, Is.EqualTo(startX), "Starting the jump must not teleport across the plant.");
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_jump"));
            for (var tick = 0; tick < 179; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventurePoleVault", zombie, .01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(12));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 74), Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 78), Is.EqualTo(1));
            var beforeLanding = zombie.X;
            Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
            Invoke(game, "AdvanceAdventurePoleVault", zombie, .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(13));
            Assert.That(beforeLanding - zombie.X, Is.EqualTo(150f / 80f).Within(.0001f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walk"));
            Assert.That(AdventureZombieFoleyData.Definitions[78].Samples, Is.EqualTo(new[] { "polevault" }));
        }

        /// <summary>Checks the original virtual foot position on and across the roof's slope boundary.</summary>
        /// <param name="nativeX">The unchanged body position before authored jump displacement.</param>
        /// <param name="offset">The original extra floor height halfway through the jump.</param>
        [TestCase(300f, 18.75f)]
        [TestCase(450f, 6.25f)]
        [TestCase(500f, 0f)]
        public void PoleVaultRoofFloorUsesAuthoredFootPosition(float nativeX, float offset)
        {
            Assert.That(AdventureZombieMotionRules.PoleVaultRoofOffset(nativeX, .5f), Is.EqualTo(offset));
        }

        /// <summary>Checks real roof updates retain the original floor correction until the authored landing.</summary>
        [Test]
        public void RoofPoleVaultUpdatesBodyAndContactHeightThenClearsOffsetAtLanding()
        {
            var game = Board(41);
            var target = new Plant { Kind = PlantKind.Wallnut, Row = 2, Column = 3, Health = 4000f };
            game.Plants.Add(target);
            var zombie = Spawn(game, ZombieKind.PoleVaulter, 2, AdventureZombieNativeRules.GridX(420f));
            Invoke(game, "BeginAdventurePoleVault", zombie, target);
            game.Update(.9f);

            // PC 541850 samples mPosY at mPosX - 150*animTime; 54D620 adds the 40-pixel foot probe.
            var progress = .9f * 24f / 43f;
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(12));
            Assert.That(zombie.PresentationVaultPositionY, Is.EqualTo(progress * 37.5f).Within(.001f));
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X);
            var nativeY = 80f + 2f * 85f + Math.Max(0f, 400f - (nativeX - 150f * progress)) * .25f - 40f;
            Assert.That(game.AdventureZombieAttackRectangle(zombie).Y, Is.EqualTo((int)nativeY));

            game.Update(.9f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(13));
            Assert.That(zombie.PresentationVaultPositionY, Is.Zero);
        }

        /// <summary>Checks that Tall-nut blocks during the vault's authored contact window.</summary>
        [Test]
        public void PoleVaultHitsTallNutDuringJump()
        {
            var game = Board(1);
            var target = new Plant { Kind = PlantKind.TallNut, Row = 2, Column = 5, Health = 8000f };
            game.Plants.Add(target);
            var zombie = Spawn(game, ZombieKind.PoleVaulter, 2, AdventureZombieNativeRules.GridX(520f));
            Invoke(game, "BeginAdventurePoleVault", zombie, target);
            for (var tick = 0; tick < 130 && zombie.PresentationNativePhase == 12; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventurePoleVault", zombie, .01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(13));
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationVaultPositionY, Is.EqualTo(-30f));
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Invoke(game, "AdvanceAdventureZombiePool", zombie, .01f);
            Assert.That(zombie.PresentationVaultPositionY, Is.Zero);
            Assert.That(game.Presentation.ZombieSounds.Last().Foley, Is.EqualTo(48));
        }

        /// <summary>Checks the jump-in clip, its splash, surfacing, and reverse submergence.</summary>
        [Test]
        public void SnorkelPlaysJumpRiseAndReverseDive()
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.Snorkel, 2, 6f);
            zombie.X = AdventureZombieNativeRules.GridX(710f);
            zombie.PresentationInPool = false;
            zombie.PresentationAltitude = 0f;
            zombie.PresentationNativePhase = 57;
            zombie.Submerged = false;
            Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_jumpinpool"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(16f));
            for (var tick = 0; tick < 119; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(59));
            Assert.That(zombie.Submerged, Is.True);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(10f).Within(.001f));
            Assert.That(game.Presentation.ZombieSplashes.Count, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 53), Is.EqualTo(1));
            zombie.Eating = true;
            Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_uptoeat"));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(zombie), Is.True);
            for (var tick = 0; tick < 51; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(61));
            zombie.Eating = false;
            Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(62));
            Assert.That(AdventureZombieMotionRules.PlaybackSeconds(zombie, 0f), Is.EqualTo(.5f));
            Assert.That(AdventureZombieMotionRules.PlaybackSeconds(zombie, .25f), Is.EqualTo(.25f));
            Assert.That(AdventureZombieMotionRules.PlaybackSeconds(zombie, .5f), Is.Zero);
            for (var tick = 0; tick < 51; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(59));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
        }

        /// <summary>Checks the first moving tick after a meal already samples reverse submergence.</summary>
        /// <param name="phase">The surfacing or eating phase that loses its target.</param>
        [TestCase(60)]
        [TestCase(61)]
        public void SnorkelStopsEatingIntoReverseDiveBeforeMoving(int phase)
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.Snorkel, 2, AdventureZombieNativeRules.GridX(460f));
            zombie.PresentationNativePhase = phase;
            zombie.Eating = true;
            zombie.Submerged = false;
            zombie.Age = .03f;
            game.AdventurePresentationZombieAnimation(zombie);
            zombie.PresentationAnimationAge = .2f;
            var startX = zombie.X;

            // PC StopEating clears the meal; UpdateZombieSnorkel then reverses uptoeat before movement.
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.Eating, Is.False);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(62));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_uptoeat"));
            Assert.That(zombie.PresentationAnimationAge, Is.Zero, "Reverse playback must begin at the authored final rise frame.");
            Assert.That(AdventureZombieMotionRules.PlaybackSeconds(zombie, 0f), Is.EqualTo(.5f));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(24f));
            Assert.That(zombie.X, Is.LessThan(startX), "PC permits movement while the original dive animation plays.");

            Invoke(game, "AdvanceAdventureZombieClock", zombie, .25f);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(62));
            Assert.That(AdventureZombieMotionRules.PlaybackSeconds(zombie, zombie.PresentationAnimationAge),
                Is.EqualTo(.25f).Within(.00001f));
            Invoke(game, "AdvanceAdventureZombieClock", zombie, .26f);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(59));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
        }

        /// <summary>Checks water death retains the native snorkel pose and the death-only waterline clip.</summary>
        [Test]
        public void SnorkelDeathReplacesSwimmingPhaseAndRetainsAltitude()
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.Snorkel, 2, 6f);
            zombie.PresentationNativePhase = 61;
            zombie.Eating = true;
            Invoke(game, "AdventurePresentationZombieDeath", zombie, 0U);
            var corpse = game.Presentation.Corpses.Single();
            Assert.That(corpse.Animation, Is.EqualTo("anim_death"));
            Assert.That(corpse.Rate, Is.EqualTo(14f));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(1));
            Assert.That(zombie.Eating, Is.False);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(10f));
            Assert.That(AdventureZombieMotionRules.ClipBottom(zombie, 220f, 1f), Is.EqualTo(338f));
        }

        /// <summary>Checks common launch position, fixed target rows, and native geometric velocity decay.</summary>
        [Test]
        public void ThreepeaterSidePeasCurveIntoFixedTargetLanes()
        {
            var game = Board(21);
            var plant = new Plant { Kind = PlantKind.Threepeater, Row = 2, Column = 2 };
            var upper = (Pea)Invoke(game, "FireAdventurePea", plant, 1, false, 20f)!;
            var middle = (Pea)Invoke(game, "FireAdventurePea", plant, 2, false, 20f)!;
            var lower = (Pea)Invoke(game, "FireAdventurePea", plant, 3, false, 20f)!;
            Assert.That(upper.X, Is.EqualTo(middle.X));
            Assert.That(lower.PresentationLaunchY, Is.EqualTo(upper.PresentationLaunchY));
            Assert.That(upper.PresentationLaunchY, Is.EqualTo(274f));
            for (var tick = 0; tick < 20; tick++)
            {
                Invoke(game, "AdvanceAdventureProjectiles", .01f, false);
            }

            var expectedOffset = 3f * (1f - (float)Math.Pow(.97f, 20)) / .03f;
            Assert.That(upper.PresentationLaneOffset, Is.EqualTo(-expectedOffset).Within(.001f));
            Assert.That(lower.PresentationLaneOffset, Is.EqualTo(expectedOffset).Within(.001f));
            Assert.That(middle.PresentationLaneOffset, Is.Zero);
            Assert.That(upper.PresentationLaneVelocity, Is.EqualTo(-3f * (float)Math.Pow(.97f, 20)).Within(.0001f));
            Assert.That(upper.Row, Is.EqualTo(1));
            Assert.That(lower.Row, Is.EqualTo(3));
            Assert.That(upper.X, Is.EqualTo(2f + 59f / 80f + 20f * 3.33f / 80f).Within(.0001f));
        }

        /// <summary>Checks the PC snorkel jump trigger's inclusive and exclusive bank boundaries.</summary>
        /// <param name="nativeX">The original integer mX value.</param>
        /// <param name="jumps">Whether the PC phase should change to jump-in-pool.</param>
        [TestCase(700f, false)]
        [TestCase(701f, true)]
        [TestCase(720f, true)]
        [TestCase(721f, false)]
        public void SnorkelJumpUsesPcBankBoundaries(float nativeX, bool jumps)
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.Snorkel, 2, 6f);
            zombie.X = AdventureZombieNativeRules.GridX(nativeX);
            zombie.PresentationNativePhase = 57;
            zombie.PresentationInPool = false;
            Invoke(game, "AdvanceAdventureSnorkel", zombie, .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(jumps ? 58 : 57));
        }

        /// <summary>Checks the actual zombie update begins a vault immediately and keeps original snorkel bite cadence.</summary>
        [Test]
        public void ZombieUpdateDispatchesMotionPhasesAtPlantContact()
        {
            var lawn = Board(1);
            var barrier = new Plant { Kind = PlantKind.TallNut, Row = 2, Column = 5, Health = 8000f };
            lawn.Plants.Add(barrier);
            var vaulter = Spawn(lawn, ZombieKind.PoleVaulter, 2, AdventureZombieNativeRules.GridX(520f));
            Invoke(lawn, "AdvanceAdventureZombies", .01f);
            Assert.That(vaulter.PresentationNativePhase, Is.EqualTo(12), "The old generic ability countdown must not delay a native vault.");
            Assert.That(barrier.Health, Is.EqualTo(8000f));

            var pool = Board(21);
            var target = new Plant { Kind = PlantKind.Wallnut, Row = 2, Column = 5, Health = 4000f };
            pool.Plants.Add(target);
            var snorkel = Spawn(pool, ZombieKind.Snorkel, 2, AdventureZombieNativeRules.GridX(460f));
            snorkel.Age = .03f;
            Invoke(pool, "AdvanceAdventureZombies", .01f);
            Assert.That(snorkel.Eating, Is.True);
            Assert.That(snorkel.Submerged, Is.False);
            Assert.That(target.Health, Is.EqualTo(3996f), "PC EatPlant starts its four-tick damage cadence before the rise animation finishes.");
            Invoke(pool, "AdvanceAdventureZombies", .01f);
            Assert.That(snorkel.PresentationNativePhase, Is.EqualTo(60));
            Assert.That(snorkel.Submerged, Is.False);
        }

        /// <summary>Creates a quiet adventure fixture without waiting for automatic wave spawning.</summary>
        /// <param name="level">The original adventure level whose terrain is required.</param>
        /// <returns>The isolated simulation fixture.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a zombie through the production native initializer at an explicit in-lawn position.</summary>
        /// <param name="game">The fixture that owns the zombie.</param>
        /// <param name="kind">The native zombie kind.</param>
        /// <param name="row">The lawn row.</param>
        /// <param name="x">The explicit logical board position.</param>
        /// <returns>The initialized production zombie.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, row, x, 0)!;
        }

        /// <summary>Invokes a private production hook without expanding runtime visibility.</summary>
        /// <param name="game">The fixture whose hook is invoked.</param>
        /// <param name="name">The private hook name.</param>
        /// <param name="arguments">The hook's original arguments.</param>
        /// <returns>The hook result, or null for a void method.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
