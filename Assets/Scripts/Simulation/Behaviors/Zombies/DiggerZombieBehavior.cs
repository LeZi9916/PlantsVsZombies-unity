#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Digger native action state and transitions.</summary>
    internal sealed class DiggerZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureDiggerState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Underground:
                    return context._diggerZombieActions.AdvanceAdventureDigger(zombie, dt);
            }
            return false;
        }
    }
}
