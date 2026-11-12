#nullable enable
using System;

namespace PlantsVsZombies.Levels
{
    /// <summary>
    /// Only the proven first-play teaching transitions 0 through 4. Methods
    /// consume events already accepted by the host; they do not validate input,
    /// simulate sun or recharge, or claim to cover later native tutorial states.
    /// </summary>
    public sealed class LevelOneTutorial
    {
        /// <summary>Gets or sets stage.</summary>
        public LevelOneTutorialStage Stage { get; private set; }

        /// <summary>Gets or sets tutorial blocks wave countdown.</summary>
        public bool TutorialBlocksWaveCountdown
        {
            get
            {
                return Stage == LevelOneTutorialStage.AwaitingSeedSelection || Stage == LevelOneTutorialStage.AwaitingPlantPlacement || Stage == LevelOneTutorialStage.AwaitingUsableSeed;
            }
        }

        /// <summary>Performs on first active gameplay update.</summary>
        /// <returns>The resulting on first active gameplay update.</returns>
        public LevelOneTutorialEffect OnFirstActiveGameplayUpdate()
        {
            if (Stage != LevelOneTutorialStage.NotStarted)
            {
                return Unchanged();
            }

            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        /// <summary>Performs on seed picked up.</summary>
        /// <returns>The resulting on seed picked up.</returns>
        public LevelOneTutorialEffect OnSeedPickedUp()
        {
            if (Stage != LevelOneTutorialStage.AwaitingSeedSelection)
            {
                return Unchanged();
            }

            return Change(LevelOneTutorialStage.AwaitingPlantPlacement, null, null);
        }

        /// <summary>Performs on planting cursor cancelled.</summary>
        /// <returns>The resulting on planting cursor cancelled.</returns>
        public LevelOneTutorialEffect OnPlantingCursorCancelled()
        {
            if (Stage != LevelOneTutorialStage.AwaitingPlantPlacement)
            {
                return Unchanged();
            }

            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        /// <param name = "nativePlantArraySizeAfterPlacement">
        /// Board+0xD4 after the accepted placement: the native plant data-array
        /// allocation count, not a lifetime count or necessarily a visible/live
        /// count when an object is awaiting removal. It must be positive.
        /// </param>
        public LevelOneTutorialEffect OnPlantPlaced(int nativePlantArraySizeAfterPlacement)
        {
            if (nativePlantArraySizeAfterPlacement < 1)
            {
                throw new ArgumentOutOfRangeException("nativePlantArraySizeAfterPlacement");
            }

            if (Stage != LevelOneTutorialStage.AwaitingPlantPlacement)
            {
                return Unchanged();
            }

            if (nativePlantArraySizeAfterPlacement < 2)
            {
                return Change(LevelOneTutorialStage.AwaitingUsableSeed, 200, null);
            }

            return Change(LevelOneTutorialStage.ZombieOnslaughtPrompt, null, 100);
        }

        /// <summary>
        /// Call only when the native-equivalent seed usability test has passed,
        /// including both recharge and affordability under ordinary play.
        /// </summary>
        public LevelOneTutorialEffect OnSeedBecameUsable()
        {
            if (Stage != LevelOneTutorialStage.AwaitingUsableSeed)
            {
                return Unchanged();
            }

            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        /// <summary>Performs unchanged.</summary>
        /// <returns>The resulting unchanged.</returns>
        private LevelOneTutorialEffect Unchanged()
        {
            return new LevelOneTutorialEffect(false, Stage, null, null);
        }

        /// <summary>Performs change.</summary>
        /// <param name = "next">The next.</param>
        /// <param name = "skySun">The sky sun.</param>
        /// <param name = "wave">The wave.</param>
        /// <returns>The resulting change.</returns>
        private LevelOneTutorialEffect Change(LevelOneTutorialStage next, int? skySun, int? wave)
        {
            Stage = next;
            return new LevelOneTutorialEffect(true, Stage, skySun, wave);
        }
    }
}
