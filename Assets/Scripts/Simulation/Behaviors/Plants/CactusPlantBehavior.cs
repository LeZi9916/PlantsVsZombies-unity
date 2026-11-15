#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns CactusPlantBehavior state and ordered action hooks.</summary>
    internal sealed class CactusPlantBehavior : PlantBehavior
    {
        /// <summary>Retains this plant capability's native state.</summary>
        internal AdventureCactusState? State;
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._plantTimingSystem.InitializeTimedPlant(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            if (plant.ActionTimer >= 100000f)
            {
                return false;
            }
            context._plantTimingSystem.AdvanceAdventureCactus(plant);
            return true;
        }
    }
}
