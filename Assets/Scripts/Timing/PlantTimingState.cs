#nullable enable

namespace PlantsVsZombies.Timing
{
    /// <summary>Defines plant timing state.</summary>
    public readonly struct PlantTimingState
    {
        /// <summary>Gets or sets kind.</summary>
        public PlantKind Kind { get; }
        /// <summary>Gets or sets countdown.</summary>
        public int Countdown { get; }
        /// <summary>Gets or sets shooting countdown.</summary>
        public int ShootingCountdown { get; }

        /// <summary>Initializes plant timing state.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "countdown">The countdown.</param>
        /// <param name = "shootingCountdown">The shooting countdown.</param>
        internal PlantTimingState(PlantKind kind, int countdown, int shootingCountdown)
        {
            Kind = kind;
            Countdown = countdown;
            ShootingCountdown = shootingCountdown;
        }
    }
}
