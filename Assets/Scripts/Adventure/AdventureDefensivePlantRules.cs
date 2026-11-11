#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves the original defensive plant damage artwork thresholds.</summary>
    public static class AdventureDefensivePlantRules
    {
        /// <summary>Gets the original visible damage stage for a defensive plant.</summary>
        /// <param name="kind">The defensive plant species.</param>
        /// <param name="health">The remaining plant health.</param>
        /// <returns>Zero for intact or unsupported plants, one for damaged, or two for heavily damaged.</returns>
        public static int DamageStage(PlantKind kind, float health)
        {
            if (kind != PlantKind.Wallnut && kind != PlantKind.TallNut && kind != PlantKind.Pumpkin)
            {
                return 0;
            }

            // PC 0x4728D0 and 0x472BB0 use strict comparisons after integer division.
            var maximum = kind == PlantKind.TallNut ? 8000 : 4000;
            return health < maximum / 3 ? 2 : health < maximum * 2 / 3 ? 1 : 0;
        }

        /// <summary>Gets the only animation track replaced by the original damage presentation.</summary>
        /// <param name="kind">The defensive plant species.</param>
        /// <returns>The body or front track name, or null for unsupported plants.</returns>
        public static string? DamageTrack(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Wallnut:
                    return "anim_face";
                case PlantKind.TallNut:
                    return "anim_idle";
                case PlantKind.Pumpkin:
                    return "Pumpkin_front";
                default:
                    return null;
            }
        }

        /// <summary>Gets the original replacement image for the plant's current damage stage.</summary>
        /// <param name="kind">The defensive plant species.</param>
        /// <param name="health">The remaining plant health.</param>
        /// <returns>The original image identifier, or null while the original undamaged art should be used.</returns>
        public static string? DamageImage(PlantKind kind, float health)
        {
            var stage = DamageStage(kind, health);
            if (stage == 0)
            {
                return null;
            }

            switch (kind)
            {
                case PlantKind.Wallnut:
                    return stage == 1 ? "IMAGE_REANIM_WALLNUT_CRACKED1" : "IMAGE_REANIM_WALLNUT_CRACKED2";
                case PlantKind.TallNut:
                    return stage == 1 ? "IMAGE_REANIM_TALLNUT_CRACKED1" : "IMAGE_REANIM_TALLNUT_CRACKED2";
                case PlantKind.Pumpkin:
                    return stage == 1 ? "IMAGE_REANIM_PUMPKIN_DAMAGE1" : "IMAGE_REANIM_PUMPKIN_DAMAGE3";
                default:
                    return null;
            }
        }
    }
}
