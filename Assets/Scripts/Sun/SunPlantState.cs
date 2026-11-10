#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun plant state.</summary>
    public readonly struct SunPlantState
    {
        /// <summary>Gets or sets kind.</summary>
        public SunPlantKind Kind { get; }
        /// <summary>Gets or sets countdown.</summary>
        public int Countdown { get; }

        /// <summary>Initializes sun plant state.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "countdown">The countdown.</param>
        public SunPlantState(SunPlantKind kind, int countdown)
        {
            SunPlantRules.ValidateKind(kind);
            Countdown = countdown;
            Kind = kind;
        }
    }
}
