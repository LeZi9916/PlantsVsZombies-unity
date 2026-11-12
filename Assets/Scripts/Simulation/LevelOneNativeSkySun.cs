#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>
    /// A live experimental sky pickup in the original 800x600 pixel space.
    /// Position is its native top-left, not a Unity or grid coordinate.
    /// </summary>
    public sealed class LevelOneNativeSkySun
    {
        /// <summary>Stores base width pixels.</summary>
        public const int BaseWidthPixels = 60;
        /// <summary>Stores base height pixels.</summary>
        public const int BaseHeightPixels = 60;
        /// <summary>Stores fall state.</summary>
        internal global::PlantsVsZombies.Sun.SkySunFallState _fallState;
        /// <summary>Initializes level one native sky sun.</summary>
        /// <param name = "state">The state.</param>
        internal LevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunFallState state)
        {
            _fallState = state;
        }

        /// <summary>Gets or sets pixel x.</summary>
        public float PixelX
        {
            get
            {
                return _fallState.Pickup.X;
            }
        }

        /// <summary>Gets or sets pixel y.</summary>
        public float PixelY
        {
            get
            {
                return _fallState.Pickup.Y;
            }
        }

        /// <summary>Gets or sets landing pixel y.</summary>
        public int LandingPixelY
        {
            get
            {
                return _fallState.LandingY;
            }
        }

        /// <summary>Gets or sets kind.</summary>
        public global::PlantsVsZombies.Sun.SunKind Kind
        {
            get
            {
                return _fallState.Pickup.Kind;
            }
        }

        /// <summary>Gets or sets is collecting.</summary>
        public bool IsCollecting
        {
            get
            {
                return _fallState.Pickup.Collecting;
            }
        }

        /// <summary>Gets or sets is dead.</summary>
        public bool IsDead
        {
            get
            {
                return _fallState.Pickup.Dead;
            }
        }

        /// <summary>Gets or sets ground age ticks.</summary>
        public int GroundAgeTicks
        {
            get
            {
                return _fallState.Pickup.GroundAge;
            }
        }

        /// <summary>Gets or sets fade ticks remaining.</summary>
        public int FadeTicksRemaining
        {
            get
            {
                return _fallState.Pickup.FadeCountdown;
            }
        }
    }
}
