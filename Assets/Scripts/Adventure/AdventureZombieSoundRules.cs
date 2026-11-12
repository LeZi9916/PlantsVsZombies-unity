#nullable enable
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie sound rules.</summary>
    public static class AdventureZombieSoundRules
    {
        // PC Projectile::PlayImpactSound 0x47D140, prior to damage changing the helm.
        /// <summary>Performs projectile foley.</summary>
        /// <param name = "projectile">The projectile.</param>
        /// <param name = "helm">The helm.</param>
        /// <param name = "fireSplash">The fire splash.</param>
        /// <returns>The resulting projectile foley.</returns>
        public static int[] ProjectileFoley(int projectile, int helm, bool fireSplash)
        {
            if (projectile == 10)
            {
                return new[]
                {
                    32
                };
            }

            if (projectile == 6 && fireSplash)
            {
                return new[]
                {
                    38
                };
            }

            var cues = new List<int>();
            var splat = true;
            if (projectile == 12)
            {
                cues.Add(43);
                splat = false;
            }

            if (projectile == 3 || projectile == 5)
            {
                cues.Add(62);
                splat = false;
            }

            if (helm == 2)
            {
                cues.Add(46);
                splat = false;
            }
            else if (helm == 1 || helm == 3 || helm == 4)
            {
                cues.Add(58);
            }

            if (splat)
            {
                cues.Add(1);
            }

            return cues.ToArray();
        }
    }
}
