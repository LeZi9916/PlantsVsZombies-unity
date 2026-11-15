#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Combines native firing timing with independently owned fear and recovery state.</summary>
    internal sealed class ScaredyPlantBehavior : TimedPlantBehavior
    {
        /// <summary>Retains the original fear, lowering, and recovery action state.</summary>
        internal global::PlantsVsZombies.Adventure.AdventureScaredyShroomState? State;
    }
}
