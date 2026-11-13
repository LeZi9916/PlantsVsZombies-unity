#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares PC sled layering, authored boarding progress, damage artwork, and debris motion.</summary>
    internal static class AdventureBobsledRules
    {
        /// <summary>Returns the original pushing height assigned to one team member.</summary>
        /// <param name="position">The front-to-back team position.</param>
        /// <returns>The original unscaled altitude in board pixels.</returns>
        internal static float InitialAltitude(int position)
        {
            return position == 0 ? -10f : position == 2 ? -7f : 9f;
        }

        /// <summary>Resolves the team's original render offsets in back-to-front order.</summary>
        /// <param name="order">The zero-based painter order within the team.</param>
        /// <returns>The member position to draw at this layer.</returns>
        internal static int PositionAtRenderOrder(int order)
        {
            return order == 0 ? 3 : order == 1 ? 1 : order == 2 ? 2 : 0;
        }

        /// <summary>Samples the original twenty-fps, twenty-four-frame jump's normalized time.</summary>
        /// <param name="seconds">The elapsed authored jump time.</param>
        /// <returns>The clamped native animation progress.</returns>
        internal static float BoardingProgress(float seconds)
        {
            return Math.Max(0f, Math.Min(1f, seconds * 20f / 24f));
        }

        /// <summary>Selects which parts of the original sled surround a member's body.</summary>
        /// <param name="position">The front-to-back member index.</param>
        /// <param name="phase">The original push, boarding, or crashing phase.</param>
        /// <param name="progress">The normalized boarding animation time.</param>
        /// <param name="beforeBody">Whether this is the pass behind the member's body.</param>
        /// <returns>Bit one for the front shell and bit two for the inside artwork.</returns>
        internal static int DrawLayers(int position, int phase, float progress, bool beforeBody)
        {
            if (phase == 19)
            {
                return position == 0 && !beforeBody ? 1 : 0;
            }

            if (phase == 17 || phase == 18 && progress < .5f)
            {
                return position == 2 && beforeBody ? 3 : 0;
            }

            if (phase == 18)
            {
                return position == 0 && !beforeBody ? 1 : position == 3 && beforeBody ? 2 : 0;
            }

            return 0;
        }

        /// <summary>Returns the original final thirty-update debris transparency.</summary>
        /// <param name="ticks">The remaining native crash updates.</param>
        /// <returns>The original integer alpha normalized to zero through one.</returns>
        internal static float CrashOpacity(int ticks)
        {
            return Math.Max(0, Math.Min(30, ticks)) * 255 / 30 / 255f;
        }

        /// <summary>Samples the original crash shell's horizontal easing relative to its walking leader.</summary>
        /// <param name="ticks">The remaining crash updates.</param>
        /// <param name="velocity">The retained original horizontal velocity.</param>
        /// <returns>The shell's original horizontal displacement in board pixels.</returns>
        internal static float CrashOffsetX(int ticks, float velocity)
        {
            var elapsed = Math.Max(0f, Math.Min(150f, 150f - ticks));
            var progress = elapsed / 150f;
            return elapsed * velocity * .5f - 50f * progress * (2f - progress);
        }

        /// <summary>Samples the original first-seventy-five-update debris drop.</summary>
        /// <param name="ticks">The remaining crash updates.</param>
        /// <returns>The additional downward displacement in board pixels.</returns>
        internal static float CrashOffsetY(int ticks)
        {
            return 5f + 5f * Math.Max(0f, Math.Min(1f, (150f - ticks) / 75f));
        }
    }
}
