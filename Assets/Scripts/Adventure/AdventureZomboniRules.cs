#nullable enable
using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares the original PC ice vehicle damage artwork and both vehicles' projectile collision intervals.</summary>
    public static class AdventureZomboniRules
    {
        /// <summary>Checks the original living vehicle threshold shared by body shaking and automatic health loss.</summary>
        /// <param name="zombie">The vehicle whose native phase and body health are inspected.</param>
        /// <returns>Whether a living ice vehicle or catapult has strictly fewer than two hundred body health points.</returns>
        internal static bool HasCriticalDamage(Zombie zombie)
        {
            return (zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Catapult)
                && (zombie.PresentationNativePhase < 1 || zombie.PresentationNativePhase > 3)
                && zombie.Health < 200f;
        }

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
        /// <param name="projectile">The straight projectile whose original type selects its logical width.</param>
        /// <param name="zombie">The ice vehicle or catapult with its unchanged native logical body rectangle.</param>
        /// <param name="previousX">The projectile center in logical columns before this native motion update.</param>
        /// <returns>Whether the original horizontal projectile and vehicle rectangles touch or overlap.</returns>
        public static bool ProjectileCollides(Pea projectile, Zombie zombie, float previousX)
        {
            // PC EffectedByDamage rejects ZombieDying/Burned/Mowed before collision selection.
            if (zombie.ZomboniState?.FlatTires == true || zombie.CatapultState?.FlatTires == true
                || zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3)
            {
                return false;
            }

            // PC 47CCB0 calls CheckForCollision before UpdateMotion stores mX; 47DFF0 reads that previous integer.
            // Convert the retained sprite center back to mX; Torchwood preserves the original pea origin.
            var puff = AdventurePuffShroomRules.IsShortRange(projectile.SourceKind) || projectile.SourceKind == PlantKind.ScaredyShroom;
            var halfWidth = projectile.SourceKind == PlantKind.Cactus ? 15f : puff ? 16f : 14f;
            var left = (int)(40f + previousX * 80f - halfWidth);
            // PC GetProjectileRect 47DFF0 starts with the 40-pixel logical width, independent of image size.
            var width = 40;
            if (projectile.Fire)
            {
                width = 30;
            }
            else if (projectile.SourceKind == PlantKind.Cactus)
            {
                left -= 25;
                width += 25;
            }
            else if (AdventureTorchwoodRules.IsPea(projectile.SourceKind))
            {
                left -= 15;
                width += 15;
            }

            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return left + width >= body.X && left <= body.X + body.Width;
        }
    }
}
