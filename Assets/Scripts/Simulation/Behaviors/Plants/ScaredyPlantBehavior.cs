#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Combines native firing timing with independently owned fear and recovery state.</summary>
    internal sealed class ScaredyPlantBehavior : TimedPlantBehavior
    {
        /// <summary>Retains the original fear, lowering, and recovery action state.</summary>
        internal global::PlantsVsZombies.Adventure.AdventureScaredyShroomState? State;
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
