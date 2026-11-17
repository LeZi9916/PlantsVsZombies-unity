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
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal override void ResetPoolState()
        {
            base.ResetPoolState();
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
