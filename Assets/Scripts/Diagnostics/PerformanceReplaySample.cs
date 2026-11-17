#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Contains one completed real Player frame without allocating a record object.</summary>
    [Serializable]
    internal struct PerformanceReplaySample
    {
        /// <summary>The recorded Player frame index.</summary>
        public int Frame;
        /// <summary>Identifies ready combat (0), restore (1), preparation (2), or evidence collection (3).</summary>
        public int Phase;
        /// <summary>Identifies the restored stress cycle.</summary>
        public int Cycle;
        /// <summary>The observed wall-clock frame interval in milliseconds.</summary>
        public double FrameMilliseconds;
        /// <summary>The complete frame's managed allocation counter in bytes.</summary>
        public long GcBytes;
        /// <summary>The main-thread allocation bytes between consecutive replay boundaries, or minus one when the positive control fails.</summary>
        public long MainThreadGcBytes;
        /// <summary>The complete production Update marker in milliseconds.</summary>
        public double UpdateMilliseconds;
        /// <summary>The complete production Present marker in milliseconds.</summary>
        public double PresentMilliseconds;
        /// <summary>The available GPU frame time in milliseconds, or minus one.</summary>
        public double GpuMilliseconds;
        /// <summary>Identifies unavailable (0), measured scene GPU span (1), or FrameTimingManager fallback (2).</summary>
        public int GpuSource;
        /// <summary>The reported complete-frame draw calls.</summary>
        public long DrawCalls;
        /// <summary>Stores completed scene instanced draws, or minus one when unavailable.</summary>
        public long InstancedDrawCount;
        /// <summary>Stores completed instance-buffer uploads, or minus one when unavailable.</summary>
        public long InstanceUploadCount;
        /// <summary>Stores instance bytes uploaded this frame, or minus one when unavailable.</summary>
        public long InstanceUploadBytes;
        /// <summary>Stores retained instance GPU-buffer bytes, or minus one when unavailable.</summary>
        public long InstancingGpuBytes;
        /// <summary>Stores prepared instance capacity, or minus one when unavailable.</summary>
        public long PreparedInstanceCapacity;
        /// <summary>Stores scene compatibility fallbacks, or minus one when unavailable.</summary>
        public long InstancingFallbackCount;
        /// <summary>Stores GPU-buffer construction count, or minus one when unavailable.</summary>
        public long GpuBufferCreationCount;
        /// <summary>Stores actual structured-buffer construction count, or minus one when unavailable.</summary>
        public long InstanceBufferCreationCount;
        /// <summary>Stores mesh vertex/index layout reconfiguration count, or minus one when unavailable.</summary>
        public long MeshBufferReconfigurationCount;
        /// <summary>Stores actual combined-mesh construction count, or minus one when unavailable.</summary>
        public long MeshCreationCount;
        /// <summary>Stores cumulative actual mesh upload bytes, or minus one when unavailable.</summary>
        public long MeshUploadBytes;
        /// <summary>Stores immutable submission binding construction count, or minus one when unavailable.</summary>
        public long InstanceSubmissionBindingCreationCount;
        /// <summary>Stores fallbacks protecting same-frame submitted instance data, or minus one when unavailable.</summary>
        public long InstanceSameFrameBufferFallbackCount;
        /// <summary>The live plant population.</summary>
        public int Plants;
        /// <summary>The live zombie population.</summary>
        public int Zombies;
        /// <summary>The active projectile population.</summary>
        public int Projectiles;
        /// <summary>Whether the authoritative match is still playing.</summary>
        public bool Playing;
    }
}
#endif
