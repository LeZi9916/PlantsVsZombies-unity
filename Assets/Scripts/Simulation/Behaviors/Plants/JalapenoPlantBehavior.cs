#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns JalapenoPlantBehavior state and ordered action hooks.</summary>
    internal sealed class JalapenoPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureJalapenoState? State;
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._explosivePlantActions.InitializeAdventureJalapeno(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._explosivePlantActions.AdvanceAdventureJalapeno(plant);
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._explosivePlantActions.DetonateAdventureJalapeno(p);
            return;
        }
    }
}
