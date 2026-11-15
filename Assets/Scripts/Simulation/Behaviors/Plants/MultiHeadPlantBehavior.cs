#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns the shared counter and independently firing heads of multi-head shooters.</summary>
    internal abstract class MultiHeadPlantBehavior : PlantBehavior
    {
        /// <summary>Retains the native launch counter and head clocks.</summary>
        internal global::PlantsVsZombies.Adventure.AdventureMultiHeadTimingState? Timing;
    }
}
