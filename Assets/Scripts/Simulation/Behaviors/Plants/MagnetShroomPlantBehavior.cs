#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MagnetShroomPlantBehavior state and ordered action hooks.</summary>
    internal sealed class MagnetShroomPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureMagnetState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._magnetPlantActions.AdvanceAdventureMagnetShroom(plant);
            return true;
        }
    }
}
