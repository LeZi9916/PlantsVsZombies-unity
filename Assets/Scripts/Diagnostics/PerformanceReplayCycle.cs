#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Records stress-cycle resource high-water context outside ready-combat frames.</summary>
    [Serializable]
    internal struct PerformanceReplayCycle
    {
        /// <summary>Identifies the cycle restored from the same checkpoint.</summary>
        public int Cycle;
        /// <summary>Identifies preparation completion (0) or the cycle's ending before restore or measurement completion (1).</summary>
        public int Boundary;
        /// <summary>Identifies the following sample boundary, or the sample count for the final ending snapshot.</summary>
        public int SampleIndex;
        /// <summary>Stores available pool counters.</summary>
        public string PoolStatistics;
        /// <summary>Stores total model pool construction, or minus one when unavailable in the baseline.</summary>
        public long PoolCreatedCount;
        /// <summary>Stores records protected by model lifetime references, or minus one when unavailable.</summary>
        public long PoolActiveCount;
        /// <summary>Stores completed model pool returns, or minus one when unavailable.</summary>
        public long PoolRecycledCount;
        /// <summary>Stores allocated native memory in bytes.</summary>
        public long NativeAllocatedBytes;
        /// <summary>Stores Unity's graphics-driver allocated-memory counter, or minus one for a nonpositive unavailable reading.</summary>
        public long GraphicsDriverAllocatedBytes;
        /// <summary>Stores occupied managed memory in bytes.</summary>
        public long ManagedBytes;
        /// <summary>Counts live scene actor-inspection components.</summary>
        public int HierarchySprites;
        /// <summary>Counts loaded meshes.</summary>
        public int Meshes;
        /// <summary>Counts loaded textures.</summary>
        public int Textures;
        /// <summary>Counts loaded materials.</summary>
        public int Materials;
        /// <summary>Stores grouped material names outside measured combat to attribute lifetime growth.</summary>
        public string MaterialInventory;
    }
}
#endif
