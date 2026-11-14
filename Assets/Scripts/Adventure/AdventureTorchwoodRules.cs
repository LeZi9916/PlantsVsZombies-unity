#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares original Torchwood projectile admission and native horizontal overlap.</summary>
    public static class AdventureTorchwoodRules
    {
        /// <summary>Checks whether a plant creates the original normal or snow pea projectile type.</summary>
        /// <param name="kind">The original projectile source plant.</param>
        /// <returns>Whether Torchwood may change this plant's ordinary pea projectiles.</returns>
        public static bool IsPea(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater
                || kind == PlantKind.GatlingPea || kind == PlantKind.Threepeater || kind == PlantKind.SplitPea;
        }

        /// <summary>Checks original GetPlantAttackRect and GetProjectileRect overlap without using sprite bounds.</summary>
        /// <param name="torch">The original Torchwood plant receiving a projectile.</param>
        /// <param name="shot">The still-normal or snow pea before its next position update.</param>
        /// <returns>Whether at least one native horizontal pixel overlaps in the same logical row.</returns>
        public static bool Overlaps(Plant torch, Pea shot)
        {
            if (!IsPea(shot.SourceKind) || shot.Fire || shot.Lobbed || shot.Homing
                || shot.Row != torch.Row || shot.LastTorchColumn == torch.Column)
            {
                return false;
            }

            var attackLeft = 40 + torch.Column * 80 + 50;
            // Pea sprites are 28x28; native projectile mX is fourteen pixels left of the retained center.
            // PC GetProjectileRect widens normal and snow projectiles fifteen pixels toward the left.
            var projectileLeft = (int)(40f + shot.X * 80f - 14f) - 15;
            return Math.Min(attackLeft + 30, projectileLeft + 55) - Math.Max(attackLeft, projectileLeft) >= 1;
        }
    }
}
