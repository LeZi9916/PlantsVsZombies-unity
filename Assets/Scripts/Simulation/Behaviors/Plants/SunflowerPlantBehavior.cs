#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SunflowerPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SunflowerPlantBehavior : PlantBehavior
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
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            context._world.Suns.Add(context._sunSystem.CreateAdventurePresentationSun(p, p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge < 120f ? 15 : 25));
            p.ActionTimer = 24f;
            return;
        }
    }
}
