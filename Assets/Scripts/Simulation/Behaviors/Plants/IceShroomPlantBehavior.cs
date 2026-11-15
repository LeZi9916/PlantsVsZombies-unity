#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns IceShroomPlantBehavior state and ordered action hooks.</summary>
    internal sealed class IceShroomPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._specialPlantActions.DetonateAdventureIceShroom(p);
            return;
        }
    }
}
