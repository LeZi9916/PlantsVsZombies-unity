#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns TorchwoodPlantBehavior state and ordered action hooks.</summary>
    internal sealed class TorchwoodPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._projectileSystem.UpdateAdventureTorchwood(plant);
            return true;
        }
    }
}
