#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Runs opt-in complete Player replays using private checkpoint copies and real game lifecycles.</summary>
    [DefaultExecutionOrder(32000)]
    internal sealed class PerformanceReplayController : MonoBehaviour
    {
        /// <summary>The real production host, initialized normally before playback.</summary>
        private PvZGame _owner = null!;
        /// <summary>The read-only external checkpoint source.</summary>
        private string _source = string.Empty;
        /// <summary>The dedicated run output directory.</summary>
        private string _output = string.Empty;
        /// <summary>The run controls and recorded evidence.</summary>
        private readonly PerformanceReplayReport _report = new PerformanceReplayReport();
        /// <summary>The number of complete real frames requested.</summary>
        private int _requestedFrames;
        /// <summary>Reads the candidate's preparation gate without requiring its type in a baseline build.</summary>
        private Func<bool>? _preparationReady;
        /// <summary>Retains a real GPU destination even when the Player window is hidden.</summary>
        private RenderTexture? _target;
        /// <summary>Reuses the production render-pipeline request without allocating per frame.</summary>
        private readonly RenderPipeline.StandardRequest _renderRequest = new RenderPipeline.StandardRequest();
        /// <summary>Measures the explicit scene-render request on supported GPU profilers.</summary>
        private UnityEngine.Profiling.CustomSampler? _gpuSampler;
        /// <summary>Reads completed GPU samples without allocating per frame.</summary>
        private UnityEngine.Profiling.Recorder? _gpuRecorder;
        /// <summary>Disposes measurement recorders if the replay host is destroyed before normal completion.</summary>
        private IEnumerator? _routine;
        /// <summary>Inserts actual GPU timestamp markers into the render context instead of CPU-only sampler scopes.</summary>
        private CommandBuffer? _gpuCommands;
        /// <summary>Identifies the complete scene camera while excluding nested actor captures.</summary>
        private Camera? _sceneCamera;

        /// <summary>Creates an isolated normal host before the generated empty replay scene loads.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!HasArgument("-pvz-performance-replay"))
            {
                return;
            }
            var host = new GameObject("Performance replay host");
            host.SetActive(false);
            DontDestroyOnLoad(host);
            var owner = host.AddComponent<PvZGame>();
            owner._smokeMode = true;
            var replay = host.AddComponent<PerformanceReplayController>();
            replay._owner = owner;
            host.SetActive(true);
        }

        /// <summary>Observes errors from the complete coroutine without skipping cleanup or reporting.</summary>
        private IEnumerator Start()
        {
            var routine = Run();
            _routine = routine;
            var failed = false;
            while (!failed)
            {
                var moved = false;
                object? current = null;
                try
                {
                    moved = routine.MoveNext();
                    current = moved ? routine.Current : null;
                }
                catch (Exception error)
                {
                    failed = true;
                    _report.Status = "failed";
                    _report.Error = error.ToString();
                    if (!string.IsNullOrEmpty(_output))
                    {
                        WriteReport();
                    }
                    Debug.LogException(error);
                }
                if (failed || !moved)
                {
                    break;
                }
                yield return current;
            }
            (routine as IDisposable)?.Dispose();
            _routine = null;
            Application.Quit(failed ? 1 : 0);
        }

        /// <summary>Waits for genuine startup, attaches a validated checkpoint and samples complete real frames.</summary>
        /// <returns>Normal Player frame boundaries.</returns>
        private IEnumerator Run()
        {
            ReadControls();
            _report.Status = "loading";
            WriteReport();
            Application.runInBackground = true;
            var startupDeadline = Time.realtimeSinceStartupAsDouble + 180d;
            while (!_owner.Loading.OriginalLoadingReady || _owner.Loading.OriginalLoadingActive)
            {
                if (Time.realtimeSinceStartupAsDouble >= startupDeadline)
                {
                    throw new TimeoutException("Normal game startup and audio did not finish within 180 seconds.");
                }
                yield return null;
            }
            AttachCheckpoint();
            Time.timeScale = 0f;
            _target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _renderRequest.destination = _target;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            ObjectSpriteRenderer.SceneCamera.enabled = false;
            _sceneCamera = ObjectSpriteRenderer.SceneCamera;
            _report.OffscreenRendering = true;
            // A hidden render target has no swapchain to honor VSync; use the real Player frame limiter.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _report.VSyncCount = QualitySettings.vSyncCount;
            _report.TargetFrameRate = Application.targetFrameRate;
            if (HasArgument("-pvz-replay-native-profiler"))
            {
                _report.ProfilerLogPath = Path.Combine(_output, "replay.raw");
                UnityEngine.Profiling.Profiler.logFile = _report.ProfilerLogPath;
                UnityEngine.Profiling.Profiler.enableAllocationCallstacks = true;
                UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, true);
                UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU, true);
                UnityEngine.Profiling.Profiler.enabled = false;
                _report.NativeProfilerEnabled = UnityEngine.Profiling.Profiler.supported;
                if (!_report.NativeProfilerEnabled)
                {
                    throw new InvalidOperationException("The requested native allocation-callstack profiler is unavailable.");
                }
            }
            _gpuSampler = UnityEngine.Profiling.CustomSampler.Create("PvZ.Replay.SceneRender", true);
            _gpuRecorder = _gpuSampler.GetRecorder();
            _gpuRecorder.enabled = true;
            _gpuCommands = new CommandBuffer { name = "Replay GPU scene timestamps" };
            RenderPipelineManager.beginCameraRendering += BeginGpuScene;
            RenderPipelineManager.endCameraRendering += EndGpuScene;
            // The preparation controller must first observe the newly attached world in its normal Update.
            // Its old world's ready flag can remain true during this attachment frame.
            yield return null;
            BindPreparationGate();
            var preparationDeadline = Time.realtimeSinceStartupAsDouble + 180d;
            while (_preparationReady != null && !_preparationReady())
            {
                if (Time.realtimeSinceStartupAsDouble >= preparationDeadline)
                {
                    throw new TimeoutException("Battle preparation did not finish within 180 seconds.");
                }
                yield return null;
            }
            SpriteRenderingProfiler.Initialize();
            SpriteRenderingProfiler.ForceEnabled = true;
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 4096,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            using var bytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            using var update = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "PvZ.Update", 1);
            using var present = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "PvZ.Present", 1);
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            if (!allocations.Valid || !bytes.Valid || !update.Valid || !present.Valid)
            {
                throw new InvalidOperationException("Required allocation or production timing recorder is unavailable.");
            }
            allocations.Start();
            var currentThreadBefore = GC.GetAllocatedBytesForCurrentThread();
            var sentinel = new byte[32768];
            _report.MainThreadByteCounterPositiveControl = GC.GetAllocatedBytesForCurrentThread() - currentThreadBefore >= 32768;
            allocations.Stop();
            _report.AllocationPositiveControl = allocations.Count > 0;
            GC.KeepAlive(sentinel);
            yield return null;
            _report.ByteCounterPositiveControl = bytes.LastValue >= 32768;
            if (!_report.AllocationPositiveControl || !_report.ByteCounterPositiveControl)
            {
                throw new InvalidOperationException("Deliberate allocation was not detected; this run cannot claim allocation evidence.");
            }
            var frameTimings = new FrameTiming[1];
            var capacity = _report.DurationSeconds > 0f ? Math.Max(_requestedFrames, (int)(_report.DurationSeconds * 240f) + 120) : _requestedFrames;
            var samples = new PerformanceReplaySample[capacity];
            var renderingCounters = new PerformanceReplayCounters();
            var cycles = new PerformanceReplayCycle[512];
            var cycleCount = 0;
            // Recorder setup and its deliberate allocation precede the formal warm-up window.
            for (var frame = 0; frame < _report.WarmupFrames; frame++)
            {
                yield return null;
            }
            AddCycle(cycles, ref cycleCount, 0, 0, false);
            _report.InitialHierarchySprites = CountHierarchySprites();
            _report.InitialPoolStatistics = ReadPoolStatistics();
            _report.InitialModelHash = Hash(_owner.RequiredGame.CaptureCheckpoint());
            _report.Status = "sampling";
            WriteReport();
            // These explicitly reported frozen boundary frames clear warmed metadata allocations.
            for (var frame = 0; frame < _report.CounterBoundaryFrames; frame++)
            {
                yield return null;
            }
            _report.PreparationReadyAtMeasurement = _preparationReady == null || _preparationReady();
            if (!_report.PreparationReadyAtMeasurement)
            {
                throw new InvalidOperationException("The attached candidate world lost preparation readiness before the first measured frame.");
            }
            var initialRendering = default(PerformanceReplaySample);
            renderingCounters.Read(ref initialRendering);
            _report.InitialRenderingFrame = Time.frameCount;
            _report.InitialInstancedDrawCount = initialRendering.InstancedDrawCount;
            _report.InitialInstanceUploadCount = initialRendering.InstanceUploadCount;
            _report.InitialInstanceUploadBytes = initialRendering.InstanceUploadBytes;
            _report.InitialInstancingGpuBytes = initialRendering.InstancingGpuBytes;
            _report.InitialPreparedInstanceCapacity = initialRendering.PreparedInstanceCapacity;
            _report.InitialInstancingFallbackCount = initialRendering.InstancingFallbackCount;
            _report.InitialGpuBufferCreationCount = initialRendering.GpuBufferCreationCount;
            _report.InitialInstanceBufferCreationCount = initialRendering.InstanceBufferCreationCount;
            _report.InitialMeshBufferReconfigurationCount = initialRendering.MeshBufferReconfigurationCount;
            _report.InitialMeshCreationCount = initialRendering.MeshCreationCount;
            _report.InitialMeshUploadBytes = initialRendering.MeshUploadBytes;
            _report.InitialInstanceSubmissionBindingCreationCount = initialRendering.InstanceSubmissionBindingCreationCount;
            _report.InitialInstanceSameFrameBufferFallbackCount = initialRendering.InstanceSameFrameBufferFallbackCount;
            if (_report.NativeProfilerEnabled)
            {
                _report.ProfilerCaptureStartFrame = Time.frameCount;
                UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                UnityEngine.Profiling.Profiler.enabled = true;
                if (!UnityEngine.Profiling.Profiler.enabled)
                {
                    throw new InvalidOperationException("The native allocation-callstack profiler did not start.");
                }
            }
            Time.timeScale = _report.Frozen ? 0f : 1f;
            var started = Time.realtimeSinceStartupAsDouble;
            var previousThreadBytes = GC.GetAllocatedBytesForCurrentThread();
            var previousPhase = 0;
            var cycle = 0;
            var waitingForPreparation = false;
            // The first sample must cover a complete frame after the measured wall-clock boundary.
            yield return null;
            while (_report.SampleCount < capacity)
            {
                var currentThreadBytes = GC.GetAllocatedBytesForCurrentThread();
                var sample = new PerformanceReplaySample
                {
                    Frame = Time.frameCount,
                    Phase = previousPhase,
                    Cycle = cycle,
                    FrameMilliseconds = Time.unscaledDeltaTime * 1000d,
                    GcBytes = bytes.LastValue,
                    MainThreadGcBytes = _report.MainThreadByteCounterPositiveControl ? currentThreadBytes - previousThreadBytes : -1,
                    UpdateMilliseconds = update.LastValue / 1000000d,
                    PresentMilliseconds = present.LastValue / 1000000d,
                    DrawCalls = draws.Valid ? draws.LastValue : -1,
                    GpuMilliseconds = -1d,
                    Plants = _owner.RequiredGame.Plants.Count,
                    Zombies = _owner.RequiredGame.Zombies.Count,
                    Projectiles = _owner.RequiredGame.Peas.Count,
                    Playing = _owner.RequiredGame.State == MatchState.Playing
                };
                previousThreadBytes = currentThreadBytes;
                if (_gpuRecorder.gpuSampleBlockCount > 0 && _gpuRecorder.gpuElapsedNanoseconds > 0)
                {
                    sample.GpuMilliseconds = _gpuRecorder.gpuElapsedNanoseconds / 1000000d;
                    sample.GpuSource = 1;
                }
                else if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0 && frameTimings[0].gpuFrameTime > 0d)
                {
                    sample.GpuMilliseconds = frameTimings[0].gpuFrameTime;
                    sample.GpuSource = 2;
                }
                renderingCounters.Read(ref sample);
                samples[_report.SampleCount++] = sample;
                if (sample.Playing)
                {
                    _report.PlayingFrameCount++;
                }
                _report.PresentationFrameCount += sample.PresentMilliseconds > 0d ? 1 : 0;
                _report.DrawFrameCount += sample.DrawCalls > 0 ? 1 : 0;
                _report.GpuTimingFrameCount += sample.GpuMilliseconds > 0d ? 1 : 0;
                FrameTimingManager.CaptureFrameTimings();
                _report.RecordedSeconds = Time.realtimeSinceStartupAsDouble - started;
                if (_report.DurationSeconds > 0f ? _report.RecordedSeconds >= _report.DurationSeconds
                    : _report.SampleCount >= _requestedFrames)
                {
                    break;
                }
                previousPhase = 0;
                if (_report.DurationSeconds > 0f && !_report.Frozen
                    && (_owner.RequiredGame.State == MatchState.Lost || _owner.RequiredGame.State == MatchState.Won || _owner._choosingSeeds))
                {
                    previousPhase = 1;
                    AddCycle(cycles, ref cycleCount, cycle, _report.SampleCount, true);
                    cycle++;
                    _report.ReloadCount++;
                    AttachCheckpoint();
                    ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
                    ObjectSpriteRenderer.SceneCamera.enabled = false;
                    BindPreparationGate();
                    waitingForPreparation = true;
                }
                else if (waitingForPreparation)
                {
                    previousPhase = 2;
                    if (_preparationReady == null || _preparationReady())
                    {
                        previousPhase = 3;
                        AddCycle(cycles, ref cycleCount, cycle, _report.SampleCount, false);
                        waitingForPreparation = false;
                    }
                }
                yield return null;
            }
            if (_report.NativeProfilerEnabled)
            {
                _report.ProfilerCaptureEndFrame = Time.frameCount;
                UnityEngine.Profiling.Profiler.enabled = false;
                // The binary writer completes its final frame before its file sink is closed.
                Time.timeScale = 0f;
                yield return null;
                UnityEngine.Profiling.Profiler.logFile = string.Empty;
                UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            }
            Array.Resize(ref samples, _report.SampleCount);
            _report.Samples = samples;
            AddCycle(cycles, ref cycleCount, cycle, _report.SampleCount, true);
            Array.Resize(ref cycles, cycleCount);
            _report.Cycles = cycles;
            if (_report.DurationSeconds > 0f && _report.RecordedSeconds < _report.DurationSeconds)
            {
                throw new InvalidOperationException($"Replay sample capacity {capacity} was exhausted after {_report.RecordedSeconds:F3} seconds, before the requested {_report.DurationSeconds:F3} seconds.");
            }
            _report.FinalHierarchySprites = CountHierarchySprites();
            _report.FinalPoolStatistics = ReadPoolStatistics();
            _report.FinalModelHash = Hash(_owner.RequiredGame.CaptureCheckpoint());
            _report.SourceHashAfter = Hash(File.ReadAllBytes(_source));
            _report.SourceUnchanged = _report.SourceHashBefore == _report.SourceHashAfter;
            _gpuRecorder.enabled = false;
            _report.ScreenshotValid = SaveScreenshot();
            if (_report.PresentationFrameCount < _report.SampleCount / 2 || _report.DrawFrameCount < _report.SampleCount / 2 || !_report.ScreenshotValid)
            {
                throw new InvalidOperationException("The hidden Player did not produce valid production presentation and rendered-pixel evidence.");
            }
            _report.Status = _report.SourceUnchanged ? "complete" : "failed";
            _report.Error = _report.SourceUnchanged ? string.Empty : "The source checkpoint changed while replaying.";
            WriteReport();
            if (!_report.SourceUnchanged)
            {
                throw new IOException(_report.Error);
            }
        }

        /// <summary>Reads explicit replay controls while keeping all output outside active save storage.</summary>
        private void ReadControls()
        {
            _source = Path.GetFullPath(ReadArgument("-pvz-replay-save") ?? throw new ArgumentException("A replay checkpoint path is required."));
            _output = Path.GetFullPath(ReadArgument("-pvz-replay-output") ?? throw new ArgumentException("A dedicated replay output directory is required."));
            if (Path.GetDirectoryName(_source) == _output
                || _output.Replace('\\', '/').StartsWith(Application.persistentDataPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Replay output must be separate from the source and active persistent storage.");
            }
            Directory.CreateDirectory(_output);
            _requestedFrames = Math.Max(1, ReadInt("-pvz-replay-frames", 900));
            _report.Label = ReadArgument("-pvz-replay-label") ?? "replay";
            _report.AdvanceTicks = Math.Max(0, ReadInt("-pvz-replay-advance-ticks", 6000));
            _report.WarmupFrames = Math.Max(120, ReadInt("-pvz-replay-warmup-frames", 120));
            _report.CounterBoundaryFrames = 2;
            _report.DurationSeconds = Math.Max(0, ReadInt("-pvz-replay-duration", 0));
            _report.Frozen = HasArgument("-pvz-replay-freeze");
            if (HasArgument("-pvz-replay-native-profiler") && (_report.DurationSeconds > 0f || _requestedFrames > 900))
            {
                throw new ArgumentException("Native GPU profiler probes are limited to 900 frames; use ordinary counters for long lifetime replays.");
            }
            _report.UnityVersion = Application.unityVersion;
            _report.Processor = SystemInfo.processorType;
            _report.GraphicsDevice = SystemInfo.graphicsDeviceName;
            _report.GraphicsApi = SystemInfo.graphicsDeviceType.ToString();
            _report.Editor = Application.isEditor;
#if ENABLE_IL2CPP
            _report.ActualScriptingBackend = "IL2CPP";
#elif ENABLE_CORECLR
            _report.ActualScriptingBackend = "CoreCLR";
#elif ENABLE_MONO
            _report.ActualScriptingBackend = "Mono";
#else
            _report.ActualScriptingBackend = "Unidentified";
#endif
            _report.Width = Screen.width;
            _report.Height = Screen.height;
            _report.VSyncCount = QualitySettings.vSyncCount;
            _report.TargetFrameRate = Application.targetFrameRate;
            _report.SourceHashBefore = Hash(File.ReadAllBytes(_source));
        }

        /// <summary>Restores through the production checksum validator and redirects every writable store.</summary>
        private void AttachCheckpoint()
        {
            Guid profile;
            int mode;
            using (var stream = File.OpenRead(_source))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadString() != "PVZRMATCH1")
                {
                    throw new InvalidDataException("The replay source is not a replica match checkpoint.");
                }
                profile = new Guid(reader.ReadBytes(16));
                mode = reader.ReadInt32();
            }
            var saves = Path.Combine(_output, "isolated-saves");
            Directory.CreateDirectory(saves);
            var store = new MatchCheckpointStore(saves);
            File.Copy(_source, store.SavePath(profile, mode), true);
            var game = store.Load(profile, mode, out _);
            var seeds = game.Adventure.SelectedSeeds.ToArray();
            if (game.ModeSession?.HasNextStage == true)
            {
                if (!game.ModeSession.Continue() || !game.ModeSession.ChooseSeeds(seeds))
                {
                    throw new InvalidOperationException("The saved mode could not continue with its original seed bank.");
                }
                _report.ContinuedStage = true;
            }
            for (var tick = 0; tick < _report.AdvanceTicks; tick++)
            {
                if (game.ModeSession != null)
                {
                    game.ModeSession.Update(.01f);
                }
                else
                {
                    game.Update(.01f);
                }
            }
            _owner._profileDialog?.Dispose();
            _owner._profileDialog = null;
            _owner._profiles = new ProfileCatalog(Path.Combine(saves, "profiles.dat"));
            if (_owner._profiles.Profiles.Count == 0)
            {
                _owner._profiles.Create("Performance replay");
            }
            _owner.Accounts._accountStore = new PlayerAccountStore(Path.Combine(saves, "player.account"));
            typeof(MatchContinuationController).GetField("_store", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_owner.Continuation, store);
            typeof(PlantsVsZombies.Modes.IndependentModeController).GetField("_modeProfileId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_owner.IndependentModes, _owner._profiles.RequiredSelectedId);
            _owner.IndependentModes._modeSession = game.ModeSession;
            _owner.Campaign._adventureTestBoard = true;
            _owner.Campaign.AttachAdventureGame(game, isRestored: true);
            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._choosingSeeds = false;
            _owner._unavailableFeature = null;
            if (game.State == MatchState.Paused)
            {
                game.TogglePause();
            }
            Time.timeScale = _report.Frozen ? 0f : 1f;
        }

        /// <summary>Binds optional candidate preparation without adding dependencies to the baseline build.</summary>
        private void BindPreparationGate()
        {
            var property = typeof(PvZGame).GetProperty("Preparation", BindingFlags.Instance | BindingFlags.NonPublic);
            var preparation = property?.GetValue(_owner);
            var getter = preparation?.GetType().GetProperty("IsReady", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(true);
            if (getter != null)
            {
                _preparationReady = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), preparation, getter);
                _report.PreparationGateAvailable = true;
            }
        }

        /// <summary>Reads value counters outside measurement without introducing per-frame reflection boxing.</summary>
        /// <returns>Readable pool counters, or an explicit baseline-unavailable marker.</returns>
        private string ReadPoolStatistics()
        {
            var property = typeof(GameSimulation).GetProperty("RuntimePoolStatistics", BindingFlags.Instance | BindingFlags.NonPublic);
            var snapshot = property?.GetValue(_owner.RequiredGame);
            if (snapshot == null)
            {
                return "Unavailable in this source revision";
            }
            var values = snapshot.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return string.Join(";", values.Select(value => value.Name + "=" + value.GetValue(snapshot)));
        }

        /// <summary>Counts live scene inspection nodes outside measured frames.</summary>
        /// <returns>The number of actor inspection components in valid runtime scenes.</returns>
        private static int CountHierarchySprites()
        {
            return Resources.FindObjectsOfTypeAll<SpriteHierarchySprite>().Count(item => item.gameObject.scene.IsValid());
        }

        /// <summary>Submits the complete normal scene and camera Canvas to a GPU target after production LateUpdate.</summary>
        private void LateUpdate()
        {
            if (_target != null)
            {
                _gpuSampler?.Begin();
                try
                {
                    Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(ObjectSpriteRenderer.SceneCamera, _renderRequest);
                }
                finally
                {
                    _gpuSampler?.End();
                }
            }
        }

        /// <summary>Stores one bounded cycle snapshot without hiding evidence truncation.</summary>
        /// <param name="cycles">The preallocated destination array.</param>
        /// <param name="count">The number of recorded snapshots.</param>
        /// <param name="cycle">The observed restored-cycle identifier.</param>
        /// <param name="sampleIndex">The sample boundary immediately following this snapshot.</param>
        /// <param name="ending">Whether the cycle is ending rather than prepared.</param>
        private void AddCycle(PerformanceReplayCycle[] cycles, ref int count, int cycle, int sampleIndex, bool ending)
        {
            if (count >= cycles.Length)
            {
                _report.CycleEvidenceTruncated = true;
                return;
            }
            cycles[count++] = ReadCycle(cycle, sampleIndex, ending);
        }

        /// <summary>Reads bounded cycle evidence during a separately classified transition frame.</summary>
        /// <param name="cycle">The restored cycle index.</param>
        /// <param name="sampleIndex">The first subsequent sample index.</param>
        /// <param name="ending">Whether the cycle is ending rather than prepared.</param>
        /// <returns>The cycle's resource and pool context.</returns>
        private PerformanceReplayCycle ReadCycle(int cycle, int sampleIndex, bool ending)
        {
            return new PerformanceReplayCycle
            {
                Cycle = cycle,
                Boundary = ending ? 1 : 0,
                SampleIndex = sampleIndex,
                PoolStatistics = ReadPoolStatistics(),
                PoolCreatedCount = ReadPoolNumber("CreatedCount"),
                PoolActiveCount = ReadPoolNumber("ActiveCount"),
                PoolRecycledCount = ReadPoolNumber("RecycledCount"),
                NativeAllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                GraphicsDriverAllocatedBytes = ReadGraphicsDriverMemory(),
                ManagedBytes = GC.GetTotalMemory(false),
                HierarchySprites = CountHierarchySprites(),
                Meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length,
                Textures = Resources.FindObjectsOfTypeAll<Texture>().Length,
                Materials = Resources.FindObjectsOfTypeAll<Material>().Length
            };
        }

        /// <summary>Reads Unity's driver allocation counter without presenting unavailable zero readings as measured memory.</summary>
        /// <returns>Positive allocated bytes reported by Unity, or minus one when unavailable.</returns>
        private static long ReadGraphicsDriverMemory()
        {
            var bytes = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
            return bytes > 0 ? bytes : -1;
        }

        /// <summary>Reads a numeric model pool counter only during classified evidence collection.</summary>
        /// <param name="name">The optional snapshot property name.</param>
        /// <returns>The counter value, or minus one when the baseline does not implement it.</returns>
        private long ReadPoolNumber(string name)
        {
            var property = typeof(GameSimulation).GetProperty("RuntimePoolStatistics", BindingFlags.Instance | BindingFlags.NonPublic);
            var snapshot = property?.GetValue(_owner.RequiredGame);
            var counter = snapshot?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return counter == null ? -1 : Convert.ToInt64(counter.GetValue(snapshot), CultureInfo.InvariantCulture);
        }

        /// <summary>Stores real GPU pixels and rejects an empty or uniformly cleared target.</summary>
        /// <returns>Whether opaque nonuniform game pixels were observed.</returns>
        private bool SaveScreenshot()
        {
            if (_target == null)
            {
                return false;
            }
            var previous = RenderTexture.active;
            var pixels = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = _target;
                pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(_output, "frame.png"), pixels.EncodeToPNG());
                var data = pixels.GetPixels32();
                var first = data[0];
                var different = 0;
                var opaque = 0;
                for (var index = 0; index < data.Length; index += 97)
                {
                    opaque += data[index].a > 200 ? 1 : 0;
                    different += data[index].Equals(first) ? 0 : 1;
                }
                return opaque > 1000 && different > 1000;
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(pixels);
            }
        }

        /// <summary>Releases diagnostic-only native recorders and render targets with the replay host.</summary>
        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeginGpuScene;
            RenderPipelineManager.endCameraRendering -= EndGpuScene;
            (_routine as IDisposable)?.Dispose();
            _routine = null;
            if (_gpuRecorder != null && _gpuRecorder.isValid)
            {
                _gpuRecorder.enabled = false;
            }
            if (_report.NativeProfilerEnabled)
            {
                UnityEngine.Profiling.Profiler.enabled = false;
                UnityEngine.Profiling.Profiler.logFile = string.Empty;
                UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            }
            if (_target != null)
            {
                if (_sceneCamera != null && _sceneCamera.targetTexture == _target)
                {
                    _sceneCamera.targetTexture = null;
                }
                if (RenderTexture.active == _target)
                {
                    RenderTexture.active = null;
                }
                _target.Release();
                Destroy(_target);
                _target = null;
            }
            _gpuCommands?.Release();
            _gpuCommands = null;
            Time.timeScale = 1f;
        }

        /// <summary>Records the beginning of the actual scene GPU command span.</summary>
        /// <param name="context">The production pipeline's current context.</param>
        /// <param name="camera">The camera about to render.</param>
        private void BeginGpuScene(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _sceneCamera || _gpuCommands == null || _gpuSampler == null)
            {
                return;
            }
#pragma warning disable CS0618 // The replay reads CustomSampler's GPU recorder, requiring this verified handle overload.
            _gpuCommands.BeginSample(_gpuSampler);
#pragma warning restore CS0618
            context.ExecuteCommandBuffer(_gpuCommands);
            _gpuCommands.Clear();
        }

        /// <summary>Completes and submits the matching GPU timestamp after the scene's draw commands.</summary>
        /// <param name="context">The production pipeline's current context.</param>
        /// <param name="camera">The camera that completed rendering.</param>
        private void EndGpuScene(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _sceneCamera || _gpuCommands == null || _gpuSampler == null)
            {
                return;
            }
#pragma warning disable CS0618 // Match the CustomSampler handle used by the beginning command.
            _gpuCommands.EndSample(_gpuSampler);
#pragma warning restore CS0618
            context.ExecuteCommandBuffer(_gpuCommands);
            _gpuCommands.Clear();
            context.Submit();
        }

        /// <summary>Persists evidence only before or after measurement.</summary>
        private void WriteReport()
        {
            File.WriteAllText(Path.Combine(_output, "replay.json"), JsonUtility.ToJson(_report, true));
        }

        /// <summary>Computes a stable hash without changing the source model.</summary>
        /// <param name="bytes">The source bytes.</param>
        /// <returns>The uppercase SHA-256 hexadecimal string.</returns>
        private static string Hash(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        /// <summary>Reads one numeric command-line control.</summary>
        /// <param name="name">The command-line option.</param>
        /// <param name="fallback">The default value.</param>
        /// <returns>The selected integer.</returns>
        private static int ReadInt(string name, int fallback)
        {
            return int.TryParse(ReadArgument(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        }

        /// <summary>Reads a command-line value without changing it.</summary>
        /// <param name="name">The requested option.</param>
        /// <returns>The following argument, or null when absent.</returns>
        private static string? ReadArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
        }

        /// <summary>Checks whether a flag is present.</summary>
        /// <param name="name">The requested flag.</param>
        /// <returns>Whether the flag exists.</returns>
        private static bool HasArgument(string name)
        {
            return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
        }
    }
}
#endif
