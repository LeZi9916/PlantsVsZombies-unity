#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides inert defaults for optional plant action phases.</summary>
    internal class PlantBehavior : IPlantBehavior
    {
        /// <inheritdoc/>
        public virtual void Initialize(Plant plant, PlantBehaviorContext context)
        {
        }
        /// <inheritdoc/>
        public virtual bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual bool BlocksAction(Plant plant)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual bool AdvanceAction(Plant plant, PlantBehaviorContext context, float dt)
        {
            return false;
        }
        /// <inheritdoc/>
        public virtual void Act(Plant plant, PlantBehaviorContext context)
        {
        }
    }
}
