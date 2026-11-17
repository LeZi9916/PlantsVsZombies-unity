#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the Pogo native action state and transitions.</summary>
    internal sealed class PogoZombieBehavior : WalkingZombieBehavior
    {
        /// <summary>Retains the species-specific native state.</summary>
        internal AdventurePogoState? State;
        /// <inheritdoc/>
        public override bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            switch (phase)
            {
                case ZombieActionPhase.AfterWalking:
                    context._pogoZombieActions.AdvanceAdventurePogo(zombie, previousX);
                    break;
            }
            return false;
        }
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal override void ResetPoolState()
        {
            base.ResetPoolState();
            State = default;
        }

        /// <summary>Retains capability-owned records while this behavior is live.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        internal override void MarkPoolState(SimulationRuntimePool pool)
        {
            base.MarkPoolState(pool);
            pool.Mark(State);
        }

    }
}
