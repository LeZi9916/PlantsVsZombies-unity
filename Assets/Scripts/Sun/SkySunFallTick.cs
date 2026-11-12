#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun fall tick.</summary>
    public readonly struct SkySunFallTick
    {
        /// <summary>Gets or sets state.</summary>
        public SkySunFallState State { get; }
        /// <summary>Gets or sets balance.</summary>
        public int Balance { get; }
        /// <summary>Gets or sets amount added.</summary>
        public int AmountAdded { get; }
        /// <summary>Gets or sets credited.</summary>
        public bool Credited { get; }
        /// <summary>Gets or sets achievement check.</summary>
        public bool AchievementCheck { get; }
        /// <summary>Gets or sets ground branch.</summary>
        public bool GroundBranch { get; }

        /// <summary>Initializes sky sun fall tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "pickup">The pickup.</param>
        /// <param name = "groundBranch">The ground branch.</param>
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
}
