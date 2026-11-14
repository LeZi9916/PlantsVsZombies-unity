#nullable enable
using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure presentation rules.</summary>
    public static class AdventurePresentationRules
    {
        /// <summary>Gets the original PC PlantDrawHeightOffset water displacement at 0x473490.</summary>
        /// <param name="kind">The plant species, including the flying coffee exception.</param>
        /// <param name="row">The native board row selecting the alternating water phase.</param>
        /// <param name="column">The native board column selecting the quarter-pi phase offset.</param>
        /// <param name="inPool">Whether the plant occupies an original pool square.</param>
        /// <param name="ticks">The board's main update counter, independent of the pool-effect clock.</param>
        /// <returns>The original two-pixel sinusoidal displacement, or zero outside water and for flying coffee.</returns>
        internal static float WaterBobbingOffset(PlantKind kind, int row, int column, bool inPool, int ticks)
        {
            return inPool && kind != PlantKind.CoffeeBean
                ? (float)Math.Sin(row * Math.PI + column * Math.PI * .25f + ticks * Math.PI * 2f / 200f) * 2f
                : 0f;
        }

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

        /// <summary>Gets the original full body shooting clip length used before a lobber reloads into idle.</summary>
        /// <param name="kind">The original plant species.</param>
        /// <returns>Thirty-four frames for corn, thirty-eight for cabbage/melons, or zero for another plant.</returns>
        internal static int LobberShootingFrames(PlantKind kind)
        {
            return kind == PlantKind.KernelPult ? 34
                : kind == PlantKind.CabbagePult || kind == PlantKind.MelonPult || kind == PlantKind.WinterMelon ? 38 : 0;
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
