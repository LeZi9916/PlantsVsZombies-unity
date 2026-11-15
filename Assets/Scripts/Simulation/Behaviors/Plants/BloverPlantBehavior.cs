#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BloverPlantBehavior state and ordered action hooks.</summary>
    internal sealed class BloverPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureBloverState? State;
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            // PC PlantInit 46C23A starts anim_blow immediately at twenty fps and delays its special action fifty updates.
            plant.BloverPresentation = new AdventureBloverState();
            plant.ActionTimer = .5f;
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventureBlover(plant);
            return true;
        }
    }
}
