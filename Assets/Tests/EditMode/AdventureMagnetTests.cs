#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses original PC Magnet-shroom circular targeting, priority, disarming and full animation lifecycle.</summary>
    public sealed class AdventureMagnetTests
    {
        /// <summary>Checks a body between the former three-cell approximation and the native circle is acquired immediately.</summary>
        [Test]
        public void ReadyMagnetChecksTheOriginalCircleEveryNativeUpdate()
        {
            var game = Board();
            var plant = Place(game, PlantKind.MagnetShroom, 2, 4);
            var target = Spawn(game, ZombieKind.Buckethead, 2, 580f);
            TickPlants(game, 1);
            Assert.That(target.ArmorHealth, Is.Zero);
            Assert.That(plant.MagnetPresentation!.NativePhase, Is.EqualTo(26));
            Assert.That(plant.MagnetPresentation.Animation, Is.EqualTo("anim_shooting"));
            Assert.That(plant.MagnetPresentation.AnimationRate, Is.EqualTo(12f));
            Assert.That(plant.MagnetPresentation.CooldownTicks, Is.EqualTo(1500));
            Assert.That(plant.MagnetPresentation.Item!.NativeKind, Is.EqualTo(1));
            Assert.That(SoundCount(game), Is.EqualTo(1));
        }

        /// <summary>Checks inclusive native circle boundaries and the original larger radius while a target eats.</summary>
        /// <param name="eating">Whether the target is eating.</param>
        /// <param name="distance">The nearest horizontal body distance from the native plant x.</param>
        /// <param name="eligible">Whether the original circle includes the body.</param>
        [TestCase(false, 270, true)]
        [TestCase(false, 271, false)]
        [TestCase(true, 320, true)]
        [TestCase(true, 321, false)]
        public void CircularRangeUsesBodyEdgesAndEatingRadius(bool eating, int distance, bool eligible)
        {
            Assert.That(AdventureMagnetRules.InRange(360, 280, new NativeRect(360 + distance, 250, 42, 115), eating), Is.EqualTo(eligible));
            Assert.That(AdventureMagnetRules.InRange(360, 280, new NativeRect(90, 299, 270, 2), false), Is.True,
                "The original circle tests body edges rather than either the actor center or a rectangular cell window.");
        }

        /// <summary>Checks nearest top-left distance and eighty-pixel cross-row cost override zombie creation order.</summary>
        [Test]
        public void NearestTargetUsesOriginalCrossRowPenaltyInsteadOfFirstInList()
        {
            var game = Board();
            Place(game, PlantKind.MagnetShroom, 2, 4);
            var nearOtherRow = Spawn(game, ZombieKind.Buckethead, 1, 420f);
            var selected = Spawn(game, ZombieKind.Buckethead, 2, 500f);
            var farther = Spawn(game, ZombieKind.Buckethead, 2, 580f);
            TickPlants(game, 1);
            Assert.That(selected.ArmorHealth, Is.Zero);
            Assert.That(nearOtherRow.ArmorHealth, Is.EqualTo(1100f));
            Assert.That(farther.ArmorHealth, Is.EqualTo(1100f));
        }

        /// <summary>Checks original head, height, death, grave, screen-edge, and maximum-row gates.</summary>
        [Test]
        public void TargetGatesRejectInvalidActorsWithoutLosingTheMagneticObject()
        {
            var game = Board();
            var target = Spawn(game, ZombieKind.Buckethead, 2, 400f);
            var body = new NativeRect(436, 250, 42, 115);
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.True);
            target.Headless = true;
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.False);
            target.Headless = false;
            target.Hypnotized = true;
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.False);
            target.Hypnotized = false;
            target.PresentationNativeHeight = 7;
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.False);
            target.PresentationNativeHeight = 0;
            target.PresentationNativePhase = 14;
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.False);
            target.PresentationNativePhase = 1;
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 2), Is.False);
            target.PresentationNativePhase = 0;
            Assert.That(AdventureMagnetRules.CanTarget(target, new NativeRect(801, 250, 42, 115), 2), Is.False);
            Assert.That(AdventureMagnetRules.CanTarget(target, body, 5), Is.False);
            Assert.That(target.ArmorHealth, Is.EqualTo(1100f));
        }

        /// <summary>Checks all supported original metal kinds retain damage artwork and apply only their own disarming branch.</summary>
        /// <param name="kind">The original magnetic target kind.</param>
        /// <param name="itemKind">The original MagnetItemType captured by the plant.</param>
        /// <param name="damaged">Whether armor or shield is in its final damage stage.</param>
        [TestCase(ZombieKind.Buckethead, 1, false)]
        [TestCase(ZombieKind.Buckethead, 3, true)]
        [TestCase(ZombieKind.Football, 4, false)]
        [TestCase(ZombieKind.Football, 6, true)]
        [TestCase(ZombieKind.ScreenDoor, 7, false)]
        [TestCase(ZombieKind.ScreenDoor, 9, true)]
        [TestCase(ZombieKind.Ladder, 14, false)]
        [TestCase(ZombieKind.Ladder, 16, true)]
        [TestCase(ZombieKind.Pogo, 10, false)]
        [TestCase(ZombieKind.JackInTheBox, 13, false)]
        [TestCase(ZombieKind.Digger, 21, false)]
        public void EachMetalRetainsItsOriginalArtworkAndDisarmingBehavior(ZombieKind kind, int itemKind, bool damaged)
        {
            var game = Board();
            var plant = Place(game, PlantKind.MagnetShroom, 2, 4);
            var target = Spawn(game, kind, 2, 450f);
            var health = target.Health;
            if (damaged)
            {
                if (target.ArmorHealth > 0f)
                {
                    target.ArmorHealth = 100f;
                }
                else
                {
                    target.ShieldHealth = 100f;
                }
            }

            TickPlants(game, 1);
            Assert.That(plant.MagnetPresentation!.Item!.NativeKind, Is.EqualTo(itemKind));
            Assert.That(target.Health, Is.EqualTo(health));
            Assert.That(SoundCount(game), Is.EqualTo(1));
            Assert.That(game.Presentation.ZombiePartDrops, Is.Empty, "Magnetic removal does not create a second detached part particle.");
            if (kind == ZombieKind.Digger)
            {
                Assert.That(target.ArmorHealth, Is.EqualTo(100f), "The original nonmagnetic hardhat remains equipped.");
                Assert.That(target.PresentationNativePhase, Is.EqualTo(34));
            }
            else if (kind == ZombieKind.Pogo)
            {
                Assert.That(target.PresentationNativeHeight, Is.EqualTo(7));
                Assert.That(target.UsedAbility, Is.True);
            }
            else if (kind == ZombieKind.JackInTheBox)
            {
                Assert.That(target.PresentationNativePhase, Is.Zero);
                Assert.That(target.JackInTheBoxTicks, Is.Zero);
            }
            else
            {
                Assert.That(target.ArmorHealth + target.ShieldHealth, Is.Zero);
            }
        }

        /// <summary>Checks native zero-blend shield removal does not override later meal or death transitions.</summary>
        /// <param name="kind">The original door or ladder shield carrier.</param>
        [TestCase(ZombieKind.ScreenDoor)]
        [TestCase(ZombieKind.Ladder)]
        public void ShieldRemovalKeepsItsInstantWalkAndLaterNativeTransitionBlends(ZombieKind kind)
        {
            var game = Board();
            Place(game, PlantKind.MagnetShroom, 2, 4);
            var door = Spawn(game, kind, 2, 450f);
            TickPlants(game, 1);
            Assert.That(door.PresentationBodyBlendTicks, Is.Zero);
            door.Eating = true;
            Assert.That(game.AdventurePresentationZombieAnimation(door), Is.EqualTo("anim_eat"));
            Assert.That(door.PresentationBodyBlendTicks, Is.EqualTo(20));
            door.PresentationBodyBlendTicks = 0;
            Invoke(game, "KillAdventureZombie", door, 0U);
            Assert.That(door.PresentationBodyBlendTicks, Is.EqualTo(20));
        }

        /// <summary>Checks later-stage tiny armor uses its actual native maximum rather than full-size health thresholds.</summary>
        [Test]
        public void SmallFullHealthBucketRetainsUndamagedMetalArtwork()
        {
            var game = Board(25);
            var plant = Place(game, PlantKind.MagnetShroom, 1, 4);
            plant.Asleep = false;
            var target = Spawn(game, ZombieKind.Buckethead, 1, 450f);
            Assert.That(target.ArmorHealth, Is.EqualTo(275f));
            TickPlants(game, 1);
            Assert.That(plant.MagnetPresentation!.Item!.NativeKind, Is.EqualTo(1));
        }

        /// <summary>Checks original body targets take precedence over placed ladders, followed by the ladder's square cell range.</summary>
        [Test]
        public void ZombieMetalPrecedesPlacedLaddersAndLadderRangeIncludesSquareCorners()
        {
            var game = Board();
            var magnet = Place(game, PlantKind.MagnetShroom, 2, 4);
            var ladder = Place(game, PlantKind.TallNut, 0, 6);
            ladder.HasLadder = true;
            var bucket = Spawn(game, ZombieKind.Buckethead, 2, 450f);
            TickPlants(game, 1);
            Assert.That(bucket.ArmorHealth, Is.Zero);
            Assert.That(ladder.HasLadder, Is.True);
            game.Plants.Remove(magnet);
            magnet = Place(game, PlantKind.MagnetShroom, 2, 4);
            TickPlants(game, 1);
            Assert.That(ladder.HasLadder, Is.False);
            Assert.That(magnet.MagnetPresentation!.Item!.NativeKind, Is.EqualTo(20));
            Assert.That(magnet.MagnetPresentation.Item.X, Is.EqualTo(560f));
            Assert.That(magnet.MagnetPresentation.Item.DestinationX, Is.InRange(0f, 20f));
            Assert.That(magnet.MagnetPresentation.Item.DestinationY, Is.InRange(-10f, 10f));
        }

        /// <summary>Checks the original sucking clip, two-FPS charging clip, full fifteen-second countdown and delayed rearm.</summary>
        [Test]
        public void SuckingChargingAndRearmUseTheOriginalIndependentTimers()
        {
            var game = Board();
            var plant = Place(game, PlantKind.MagnetShroom, 2, 4);
            Spawn(game, ZombieKind.Buckethead, 2, 450f);
            var second = Spawn(game, ZombieKind.Buckethead, 2, 500f);
            TickPlants(game, 1);
            var state = plant.MagnetPresentation!;
            TickPlants(game, 216);
            Assert.That(state.NativePhase, Is.EqualTo(26));
            TickPlants(game, 1);
            Assert.That(state.NativePhase, Is.EqualTo(27));
            Assert.That(state.Animation, Is.EqualTo("anim_nonactive_idle2"));
            Assert.That(state.AnimationRate, Is.EqualTo(2f));
            TickPlants(game, 1282);
            Assert.That(state.NativePhase, Is.EqualTo(27));
            Assert.That(state.Item, Is.Not.Null);
            TickPlants(game, 1);
            Assert.That(state.NativePhase, Is.EqualTo(1));
            Assert.That(state.AnimationRate, Is.InRange(10f, 15f));
            Assert.That(state.Item, Is.Null);
            Assert.That(second.ArmorHealth, Is.EqualTo(1100f));
            TickPlants(game, 1);
            Assert.That(second.ArmorHealth, Is.Zero);
            Assert.That(SoundCount(game), Is.EqualTo(2));
        }

        /// <summary>Checks sleeping mushrooms cannot attract or run a charging countdown.</summary>
        [Test]
        public void SleepingMagnetFreezesItsAbilityAndResumesAfterWaking()
        {
            var game = Board();
            var plant = Place(game, PlantKind.MagnetShroom, 2, 4);
            var target = Spawn(game, ZombieKind.Buckethead, 2, 450f);
            plant.Asleep = true;
            TickPlants(game, 10);
            Assert.That(plant.MagnetPresentation, Is.Null);
            plant.Asleep = false;
            TickPlants(game, 1);
            Assert.That(target.ArmorHealth, Is.Zero);
            plant.Asleep = true;
            TickPlants(game, 10);
            Assert.That(plant.MagnetPresentation!.CooldownTicks, Is.EqualTo(1500));
            plant.Asleep = false;
            TickPlants(game, 1);
            Assert.That(plant.MagnetPresentation.CooldownTicks, Is.EqualTo(1499));
        }

        /// <summary>Checks native magnetic item motion and its distinct twenty-pixel movement and draw-order thresholds.</summary>
        [Test]
        public void MetalMovesFivePercentPerUpdateAndStopsWithinTwentyPixels()
        {
            var item = new AdventureMagnetItem { X = 100f, Y = 0f, PositionResolved = true, AgeTicks = 1 };
            Assert.That(AdventureMagnetRules.SampleItem(item, 0f, 0f, out var x, out var y), Is.True);
            Assert.That(x, Is.EqualTo(95f));
            Assert.That(y, Is.Zero);
            item.AgeTicks = 1500;
            Assert.That(AdventureMagnetRules.SampleItem(item, 0f, 0f, out x, out y), Is.False);
            Assert.That(x, Is.EqualTo(19.37115f).Within(.00003f));
            item.X = 20f;
            item.AgeTicks = 0;
            Assert.That(AdventureMagnetRules.SampleItem(item, 0f, 0f, out x, out y), Is.False);
            item.AgeTicks = 1;
            AdventureMagnetRules.SampleItem(item, 0f, 0f, out x, out y);
            Assert.That(x, Is.EqualTo(19f));
        }

        /// <summary>Checks two magnets cannot remove the same original piece of metal twice in one update.</summary>
        [Test]
        public void TwoMagnetsDoNotDuplicateOneZombieObject()
        {
            var game = Board();
            var first = Place(game, PlantKind.MagnetShroom, 2, 3);
            var second = Place(game, PlantKind.MagnetShroom, 2, 4);
            Spawn(game, ZombieKind.Buckethead, 2, 500f);
            TickPlants(game, 1);
            Assert.That(first.MagnetPresentation!.Item, Is.Not.Null);
            Assert.That(second.MagnetPresentation!.Item, Is.Null);
            Assert.That(SoundCount(game), Is.EqualTo(1));
        }

        /// <summary>Creates a quiet original adventure board.</summary>
        /// <param name="level">The original adventure level selecting terrain and size rules.</param>
        /// <returns>The active board with unrelated wave spawning postponed.</returns>
        private static GameSimulation Board(int level = 18)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            Assert.That(game.State, Is.EqualTo(MatchState.Playing));
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Places a real plant through the production initialization path.</summary>
        /// <param name="game">The receiving board.</param>
        /// <param name="kind">The original plant kind.</param>
        /// <param name="row">The original row.</param>
        /// <param name="column">The original column.</param>
        /// <returns>The initialized plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, column)!;
        }

        /// <summary>Creates a real zombie at an explicit original body origin.</summary>
        /// <param name="game">The receiving board.</param>
        /// <param name="kind">The original zombie kind.</param>
        /// <param name="row">The original row.</param>
        /// <param name="x">The original native x coordinate.</param>
        /// <returns>The production-initialized zombie.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, row, AdventureZombieNativeRules.GridX(x), 0)!;
        }

        /// <summary>Advances only native plant abilities to keep independent target movement outside timing assertions.</summary>
        /// <param name="game">The active production board.</param>
        /// <param name="ticks">The number of original hundred-hertz updates.</param>
        private static void TickPlants(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
            }
        }

        /// <summary>Counts retained original attraction sound requests.</summary>
        /// <param name="game">The active production board.</param>
        /// <returns>The number of original magnet Foley requests.</returns>
        private static int SoundCount(GameSimulation game)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 42).Count;
        }

        /// <summary>Invokes an existing production operation without adding runtime-only test hooks.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="name">The original private operation.</param>
        /// <param name="arguments">The original arguments.</param>
        /// <returns>The production operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
