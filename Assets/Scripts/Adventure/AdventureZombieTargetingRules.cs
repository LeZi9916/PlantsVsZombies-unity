#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie targeting rules.</summary>
    public static class AdventureZombieTargetingRules
    {
        // PC Plant::FindTargetZombie 0x475AA0: has-head is required by these three contact plants only.
        // The separate PC EffectedByDamage gate rejects dying/burned/mowed/dead actors.
        /// <summary>Performs can be auto targeted.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting can be auto targeted.</returns>
        public static bool CanBeAutoTargeted(Zombie z, PlantKind kind = PlantKind.Peashooter)
        {
            if (z == null || z.Health <= 0 || z.PresentationNativePhase >= 1 && z.PresentationNativePhase <= 3)
            {
                return false;
            }

            return !z.Headless || kind != PlantKind.PotatoMine && kind != PlantKind.Chomper && kind != PlantKind.TangleKelp;
        }
    }
}
