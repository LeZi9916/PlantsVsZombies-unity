#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.Shared;
using PlantsVsZombies.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Replays an external, read-only checkpoint through production simulation and presentation.</summary>
    public sealed class CheckpointRenderingBenchmarkTests
    {
        /// <summary>Measures one deterministic external board without changing the source or personal accounts.</summary>
        /// <returns>Player frame boundaries required for real camera submission.</returns>
        [UnityTest]
        public IEnumerator RecordsExternalCheckpoint()
        {
            var source = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_CHECKPOINT");
            var output = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_OUTPUT");
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(output) || string.IsNullOrEmpty(root))
            {
                Assert.Ignore("Set PVZ_RENDER_BENCHMARK_CHECKPOINT, PVZ_RENDER_BENCHMARK_OUTPUT and PVZ_BOARD_STRESS_ASSETS.");
            }
            if (Environment.GetEnvironmentVariable("PVZ_RENDER_USE_BAKED") == "1")
            {
                root = Application.streamingAssetsPath;
            }
            var advanceTicks = ReadCount("PVZ_RENDER_BENCHMARK_TICKS", 0, 1000000);
            var frames = ReadCount("PVZ_RENDER_BENCHMARK_FRAMES", 900, 18000);
            var advancing = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_ADVANCE") == "1";
            var profileAllocations = Environment.GetEnvironmentVariable("PVZ_RENDER_PROFILE_ALLOCATIONS") == "1";
#if UNITY_EDITOR
            ProfilerDriver.enabled = false;
            ProfilerDriver.deepProfiling = false;
            UnityEngine.Profiling.Profiler.enableAllocationCallstacks = false;
#endif
            var sourceBytes = File.ReadAllBytes(source!);
            var sourceHash = Hash(sourceBytes);
            var directory = Path.Combine(Path.GetFullPath(output!), "isolated-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Guid profile;
            int mode;
            using (var reader = new BinaryReader(new MemoryStream(sourceBytes, false)))
            {
                Assert.That(reader.ReadString(), Is.EqualTo("PVZRMATCH1"));
                profile = new Guid(reader.ReadBytes(16));
                mode = reader.ReadInt32();
            }
            var store = new MatchCheckpointStore(directory);
            File.WriteAllBytes(store.SavePath(profile, mode), sourceBytes);
            var simulation = store.Load(profile, mode, out _);
            if (Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_NEXT_STAGE") == "1")
            {
                var seeds = new PlantKind[simulation.Adventure.SelectedSeeds.Count];
                for (var i = 0; i < seeds.Length; i++)
                {
                    seeds[i] = simulation.Adventure.SelectedSeeds[i];
                }
                Assert.That(simulation.ModeSession, Is.Not.Null);
                Assert.That(simulation.ModeSession!.Continue(), Is.True);
                Assert.That(simulation.ModeSession.ChooseSeeds(seeds), Is.True);
            }
            for (var tick = 0; tick < advanceTicks; tick++)
            {
                Advance(simulation);
            }

            var host = new GameObject("Isolated checkpoint rendering benchmark");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root!);
            var options = new OptionsView(root!);
            var presenter = new GamePresenter(owner);
            var camera = ObjectSpriteRenderer.SceneCamera;
            var previousTarget = camera.targetTexture;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var timings = new FrameTiming[1];
            var samples = new FrameSample[frames];
            var backendProperty = typeof(ObjectSpriteRenderer).GetProperty("Backend", BindingFlags.Static | BindingFlags.NonPublic);
            var previousBackend = backendProperty?.GetValue(null);
            var backend = Environment.GetEnvironmentVariable("PVZ_RENDER_BACKEND");
            var scenePools = (List<ObjectSpriteRenderer>)typeof(ObjectSpriteRenderer)
                .GetField("s_scenePools", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var instanceDraws = BindRendererCounter("InstancedDrawCount", -1);
            var instanceUploads = BindRendererCounter("InstanceUploadCount", -1);
            var instanceUploadBytes = BindRendererCounter("InstanceUploadBytes", -1L);
            var instanceGpuBytes = BindRendererCounter("InstancingGpuBytes", -1L);
            var preparedInstances = BindRendererCounter("PreparedInstanceCapacity", -1);
            var fallbackInstances = BindRendererCounter("InstancingFallbackCount", -1);
            var gpuBufferCreations = BindRendererCounter("GpuBufferCreationCount", -1L);
            var instanceBufferCreations = BindRendererCounter("InstanceBufferCreationCount", -1L);
            var meshReconfigurations = BindRendererCounter("MeshBufferReconfigurationCount", -1L);
            var meshCreations = BindRendererCounter("MeshCreationCount", -1L);
            var meshUploadBytes = BindRendererCounter("MeshUploadBytes", -1L);
            var textureSplits = BindRendererCounter("InstanceTextureBankSplitCount", -1L);
            var blendSplits = BindRendererCounter("InstanceBlendSplitCount", -1L);
            var painterSplits = BindRendererCounter("InstancePainterOrderSplitCount", -1L);
            var capacitySplits = BindRendererCounter("InstanceCapacitySplitCount", -1L);
            var submissionBindings = BindRendererCounter("InstanceSubmissionBindingCreationCount", -1L);
            var sameFrameFallbacks = BindRendererCounter("InstanceSameFrameBufferFallbackCount", -1L);
            var hierarchyLabels = BindCounter(animations.Hierarchy, "ActorLabelMissCount", -1);
            var hierarchyActors = BindCounter(animations.Hierarchy, "ActorRootCreationCount", -1);
            var hierarchySprites = BindCounter(animations.Hierarchy, "SpriteNodeCreationCount", -1);
            using var simulationAllocations = NewAllocationRecorder();
            using var presentationAllocations = NewAllocationRecorder();
            using var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var frameBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            try
            {
                if (backendProperty != null && !string.IsNullOrEmpty(backend))
                {
                    backendProperty.SetValue(null, Enum.Parse(backendProperty.PropertyType, backend, true));
                }
                owner._root = root!;
                owner._presenter = presenter;
                owner._smokeMode = true;
                owner._game = simulation;
                owner._animations = animations;
                owner._optionsView = options;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._deck.Clear();
                owner._deck.AddRange(simulation.Adventure.SelectedSeeds);
                owner._optionsBitmap = MenuController.LoadBitmapFont("DwarvenTodcraft18");
                owner._modeTitleBitmap = MenuController.LoadBitmapFont("HouseofTerror28");
                owner._modeLabelBitmap = MenuController.LoadBitmapFont("BrianneTod12");
                owner._sunCounterBitmap = MenuController.LoadBitmapFont("ContinuumBold14");
                owner._choosingSeeds = false;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                GameMemberReflection.SetField(owner, "_modeSession", simulation.ModeSession);
                GameMemberReflection.SetField(owner, "_adventureTestBoard", true);
                if (simulation.State == MatchState.Paused)
                {
                    simulation.TogglePause();
                }
                target.Create();
                camera.targetTexture = target;
                CheckAllocationRecorder(simulationAllocations);
                CheckAllocationRecorder(presentationAllocations);
                PrepareSimulation(simulation);
                PrepareGame(owner);
                for (var warm = 0; warm < 120; warm++)
                {
                    presenter.Present();
                    yield return null;
                }
                var startPayload = Hash(simulation.CaptureCheckpoint());
                var initialPool = ReadPoolStatistics(simulation);
                var initialGpuBufferCreations = gpuBufferCreations();
                var initialPreparedInstances = preparedInstances();
                var initialInstanceGpuBytes = instanceGpuBytes();
                var initialHierarchyLabels = hierarchyLabels();
                var initialHierarchyActors = hierarchyActors();
                var initialHierarchySprites = hierarchySprites();
                var initialInstanceBufferCreations = instanceBufferCreations();
                var initialMeshReconfigurations = meshReconfigurations();
                var initialMeshCreations = meshCreations();
                var initialMeshUploadBytes = meshUploadBytes();
                var initialTextureSplits = textureSplits();
                var initialBlendSplits = blendSplits();
                var initialPainterSplits = painterSplits();
                var initialCapacitySplits = capacitySplits();
                var initialSubmissionBindings = submissionBindings();
                var initialSameFrameFallbacks = sameFrameFallbacks();
#if UNITY_EDITOR
                if (profileAllocations)
                {
                    Directory.CreateDirectory(output!);
                    ProfilerDriver.ClearAllFrames();
                    ProfilerDriver.profileEditor = true;
                    ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, true);
                    UnityEngine.Profiling.Profiler.logFile = Path.Combine(output!, "allocations.raw");
                    UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                    UnityEngine.Profiling.Profiler.enableAllocationCallstacks = true;
                    ProfilerDriver.enabled = true;
                    yield return null;
                }
#endif
                for (var frame = 0; frame < frames; frame++)
                {
                    if (profileAllocations)
                    {
                        UnityEngine.Profiling.Profiler.BeginSample("Checkpoint.Simulation");
                    }
                    simulationAllocations.Reset();
                    simulationAllocations.Start();
                    var updateBytes = GC.GetAllocatedBytesForCurrentThread();
                    var updateStart = Stopwatch.GetTimestamp();
                    if (advancing)
                    {
                        Advance(simulation);
                    }
                    var updateEnd = Stopwatch.GetTimestamp();
                    updateBytes = GC.GetAllocatedBytesForCurrentThread() - updateBytes;
                    simulationAllocations.Stop();
                    if (profileAllocations)
                    {
                        UnityEngine.Profiling.Profiler.EndSample();
                        UnityEngine.Profiling.Profiler.BeginSample("Checkpoint.Presentation");
                    }
                    presentationAllocations.Reset();
                    presentationAllocations.Start();
                    var presentBytes = GC.GetAllocatedBytesForCurrentThread();
                    var presentStart = Stopwatch.GetTimestamp();
                    presenter.Present();
                    var presentEnd = Stopwatch.GetTimestamp();
                    presentBytes = GC.GetAllocatedBytesForCurrentThread() - presentBytes;
                    presentationAllocations.Stop();
                    if (profileAllocations)
                    {
                        UnityEngine.Profiling.Profiler.EndSample();
                    }
                    Assert.That(simulationAllocations.Count, Is.LessThan(simulationAllocations.Capacity));
                    Assert.That(presentationAllocations.Count, Is.LessThan(presentationAllocations.Capacity));
                    samples[frame] = new FrameSample
                    {
                        SimulationMs = (updateEnd - updateStart) * 1000d / Stopwatch.Frequency,
                        PresentationMs = (presentEnd - presentStart) * 1000d / Stopwatch.Frequency,
                        SimulationAllocationEvents = simulationAllocations.Count,
                        PresentationAllocationEvents = presentationAllocations.Count,
                        SimulationAllocationBytes = updateBytes,
                        PresentationAllocationBytes = presentBytes,
                        InstanceDraws = instanceDraws(),
                        InstanceUploads = instanceUploads(),
                        InstanceUploadBytes = instanceUploadBytes(),
                        InstanceGpuBytes = instanceGpuBytes(),
                        PreparedInstances = preparedInstances(),
                        FallbackInstances = fallbackInstances(),
                        GpuBufferCreations = gpuBufferCreations(),
                        SubmittedSceneSpriteDrawCalls = CountSceneSpriteDraws(scenePools),
                        InstanceBufferCreations = instanceBufferCreations(),
                        MeshBufferReconfigurations = meshReconfigurations(),
                        MeshCreations = meshCreations(),
                        MeshUploadBytes = meshUploadBytes(),
                        TextureBankSplits = textureSplits(),
                        BlendSplits = blendSplits(),
                        PainterOrderSplits = painterSplits(),
                        CapacitySplits = capacitySplits(),
                        SubmissionBindingCreations = submissionBindings(),
                        SameFrameBufferFallbacks = sameFrameFallbacks(),
                        HierarchyLabelMisses = hierarchyLabels(),
                        HierarchyActorCreations = hierarchyActors(),
                        HierarchySpriteCreations = hierarchySprites(),
                        Plants = simulation.Plants.Count,
                        Zombies = simulation.Zombies.Count,
                        Peas = simulation.Peas.Count,
                        Elapsed = simulation.Elapsed,
                        RandomDraws = simulation.BattleRandomDrawCount
                    };
                    FrameTimingManager.CaptureFrameTimings();
                    yield return null;
                    samples[frame].DrawCalls = drawCalls.Valid ? drawCalls.LastValue : -1;
                    samples[frame].WholeFrameGcBytes = frameBytes.Valid ? frameBytes.LastValue : -1;
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    {
                        samples[frame].GpuMs = timings[0].gpuFrameTime;
                    }
                }
#if UNITY_EDITOR
                if (profileAllocations)
                {
                    ExportAllocationStacks(output!);
                }
#endif
                var result = new Measurement
                {
                    UnityVersion = Application.unityVersion,
                    GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                    GraphicsDevice = SystemInfo.graphicsDeviceName,
                    Processor = SystemInfo.processorType,
                    Backend = backend ?? "Auto",
                    SourceSha256 = sourceHash,
                    StartPayloadSha256 = startPayload,
                    FinalPayloadSha256 = Hash(simulation.CaptureCheckpoint()),
                    AdvanceTicks = advanceTicks,
                    Advancing = advancing,
                    InitialPool = initialPool,
                    FinalPool = ReadPoolStatistics(simulation),
                    InitialGpuBufferCreations = initialGpuBufferCreations,
                    InitialPreparedInstances = initialPreparedInstances,
                    InitialInstanceGpuBytes = initialInstanceGpuBytes,
                    InitialHierarchyLabelMisses = initialHierarchyLabels,
                    InitialHierarchyActorCreations = initialHierarchyActors,
                    InitialHierarchySpriteCreations = initialHierarchySprites,
                    InitialInstanceBufferCreations = initialInstanceBufferCreations,
                    InitialMeshBufferReconfigurations = initialMeshReconfigurations,
                    InitialMeshCreations = initialMeshCreations,
                    InitialMeshUploadBytes = initialMeshUploadBytes,
                    InitialTextureBankSplits = initialTextureSplits,
                    InitialBlendSplits = initialBlendSplits,
                    InitialPainterOrderSplits = initialPainterSplits,
                    InitialCapacitySplits = initialCapacitySplits,
                    InitialSubmissionBindingCreations = initialSubmissionBindings,
                    InitialSameFrameBufferFallbacks = initialSameFrameFallbacks,
                    Samples = samples,
                    AllocationCounter = "Current-thread GC.Alloc event count; positive control required. Whole-frame bytes include the test runner."
                };
                Directory.CreateDirectory(output!);
                File.WriteAllText(Path.Combine(output!, "checkpoint.json"), JsonUtility.ToJson(result, true));
                File.WriteAllBytes(Path.Combine(output!, "final-checkpoint-payload.dat"), simulation.CaptureCheckpoint());
                SaveScreenshot(target, Path.Combine(output!, "checkpoint.png"));
                Assert.That(Hash(File.ReadAllBytes(source!)), Is.EqualTo(sourceHash), "The external checkpoint must remain unchanged.");
                UnityEngine.Debug.Log("External checkpoint benchmark: " + Path.Combine(output!, "checkpoint.json"));
            }
            finally
            {
#if UNITY_EDITOR
                if (profileAllocations)
                {
                    ProfilerDriver.enabled = false;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = false;
                    UnityEngine.Profiling.Profiler.enableAllocationCallstacks = false;
                }
#endif
                if (backendProperty != null)
                {
                    backendProperty.SetValue(null, previousBackend);
                }
                camera.targetTexture = previousTarget;
                presenter.Dispose();
                animations.Dispose();
                options.Dispose();
                owner._optionsBitmap?.Dispose();
                owner._modeTitleBitmap?.Dispose();
                owner._modeLabelBitmap?.Dispose();
                owner._sunCounterBitmap?.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(host);
            }
        }

#if UNITY_EDITOR
        /// <summary>Groups raw allocation callstacks only inside the explicitly measured production traversals.</summary>
        /// <param name="output">The ignored allocation evidence directory.</param>
        private static void ExportAllocationStacks(string output)
        {
            ProfilerDriver.enabled = false;
            UnityEngine.Profiling.Profiler.logFile = string.Empty;
            UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            UnityEngine.Profiling.Profiler.enableAllocationCallstacks = false;
            ProfilerDriver.SaveProfile(Path.Combine(output, "allocations-editor.raw"));
            var groups = new Dictionary<string, long[]>();
            var stack = new List<ulong>();
            var names = new StringBuilder();
            for (var frame = ProfilerDriver.firstFrameIndex; frame <= ProfilerDriver.lastFrameIndex; frame++)
            {
                using var data = ProfilerDriver.GetRawFrameDataView(frame, 0);
                if (!data.valid)
                {
                    continue;
                }
                var scope = string.Empty;
                var end = -1;
                for (var sample = 0; sample < data.sampleCount; sample++)
                {
                    var name = data.GetSampleName(sample);
                    if (name == "Checkpoint.Simulation" || name == "Checkpoint.Presentation")
                    {
                        scope = name;
                        end = sample + data.GetSampleChildrenCountRecursive(sample) + 1;
                    }
                    if (sample >= end || name != "GC.Alloc")
                    {
                        continue;
                    }
                    names.Clear();
                    names.AppendLine(scope);
                    stack.Clear();
                    data.GetSampleCallstack(sample, stack);
                    foreach (var address in stack)
                    {
                        var method = data.ResolveMethodInfo(address);
                        if (!string.IsNullOrEmpty(method.methodName))
                        {
                            names.AppendLine(method.methodName);
                        }
                    }
                    var key = names.ToString();
                    if (!groups.TryGetValue(key, out var count))
                    {
                        count = new long[2];
                        groups.Add(key, count);
                    }
                    count[0]++;
                    if (data.GetSampleMetadataCount(sample) > 0)
                    {
                        count[1] += data.GetSampleMetadataAsLong(sample, 0);
                    }
                }
            }
            var sorted = new List<KeyValuePair<string, long[]>>(groups);
            sorted.Sort((left, right) => right.Value[0].CompareTo(left.Value[0]));
            names.Clear();
            foreach (var group in sorted)
            {
                names.Append("COUNT=").Append(group.Value[0]).Append(" BYTES=").Append(group.Value[1]).AppendLine();
                names.AppendLine(group.Key).AppendLine();
            }
            File.WriteAllText(Path.Combine(output, "allocation-stacks.txt"), names.ToString());
        }
#endif

        /// <summary>Advances the authoritative mode facade by exactly one native tick.</summary>
        /// <param name="simulation">The detached board being replayed.</param>
        private static void Advance(GameSimulation simulation)
        {
            if (simulation.ModeSession != null)
            {
                simulation.ModeSession.Update(.01f);
            }
            else
            {
                simulation.Update(.01f);
            }
        }

        /// <summary>Supports comparing revisions that precede the capacity preparation interface.</summary>
        /// <param name="simulation">The board whose derived storage may be prepared.</param>
        private static void PrepareSimulation(GameSimulation simulation)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var requirementsMethod = typeof(GameSimulation).GetMethod("GetCapacityRequirements", flags);
            var prepareMethod = typeof(GameSimulation).GetMethod("PrepareCapacityStep", flags);
            if (requirementsMethod == null || prepareMethod == null)
            {
                return;
            }
            var requirements = requirementsMethod.Invoke(simulation, null);
            var arguments = new[] { requirements, (object)16 };
            for (var step = 0; step < 100000; step++)
            {
                if ((bool)prepareMethod.Invoke(simulation, arguments)!)
                {
                    return;
                }
            }
            Assert.Fail("Capacity preparation did not converge.");
        }

        /// <summary>Binds an optional primitive counter once so frame observations do not allocate through reflection.</summary>
        /// <typeparam name="T">The exact primitive property type.</typeparam>
        /// <param name="name">The renderer property shared with newer revisions.</param>
        /// <param name="unavailable">The explicit value recorded by older revisions.</param>
        /// <returns>A retained allocation-free getter.</returns>
        private static Func<T> BindRendererCounter<T>(string name, T unavailable)
        {
            var property = typeof(ObjectSpriteRenderer).GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic);
            var getter = property?.GetGetMethod(true);
            return getter == null ? () => unavailable : (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), getter);
        }

        /// <summary>Counts submitted scene sprite batches separately from Canvas, water and engine draw counters.</summary>
        /// <param name="pools">The retained scene-pool list shared by both compared revisions.</param>
        /// <returns>The completed scene sprite draw submissions.</returns>
        private static int CountSceneSpriteDraws(List<ObjectSpriteRenderer> pools)
        {
            var draws = 0;
            for (var index = 0; index < pools.Count; index++)
            {
                draws += pools[index].BatchCount;
            }
            return draws;
        }

        /// <summary>Binds an optional instance diagnostic before entering the warmed measurement window.</summary>
        /// <typeparam name="T">The exact primitive property type.</typeparam>
        /// <param name="owner">The retained diagnostic owner.</param>
        /// <param name="name">The property provided by compatible revisions.</param>
        /// <param name="unavailable">The value for older implementations.</param>
        /// <returns>An allocation-free getter bound to that owner.</returns>
        private static Func<T> BindCounter<T>(object owner, string name, T unavailable)
        {
            var getter = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(true);
            return getter == null ? () => unavailable : (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), owner, getter);
        }

        /// <summary>Reads a match-pool snapshot outside all measured traversals, including on pre-pooling revisions.</summary>
        /// <param name="simulation">The board whose cumulative capacity is observed.</param>
        /// <returns>The supported counters or explicit unavailable values.</returns>
        private static PoolSnapshot ReadPoolStatistics(GameSimulation simulation)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var value = typeof(GameSimulation).GetProperty("RuntimePoolStatistics", flags)?.GetValue(simulation);
            if (value == null)
            {
                return new PoolSnapshot { Created = -1, Active = -1, Recycled = -1 };
            }
            var type = value.GetType();
            return new PoolSnapshot
            {
                Created = (int)type.GetProperty("CreatedCount", flags)!.GetValue(value)!,
                Active = (int)type.GetProperty("ActiveCount", flags)!.GetValue(value)!,
                Recycled = (long)type.GetProperty("RecycledCount", flags)!.GetValue(value)!
            };
        }

        /// <summary>Creates an initially stopped recorder outside measured scopes.</summary>
        /// <returns>A valid current-thread allocation event recorder.</returns>
        private static ProfilerRecorder NewAllocationRecorder()
        {
            var recorder = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 1048576, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(recorder.Valid, Is.True);
            return recorder;
        }

        /// <summary>Runs optional cooperative preparation outside the measured traversal on compatible revisions.</summary>
        /// <param name="owner">The isolated production host.</param>
        private static void PrepareGame(PvZGame owner)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var property = typeof(PvZGame).GetProperty("Preparation", flags);
            if (property == null)
            {
                return;
            }
            var preparation = property.GetValue(owner)!;
            var originalPayload = Hash(owner.RequiredGame.CaptureCheckpoint());
            var advance = preparation.GetType().GetMethod("Advance", flags)!;
            var ready = preparation.GetType().GetProperty("IsReady", flags)!;
            for (var step = 0; step < 100000; step++)
            {
                advance.Invoke(preparation, null);
                if ((bool)ready.GetValue(preparation)!)
                {
                    Assert.That(Hash(owner.RequiredGame.CaptureCheckpoint()), Is.EqualTo(originalPayload),
                        "Preparing derived resources must not alter any authoritative field or random draw.");
                    return;
                }
            }
            Assert.Fail("Presentation preparation did not converge.");
        }

        /// <summary>Rejects runtimes whose recorder cannot observe a deliberate allocation.</summary>
        /// <param name="recorder">The native recorder used for the following timed scopes.</param>
        private static void CheckAllocationRecorder(ProfilerRecorder recorder)
        {
            recorder.Reset();
            recorder.Start();
            var sentinel = new byte[1024];
            recorder.Stop();
            GC.KeepAlive(sentinel);
            Assert.That(recorder.Count, Is.GreaterThan(0));
            recorder.Reset();
        }

        /// <summary>Reads a bounded workload parameter before measurement begins.</summary>
        /// <param name="name">The environment variable.</param>
        /// <param name="fallback">The default count.</param>
        /// <param name="maximum">The maximum admitted count.</param>
        /// <returns>The requested count, or the default when absent.</returns>
        private static int ReadCount(string name, int fallback, int maximum)
        {
            var text = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }
            Assert.That(int.TryParse(text, out var value) && value >= 0 && value <= maximum, Is.True, name);
            return value;
        }

        /// <summary>Computes a stable digest outside timed scopes.</summary>
        /// <param name="bytes">The complete checkpoint bytes.</param>
        /// <returns>An uppercase hexadecimal SHA-256 digest.</returns>
        private static string Hash(byte[] bytes)
        {
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        /// <summary>Captures final pixels outside allocation and CPU measurements.</summary>
        /// <param name="target">The last rendered game target.</param>
        /// <param name="path">The ignored PNG output.</param>
        private static void SaveScreenshot(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>Stores all observations, including event frames and allocation failures.</summary>
        [Serializable]
        private sealed class Measurement
        {
            /// <summary>The exact Editor or Player version.</summary>
            public string UnityVersion = string.Empty;
            /// <summary>The graphics API under test.</summary>
            public string GraphicsApi = string.Empty;
            /// <summary>The graphics device under test.</summary>
            public string GraphicsDevice = string.Empty;
            /// <summary>The processor under test.</summary>
            public string Processor = string.Empty;
            /// <summary>The selected rendering backend.</summary>
            public string Backend = string.Empty;
            /// <summary>The immutable source checkpoint digest.</summary>
            public string SourceSha256 = string.Empty;
            /// <summary>The authoritative model digest before timed ticks.</summary>
            public string StartPayloadSha256 = string.Empty;
            /// <summary>The authoritative model digest after timed ticks.</summary>
            public string FinalPayloadSha256 = string.Empty;
            /// <summary>The deterministic offset before warm-up.</summary>
            public int AdvanceTicks;
            /// <summary>Whether timed frames include one native simulation tick.</summary>
            public bool Advancing;
            /// <summary>The pool capacity immediately after warm-up.</summary>
            public PoolSnapshot InitialPool;
            /// <summary>The cumulative pool capacity after every measured event frame.</summary>
            public PoolSnapshot FinalPool;
            /// <summary>The cumulative GPU buffer creations immediately after warm-up, or minus one.</summary>
            public long InitialGpuBufferCreations;
            /// <summary>The actual structured-buffer creations immediately after warm-up, or minus one.</summary>
            public long InitialInstanceBufferCreations;
            /// <summary>The cumulative Mesh vertex/index buffer reconfigurations after warm-up, or minus one.</summary>
            public long InitialMeshBufferReconfigurations;
            /// <summary>The cumulative Mesh objects created after warm-up, or minus one.</summary>
            public long InitialMeshCreations;
            /// <summary>The cumulative Mesh upload bytes after warm-up, or minus one.</summary>
            public long InitialMeshUploadBytes;
            /// <summary>The cumulative texture-bank splits after warm-up, or minus one.</summary>
            public long InitialTextureBankSplits;
            /// <summary>The cumulative hardware blend splits after warm-up, or minus one.</summary>
            public long InitialBlendSplits;
            /// <summary>The cumulative painter barriers after warm-up, or minus one.</summary>
            public long InitialPainterOrderSplits;
            /// <summary>The cumulative fixed-chunk splits after warm-up, or minus one.</summary>
            public long InitialCapacitySplits;
            /// <summary>The cumulative immutable native binding snapshots after warm-up, or minus one.</summary>
            public long InitialSubmissionBindingCreations;
            /// <summary>The cumulative same-frame buffer protection fallbacks after warm-up, or minus one.</summary>
            public long InitialSameFrameBufferFallbacks;
            /// <summary>The instance slots prepared immediately before the first measured traversal, or minus one.</summary>
            public int InitialPreparedInstances;
            /// <summary>The fixed instance GPU bytes retained immediately before measurement, or minus one.</summary>
            public long InitialInstanceGpuBytes;
            /// <summary>The Editor model-label misses before measurement, or minus one.</summary>
            public int InitialHierarchyLabelMisses;
            /// <summary>The Editor actor roots created before measurement, or minus one.</summary>
            public int InitialHierarchyActorCreations;
            /// <summary>The Editor sprite components created before measurement, or minus one.</summary>
            public int InitialHierarchySpriteCreations;
            /// <summary>The precise scope and units of allocation evidence.</summary>
            public string AllocationCounter = string.Empty;
            /// <summary>The complete unfiltered frame observations.</summary>
            public FrameSample[] Samples = Array.Empty<FrameSample>();
        }

        /// <summary>Stores one frame without creating a diagnostic object in the timed loop.</summary>
        [Serializable]
        private struct FrameSample
        {
            /// <summary>The CPU duration of the authoritative update.</summary>
            public double SimulationMs;
            /// <summary>The CPU duration of complete presentation submission.</summary>
            public double PresentationMs;
            /// <summary>The allocation events inside the authoritative update.</summary>
            public int SimulationAllocationEvents;
            /// <summary>The allocation events inside presentation submission.</summary>
            public int PresentationAllocationEvents;
            /// <summary>The managed bytes allocated inside the authoritative update.</summary>
            public long SimulationAllocationBytes;
            /// <summary>The managed bytes allocated inside presentation submission.</summary>
            public long PresentationAllocationBytes;
            /// <summary>The scene's ordered instance draws, or minus one on older revisions.</summary>
            public int InstanceDraws;
            /// <summary>The scene's changed instance-buffer prefixes, or minus one.</summary>
            public int InstanceUploads;
            /// <summary>The instance and clipped-vertex bytes uploaded this frame, or minus one.</summary>
            public long InstanceUploadBytes;
            /// <summary>The retained fixed-chunk GPU data bytes, or minus one.</summary>
            public long InstanceGpuBytes;
            /// <summary>The scene's prepared instance capacity, or minus one.</summary>
            public int PreparedInstances;
            /// <summary>The polygons sent to safe Mesh fallback this frame, or minus one.</summary>
            public int FallbackInstances;
            /// <summary>The cumulative real GPU buffer creations, or minus one.</summary>
            public long GpuBufferCreations;
            /// <summary>The actual cumulative structured-buffer creations, or minus one.</summary>
            public long InstanceBufferCreations;
            /// <summary>The cumulative Mesh vertex/index buffer reconfigurations, or minus one.</summary>
            public long MeshBufferReconfigurations;
            /// <summary>The cumulative actual Mesh creations, or minus one.</summary>
            public long MeshCreations;
            /// <summary>The cumulative Mesh vertex and index upload bytes, or minus one.</summary>
            public long MeshUploadBytes;
            /// <summary>The cumulative eight-texture bank boundaries, or minus one.</summary>
            public long TextureBankSplits;
            /// <summary>The cumulative hardware blend boundaries, or minus one.</summary>
            public long BlendSplits;
            /// <summary>The cumulative pool/painter barriers, or minus one.</summary>
            public long PainterOrderSplits;
            /// <summary>The cumulative fixed-chunk boundaries, or minus one.</summary>
            public long CapacitySplits;
            /// <summary>The cumulative immutable native binding snapshot creations, or minus one.</summary>
            public long SubmissionBindingCreations;
            /// <summary>The cumulative same-frame buffer protection fallbacks, or minus one.</summary>
            public long SameFrameBufferFallbacks;
            /// <summary>The cumulative Editor actor-label formatting misses, or minus one.</summary>
            public int HierarchyLabelMisses;
            /// <summary>The cumulative Editor actor-root creations, or minus one.</summary>
            public int HierarchyActorCreations;
            /// <summary>The cumulative Editor sprite-component creations, or minus one.</summary>
            public int HierarchySpriteCreations;
            /// <summary>The render counter recorded at the following frame boundary.</summary>
            public long DrawCalls;
            /// <summary>The submitted scene sprite batches, excluding native Canvas/water and engine draws.</summary>
            public int SubmittedSceneSpriteDrawCalls;
            /// <summary>The entire recorded frame's allocation bytes, including framework activity.</summary>
            public long WholeFrameGcBytes;
            /// <summary>The frame-timing GPU observation, zero when unavailable.</summary>
            public double GpuMs;
            /// <summary>The live plant population.</summary>
            public int Plants;
            /// <summary>The live zombie population.</summary>
            public int Zombies;
            /// <summary>The live projectile population.</summary>
            public int Peas;
            /// <summary>The authoritative simulation time.</summary>
            public float Elapsed;
            /// <summary>The exact battle random stream position.</summary>
            public ulong RandomDraws;
        }

        /// <summary>Stores cumulative lifetime-pool observations without retaining model references.</summary>
        [Serializable]
        private struct PoolSnapshot
        {
            /// <summary>The cumulative number of constructed model records, or minus one.</summary>
            public int Created;
            /// <summary>The records currently protected by authoritative references, or minus one.</summary>
            public int Active;
            /// <summary>The records returned after complete reference retirement, or minus one.</summary>
            public long Recycled;
        }
    }
}
