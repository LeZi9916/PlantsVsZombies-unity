#nullable enable
using System;

namespace PlantsVsZombies.Progression
{
    /// <summary>
    /// A selector refresh result, not a promise that the host implements a destination.
    /// Transitions and other UI conditions can still hide or disable individual widgets.
    /// </summary>
    public sealed class SelectorProgressState
    {
        /// <summary>Initializes selector progress state.</summary>
        /// <param name = "displayedAdventureLevel">The displayed adventure level.</param>
        /// <param name = "useStartAdventureArtwork">The use start adventure artwork.</param>
        /// <param name = "miniGamesLocked">The mini games locked.</param>
        /// <param name = "puzzleLocked">The puzzle locked.</param>
        /// <param name = "survivalLocked">The survival locked.</param>
        /// <param name = "almanacVisible">The almanac visible.</param>
        /// <param name = "storeVisible">The store visible.</param>
        /// <param name = "zenGardenVisible">The zen garden visible.</param>
        /// <param name = "completionIndicatorEligible">The completion indicator eligible.</param>
        /// <param name = "profileButtonStyleFlag">The profile button style flag.</param>
        internal SelectorProgressState(int displayedAdventureLevel, bool useStartAdventureArtwork, bool miniGamesLocked, bool puzzleLocked, bool survivalLocked, bool almanacVisible, bool storeVisible, bool zenGardenVisible, bool completionIndicatorEligible, bool profileButtonStyleFlag)
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

        /// <summary>Gets or sets displayed adventure level.</summary>
        public int DisplayedAdventureLevel { get; }
        /// <summary>Gets or sets use start adventure artwork.</summary>
        public bool UseStartAdventureArtwork { get; }
        /// <summary>Gets or sets mini games locked.</summary>
        public bool MiniGamesLocked { get; }
        /// <summary>Gets or sets puzzle locked.</summary>
        public bool PuzzleLocked { get; }
        /// <summary>Gets or sets survival locked.</summary>
        public bool SurvivalLocked { get; }
        /// <summary>Gets or sets almanac visible.</summary>
        public bool AlmanacVisible { get; }
        /// <summary>Gets or sets store visible.</summary>
        public bool StoreVisible { get; }
        /// <summary>Gets or sets zen garden visible.</summary>
        public bool ZenGardenVisible { get; }
        /// <summary>Selector + 0x158 eligibility, not unconditional on-screen visibility.</summary>
        public bool CompletionIndicatorEligible { get; }
        /// <summary>Gets or sets profile button style flag.</summary>
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
                case SelectorMode.MiniGames:
                    locked = MiniGamesLocked;
                    break;
                case SelectorMode.Puzzle:
                    locked = PuzzleLocked;
                    break;
                case SelectorMode.Survival:
                    locked = SurvivalLocked;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (selectorInputBlocker != 0)
            {
                return SelectorModeClick.Ignored;
            }

            return locked ? SelectorModeClick.ShowLockedDialog : SelectorModeClick.OpenModeSelection;
        }
    }
}
