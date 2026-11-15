#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns one zombie's special action state and phase-specific behavior.</summary>
    internal interface IZombieBehavior
    {
        /// <summary>Advances a species action at its original place in the common update.</summary>
        /// <param name="zombie">The active actor.</param>
        /// <param name="context">The required zombie action services.</param>
        /// <param name="phase">The current ordered action slot.</param>
        /// <param name="dt">The fixed simulation interval.</param>
        /// <param name="previousX">The pre-movement position for motion-triggered actions.</param>
        /// <returns>Whether the action consumes this slot's remaining common update.</returns>
        bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f);
    }
}
