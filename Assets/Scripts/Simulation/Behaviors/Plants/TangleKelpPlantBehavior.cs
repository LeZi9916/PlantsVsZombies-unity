#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns TangleKelpPlantBehavior state and ordered action hooks.</summary>
    internal sealed class TangleKelpPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureTangleKelpState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventureTangleKelp(plant);
            return true;
        }
    }
}
