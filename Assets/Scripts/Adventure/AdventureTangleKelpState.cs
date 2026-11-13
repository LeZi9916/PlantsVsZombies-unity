#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC kelp's hundred-update grab and its original zombie attachment.</summary>
    public sealed class AdventureTangleKelpState
    {
        /// <summary>Stores the remaining original plant-state updates.</summary>
        public int Countdown = 100;
        /// <summary>Stores the stable identifier of the grabbed zombie.</summary>
        public int TargetZombieId;
        /// <summary>Stores the original unscaled attachment offsets from the zombie's native position.</summary>
        public int OffsetX, OffsetY;

        /// <summary>Samples the original once-and-hold grab without changing the native countdown.</summary>
        /// <param name="remainder">The nonnegative time since the last simulation update.</param>
        /// <returns>The attachment's elapsed animation time in seconds.</returns>
        public float SampleSeconds(float remainder)
        {
            return (100 - Countdown) * .01f + Math.Max(0f, remainder);
        }
    }
}
