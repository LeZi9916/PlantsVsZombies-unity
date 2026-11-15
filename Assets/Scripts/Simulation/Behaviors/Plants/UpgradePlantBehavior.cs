#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns UpgradePlantBehavior state and ordered action hooks.</summary>
    internal sealed class UpgradePlantBehavior : PlantBehavior
    {
        /// <summary>Retains the native UpgradeBurstTicks state for this capability.</summary>
        internal int BurstTicks;

        /// <summary>Retains the native CannonTargetRow state for this capability.</summary>
        internal float CannonTargetRow;

        /// <summary>Retains the native CannonTargetX state for this capability.</summary>
        internal float CannonTargetX;

        /// <summary>Retains the native CannonReloadRemaining state for this capability.</summary>
        internal float CannonReloadRemaining;

        /// <summary>Retains the native CannonPhase state for this capability.</summary>
        internal CobCannonPhase CannonPhase;

        /// <inheritdoc/>
        public override bool AdvanceAction(Plant plant, PlantBehaviorContext context, float dt)
        {
            return context._upgradePlantActions.AdvanceAdventureUpgradePlant(plant, dt);
        }
    }
}
