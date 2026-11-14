#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original coffee bean's independent special countdown and authored crumble playback.</summary>
    public sealed class AdventureCoffeeBeanState
    {
        /// <summary>Stores original idle updates remaining before coffee starts the mushroom's separate wake counter.</summary>
        public int Countdown = 100;
        /// <summary>Identifies PlantState.DoingSpecial after the original coffee special has fired.</summary>
        public bool Crumbling;
        /// <summary>Stores elapsed native reanimation updates, including fractional slowed playback while lifted.</summary>
        public float AnimationTicks;
        /// <summary>Defines the unchanged original crumble layer's frame count.</summary>
        internal const int CrumbleFrames = 15;
        /// <summary>Defines the original crumble playback rate in frames per second.</summary>
        internal const float CrumbleRate = 22f;

        /// <summary>Samples PC 471F22's original wake squish/stretch without moving the plant's foot anchor.</summary>
        /// <param name="remaining">The mushroom's original remaining wake updates.</param>
        /// <returns>The native vertical scale multiplier, or one outside the last seventy updates.</returns>
        internal static float WakeScale(int remaining)
        {
            if (remaining <= 0 || remaining >= 70)
            {
                return 1f;
            }

            var time = (70f - remaining) / 70f;
            var eased = time * time * (3f - 2f * time);
            var wave = (float)Math.Sin(eased * (float)(Math.PI * 2d));
            return (float)(1d - .2d * wave);
        }
    }
}
