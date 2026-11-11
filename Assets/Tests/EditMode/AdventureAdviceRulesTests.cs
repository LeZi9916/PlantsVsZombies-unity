#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks that board advice selects original resource keys and omits invented biome lessons.</summary>
    public sealed class AdventureAdviceRulesTests
    {
        /// <summary>Checks original tutorial text selection as seed and shovel cursor state changes.</summary>
        /// <param name="tutorial">The introductory tutorial state.</param>
        /// <param name="holdingSeed">Whether the cursor holds a plant.</param>
        /// <param name="holdingShovel">Whether the cursor holds the shovel.</param>
        /// <param name="plants">The number of tutorial plants remaining.</param>
        /// <param name="expected">The exact original LawnStrings resource key.</param>
        [TestCase(AdventureTutorial.ChooseSeed, false, false, 3, "ADVICE_CLICK_SEED_PACKET")]
        [TestCase(AdventureTutorial.PlantPeashooter, false, false, 3, "ADVICE_CLICK_SEED_PACKET")]
        [TestCase(AdventureTutorial.PlantPeashooter, true, false, 3, "ADVICE_CLICK_ON_GRASS")]
        [TestCase(AdventureTutorial.CollectSun, false, false, 3, "ADVICE_CLICK_ON_SUN")]
        [TestCase(AdventureTutorial.PlantSunflower, false, false, 3, "ADVICE_PLANT_SUNFLOWER1")]
        [TestCase(AdventureTutorial.PlantSunflower, true, false, 3, "ADVICE_CLICK_ON_GRASS")]
        [TestCase(AdventureTutorial.RechargeSunflower, false, false, 3, "ADVICE_PLANT_SUNFLOWER2")]
        [TestCase(AdventureTutorial.Shovel, false, false, 3, "ADVICE_CLICK_SHOVEL")]
        [TestCase(AdventureTutorial.Shovel, false, true, 3, "ADVICE_CLICK_PLANT")]
        [TestCase(AdventureTutorial.Shovel, false, false, 2, "ADVICE_KEEP_DIGGING")]
        public void TutorialUsesOriginalAdvice(AdventureTutorial tutorial, bool holdingSeed, bool holdingShovel, int plants, string expected)
        {
            Assert.That(AdventureAdviceRules.TutorialKey(tutorial, holdingSeed, holdingShovel, plants), Is.EqualTo(expected));
        }

        /// <summary>Checks that nonoriginal biome and mode tutorial overlays supply no replacement prose.</summary>
        /// <param name="tutorial">The biome or mode formerly displaying a custom overlay.</param>
        [TestCase(AdventureTutorial.NightMushrooms)]
        [TestCase(AdventureTutorial.PoolSupport)]
        [TestCase(AdventureTutorial.FogVisibility)]
        [TestCase(AdventureTutorial.RoofPots)]
        [TestCase(AdventureTutorial.Conveyor)]
        [TestCase(AdventureTutorial.Bowling)]
        [TestCase(AdventureTutorial.Hammer)]
        [TestCase(AdventureTutorial.Vases)]
        [TestCase(AdventureTutorial.Boss)]
        [TestCase(AdventureTutorial.Complete)]
        public void NonOriginalTutorialHasNoOverlay(AdventureTutorial tutorial)
        {
            Assert.That(AdventureAdviceRules.TutorialKey(tutorial, false, false, 0), Is.Null);
        }

        /// <summary>Checks original terrain and special plant placement advice.</summary>
        /// <param name="failure">The rejected planting rule.</param>
        /// <param name="plant">The plant held by the cursor.</param>
        /// <param name="firstRoof">Whether the first rooftop lesson is active.</param>
        /// <param name="expected">The exact original resource key.</param>
        [TestCase(AdventurePlantingFailure.NeedsLilyPad, PlantKind.Peashooter, false, "ADVICE_PLANT_NOT_ON_WATER")]
        [TestCase(AdventurePlantingFailure.NeedsLilyPad, PlantKind.PotatoMine, false, "ADVICE_CANT_PLANT_THERE")]
        [TestCase(AdventurePlantingFailure.NeedsFlowerPot, PlantKind.Peashooter, true, "ADVICE_PLANT_NEED_POT1")]
        [TestCase(AdventurePlantingFailure.NeedsFlowerPot, PlantKind.Peashooter, false, "ADVICE_PLANT_NEED_POT2")]
        [TestCase(AdventurePlantingFailure.NeedsWater, PlantKind.LilyPad, false, "ADVICE_LILYPAD_ON_WATER")]
        [TestCase(AdventurePlantingFailure.NeedsWater, PlantKind.TangleKelp, false, "ADVICE_TANGLEKELP_ON_WATER")]
        [TestCase(AdventurePlantingFailure.NeedsWater, PlantKind.SeaShroom, false, "ADVICE_SEASHROOM_ON_WATER")]
        [TestCase(AdventurePlantingFailure.Grave, PlantKind.Peashooter, false, "ADVICE_PLANT_NOT_ON_GRAVE")]
        [TestCase(AdventurePlantingFailure.Grave, PlantKind.GraveBuster, false, "ADVICE_GRAVEBUSTERS_ON_GRAVES")]
        [TestCase(AdventurePlantingFailure.Sun, PlantKind.Peashooter, false, "ADVICE_CANT_AFFORD_PLANT")]
        [TestCase(AdventurePlantingFailure.Cooldown, PlantKind.Peashooter, false, "ADVICE_SEED_REFRESH")]
        [TestCase(AdventurePlantingFailure.BowlingLine, PlantKind.Wallnut, false, "ADVICE_NOT_PASSED_LINE")]
        public void PlantingUsesOriginalAdvice(AdventurePlantingFailure failure, PlantKind plant, bool firstRoof, string expected)
        {
            Assert.That(AdventureAdviceRules.PlantingKey(failure, plant, firstRoof), Is.EqualTo(expected));
        }

        /// <summary>Checks the original prerequisite advice for each upgrade plant.</summary>
        /// <param name="plant">The upgrade being placed.</param>
        /// <param name="expected">The original prerequisite resource key.</param>
        [TestCase(PlantKind.GatlingPea, "ADVICE_ONLY_ON_REPEATERS")]
        [TestCase(PlantKind.WinterMelon, "ADVICE_ONLY_ON_MELONPULT")]
        [TestCase(PlantKind.TwinSunflower, "ADVICE_ONLY_ON_SUNFLOWER")]
        [TestCase(PlantKind.Spikerock, "ADVICE_ONLY_ON_SPIKEWEED")]
        [TestCase(PlantKind.CobCannon, "ADVICE_ONLY_ON_KERNELPULT")]
        [TestCase(PlantKind.GoldMagnet, "ADVICE_ONLY_ON_MAGNETSHROOM")]
        [TestCase(PlantKind.GloomShroom, "ADVICE_ONLY_ON_FUMESHROOM")]
        [TestCase(PlantKind.Cattail, "ADVICE_ONLY_ON_LILYPAD")]
        public void UpgradeUsesOriginalPrerequisite(PlantKind plant, string expected)
        {
            Assert.That(AdventureAdviceRules.PlantingKey(AdventurePlantingFailure.NeedsUpgrade, plant, false), Is.EqualTo(expected));
        }
    }
}
