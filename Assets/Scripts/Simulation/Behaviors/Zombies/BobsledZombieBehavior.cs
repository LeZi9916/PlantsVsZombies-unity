#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Bobsled native action state and transitions.</summary>
    internal sealed class BobsledZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureBobsledState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.AfterMotion:
                    context._bobsledZombieActions.AdvanceAdventureBobsled(zombie);
                    break;
            }
            return false;
        }
    }
}
