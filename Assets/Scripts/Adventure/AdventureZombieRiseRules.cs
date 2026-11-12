#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines the original PC grave ambush's underground delay, ascent, and clipping.</summary>
    public static class AdventureZombieRiseRules
    {
        /// <summary>The original land zombie's complete grave phase in native updates.</summary>
        public const int DurationTicks = 150;

        /// <summary>Gets the original altitude sampled by PC Zombie::UpdateZombieRiseFromGrave at 0x544970.</summary>
        /// <param name="remainingTicks">The remaining native grave-phase updates.</param>
        /// <returns>The altitude in board pixels, from minus two hundred underground to zero above ground.</returns>
        public static float Altitude(int remainingTicks)
        {
            return -4f * Math.Max(0, Math.Min(50, remainingTicks));
        }

        /// <summary>Gets the original PC GetDrawPos and DrawReanim clipping boundary.</summary>
        /// <param name="nativePositionY">The zombie's original unscaled vertical position.</param>
        /// <param name="remainingTicks">The remaining native grave-phase updates.</param>
        /// <returns>The board-space bottom edge below which the emerging body is hidden.</returns>
        public static float ClipBottom(float nativePositionY, int remainingTicks)
        {
            // PC 0x54990C: clipHeight=-altitude+min(counter,40); 0x548DAA: 120+71-clipHeight.
            return nativePositionY + 113f - Math.Max(0, Math.Min(40, remainingTicks));
        }
    }
}
