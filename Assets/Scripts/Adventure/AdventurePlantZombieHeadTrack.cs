#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Samples original Zombie.reanim anim_head1 translations for PC plant-zombie projectile launch positions.</summary>
    internal static class AdventurePlantZombieHeadTrack
    {
        /// <summary>Stores literal original authored frame translations for each native body marker.</summary>
        private static readonly Dictionary<string, float[]> s_frames = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["anim_superlongdeath"] = new float[] { 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f },
            ["anim_swim"] = new float[] { 6.7f, 8.8f, 8.3f, 7.8f, 10f, 6.8f, 11.7f, 6f, 13.4f, 5f, 15.2f, 3.9f, 16.8f, 3f, 18.5f, 2f, 20.3f, 1.2f, 22f, 0.1f, 19.3f, 2.3f, 16.7f, 4.5f, 14f, 6.7f, 11.4f, 8.8f, 8.9f, 11f, 6.3f, 13.2f, 3.8f, 15.5f, 3.6f, 14.4f, 3.4f, 13.6f, 4.4f, 12.3f, 5.5f, 10.9f, 6.5f, 9.7f, 7.6f, 8.6f, 8.7f, 7.3f, 10.2f, 6.2f, 11.8f, 5f, 13.3f, 4f, 14.8f, 2.9f, 16.4f, 1.7f, 17.9f, 0.7f, 19.6f, -0.3f, 21.1f, -1.3f, 22.7f, -2.5f, 20.4f, -0.4f, 18f, 1.5f, 15.7f, 3.6f, 13.4f, 5.8f, 11.1f, 7.9f, 8.8f, 10.1f, 6.6f, 12.2f, 6.7f, 10.5f, 6.7f, 8.8f },
            ["anim_death2"] = new float[] { 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f },
            ["anim_death"] = new float[] { 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f },
            ["anim_walk2"] = new float[] { 6.7f, 8.8f, 7.1f, 8.4f, 7.5f, 8f, 8f, 7.7f, 8.4f, 7.3f, 8.8f, 6.8f, 9.2f, 6.5f, 9.7f, 6.1f, 10.1f, 5.7f, 10.5f, 5.3f, 9.6f, 6.3f, 8.6f, 7.3f, 7.7f, 8.3f, 6.7f, 9.4f, 5.7f, 10.4f, 4.8f, 11.4f, 3.8f, 12.5f, 3.5f, 12.9f, 3.4f, 13.6f, 3.9f, 12.6f, 4.4f, 11.6f, 5f, 10.6f, 5.5f, 9.7f, 6f, 8.8f, 6.2f, 8.2f, 6.3f, 7.5f, 6.6f, 6.8f, 6.8f, 6.2f, 7f, 5.5f, 7.2f, 4.8f, 7.5f, 4.1f, 7.7f, 3.5f, 8f, 2.8f, 7.1f, 3.7f, 6.3f, 4.6f, 5.6f, 5.5f, 4.8f, 6.4f, 4.1f, 7.3f, 3.3f, 8.3f, 2.6f, 9.2f, 2.6f, 10.7f, 2.6f, 12.1f, 3.5f, 11.4f, 4.3f, 10.7f, 5.1f, 10.1f, 5.9f, 9.4f, 6.7f, 8.8f },
            ["anim_walk"] = new float[] { 6.7f, 8.8f, 8.5f, 6.1f, 10.4f, 3.6f, 12.4f, 1.2f, 14.5f, -1.1f, 15.9f, -1.6f, 17.2f, -2.1f, 18.5f, -2.5f, 19.9f, -3f, 20.8f, -3.5f, 21.7f, -3.9f, 22.7f, -4.4f, 23.7f, -5f, 22.8f, -3.8f, 22f, -2.7f, 21.2f, -1.5f, 20.2f, -0.4f, 15.6f, 2f, 11.2f, 4.6f, 6.8f, 7.6f, 2.6f, 10.7f, 1.7f, 10.5f, 0.9f, 9.9f, 2.5f, 6.2f, 4.5f, 2.7f, 6.8f, 1.8f, 9.2f, 1f, 11.6f, 0f, 13.9f, -0.7f, 16.6f, -1.6f, 19.1f, -2.5f, 21.9f, -3.5f, 24.6f, -4.5f, 25.3f, -4.7f, 26.2f, -4.8f, 26.8f, -4.9f, 27.6f, -5.2f, 21.9f, -2.9f, 16.3f, -0.5f, 10.7f, 2f, 5.5f, 4.8f, 5.6f, 5.5f, 5.8f, 6.2f, 6f, 6.9f, 6.1f, 7.6f, 6.3f, 8.3f, 6.7f, 8.8f },
            ["anim_idle2"] = new float[] { 6.7f, 8.8f, 6.2f, 9.3f, 5.8f, 9.7f, 5.3f, 10.1f, 5f, 10.5f, 4.5f, 11f, 4.1f, 11.4f, 3.6f, 11.8f, 2.8f, 13.3f, 1.9f, 14.7f, 2.9f, 13.5f, 3.8f, 12.3f, 4.8f, 11.1f, 5.8f, 10f, 6.7f, 8.8f },
            ["anim_idle"] = new float[] { 6.7f, 8.8f, 8.5f, 7.4f, 10.3f, 6f, 12.1f, 4.6f, 14f, 3.1f, 16f, 3.4f, 17.9f, 3.7f, 19.8f, 4f, 21.8f, 4.3f, 23.9f, 4.6f, 25.9f, 4.9f, 25.9f, 4f, 25.9f, 3f, 23.3f, 2.8f, 20.8f, 2.5f, 18.2f, 2.4f, 15.8f, 2f, 13.3f, 4.2f, 10.8f, 6.3f, 8.5f, 8.6f, 6.1f, 10.8f, 3.6f, 13f, 1.5f, 15.4f, 1.2f, 16f, 0.9f, 16.5f, 2.3f, 14.5f, 3.6f, 12.6f, 5.2f, 10.6f, 6.7f, 8.8f },
            ["anim_eat"] = new float[] { 6.7f, 8.8f, 7.3f, 8.4f, 8.1f, 8f, 8.7f, 7.7f, 9.4f, 7.3f, 8.2f, 7.8f, 7f, 8.3f, 5.8f, 9f, 5.2f, 9.6f, 4.6f, 10.1f, 4f, 10.7f, 3.3f, 11.4f, 2.6f, 11.9f, 2f, 12.6f, 2.8f, 11.9f, 3.5f, 11.3f, 4.3f, 10.7f, 5.1f, 10f, 5.9f, 9.4f, 6.7f, 8.8f, 7.2f, 8.5f, 7.8f, 8.2f, 8.3f, 7.9f, 8.9f, 7.6f, 9.4f, 7.3f, 8.2f, 7.8f, 7f, 8.3f, 5.8f, 9f, 5.2f, 9.6f, 4.6f, 10.1f, 4f, 10.7f, 3.3f, 11.4f, 2.6f, 11.9f, 2f, 12.6f, 2.8f, 11.9f, 3.5f, 11.3f, 4.3f, 10.7f, 5.1f, 10f, 5.9f, 9.4f, 6.7f, 8.8f },
            ["anim_waterdeath"] = new float[] { 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f, 6.7f, 8.8f },
            ["anim_dance"] = new float[] { 20.9f, 1.8f, 21f, 1.7f, 21.1f, 1.6f, 21.1f, 1.6f, 21.1f, 1.6f, 21.3f, 1.5f, 21.3f, 1.5f, 21.4f, 1.4f, 21.5f, 1.3f, 21.6f, 1.3f, 21.6f, 1.2f, 21.8f, 1.1f, 21.9f, 1.1f, 21.4f, 1.2f, 20.9f, 1.3f, 20.5f, 1.4f, 20f, 1.5f, 19.4f, 1.6f, 19f, 1.7f, 18.5f, 1.8f, 18f, 1.9f, 17.6f, 2f, 17.2f, 1.9f, 16.7f, 2.1f, 16.2f, 2.1f, 15.6f, 2.3f, 16.2f, 2.2f, 16.8f, 2f, 17.3f, 2f, 17.7f, 1.9f, 18.2f, 1.8f, 18.8f, 1.8f, 19.3f, 1.7f, 19.9f, 1.5f, 20.4f, 1.5f, 21f, 1.4f, 21.5f, 1.3f, 22.1f, 1.1f, 21.9f, 1.3f, 21.9f, 1.2f, 21.8f, 1.3f, 21.7f, 1.3f, 21.5f, 1.4f, 21.4f, 1.4f, 21.3f, 1.5f, 21.2f, 1.6f, 21.1f, 1.6f, 21.1f, 1.6f, 21f, 1.7f, 20.9f, 1.8f }
        };

        /// <summary>Reads the original unscaled head bone using the native looping body phase.</summary>
        /// <param name="clip">The actual original body animation.</param>
        /// <param name="seconds">The simulation-owned body animation age.</param>
        /// <param name="rate">The original body animation rate.</param>
        /// <param name="x">Receives the authored head-bone x coordinate.</param>
        /// <param name="y">Receives the authored head-bone y coordinate.</param>
        /// <exception cref="InvalidOperationException">The requested marker is absent from the original definition.</exception>
        internal static void Sample(string clip, float seconds, float rate, out float x, out float y)
        {
            if (!s_frames.TryGetValue(clip, out var data))
            {
                throw new InvalidOperationException("The original plant-zombie body marker is unavailable: " + clip);
            }
            var count = data.Length / 2;
            var phase = Math.Max(0d, seconds) * Math.Max(0d, rate) / count;
            phase -= Math.Floor(phase);
            var frame = phase * (count - 1);
            var first = (int)frame;
            var next = Math.Min(first + 1, count - 1);
            var fraction = (float)(frame - first);
            x = data[first * 2] + (data[next * 2] - data[first * 2]) * fraction;
            y = data[first * 2 + 1] + (data[next * 2 + 1] - data[first * 2 + 1]) * fraction;
        }
    }
}
