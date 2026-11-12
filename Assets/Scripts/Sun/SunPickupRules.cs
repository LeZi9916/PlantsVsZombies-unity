#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun pickup rules.</summary>
    public static class SunPickupRules
    {
        /// <summary>Stores balance cap.</summary>
        public const int BalanceCap = 9990;
        /// <summary>Stores fade ticks.</summary>
        public const int FadeTicks = 15;
        /// <summary>Performs value.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting value.</returns>
        public static int Value(SunKind kind)
        {
            SunValidation.Kind(kind);
            return kind == SunKind.Small ? 15 : kind == SunKind.Large ? 50 : 25;
        }

        // The EXE uses a 60x60 base rectangle; mode-specific expansion is supplied explicitly.
        /// <summary>Performs hit test.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "mouseX">The mouse x.</param>
        /// <param name = "mouseY">The mouse y.</param>
        /// <param name = "specialExpansion">The special expansion.</param>
        /// <returns>The resulting hit test.</returns>
        public static bool HitTest(SunPickupState state, int mouseX, int mouseY, bool specialExpansion)
        {
            SunValidation.Kind(state.Kind);
            if (state.Dead || state.Collecting)
            {
                return false;
            }

            var margin = state.Kind == SunKind.Normal || specialExpansion ? 15 : 0;
            var extraBottom = specialExpansion ? 30 : 0;
            float x = mouseX;
            float y = mouseY;
            return x >= (double)state.X - margin && x < (double)state.X + 60 + margin && y >= (double)state.Y - margin && y < (double)state.Y + 60 + margin + extraBottom;
        }

        // This includes the mouse-dispatch guards, not overlapping-item selection or input routing.
        /// <summary>Performs try click.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "mouseX">The mouse x.</param>
        /// <param name = "mouseY">The mouse y.</param>
        /// <param name = "button">The button.</param>
        /// <param name = "gameplayScene">The gameplay scene.</param>
        /// <param name = "boardPaused">The board paused.</param>
        /// <param name = "specialExpansion">The special expansion.</param>
        /// <param name = "result">The result.</param>
        /// <returns>The resulting try click.</returns>
        public static bool TryClick(SunPickupState state, int mouseX, int mouseY, int button, bool gameplayScene, bool boardPaused, bool specialExpansion, out SunPickupState result)
        {
            result = state;
            if (!gameplayScene || boardPaused || button < 0 || !HitTest(state, mouseX, mouseY, specialExpansion))
            {
                return false;
            }

            result = new SunPickupState(state.Kind, state.X, state.Y, state.GroundAge, 0, true);
            return true;
        }

        // groundBranch is the result of the EXE's movement/landing branch for this update.
        // ageEnabled represents its mode-31/board guard; extendedLifetime is pickup byte +0xC9.
        /// <summary>Performs tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "balance">The balance.</param>
        /// <param name = "updateEnabled">The update enabled.</param>
        /// <param name = "groundBranch">The ground branch.</param>
        /// <param name = "ageEnabled">The age enabled.</param>
        /// <param name = "extendedLifetime">The extended lifetime.</param>
        /// <param name = "mode">The mode.</param>
        /// <returns>The resulting tick.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static SunPickupTick Tick(SunPickupState state, int balance, bool updateEnabled, bool groundBranch, bool ageEnabled, bool extendedLifetime, int mode)
        {
            SunValidation.Kind(state.Kind);
            if (balance < 0 || balance > BalanceCap)
            {
                throw new ArgumentOutOfRangeException(nameof(balance));
            }

            if (!updateEnabled || state.Dead)
            {
                return new SunPickupTick(state, balance, 0, false);
            }

            if (state.FadeCountdown != 0)
            {
                var fade = state.FadeCountdown - 1;
                return new SunPickupTick(new SunPickupState(state.Kind, state.X, state.Y, state.GroundAge, fade, false, fade == 0), balance, 0, false);
            }

            if (state.Collecting)
            {
                // Native code measures the pre-move distance, stores float intermediates,
                // then moves each coordinate 1/21 of the distance towards (15, 0).
                var dx = Math.Abs(state.X - 15.0f);
                var dy = Math.Abs(state.Y);
                var x = state.X > 15.0f ? (float)(state.X - (double)dx / 21.0) : state.X < 15.0f ? (float)(state.X + (double)dx / 21.0) : state.X;
                var y = state.Y > 0.0f ? (float)(state.Y - (double)dy / 21.0) : state.Y < 0.0f ? (float)(state.Y + (double)dy / 21.0) : state.Y;
                var squared = (float)((double)dx * dx + (double)dy * dy);
                var distance = (float)Math.Sqrt(squared);
                var credited = distance < 8.0f;
                var nextBalance = credited ? Math.Min(BalanceCap, balance + Value(state.Kind)) : balance;
                var next = new SunPickupState(state.Kind, x, y, state.GroundAge, 0, true, credited);
                return new SunPickupTick(next, nextBalance, nextBalance - balance, credited);
            }

            if (!groundBranch || !ageEnabled)
            {
                return new SunPickupTick(state, balance, 0, false);
            }

            var age = checked(state.GroundAge + 1);
            var lifetime = mode == 43 ? 6000 : extendedLifetime ? 1500 : 750;
            var fadeCountdown = age >= lifetime ? FadeTicks : 0;
            return new SunPickupTick(new SunPickupState(state.Kind, state.X, state.Y, age, fadeCountdown), balance, 0, false);
        }
    }
}
