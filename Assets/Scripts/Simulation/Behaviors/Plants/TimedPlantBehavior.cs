#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns TimedPlantBehavior state and ordered action hooks.</summary>
    internal class TimedPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._plantTimingSystem.InitializeTimedPlant(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._plantTimingSystem.AdvanceTimedPlant(plant);
        }
    }
}
