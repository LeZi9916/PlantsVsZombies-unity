#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses PC 543CC0/543D90 digger phases, magnetic axe loss and actual plant eating.</summary>
    public sealed class AdventureDiggerTests
    {
        /// <summary>Checks production initialization retains the original tunnel phase, drill velocity and hardhat.</summary>
        [Test]
        public void SpawnStartsTunnelingWithOriginalEquipmentAndSpeed()
        {
            var game = Board();
            var zombie = Spawn(game, 600f);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(32));
            Assert.That(zombie.DiggerState, Is.Not.Null);
            Assert.That(zombie.Submerged, Is.True);
            Assert.That(zombie.ArmorHealth, Is.EqualTo(100f));
            Assert.That(zombie.Health, Is.EqualTo(270f));
            Assert.That(zombie.PresentationNativeVelocity, Is.InRange(.66f, .68f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_dig"));
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "Zombie_digger_pickaxe"), Is.True);
        }

        /// <summary>Checks the strict ten-pixel source-coordinate edge before this update's unchilled tunnel movement.</summary>
        [Test]
        public void TunnelChecksOriginalStrictNativeEdgeBeforeMoving()
        {
            var game = Board();
            var zombie = Spawn(game, 10f);
            zombie.ChillRemaining = 10f;
            var before = zombie.X;
            var velocity = zombie.PresentationNativeVelocity;
            DiggerTicks(game, zombie, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(32), "Native x exactly ten is still underground for this update.");
            Assert.That((before - zombie.X) * 80f, Is.EqualTo(velocity).Within(.0001f), "Tunneling uses mVelX without the chill multiplier.");
            DiggerTicks(game, zombie, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(33));
            Assert.That(zombie.DiggerState!.PhaseTicks, Is.EqualTo(130));
        }

        /// <summary>Checks the normal ascent, thirty-tick landing and two complete original dizzy loops before retreating.</summary>
        [Test]
        public void NormalEmergenceUsesOriginalClipsRatesSoundsAndDizzyDuration()
        {
            var game = Board();
            var zombie = Spawn(game, 9f);
            DiggerTicks(game, zombie, 1);
            var state = zombie.DiggerState!;
            var x = zombie.X;
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(-120f));
            Assert.That(state.PhaseTicks, Is.EqualTo(130));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_drill"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(20f));
            Assert.That(AdventureDiggerState.WalksBackwards(zombie), Is.True);
            Assert.That(CountSound(game, 80), Is.EqualTo(1));
            Assert.That(CountSound(game, 83), Is.EqualTo(1));
            DiggerTicks(game, zombie, 99);
            Assert.That(state.PhaseTicks, Is.EqualTo(31));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_drill"));
            DiggerTicks(game, zombie, 1);
            Assert.That(state.PhaseTicks, Is.EqualTo(30));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(20f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_landing"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(zombie), Is.True);
            DiggerTicks(game, zombie, 30);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(36));
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_dizzy"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(state.AnimationBlendTicks, Is.EqualTo(10));
            Tick(game, 349);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(36), "Twenty-one source frames at twelve FPS require two whole loops.");
            Assert.That(zombie.X, Is.EqualTo(x));
            Tick(game, 3);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(37));
            Assert.That(zombie.AbilityPhase, Is.EqualTo(2));
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(.12f));
            Assert.That(zombie.X, Is.GreaterThan(x));
        }

        /// <summary>Checks the original two-hundred-tick axe-loss pause, surprise attachment and unturned emergence.</summary>
        [Test]
        public void UndergroundAxeLossPausesThenEmergesWithoutDizzyOrTurning()
        {
            var game = Board();
            var zombie = Spawn(game, 400f);
            var x = zombie.X;
            Invoke(game, "LoseAdventureDiggerAxe", zombie);
            var state = zombie.DiggerState!;
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(34));
            Assert.That(state.PhaseTicks, Is.EqualTo(200));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.Zero);
            DiggerTicks(game, zombie, 49);
            Assert.That(state.SurpriseAge, Is.EqualTo(-1f));
            DiggerTicks(game, zombie, 1);
            Assert.That(state.PhaseTicks, Is.EqualTo(150));
            Assert.That(state.SurpriseAge, Is.Zero);
            DiggerTicks(game, zombie, 150);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(35));
            Assert.That(state.PhaseTicks, Is.EqualTo(130));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(-120f));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_landing"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.Zero);
            Assert.That(AdventureDiggerState.WalksBackwards(zombie), Is.False);
            Assert.That(CountSound(game, 80), Is.EqualTo(1));
            Assert.That(CountSound(game, 83), Is.Zero, "The original no-axe emergence has no Wakeup Foley.");
            DiggerTicks(game, zombie, 99);
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.Zero);
            zombie.PresentationAnimationAge = .5f;
            DiggerTicks(game, zombie, 1);
            Assert.That(state.PhaseTicks, Is.EqualTo(30));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(zombie.PresentationAnimationAge, Is.Zero, "The held landing clip restarts at the thirty-tick milestone.");
            Assert.That(state.AnimationBlendTicks, Is.EqualTo(20));
            DiggerTicks(game, zombie, 30);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(38));
            Assert.That(zombie.AbilityPhase, Is.EqualTo(3));
            Assert.That(zombie.PresentationNativeVelocity, Is.InRange(.23f, .37f));
            Assert.That(zombie.X, Is.EqualTo(x));
            Tick(game, 10);
            Assert.That(zombie.X, Is.LessThan(x), "The original disarmed underground digger continues toward the house.");
        }

        /// <summary>Checks freeze suspends the body countdown while the original attached question mark keeps advancing.</summary>
        [Test]
        public void FrozenAxeLossRetainsBodyPhaseButAdvancesSurpriseAttachment()
        {
            var game = Board();
            var zombie = Spawn(game, 400f);
            Invoke(game, "LoseAdventureDiggerAxe", zombie);
            DiggerTicks(game, zombie, 50);
            var state = zombie.DiggerState!;
            zombie.FrozenRemaining = 2f;
            var x = zombie.X;
            var bodyAge = zombie.PresentationAnimationAge;
            Tick(game, 10);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(34));
            Assert.That(state.PhaseTicks, Is.EqualTo(150));
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(bodyAge));
            Assert.That(zombie.X, Is.EqualTo(x));
            Assert.That(state.SurpriseAge, Is.EqualTo(.1f).Within(.0001f));
        }

        /// <summary>Checks removing a retained axe after emergence never changes the original retreat phase or direction.</summary>
        /// <param name="phase">The original dizzy or retreating phase when the axe is removed.</param>
        [TestCase(36)]
        [TestCase(37)]
        public void AboveGroundAxeLossRetainsRetreatPhaseAndHardhat(int phase)
        {
            var game = Board();
            var zombie = Spawn(game, 400f);
            zombie.PresentationNativePhase = phase;
            zombie.AbilityPhase = phase == 36 ? 1 : 2;
            zombie.Submerged = false;
            Assert.That(AdventureDiggerState.CanLoseAxe(zombie), Is.True);
            Invoke(game, "LoseAdventureDiggerAxe", zombie);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(phase));
            Assert.That(AdventureDiggerState.WalksBackwards(zombie), Is.True);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.ArmorHealth, Is.EqualTo(100f));
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "Zombie_digger_hardhat"), Is.True);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "Zombie_digger_pickaxe"), Is.False);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "Zombie_digger_dirt"), Is.False);
            Assert.That(AdventureDiggerState.CanLoseAxe(zombie), Is.False);
        }

        /// <summary>Checks real magnet processing rejects a rising digger and later removes only the original retained pickaxe.</summary>
        [Test]
        public void MagnetSkipsRisingDiggerThenRemovesOnlyThePickaxe()
        {
            var game = Board();
            var magnet = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.MagnetShroom, 1, 4)!;
            magnet.Asleep = false;
            magnet.ActionTimer = .01f;
            var zombie = Spawn(game, 400f);
            zombie.PresentationNativePhase = 33;
            zombie.AbilityPhase = 1;
            zombie.DiggerState!.PhaseTicks = 100;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(zombie.UsedAbility, Is.False);
            Assert.That(zombie.ArmorHealth, Is.EqualTo(100f));
            Assert.That(CountSound(game, 42), Is.Zero);
            zombie.PresentationNativePhase = 36;
            magnet.ActionTimer = .01f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(zombie.UsedAbility, Is.True);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(36));
            Assert.That(zombie.ArmorHealth, Is.EqualTo(100f));
            Assert.That(CountSound(game, 42), Is.EqualTo(1));
            Assert.That(magnet.ActionTimer, Is.GreaterThan(1f));
        }

        /// <summary>Checks both original walking directions actually chew ordinary and ladder-bearing plants and resume their direction.</summary>
        /// <param name="phase">The original retreating or disarmed walking phase.</param>
        /// <param name="ladder">Whether an existing ladder is attached to the plant.</param>
        [TestCase(37, false)]
        [TestCase(37, true)]
        [TestCase(38, false)]
        [TestCase(38, true)]
        public void SurfaceDiggerEatsPlantsAndResumesOriginalDirection(int phase, bool ladder)
        {
            var game = Board();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 1, 4)!;
            plant.HasLadder = ladder;
            var zombie = Spawn(game, 330f);
            zombie.PresentationNativePhase = phase;
            zombie.AbilityPhase = phase == 37 ? 2 : 3;
            zombie.UsedAbility = phase == 38;
            zombie.Submerged = false;
            var health = plant.Health;
            Tick(game, 80);
            Assert.That(zombie.Eating, Is.True);
            Assert.That(plant.Health, Is.LessThan(health), "An overlap must deliver the original native bite damage.");
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_eat"));
            Assert.That(CountSound(game, 5) + CountSound(game, 6), Is.GreaterThan(0), "The native chew cycle requests its original Foley.");
            var x = zombie.X;
            game.Plants.Clear();
            Tick(game, 12);
            Assert.That(zombie.Eating, Is.False);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_walk"));
            Assert.That(zombie.X, phase == 37 ? Is.GreaterThan(x) : Is.LessThan(x));
        }

        /// <summary>Checks independently calculated original integer ascent and descent samples.</summary>
        [Test]
        public void RiseAltitudeMatchesOriginalIntegerCurveSamples()
        {
            Assert.That(AdventureDiggerState.RiseAltitude(130), Is.EqualTo(-120f));
            Assert.That(AdventureDiggerState.RiseAltitude(85), Is.EqualTo(-15f));
            Assert.That(AdventureDiggerState.RiseAltitude(40), Is.EqualTo(20f));
            Assert.That(AdventureDiggerState.RiseAltitude(30), Is.EqualTo(20f));
            Assert.That(AdventureDiggerState.RiseAltitude(15), Is.EqualTo(15f));
            Assert.That(AdventureDiggerState.RiseAltitude(0), Is.Zero);
        }

        /// <summary>Checks original transient emergence and axe-loss phases reject ordinary projectile damage.</summary>
        /// <param name="phase">The original rising or axe-loss pause phase.</param>
        [TestCase(33)]
        [TestCase(34)]
        [TestCase(35)]
        public void EmergenceAndAxeLossPauseRejectNormalPeas(int phase)
        {
            var game = Board();
            var zombie = Spawn(game, 400f);
            zombie.PresentationNativePhase = phase;
            zombie.AbilityPhase = 1;
            zombie.Submerged = false;
            var pea = new Pea
            {
                Row = zombie.Row, FlightRow = zombie.Row, X = zombie.X, Damage = 20f,
                VelocityX = 4.5f, SourceKind = PlantKind.Peashooter
            };
            game.Peas.Add(pea);
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.False);
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(zombie.ArmorHealth, Is.EqualTo(100f));
            Assert.That(zombie.Health, Is.EqualTo(270f));
            Assert.That(game.Peas, Has.Member(pea));
        }

        /// <summary>Checks the independent original dirt effects outlive a zombie killed during emergence.</summary>
        [Test]
        public void RisingDirtSurvivesItsZombieAndKeepsOriginalFiveSecondParticleLifetime()
        {
            var game = Board();
            var zombie = Spawn(game, 9f);
            DiggerTicks(game, zombie, 1);
            Assert.That(game.Presentation.ZombieRiseEffects, Has.Count.EqualTo(1));
            var effect = game.Presentation.ZombieRiseEffects[0];
            Assert.That(effect.Digger, Is.True);
            Invoke(game, "KillAdventureZombie", zombie, 0U);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Tick(game, 210);
            Assert.That(game.Presentation.ZombieRiseEffects, Has.Member(effect));
            Tick(game, 300);
            Assert.That(game.Presentation.ZombieRiseEffects, Has.No.Member(effect));
        }

        /// <summary>Checks the original circle-to-rectangle magnet reach and its larger eating radius.</summary>
        /// <param name="eating">Whether the digger is eating when considered by the magnet.</param>
        /// <param name="radius">The original circular reach for this state.</param>
        [TestCase(false, 270)]
        [TestCase(true, 320)]
        public void MagnetReachIncludesTheOriginalCircularBoundary(bool eating, int radius)
        {
            var game = Board();
            var zombie = Spawn(game, 400f);
            zombie.Eating = eating;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, 150);
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - radius, 150, 150), Is.True);
            Assert.That(AdventureZombieEquipmentRules.InMagnetRange(zombie, body.X - radius - 1, 150, 150), Is.False);
        }

        /// <summary>Creates a deterministic dry-row board with unrelated automatic waves postponed.</summary>
        /// <returns>The active production adventure board.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Spawns a real digger at one exact original horizontal coordinate in dry row one.</summary>
        /// <param name="game">The active board receiving the digger.</param>
        /// <param name="nativeX">The original unscaled horizontal origin in pixels.</param>
        /// <returns>The production-initialized live digger.</returns>
        private static Zombie Spawn(GameSimulation game, float nativeX)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Digger, 1, AdventureZombieNativeRules.GridX(nativeX), 0)!;
        }

        /// <summary>Runs only the original species state update at an exact native boundary.</summary>
        /// <param name="game">The active production board.</param>
        /// <param name="zombie">The digger whose state advances.</param>
        /// <param name="ticks">The number of one-hundred-hertz state updates.</param>
        private static void DiggerTicks(GameSimulation game, Zombie zombie, int ticks)
        {
            for (var index = 0; index < ticks; index++)
            {
                Invoke(game, "AdvanceAdventureDigger", zombie, .01f);
            }
        }

        /// <summary>Runs the entire production simulation for an exact number of native updates.</summary>
        /// <param name="game">The active production board.</param>
        /// <param name="ticks">The number of one-hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var index = 0; index < ticks; index++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Counts original Foley requests retained by the production presentation model.</summary>
        /// <param name="game">The production board retaining requests.</param>
        /// <param name="foley">The original Foley category.</param>
        /// <returns>The number of matching original sound requests.</returns>
        private static int CountSound(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == foley).Count;
        }

        /// <summary>Invokes an existing production operation without introducing test-only runtime APIs.</summary>
        /// <param name="game">The production board owning the operation.</param>
        /// <param name="name">The private production operation name.</param>
        /// <param name="arguments">The operation's original arguments.</param>
        /// <returns>The original operation's result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
