#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC Doom-shroom's awake pre-explosion animation (Plant::UpdateDoomShroom, 0x46F1A0).</summary>
    public sealed class AdventureDoomShroomState
    {
        /// <summary>Defines the original exploding body's animation rate.</summary>
        public const float Rate = 23f;
        /// <summary>Defines the number of frames in the original anim_explode layer.</summary>
        private const int FrameCount = 33;
        /// <summary>Gets the native special countdown, initialized when the mushroom first updates awake.</summary>
        public int Countdown { get; internal set; } = 100;
        /// <summary>Gets the number of native animation updates since the exploding layer started.</summary>
        public int AnimationTicks { get; internal set; }
        /// <summary>Stores each head track's current horizontal shake, in native track order.</summary>
        public readonly float[] HeadShakeX = new float[3];
        /// <summary>Stores each head track's current vertical shake, in native track order.</summary>
        public readonly float[] HeadShakeY = new float[3];

        /// <summary>Samples the original exploding body without advancing its simulation clock.</summary>
        /// <param name="remainder">The nonnegative render fraction after the latest native tick.</param>
        /// <returns>The animation playback time, clamped to the original PlayOnceAndHold endpoint.</returns>
        public float SampleSeconds(float remainder)
        {
            return Math.Min(.999999f * FrameCount / Rate, AnimationTicks * .01f + Math.Max(0f, remainder));
        }
    }
}
