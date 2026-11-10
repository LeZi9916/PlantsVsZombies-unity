#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun fall.</summary>
    public static class SkySunFall
    {
        /// <summary>Stores landing draw bound.</summary>
        public const int LandingDrawBound = 250;
        /// <summary>Stores vertical speed.</summary>
        public const float VerticalSpeed = 0.67f;
        // This draw occurs in pickup construction, not necessarily immediately after the scheduler's draws.
        /// <summary>Performs initialize.</summary>
        /// <param name = "spawn">The spawn.</param>
        /// <param name = "landingDraw">The landing draw.</param>
        /// <returns>The resulting initialize.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public static SkySunFallState Initialize(SkySunTick spawn, int landingDraw)
        {
            if (!spawn.Spawned || spawn.MotionType != 0)
            {
                throw new ArgumentException("A sky spawn is required.", nameof(spawn));
            }

            SunValidation.Draw(landingDraw, LandingDrawBound);
            return new SkySunFallState(new SunPickupState(spawn.Kind, spawn.X, spawn.Y), 300 + landingDraw);
        }

        /// <summary>Performs tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "balance">The balance.</param>
        /// <param name = "updateEnabled">The update enabled.</param>
        /// <param name = "ageEnabled">The age enabled.</param>
        /// <param name = "extendedLifetime">The extended lifetime.</param>
        /// <param name = "mode">The mode.</param>
        /// <returns>The resulting tick.</returns>
        public static SkySunFallTick Tick(SkySunFallState state, int balance, bool updateEnabled, bool ageEnabled, bool extendedLifetime, int mode)
        {
            var pickup = state.Pickup;
            var ground = false;
            if (updateEnabled && !pickup.Dead && !pickup.Collecting && pickup.FadeCountdown == 0)
            {
                var nextY = (double)pickup.Y + VerticalSpeed;
                ground = nextY >= state.LandingY;
                pickup = pickup.WithPosition(pickup.X, ground ? state.LandingY : (float)nextY);
            }

            var tick = SunPickupRules.Tick(pickup, balance, updateEnabled, ground, ageEnabled, extendedLifetime, mode);
            return new SkySunFallTick(new SkySunFallState(tick.State, state.LandingY), tick, ground);
        }
    }
}
