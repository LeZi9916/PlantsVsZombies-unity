#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun pickup state.</summary>
    public readonly struct SunPickupState
    {
        /// <summary>Gets or sets kind.</summary>
        public SunKind Kind { get; }
        /// <summary>Gets or sets x.</summary>
        public float X { get; }
        /// <summary>Gets or sets y.</summary>
        public float Y { get; }
        /// <summary>Gets or sets ground age.</summary>
        public int GroundAge { get; }
        /// <summary>Gets or sets fade countdown.</summary>
        public int FadeCountdown { get; }
        /// <summary>Gets or sets collecting.</summary>
        public bool Collecting { get; }
        /// <summary>Gets or sets dead.</summary>
        public bool Dead { get; }

        /// <summary>Initializes sun pickup state.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "groundAge">The ground age.</param>
        /// <param name = "fadeCountdown">The fade countdown.</param>
        /// <param name = "collecting">The collecting.</param>
        /// <param name = "dead">The dead.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public SunPickupState(SunKind kind, float x, float y, int groundAge = 0, int fadeCountdown = 0, bool collecting = false, bool dead = false)
        {
            SunValidation.Kind(kind);
            if (float.IsNaN(x) || float.IsInfinity(x))
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (float.IsNaN(y) || float.IsInfinity(y))
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            if (groundAge < 0 || groundAge > 1000000)
            {
                throw new ArgumentOutOfRangeException(nameof(groundAge));
            }

            if (fadeCountdown < 0 || fadeCountdown > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeCountdown));
            }

            if (collecting && fadeCountdown != 0)
            {
                throw new ArgumentException("Collecting cancels the fade countdown.");
            }

            Kind = kind;
            X = x;
            Y = y;
            GroundAge = groundAge;
            FadeCountdown = fadeCountdown;
            Collecting = collecting;
            Dead = dead;
        }

        // Falling/bouncing motion is outside this model and can supply the current position here.
        /// <summary>Performs with position.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting with position.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public SunPickupState WithPosition(float x, float y)
        {
            if (Collecting || Dead)
            {
                throw new InvalidOperationException("Only an uncollected live pickup can be repositioned.");
            }

            return new SunPickupState(Kind, x, y, GroundAge, FadeCountdown);
        }
    }
}
