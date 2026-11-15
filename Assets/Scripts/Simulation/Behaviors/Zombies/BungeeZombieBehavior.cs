#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Bungee native action state and transitions.</summary>
    internal sealed class BungeeZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureBungeeState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Carrier:
                    context._bungeeZombieActions.AdvanceAdventureBungee(zombie);
                    return true;
            }
            return false;
        }
    }
}
