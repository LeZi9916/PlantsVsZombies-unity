#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MarigoldPlantBehavior state and ordered action hooks.</summary>
    internal sealed class MarigoldPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            context._zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            p.ActionCount++;
            context._coinSystem.SpawnAdventureCoin(context._world._random.Next(100) < 10 ? AdventureCoinKind.Gold : AdventureCoinKind.Silver, p.Column + 0.5f, p.Row);
            p.ActionTimer = 24f;
            return;
        }
    }
}
