#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the JackInTheBox native action state and transitions.</summary>
    internal sealed class JackInTheBoxZombieBehavior : WalkingZombieBehavior
    {
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.TimedSpecial:
                    return context._jackZombieActions.AdvanceAdventureJackInTheBox(zombie);
            }
            return false;
        }
    }
}
