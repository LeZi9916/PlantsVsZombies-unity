#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns CoffeeBeanPlantBehavior state and ordered action hooks.</summary>
    internal sealed class CoffeeBeanPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureCoffeeBeanState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventureCoffeeBean(plant);
            return true;
        }
    }
}
