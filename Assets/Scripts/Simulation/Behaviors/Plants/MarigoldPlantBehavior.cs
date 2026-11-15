#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MarigoldPlantBehavior state and ordered action hooks.</summary>
    internal sealed class MarigoldPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            plant.ActionTimer = context._world._random.Next(2501) * .01f;
        }

        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            p.ActionTimer = (2350 + context._world._random.Next(151)) * .01f;
            context._zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            p.ActionCount++;
            context._coinSystem.SpawnAdventureCoin(context._world._random.Next(100) < 10 ? AdventureCoinKind.Gold : AdventureCoinKind.Silver, p.Column + 0.5f, p.Row);
            return;
        }
    }
}
