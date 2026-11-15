#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SunShroomPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SunShroomPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            context._world.Suns.Add(context._sunSystem.CreateAdventurePresentationSun(p, p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge < 120f ? 15 : 25));
            p.ActionTimer = 24f;
            return;
        }
        /// <inheritdoc/>
        public override bool BlocksAction(Plant plant)
        {
            return plant.PresentationAwakeAge >= 120f && plant.PresentationAwakeAge < 121f;
        }
    }
}
