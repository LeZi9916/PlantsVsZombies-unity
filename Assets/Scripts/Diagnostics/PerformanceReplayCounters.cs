#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;
using System.Reflection;
using PlantsVsZombies.Rendering;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Binds optional baseline-compatible rendering counters once without per-frame reflection or boxing.</summary>
    internal sealed class PerformanceReplayCounters
    {
        /// <summary>Reads the completed instanced draw count.</summary>
        private readonly Func<long> _draws = Bind("InstancedDrawCount");
        /// <summary>Reads the completed upload operation count.</summary>
        private readonly Func<long> _uploads = Bind("InstanceUploadCount");
        /// <summary>Reads bytes uploaded during the completed frame.</summary>
        private readonly Func<long> _uploadBytes = Bind("InstanceUploadBytes");
        /// <summary>Reads retained GPU instance-buffer storage.</summary>
        private readonly Func<long> _gpuBytes = Bind("InstancingGpuBytes");
        /// <summary>Reads the prepared instance population.</summary>
        private readonly Func<long> _capacity = Bind("PreparedInstanceCapacity");
        /// <summary>Reads compatibility fallback submissions.</summary>
        private readonly Func<long> _fallbacks = Bind("InstancingFallbackCount");
        /// <summary>Reads instance GPU-buffer construction events.</summary>
        private readonly Func<long> _creations = Bind("GpuBufferCreationCount");
        /// <summary>Reads actual structured-buffer construction events.</summary>
        private readonly Func<long> _instanceCreations = Bind("InstanceBufferCreationCount");
        /// <summary>Reads actual mesh vertex/index layout reconfigurations.</summary>
        private readonly Func<long> _meshReconfigurations = Bind("MeshBufferReconfigurationCount");
        /// <summary>Reads actual combined-mesh construction events.</summary>
        private readonly Func<long> _meshCreations = Bind("MeshCreationCount");
        /// <summary>Reads cumulative actual mesh vertex/index upload bytes.</summary>
        private readonly Func<long> _meshUploadBytes = Bind("MeshUploadBytes");
        /// <summary>Reads immutable native submission binding construction events.</summary>
        private readonly Func<long> _submissionBindings = Bind("InstanceSubmissionBindingCreationCount");
        /// <summary>Reads scene fallbacks that protect instance data already submitted in the same Unity frame.</summary>
        private readonly Func<long> _sameFrameFallbacks = Bind("InstanceSameFrameBufferFallbackCount");

        /// <summary>Copies counter values into an existing sample without allocating.</summary>
        /// <param name="sample">The destination frame record.</param>
        internal void Read(ref PerformanceReplaySample sample)
        {
            sample.InstancedDrawCount = _draws();
            sample.InstanceUploadCount = _uploads();
            sample.InstanceUploadBytes = _uploadBytes();
            sample.InstancingGpuBytes = _gpuBytes();
            sample.PreparedInstanceCapacity = _capacity();
            sample.InstancingFallbackCount = _fallbacks();
            sample.GpuBufferCreationCount = _creations();
            sample.InstanceBufferCreationCount = _instanceCreations();
            sample.MeshBufferReconfigurationCount = _meshReconfigurations();
            sample.MeshCreationCount = _meshCreations();
            sample.MeshUploadBytes = _meshUploadBytes();
            sample.InstanceSubmissionBindingCreationCount = _submissionBindings();
            sample.InstanceSameFrameBufferFallbackCount = _sameFrameFallbacks();
        }

        /// <summary>Creates an integer getter for an optional source-revision counter.</summary>
        /// <param name="name">The ObjectSpriteRenderer property name.</param>
        /// <returns>A bound getter, or a stable unavailable sentinel.</returns>
        private static Func<long> Bind(string name)
        {
            var method = typeof(ObjectSpriteRenderer).GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(true);
            if (method?.ReturnType == typeof(long))
            {
                return (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            }
            if (method?.ReturnType == typeof(int))
            {
                var read = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), method);
                return () => read();
            }
            if (method?.ReturnType == typeof(ulong))
            {
                var read = (Func<ulong>)Delegate.CreateDelegate(typeof(Func<ulong>), method);
                return () => unchecked((long)read());
            }
            return Unavailable;
        }

        /// <summary>Reports that a baseline does not implement a requested rendering counter.</summary>
        /// <returns>Minus one, never an invented zero measurement.</returns>
        private static long Unavailable()
        {
            return -1;
        }
    }
}
#endif
