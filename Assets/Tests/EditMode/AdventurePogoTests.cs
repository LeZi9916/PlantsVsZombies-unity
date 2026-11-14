#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses PC 540F10/540DF0 pogo animation, bounce movement, blocking, and stick loss.</summary>
    public sealed class AdventurePogoTests
    {
        /// <summary>Checks original initialization, held body animation, and native movement independent of the reanimation ground track.</summary>
        [Test]
        public void SpawnUsesOriginalHeldFortyFpsClipAndNativeBounceVelocity()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(20));
            Assert.That(zombie.PogoState!.PhaseTicks, Is.InRange(1, 80));
            Assert.That(zombie.Health, Is.EqualTo(500f));
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(.45f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_pogo"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(40f));
            Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(11f / 40f));
            zombie.PogoState.PhaseTicks = 41;
            var before = zombie.X;
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That((before - zombie.X) * 80f, Is.EqualTo(.45f).Within(.0001f));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(49f));
            Assert.That(zombie.PogoState.PhaseTicks, Is.EqualTo(40));
        }

        /// <summary>Checks independently calculated native heights for every authored pogo bounce phase.</summary>
        [Test]
        public void BounceSlowMiddleRetainsOriginalPhaseHeightsAndNinePixelDeflection()
        {
            Assert.That(AdventurePogoRules.Altitude(20, 80), Is.EqualTo(9f));
            Assert.That(AdventurePogoRules.Altitude(20, 40), Is.EqualTo(49f));
            Assert.That(AdventurePogoRules.Altitude(20, 5), Is.EqualTo(18.375f));
            Assert.That(AdventurePogoRules.Altitude(20, 0), Is.EqualTo(9f));
            for (var phase = 21; phase <= 26; phase++)
            {
                Assert.That(AdventurePogoRules.Altitude(phase, 40), Is.EqualTo(59f + 20f * (phase - 21)));
            }

            Assert.That(AdventurePogoRules.Altitude(27, 40), Is.EqualTo(99f));
            Assert.That(AdventurePogoRules.Altitude(28, 40), Is.EqualTo(179f));
        }

        /// <summary>Checks the seven-update body restart and five-update original spring sample at their precise PC boundaries.</summary>
        [Test]
        public void BodyRestartsAtSevenAndLandingFoleyPlaysAtFive()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            zombie.PogoState!.PhaseTicks = 8;
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Assert.That(zombie.PogoState.PhaseTicks, Is.EqualTo(7));
            Assert.That(zombie.PogoState.AnimationSeconds, Is.Zero);
            Assert.That(zombie.PresentationAnimationAge, Is.Zero);
            Assert.That(CountSound(game, 26), Is.Zero);
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Assert.That(zombie.PogoState.PhaseTicks, Is.EqualTo(5));
            Assert.That(CountSound(game, 26), Is.EqualTo(1));
            Assert.That(AdventureZombieFoleyData.Definitions[26].PitchRange, Is.EqualTo(4f));
            Assert.That(AdventureZombieFoleyData.Definitions[26].Samples, Is.EqualTo(new[] { "pogo_zombie" }));
        }

        /// <summary>Checks the native stationary preparation and subsequent eighty-update forward vault without damaging the plant.</summary>
        /// <param name="ladder">Whether the original target carries an existing ladder.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void PlantTriggersPreparationThenAForwardVaultIncludingLadders(bool ladder)
        {
            var game = Board();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 1, 3)!;
            plant.HasLadder = ladder;
            var zombie = Spawn(game, 280f);
            zombie.PogoState!.PhaseTicks = 1;
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(21));
            Assert.That(zombie.PresentationNativeVelocity, Is.Zero);
            Assert.That(zombie.UsedAbility, Is.False);
            var before = zombie.X;
            for (var index = 0; index < 80; index++)
            {
                Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            }

            Assert.That(zombie.X, Is.EqualTo(before));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(27));
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(.75f));
            var health = plant.Health;
            for (var index = 0; index < 80; index++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That((before - zombie.X) * 80f, Is.EqualTo(60f).Within(.01f));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(20));
            Assert.That(zombie.UsedAbility, Is.False);
            Assert.That(plant.Health, Is.EqualTo(health));
        }

        /// <summary>Checks Tall-nut blocks only the native seventieth-update forward-vault probe and produces its original feedback.</summary>
        [Test]
        public void TallNutBreaksTheStickAtForwardBounceSeventy()
        {
            var game = Board();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.TallNut, 1, 3)!;
            plant.HasLadder = true;
            var zombie = Spawn(game, 280f);
            zombie.PresentationNativePhase = 27;
            zombie.PogoState!.PhaseTicks = 72;
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Assert.That(zombie.UsedAbility, Is.False, "The original PC probe is seventy, not the mobile port's seventy-one.");
            Invoke(game, "AdvanceAdventurePogo", zombie, zombie.X);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(48.375f));
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(.45f));
            Assert.That(CountSound(game, 48), Is.EqualTo(1));
            Assert.That(game.Presentation.PlantBiteEffects.Exists(effect => effect.ParticleId == 29), Is.True);
            Assert.That(game.Presentation.ZombiePartDrops.Exists(part => part.ParticleId == 16), Is.True);
        }

        /// <summary>Checks actual magnetic stick removal suppresses duplicate debris, falls one pixel per update, then chews on foot.</summary>
        [Test]
        public void MagnetRemovesTheStickThenTheOriginalFallingBodyCanEat()
        {
            var game = Board();
            var magnet = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.MagnetShroom, 1, 4)!;
            magnet.Asleep = false;
            magnet.ActionTimer = .01f;
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 1, 3)!;
            var zombie = Spawn(game, 280f);
            zombie.PresentationAltitude = 12f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(12f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walk"));
            Assert.That(game.Presentation.ZombiePartDrops.Exists(part => part.ParticleId == 16), Is.False);
            Invoke(game, "AdvanceAdventurePogoFalling", zombie);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(11f));
            var health = plant.Health;
            for (var index = 0; index < 80; index++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.Eating, Is.True);
            Assert.That(plant.Health, Is.LessThan(health));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_eat"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(36f));
        }

        /// <summary>Checks the original pogo magnet's head, height, screen edge, and inclusive circular reach gates.</summary>
        [Test]
        public void MagnetAdmissionUsesNativePogoGatesAndCircleBoundary()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.True);
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, 100);
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - 270, 80, 100), Is.True);
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - 271, 80, 100), Is.False);
            zombie.Eating = true;
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - 320, 80, 100), Is.True);
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - 321, 80, 100), Is.False);
            zombie.Eating = false;
            zombie.Headless = true;
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.False);
            zombie.Headless = false;
            zombie.PresentationNativeHeight = 7;
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.False);
            zombie.PresentationNativeHeight = 0;
            zombie.X = AdventureZombieNativeRules.GridX(765f);
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.False, "The PC right-edge gate tests body.X, not the actor origin.");
            zombie.X = AdventureZombieNativeRules.GridX(764f);
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.True);
            zombie.UsedAbility = true;
            Assert.That(AdventurePogoRules.CanLoseStick(zombie), Is.False);
        }

        /// <summary>Checks the actual production update descends exactly one pixel per tick without a second pool-falling update.</summary>
        [Test]
        public void ProductionUpdateFallsExactlyTenPixelsInTenTicks()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            zombie.PresentationAltitude = 20f;
            Invoke(game, "BreakAdventurePogo", zombie, 16U);
            for (var index = 0; index < 10; index++)
            {
                game.Update(.01f);
            }

            Assert.That(zombie.PresentationAltitude, Is.EqualTo(10f));
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            for (var index = 0; index < 10; index++)
            {
                game.Update(.01f);
            }

            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
        }

        /// <summary>Checks chill slows horizontal motion and the body, while the jump counter keeps its original eighty updates.</summary>
        [Test]
        public void ChillSlowsMovementAndBodyButNotTheBounceCounter()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            zombie.PogoState!.PhaseTicks = 41;
            zombie.PogoState.AnimationSeconds = 0f;
            zombie.ChillRemaining = 10f;
            Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(.005f).Within(.000001f));
            var before = zombie.X;
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That((before - zombie.X) * 80f, Is.EqualTo(.225f).Within(.0001f));
            Assert.That(zombie.PogoState.PhaseTicks, Is.EqualTo(40));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(49f));
            Assert.That(game.ApplyAdventureIceTrap(zombie), Is.False);
            Assert.That(zombie.FrozenRemaining, Is.Zero);
        }

        /// <summary>Checks butter cannot immobilize a bouncing pogo and head loss breaks the stick before an ordinary grounded death.</summary>
        [Test]
        public void BouncingPogoRejectsButterAndHeadLossBreaksItsStick()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            var shot = new Pea { Row = 1, FlightRow = 1, X = zombie.X, Butter = true, VelocityX = 0f, SourceKind = PlantKind.KernelPult };
            game.Peas.Add(shot);
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(zombie.PresentationButter, Is.Zero);
            Assert.That(zombie.FrozenRemaining, Is.Zero);
            zombie.PresentationAltitude = 40f;
            Invoke(game, "LoseAdventureZombieHead", zombie, (int)ZombieKind.Pogo, false, false, 0U);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(40f));
            Assert.That(game.Presentation.ZombiePartDrops.Exists(part => part.ParticleId == 16), Is.True);
            zombie.Health = 0f;
            Invoke(game, "KillAdventureZombie", zombie, 0U);
            Assert.That(zombie.PresentationAltitude, Is.Zero, "PC PlayDeathAnim cancels the retained pogo altitude before death.");
        }

        /// <summary>Checks UI Pogo initialization retains the original randomized counter and held final pose.</summary>
        /// <param name="ticket">The original zero-based random ticket within eighty updates.</param>
        [TestCase(0)]
        [TestCase(79)]
        public void StreetPreviewInitializesOriginalBounceCounter(int ticket)
        {
            var preview = AdventureIntroPresentation.PlaceStreetZombie(new AdventureIntroContext(), (int)ZombieKind.Pogo,
                0, 1, bound => bound == 80 ? ticket : 0, _ => 0f, (kind, clip) => true);
            Assert.That(preview.PogoState, Is.Not.Null);
            Assert.That(preview.PogoState!.PhaseTicks, Is.EqualTo(ticket + 1));
            Assert.That(preview.PogoState.AnimationSeconds, Is.EqualTo(11f / 40f));
            Assert.That(preview.Animation, Is.EqualTo("anim_pogo"));
            Assert.That(preview.AnimationRate, Is.EqualTo(40f));
            Assert.That(preview.InitialPhase, Is.EqualTo(1f));
            Assert.That(preview.Loop, Is.False);
        }

        /// <summary>Checks UI bounce height, the seven-update restart, and repeated held animation without walking.</summary>
        [Test]
        public void StreetPreviewAdvancesOriginalBounceAndHeldAnimationWithoutWalking()
        {
            var preview = AdventureIntroPresentation.PlaceStreetZombie(new AdventureIntroContext(), (int)ZombieKind.Pogo,
                0, 1, bound => bound == 80 ? 79 : 0, _ => 0f, (kind, clip) => true);
            var x = preview.WorldX;
            var y = preview.WorldY;
            for (var tick = 0; tick < 40; tick++)
            {
                AdventurePogoRules.AdvanceStreetPreview(preview);
            }

            Assert.That(preview.PogoState!.PhaseTicks, Is.EqualTo(40));
            Assert.That(preview.Altitude, Is.EqualTo(49f));
            Assert.That(preview.PogoState.AnimationSeconds, Is.EqualTo(11f / 40f));
            for (var tick = 0; tick < 33; tick++)
            {
                AdventurePogoRules.AdvanceStreetPreview(preview);
            }

            Assert.That(preview.PogoState.PhaseTicks, Is.EqualTo(7));
            Assert.That(preview.PogoState.AnimationSeconds, Is.Zero);
            AdventurePogoRules.AdvanceStreetPreview(preview);
            AdventurePogoRules.AdvanceStreetPreview(preview);
            Assert.That(preview.Altitude, Is.EqualTo(18.375f));
            Assert.That(preview.PogoState.AnimationSeconds, Is.EqualTo(.02f));
            for (var tick = 0; tick < 80; tick++)
            {
                AdventurePogoRules.AdvanceStreetPreview(preview);
            }

            Assert.That(preview.PogoState.PhaseTicks, Is.EqualTo(5));
            Assert.That(preview.Altitude, Is.EqualTo(18.375f));
            Assert.That(preview.PogoState.AnimationSeconds, Is.EqualTo(.02f));
            Assert.That(preview.WorldX, Is.EqualTo(x));
            Assert.That(preview.WorldY, Is.EqualTo(y));
        }

        /// <summary>Creates an active deterministic dry-row production board.</summary>
        /// <returns>The production board with automatic waves postponed.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Spawns the real pogo through production initialization at an exact native horizontal origin.</summary>
        /// <param name="game">The board receiving the pogo.</param>
        /// <param name="nativeX">The original horizontal body origin in pixels.</param>
        /// <returns>The initialized pogo in dry row one.</returns>
        private static Zombie Spawn(GameSimulation game, float nativeX)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Pogo, 1, AdventureZombieNativeRules.GridX(nativeX), 0)!;
        }

        /// <summary>Counts retained native Foley requests without running an audio device.</summary>
        /// <param name="game">The board retaining its original requests.</param>
        /// <param name="id">The original Foley identifier.</param>
        /// <returns>The number of matching requests.</returns>
        private static int CountSound(GameSimulation game, int id)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == id).Count;
        }

        /// <summary>Invokes production operations without adding test-only runtime APIs.</summary>
        /// <param name="game">The production board owning the operation.</param>
        /// <param name="name">The existing private operation name.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The original operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
