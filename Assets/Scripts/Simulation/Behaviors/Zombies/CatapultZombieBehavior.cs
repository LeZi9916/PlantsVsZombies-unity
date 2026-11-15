#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Catapult native action state and transitions.</summary>
    internal sealed class CatapultZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureCatapultState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Vehicle:
                    return context._vehicleZombieActions.AdvanceAdventureCatapult(zombie);
            }
            return false;
        }
    }
}
