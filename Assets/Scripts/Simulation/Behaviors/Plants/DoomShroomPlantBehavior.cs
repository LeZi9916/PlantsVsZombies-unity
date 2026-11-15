#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns DoomShroomPlantBehavior state and ordered action hooks.</summary>
    internal sealed class DoomShroomPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureDoomShroomState? State;
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventureDoomShroom(plant);
            return true;
        }
    }
}
