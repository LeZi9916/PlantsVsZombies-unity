#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun state.</summary>
    public readonly struct SkySunState
    {
        /// <summary>Gets or sets countdown.</summary>
        public int Countdown { get; }
        /// <summary>Gets or sets drops.</summary>
        public int Drops { get; }

        /// <summary>Initializes sky sun state.</summary>
        /// <param name = "countdown">The countdown.</param>
        /// <param name = "drops">The drops.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public SkySunState(int countdown, int drops)
        {
            if (drops < 0 || drops == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(drops));
            }

            Countdown = countdown;
            Drops = drops;
        }
    }
}
