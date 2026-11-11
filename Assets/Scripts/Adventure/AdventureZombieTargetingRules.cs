#nullable enable
using PlantsVsZombies.Simulation;
using PlantsVsZombies.BoardUI;

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
            if (z == null || z.Health <= 0 || z.GraveRiseTicks > 0 || z.PresentationNativePhase >= 1 && z.PresentationNativePhase <= 3)
            {
                return false;
            }

            // PC EffectedByDamage 0x54D810 tests the left edge of GetZombieRect,
            // not the actor origin or the animation's visible pixels. Bobsled is exempt.
            if (z.Kind != ZombieKind.Bobsled && BodyRectangle(z).X > 800)
            {
                return false;
            }

            return !z.Headless || kind != PlantKind.PotatoMine && kind != PlantKind.Chomper && kind != PlantKind.TangleKelp;
        }

        /// <summary>Gets the native body rectangle used by plant targeting.</summary>
        /// <param name="z">The zombie whose logical collision rectangle is required.</param>
        /// <returns>The body rectangle in original board pixels; visual scaling does not shrink it.</returns>
        public static NativeRect BodyRectangle(Zombie z)
        {
            var x = (int)AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f);
            var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2);
            if (z.Kind == ZombieKind.Bobsled && (z.PresentationHasSled ?? !z.UsedAbility))
            {
                return new NativeRect(x + (backwards ? -105 : -50), 0, 275, 115);
            }

            return AdventureBowlingNativeRules.ZombieBounds((int)z.Kind, z.PresentationNativePhase, x, 0, backwards);
        }
    }
}
