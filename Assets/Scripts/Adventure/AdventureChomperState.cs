#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Stores the original PC chomper's bite, digestion, and swallow animation state.</summary>
    public sealed class AdventureChomperState
    {
        /// <summary>Stores the original plant state: idle 1, bite 10, success 11, miss 12, chew 13, swallow 14.</summary>
        public int NativePhase = 1;
        /// <summary>Stores elapsed native animation ticks and the active native countdown.</summary>
        public int PhaseTicks, Countdown;
        /// <summary>Stores the animation selected by the original state transition.</summary>
        public string Animation = "anim_idle";
        /// <summary>Stores the original animation frame rate, with zero selecting the randomized idle rate.</summary>
        public float Rate;
        /// <summary>Gets whether the current original animation repeats.</summary>
        public bool Loop => NativePhase == 1 || NativePhase == 13;
        /// <summary>Gets elapsed time within the current original animation.</summary>
        public float AnimationAge => PhaseTicks * .01f;
    }
}
