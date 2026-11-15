#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Zomboni native action state and transitions.</summary>
    internal sealed class ZomboniZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventureZomboniState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Vehicle:
                    context._boardEnvironment.ExtendAdventureIceTrail(zombie);
                    break;
            }
            return false;
        }
    }
}
