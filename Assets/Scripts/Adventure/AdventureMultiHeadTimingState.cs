#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Stores the native countdown and shooting-head state at a tick boundary.</summary>
    public readonly struct AdventureMultiHeadTimingState
    {
        /// <summary>Gets the plant species.</summary>
        public PlantKind Kind { get; }

        /// <summary>Gets the ticks until the next primary attack attempt.</summary>
        public int Countdown { get; }

        /// <summary>Gets the shared shooting countdown.</summary>
        public int ShootingCountdown { get; }

        /// <summary>Gets the heads still playing their shooting animation.</summary>
        public AdventureShootingHeads ActiveHeads { get; }

        /// <summary>Initializes a native multi-head timing snapshot.</summary>
        /// <param name="kind">The species represented by this snapshot.</param>
        /// <param name="countdown">The primary attack countdown.</param>
        /// <param name="shootingCountdown">The shared shooting countdown.</param>
        /// <param name="activeHeads">The heads still playing their shooting animation.</param>
        internal AdventureMultiHeadTimingState(PlantKind kind, int countdown, int shootingCountdown, AdventureShootingHeads activeHeads)
        {
            Kind = kind;
            Countdown = countdown;
            ShootingCountdown = shootingCountdown;
            ActiveHeads = activeHeads;
        }
    }
}
