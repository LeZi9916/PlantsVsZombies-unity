#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ChomperPlantBehavior state and ordered action hooks.</summary>
    internal sealed class ChomperPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureChomperState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._specialPlantActions.AdvanceAdventureChomper(plant);
        }
    }
}
