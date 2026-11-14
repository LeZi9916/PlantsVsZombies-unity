#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses PC ladder placement, continuous climbing, interruption and cell lifetime.</summary>
    public sealed class AdventureLadderTests
    {
        /// <summary>Checks the complete native placing clip before generating the ladder and losing its shield.</summary>
        /// <param name="kind">The original defensive plant that accepts a ladder.</param>
        [TestCase(PlantKind.Wallnut)]
        [TestCase(PlantKind.TallNut)]
        [TestCase(PlantKind.Pumpkin)]
        public void PlacementWaitsForTheOriginalTwelveFrameClip(PlantKind kind)
        {
            var game = Board();
            var plant = Place(game, kind);
            var zombie = Spawn(game, ZombieKind.Ladder, 400f);
            var originalX = zombie.X;
            var originalVelocity = zombie.PresentationNativeVelocity;
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(77));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_placeladder"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(24f));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(zombie), Is.True);
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(10));
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(originalVelocity));
            Assert.That(zombie.X, Is.EqualTo(originalX));
            Assert.That(plant.HasLadder, Is.False);
            Tick(game, 49);
            Assert.That(plant.HasLadder, Is.False);
            Assert.That(zombie.X, Is.EqualTo(originalX));
            Tick(game, 1);
            Assert.That(plant.HasLadder, Is.True);
            Assert.That(zombie.ShieldHealth, Is.Zero);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(6));
            Assert.That(zombie.UseLadderColumn, Is.EqualTo(4));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.Zero);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == -7).Count, Is.EqualTo(1));
            Assert.That(plant.Health, Is.EqualTo(kind == PlantKind.TallNut ? 8000f : 4000f));
        }

        /// <summary>Checks a vanished target returns the surviving carrier to its original carrying walk.</summary>
        [Test]
        public void MissingPlacementTargetKeepsTheCarriedLadder()
        {
            var game = Board();
            var plant = Place(game, PlantKind.Wallnut);
            var zombie = Spawn(game, ZombieKind.Ladder, 400f);
            Tick(game, 1);
            Assert.That(game.TryShovelAdventurePlant(plant), Is.True);
            Tick(game, 50);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(76));
            Assert.That(zombie.ShieldHealth, Is.GreaterThan(0f));
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(game.AdventureHasLadderAt(1, 4), Is.False);
        }

        /// <summary>Checks losing the carried shield interrupts placement even after the actor loses its head.</summary>
        /// <param name="headless">Whether the original head has already been lost.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void LostShieldCancelsPlacementWithoutCreatingALadder(bool headless)
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            var zombie = Spawn(game, ZombieKind.Ladder, 400f);
            Tick(game, 1);
            zombie.ShieldHealth = 0f;
            zombie.Headless = headless;
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(plant.HasLadder, Is.False);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
        }

        /// <summary>Checks freezing and butter stop both the original action clock and movement.</summary>
        /// <param name="butter">Whether butter rather than ice immobilizes the carrier.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ImmobilizationHoldsThePlacementClock(bool butter)
        {
            var game = Board();
            var plant = Place(game, PlantKind.Wallnut);
            var zombie = Spawn(game, ZombieKind.Ladder, 400f);
            Tick(game, 1);
            if (butter)
            {
                zombie.PresentationButter = 2f;
            }
            else
            {
                zombie.FrozenRemaining = 2f;
            }

            var age = zombie.PresentationAnimationAge;
            var x = zombie.X;
            Tick(game, 30);
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(age));
            Assert.That(zombie.X, Is.EqualTo(x));
            Assert.That(plant.HasLadder, Is.False);
        }

        /// <summary>Checks chill slows the placement clip while the subsequent native rise remains .8 pixels per action.</summary>
        [Test]
        public void ChilledPlacementAndRiseKeepTheirIndependentNativeRates()
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            var zombie = Spawn(game, ZombieKind.Ladder, 400f);
            Tick(game, 1);
            zombie.ChillRemaining = 5f;
            Tick(game, 99);
            Assert.That(plant.HasLadder, Is.False);
            Tick(game, 1);
            Assert.That(plant.HasLadder, Is.True);
            Tick(game, 1);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(.8f).Within(.0001f));
        }

        /// <summary>Checks real followers climb and fall while walking, leaving the ladder and plant intact.</summary>
        /// <param name="kind">The original walking body using an existing ladder.</param>
        [TestCase(ZombieKind.Normal)]
        [TestCase(ZombieKind.Conehead)]
        [TestCase(ZombieKind.Football)]
        [TestCase(ZombieKind.Ladder)]
        [TestCase(ZombieKind.PoleVaulter)]
        public void FollowingZombieUsesAltitudeRatherThanWalkingThroughThePlant(ZombieKind kind)
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            plant.HasLadder = true;
            var zombie = Spawn(game, kind, kind == ZombieKind.Ladder ? 400f : 380f);
            Tick(game, 1);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(6));
            Assert.That(zombie.UseLadderColumn, Is.EqualTo(4));
            var startX = zombie.X;
            var rose = false;
            var fell = false;
            for (var tick = 0; tick < 250; tick++)
            {
                Tick(game, 1);
                rose |= zombie.PresentationAltitude > 0f;
                fell |= zombie.PresentationNativeHeight == 7;
                if (rose && zombie.PresentationNativeHeight == 0)
                {
                    break;
                }
            }

            Assert.That(rose, Is.True);
            Assert.That(fell, Is.True);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(zombie.X, Is.LessThan(startX));
            Assert.That(plant.Health, Is.EqualTo(8000f));
            Assert.That(plant.HasLadder, Is.True);
            Assert.That(zombie.PoolState == null || zombie.PoolState.Altitude == 0f, Is.True);
        }

        /// <summary>Checks an approaching carrier uses an existing cell ladder without placing or losing its own shield.</summary>
        /// <param name="lifted">Whether a bungee has lifted the defensive body, leaving its flower pot targetable.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void CarryingZombieClimbsAnExistingLadderFromItsNaturalApproach(bool lifted)
        {
            var game = Board();
            var pot = Place(game, PlantKind.FlowerPot);
            var plant = Place(game, PlantKind.TallNut);
            plant.HasLadder = true;
            plant.BungeeState = lifted ? 2 : 0;
            var zombie = Spawn(game, ZombieKind.Ladder, 520f);
            var shield = zombie.ShieldHealth;
            var rose = false;
            var fell = false;
            for (var tick = 0; tick < 900; tick++)
            {
                Tick(game, 1);
                Assert.That(zombie.PresentationNativePhase, Is.EqualTo(76), "An existing ladder must keep the carrying walk.");
                rose |= zombie.PresentationNativeHeight == 6 && zombie.PresentationAltitude > 0f;
                fell |= zombie.PresentationNativeHeight == 7;
                if (fell && zombie.PresentationNativeHeight == 0)
                {
                    break;
                }
            }

            Assert.That(rose, Is.True, "The carrying body must rise while walking over the cell ladder.");
            Assert.That(fell, Is.True);
            Assert.That(zombie.PresentationNativeHeight, Is.Zero);
            Assert.That(zombie.PresentationAltitude, Is.Zero);
            Assert.That(zombie.ShieldHealth, Is.EqualTo(shield));
            Assert.That(zombie.UsedAbility, Is.False);
            Assert.That(pot.Health, Is.EqualTo(300f));
            Assert.That(plant.Health, Is.EqualTo(8000f));
        }

        /// <summary>Checks radial specials clear native square cell ranges even without zombies or circular overlap.</summary>
        /// <param name="level">The native board background.</param>
        /// <param name="range">The original explosion's cell range.</param>
        [TestCase(9, 1)]
        [TestCase(19, 3)]
        [TestCase(29, 1)]
        [TestCase(49, 1)]
        [TestCase(49, 3)]
        public void ExplosionsClearTheOriginalCellSquare(int level, int range)
        {
            var game = Board(level);
            FillLadders(game);
            // PC 4294C0 converts the blast center through PixelToGrid, then uses the row range for both axes.
            // A radius of one isolates grid-item cleanup from the zombie circle-intersection branch.
            Invoke(game, "BlastAdventureRadius", 200, level == 49 ? 299 : 280, 1, 1, range, 0u);
            foreach (var plant in game.Plants)
            {
                var expected = Math.Abs(plant.Column - 2) > range || Math.Abs(plant.Row - 2) > range;
                Assert.That(plant.HasLadder, Is.EqualTo(expected), "Cell " + plant.Column + "," + plant.Row);
            }
        }

        /// <summary>Checks the roof inverse-grid offset, rather than the supplied zombie-damage row, centers ladder cleanup.</summary>
        [Test]
        public void RoofExplosionUsesItsPixelCenterForLadderCleanup()
        {
            var game = Board();
            FillLadders(game);
            Invoke(game, "BlastAdventureRadius", 200, 289, 4, 115, 1, 0x7Fu);
            foreach (var plant in game.Plants)
            {
                // Column two's inverse slope is forty: (289 - 80 - 40) / 85 = row one.
                var expected = Math.Abs(plant.Column - 2) > 1 || Math.Abs(plant.Row - 1) > 1;
                Assert.That(plant.HasLadder, Is.EqualTo(expected));
            }
        }

        /// <summary>Checks a real Jalapeno special removes every ladder in its row and preserves other rows.</summary>
        [Test]
        public void JalapenoClearsItsEntireRowWithoutTargets()
        {
            var game = Board();
            FillLadders(game);
            var pepper = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Jalapeno, 2, 4)!;
            Invoke(game, "DetonateAdventureJalapeno", pepper);
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 9; column++)
                {
                    Assert.That(game.AdventureHasLadderAt(row, column), Is.EqualTo(row != 2));
                }
            }
        }

        /// <summary>Checks a non-burning mine still clears only the original explosion-center cell.</summary>
        [Test]
        public void MineClearsOnlyItsOwnCellAcrossAllPlantLayers()
        {
            var game = Board();
            FillLadders(game);
            var mine = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.PotatoMine, 1, 4)!;
            mine.HasLadder = true;
            Invoke(game, "DamageAdventurePotatoMine", mine);
            foreach (var plant in game.Plants)
            {
                Assert.That(plant.HasLadder, Is.EqualTo(plant.Row != 1 || plant.Column != 4));
            }
        }

        /// <summary>Checks explosion removal makes a surviving climber fall on its next native action.</summary>
        [Test]
        public void DestroyedLadderMakesAnUnaffectedCarrierFall()
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            plant.HasLadder = true;
            var zombie = Spawn(game, ZombieKind.Ladder, 380f);
            zombie.UseLadderColumn = 4;
            zombie.PresentationNativeHeight = 6;
            zombie.PresentationAltitude = 40f;
            Invoke(game, "BlastAdventureRadius", 400, 215, 1, 1, 1, 0u);
            Tick(game, 1);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(39f));
            Assert.That(zombie.ShieldHealth, Is.GreaterThan(0f));
        }

        /// <summary>Creates a complete grid of harmless ladder-bearing support plants.</summary>
        /// <param name="game">The board whose complete grid receives surviving placed ladders.</param>
        private static void FillLadders(GameSimulation game)
        {
            for (var row = 0; row < game.RowCount; row++)
            {
                for (var column = 0; column < 9; column++)
                {
                    var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, row, column)!;
                    plant.HasLadder = true;
                }
            }
        }

        /// <summary>Checks the native probe and falling update execute in the same original action pass.</summary>
        [Test]
        public void RemovedLadderImmediatelyChangesClimbingIntoFalling()
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            plant.HasLadder = true;
            var zombie = Spawn(game, ZombieKind.Normal, 380f);
            zombie.UseLadderColumn = 4;
            zombie.PresentationNativeHeight = 6;
            zombie.PresentationAltitude = 40f;
            zombie.PoolState = new AdventureZombiePoolState { Altitude = 40f };
            plant.HasLadder = false;
            Tick(game, 1);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(39f));
            Assert.That(zombie.PresentationBiteBodyY, Is.EqualTo(-39));
            Assert.That(zombie.PoolState!.Altitude, Is.EqualTo(39f));
        }

        /// <summary>Checks native ascent reaches ninety pixels then immediately executes its one-pixel fall.</summary>
        [Test]
        public void TopTransitionKeepsTheNativeRiseAndFallOrder()
        {
            var game = Board();
            var plant = Place(game, PlantKind.TallNut);
            plant.HasLadder = true;
            var zombie = Spawn(game, ZombieKind.Normal, 375f);
            zombie.UseLadderColumn = 4;
            zombie.PresentationNativeHeight = 6;
            zombie.PresentationAltitude = 89.5f;
            Tick(game, 1);
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(89.3f).Within(.0001f));
        }

        /// <summary>Checks deleting any non-flying occupant removes the cell ladder even when its shell survives.</summary>
        [Test]
        public void ShovelingTheProtectedBodyRemovesTheSharedLadder()
        {
            var game = Board();
            var main = Place(game, PlantKind.Peashooter);
            var pumpkin = Place(game, PlantKind.Pumpkin);
            pumpkin.HasLadder = true;
            Assert.That(game.AdventureHasLadderAt(1, 4), Is.True);
            Assert.That(game.TryShovelAdventurePlant(main), Is.True);
            Assert.That(game.Plants, Has.Member(pumpkin));
            Assert.That(game.AdventureHasLadderAt(1, 4), Is.False);
        }

        /// <summary>Checks a climbing death retains its raised body and falls during the original corpse playback.</summary>
        [Test]
        public void DeathChangesUpLadderIntoTheOriginalFallingCorpse()
        {
            var game = Board();
            var zombie = Spawn(game, ZombieKind.Normal, 380f);
            zombie.UseLadderColumn = 4;
            zombie.PresentationNativeHeight = 6;
            zombie.PresentationAltitude = 20f;
            Invoke(game, "HitAdventureZombie", zombie, 300f, true, 0u);
            Assert.That(game.Presentation.Corpses.Count, Is.EqualTo(1));
            Assert.That(zombie.PresentationNativeHeight, Is.EqualTo(7));
            Invoke(game, "AdventurePresentationTick", .01f);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(19f));
        }

        /// <summary>Creates a controlled production board without waves, graves or preplanted objects.</summary>
        /// <param name="level">The original level selecting the board background and row count.</param>
        /// <returns>The playing simulation for original ladder interactions.</returns>
        private static GameSimulation Board(int level = 49)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            return game;
        }

        /// <summary>Places a real plant in row one, column four.</summary>
        /// <param name="game">The production board receiving the plant.</param>
        /// <param name="kind">The native plant type.</param>
        /// <returns>The initialized live plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 4)!;
        }

        /// <summary>Creates a real native zombie at a controlled board-pixel coordinate.</summary>
        /// <param name="game">The production board receiving the actor.</param>
        /// <param name="kind">The native zombie type.</param>
        /// <param name="nativeX">The actor's original horizontal position.</param>
        /// <returns>The initialized live actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, float nativeX)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, 1,
                AdventureZombieNativeRules.GridX(nativeX), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Runs an exact number of native production updates.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="ticks">The number of hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Invokes a production entry point while retaining its original implementation.</summary>
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
