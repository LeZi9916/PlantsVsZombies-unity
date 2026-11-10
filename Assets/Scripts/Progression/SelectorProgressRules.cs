#nullable enable
using System;

namespace PlantsVsZombies.Progression
{
    /// <summary>Selector-only rules for the locally examined executable.</summary>
    public static class SelectorProgressRules
    {
        /// <param name = "profile">Null represents the native null profile pointer.</param>
        /// <param name = "runtime">The host must provide actual runtime conditions.</param>
        public static SelectorProgressState Evaluate(SelectorProfileFields profile, SelectorRuntimeFields runtime)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            var hasProfile = profile != null;
            var level = hasProfile ? profile.AdventureLevel : 1;
            var completed = hasProfile && profile.AdventureCompletions > 0;
            var modesRestricted = IsRestricted(runtime, runtime.ModeQueryPassed);
            var completionRestricted = IsRestricted(runtime, runtime.CompletionQueryPassed);
            var gardenRestricted = IsRestricted(runtime, runtime.GardenQueryPassed);
            var miniGamesUnlocked = hasProfile && (completed || profile.MiniGamesUnlockValue > 0);
            var puzzleUnlocked = hasProfile && (completed || profile.PuzzleUnlockValue > 0) && !modesRestricted;
            var survivalUnlocked = hasProfile && (completed || profile.SurvivalUnlockValue > 0) && !modesRestricted;
            var almanacVisible = runtime.AuxiliaryVisibilityOverride || (hasProfile && (completed || level >= 15));
            var storeVisible = runtime.AuxiliaryVisibilityOverride || (hasProfile && (completed || profile.StoreEarlyAccessValue != 0 || level >= 25));
            var zenGardenVisible = runtime.AuxiliaryVisibilityOverride || (hasProfile && !gardenRestricted && (completed || level >= 45));
            return new SelectorProgressState(level, !hasProfile || (level < 2 && !completed), !miniGamesUnlocked, !puzzleUnlocked, !survivalUnlocked, almanacVisible, storeVisible, zenGardenVisible, completed && !completionRestricted, hasProfile && profile.ProfileButtonStyleFlag);
        }

        /// <summary>Performs is restricted.</summary>
        /// <param name = "runtime">The runtime.</param>
        /// <param name = "queryPassed">The query passed.</param>
        /// <returns>The resulting is restricted.</returns>
        private static bool IsRestricted(SelectorRuntimeFields runtime, bool queryPassed)
        {
            return runtime.AppFlag9E8 || (runtime.AppValue9E4 == 1 && !queryPassed);
        }
    }
}
