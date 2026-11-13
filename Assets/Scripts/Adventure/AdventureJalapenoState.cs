#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC Jalapeno's independent hundred-update fuse, original body playback, and shake.</summary>
    public sealed class AdventureJalapenoState
    {
        /// <summary>The original special countdown, independent of animation completion.</summary>
        public const int FuseTicks = 100;
        /// <summary>The original Jalapeno.reanim anim_explode frame count, frames twelve through twenty-four.</summary>
        public const int ExplodeFrameCount = 13;
        /// <summary>The authored body layer selected by the original plant initialization.</summary>
        public const string Animation = "anim_explode";
        /// <summary>Gets or sets the native updates remaining before burning the row.</summary>
        public int Countdown { get; internal set; } = FuseTicks;
        /// <summary>Gets or sets updates elapsed since the original fuse animation started.</summary>
        public int AnimationTicks { get; internal set; }
        /// <summary>Gets or sets the PC generic body rate of ten plus a random value up to five.</summary>
        public float Rate { get; internal set; }
        /// <summary>Gets or sets the original normalized once-and-hold animation position.</summary>
        public float NormalizedTime { get; internal set; }
        /// <summary>Gets or sets the native per-update horizontal shake offset.</summary>
        public float ShakeX { get; internal set; }
        /// <summary>Gets or sets the native per-update vertical shake offset.</summary>
        public float ShakeY { get; internal set; }
        /// <summary>Gets or sets whether the row special has already run.</summary>
        public bool Detonated { get; internal set; }

        /// <summary>Samples the PC once-and-hold body without changing its fixed simulation clock.</summary>
        /// <param name="remainder">The nonnegative interpolation remainder since the latest native update.</param>
        /// <returns>The elapsed animation seconds accepted by native playback.</returns>
        public float SampleSeconds(float remainder)
        {
            if (Rate <= 0f)
            {
                return 0f;
            }

            var phase = Math.Min(.999999f, NormalizedTime + Math.Max(0f, remainder) * Rate / ExplodeFrameCount);
            return phase * ExplodeFrameCount / Rate;
        }
    }
}
