#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the PoleVaulter native action state and transitions.</summary>
    internal sealed class PoleVaulterZombieBehavior : WalkingZombieBehavior
    {
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Motion:
                    return context._vaultZombieActions.AdvanceAdventurePoleVault(zombie, dt);
            }
            return false;
        }
    }
}
