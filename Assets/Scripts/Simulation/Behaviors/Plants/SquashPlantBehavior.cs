#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SquashPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SquashPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureSquashState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventureSquash(plant);
            return true;
        }
    }
}
