#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure presentation rules.</summary>
    public static class AdventurePresentationRules
    {
        // PC 0x46D240, 0x472C70. Counts are native logic updates, not guessed reanim frames.
        /// <summary>Performs shooting rate.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting shooting rate.</returns>
        public static float ShootingRate(PlantKind kind)
        {
            if (kind == PlantKind.Repeater || kind == PlantKind.SplitPea)
            {
                return 45f;
            }

            if (kind == PlantKind.GatlingPea)
            {
                return 38f;
            }

            if (kind == PlantKind.Threepeater)
            {
                return 20f;
            }

            if (kind == PlantKind.GloomShroom)
            {
                return 14f;
            }

            if (kind == PlantKind.Starfruit)
            {
                return 28f;
            }

            if (kind == PlantKind.Cattail)
            {
                return 30f;
            }

            return 35f;
        }

        /// <summary>Performs launch age.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting launch age.</returns>
        public static float LaunchAge(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Peashooter:
                case PlantKind.SnowPea:
                case PlantKind.Cactus:
                case PlantKind.Threepeater:
                    return .34f;
                case PlantKind.Repeater:
                case PlantKind.SplitPea:
                    return .25f;
                case PlantKind.CabbagePult:
                    return .31f;
                case PlantKind.KernelPult:
                    return .29f;
                case PlantKind.MelonPult:
                case PlantKind.WinterMelon:
                    return .35f;
                case PlantKind.FumeShroom:
                    return .49f;
                case PlantKind.Starfruit:
                    return .39f;
                case PlantKind.ScaredyShroom:
                    return .24f;
                case PlantKind.GatlingPea:
                    return .31f;
                case PlantKind.Cattail:
                    return .30f;
                default:
                    return .28f;
            }
        }

        /// <summary>Performs attached head.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting attached head.</returns>
        public static bool AttachedHead(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater || kind == PlantKind.GatlingPea || kind == PlantKind.Threepeater || kind == PlantKind.SplitPea;
        }
    }
}
