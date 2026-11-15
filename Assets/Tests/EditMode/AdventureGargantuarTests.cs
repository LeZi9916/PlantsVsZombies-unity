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
        /// <summary>Checks smash acquisition, the .64 event, full clip completion and original whole-cell crushing.</summary>
        /// <param name="level">The original day or roof board.</param>
        [TestCase(9)]
        [TestCase(49)]
        public void SmashUsesOriginalAnimationEventAndRetainsEveryCrushedLayer(int level)
        {
            var game = Board(level);
            var pot = Place(game, PlantKind.FlowerPot);
            var main = Place(game, PlantKind.Wallnut);
            var shell = Place(game, PlantKind.Pumpkin);
            shell.HasLadder = true;
            var giant = Spawn(game, 330f);
            Tick(game, 1);
            Assert.That(giant.PresentationNativePhase, Is.EqualTo(70));
            Assert.That(giant.PresentationAnimation, Is.EqualTo("anim_smash"));
            Assert.That(game.AdventurePresentationZombieRate(giant), Is.EqualTo(16f));
            Assert.That(giant.PresentationBodyBlendTicks, Is.EqualTo(20));
            var x = giant.X;
            Tick(game, 131);
            Assert.That(game.Plants, Does.Contain(main));
            Assert.That(Sounds(game, 40), Is.Zero);
            Tick(game, 1);
            Assert.That(game.Plants.Contains(main), Is.False);
            Assert.That(game.Plants.Contains(shell), Is.False);
            Assert.That(game.Plants.Contains(pot), Is.False);
            Assert.That(game.Presentation.SquishedPlants.Select(item => item.Plant), Is.EquivalentTo(new[] { pot, main, shell }));
            Assert.That(game.Presentation.SquishedPlants.All(item => item.Remaining == 500), Is.True);
            Assert.That(game.AdventureHasLadderAt(1, 3), Is.False);
            Assert.That(Sounds(game, 40), Is.EqualTo(1));
            Assert.That(Sounds(game, 21), Is.EqualTo(3));
            Assert.That(giant.X, Is.EqualTo(x));
            Tick(game, 74);
            Assert.That(giant.PresentationNativePhase, Is.EqualTo(70));
            Tick(game, 1);
            Assert.That(giant.PresentationNativePhase, Is.Zero);
            Assert.That(giant.PresentationAnimation, Is.EqualTo("anim_walk"));
            Tick(game, 425);
            Assert.That(game.Presentation.SquishedPlants.All(item => item.Remaining == 0), Is.True);
            Tick(game, 1);
            Assert.That(game.Presentation.SquishedPlants, Is.Empty);
        }

        /// <summary>Checks a smash rechecks the current target and still thumps after its original target disappears.</summary>
        [Test]
        public void SmashReacquiresAtImpactAndFreezingPausesItsBodyEvent()
        {
            var game = Board();
            var main = Place(game, PlantKind.Wallnut);
            var giant = Spawn(game, 330f);
            Tick(game, 1);
            Tick(game, 100);
            var age = giant.PresentationAnimationAge;
            giant.FrozenRemaining = .5f;
            Tick(game, 40);
            Assert.That(giant.PresentationAnimationAge, Is.EqualTo(age));
            SimulationFixture.Plants(game).Remove(main);
            giant.FrozenRemaining = 0f;
            Tick(game, 32);
            Assert.That(Sounds(game, 40), Is.EqualTo(1));
            Assert.That(game.Presentation.SquishedPlants, Is.Empty);
        }

        /// <summary>Checks strict half-health/X admission, PC's 360-pixel reference, timed release and complete imp landing.</summary>
        /// <param name="level">The original dry or roof board.</param>
        [TestCase(9)]
        [TestCase(49)]
        public void ThrowUsesOriginalThresholdAndBallisticsBeforeLanding(int level)
        {
            var game = Board(level);
            var giant = Spawn(game, 650f);
            giant.Health = 1500f;
            Tick(game, 1);
            Assert.That(giant.PresentationNativePhase, Is.Zero, "Exactly half health cannot throw.");
            giant.X = AdventureZombieNativeRules.GridX(400f);
            giant.Health = 1499f;
            Tick(game, 1);
            Assert.That(giant.PresentationNativePhase, Is.Zero, "Native X400 cannot throw.");
            giant.X = AdventureZombieNativeRules.GridX(650f);
            giant.ChillRemaining = 0f;
            Tick(game, 1);
            Assert.That(giant.PresentationNativePhase, Is.EqualTo(69));
            Assert.That(giant.PresentationAnimation, Is.EqualTo("anim_throw"));
            Assert.That(game.AdventurePresentationZombieRate(giant), Is.EqualTo(24f));
            Tick(game, 104);
            Assert.That(game.Zombies.Count, Is.EqualTo(1));
            Assert.That(giant.ThrownImp, Is.False);
            Tick(game, 1);
            var imp = game.Zombies.Single(zombie => zombie.Kind == ZombieKind.Imp);
            Assert.That(giant.ThrownImp, Is.True);
            Assert.That(AdventureZombieNativeRules.NativePositionX(imp.X), Is.EqualTo(517f).Within(.001f));
            Assert.That(imp.PresentationPreviousX, Is.EqualTo(imp.X), "The release frame must draw at the throwing hand before the imp's first movement tick.");
            Assert.That(imp.PresentationNativePhase, Is.EqualTo(71));
            Assert.That(imp.PresentationAltitude, Is.EqualTo(88f));
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(imp), Is.False);
            Assert.That(Invoke(game, "CanTakeAdventureAshDamage", imp, 0x7FU), Is.EqualTo(true));
            Assert.That(imp.PresentationAnimation, Is.EqualTo("anim_thrown"));
            var velocity = imp.ThrownImpState!.VelocityZ;
            Assert.That(velocity * 120f, level == 49 ? Is.EqualTo(110f).Within(.001f) : Is.InRange(190f, 290f));
            Assert.That(Sounds(game, 47), Is.EqualTo(1));
            Assert.That(Sounds(game, 88), Is.EqualTo(1));
            var start = imp.X;
            Tick(game, 1);
            Assert.That(imp.X, Is.EqualTo(start - 3f / 80f).Within(.000001f));
            Assert.That(imp.ThrownImpState.VelocityZ, Is.EqualTo(velocity - .05f).Within(.000001f));
            for (var tick = 0; tick < 500 && imp.PresentationNativePhase == 71; tick++)
            {
                Tick(game, 1);
            }

            Assert.That(imp.PresentationNativePhase, Is.EqualTo(72));
            Assert.That(imp.PresentationAltitude, Is.Zero);
            Assert.That(imp.PresentationAnimation, Is.EqualTo("anim_land"));
            var landed = imp.X;
            Tick(game, 24);
            Assert.That(imp.PresentationNativePhase, Is.EqualTo(72));
            Tick(game, 1);
            Assert.That(imp.PresentationNativePhase, Is.Zero);
            Assert.That(imp.X, Is.LessThan(landed), "PC resumes walking in the same update that landing completes.");
            Assert.That(Sounds(game, 88), Is.EqualTo(1));
        }

        /// <summary>Checks a near-edge roof throw retains PC's negative launch velocity and the giant's original row floor.</summary>
        [Test]
        public void NearRoofThrowPreservesNegativeVelocityAndRebasesOnlyOnTheNextImpUpdate()
        {
            var game = Board(49);
            var giant = Spawn(game, 401f);
            giant.Health = 1499f;
            Tick(game, 106);
            var imp = game.Zombies.Single(zombie => zombie.Kind == ZombieKind.Imp);
            Assert.That(imp.ThrownImpState!.VelocityZ, Is.EqualTo(-139f / 120f).Within(.000001f));
            Assert.That(imp.ThrownImpState.NativeY, Is.EqualTo(125f));
            Assert.That(imp.PresentationAltitude, Is.EqualTo(88f));
            Tick(game, 1);
            Assert.That(imp.ThrownImpState.NativeY, Is.EqualTo(158.75f).Within(.001f));
            Assert.That(imp.PresentationAltitude, Is.EqualTo(88f - 139f / 120f - .05f + 33.75f).Within(.001f));
        }

        /// <summary>Checks original integer-thirds body, arm, foot and red-eye head replacements.</summary>
        [Test]
        public void DurabilityUsesOriginalBodyImagesAndHidesReleasedImp()
        {
            var giant = new Zombie { Kind = ZombieKind.Gargantuar, StartingHealth = 3000f, Health = 2000f };
            string? Image(string track)
            {
                return AdventureZombieEquipmentRules.Image(giant, "Zombie_gargantuar", "Zombie_gargantuar", track);
            }

            Assert.That(Image("zombie_gargantua_body1"), Is.Null);
            giant.Health = 1999f;
            Assert.That(Image("zombie_gargantua_body1"), Is.EqualTo("IMAGE_REANIM_ZOMBIE_GARGANTUAR_BODY1_2"));
            Assert.That(Image("zombie_gargantuar_outerarm_lower"), Is.EqualTo("IMAGE_REANIM_ZOMBIE_GARGANTUAR_OUTERARM_LOWER2"));
            giant.Health = 1000f;
            Assert.That(Image("anim_head1"), Is.Null);
            giant.Health = 999f;
            Assert.That(Image("zombie_gargantuar_outerleg_foot"), Is.EqualTo("IMAGE_REANIM_ZOMBIE_GARGANTUAR_FOOT2"));
            Assert.That(Image("anim_head1"), Is.EqualTo("IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2"));
            giant.ModeHead = 32;
            giant.StartingHealth = 6000f;
            Assert.That(Image("anim_head1"), Is.EqualTo("IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2_REDEYE"));
            giant.ThrownImp = true;
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(giant, "Zombie_imp_head"), Is.False);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(giant, "zombie_gargantuar_whiterope"), Is.False);
        }

        /// <summary>Checks Spikerock takes nine original fifty-health smashes and damages the giant at impact.</summary>
        [Test]
        public void SpikerockKeepsOriginalNineSmashDurability()
        {
            var game = Board();
            var rock = Place(game, PlantKind.Spikerock);
            rock.ActionTimer = 10000f;
            var giant = Spawn(game, 330f);
            Assert.That(rock.Health, Is.EqualTo(450f));
            for (var impact = 1; impact <= 9; impact++)
            {
                for (var tick = 0; tick < 210 && Sounds(game, 40) < impact; tick++)
                {
                    Tick(game, 1);
                }

                Assert.That(Sounds(game, 40), Is.EqualTo(impact));
                Assert.That(rock.Health, Is.EqualTo(450f - impact * 50f));
                Assert.That(game.Plants.Contains(rock), Is.EqualTo(impact < 9));
            }

            Assert.That(giant.Health, Is.EqualTo(2820f));
            Assert.That(game.Presentation.SquishedPlants, Is.Empty);
        }

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
            SimulationFixture.Plants(game).Clear();
            SimulationFixture.Zombies(game).Clear();
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
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
