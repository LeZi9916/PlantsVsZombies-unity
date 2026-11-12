#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>
    /// One 1-based adventure table row. Base waves and raw zombie flags are static
    /// inputs, not a complete level script or a list of guaranteed spawns. Runtime
    /// rules can override wave counts and bypass or modify selection.
    /// </summary>
    public sealed class AdventureLevelDefinition
    {
        /// <summary>Stores the read-only wrapper for the original zombie-selection flag row.</summary>
        private readonly ReadOnlyCollection<int> _zombieFlagsRaw;
        /// <summary>Creates one verified adventure-level table row.</summary>
        /// <param name = "levelIndex">The original adventure level index starting at one.</param>
        /// <param name = "baseWaveCount">The unadjusted wave count in the original table.</param>
        /// <param name = "zombieFlagsRaw">The raw selection flags indexed by original zombie identifiers.</param>
        internal AdventureLevelDefinition(int levelIndex, int baseWaveCount, int[] zombieFlagsRaw)
        {
            LevelIndex = levelIndex;
            BaseWaveCount = baseWaveCount;
            _zombieFlagsRaw = Array.AsReadOnly(zombieFlagsRaw);
        }

        /// <summary>Gets the original one-based adventure level index.</summary>
        public int LevelIndex { get; }
        /// <summary>Gets the original base wave count before runtime rules override it.</summary>
        public int BaseWaveCount { get; }

        /// <summary>Raw flag values indexed by zombie ID, not final spawn decisions.</summary>
        public IReadOnlyList<int> ZombieFlagsRaw
        {
            get
            {
                return _zombieFlagsRaw;
            }
        }
    }
}
