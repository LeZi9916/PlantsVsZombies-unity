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
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal override void ResetPoolState()
        {
            base.ResetPoolState();
            MinePhase = default;
            State = default;
        }

        /// <summary>Retains capability-owned records while this behavior is live.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        internal override void MarkPoolState(SimulationRuntimePool pool)
        {
            base.MarkPoolState(pool);
            pool.Mark(State);
        }

    }
}
