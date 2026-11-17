#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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
            var advanceTicks = ReadCount("PVZ_RENDER_BENCHMARK_TICKS", 0, 1000000);
            var frames = ReadCount("PVZ_RENDER_BENCHMARK_FRAMES", 900, 18000);
            var advancing = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_ADVANCE") == "1";
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
                owner._smokeMode = true;
                owner._game = simulation;
                owner._animations = animations;
                owner._optionsView = options;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._deck.Clear();
                owner._deck.AddRange(simulation.Adventure.SelectedSeeds);
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
                for (var warm = 0; warm < 120; warm++)
                {
                    presenter.Present();
                    yield return null;
                }
                var startPayload = Hash(simulation.CaptureCheckpoint());
                for (var frame = 0; frame < frames; frame++)
                {
                    simulationAllocations.Reset();
                    simulationAllocations.Start();
                    var updateStart = Stopwatch.GetTimestamp();
                    if (advancing)
                    {
                        Advance(simulation);
                    }
                    var updateEnd = Stopwatch.GetTimestamp();
                    simulationAllocations.Stop();
                    presentationAllocations.Reset();
                    presentationAllocations.Start();
                    var presentStart = Stopwatch.GetTimestamp();
                    presenter.Present();
                    var presentEnd = Stopwatch.GetTimestamp();
                    presentationAllocations.Stop();
                    Assert.That(simulationAllocations.Count, Is.LessThan(simulationAllocations.Capacity));
                    Assert.That(presentationAllocations.Count, Is.LessThan(presentationAllocations.Capacity));
                    samples[frame] = new FrameSample
                    {
                        SimulationMs = (updateEnd - updateStart) * 1000d / Stopwatch.Frequency,
                        PresentationMs = (presentEnd - presentStart) * 1000d / Stopwatch.Frequency,
                        SimulationAllocationEvents = simulationAllocations.Count,
                        PresentationAllocationEvents = presentationAllocations.Count,
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
                if (backendProperty != null)
                {
                    backendProperty.SetValue(null, previousBackend);
                }
                camera.targetTexture = previousTarget;
                presenter.Dispose();
                animations.Dispose();
                options.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(host);
            }
        }

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

        /// <summary>Creates an initially stopped recorder outside measured scopes.</summary>
        /// <returns>A valid current-thread allocation event recorder.</returns>
        private static ProfilerRecorder NewAllocationRecorder()
        {
            var recorder = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 32768, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(recorder.Valid, Is.True);
            return recorder;
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
            /// <summary>The render counter recorded at the following frame boundary.</summary>
            public long DrawCalls;
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
    }
}
