#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Selects original LawnStrings advice without supplying replacement prose.</summary>
    public static class AdventureAdviceRules
    {
        /// <summary>Selects advice for the original introductory planting and shovel states.</summary>
        /// <param name="tutorial">The active introductory lesson.</param>
        /// <param name="holdingSeed">Whether a plant packet is in the cursor.</param>
        /// <param name="holdingShovel">Whether the shovel is in the cursor.</param>
        /// <param name="shovelPlants">The number of tutorial plants still on the lawn.</param>
        /// <returns>An original resource key, or null when the original has no matching lesson.</returns>
        public static string? TutorialKey(AdventureTutorial tutorial, bool holdingSeed, bool holdingShovel, int shovelPlants)
        {
            switch (tutorial)
            {
                case AdventureTutorial.ChooseSeed:
                    return "ADVICE_CLICK_SEED_PACKET";
                case AdventureTutorial.PlantPeashooter:
                    return holdingSeed ? "ADVICE_CLICK_ON_GRASS" : "ADVICE_CLICK_SEED_PACKET";
                case AdventureTutorial.CollectSun:
                    return "ADVICE_CLICK_ON_SUN";
                case AdventureTutorial.PlantSunflower:
                    return holdingSeed ? "ADVICE_CLICK_ON_GRASS" : "ADVICE_PLANT_SUNFLOWER1";
                case AdventureTutorial.RechargeSunflower:
                    return "ADVICE_PLANT_SUNFLOWER2";
                case AdventureTutorial.Shovel:
                    return shovelPlants < 3 ? "ADVICE_KEEP_DIGGING" : holdingShovel ? "ADVICE_CLICK_PLANT" : "ADVICE_CLICK_SHOVEL";
                default:
                    return null;
            }
        }

        /// <summary>Selects the advice associated with a failed original planting rule.</summary>
        /// <param name="failure">The rule that rejected the planting operation.</param>
        /// <param name="kind">The resolved plant held by the cursor.</param>
        /// <param name="firstRoofLevel">Whether this is the first adventure rooftop lesson.</param>
        /// <returns>The original advice resource key, or null for failures without a board message.</returns>
        public static string? PlantingKey(AdventurePlantingFailure failure, PlantKind kind, bool firstRoofLevel)
        {
            switch (failure)
            {
                case AdventurePlantingFailure.NeedsLilyPad:
                    return kind == PlantKind.PotatoMine ? "ADVICE_CANT_PLANT_THERE" : "ADVICE_PLANT_NOT_ON_WATER";
                case AdventurePlantingFailure.PotatoMineOnLilyPad:
                    return "ADVICE_POTATO_MINE_ON_LILY";
                case AdventurePlantingFailure.NeedsFlowerPot:
                    return firstRoofLevel ? "ADVICE_PLANT_NEED_POT1" : "ADVICE_PLANT_NEED_POT2";
                case AdventurePlantingFailure.NeedsWater:
                    return kind == PlantKind.LilyPad ? "ADVICE_LILYPAD_ON_WATER" : kind == PlantKind.TangleKelp ? "ADVICE_TANGLEKELP_ON_WATER" : "ADVICE_SEASHROOM_ON_WATER";
                case AdventurePlantingFailure.NeedsSleepingMushroom:
                    return "ADVICE_PLANTING_NEED_SLEEPING";
                case AdventurePlantingFailure.Grave:
                    return kind == PlantKind.GraveBuster ? "ADVICE_GRAVEBUSTERS_ON_GRAVES" : "ADVICE_PLANT_NOT_ON_GRAVE";
                case AdventurePlantingFailure.Crater:
                    return "ADVICE_PLANT_NOT_ON_CRATER";
                case AdventurePlantingFailure.RoofRestriction:
                    return "ADVICE_PLANTING_NEEDS_GROUND";
                case AdventurePlantingFailure.Sun:
                    return "ADVICE_CANT_AFFORD_PLANT";
                case AdventurePlantingFailure.Cooldown:
                    return "ADVICE_SEED_REFRESH";
                case AdventurePlantingFailure.BowlingLine:
                    return "ADVICE_NOT_PASSED_LINE";
                case AdventurePlantingFailure.NeedsUpgrade:
                    return UpgradeKey(kind);
                default:
                    return null;
            }
        }

        /// <summary>Selects the original upgrade prerequisite message for a plant.</summary>
        /// <param name="kind">The upgrade being placed.</param>
        /// <returns>The corresponding original prerequisite resource key.</returns>
        private static string? UpgradeKey(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.GatlingPea:
                    return "ADVICE_ONLY_ON_REPEATERS";
                case PlantKind.WinterMelon:
                    return "ADVICE_ONLY_ON_MELONPULT";
                case PlantKind.TwinSunflower:
                    return "ADVICE_ONLY_ON_SUNFLOWER";
                case PlantKind.Spikerock:
                    return "ADVICE_ONLY_ON_SPIKEWEED";
                case PlantKind.CobCannon:
                    return "ADVICE_ONLY_ON_KERNELPULT";
                case PlantKind.GoldMagnet:
                    return "ADVICE_ONLY_ON_MAGNETSHROOM";
                case PlantKind.GloomShroom:
                    return "ADVICE_ONLY_ON_FUMESHROOM";
                case PlantKind.Cattail:
                    return "ADVICE_ONLY_ON_LILYPAD";
                default:
                    return null;
            }
        }
    }
}
