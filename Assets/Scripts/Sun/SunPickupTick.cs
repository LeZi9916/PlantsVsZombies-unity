#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun pickup tick.</summary>
    public readonly struct SunPickupTick
    {
        /// <summary>Gets or sets state.</summary>
        public SunPickupState State { get; }
        /// <summary>Gets or sets balance.</summary>
        public int Balance { get; }
        /// <summary>Gets or sets amount added.</summary>
        public int AmountAdded { get; }
        /// <summary>Gets or sets credited.</summary>
        public bool Credited { get; }
        /// <summary>Gets or sets achievement check.</summary>
        public bool AchievementCheck { get; }

        /// <summary>Initializes sun pickup tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "balance">The balance.</param>
        /// <param name = "amountAdded">The amount added.</param>
        /// <param name = "credited">The credited.</param>
        internal SunPickupTick(SunPickupState state, int balance, int amountAdded, bool credited)
        {
            State = state;
            Balance = balance;
            AmountAdded = amountAdded;
            Credited = credited;
            AchievementCheck = credited && balance >= 8000;
        }
    }
}
