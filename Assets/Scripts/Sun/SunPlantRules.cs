#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun plant rules.</summary>
    public static class SunPlantRules
    {
        /// <summary>Stores initial draw bound.</summary>
        public const int InitialDrawBound = 951;
        /// <summary>Stores period draw bound.</summary>
        public const int PeriodDrawBound = 151;
        /// <summary>Performs initialize.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "draw">The draw.</param>
        /// <returns>The resulting initialize.</returns>
        public static SunPlantState Initialize(SunPlantKind kind, int draw)
        {
            ValidateKind(kind);
            SunValidation.Draw(draw, InitialDrawBound);
            return new SunPlantState(kind, 300 + draw);
        }

        // enabled aggregates the plant-update, production and special-mode predicates.
        /// <summary>Performs needs draw.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "enabled">The enabled.</param>
        /// <returns>The resulting needs draw.</returns>
        public static bool NeedsDraw(SunPlantState state, bool enabled)
        {
            ValidateKind(state.Kind);
            return enabled && unchecked(state.Countdown - 1) <= 0;
        }

        /// <summary>Performs tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "enabled">The enabled.</param>
        /// <param name = "mode">The mode.</param>
        /// <param name = "plantPhase">The plant phase.</param>
        /// <param name = "draw">The draw.</param>
        /// <returns>The resulting tick.</returns>
        public static SunPlantTick Tick(SunPlantState state, bool enabled, int mode, int plantPhase, int draw)
        {
            ValidateKind(state.Kind);
            if (!enabled)
            {
                return new SunPlantTick(state, 0, SunKind.Normal);
            }

            var countdown = unchecked(state.Countdown - 1);
            if (countdown > 0)
            {
                return new SunPlantTick(new SunPlantState(state.Kind, countdown), 0, SunKind.Normal);
            }

            SunValidation.Draw(draw, PeriodDrawBound);
            var next = new SunPlantState(state.Kind, 2350 + draw);
            var kind = state.Kind == SunPlantKind.SunShroom && plantPhase == 23 ? SunKind.Small : SunKind.Normal;
            var count = state.Kind == SunPlantKind.TwinSunflower || (state.Kind == SunPlantKind.Sunflower && mode == 39) ? 2 : 1;
            return new SunPlantTick(next, count, kind);
        }

        /// <summary>Performs validate kind.</summary>
        /// <param name = "kind">The kind.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal static void ValidateKind(SunPlantKind kind)
        {
            if (kind != SunPlantKind.Sunflower && kind != SunPlantKind.SunShroom && kind != SunPlantKind.TwinSunflower)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
