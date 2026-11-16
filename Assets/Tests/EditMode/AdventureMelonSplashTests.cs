#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 47DFF0/47C6A0 melon splash rectangles through the production projectile impact path.</summary>
    public sealed class AdventureMelonSplashTests
    {
        /// <summary>Checks touching edges, one-pixel gaps and wide bodies against the original projectile rectangle.</summary>
        /// <param name="source">The original ordinary or winter melon source.</param>
        [TestCase(PlantKind.MelonPult)]
        [TestCase(PlantKind.WinterMelon)]
        public void SplashUsesInclusiveProjectileAndBodyOverlap(PlantKind source)
        {
            // Projectile mX400 gives [420,480], independently of the primary victim's position.
            foreach (var sample in new[]
            {
                (ZombieKind.Normal, 341, false),
                (ZombieKind.Normal, 342, true),
                (ZombieKind.Normal, 343, true),
                (ZombieKind.Normal, 443, true),
                (ZombieKind.Normal, 444, true),
                (ZombieKind.Normal, 445, false),
                (ZombieKind.Zomboni, 267, true),
                (ZombieKind.Catapult, 267, true),
                (ZombieKind.Gargantuar, 312, true),
                (ZombieKind.Normal, 464, false)
            })
            {
                var game = Board();
                var primary = Target(game, 1, ZombieKind.Normal, 1, 400);
                var secondary = Target(game, 2, sample.Item1, 2, sample.Item2);
                Impact(game, source);
                Assert.That(primary.Health, Is.EqualTo(2920f));
                Assert.That(secondary.Health, Is.EqualTo(sample.Item3 ? 2974f : 3000f),
                    source + "/" + sample + ": PC splash admits body contact, including exact touching edges.");
                if (secondary.Kind == ZombieKind.Normal)
                {
                    Assert.That(secondary.ChillRemaining > 0f, Is.EqualTo(sample.Item3 && source == PlantKind.WinterMelon));
                }
            }
        }

        /// <summary>Checks row selection uses the projectile's native row and admits only the same or adjacent rows.</summary>
        /// <param name="source">The original ordinary or winter melon source.</param>
        [TestCase(PlantKind.MelonPult)]
        [TestCase(PlantKind.WinterMelon)]
        public void SplashUsesOriginalAdjacentRows(PlantKind source)
        {
            for (var row = 0; row < 5; row++)
            {
                var game = Board();
                Target(game, 1, ZombieKind.Normal, 1, 400);
                var secondary = Target(game, 2, ZombieKind.Normal, row, 400);
                Impact(game, source);
                Assert.That(secondary.Health, Is.EqualTo(row <= 2 ? 2974f : 3000f));
                Assert.That(secondary.ChillRemaining > 0f, Is.EqualTo(row <= 2 && source == PlantKind.WinterMelon));
            }
        }

        /// <summary>Checks original damage-range admission occurs before splash damage and winter chilling.</summary>
        /// <param name="source">The original ordinary or winter melon source.</param>
        /// <param name="phase">The bungee phase at the same horizontal body position.</param>
        /// <param name="admitted">Whether PC EffectedByDamage admits that phase.</param>
        [TestCase(PlantKind.MelonPult, 4, false)]
        [TestCase(PlantKind.MelonPult, 6, true)]
        [TestCase(PlantKind.MelonPult, 8, false)]
        [TestCase(PlantKind.WinterMelon, 4, false)]
        [TestCase(PlantKind.WinterMelon, 6, true)]
        [TestCase(PlantKind.WinterMelon, 8, false)]
        public void SplashRespectsOriginalBungeePhaseAdmission(PlantKind source, int phase, bool admitted)
        {
            var game = Board();
            Target(game, 1, ZombieKind.Normal, 1, 400);
            var secondary = Target(game, 2, ZombieKind.Bungee, 2, 400);
            secondary.PresentationNativePhase = phase;
            Impact(game, source);
            Assert.That(secondary.Health, Is.EqualTo(admitted ? 2974f : 3000f));
            Assert.That(secondary.ChillRemaining > 0f, Is.EqualTo(admitted && source == PlantKind.WinterMelon));
        }

        /// <summary>Creates a quiet board without imminent automatic waves.</summary>
        /// <returns>The initialized production simulation.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Adds a live fixture at an exact original actor origin.</summary>
        /// <param name="game">The board receiving the fixture.</param>
        /// <param name="id">The per-board actor identifier.</param>
        /// <param name="kind">The original body species.</param>
        /// <param name="row">The native row.</param>
        /// <param name="nativeX">The integer actor origin before species-specific body offsets.</param>
        /// <returns>The live fixture.</returns>
        private static Zombie Target(GameSimulation game, int id, ZombieKind kind, int row, int nativeX)
        {
            var zombie = new Zombie { Id = id, Kind = kind, Row = row,
                X = AdventureZombieNativeRules.GridX(nativeX + .25f), Health = 3000f, StartingHealth = 3000f };
            SimulationFixture.Zombies(game).Add(zombie);
            return zombie;
        }

        /// <summary>Runs a real landing impact at preceding integer projectile X400.</summary>
        /// <param name="game">The board containing the collision candidates.</param>
        /// <param name="source">The original melon source selecting splash and cold flags.</param>
        private static void Impact(GameSimulation game, PlantKind source)
        {
            var winter = source == PlantKind.WinterMelon;
            SimulationFixture.Peas(game).Add(new Pea
            {
                SourceKind = source, Row = 1, FlightRow = 1f, X = 4.5f, VelocityX = 0f,
                Lobbed = true, Splash = true, ChillSplash = winter, IsSnow = winter, Damage = 80f,
                PresentationInitialized = true, PresentationMotionInitialized = true,
                PresentationLobNativeX = 400f, PresentationLobNativeY = 200f, PresentationLaunchY = 200f
            });
            SimulationInspection.GetMethod("AdvanceAdventureProjectiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { .01f, true });
            Assert.That(game.Peas, Is.Empty);
        }
    }
}
