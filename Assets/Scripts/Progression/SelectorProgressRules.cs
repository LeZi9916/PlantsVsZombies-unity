using System;

namespace PvZReplica.Progression
{
    /// <summary>
    /// The selector-relevant fields of one native profile snapshot. Values are
    /// raw signed 32-bit fields except for the byte at 0x340; this is not a save format.
    /// </summary>
    public sealed class SelectorProfileFields
    {
        public SelectorProfileFields(int adventureLevel, int adventureCompletions,
            int miniGamesUnlockValue, int puzzleUnlockValue, int survivalUnlockValue,
            int storeEarlyAccessValue, bool profileButtonStyleFlag)
        {
            AdventureLevel = adventureLevel;
            AdventureCompletions = adventureCompletions;
            MiniGamesUnlockValue = miniGamesUnlockValue;
            PuzzleUnlockValue = puzzleUnlockValue;
            SurvivalUnlockValue = survivalUnlockValue;
            StoreEarlyAccessValue = storeEarlyAccessValue;
            ProfileButtonStyleFlag = profileButtonStyleFlag;
        }

        public int AdventureLevel { get; }             // profile + 0x04c
        public int AdventureCompletions { get; }       // profile + 0x054
        public int MiniGamesUnlockValue { get; }       // profile + 0x34c
        public int PuzzleUnlockValue { get; }          // profile + 0x350
        public int SurvivalUnlockValue { get; }        // profile + 0x364
        public int StoreEarlyAccessValue { get; }      // profile + 0x374
        public bool ProfileButtonStyleFlag { get; }    // profile + 0x340 != 0
    }

    /// <summary>Runtime inputs for one selector refresh, including separate query results.</summary>
    public sealed class SelectorRuntimeFields
    {
        /// <summary>Uses one effective platform result at all three native call sites.</summary>
        public SelectorRuntimeFields(bool appFlag9E8, int appValue9E4,
            bool platformQueryPassed, bool auxiliaryVisibilityOverride)
            : this(appFlag9E8, appValue9E4, platformQueryPassed, platformQueryPassed,
                platformQueryPassed, auxiliaryVisibilityOverride)
        {
        }

        /// <summary>Supplies the effective platform results at the three call sites separately.</summary>
        public SelectorRuntimeFields(bool appFlag9E8, int appValue9E4,
            bool modeQueryPassed, bool completionQueryPassed, bool gardenQueryPassed,
            bool auxiliaryVisibilityOverride)
        {
            AppFlag9E8 = appFlag9E8;
            AppValue9E4 = appValue9E4;
            ModeQueryPassed = modeQueryPassed;
            CompletionQueryPassed = completionQueryPassed;
            GardenQueryPassed = gardenQueryPassed;
            AuxiliaryVisibilityOverride = auxiliaryVisibilityOverride;
        }

        public bool AppFlag9E8 { get; }
        public int AppValue9E4 { get; }

        /// <summary>Effective app + 0x99c / 0x651f30 result for the mode-lock check.</summary>
        public bool ModeQueryPassed { get; }
        /// <summary>Effective result for the completion-indicator check.</summary>
        public bool CompletionQueryPassed { get; }
        /// <summary>Effective result for the garden-visibility check.</summary>
        public bool GardenQueryPassed { get; }

        /// <summary>Selector byte + 0x159; affects auxiliary visibility, not mode locks.</summary>
        public bool AuxiliaryVisibilityOverride { get; }
    }

    public enum SelectorMode { MiniGames, Puzzle, Survival }

    public enum SelectorModeClick
    {
        Ignored,
        ShowLockedDialog,
        OpenModeSelection
    }

    /// <summary>
    /// A selector refresh result, not a promise that the host implements a destination.
    /// Transitions and other UI conditions can still hide or disable individual widgets.
    /// </summary>
    public sealed class SelectorProgressState
    {
        internal SelectorProgressState(int displayedAdventureLevel, bool useStartAdventureArtwork,
            bool miniGamesLocked, bool puzzleLocked, bool survivalLocked,
            bool almanacVisible, bool storeVisible, bool zenGardenVisible,
            bool completionIndicatorEligible, bool profileButtonStyleFlag)
        {
            DisplayedAdventureLevel = displayedAdventureLevel;
            UseStartAdventureArtwork = useStartAdventureArtwork;
            MiniGamesLocked = miniGamesLocked;
            PuzzleLocked = puzzleLocked;
            SurvivalLocked = survivalLocked;
            AlmanacVisible = almanacVisible;
            StoreVisible = storeVisible;
            ZenGardenVisible = zenGardenVisible;
            CompletionIndicatorEligible = completionIndicatorEligible;
            ProfileButtonStyleFlag = profileButtonStyleFlag;
        }

        public int DisplayedAdventureLevel { get; }
        public bool UseStartAdventureArtwork { get; }
        public bool MiniGamesLocked { get; }
        public bool PuzzleLocked { get; }
        public bool SurvivalLocked { get; }
        public bool AlmanacVisible { get; }
        public bool StoreVisible { get; }
        public bool ZenGardenVisible { get; }

        /// <summary>Selector + 0x158 eligibility, not unconditional on-screen visibility.</summary>
        public bool CompletionIndicatorEligible { get; }
        public bool ProfileButtonStyleFlag { get; }

        /// <summary>
        /// Mirrors the three mode branches in the selector callback. A nonzero
        /// selector + 0x15c suppresses callbacks before the lock is checked.
        /// </summary>
        public SelectorModeClick DecideModeClick(SelectorMode mode, int selectorInputBlocker)
        {
            bool locked;
            switch (mode)
            {
                case SelectorMode.MiniGames: locked = MiniGamesLocked; break;
                case SelectorMode.Puzzle: locked = PuzzleLocked; break;
                case SelectorMode.Survival: locked = SurvivalLocked; break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
            if (selectorInputBlocker != 0) return SelectorModeClick.Ignored;
            return locked ? SelectorModeClick.ShowLockedDialog : SelectorModeClick.OpenModeSelection;
        }
    }

    /// <summary>Selector-only rules for the locally examined executable.</summary>
    public static class SelectorProgressRules
    {
        /// <param name="profile">Null represents the native null profile pointer.</param>
        /// <param name="runtime">The host must provide actual runtime conditions.</param>
        public static SelectorProgressState Evaluate(SelectorProfileFields profile, SelectorRuntimeFields runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            bool hasProfile = profile != null;
            int level = hasProfile ? profile.AdventureLevel : 1;
            bool completed = hasProfile && profile.AdventureCompletions > 0;
            bool modesRestricted = IsRestricted(runtime, runtime.ModeQueryPassed);
            bool completionRestricted = IsRestricted(runtime, runtime.CompletionQueryPassed);
            bool gardenRestricted = IsRestricted(runtime, runtime.GardenQueryPassed);

            bool miniGamesUnlocked = hasProfile && (completed || profile.MiniGamesUnlockValue > 0);
            bool puzzleUnlocked = hasProfile && (completed || profile.PuzzleUnlockValue > 0) && !modesRestricted;
            bool survivalUnlocked = hasProfile && (completed || profile.SurvivalUnlockValue > 0) && !modesRestricted;
            bool almanacVisible = runtime.AuxiliaryVisibilityOverride ||
                (hasProfile && (completed || level >= 15));
            bool storeVisible = runtime.AuxiliaryVisibilityOverride ||
                (hasProfile && (completed || profile.StoreEarlyAccessValue != 0 || level >= 25));
            bool zenGardenVisible = runtime.AuxiliaryVisibilityOverride ||
                (hasProfile && !gardenRestricted && (completed || level >= 45));

            return new SelectorProgressState(level, !hasProfile || (level < 2 && !completed),
                !miniGamesUnlocked, !puzzleUnlocked, !survivalUnlocked,
                almanacVisible, storeVisible, zenGardenVisible, completed && !completionRestricted,
                hasProfile && profile.ProfileButtonStyleFlag);
        }

        private static bool IsRestricted(SelectorRuntimeFields runtime, bool queryPassed)
        {
            return runtime.AppFlag9E8 || (runtime.AppValue9E4 == 1 && !queryPassed);
        }
    }
}
