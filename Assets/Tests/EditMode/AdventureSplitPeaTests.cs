#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original Split Pea's shared animated muzzle anchor and rear projectile origin.</summary>
    public sealed class AdventureSplitPeaTests
    {
        /// <summary>Checks PC Fire's forward and backward origins against original body-track sample positions.</summary>
        /// <param name="age">The body animation age at twenty native frames per second.</param>
        /// <param name="headX">The original body-track horizontal translation after integer truncation.</param>
        /// <param name="headY">The original body-track vertical translation after integer truncation.</param>
        [TestCase(0f, 37, 48)]
        [TestCase(.25f, 39, 43)]
        [TestCase(.3125f, 40, 42)]
        [TestCase(.625f, 47, 46)]
        [TestCase(1.25f, 37, 48)]
        public void BothMuzzlesUseOriginalAnimatedBodyAnchor(float age, int headX, int headY)
        {
            var game = Board(1);
            var plant = new Plant { Kind = PlantKind.SplitPea, Row = 2, Column = 3, Age = age, PresentationRate = 20f };
            var forward = Fire(game, plant, false);
            var backward = Fire(game, plant, true);
            // PC 4752E0 creates image top-left at anchor+(24,-33) or anchor+(-64,-33).
            // ProjectilePea.png is 28x28; simulation coordinates locate the image center.
            Assert.That(40f + forward.X * 80f, Is.EqualTo(280f + headX + 24f + 14f).Within(.0001f));
            Assert.That(40f + backward.X * 80f, Is.EqualTo(280f + headX - 64f + 14f).Within(.0001f));
            Assert.That((forward.X - backward.X) * 80f, Is.EqualTo(88f).Within(.0001f));
            Assert.That(forward.PresentationLaunchY, Is.EqualTo(280f + headY - 33f + 14f));
            Assert.That(backward.PresentationLaunchY, Is.EqualTo(forward.PresentationLaunchY));
            Assert.That(forward.PresentationPreviousX, Is.EqualTo(forward.X));
            Assert.That(backward.PresentationPreviousX, Is.EqualTo(backward.X));
            Assert.That(forward.StartX, Is.EqualTo(forward.X));
            Assert.That(backward.StartX, Is.EqualTo(backward.X));
            Assert.That(forward.Backward, Is.False);
            Assert.That(backward.Backward, Is.True);
        }

        /// <summary>Checks launch Y uses the plant cell and original five-pixel pot correction on every terrain.</summary>
        /// <param name="level">The original adventure level selecting lawn, pool, or roof terrain.</param>
        /// <param name="flowerPot">Whether the shooter occupies a flower pot.</param>
        /// <param name="centerY">The expected native projectile image center at the first body frame.</param>
        [TestCase(1, false, 309f)]
        [TestCase(21, false, 279f)]
        [TestCase(41, false, 309f)]
        [TestCase(41, true, 304f)]
        public void LaunchHeightRetainsOriginalTerrainAndPotCorrection(int level, bool flowerPot, float centerY)
        {
            var game = Board(level);
            var plant = new Plant { Kind = PlantKind.SplitPea, Row = 2, Column = 3, PresentationRate = 20f };
            game.Plants.Clear();
            if (flowerPot)
            {
                game.Plants.Add(new Plant { Kind = PlantKind.FlowerPot, Row = 2, Column = 3 });
            }

            var forward = Fire(game, plant, false);
            var backward = Fire(game, plant, true);
            Assert.That(forward.PresentationLaunchY, Is.EqualTo(centerY));
            Assert.That(backward.PresentationLaunchY, Is.EqualTo(centerY));
        }

        /// <summary>Creates a quiet adventure board with the requested original terrain.</summary>
        /// <param name="level">The original adventure level providing board geometry.</param>
        /// <returns>The isolated simulation with no scheduled zombie waves.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Fires a projectile through the production multi-head launch hook.</summary>
        /// <param name="game">The board that owns the projectile.</param>
        /// <param name="plant">The shooter whose body clock selects the native muzzle anchor.</param>
        /// <param name="backward">Whether to fire the rear head's secondary weapon.</param>
        /// <returns>The newly created original pea.</returns>
        private static Pea Fire(GameSimulation game, Plant plant, bool backward)
        {
            return (Pea)typeof(GameSimulation).GetMethod("FireAdventurePea", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { plant, plant.Row, backward, 20f })!;
        }
    }
}
