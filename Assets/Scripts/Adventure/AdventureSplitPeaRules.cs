#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Samples the original Split Pea body anchor used by both projectile muzzles.</summary>
    internal static class AdventureSplitPeaRules
    {
        /// <summary>Stores the original SplitPea.reanim anim_idle horizontal translations, frames 7 through 31.</summary>
        private static readonly float[] s_headX =
        {
            37.6f, 38f, 38.3f, 38.6f, 39.1f, 39.6f, 40.1f, 41.1f, 42.1f,
            43.2f, 44.6f, 46f, 47.4f, 46f, 44.6f, 43.2f, 42.1f, 41.1f,
            40.1f, 39.6f, 39.1f, 38.6f, 38.3f, 38f, 37.6f
        };

        /// <summary>Stores the original SplitPea.reanim anim_idle vertical translations, frames 7 through 31.</summary>
        private static readonly float[] s_headY =
        {
            48.7f, 47.1f, 45.5f, 43.8f, 43.5f, 43.1f, 42.8f, 43.1f, 43.5f,
            43.8f, 44.6f, 45.5f, 46.3f, 45.5f, 44.6f, 43.8f, 43.5f, 43.1f,
            42.8f, 43.1f, 43.5f, 43.8f, 45.5f, 47.1f, 48.7f
        };

        /// <summary>Gets the integer body-track translation read by PC Plant::GetPeaHeadOffset at 0x4737D0.</summary>
        /// <param name="seconds">The body animation clock shared with the attached heads.</param>
        /// <param name="rate">The body's randomized native animation frame rate.</param>
        /// <param name="x">The original horizontal anchor translation, truncated toward zero.</param>
        /// <param name="y">The original vertical anchor translation, truncated toward zero.</param>
        internal static void HeadOffset(float seconds, float rate, out int x, out int y)
        {
            // Native Loop samples (frameCount - 1) * normalized time without a last-to-first blend.
            var phase = Math.Max(0d, seconds) * Math.Max(0d, rate) / s_headX.Length;
            phase -= Math.Floor(phase);
            var frame = phase * (s_headX.Length - 1);
            var current = (int)frame;
            var next = Math.Min(current + 1, s_headX.Length - 1);
            var fraction = (float)(frame - current);
            x = (int)(s_headX[current] + (s_headX[next] - s_headX[current]) * fraction);
            y = (int)(s_headY[current] + (s_headY[next] - s_headY[current]) * fraction);
        }
    }
}
