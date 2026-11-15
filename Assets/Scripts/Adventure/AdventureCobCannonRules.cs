#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines the original PC 46F3F0/475230/472C70 cannon animation milestones.</summary>
    internal static class AdventureCobCannonRules
    {
        /// <summary>The fifteen charge frames at the original twelve frames per second.</summary>
        internal const float LoadingSeconds = 15f / 12f;
        /// <summary>The forty-two shooting frames at the original twelve frames per second.</summary>
        internal const float FiringSeconds = 42f / 12f;
        /// <summary>PC 4731AE passes twenty native updates to PlayReanim when returning to the unarmed idle.</summary>
        internal const int ReturnBlendTicks = 20;
        /// <summary>The original 206 shooting counter emits when its decremented value reaches one.</summary>
        internal const float ReleaseSeconds = 2.05f;

        /// <summary>Samples PC GetFlashingColor's grayscale multiplier for the ready cob track only.</summary>
        /// <param name="ticks">The board's native main counter.</param>
        /// <returns>The original integer color component from 55 through 255.</returns>
        internal static int ReadyBrightness(int ticks)
        {
            return Math.Min(255, 55 + Math.Abs(37 - ticks % 75) * 200 / 37);
        }
    }
}
