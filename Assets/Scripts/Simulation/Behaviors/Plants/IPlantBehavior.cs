#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines ordered hooks for one plant's simulation behavior.</summary>
    internal interface IPlantBehavior
    {
        /// <summary>Initializes native timers without changing random draw order.</summary>
        /// <param name="plant">The newly created plant.</param>
        /// <param name="context">The required board services.</param>
        void Initialize(Plant plant, PlantBehaviorContext context);
        /// <summary>Advances the native action before generic countdowns.</summary>
        /// <param name="plant">The active plant.</param>
        /// <param name="context">The required board services.</param>
        /// <returns>Whether the native action owns this update.</returns>
        bool AdvanceNative(Plant plant, PlantBehaviorContext context);
        /// <summary>Checks whether growth temporarily suspends the generic action clock.</summary>
        /// <param name="plant">The active plant.</param>
        /// <returns>Whether growth suspends the action.</returns>
        bool BlocksAction(Plant plant);
        /// <summary>Advances an action after the shared action countdown decrements.</summary>
        /// <param name="plant">The active plant.</param>
        /// <param name="context">The required board services.</param>
        /// <param name="dt">The fixed simulation interval.</param>
        /// <returns>Whether the action owns the remaining update.</returns>
        bool AdvanceAction(Plant plant, PlantBehaviorContext context, float dt);
        /// <summary>Executes an action whose countdown has expired.</summary>
        /// <param name="plant">The ready plant.</param>
        /// <param name="context">The required board services.</param>
        void Act(Plant plant, PlantBehaviorContext context);
    }
}
