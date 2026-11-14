#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Timing;
using TimingPlantKind = PlantsVsZombies.Timing.PlantKind;
using PlantKind = PlantsVsZombies.Simulation.PlantKind;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original plant sound requests at their existing simulation action boundaries.</summary>
    public sealed class AdventurePlantSoundTests
    {
        /// <summary>Checks every released short mushroom projectile requests the PC throw and puff pair once.</summary>
        /// <param name="kind">The mushroom whose production projectile path is exercised.</param>
        [TestCase(PlantKind.PuffShroom)]
        [TestCase(PlantKind.ScaredyShroom)]
        [TestCase(PlantKind.SeaShroom)]
        public void SmallMushroomReleaseRequestsThrowAndPuffOnce(PlantKind kind)
        {
            // PC Plant::Fire 0x47545D / 0x475489: Foley 3 followed by Foley 29.
            var game = Fixture();
            var plant = Place(game, kind);
            game.Presentation.ZombieSounds.Clear();
            var events = new List<GameEvent>();
            game.EventRaised += events.Add;
            Invoke(game, "FireAdventurePea", plant, plant.Row, false, 20f);
            Assert.That(game.Peas, Has.Count.EqualTo(1));
            Assert.That(Sounds(game), Is.EqualTo(new[] { 3, 29 }));
            Assert.That(events.FindAll(value => value == GameEvent.Shoot), Has.Count.EqualTo(1));
            Assert.That(game.Presentation._nativeShootSuppressions, Is.EqualTo(1));
        }

        /// <summary>Checks Fume fires its own sample while Gloom retains an observable silent launch pulse.</summary>
        [Test]
        public void FumeAndGloomDoNotAcquireTheGenericThrowSample()
        {
            // PC 0x475353 requests Foley 30; Gloom's 0x475367 path returns without launch Foley.
            var game = Fixture();
            var fume = Place(game, PlantKind.FumeShroom);
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "FireAdventureFume", fume);
            Assert.That(Sounds(game), Is.EqualTo(new[] { 30 }));
            game.Presentation.ZombieSounds.Clear();
            game.Presentation._nativeShootSuppressions = 0;
            var gloom = Place(game, PlantKind.GloomShroom, 2, 3);
            gloom.ActionTimer = 10f;
            gloom.UpgradeBurstTicks = 127;
            var events = new List<GameEvent>();
            game.EventRaised += events.Add;
            Invoke(game, "AdvanceAdventureUpgradePlant", gloom, .01f);
            Assert.That(gloom.ActionCount, Is.EqualTo(1));
            Assert.That(Sounds(game), Is.Empty);
            Assert.That(events.FindAll(value => value == GameEvent.Shoot), Has.Count.EqualTo(1));
            Assert.That(game.Presentation._nativeShootSuppressions, Is.EqualTo(1));
        }

        /// <summary>Checks one producer action requests one native sound even when it creates two suns.</summary>
        /// <param name="kind">The producer whose existing action is due.</param>
        /// <param name="sunCount">The number of suns created by this action.</param>
        [TestCase(PlantKind.Sunflower, 1)]
        [TestCase(PlantKind.SunShroom, 1)]
        [TestCase(PlantKind.TwinSunflower, 2)]
        public void SunProductionRequestsOneSoundPerProductionAction(PlantKind kind, int sunCount)
        {
            // PC Plant::Produce 0x46DE73 plays Foley 4 before the one or two pickup allocations.
            var game = Fixture();
            var plant = Place(game, kind);
            if (kind == PlantKind.Sunflower)
            {
                plant.Timing = PlantTiming.FromRaw(TimingPlantKind.Sunflower, 1, 0);
            }

            plant.ActionTimer = .01f;
            game.Presentation.ZombieSounds.Clear();
            game.Update(.01f);
            Assert.That(game.Suns, Has.Count.EqualTo(sunCount));
            Assert.That(CountSound(game, 4), Is.EqualTo(1));
            game.Update(.02f);
            Assert.That(CountSound(game, 4), Is.EqualTo(1));
        }

        /// <summary>Checks the Sun-shroom growth sample occurs once when the awake growth clock reaches its threshold.</summary>
        [Test]
        public void SunShroomGrowthRequestsItsOriginalGrowSampleOnce()
        {
            // PC UpdateSunShroom 0x46DFE8 starts Foley 63 with anim_grow.
            var game = Fixture();
            var plant = Place(game, PlantKind.SunShroom);
            plant.PresentationAwakeAge = 119.99f;
            plant.ActionTimer = 10f;
            game.Presentation.ZombieSounds.Clear();
            game.Update(.02f);
            Assert.That(CountSound(game, 63), Is.EqualTo(1));
            game.Update(.05f);
            Assert.That(CountSound(game, 63), Is.EqualTo(1));
        }

        /// <summary>Checks instant mushroom and wind specials request only their original activation sample.</summary>
        /// <param name="kind">The special plant whose fuse expires.</param>
        /// <param name="sound">The expected native Foley or direct sample identifier.</param>
        [TestCase(PlantKind.IceShroom, 18)]
        [TestCase(PlantKind.Blover, -2)]
        public void InstantSpecialRequestsItsNativeActivationSample(PlantKind kind, int sound)
        {
            var game = Fixture();
            var plant = Place(game, kind);
            game.Presentation.ZombieSounds.Clear();
            if (kind == PlantKind.Blover)
            {
                // PC 46C23A starts a fifty-tick fuse; 4715AB retains the blowing body for two hundred further updates.
                for (var tick = 0; tick < 49; tick++)
                {
                    game.Update(.01f);
                }

                Assert.That(CountSound(game, sound), Is.Zero);
                Assert.That(game.Plants, Has.Member(plant));
                game.Update(.01f);
                Assert.That(CountSound(game, sound), Is.EqualTo(1));
                Assert.That(game.Plants, Has.Member(plant));
                for (var tick = 0; tick < 200; tick++)
                {
                    game.Update(.01f);
                }

                Assert.That(game.Plants, Has.Member(plant));
                game.Update(.01f);
            }
            else
            {
                plant.ActionTimer = .01f;
                game.Update(.01f);
            }

            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(CountSound(game, sound), Is.EqualTo(1));
            Assert.That(CountSound(game, 3), Is.Zero);
            Assert.That(CountSound(game, 55), Is.Zero);
        }

        /// <summary>Checks Plantern requests its original direct sample when it is created.</summary>
        [Test]
        public void PlanternPlacementRequestsItsOwnSample()
        {
            var game = Fixture();
            Place(game, PlantKind.Plantern);
            Assert.That(CountSound(game, -3), Is.EqualTo(1));
        }

        /// <summary>Checks coffee's crumble and wake samples retain their separate original milestones.</summary>
        [Test]
        public void CoffeeAndWakeSamplesFollowTheCombinedTwoSecondTimer()
        {
            // PC Coffee special 0x47503E requests Foley 69; Plant::Update 0x47161E requests Foley 83 at wake counter 60.
            var game = Fixture(9);
            var mushroom = Place(game, PlantKind.PuffShroom);
            Assert.That(mushroom.Asleep, Is.True);
            Invoke(game, "PlaceAdventurePlant", PlantKind.CoffeeBean, mushroom.Row, mushroom.Column);
            game.Presentation.ZombieSounds.Clear();
            game.Update(.99f);
            Assert.That(CountSound(game, 69), Is.Zero);
            Assert.That(CountSound(game, 83), Is.Zero);
            game.Update(.02f);
            Assert.That(CountSound(game, 69), Is.EqualTo(1));
            Assert.That(CountSound(game, 83), Is.Zero);
            game.Update(.38f);
            Assert.That(CountSound(game, 83), Is.Zero);
            game.Update(.02f);
            Assert.That(CountSound(game, 83), Is.EqualTo(1));
            Assert.That(mushroom.Asleep, Is.True);
            game.Update(.61f);
            Assert.That(mushroom.Asleep, Is.False);
            Assert.That(CountSound(game, 69), Is.EqualTo(1));
            Assert.That(CountSound(game, 83), Is.EqualTo(1));
        }

        /// <summary>Checks an actual metal removal requests the magnet sample and respects the existing cooldown.</summary>
        [Test]
        public void MagnetRequestsOneSoundWhenMetalIsRemoved()
        {
            var game = Fixture();
            var plant = Place(game, PlantKind.MagnetShroom);
            plant.ActionTimer = .01f;
            var zombie = Target(game, ZombieKind.Buckethead, plant.Row, 5.5f);
            zombie.ArmorHealth = 1100f;
            game.Presentation.ZombieSounds.Clear();
            game.Update(.01f);
            Assert.That(zombie.ArmorHealth, Is.Zero);
            Assert.That(CountSound(game, 42), Is.EqualTo(1));
            game.Update(.02f);
            Assert.That(CountSound(game, 42), Is.EqualTo(1));
        }

        /// <summary>Checks one Gold Magnet action requests one sample for its batch of collectible currency.</summary>
        [Test]
        public void GoldMagnetRequestsOneSoundForItsCollectionBatch()
        {
            var game = Fixture();
            var plant = Place(game, PlantKind.GoldMagnet);
            var coin = game.RequiredSpawnAdventureCoin(AdventureCoinKind.Silver, 5.5f, 1f);
            coin.Age = 1f;
            plant.ActionTimer = 0f;
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
            Assert.That(coin.CollectorPlantId, Is.EqualTo(plant.Id));
            Assert.That(CountSound(game, 42), Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureUpgradePlant", plant, .01f);
            Assert.That(CountSound(game, 42), Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureCoins", .5f);
            Assert.That(game.Adventure.Coins, Is.Empty);
            Assert.That(CountSound(game, 31), Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureCoins", .01f);
            Assert.That(CountSound(game, 31), Is.EqualTo(1));
        }

        /// <summary>Checks Hypno-shroom preserves its chew Foley and also requests the original mind-control sample.</summary>
        [Test]
        public void HypnoChewRequestsFloopAndMindControl()
        {
            // PC AnimateChewSound 0x547610 requests Foley 68 before StartMindControlled.
            var game = Fixture();
            var plant = Place(game, PlantKind.HypnoShroom);
            var zombie = Target(game, ZombieKind.Normal, plant.Row, 4.5f);
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AnimateAdventureChew", zombie, plant, false);
            Assert.That(zombie.Hypnotized, Is.True);
            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(CountSound(game, 68), Is.EqualTo(1));
            Assert.That(CountSound(game, -5), Is.EqualTo(1));
        }

        /// <summary>Checks Squash target and landing cues do not fall back to the Cherry Bomb sample.</summary>
        /// <param name="level">The level supplying land or water terrain.</param>
        /// <param name="row">The target row.</param>
        /// <param name="landingSound">The native land thump or water splat Foley.</param>
        [TestCase(19, 1, 40)]
        [TestCase(29, 2, 1)]
        public void SquashUsesItsOwnTargetAndDelayedLandingSamples(int level, int row, int landingSound)
        {
            var game = Fixture(level);
            var plant = Place(game, PlantKind.Squash, row);
            plant.ActionTimer = .01f;
            Target(game, ZombieKind.Normal, row, 4.5f);
            game.Presentation.ZombieSounds.Clear();
            game.Update(.01f);
            Assert.That(CountSound(game, 41), Is.EqualTo(1));
            Assert.That(CountSound(game, landingSound), Is.Zero);
            Assert.That(CountSound(game, 55), Is.Zero);
            game.Update(1.84f);
            Assert.That(CountSound(game, landingSound), Is.Zero);
            game.Update(.01f);
            Assert.That(CountSound(game, landingSound), Is.EqualTo(1));
            Assert.That(CountSound(game, -4), Is.EqualTo(level == 29 ? 1 : 0));
        }

        /// <summary>Checks Kelp requests its grab immediately and its original water sound eighty ticks later.</summary>
        [Test]
        public void TangleKelpRetainsItsOriginalGrabAndSplashMilestones()
        {
            var game = Fixture(29);
            var plant = Place(game, PlantKind.TangleKelp, 2);
            plant.ActionTimer = .01f;
            Target(game, ZombieKind.Normal, 2, 4.5f).PresentationInPool = true;
            game.Presentation.ZombieSounds.Clear();
            game.Update(.01f);
            Assert.That(CountSound(game, 68), Is.EqualTo(1));
            Assert.That(CountSound(game, 53), Is.Zero);
            game.Update(.79f);
            Assert.That(CountSound(game, 53), Is.Zero);
            game.Update(.01f);
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
            game.Update(.02f);
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
        }

        /// <summary>Checks cob cues cross the original reanimation milestones once before actual shell release.</summary>
        [Test]
        public void CobCannonRequestsLoadingLaunchAndReleaseAtSeparateMilestones()
        {
            // PC charge has 15 frames at 12 fps (.5 marker); shooting has 42 frames at 12 fps (.48 marker).
            var game = Fixture();
            var plant = Place(game, PlantKind.CobCannon);
            plant.CannonPhase = CobCannonPhase.Loading;
            plant.CannonReloadRemaining = 1.89f;
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AdvanceAdventureCannon", plant, .01f);
            Assert.That(CountSound(game, 64), Is.Zero);
            Invoke(game, "AdvanceAdventureCannon", plant, .01f);
            Assert.That(CountSound(game, 64), Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureCannon", plant, .01f);
            Assert.That(CountSound(game, 64), Is.EqualTo(1));
            plant.CannonPhase = CobCannonPhase.Firing;
            plant.CannonReloadRemaining = .40f;
            Invoke(game, "AdvanceAdventureCannon", plant, .01f);
            Assert.That(CountSound(game, 76), Is.Zero);
            Invoke(game, "AdvanceAdventureCannon", plant, .02f);
            Assert.That(CountSound(game, 76), Is.EqualTo(1));
            Assert.That(game.Adventure.CobShells, Is.Empty);
            plant.CannonReloadRemaining = .01f;
            Invoke(game, "AdvanceAdventureCannon", plant, .01f);
            Assert.That(game.Adventure.CobShells, Has.Count.EqualTo(1));
            Assert.That(CountSound(game, 3), Is.EqualTo(1));
            Assert.That(CountSound(game, 76), Is.EqualTo(1));
        }

        /// <summary>Creates a playing simulation without nearby waves or persistent player data.</summary>
        /// <param name="level">The level supplying the required terrain.</param>
        /// <returns>The isolated production simulation.</returns>
        private static GameSimulation Fixture(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a fixture plant through the existing internal production placement path.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="kind">The desired plant kind.</param>
        /// <param name="row">The lawn row.</param>
        /// <param name="column">The lawn column.</param>
        /// <returns>The plant created by the simulation.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row = 1, int column = 4)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, column)!;
        }

        /// <summary>Creates a durable target fixture without advancing unrelated spawn presentation.</summary>
        /// <param name="game">The simulation receiving the target.</param>
        /// <param name="kind">The zombie kind.</param>
        /// <param name="row">The target row.</param>
        /// <param name="x">The target's horizontal lawn position.</param>
        /// <returns>The live target stored in the simulation.</returns>
        private static Zombie Target(GameSimulation game, ZombieKind kind, int row, float x)
        {
            var zombie = new Zombie { Id = 1000, Kind = kind, Row = row, X = x, Health = 270f, StartingHealth = 270f, PresentationNativeHeight = 0 };
            game.Zombies.Add(zombie);
            return zombie;
        }

        /// <summary>Counts requests for one native category in the production presentation queue.</summary>
        /// <param name="game">The simulation retaining sound requests.</param>
        /// <param name="sound">The Foley or direct sample category.</param>
        /// <returns>The number of matching requests.</returns>
        private static int CountSound(GameSimulation game, int sound)
        {
            return game.Presentation.ZombieSounds.FindAll(value => value.Foley == sound).Count;
        }

        /// <summary>Copies requested native categories in their production order for exact pair assertions.</summary>
        /// <param name="game">The simulation retaining sound requests.</param>
        /// <returns>The ordered native categories.</returns>
        private static List<int> Sounds(GameSimulation game)
        {
            var result = new List<int>();
            foreach (var sound in game.Presentation.ZombieSounds)
            {
                result.Add(sound.Foley);
            }

            return result;
        }

        /// <summary>Invokes an existing production action without adding a test-only runtime entry point.</summary>
        /// <param name="game">The simulation owning the action.</param>
        /// <param name="name">The nonpublic action name.</param>
        /// <param name="arguments">The production arguments.</param>
        /// <returns>The production action's return value.</returns>
        /// <exception cref="TargetInvocationException">The production action throws.</exception>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
