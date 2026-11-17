#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the shared counter and independently firing heads of multi-head shooters.</summary>
    internal abstract class MultiHeadPlantBehavior : PlantBehavior
    {
        /// <summary>Retains the native launch counter and head clocks.</summary>
        internal global::PlantsVsZombies.Adventure.AdventureMultiHeadTimingState? Timing;
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal override void ResetPoolState()
        {
            base.ResetPoolState();
            Timing = default;
        }

        /// <summary>Retains capability-owned records while this behavior is live.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        internal override void MarkPoolState(SimulationRuntimePool pool)
        {
            base.MarkPoolState(pool);
        }

    }
}
