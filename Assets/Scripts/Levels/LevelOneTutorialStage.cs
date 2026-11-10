#nullable enable

namespace PlantsVsZombies.Levels
{
    // These numeric values are the native Board field, not a complete enum.
    /// <summary>Defines level one tutorial stage.</summary>
    public enum LevelOneTutorialStage
    {
        /// <summary>Identifies not started.</summary>
        NotStarted = 0,
        /// <summary>Identifies awaiting seed selection.</summary>
        AwaitingSeedSelection = 1,
        /// <summary>Identifies awaiting plant placement.</summary>
        AwaitingPlantPlacement = 2,
        /// <summary>Identifies awaiting usable seed.</summary>
        AwaitingUsableSeed = 3,
        /// <summary>Identifies zombie onslaught prompt.</summary>
        ZombieOnslaughtPrompt = 4
    }
}
