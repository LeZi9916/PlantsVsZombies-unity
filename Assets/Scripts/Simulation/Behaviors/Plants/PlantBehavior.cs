#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides inert defaults for optional plant action phases.</summary>
    internal class PlantBehavior : IPlantBehavior, ISimulationPooledObject
    {
        /// <inheritdoc/>
        public virtual void Initialize(Plant plant, PlantBehaviorContext context)
        {
        }
        /// <inheritdoc/>
        public virtual bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual bool BlocksAction(Plant plant)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual bool AdvanceAction(Plant plant, PlantBehaviorContext context, float dt)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual void Act(Plant plant, PlantBehaviorContext context)
        {
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
