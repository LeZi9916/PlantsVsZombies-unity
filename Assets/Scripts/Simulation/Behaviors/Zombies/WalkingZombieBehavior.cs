#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Uses the common walking, equipment, status and chewing capabilities.</summary>
    internal class WalkingZombieBehavior : IZombieBehavior
    {
        /// <inheritdoc/>
        public virtual bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            return false;
        }
    }
}
