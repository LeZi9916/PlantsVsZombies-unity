#nullable enable

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>
    /// A static zombie selection row. Costs, gates and weights are raw selection inputs,
    /// not health values, spawn guarantees, or a replacement for runtime exceptions.
    /// </summary>
    public sealed class ZombieDefinition
    {
        /// <summary>Creates one verified static zombie-selection row.</summary>
        /// <param name = "id">The zero-based original zombie identifier.</param>
        /// <param name = "internalName">The original internal zombie name.</param>
        /// <param name = "reanimationId">The original animation-table identifier.</param>
        /// <param name = "waveBudgetCostRaw">The raw cost used by wave selection.</param>
        /// <param name = "baseLevelGateRaw">The unadjusted level gate.</param>
        /// <param name = "baseWaveGateRaw">The unadjusted wave gate.</param>
        /// <param name = "baseSelectionWeightRaw">The unadjusted random-selection weight.</param>
        internal ZombieDefinition(int id, string internalName, int reanimationId, int waveBudgetCostRaw, int baseLevelGateRaw, int baseWaveGateRaw, int baseSelectionWeightRaw)
        {
            Id = id;
            InternalName = internalName;
            ReanimationId = reanimationId;
            WaveBudgetCostRaw = waveBudgetCostRaw;
            BaseLevelGateRaw = baseLevelGateRaw;
            BaseWaveGateRaw = baseWaveGateRaw;
            BaseSelectionWeightRaw = baseSelectionWeightRaw;
        }

        /// <summary>Gets the original zero-based zombie identifier.</summary>
        public int Id { get; }
        /// <summary>Gets the internal zombie name recorded in the original table.</summary>
        public string InternalName { get; }
        /// <summary>Gets the original reanimation identifier used by this zombie.</summary>
        public int ReanimationId { get; }
        /// <summary>Gets the raw wave-budget cost used as a selection input.</summary>
        public int WaveBudgetCostRaw { get; }
        /// <summary>Gets the original level-selection gate before runtime exceptions are applied.</summary>
        public int BaseLevelGateRaw { get; }
        /// <summary>Gets the original wave-selection gate before runtime exceptions are applied.</summary>
        public int BaseWaveGateRaw { get; }
        /// <summary>Gets the original relative selection weight before runtime adjustments.</summary>
        public int BaseSelectionWeightRaw { get; }
    }
}
