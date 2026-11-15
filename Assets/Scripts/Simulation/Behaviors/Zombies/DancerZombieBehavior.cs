#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Dancing native action state and transitions.</summary>
    internal sealed class DancerZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureDancerState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Dance:
                    context._dancerZombieActions.AdvanceAdventureDancer(zombie);
                    return zombie.PresentationNativePhase == 50;
            }
            return false;
        }
    }
}
