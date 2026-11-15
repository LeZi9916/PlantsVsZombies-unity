#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PotatoMinePlantBehavior state and ordered action hooks.</summary>
    internal sealed class PotatoMinePlantBehavior : PlantBehavior
    {
        /// <summary>Retains the native MinePhase state for this capability.</summary>
        internal MinePhase MinePhase;

        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventurePotatoMineState? State;
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._specialPlantActions.InitializeAdventurePotatoMine(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._specialPlantActions.AdvanceAdventurePotatoMine(plant);
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._specialPlantActions.AdvanceAdventurePotatoMine(p);
            return;
        }
    }
}
