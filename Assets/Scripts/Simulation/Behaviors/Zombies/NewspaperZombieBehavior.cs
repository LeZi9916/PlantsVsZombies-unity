#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Newspaper native action state and transitions.</summary>
    internal sealed class NewspaperZombieBehavior : WalkingZombieBehavior
    {
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.Equipment:
                    if (zombie.ShieldHealth <= 0 && !zombie.UsedAbility)
                    {
                        zombie.UsedAbility = true;
                        context._zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 31);
                    }
                    break;
            }
            return false;
        }
    }
}
