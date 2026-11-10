using System;

namespace PvZUnity.Sun
{
    public readonly struct SkySunFallState
    {
        public SunPickupState Pickup { get; }
        public int LandingY { get; }

        internal SkySunFallState(SunPickupState pickup, int landingY)
        {
            Pickup = pickup;
            LandingY = landingY;
        }

        // Use this after TryClick; arbitrary repositioning is not a sky-fall rule.
        public SkySunFallState WithPickup(SunPickupState pickup)
        {
            if (pickup.Kind != Pickup.Kind) throw new ArgumentException("Pickup kind changed.", nameof(pickup));
            return new SkySunFallState(pickup, LandingY);
        }
    }

    public readonly struct SkySunFallTick
    {
        public SkySunFallState State { get; }
        public int Balance { get; }
        public int AmountAdded { get; }
        public bool Credited { get; }
        public bool AchievementCheck { get; }
        public bool GroundBranch { get; }

        internal SkySunFallTick(SkySunFallState state, SunPickupTick pickup, bool groundBranch)
        {
            State = state;
            Balance = pickup.Balance;
            AmountAdded = pickup.AmountAdded;
            Credited = pickup.Credited;
            AchievementCheck = pickup.AchievementCheck;
            GroundBranch = groundBranch;
        }
    }

    public static class SkySunFall
    {
        public const int LandingDrawBound = 250;
        public const float VerticalSpeed = 0.67f;

        // This draw occurs in pickup construction, not necessarily immediately after the scheduler's draws.
        public static SkySunFallState Initialize(SkySunTick spawn, int landingDraw)
        {
            if (!spawn.Spawned || spawn.MotionType != 0) throw new ArgumentException("A sky spawn is required.", nameof(spawn));
            SunValidation.Draw(landingDraw, LandingDrawBound);
            return new SkySunFallState(new SunPickupState(spawn.Kind, spawn.X, spawn.Y), 300 + landingDraw);
        }

        public static SkySunFallTick Tick(SkySunFallState state, int balance, bool updateEnabled,
            bool ageEnabled, bool extendedLifetime, int mode)
        {
            SunPickupState pickup = state.Pickup;
            bool ground = false;
            if (updateEnabled && !pickup.Dead && !pickup.Collecting && pickup.FadeCountdown == 0)
            {
                double nextY = (double)pickup.Y + VerticalSpeed;
                ground = nextY >= state.LandingY;
                pickup = pickup.WithPosition(pickup.X, ground ? state.LandingY : (float)nextY);
            }
            SunPickupTick tick = SunPickupRules.Tick(pickup, balance, updateEnabled, ground, ageEnabled,
                extendedLifetime, mode);
            return new SkySunFallTick(new SkySunFallState(tick.State, state.LandingY), tick, ground);
        }
    }
}
