#nullable enable
using System;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ThreepeaterPlantBehavior state and ordered action hooks.</summary>
    internal sealed class ThreepeaterPlantBehavior : MultiHeadPlantBehavior
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
            var target = false;
            for (var r = Math.Max(0, p.Row - 1); r <= Math.Min(context._world.RowCount - 1, p.Row + 1); r++)
            {
                if (context._boardQueries.FindAdventureTarget(r, x - 0.1f, 20f, false) != null)
                {
                    target = true;
                }
            }

            if (target)
            {
                for (var r = Math.Max(0, p.Row - 1); r <= Math.Min(context._world.RowCount - 1, p.Row + 1); r++)
                {
                    context._projectileSystem.FireAdventurePea(p, r, false, 20f);
                }

                p.ActionTimer = 1.425f;
            }

            return;
        }
    }
}
