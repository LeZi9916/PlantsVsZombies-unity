#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns CherryBombPlantBehavior state and ordered action hooks.</summary>
    internal sealed class CherryBombPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureCherryBombState? State;
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._explosivePlantActions.InitializeAdventureCherryBomb(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._explosivePlantActions.AdvanceAdventureCherryBomb(plant);
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._explosivePlantActions.DetonateAdventureCherryBomb(p);
            return;
        }
    }
}
