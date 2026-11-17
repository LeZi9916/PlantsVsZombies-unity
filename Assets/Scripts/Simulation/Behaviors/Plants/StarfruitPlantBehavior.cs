#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns StarfruitPlantBehavior state and ordered action hooks.</summary>
    internal sealed class StarfruitPlantBehavior : PlantBehavior
    {
        /// <summary>Retains the native StarfruitShootingCountdown state for this capability.</summary>
        internal int ShootingCountdown;

        /// <summary>Retains the native StarfruitLaunchCountdown state for this capability.</summary>
        internal int LaunchCountdown = -1;

        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            plant.StarfruitLaunchCountdown = context._world._random.Next(151);
            plant.ActionTimer = plant.StarfruitLaunchCountdown * .01f;
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            context._plantTimingSystem.AdvanceAdventureStarfruit(plant);
            return true;
        }
        /// <summary>Restores construction defaults after all board references expire.</summary>
        internal override void ResetPoolState()
        {
            base.ResetPoolState();
            ShootingCountdown = default;
            LaunchCountdown = -1;
        }

        /// <summary>Retains capability-owned records while this behavior is live.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        internal override void MarkPoolState(SimulationRuntimePool pool)
        {
            base.MarkPoolState(pool);
        }

    }
}
