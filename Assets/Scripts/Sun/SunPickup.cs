using System;

namespace PvZUnity.Sun
{
    public readonly struct SunPickupState
    {
        public SunKind Kind { get; }
        public float X { get; }
        public float Y { get; }
        public int GroundAge { get; }
        public int FadeCountdown { get; }
        public bool Collecting { get; }
        public bool Dead { get; }

        public SunPickupState(SunKind kind, float x, float y, int groundAge = 0,
            int fadeCountdown = 0, bool collecting = false, bool dead = false)
        {
            SunValidation.Kind(kind);
            if (float.IsNaN(x) || float.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x));
            if (float.IsNaN(y) || float.IsInfinity(y)) throw new ArgumentOutOfRangeException(nameof(y));
            if (groundAge < 0 || groundAge > 1000000) throw new ArgumentOutOfRangeException(nameof(groundAge));
            if (fadeCountdown < 0 || fadeCountdown > 15) throw new ArgumentOutOfRangeException(nameof(fadeCountdown));
            if (collecting && fadeCountdown != 0) throw new ArgumentException("Collecting cancels the fade countdown.");
            Kind = kind;
            X = x;
            Y = y;
            GroundAge = groundAge;
            FadeCountdown = fadeCountdown;
            Collecting = collecting;
            Dead = dead;
        }

        // Falling/bouncing motion is outside this model and can supply the current position here.
        public SunPickupState WithPosition(float x, float y)
        {
            if (Collecting || Dead) throw new InvalidOperationException("Only an uncollected live pickup can be repositioned.");
            return new SunPickupState(Kind, x, y, GroundAge, FadeCountdown);
        }
    }

    public readonly struct SunPickupTick
    {
        public SunPickupState State { get; }
        public int Balance { get; }
        public int AmountAdded { get; }
        public bool Credited { get; }
        public bool AchievementCheck { get; }

        internal SunPickupTick(SunPickupState state, int balance, int amountAdded, bool credited)
        {
            State = state;
            Balance = balance;
            AmountAdded = amountAdded;
            Credited = credited;
            AchievementCheck = credited && balance >= 8000;
        }
    }

    public static class SunPickupRules
    {
        public const int BalanceCap = 9990;
        public const int FadeTicks = 15;

        public static int Value(SunKind kind)
        {
            SunValidation.Kind(kind);
            return kind == SunKind.Small ? 15 : kind == SunKind.Large ? 50 : 25;
        }

        // The EXE uses a 60x60 base rectangle; mode-specific expansion is supplied explicitly.
        public static bool HitTest(SunPickupState state, int mouseX, int mouseY, bool specialExpansion)
        {
            SunValidation.Kind(state.Kind);
            if (state.Dead || state.Collecting) return false;
            int margin = state.Kind == SunKind.Normal || specialExpansion ? 15 : 0;
            int extraBottom = specialExpansion ? 30 : 0;
            float x = mouseX;
            float y = mouseY;
            return x >= (double)state.X - margin && x < (double)state.X + 60 + margin
                && y >= (double)state.Y - margin && y < (double)state.Y + 60 + margin + extraBottom;
        }

        // This includes the mouse-dispatch guards, not overlapping-item selection or input routing.
        public static bool TryClick(SunPickupState state, int mouseX, int mouseY, int button,
            bool gameplayScene, bool boardPaused, bool specialExpansion, out SunPickupState result)
        {
            result = state;
            if (!gameplayScene || boardPaused || button < 0 || !HitTest(state, mouseX, mouseY, specialExpansion))
                return false;
            result = new SunPickupState(state.Kind, state.X, state.Y, state.GroundAge, 0, true);
            return true;
        }

        // groundBranch is the result of the EXE's movement/landing branch for this update.
        // ageEnabled represents its mode-31/board guard; extendedLifetime is pickup byte +0xC9.
        public static SunPickupTick Tick(SunPickupState state, int balance, bool updateEnabled,
            bool groundBranch, bool ageEnabled, bool extendedLifetime, int mode)
        {
            SunValidation.Kind(state.Kind);
            if (balance < 0 || balance > BalanceCap) throw new ArgumentOutOfRangeException(nameof(balance));
            if (!updateEnabled || state.Dead) return new SunPickupTick(state, balance, 0, false);
            if (state.FadeCountdown != 0)
            {
                int fade = state.FadeCountdown - 1;
                return new SunPickupTick(new SunPickupState(state.Kind, state.X, state.Y, state.GroundAge,
                    fade, false, fade == 0), balance, 0, false);
            }
            if (state.Collecting)
            {
                // Native code measures the pre-move distance, stores float intermediates,
                // then moves each coordinate 1/21 of the distance towards (15, 0).
                float dx = Math.Abs(state.X - 15.0f);
                float dy = Math.Abs(state.Y);
                float x = state.X > 15.0f ? (float)(state.X - (double)dx / 21.0)
                    : state.X < 15.0f ? (float)(state.X + (double)dx / 21.0) : state.X;
                float y = state.Y > 0.0f ? (float)(state.Y - (double)dy / 21.0)
                    : state.Y < 0.0f ? (float)(state.Y + (double)dy / 21.0) : state.Y;
                float squared = (float)((double)dx * dx + (double)dy * dy);
                float distance = (float)Math.Sqrt(squared);
                bool credited = distance < 8.0f;
                int nextBalance = credited ? Math.Min(BalanceCap, balance + Value(state.Kind)) : balance;
                var next = new SunPickupState(state.Kind, x, y, state.GroundAge, 0, true, credited);
                return new SunPickupTick(next, nextBalance, nextBalance - balance, credited);
            }
            if (!groundBranch || !ageEnabled) return new SunPickupTick(state, balance, 0, false);
            int age = checked(state.GroundAge + 1);
            int lifetime = mode == 43 ? 6000 : extendedLifetime ? 1500 : 750;
            int fadeCountdown = age >= lifetime ? FadeTicks : 0;
            return new SunPickupTick(new SunPickupState(state.Kind, state.X, state.Y, age, fadeCountdown),
                balance, 0, false);
        }
    }
}
