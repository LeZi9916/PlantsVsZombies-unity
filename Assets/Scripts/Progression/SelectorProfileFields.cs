#nullable enable

namespace PlantsVsZombies.Progression
{
    /// <summary>
    /// The selector-relevant fields of one native profile snapshot. Values are
    /// raw signed 32-bit fields except for the byte at 0x340; this is not a save format.
    /// </summary>
    public sealed class SelectorProfileFields
    {
        /// <summary>Initializes selector profile fields.</summary>
        /// <param name = "adventureLevel">The adventure level.</param>
        /// <param name = "adventureCompletions">The adventure completions.</param>
        /// <param name = "miniGamesUnlockValue">The mini games unlock value.</param>
        /// <param name = "puzzleUnlockValue">The puzzle unlock value.</param>
        /// <param name = "survivalUnlockValue">The survival unlock value.</param>
        /// <param name = "storeEarlyAccessValue">The store early access value.</param>
        /// <param name = "profileButtonStyleFlag">The profile button style flag.</param>
        public SelectorProfileFields(int adventureLevel, int adventureCompletions, int miniGamesUnlockValue, int puzzleUnlockValue, int survivalUnlockValue, int storeEarlyAccessValue, bool profileButtonStyleFlag)
        {
            AdventureLevel = adventureLevel;
            AdventureCompletions = adventureCompletions;
            MiniGamesUnlockValue = miniGamesUnlockValue;
            PuzzleUnlockValue = puzzleUnlockValue;
            SurvivalUnlockValue = survivalUnlockValue;
            StoreEarlyAccessValue = storeEarlyAccessValue;
            ProfileButtonStyleFlag = profileButtonStyleFlag;
        }

        /// <summary>Gets or sets adventure level.</summary>
        public int AdventureLevel { get; } // profile + 0x04c
        /// <summary>Gets or sets adventure completions.</summary>
        public int AdventureCompletions { get; } // profile + 0x054
        /// <summary>Gets or sets mini games unlock value.</summary>
        public int MiniGamesUnlockValue { get; } // profile + 0x34c
        /// <summary>Gets or sets puzzle unlock value.</summary>
        public int PuzzleUnlockValue { get; } // profile + 0x350
        /// <summary>Gets or sets survival unlock value.</summary>
        public int SurvivalUnlockValue { get; } // profile + 0x364
        /// <summary>Gets or sets store early access value.</summary>
        public int StoreEarlyAccessValue { get; } // profile + 0x374
        /// <summary>Gets or sets profile button style flag.</summary>
        public bool ProfileButtonStyleFlag { get; } // profile + 0x340 != 0
    }
}
