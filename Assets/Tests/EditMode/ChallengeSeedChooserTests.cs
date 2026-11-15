#nullable enable
using System.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 49A300/49B120/49AF50 seed restrictions and original confirmation order.</summary>
    public sealed class ChallengeSeedChooserTests
    {
        /// <summary>Checks Last Stand's complete five-seed ban and its Imitater target gate.</summary>
        /// <param name="kind">The original prohibited plant.</param>
        [TestCase(PlantKind.Sunflower)]
        [TestCase(PlantKind.SunShroom)]
        [TestCase(PlantKind.TwinSunflower)]
        [TestCase(PlantKind.PuffShroom)]
        [TestCase(PlantKind.SeaShroom)]
        public void LastStandKeepsDisabledCardsVisibleAndRejectsCopies(PlantKind kind)
        {
            var session = new ModeSession(31, AdventureSeedAccess.DevelopmentAll());
            Assert.That(session.Game.Adventure.AvailableSeeds, Has.Member(kind));
            Assert.That(ModeSeedChooserRules.IsDisabled(31, kind), Is.True);
            Assert.That(session.Game.SetModeImitaterTarget(kind), Is.False);
            Assert.That(ModeSeedChooserRules.IsDisabled(16, kind), Is.False);
        }

        /// <summary>Checks the native warning order and the original survival exemption from upgrade checks.</summary>
        [Test]
        public void WarningsKeepNativeOrderAndSurvivalExceptions()
        {
            var session = new ModeSession(22, AdventureSeedAccess.DevelopmentAll());
            var selection = new AdventureSeedChooserState(new[] { PlantKind.GatlingPea, PlantKind.WinterMelon });
            var warnings = ModeSeedChooserRules.Warnings(session, selection);
            Assert.That(warnings.Select(warning => warning.Key).Take(2), Is.EqualTo(new[] { "SEED_CHOOSER_SEEING_STARS_WARNING", "SEED_CHOOSER_SUN_WARNING" }));
            var upgrades = warnings.Where(warning => warning.Upgrade.HasValue).ToArray();
            Assert.That(upgrades.Select(warning => warning.Upgrade), Is.EqualTo(new PlantKind?[] { PlantKind.GatlingPea, PlantKind.WinterMelon }));
            Assert.That(upgrades.Select(warning => warning.Base), Is.EqualTo(new PlantKind?[] { PlantKind.Repeater, PlantKind.MelonPult }));
            var survival = new ModeSession(1, AdventureSeedAccess.DevelopmentAll()) { Stage = 2 };
            Assert.That(ModeSeedChooserRules.Warnings(survival, selection), Is.Empty);
        }

        /// <summary>Checks a settled Imitater counts as the original plant when evaluating warnings.</summary>
        [Test]
        public void ImitaterSatisfiesTheOriginalProducerWarning()
        {
            var session = new ModeSession(16, AdventureSeedAccess.DevelopmentAll(PlantKind.Sunflower));
            var selection = new AdventureSeedChooserState(new[] { PlantKind.Imitater });
            Assert.That(ModeSeedChooserRules.Warnings(session, selection).Any(warning => warning.Key == "SEED_CHOOSER_SUN_WARNING"), Is.False);
        }

        /// <summary>Checks retained purchased access keeps Last Stand exclusions and mandatory Starfruit available.</summary>
        [Test]
        public void ShopReturnRefreshPreservesNativeModeSeedRules()
        {
            var lastStand = new ModeSession(31);
            lastStand.RefreshChooserSeedAccess(AdventureSeedAccess.DevelopmentAll(), 10);
            Assert.That(lastStand.Game.Adventure.AvailableSeeds, Has.Member(PlantKind.TwinSunflower));
            Assert.That(lastStand.Game.SetModeImitaterTarget(PlantKind.PuffShroom), Is.False);
            var stars = new ModeSession(22, unlockedBasicSeeds: 20);
            stars.RefreshChooserSeedAccess(new AdventureSeedAccess(System.Array.Empty<PlantKind>()), 7);
            Assert.That(stars.Game.Adventure.AvailableSeeds, Has.Member(PlantKind.Starfruit));
        }
    }
}
