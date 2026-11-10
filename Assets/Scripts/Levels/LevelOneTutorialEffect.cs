#nullable enable

namespace PlantsVsZombies.Levels
{
    /// <summary>
    /// A transition and any native counters explicitly reset by that transition.
    /// Null means no reset, not that the counter is zero or absent.
    /// </summary>
    public struct LevelOneTutorialEffect
    {
        /// <summary>Stores changed.</summary>
        public readonly bool Changed;
        /// <summary>Stores stage.</summary>
        public readonly LevelOneTutorialStage Stage;
        /// <summary>Stores sky sun countdown native ticks.</summary>
        public readonly int? SkySunCountdownNativeTicks;
        /// <summary>Stores wave countdown and initial native ticks.</summary>
        public readonly int? WaveCountdownAndInitialNativeTicks;
        /// <summary>Initializes level one tutorial effect.</summary>
        /// <param name = "changed">The changed.</param>
        /// <param name = "stage">The stage.</param>
        /// <param name = "skySun">The sky sun.</param>
        /// <param name = "wave">The wave.</param>
        internal LevelOneTutorialEffect(bool changed, LevelOneTutorialStage stage, int? skySun, int? wave)
        {
            Changed = changed;
            Stage = stage;
            SkySunCountdownNativeTicks = skySun;
            WaveCountdownAndInitialNativeTicks = wave;
        }
    }
}
