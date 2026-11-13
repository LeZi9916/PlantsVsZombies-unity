#nullable enable
using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares the original PC ice vehicle damage artwork and projectile collision intervals.</summary>
    public static class AdventureZomboniRules
    {
        /// <summary>Resolves the two original damaged body images at strict integer-third thresholds.</summary>
        /// <param name="zombie">The vehicle whose body health and puncture state select its artwork.</param>
        /// <param name="track">The original body track being drawn.</param>
        /// <returns>The original damage image identifier, or null for unchanged artwork.</returns>
        public static string? DamageImage(Zombie zombie, string track)
        {
            var stage = zombie.ZomboniState?.FlatTires == true ? 2
                : AdventureZombieEquipmentRules.DamageIndex(zombie.Health, (int)zombie.StartingHealth);
            if (stage == 0)
            {
                return null;
            }

            if (string.Equals(track, "Zombie_zamboni_1", StringComparison.OrdinalIgnoreCase))
            {
                return stage == 1 ? "IMAGE_REANIM_ZOMBIE_ZAMBONI_1_DAMAGE1" : "IMAGE_REANIM_ZOMBIE_ZAMBONI_1_DAMAGE2";
            }

            if (string.Equals(track, "Zombie_zamboni_2", StringComparison.OrdinalIgnoreCase))
            {
                return stage == 1 ? "IMAGE_REANIM_ZOMBIE_ZAMBONI_2_DAMAGE1" : "IMAGE_REANIM_ZOMBIE_ZAMBONI_2_DAMAGE2";
            }

            return null;
        }

        /// <summary>Checks the PC projectile interval against the original full-sized 153-pixel vehicle body.</summary>
        /// <param name="projectile">The projectile at the end of this native update.</param>
        /// <param name="zombie">The ice vehicle with its unchanged native logical body rectangle.</param>
        /// <returns>Whether the original horizontal projectile and vehicle rectangles touch or overlap.</returns>
        public static bool ProjectileCollides(Pea projectile, Zombie zombie)
        {
            // PC EffectedByDamage rejects ZombieDying/Burned/Mowed before collision selection.
            if (zombie.ZomboniState?.FlatTires == true
                || zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3)
            {
                return false;
            }

            // PC Projectile::GetProjectileRect 0x47DFF0: image size never replaces the 40-pixel logical width.
            var x = (int)(40f + projectile.X * 80f - 16f);
            var left = x;
            var width = 40;
            if (projectile.Fire)
            {
                width = 30;
            }
            else if (projectile.SourceKind == PlantKind.Cactus || projectile.SourceKind == PlantKind.Cattail)
            {
                left -= 25;
                width += 25;
            }
            else if (projectile.Lobbed && projectile.Splash)
            {
                left += 20;
                width = 60;
            }
            else if (!projectile.Lobbed && (projectile.SourceKind == PlantKind.Peashooter
                || projectile.SourceKind == PlantKind.SnowPea || projectile.SourceKind == PlantKind.Repeater
                || projectile.SourceKind == PlantKind.Threepeater || projectile.SourceKind == PlantKind.SplitPea
                || projectile.SourceKind == PlantKind.GatlingPea))
            {
                left -= 15;
                width += 15;
            }

            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return left + width >= body.X && left <= body.X + body.Width;
        }
    }
}
