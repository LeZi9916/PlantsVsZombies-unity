#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    // Local PC: 46C07A..46C0A4 (10 + Rand(5)), 46C624..46C645
    // (anim_explode, PlayOnceAndHold, special=100, reverse-explosion Foley 57).
    /// <summary>Defines adventure cherry bomb state.</summary>
    public sealed class AdventureCherryBombState
    {
        /// <summary>Stores fuse ticks.</summary>
        public const int FuseTicks = 100;
        /// <summary>Stores explode frame count.</summary>
        public const int ExplodeFrameCount = 14;
        /// <summary>Stores animation.</summary>
        public const string Animation = "anim_explode";
        /// <summary>Gets or sets countdown.</summary>
        public int Countdown { get; internal set; } = FuseTicks;
        /// <summary>Gets or sets animation ticks.</summary>
        public int AnimationTicks { get; internal set; }
        /// <summary>Gets or sets rate.</summary>
        public float Rate { get; internal set; }
        /// <summary>Gets or sets normalized time.</summary>
        public float NormalizedTime { get; internal set; }
        /// <summary>Gets or sets shake x.</summary>
        public float ShakeX { get; internal set; }
        /// <summary>Gets or sets shake y.</summary>
        public float ShakeY { get; internal set; }
        /// <summary>Gets or sets detonated.</summary>
        public bool Detonated { get; internal set; }

        /// <summary>Performs sample seconds.</summary>
        /// <param name = "remainder">The remainder.</param>
        /// <returns>The resulting sample seconds.</returns>
        public float SampleSeconds(float remainder)
        {
            if (Rate <= 0)
            {
                return 0;
            }

            // PC PlayOnceAndHold clamps mAnimTime to .999999, and samples
            // frameStart + mAnimTime * (frameCount - 1), not frameCount * time.
            double phase = Math.Min(.999999f, NormalizedTime + Math.Max(0f, remainder) * Rate / ExplodeFrameCount);
            return (float)(phase * ExplodeFrameCount / Rate);
        }
    }
}
