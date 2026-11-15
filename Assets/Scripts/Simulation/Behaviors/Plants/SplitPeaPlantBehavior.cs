#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SplitPeaPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SplitPeaPlantBehavior : MultiHeadPlantBehavior
    {
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            plant.MultiHeadTiming = AdventureMultiHeadTimingRules.Initialize(plant.Kind, context._world._random.Next(151));
            plant.ActionTimer = plant.MultiHeadTiming.Value.Countdown * .01f;
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            if (plant.ActionTimer >= 100000f)
            {
                return false;
            }
            context._plantTimingSystem.AdvanceAdventureMultiHeadPlant(plant);
            return true;
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            var x = p.Column + 0.5f;
            bool front = context._boardQueries.FindAdventureTarget(p.Row, x, 20f, false) != null, back = context._boardQueries.FindAdventureTarget(p.Row, -1f, x, false) != null;
            if (front)
            {
                context._projectileSystem.FireAdventurePea(p, p.Row, false, 20f);
            }

            if (back)
            {
                context._projectileSystem.FireAdventurePea(p, p.Row, true, 20f);
                context._projectileSystem.FireAdventurePea(p, p.Row, true, 20f).X += 0.3f;
            }

            if (front || back)
            {
                p.ActionTimer = 1.425f;
            }

            return;

        }
    }
}
