#nullable enable

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
            var cues = ProjectileFoleyValues(projectile, helm, fireSplash);
            return cues.Count == 1 ? new[] { cues.First } : new[] { cues.First, cues.Second };
        }

        /// <summary>Returns the ordered native impact cues without constructing a temporary collection.</summary>
        /// <param name="projectile">The original projectile type.</param>
        /// <param name="helm">The target's original intact helmet type.</param>
        /// <param name="fireSplash">Whether the target admits the fire splash impact.</param>
        /// <returns>The first cue, optional second cue, and valid cue count.</returns>
        internal static (int First, int Second, int Count) ProjectileFoleyValues(int projectile, int helm, bool fireSplash)
        {
            if (projectile == 10)
            {
                return (32, 0, 1);
            }
            if (projectile == 6 && fireSplash)
            {
                return (38, 0, 1);
            }
            var first = projectile == 12 ? 43 : projectile == 3 || projectile == 5 ? 62 : 0;
            if (helm == 2)
            {
                return first == 0 ? (46, 0, 1) : (first, 46, 2);
            }
            if (helm == 1 || helm == 3 || helm == 4)
            {
                return first == 0 ? (58, 1, 2) : (first, 58, 2);
            }
            return first == 0 ? (1, 0, 1) : (first, 0, 1);
        }
    }
}
