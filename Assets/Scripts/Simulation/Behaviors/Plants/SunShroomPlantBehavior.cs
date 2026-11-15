#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SunShroomPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SunShroomPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            plant.ActionTimer = (300 + context._world._random.Next(951)) * .01f;
        }

        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            // PC 46DE52 resets the production counter before constructing the pickup.
            p.ActionTimer = (2350 + context._world._random.Next(151)) * .01f;
            context._zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            context._world.Suns.Add(context._sunSystem.CreateAdventurePresentationSun(p, p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge < 120f ? 15 : 25));
            return;
        }
        /// <inheritdoc/>
        public override bool BlocksAction(Plant plant)
        {
            return plant.PresentationAwakeAge >= 120f && plant.PresentationAwakeAge < 121f;
        }
    }
}
