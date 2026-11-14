#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original Torchwood admission, snow thawing and independent FirePea attachment clocks.</summary>
    public sealed class AdventureTorchwoodTests
    {
        /// <summary>Checks ignition retains position and motion while creating the original new fire animation and cue.</summary>
        /// <param name="kind">The original pea source.</param>
        /// <param name="backward">Whether the pea uses original backward motion.</param>
        [TestCase(PlantKind.Peashooter, false)]
        [TestCase(PlantKind.SplitPea, true)]
        public void IgnitionRetainsMotionAndStartsANewFireAnimationClock(PlantKind kind, bool backward)
        {
            var game = Board();
            Torch(game, 2, 4);
            var shot = Fire(game, kind, 2, 2, backward);
            shot.X = 4.45f;
            shot.PresentationAge = 3f;
            shot.PresentationNativeAge = 3f;
            var x = shot.X;
            var launchY = shot.PresentationLaunchY;
            var vx = shot.VelocityX;
            var vy = shot.VelocityY;
            Assert.That(vx * 80f / 100f, Is.EqualTo(backward ? -3.33f : 3.33f).Within(.00001f));
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.True);
            Assert.That(shot.Damage, Is.EqualTo(40f));
            Assert.That(shot.Splash, Is.True);
            Assert.That(shot.X, Is.EqualTo(x));
            Assert.That(shot.PresentationLaunchY, Is.EqualTo(launchY));
            Assert.That(shot.VelocityX, Is.EqualTo(vx));
            Assert.That(shot.VelocityY, Is.EqualTo(vy));
            Assert.That(shot.Backward, Is.EqualTo(backward));
            Assert.That(shot.PresentationAge, Is.EqualTo(3f));
            Assert.That(shot.PresentationFireAge, Is.Zero);
            Assert.That(shot.PresentationFireRate, Is.InRange(50f, 80f));
            Assert.That(CountSound(game, 39), Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureProjectiles", .01f, false);
            Assert.That(shot.PresentationFireAge, Is.EqualTo(.01f).Within(.00001f));
            Assert.That(shot.X, Is.EqualTo(x + vx * .01f).Within(.00001f));
            Assert.That(shot.PresentationLaunchY, Is.EqualTo(launchY));
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(CountSound(game, 39), Is.EqualTo(1), "The existing fireball cannot be ignited again.");
            Assert.That(AdventureZombieFoleyData.Definitions[39].Samples, Is.EqualTo(new[] { "firepea" }));
            Assert.That(AdventureZombieFoleyData.Definitions[39].PitchRange, Is.EqualTo(10f));
        }

        /// <summary>Checks snow first becomes a normal pea, destroys its trail and needs a different tree to ignite.</summary>
        [Test]
        public void SnowPeaNeedsTwoDifferentTorchesAndPlaysEachOriginalConversionCueOnce()
        {
            var game = Board();
            Torch(game, 2, 4);
            Torch(game, 2, 5);
            var shot = Fire(game, PlantKind.SnowPea, 2, 2, false);
            shot.X = 4.45f;
            Invoke(game, "UpdateAdventureSnowTrail", shot);
            Assert.That(shot.IsSnow, Is.True);
            Assert.That(shot.PresentationSnowTrail, Is.Not.Null);
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.IsSnow, Is.False);
            Assert.That(shot.Fire, Is.False);
            Assert.That(shot.LastTorchColumn, Is.EqualTo(4));
            Assert.That(shot.PresentationSnowTrail, Is.Null);
            Assert.That(game.Presentation.SnowTrails, Is.Empty);
            Assert.That(CountSound(game, 3), Is.EqualTo(1));
            Assert.That(CountSound(game, 39), Is.Zero);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.False, "The same Torchwood must not thaw and ignite the same snow pea.");
            Assert.That(CountSound(game, 3), Is.EqualTo(1));
            shot.X = 5.45f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.True);
            Assert.That(shot.LastTorchColumn, Is.EqualTo(5));
            Assert.That(CountSound(game, 39), Is.EqualTo(1));
            Assert.That(shot.PresentationFireAge, Is.Zero);
        }

        /// <summary>Checks non-pea projectile types and another logical row never acquire a fire attachment.</summary>
        [Test]
        public void TorchAdmitsOnlyOriginalPeaTypesInItsLogicalRow()
        {
            var game = Board();
            var torch = Torch(game, 2, 4);
            foreach (var kind in new[] { PlantKind.Starfruit, PlantKind.PuffShroom, PlantKind.Cactus, PlantKind.Cattail, PlantKind.CabbagePult, PlantKind.KernelPult })
            {
                var shot = new Pea { SourceKind = kind, Row = 2, X = 4.45f };
                game.Peas.Add(shot);
                Assert.That(AdventureTorchwoodRules.Overlaps(torch, shot), Is.False, kind + " does not create an original normal/snow pea.");
            }

            var differentRow = new Pea { Row = 3, X = 4.45f };
            game.Peas.Add(differentRow);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(game.Peas.TrueForAll(shot => !shot.Fire), Is.True);
            Assert.That(CountSound(game, 39), Is.Zero);
        }

        /// <summary>Checks Torchwood leaves Threepeater's authored lane displacement and velocity intact.</summary>
        [Test]
        public void ThreepeaterKeepsItsLaneCurveWhenItsLogicalRowPassesTorchwood()
        {
            var game = Board();
            Torch(game, 3, 4);
            var plant = new Plant { Kind = PlantKind.Threepeater, Row = 2, Column = 2 };
            var shot = (Pea)Invoke(game, "FireAdventurePea", plant, 3, false, 20f)!;
            shot.X = 4.45f;
            shot.PresentationLaneOffset = 17f;
            var laneVelocity = shot.PresentationLaneVelocity;
            var launchY = shot.PresentationLaunchY;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.True);
            Assert.That(shot.Row, Is.EqualTo(3));
            Assert.That(shot.PresentationLaneOffset, Is.EqualTo(17f));
            Assert.That(shot.PresentationLaneVelocity, Is.EqualTo(laneVelocity));
            Assert.That(shot.PresentationLaunchY, Is.EqualTo(launchY));
            Invoke(game, "AdvanceAdventureProjectiles", .01f, false);
            Assert.That(shot.PresentationLaneOffset, Is.EqualTo(17f + laneVelocity));
            Assert.That(shot.PresentationLaneVelocity, Is.EqualTo(laneVelocity * .97f).Within(.00001f));
        }

        /// <summary>Checks the original thirty-pixel attack interval requires one pixel of overlap with the widened pea rectangle.</summary>
        [Test]
        public void TorchUsesOriginalHorizontalRectangleOverlapThreshold()
        {
            var game = Board();
            Torch(game, 2, 4);
            var shot = Fire(game, PlantKind.Peashooter, 2, 2, false);
            shot.X = (384f - 40f) / 80f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.False, "Native projectile right edge 410 only touches the Torchwood attack interval.");
            shot.X = (385f - 40f) / 80f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.True, "Native projectile right edge 411 overlaps by exactly one pixel.");
        }

        /// <summary>Checks the existing ordinary pea muzzle retains its launch height throughout roof flight and conversion.</summary>
        [Test]
        public void OrdinaryPeaAndFireAttachmentRetainOneLaunchHeightAboveRoofSlope()
        {
            var game = Board(41);
            var shot = Fire(game, PlantKind.Peashooter, 2, 1, false);
            var launchY = shot.PresentationLaunchY;
            for (var tick = 0; tick < 40; tick++)
            {
                Invoke(game, "AdvanceAdventureProjectiles", .01f, false);
            }

            var arguments = new object[] { shot, 0f, 0f };
            typeof(GameSimulation).GetMethod("AdventureIceProjectileTopLeft", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
            Assert.That((float)arguments[2], Is.EqualTo(launchY - 14f));
            Torch(game, 2, 3);
            var x = shot.X;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(shot.Fire, Is.True);
            Assert.That(shot.X, Is.EqualTo(x));
            Assert.That(shot.PresentationLaunchY, Is.EqualTo(launchY));
            Assert.That(shot.PresentationFireAge, Is.Zero);
        }

        /// <summary>Creates a playing production board with unrelated waves postponed.</summary>
        /// <param name="level">The original adventure board definition.</param>
        /// <returns>The deterministic production simulation.</returns>
        private static GameSimulation Board(int level = 11)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a real Torchwood through production initialization.</summary>
        /// <param name="game">The board receiving the plant.</param>
        /// <param name="row">The original lawn row.</param>
        /// <param name="column">The original lawn column.</param>
        /// <returns>The live production Torchwood.</returns>
        private static Plant Torch(GameSimulation game, int row, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Torchwood, row, column)!;
        }

        /// <summary>Creates a pea through the existing original-species firing path.</summary>
        /// <param name="game">The production board receiving the projectile.</param>
        /// <param name="kind">The original source plant.</param>
        /// <param name="row">The original lawn row.</param>
        /// <param name="column">The original lawn column.</param>
        /// <param name="backward">Whether the shot uses original backward motion.</param>
        /// <returns>The living projectile at its existing muzzle.</returns>
        private static Pea Fire(GameSimulation game, PlantKind kind, int row, int column, bool backward)
        {
            var plant = new Plant { Kind = kind, Row = row, Column = column };
            var shot = (Pea)Invoke(game, "FireAdventurePea", plant, row, backward, 20f)!;
            // AdvanceAdventureNativeTimedPlant assigns the element after the shared projectile allocation.
            shot.IsSnow = kind == PlantKind.SnowPea;
            return shot;
        }

        /// <summary>Counts original conversion Foley requests retained by the production presentation model.</summary>
        /// <param name="game">The production board retaining the requests.</param>
        /// <param name="foley">The original Foley category.</param>
        /// <returns>The number of original requests.</returns>
        private static int CountSound(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == foley).Count;
        }

        /// <summary>Invokes an existing production operation without adding test-only runtime APIs.</summary>
        /// <param name="game">The production board owning the operation.</param>
        /// <param name="name">The private production operation.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The original operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
