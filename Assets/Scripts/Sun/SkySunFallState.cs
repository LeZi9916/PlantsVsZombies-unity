#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun fall state.</summary>
    public readonly struct SkySunFallState
    {
        /// <summary>Gets or sets pickup.</summary>
        public SunPickupState Pickup { get; }
        /// <summary>Gets or sets landing y.</summary>
        public int LandingY { get; }

        /// <summary>Initializes sky sun fall state.</summary>
        /// <param name = "pickup">The pickup.</param>
        /// <param name = "landingY">The landing y.</param>
        internal SkySunFallState(SunPickupState pickup, int landingY)
        {
            Pickup = pickup;
            LandingY = landingY;
        }

        // Use this after TryClick; arbitrary repositioning is not a sky-fall rule.
        /// <summary>Performs with pickup.</summary>
        /// <param name = "pickup">The pickup.</param>
        /// <returns>The resulting with pickup.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public SkySunFallState WithPickup(SunPickupState pickup)
        {
            if (pickup.Kind != Pickup.Kind)
            {
                throw new ArgumentException("Pickup kind changed.", nameof(pickup));
            }

            return new SkySunFallState(pickup, LandingY);
        }
    }
}
