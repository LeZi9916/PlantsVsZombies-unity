#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Ladder native action state and transitions.</summary>
    internal sealed class LadderZombieBehavior : WalkingZombieBehavior
    {
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.AfterMotion:
                    return context._ladderSystem.AdvanceAdventureLadderPlacing(zombie);
            }
            return false;
        }
    }
}
