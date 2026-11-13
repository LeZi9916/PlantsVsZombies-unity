#nullable enable

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the complete local PC dolphin sequence through actual fixed simulation updates.</summary>
    public sealed class AdventureDolphinTests
    {
        /// <summary>Checks original spawn speed, entrance cue, the forty-five-frame dive, splash and seventy-pixel ride placement.</summary>
        [Test]
        public void EntryUsesOriginalStationaryDiveAndTimedSplashBeforeRiding()
        {
            var game = Board();
            var zombie = Spawn(game, 51, 720f);
            Assert.That(zombie.PresentationNativeVelocity, Is.InRange(.89f, .91f));
            Assert.That(CountSound(game, 51), Is.EqualTo(1));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walkdolphin"));
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(52));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_jumpinpool"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(16f));
            var entryX = zombie.X;
            Tick(game, 157);
            Assert.That(zombie.X, Is.EqualTo(entryX));
            Assert.That(CountSound(game, 53), Is.Zero);
            game.Update(.01f);
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
            var splash = game.Presentation.ZombieSplashes.Single();
            Assert.That(splash.X, Is.EqualTo(637f));
            Assert.That(splash.Y, Is.EqualTo(293f));
            Assert.That(splash.ParticleX, Is.EqualTo(674f));
            Assert.That(splash.ParticleY, Is.EqualTo(335f));
            Assert.That(splash.ScaleX, Is.EqualTo(1.2f));
            Assert.That(splash.ScaleY, Is.EqualTo(.8f));
            Tick(game, 123);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(52));
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(53));
            Assert.That(zombie.PresentationInPool, Is.True);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_ride"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(NativeX(zombie), Is.InRange(649f, 650f));
            Assert.That(zombie.PresentationNativeVelocity, Is.InRange(.89f, .91f), "The PC ride retains its original speed instead of inventing a one-pixel velocity.");
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
        }

        /// <summary>Checks the local PC bank interval rather than the mobile-derived eight-hundred-pixel threshold.</summary>
        /// <param name="nativeX">The walking actor's native position before its update.</param>
        /// <param name="phase">The expected PC phase after checking the pool bank.</param>
        [TestCase(700f, 51)]
        [TestCase(701f, 52)]
        [TestCase(720f, 52)]
        [TestCase(721f, 51)]
        public void EntryUsesOriginalIntegerBankInterval(float nativeX, int phase)
        {
            var game = Board();
            var zombie = Spawn(game, 51, nativeX);
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(phase));
        }

        /// <summary>Checks frozen script clocks stop and the original zero-ground-distance animations ignore chill.</summary>
        /// <param name="phase">The original stationary entry or moving jump phase.</param>
        /// <param name="rate">The original authored playback rate.</param>
        [TestCase(52, 16f)]
        [TestCase(54, 10f)]
        public void FrozenScriptsPauseButChillDoesNotSlowTheirOriginalClocks(int phase, float rate)
        {
            var game = Board();
            var zombie = Spawn(game, phase, 500f);
            zombie.DolphinState!.JumpTicks = 120;
            zombie.PresentationNativeVelocity = .5f;
            zombie.Speed = AdventureZombieNativeRules.GridSpeed(.5f);
            zombie.ChillRemaining = 10f;
            game.Update(.10f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(phase));
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(.10f).Within(.00001f));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(rate));
            Assert.That(NativeX(zombie), Is.EqualTo(phase == 54 ? 495f : 500f).Within(.001f));
            if (phase == 54)
            {
                Assert.That(zombie.DolphinState.JumpTicks, Is.EqualTo(110));
                Assert.That(zombie.PresentationAltitude, Is.EqualTo(10f / 12f).Within(.0001f));
            }

            var heldX = zombie.X;
            var heldAge = zombie.PresentationAnimationAge;
            var heldCounter = zombie.DolphinState.JumpTicks;
            zombie.FrozenRemaining = 1f;
            Tick(game, 20);
            Assert.That(zombie.X, Is.EqualTo(heldX));
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(heldAge));
            Assert.That(zombie.DolphinState.JumpTicks, Is.EqualTo(heldCounter));
        }

        /// <summary>Checks one original leap, its independent countdown, landing event, and persistent dismounted body.</summary>
        [Test]
        public void JumpUsesOriginalTimelineInsteadOfGenericTeleport()
        {
            var game = Board();
            var zombie = Spawn(game, 53, 400f);
            PlaceBarrier(game, PlantKind.Wallnut);
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(54));
            Assert.That(zombie.DolphinState!.JumpTicks, Is.EqualTo(120));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_dolphinjump"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(10f));
            Assert.That(CountSound(game, 50), Is.EqualTo(1));
            Assert.That(CountSound(game, 52), Is.EqualTo(1));
            Assert.That(NativeX(zombie), Is.InRange(399f, 400f));
            Tick(game, 112);
            Assert.That(CountSound(game, 53), Is.Zero);
            game.Update(.01f);
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
            Assert.That(zombie.PresentationNativeVelocity, Is.Zero);
            var landingX = zombie.X;
            Tick(game, 7);
            Assert.That(zombie.DolphinState.JumpTicks, Is.Zero, "The physical jump countdown is independent of the longer authored reanimation.");
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(10f));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(54));
            Assert.That(zombie.X, Is.EqualTo(landingX));
            Tick(game, 109);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(54));
            game.Update(.01f);
            // PC Reanimation::Update stores 0.9999974966 at tick 230; only tick 231 completes this authored clip.
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(54));
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(55),
                "Original jump phase at finish: age=" + zombie.PresentationAnimationAge + ", loops=" + zombie._presentationWalkLoops
                + ", fraction=" + zombie._presentationWalkPhase);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That((landingX - zombie.X) * 80f, Is.InRange(94f, 94.5f));
            Assert.That(Math.Abs(zombie.PresentationPreviousX - zombie.X) * 80f, Is.LessThan(1f),
                "The landing's ninety-four-pixel coordinate rebase must not become render interpolation travel.");
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
            Assert.That(AdventureZombieTargetingRules.BodyRectangle(zombie).X, Is.EqualTo((int)NativeX(zombie) + 20));
            Assert.That(AdventureZombieBiteRules.LocalAttackRect(14, 55, false).X, Is.EqualTo(30));
            Assert.That(CountSound(game, 50), Is.EqualTo(1));
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
        }

        /// <summary>Checks the coordinate rebase before ordinary movement and the independent original meal-transition blend.</summary>
        /// <param name="eating">Whether the rider starts eating immediately after landing, before its first rendered swim pose.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void LandingRebasesHistoryBeforeFirstSwimOrImmediateMeal(bool eating)
        {
            var game = Board();
            var zombie = Spawn(game, 54, 400f);
            zombie._presentationInitialized = true;
            zombie.PresentationPreviousX = zombie.X;
            zombie.PresentationAltitude = 10f;
            zombie.DolphinState!.JumpTicks = 0;
            zombie._presentationPreviousWalkCycle = .99d;
            zombie._presentationWalkPhase = 0f;
            zombie._presentationWalkLoops = 1;
            var before = NativeX(zombie);
            var previousVersion = zombie.DolphinState.WalkingPoseVersion;
            Invoke(game, "AdvanceAdventureDolphin", zombie, .01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(55));
            Assert.That(NativeX(zombie), Is.EqualTo(before - 94f).Within(.001f));
            Assert.That(zombie.PresentationPreviousX, Is.EqualTo(zombie.X),
                "New swim/eat artwork must use the landing coordinate at every render interpolation fraction.");
            Assert.That(zombie.DolphinState.WalkingPoseVersion, Is.EqualTo(previousVersion + 1),
                "Every native landing must seed a new walking pose even when the renderer never displayed its intermediate swim clip.");
            zombie.Eating = eating;
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo(eating ? "anim_eat" : "anim_swim"));
            Assert.That(zombie.DolphinState.AnimationBlendTicks, Is.EqualTo(eating ? 20 : 0),
                "Immediate eating keeps the original twenty-tick meal blend; only the landing rebase is instantaneous.");
        }

        /// <summary>Checks the original high-nut contact event replaces the leap with falling and permanently removes the dolphin.</summary>
        [Test]
        public void TallNutBlocksAtOriginalJumpEventAndUsesBonk()
        {
            var game = Board();
            var zombie = Spawn(game, 53, 400f);
            var nut = PlaceBarrier(game, PlantKind.TallNut);
            game.Update(.01f);
            Tick(game, 68);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(54));
            Assert.That(CountSound(game, 48), Is.Zero);
            for (var tick = 69; tick <= 70 && zombie.PresentationNativePhase == 54; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(55));
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(30f));
            Assert.That(NativeX(zombie), Is.InRange(40f + nut.Column * 80f + 24f, 40f + nut.Column * 80f + 25f),
                "PC UpdateWalking follows the contact action and keeps normal fractional movement in the collision frame.");
            Assert.That(CountSound(game, 48), Is.EqualTo(1));
            Assert.That(game.Presentation.PlantBiteEffects.Any(effect => effect.ParticleId == 29), Is.True);
            Tick(game, 30);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.UsedAbility, Is.True);
        }

        /// <summary>Checks a held dolphin leaves at the original left edge and keeps its walk-with-dolphin artwork.</summary>
        [Test]
        public void RiderLeavesAtNativeLeftBankWithoutInventingAnotherJump()
        {
            var game = Board();
            var zombie = Spawn(game, 53, 10f);
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(51));
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(2));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(-40f));
            Assert.That(zombie.UsedAbility, Is.False);
            Assert.That(CountSound(game, 52), Is.EqualTo(1));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walkdolphin"));
            Tick(game, 40);
            Assert.That(zombie.PresentationInPool, Is.False);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
        }

        /// <summary>Checks both native pool banks, including the mind-controlled backward-walking branch.</summary>
        /// <param name="backwards">Whether the swimmer is mind controlled.</param>
        /// <param name="nativeX">The original pool position before movement.</param>
        /// <param name="phase">The original phase expected after checking the bank.</param>
        [TestCase(false, 10f, 56)]
        [TestCase(false, 11f, 55)]
        [TestCase(true, 680f, 55)]
        [TestCase(true, 681f, 56)]
        public void DismountedSwimmerUsesBothOriginalBankLimits(bool backwards, float nativeX, int phase)
        {
            var game = Board();
            var zombie = Spawn(game, 55, nativeX);
            zombie.Hypnotized = backwards;
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(phase));
            Assert.That(zombie.UsedAbility, Is.True);
            if (phase == 56)
            {
                Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(2));
                Assert.That(zombie.PresentationAltitude, Is.EqualTo(-40f));
                Assert.That(CountSound(game, 52), Is.EqualTo(1));
                Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walk"));
                Assert.That(AdventureZombieBiteRules.LocalAttackRect(14, 56, false).X, Is.EqualTo(30));
            }
        }

        /// <summary>Checks only the original swimming phase can eat and resumes swimming when its actual meal disappears.</summary>
        [Test]
        public void SwimmingEatsWithOriginalRateAndResumesSwimming()
        {
            var game = Board();
            var zombie = Spawn(game, 55, 370f);
            var nut = PlaceBarrier(game, PlantKind.Wallnut);
            var health = nut.Health;
            Tick(game, 8);
            Assert.That(zombie.Eating, Is.True);
            Assert.That(nut.Health, Is.LessThan(health));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_eat"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(36f));
            game.Plants.Clear();
            Tick(game, 8);
            Assert.That(zombie.Eating, Is.False);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_swim"));
        }

        /// <summary>Checks the original has-head gate prevents a new dolphin leap without changing an existing riding phase.</summary>
        [Test]
        public void HeadlessRiderCannotBeginANewLeap()
        {
            var game = Board();
            var zombie = Spawn(game, 53, 400f);
            PlaceBarrier(game, PlantKind.Wallnut);
            zombie.Headless = true;
            game.Update(.01f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(53));
            Assert.That(CountSound(game, 50), Is.Zero);
            Assert.That(zombie.UsedAbility, Is.False);
        }

        /// <summary>Checks the PC kelp guard holds the original rider phase instead of beginning a new leap.</summary>
        [Test]
        public void ActualKelpGrabSuppressesRiderLeap()
        {
            var game = Board();
            var zombie = Spawn(game, 53, 400f);
            var kelp = Place(game, PlantKind.TangleKelp, 5);
            PlaceBarrier(game, PlantKind.Wallnut);
            game.Update(.01f);
            Assert.That(kelp.TangleKelpPresentation, Is.Not.Null);
            Assert.That(zombie.TangleKelpGrab, Is.SameAs(kelp.TangleKelpPresentation));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(53));
            Assert.That(CountSound(game, 50), Is.Zero);
            Assert.That(zombie.UsedAbility, Is.False);
        }

        /// <summary>Checks already flying ordinary peas, spores and descending cabbages pass through the original protected scripts.</summary>
        /// <param name="phase">The native entrance or jump phase protected by damage-range bit sixteen.</param>
        [TestCase(52)]
        [TestCase(54)]
        public void InFlightProjectilesRespectOriginalProtectedPhases(int phase)
        {
            foreach (var kind in new[] { PlantKind.Peashooter, PlantKind.PuffShroom, PlantKind.CabbagePult })
            {
                var game = Board();
                var zombie = Spawn(game, phase, 400f);
                var projectile = LandingProjectile(kind, zombie.X);
                game.Peas.Add(projectile);
                Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                Assert.That(zombie.Health, Is.EqualTo(500f), kind + " must respect PC EffectedByDamage for an already flying projectile.");
                Assert.That(game.Peas, Has.Member(projectile));
                Assert.That(projectile.HitIds, Has.No.Member(zombie.Id));
            }
        }

        /// <summary>Checks fire and winter-melon secondary damage cannot bypass the protected phase, while original ash damage still works.</summary>
        /// <param name="phase">The original protected movement phase.</param>
        [TestCase(52)]
        [TestCase(54)]
        public void SplashAndChillSkipScriptsButOriginalCherryBlastCanHit(int phase)
        {
            foreach (var winter in new[] { false, true })
            {
                var game = Board();
                var zombie = Spawn(game, phase, 400f);
                var primary = new Zombie { Id = 9001, Kind = ZombieKind.Normal, Row = 2, X = zombie.X + .1f, Health = 270f, StartingHealth = 270f };
                game.Zombies.Add(primary);
                var projectile = LandingProjectile(winter ? PlantKind.WinterMelon : PlantKind.Peashooter, primary.X);
                projectile.Fire = !winter;
                projectile.Splash = true;
                projectile.ChillSplash = winter;
                projectile.Damage = winter ? 80f : 40f;
                game.Peas.Add(projectile);
                Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                Assert.That(primary.Health, Is.LessThan(270f), "The real projectile must hit its ordinary primary victim.");
                Assert.That(zombie.Health, Is.EqualTo(500f), "Secondary splash must use the same original range mask.");
                Assert.That(zombie.ChillRemaining, Is.Zero);
                Assert.That(game.Peas, Has.No.Member(projectile));
                var cherry = Place(game, PlantKind.CherryBomb, 5);
                cherry.RequiredCherryBombPresentation.Countdown = 1;
                game.Update(.01f);
                Assert.That(game.Zombies, Has.No.Member(zombie), "The original Cherry Bomb mask 127 includes protected scripts.");
            }
        }

        /// <summary>Checks the native phase gate permits the authored death animation only after losing the dolphin in water.</summary>
        /// <param name="phase">The victim's original phase immediately before lethal damage.</param>
        /// <param name="corpse">Whether PC PlayDeathAnim retains an animated corpse.</param>
        [TestCase(51, false)]
        [TestCase(52, false)]
        [TestCase(53, false)]
        [TestCase(54, false)]
        [TestCase(55, true)]
        [TestCase(56, false)]
        public void DeathUsesOriginalSwimmingOnlyAnimationGate(int phase, bool corpse)
        {
            var game = Board();
            var zombie = Spawn(game, phase, 500f);
            Invoke(game, "HitAdventureZombie", zombie, 1000f, true, 0U);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(game.Presentation.Corpses.Count, Is.EqualTo(corpse ? 1 : 0));
            if (corpse)
            {
                Assert.That(game.Presentation.Corpses[0].Animation, Is.EqualTo("anim_death"));
                Assert.That(game.Presentation.Corpses[0].Rate, Is.InRange(24f, 30f));
            }
        }

        /// <summary>Checks a swimming death continues the original one-pixel fall after a high-nut collision.</summary>
        [Test]
        public void SwimmingDeathContinuesOriginalHighNutFall()
        {
            var game = Board();
            var zombie = Spawn(game, 55, 500f);
            zombie.PresentationNativeHeight = 7;
            zombie.PresentationAltitude = 30f;
            Invoke(game, "HitAdventureZombie", zombie, 1000f, true, 0U);
            Assert.That(game.Presentation.Corpses, Has.Count.EqualTo(1));
            game.Update(.10f);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(20f));
            game.Update(.20f);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
        }

        /// <summary>Checks PC death playback keeps the existing chill multiplier throughout the swimming death.</summary>
        [Test]
        public void ChilledSwimmingDeathUsesOriginalHalfRate()
        {
            var game = Board();
            var zombie = Spawn(game, 55, 500f);
            zombie.ChillRemaining = 10f;
            Invoke(game, "HitAdventureZombie", zombie, 1000f, true, 0U);
            Assert.That(game.Presentation.Corpses, Has.Count.EqualTo(1));
            var corpse = game.Presentation.Corpses[0];
            Assert.That(corpse.Rate, Is.InRange(12f, 15f));
            Assert.That(corpse.Duration, Is.GreaterThanOrEqualTo(2.6f));
        }

        /// <summary>Checks real damage retains detached dolphin phase and altitude without inferring ambiguous walking or eating clips.</summary>
        [Test]
        public void DetachedPartsRetainOriginalDolphinPhaseAndAltitude()
        {
            var game = Board();
            var walker = Spawn(game, 56, 5f);
            Invoke(game, "HitAdventureZombie", walker, 200f, true, 0U);
            var arm = game.Presentation.ArmDrops.Single();
            Assert.That(arm.NativePhase, Is.EqualTo(56));
            Assert.That(arm.NativeAltitude, Is.Zero);
            Assert.That(arm.Animation, Is.EqualTo("anim_walk"));
            var swimmer = Spawn(game, 55, 500f);
            swimmer.PresentationNativeHeight = 7;
            swimmer.PresentationAltitude = 30f;
            Invoke(game, "HitAdventureZombie", swimmer, 360f, true, 0U);
            var head = game.Presentation.ZombiePartDrops.Single(part => part.ParticleId == 74);
            Assert.That(head.NativePhase, Is.EqualTo(55));
            Assert.That(head.NativeAltitude, Is.EqualTo(30f));
            Assert.That(swimmer.Headless, Is.True);
            swimmer.PresentationNativePhase = 56;
            swimmer.PresentationAltitude = 0f;
            walker.PresentationNativePhase = 51;
            walker.PresentationAltitude = 10f;
            Assert.That(head.NativePhase, Is.EqualTo(55));
            Assert.That(head.NativeAltitude, Is.EqualTo(30f));
            Assert.That(arm.NativePhase, Is.EqualTo(56));
            Assert.That(arm.NativeAltitude, Is.Zero);
        }

        /// <summary>Creates a deterministic pool board with original fixed updates and no unrelated automatic waves.</summary>
        /// <returns>The playing pool simulation.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            foreach (var mower in game.Mowers)
            {
                mower.Available = false;
            }

            return game;
        }

        /// <summary>Creates a native dolphin fixture with an explicit original phase and fixed board origin.</summary>
        /// <param name="game">The production board receiving the actor.</param>
        /// <param name="phase">The original phase being exercised.</param>
        /// <param name="nativeX">The unscaled original horizontal position.</param>
        /// <returns>The live production-created rider.</returns>
        private static Zombie Spawn(GameSimulation game, int phase, float nativeX)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.DolphinRider, 2,
                AdventureZombieNativeRules.GridX(nativeX), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX);
            zombie.PresentationPreviousX = zombie.X;
            zombie.PresentationNativePhase = phase;
            zombie.PresentationNativeHeight = 0;
            zombie.PresentationInPool = phase >= 53 && phase <= 55;
            zombie.PresentationAltitude = 0f;
            zombie.UsedAbility = phase >= 55;
            zombie.DolphinState ??= new AdventureDolphinState();
            zombie.PresentationAnimation = "";
            zombie.PresentationAnimationAge = 0f;
            zombie._presentationWalkPhase = 0f;
            zombie._presentationWalkLoops = 0;
            zombie._presentationPreviousWalkCycle = 0d;
            game.AdventurePresentationZombieAnimation(zombie);
            return zombie;
        }

        /// <summary>Creates a real in-flight projectile at the target's row with lobbers near their descending ground contact.</summary>
        /// <param name="kind">The original projectile's source plant kind.</param>
        /// <param name="x">The existing board position at which the projectile is already flying.</param>
        /// <returns>The native projectile fixture consumed by the production projectile update.</returns>
        private static Pea LandingProjectile(PlantKind kind, float x)
        {
            var lobbed = kind == PlantKind.CabbagePult || kind == PlantKind.WinterMelon;
            return new Pea
            {
                SourceKind = kind, Row = 2, FlightRow = 2f, X = x, StartX = x, Range = 20f, Damage = 20f,
                VelocityX = 0f, Lobbed = lobbed, PresentationInitialized = true,
                PresentationLobX = x, PresentationNativeAge = lobbed ? 1.2f : 0f
            };
        }

        /// <summary>Places a real supported defensive target in the rider's original vault or bite interval.</summary>
        /// <param name="game">The playing pool board.</param>
        /// <param name="kind">The defensive plant kind.</param>
        /// <returns>The live target in pool row two, column four.</returns>
        private static Plant PlaceBarrier(GameSimulation game, PlantKind kind)
        {
            Place(game, PlantKind.LilyPad, 4);
            return Place(game, kind, 4);
        }

        /// <summary>Places a plant through the actual production placement initializer.</summary>
        /// <param name="game">The board receiving the plant.</param>
        /// <param name="kind">The original seed kind.</param>
        /// <param name="column">The pool column.</param>
        /// <returns>The production-created plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 2, column)!;
        }

        /// <summary>Advances an exact number of native hundred-hertz updates.</summary>
        /// <param name="game">The active board.</param>
        /// <param name="ticks">The exact update count.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var index = 0; index < ticks; index++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Counts original Foley requests without reproducing any audio implementation.</summary>
        /// <param name="game">The board retaining the requests.</param>
        /// <param name="foley">The original Foley category.</param>
        /// <returns>The number of matching requests.</returns>
        private static int CountSound(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.Count(sound => sound.Foley == foley);
        }

        /// <summary>Converts the displayed board coordinate back to the original zombie origin.</summary>
        /// <param name="zombie">The original rider.</param>
        /// <returns>The unscaled native position in board pixels.</returns>
        private static float NativeX(Zombie zombie)
        {
            return AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
        }

        /// <summary>Invokes an existing production boundary without adding a runtime test-only entry point.</summary>
        /// <param name="game">The board owning the boundary.</param>
        /// <param name="name">The original nonpublic method name.</param>
        /// <param name="arguments">The original method arguments.</param>
        /// <returns>The production method's result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
