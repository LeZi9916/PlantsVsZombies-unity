#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Stores replay environment, controls and raw samples after measurement finishes.</summary>
    [Serializable]
    internal sealed class PerformanceReplayReport
    {
        /// <summary>Names the candidate or baseline run.</summary>
        public string Label = string.Empty;
        /// <summary>Identifies the exact runtime editor/player version.</summary>
        public string UnityVersion = string.Empty;
        /// <summary>Identifies the CPU used for this run.</summary>
        public string Processor = string.Empty;
        /// <summary>Identifies the GPU used for this run.</summary>
        public string GraphicsDevice = string.Empty;
        /// <summary>Identifies the selected Player graphics API.</summary>
        public string GraphicsApi = string.Empty;
        /// <summary>Identifies whether the replay runs in an Editor.</summary>
        public bool Editor;
        /// <summary>Identifies the backend actually selected by Unity's Player compiler.</summary>
        public string ActualScriptingBackend = string.Empty;
        /// <summary>Records explicit offscreen rendering required by hidden Player runs.</summary>
        public bool OffscreenRendering;
        /// <summary>Records whether the optional native CPU/GPU profiler was requested.</summary>
        public bool NativeProfilerEnabled;
        /// <summary>Stores the optional binary native-profiler capture path with allocation call stacks enabled.</summary>
        public string ProfilerLogPath = string.Empty;
        /// <summary>Identifies the Player boundary at which binary profiling starts.</summary>
        public int ProfilerCaptureStartFrame;
        /// <summary>Identifies the Player boundary at which binary profiling ends.</summary>
        public int ProfilerCaptureEndFrame;
        /// <summary>Describes the direct GPU sampler and supported fallback timing source.</summary>
        public string GpuTimingSource = "CustomSampler GPU scene-render span; FrameTimingManager fallback; unavailable=-1";
        /// <summary>Stores the source checkpoint SHA-256 before playback.</summary>
        public string SourceHashBefore = string.Empty;
        /// <summary>Stores the source checkpoint SHA-256 after playback.</summary>
        public string SourceHashAfter = string.Empty;
        /// <summary>Confirms the original checkpoint bytes remain unchanged.</summary>
        public bool SourceUnchanged;
        /// <summary>Stores the requested detached native updates before normal playback.</summary>
        public int AdvanceTicks;
        /// <summary>Records whether the source stage was continued before advancing.</summary>
        public bool ContinuedStage;
        /// <summary>Stores the initial model hash at the measured replay point.</summary>
        public string InitialModelHash = string.Empty;
        /// <summary>Stores the final model hash after complete Player playback.</summary>
        public string FinalModelHash = string.Empty;
        /// <summary>Stores the actual sample count.</summary>
        public int SampleCount;
        /// <summary>Stores the number of frames whose authoritative board remained playing.</summary>
        public int PlayingFrameCount;
        /// <summary>Counts frames with nonzero full production Present time.</summary>
        public int PresentationFrameCount;
        /// <summary>Counts frames with observed draw submissions.</summary>
        public int DrawFrameCount;
        /// <summary>Counts usable GPU timing samples, which can be unavailable offscreen.</summary>
        public int GpuTimingFrameCount;
        /// <summary>Counts source reloads during long stress observations.</summary>
        public int ReloadCount;
        /// <summary>Identifies the setup frame whose render counters precede the first measured frame.</summary>
        public int InitialRenderingFrame;
        /// <summary>Stores pre-measurement instanced draws, or minus one when unavailable.</summary>
        public long InitialInstancedDrawCount;
        /// <summary>Stores pre-measurement instance upload count, or minus one when unavailable.</summary>
        public long InitialInstanceUploadCount;
        /// <summary>Stores pre-measurement instance upload bytes, or minus one when unavailable.</summary>
        public long InitialInstanceUploadBytes;
        /// <summary>Stores retained instance GPU bytes before the first measured frame.</summary>
        public long InitialInstancingGpuBytes;
        /// <summary>Stores prepared instance capacity before the first measured frame.</summary>
        public long InitialPreparedInstanceCapacity;
        /// <summary>Stores compatibility fallbacks before the first measured frame.</summary>
        public long InitialInstancingFallbackCount;
        /// <summary>Stores cumulative GPU-buffer creations before any measured frame can grow storage.</summary>
        public long InitialGpuBufferCreationCount;
        /// <summary>Stores actual structured-buffer constructions before the first measured frame.</summary>
        public long InitialInstanceBufferCreationCount;
        /// <summary>Stores mesh vertex/index layout reconfigurations before the first measured frame.</summary>
        public long InitialMeshBufferReconfigurationCount;
        /// <summary>Stores actual combined-mesh constructions before the first measured frame.</summary>
        public long InitialMeshCreationCount;
        /// <summary>Stores cumulative actual mesh upload bytes before the first measured frame.</summary>
        public long InitialMeshUploadBytes;
        /// <summary>Stores immutable submission binding constructions before the first measured frame.</summary>
        public long InitialInstanceSubmissionBindingCreationCount;
        /// <summary>Stores same-frame instance-data protection fallbacks before the first measured frame.</summary>
        public long InitialInstanceSameFrameBufferFallbackCount;
        /// <summary>Stores resource context at each stress-cycle preparation and ending boundary.</summary>
        public PerformanceReplayCycle[] Cycles = Array.Empty<PerformanceReplayCycle>();
        /// <summary>Records whether a fixed evidence array prevented recording a stress-cycle boundary.</summary>
        public bool CycleEvidenceTruncated;
        /// <summary>Records whether the source revision exposes a candidate preparation gate.</summary>
        public bool PreparationGateAvailable;
        /// <summary>Confirms the candidate still reports ready at the first measured boundary, or true for a baseline without a gate.</summary>
        public bool PreparationReadyAtMeasurement;
        /// <summary>Confirms the rendered evidence contains nonuniform opaque game pixels.</summary>
        public bool ScreenshotValid;
        /// <summary>Stores requested measurement duration, or zero for fixed-frame capture.</summary>
        public float DurationSeconds;
        /// <summary>Stores wall-clock seconds from the boundary before the first complete measured frame to the boundary reading the final sample.</summary>
        public double RecordedSeconds;
        /// <summary>Records whether native simulation time was frozen during measurement.</summary>
        public bool Frozen;
        /// <summary>Stores the count of warm frames before measurement.</summary>
        public int WarmupFrames;
        /// <summary>Stores extra frozen frames after warm-up that clear metadata allocations from complete-frame recorders.</summary>
        public int CounterBoundaryFrames;
        /// <summary>Stores the actual pixel width.</summary>
        public int Width;
        /// <summary>Stores the actual pixel height.</summary>
        public int Height;
        /// <summary>Stores the configured vertical-sync interval.</summary>
        public int VSyncCount;
        /// <summary>Stores the configured application frame target.</summary>
        public int TargetFrameRate;
        /// <summary>Records whether native allocation events detected deliberate managed garbage.</summary>
        public bool AllocationPositiveControl;
        /// <summary>Records whether the whole-frame byte counter detected the deliberate allocation.</summary>
        public bool ByteCounterPositiveControl;
        /// <summary>Records whether the optional current-thread byte counter detected the deliberate allocation.</summary>
        public bool MainThreadByteCounterPositiveControl;
        /// <summary>Counts live actor inspection components after warm-up.</summary>
        public int InitialHierarchySprites;
        /// <summary>Counts live actor inspection components at capture completion.</summary>
        public int FinalHierarchySprites;
        /// <summary>Records available initial pool counters outside the measured frames.</summary>
        public string InitialPoolStatistics = string.Empty;
        /// <summary>Records available final pool counters outside the measured frames.</summary>
        public string FinalPoolStatistics = string.Empty;
        /// <summary>Stores the measurement status, including unsupported counter failures.</summary>
        public string Status = string.Empty;
        /// <summary>Stores a failure description when capture cannot be completed.</summary>
        public string Error = string.Empty;
        /// <summary>Stores raw samples in original frame order.</summary>
        public PerformanceReplaySample[] Samples = Array.Empty<PerformanceReplaySample>();
    }
}
#endif
