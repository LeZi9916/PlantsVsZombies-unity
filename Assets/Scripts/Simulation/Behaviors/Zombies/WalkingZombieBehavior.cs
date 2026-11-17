#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Uses the common walking, equipment, status and chewing capabilities.</summary>
    internal class WalkingZombieBehavior : IZombieBehavior, ISimulationPooledObject
    {
        /// <inheritdoc/>
        public virtual bool Advance(Zombie zombie, ZombieBehaviorContext context, ZombieActionPhase phase, float dt, float previousX = 0f)
        {
            return false;
        }
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal virtual void ResetPoolState()
        {

        }

        /// <summary>Retains capability-owned records while this behavior is live.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        internal virtual void MarkPoolState(SimulationRuntimePool pool)
        {

        }

        /// <inheritdoc/>
        void ISimulationPooledObject.ResetForPool()
        {
            ResetPoolState();
        }

        /// <inheritdoc/>
        void ISimulationPooledObject.MarkPoolReferences(SimulationRuntimePool pool)
        {
            MarkPoolState(pool);
        }

    }
}
