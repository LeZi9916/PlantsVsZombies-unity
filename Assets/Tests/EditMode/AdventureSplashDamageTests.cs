#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 47C780's full primary damage, integer thirds, crowd thresholds and one-point floor.</summary>
    public sealed class AdventureSplashDamageTests
    {
        /// <summary>Checks independent native integer results through actual projectile collision and damage.</summary>
        /// <param name="source">The original fireball or melon source.</param>
        /// <param name="count">The number of eligible secondary targets, excluding the direct victim.</param>
        /// <param name="expected">The original integer damage per secondary.</param>
        [TestCase(PlantKind.Peashooter, 0, 13)]
        [TestCase(PlantKind.Peashooter, 1, 13)]
        [TestCase(PlantKind.Peashooter, 3, 13)]
        [TestCase(PlantKind.Peashooter, 4, 10)]
        [TestCase(PlantKind.Peashooter, 100, 1)]
        [TestCase(PlantKind.MelonPult, 1, 26)]
        [TestCase(PlantKind.MelonPult, 21, 26)]
        [TestCase(PlantKind.MelonPult, 22, 26)]
        [TestCase(PlantKind.MelonPult, 23, 24)]
        [TestCase(PlantKind.MelonPult, 1000, 1)]
        [TestCase(PlantKind.WinterMelon, 1, 26)]
        [TestCase(PlantKind.WinterMelon, 21, 26)]
        [TestCase(PlantKind.WinterMelon, 22, 26)]
        [TestCase(PlantKind.WinterMelon, 23, 24)]
        [TestCase(PlantKind.WinterMelon, 1000, 1)]
        public void CrowdDamageMatchesOriginalIntegerThresholds(PlantKind source, int count, int expected)
        {
            var game = Board();
            var primary = Add(game, 1, ZombieKind.Normal);
            for (var index = 0; index < count; index++)
            {
                Add(game, index + 2, ZombieKind.Normal);
            }
            Impact(game, source);
            Assert.That(primary.Health, Is.EqualTo(source == PlantKind.Peashooter ? 2960f : 2920f));
            for (var index = 1; index < game.Zombies.Count; index++)
            {
                Assert.That(game.Zombies[index].Health, Is.EqualTo(3000f - expected), "Secondary " + index);
                Assert.That(game.Zombies[index].ChillRemaining > 0f, Is.EqualTo(source == PlantKind.WinterMelon));
            }
        }

        /// <summary>Checks PC 47C5E0 suppresses all secondary fire damage when the direct victim blocks splash.</summary>
        /// <param name="kind">The original vehicle or held-shield direct victim.</param>
        [TestCase(ZombieKind.Zomboni)]
        [TestCase(ZombieKind.Catapult)]
        [TestCase(ZombieKind.ScreenDoor)]
        [TestCase(ZombieKind.Ladder)]
        public void FireBlockedPrimaryReceivesOnlyItsDirectHit(ZombieKind kind)
        {
            var game = Board();
            var primary = Add(game, 1, kind);
            primary.ShieldHealth = kind == ZombieKind.ScreenDoor ? 1100f : kind == ZombieKind.Ladder ? 500f : 0f;
            var shield = primary.ShieldHealth;
            var secondary = Add(game, 2, ZombieKind.Normal);
            Impact(game, PlantKind.Peashooter);
            Assert.That(primary.Health, Is.EqualTo(shield > 0f ? 3000f : 2960f));
            Assert.That(primary.ShieldHealth, Is.EqualTo(shield > 0f ? shield - 40f : 0f));
            Assert.That(secondary.Health, Is.EqualTo(3000f));
        }

        /// <summary>Checks ineligible bodies neither take fire splash damage nor reduce the three admitted secondary hits.</summary>
        [Test]
        public void FireCrowdCountUsesTheSameNativeTargetGateAsDamage()
        {
            var game = Board();
            Add(game, 1, ZombieKind.Normal);
            for (var id = 2; id <= 4; id++)
            {
                Add(game, id, ZombieKind.Normal);
            }
            // Native fire splash is [458,558] here. These bodies touch its left and right edges exactly.
            game.Zombies[2].X = AdventureZombieNativeRules.GridX(380.25f);
            game.Zombies[3].X = AdventureZombieNativeRules.GridX(522.25f);
            var vehicle = Add(game, 5, ZombieKind.Zomboni);
            var shield = Add(game, 6, ZombieKind.ScreenDoor);
            shield.ShieldHealth = 1100f;
            var bungee = Add(game, 7, ZombieKind.Bungee);
            bungee.PresentationNativePhase = 4;
            var otherRow = Add(game, 8, ZombieKind.Normal);
            otherRow.Row = 2;
            var distant = Add(game, 9, ZombieKind.Normal);
            distant.X += 3f;
            var leftGap = Add(game, 10, ZombieKind.Normal);
            leftGap.X = AdventureZombieNativeRules.GridX(379.25f);
            var rightGap = Add(game, 11, ZombieKind.Normal);
            rightGap.X = AdventureZombieNativeRules.GridX(523.25f);
            Impact(game, PlantKind.Peashooter);
            for (var index = 1; index <= 3; index++)
            {
                Assert.That(game.Zombies[index].Health, Is.EqualTo(2987f));
            }
            foreach (var target in new[] { vehicle, shield, bungee, otherRow, distant, leftGap, rightGap })
            {
                Assert.That(target.Health, Is.EqualTo(3000f));
            }
            Assert.That(shield.ShieldHealth, Is.EqualTo(1100f));
        }

        /// <summary>Checks PC 47C620's splash bit damages the shield and sends the full hit to the body independently.</summary>
        /// <param name="source">The original fireball or melon source.</param>
        [TestCase(PlantKind.Peashooter)]
        [TestCase(PlantKind.MelonPult)]
        [TestCase(PlantKind.WinterMelon)]
        public void NativeSplashFlagsDamageBothPaperAndBody(PlantKind source)
        {
            var game = Board();
            var primary = Add(game, 1, ZombieKind.Newspaper);
            var secondary = Add(game, 2, ZombieKind.Newspaper);
            primary.ShieldHealth = secondary.ShieldHealth = 150f;
            Impact(game, source);
            var damage = source == PlantKind.Peashooter ? 40f : 80f;
            var splash = source == PlantKind.Peashooter ? 13f : 26f;
            Assert.That(primary.Health, Is.EqualTo(3000f - damage));
            Assert.That(primary.ShieldHealth, Is.EqualTo(150f - damage));
            Assert.That(secondary.Health, Is.EqualTo(3000f - splash));
            Assert.That(secondary.ShieldHealth, Is.EqualTo(150f - splash));
        }

        /// <summary>Creates an isolated playing board.</summary>
        /// <returns>The production board with scheduled waves held.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Adds an actor with enough body health to observe exact damage without death side effects.</summary>
        /// <param name="game">The owning board.</param>
        /// <param name="id">The unique fixture identifier.</param>
        /// <param name="kind">The original body and shield family.</param>
        /// <returns>The live production target.</returns>
        private static Zombie Add(GameSimulation game, int id, ZombieKind kind)
        {
            var zombie = new Zombie { Id = id, Kind = kind, Row = 1,
                X = AdventureZombieNativeRules.GridX(400.25f), Health = 3000f, StartingHealth = 3000f };
            SimulationFixture.Zombies(game).Add(zombie);
            return zombie;
        }

        /// <summary>Resolves a real fireball or melon impact against the controlled crowd.</summary>
        /// <param name="game">The board containing the native collision candidates.</param>
        /// <param name="source">The plant selecting the original projectile type.</param>
        private static void Impact(GameSimulation game, PlantKind source)
        {
            var fire = source == PlantKind.Peashooter;
            var winter = source == PlantKind.WinterMelon;
            SimulationFixture.Peas(game).Add(new Pea
            {
                SourceKind = source, Row = 1, FlightRow = 1f, X = fire ? AdventureZombieNativeRules.GridX(400.25f) : 4.5f,
                VelocityX = 0f, Fire = fire, Lobbed = !fire, Splash = true, IsSnow = winter, ChillSplash = winter,
                PiercesShield = !fire, Damage = fire ? 40f : 80f,
                PresentationInitialized = true, PresentationMotionInitialized = true,
                PresentationLobNativeX = 400f, PresentationLobNativeY = 200f, PresentationLaunchY = 200f
            });
            SimulationInspection.GetMethod("AdvanceAdventureProjectiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { .01f, true });
            Assert.That(game.Peas, Is.Empty);
        }
    }
}
