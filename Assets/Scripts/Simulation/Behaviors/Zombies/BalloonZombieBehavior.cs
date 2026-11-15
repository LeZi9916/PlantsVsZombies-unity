#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Balloon native action state and transitions.</summary>
    internal sealed class BalloonZombieBehavior : WalkingZombieBehavior
    {
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Wind:
                    return context._balloonZombieActions.AdvanceAdventureBalloonWind(zombie);
                case ZombieActionPhase.AfterMotion:
                    context._balloonZombieActions.AdvanceAdventureBalloon(zombie);
                    break;
            }
            return false;
        }
    }
}
