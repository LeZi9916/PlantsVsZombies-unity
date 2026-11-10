#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun rules.</summary>
    public static class SkySunRules
    {
        /// <summary>Stores initial draw bound.</summary>
        public const int InitialDrawBound = 276;
        /// <summary>Stores xdraw bound.</summary>
        public const int XDrawBound = 550;
        /// <summary>Stores interval draw bound.</summary>
        public const int IntervalDrawBound = 275;
        /// <summary>Performs supports background.</summary>
        /// <param name = "background">The background.</param>
        /// <returns>The resulting supports background.</returns>
        public static bool SupportsBackground(int background)
        {
            return background != 1 && background != 3 && background != 5 && background != 6 && background != 8;
        }

        // Only call for a background on which the initializer writes the counter.
        /// <summary>Performs initialize.</summary>
        /// <param name = "background">The background.</param>
        /// <param name = "draw">The draw.</param>
        /// <returns>The resulting initialize.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public static SkySunState Initialize(int background, int draw)
        {
            if (!SupportsBackground(background))
            {
                throw new ArgumentException("This background does not initialize the sky counter.", nameof(background));
            }

            SunValidation.Draw(draw, InitialDrawBound);
            return new SkySunState(425 + draw, 0);
        }

        /// <summary>Performs can advance.</summary>
        /// <param name = "context">The context.</param>
        /// <returns>The resulting can advance.</returns>
        public static bool CanAdvance(SkySunContext context)
        {
            var mode = context.Mode;
            return context.UpdateEnabled && SupportsBackground(context.Background) && !context.BoardFlag5624 && context.BoardCountdown561C <= 0 && context.BoardCountdown5618 < 0 && mode != 19 && mode != 42 && mode != 71 && mode != 72 && mode != 23 && mode != 43 && mode != 50 && mode != 31 && !context.ExternalPredicateBlocked && context.TutorialState != 13 && ((context.TutorialState != 1 && context.TutorialState != 2) || context.PlantCount != 0);
        }

        /// <summary>Performs needs draws.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "context">The context.</param>
        /// <returns>The resulting needs draws.</returns>
        public static bool NeedsDraws(SkySunState state, SkySunContext context)
        {
            return CanAdvance(context) && state.Countdown == 1;
        }

        // Draw order at the scheduler is X then interval. Pickup construction can consume more draws.
        /// <summary>Performs tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "context">The context.</param>
        /// <param name = "xDraw">The x draw.</param>
        /// <param name = "intervalDraw">The interval draw.</param>
        /// <returns>The resulting tick.</returns>
        public static SkySunTick Tick(SkySunState state, SkySunContext context, int xDraw, int intervalDraw)
        {
            if (!CanAdvance(context))
            {
                return new SkySunTick(state, false, 0, SunKind.Normal);
            }

            var countdown = unchecked(state.Countdown - 1);
            if (countdown != 0)
            {
                return new SkySunTick(new SkySunState(countdown, state.Drops), false, 0, SunKind.Normal);
            }

            SunValidation.Draw(xDraw, XDrawBound);
            SunValidation.Draw(intervalDraw, IntervalDrawBound);
            var drops = checked(state.Drops + 1);
            var baseInterval = drops >= 53 ? 950 : 425 + 10 * drops;
            var next = new SkySunState(baseInterval + intervalDraw, drops);
            return new SkySunTick(next, true, 100 + xDraw, context.Mode == 37 ? SunKind.Large : SunKind.Normal);
        }
    }
}
