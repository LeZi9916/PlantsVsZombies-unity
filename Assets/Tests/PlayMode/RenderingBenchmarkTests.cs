#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Captures repeatable visible-board rendering measurements using read-only original artwork.</summary>
    public sealed class RenderingBenchmarkTests
    {
        /// <summary>Measures deterministic board workloads with optional fixed-step simulation advancement.</summary>
        /// <returns>Frame boundaries required to measure actual camera rendering.</returns>
        [UnityTest]
        public IEnumerator RecordsVisibleBoardBaseline()
        {
            var output = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_OUTPUT");
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(root))
            {
                Assert.Ignore("Set PVZ_RENDER_BENCHMARK_OUTPUT and PVZ_BOARD_STRESS_ASSETS to enable the rendering benchmark.");
            }

            if (Environment.GetEnvironmentVariable("PVZ_RENDER_USE_BAKED") == "1")
            {
                root = Application.streamingAssetsPath;
            }
            var plantKinds = ReadEnvironmentKinds("PVZ_RENDER_BENCHMARK_PLANTS", new[] { PlantKind.Sunflower, PlantKind.Peashooter, PlantKind.Peashooter });
            var zombieKinds = ReadEnvironmentKinds("PVZ_RENDER_BENCHMARK_ZOMBIES", new[] { ZombieKind.Normal });
            var execution = Environment.GetEnvironmentVariable("PVZ_RENDER_GEOMETRY_MODE");
            var executionProperty = typeof(ObjectSpriteRenderer).GetProperty("GeometryExecution", BindingFlags.Static | BindingFlags.NonPublic);
            var previousExecution = executionProperty?.GetValue(null);
            if (!string.IsNullOrEmpty(execution) && executionProperty != null)
            {
                executionProperty.SetValue(null, Enum.Parse(executionProperty.PropertyType, execution, true));
            }
            var plantCount = ReadEnvironmentCount("PVZ_RENDER_BENCHMARK_PLANT_COUNT", 30, 45);
            var zombieCount = ReadEnvironmentCount("PVZ_RENDER_BENCHMARK_ZOMBIE_COUNT", 50, 250);
            var advancing = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_ADVANCE") == "1";
            yield return null;
            Directory.CreateDirectory(output!);
            var host = new GameObject("Inactive rendering benchmark");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root!);
            var options = new OptionsView(root!);
            var presenter = new GamePresenter(owner);
            var camera = ObjectSpriteRenderer.SceneCamera;
            var oldTarget = camera.targetTexture;
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            target.Create();
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            SpriteRenderingProfiler.Initialize();
            SpriteRenderingProfiler.ForceEnabled = Environment.GetEnvironmentVariable("PVZ_RENDER_PROFILE") == "1";
            var profileNames = new[]
            {
                "PvZ.Atlas.Regions", "PvZ.Sprite.Submit", "PvZ.Sprite.Upload", "PvZ.Hierarchy.Refresh", "PvZ.Animation.Sample",
                "PvZ.Sprite.Prepare", "PvZ.Sprite.Geometry", "PvZ.Sprite.Commit", "PvZ.Present", "PvZ.Update", "PvZ.Update.Input", "PvZ.Update.Simulation", "PvZ.Update.Audio"
            };
            var profiles = new ProfilerRecorder[profileNames.Length];
            if (Environment.GetEnvironmentVariable("PVZ_RENDER_PROFILE") == "1")
            {
                for (var index = 0; index < profiles.Length; index++)
                {
                    profiles[index] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, profileNames[index]);
                }
            }
            var timings = new FrameTiming[1];
            using var simulationAllocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            using var presentationAllocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(simulationAllocations.Valid && presentationAllocations.Valid, Is.True,
                "The scoped native GC.Alloc recorder must be available for allocation evidence.");
            try
            {
                owner._root = root!;
                owner._animations = animations;
                owner._optionsView = options;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._smokeMode = true;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._game.ResetAdventureWaveSchedule(1000000, false, false);
                owner._deck.Clear();
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                for (var index = 0; index < plantCount; index++)
                {
                    owner._game.Plants.Add(new Plant
                    {
                        Id = index + 1,
                        Kind = plantKinds[index % plantKinds.Length],
                        Layer = PlantLayer.Main,
                        Row = index % 5,
                        Column = index / 5,
                        Health = 4000f,
                        Age = .5f,
                        PresentationRate = 12f,
                        PresentationAwakeAge = .5f
                    });
                }

                for (var index = 0; index < zombieCount; index++)
                {
                    var kind = zombieKinds[index % zombieKinds.Length];
                    var armor = kind == ZombieKind.Buckethead ? 1100f : kind == ZombieKind.Conehead ? 370f : 0f;
                    owner._game.Zombies.Add(new Zombie
                    {
                        Id = 1000 + index,
                        Kind = kind,
                        Row = index % 5,
                        X = 6.4f + index / 5 * .2f,
                        Health = 270f,
                        StartingHealth = 270f,
                        ArmorHealth = armor,
                        PresentationMaxArmor = armor,
                        PresentationAnimation = "anim_walk",
                        PresentationAnimationAge = .3f
                    });
                }

                camera.targetTexture = target;
                var stateCount = 3;
                if (int.TryParse(Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_STATES"), out var requestedStates) && requestedStates >= 1 && requestedStates <= 3)
                {
                    stateCount = requestedStates;
                }
                for (var state = 0; state < stateCount; state++)
                {
                    foreach (var zombie in owner._game.Zombies)
                    {
                        zombie.PresentationFlash = state == 1 ? .05f : 0f;
                        zombie.ChillRemaining = state == 2 ? 1f : 0f;
                    }

                    for (var warm = 0; warm < 120; warm++)
                    {
                        if (advancing)
                        {
                            owner._game.Update(.01f);
                        }
                        presenter.Present();
                        yield return null;
                    }

                    var result = new Measurement { State = state, Width = target.width, Height = target.height,
                        PlantKinds = string.Join(",", plantKinds), ZombieKinds = string.Join(",", zombieKinds),
                        PlantCount = plantCount, ZombieCount = zombieCount, AdvancesSimulation = advancing,
                        ProfileNames = profileNames, UnityVersion = Application.unityVersion,
                        OperatingSystem = SystemInfo.operatingSystem, Processor = SystemInfo.processorType,
                        ScreenWidth = Screen.width, ScreenHeight = Screen.height, GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                        GraphicsDevice = SystemInfo.graphicsDeviceName, GeometryExecution = execution ?? "Default", UsesBakedAssets = Environment.GetEnvironmentVariable("PVZ_RENDER_USE_BAKED") == "1" };
                    var totalSamples = new double[900];
                    var cpuSamples = new double[900];
                    var eventReferencePath = Environment.GetEnvironmentVariable("PVZ_RENDER_BENCHMARK_EVENT_REFERENCE");
                    var reference = string.IsNullOrEmpty(eventReferencePath) ? null
                        : JsonUtility.FromJson<Measurement>(File.ReadAllText(eventReferencePath));
                    var referenceFlags = reference?.AllocationEventFrameFlags;
                    if (referenceFlags != null)
                    {
                        Assert.That(referenceFlags.Length, Is.EqualTo(900));
                        Assert.That(reference!.PlantCount, Is.EqualTo(plantCount));
                        Assert.That(reference.ZombieCount, Is.EqualTo(zombieCount));
                        Assert.That(reference.AdvancesSimulation, Is.EqualTo(advancing));
                        result.EventFrameClassification = "Fixed frame indices from paired optimized workload";
                    }
                    result.AllocationEventFrameFlags = new bool[900];
                    result.SimulationAllocationEventSamples = new int[900];
                    result.PresentationAllocationEventSamples = new int[900];
                    var eventCpuSamples = new double[900];
                    var steadyCpuSamples = new double[900];
                    var eventTotalSamples = new double[900];
                    var steadyTotalSamples = new double[900];
                    var gpuSamples = new double[900];
                    var profileTotals = new long[profiles.Length];
                    for (var round = 0; round < 3; round++)
                    {
                        for (var frame = 0; frame < 300; frame++)
                        {
                            simulationAllocations.Reset();
                            simulationAllocations.Start();
                            var updateStart = Stopwatch.GetTimestamp();
                            if (advancing)
                            {
                                owner._game.Update(.01f);
                            }
                            var updateEnd = Stopwatch.GetTimestamp();
                            simulationAllocations.Stop();
                            presentationAllocations.Reset();
                            presentationAllocations.Start();
                            var start = Stopwatch.GetTimestamp();
                            presenter.Present();
                            var end = Stopwatch.GetTimestamp();
                            presentationAllocations.Stop();
                            var allocations = simulationAllocations.Count + presentationAllocations.Count;
                            result.SimulationAllocationEvents += simulationAllocations.Count;
                            result.PresentationAllocationEvents += presentationAllocations.Count;
                            result.PeakFrameAllocationEvents = Math.Max(result.PeakFrameAllocationEvents, allocations);
                            result.AllocatingFrames += allocations > 0 ? 1 : 0;
                            result.AllocationEventFrameFlags[result.Frames] = allocations > 0;
                            result.SimulationAllocationEventSamples[result.Frames] = simulationAllocations.Count;
                            result.PresentationAllocationEventSamples[result.Frames] = presentationAllocations.Count;
                            cpuSamples[result.Frames] = (end - start) * 1000.0 / Stopwatch.Frequency;
                            totalSamples[result.Frames] = cpuSamples[result.Frames] + (updateEnd - updateStart) * 1000.0 / Stopwatch.Frequency;
                            result.PresentationMilliseconds += cpuSamples[result.Frames];
                            result.UpdateAndPresentationMilliseconds += totalSamples[result.Frames];
                            var eventFrame = referenceFlags == null ? allocations > 0 : referenceFlags[result.Frames];
                            if (eventFrame)
                            {
                                eventCpuSamples[result.EventFrames] = cpuSamples[result.Frames];
                                eventTotalSamples[result.EventFrames++] = totalSamples[result.Frames];
                                result.EventFrameAllocationEvents += allocations;
                            }
                            else
                            {
                                steadyCpuSamples[result.SteadyFrames] = cpuSamples[result.Frames];
                                steadyTotalSamples[result.SteadyFrames++] = totalSamples[result.Frames];
                                result.SteadyFrameAllocationEvents += allocations;
                            }
                            FrameTimingManager.CaptureFrameTimings();
                            yield return null;
                            result.DrawCalls += draws.Valid ? draws.LastValue : 0;
                            result.SetPassCalls += passes.Valid ? passes.LastValue : 0;
                            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0d)
                            {
                                gpuSamples[result.GpuTimingSamples++] = timings[0].gpuFrameTime;
                                result.GpuMilliseconds += timings[0].gpuFrameTime;
                            }
                            for (var index = 0; index < profiles.Length; index++)
                            {
                                if (profiles[index].Valid)
                                {
                                    profileTotals[index] += profiles[index].LastValue;
                                }
                            }
                            result.Frames++;
                        }
                    }

                    result.PresentationMilliseconds /= result.Frames;
                    result.UpdateAndPresentationMilliseconds /= result.Frames;
                    result.FinalPlantCount = owner._game.Plants.Count;
                    result.FinalZombieCount = owner._game.Zombies.Count;
                    result.FinalSimulationSeconds = owner._game.Elapsed;
                    result.FinalMatchState = owner._game.State.ToString();
                    result.PresentationSamples = (double[])cpuSamples.Clone();
                    result.UpdateAndPresentationSamples = (double[])totalSamples.Clone();
                    SummarizeSamples(steadyCpuSamples, result.SteadyFrames, out result.SteadyPresentationMilliseconds,
                        out result.SteadyPresentationP95Milliseconds);
                    SummarizeSamples(eventCpuSamples, result.EventFrames, out result.EventPresentationMilliseconds,
                        out result.EventPresentationP95Milliseconds);
                    SummarizeSamples(steadyTotalSamples, result.SteadyFrames, out result.SteadyUpdateAndPresentationMilliseconds,
                        out result.SteadyUpdateAndPresentationP95Milliseconds);
                    SummarizeSamples(eventTotalSamples, result.EventFrames, out result.EventUpdateAndPresentationMilliseconds,
                        out result.EventUpdateAndPresentationP95Milliseconds);
                    if (reference != null)
                    {
                        Assert.That(result.FinalPlantCount, Is.EqualTo(reference.FinalPlantCount));
                        Assert.That(result.FinalZombieCount, Is.EqualTo(reference.FinalZombieCount));
                        Assert.That(result.FinalSimulationSeconds, Is.EqualTo(reference.FinalSimulationSeconds));
                    }
                    result.GpuTimingAvailable = result.GpuTimingSamples >= result.Frames / 2;
                    result.GpuMilliseconds = result.GpuTimingSamples > 0 ? result.GpuMilliseconds / result.GpuTimingSamples : 0d;
                    result.DrawCalls /= result.Frames;
                    result.SetPassCalls /= result.Frames;
                    Array.Sort(cpuSamples);
                    Array.Sort(totalSamples);
                    Array.Sort(gpuSamples, 0, result.GpuTimingSamples);
                    result.PresentationP95Milliseconds = cpuSamples[854];
                    result.UpdateAndPresentationP95Milliseconds = totalSamples[854];
                    result.GpuP95Milliseconds = result.GpuTimingSamples > 0 ? gpuSamples[(int)((result.GpuTimingSamples - 1) * .95)] : 0d;
                    result.ProfileMilliseconds = new double[profiles.Length];
                    for (var index = 0; index < profiles.Length; index++)
                    {
                        result.ProfileMilliseconds[index] = profileTotals[index] / (double)result.Frames / 1000000d;
                    }
                    result.TextureBytes = Texture.currentTextureMemory;
                    result.CaptureSurfaces = animations.NativeSurfaceCount;
                    ReadBatchStatistics(animations, result);
                    SaveTextureMemory(output!, state, animations, result);
                    using (var capture = new RenderingCoverageCapture())
                    {
                        using (capture.Begin())
                        {
                            presenter.Present();
                        }
                        result.CaptureRequests = capture.CaptureCount;
                        result.CaptureCoveredPixelSamples = capture.CoveredPixelSamples;
                        result.CaptureTargetPixels = capture.TargetPixelCount;
                        result.CaptureMeanGeometryCoverage = capture.MeanGeometryCoverage;
                        capture.SaveHeatmaps(output!, "capture-" + state);
                    }
                    SaveCoverage(camera, target.width, target.height, output!, state, result);
                    File.WriteAllText(Path.Combine(output!, "state-" + state + ".json"), JsonUtility.ToJson(result, true));
                    var oldActive = RenderTexture.active;
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                    pixels.Apply();
                    RenderTexture.active = oldActive;
                    File.WriteAllBytes(Path.Combine(output!, "state-" + state + ".png"), pixels.EncodeToPNG());
                    UnityEngine.Debug.Log("Rendering benchmark: " + JsonUtility.ToJson(result));
                }
            }
            finally
            {
                if (executionProperty != null)
                {
                    executionProperty.SetValue(null, previousExecution);
                }
                SpriteRenderingProfiler.ForceEnabled = false;
                foreach (var profile in profiles)
                {
                    profile.Dispose();
                }
                camera.targetTexture = oldTarget;
                presenter.Dispose();
                animations.Dispose();
                options.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Summarizes a populated CPU sample prefix without including unused buffer capacity.</summary>
        /// <param name="samples">The reusable event or steady-state sample buffer, sorted in place.</param>
        /// <param name="count">The number of valid observations.</param>
        /// <param name="average">Receives the arithmetic mean, or zero for an empty class.</param>
        /// <param name="percentile95">Receives the 95th percentile, or zero for an empty class.</param>
        private static void SummarizeSamples(double[] samples, int count, out double average, out double percentile95)
        {
            average = 0d;
            percentile95 = 0d;
            if (count == 0)
            {
                return;
            }
            for (var index = 0; index < count; index++)
            {
                average += samples[index];
            }
            average /= count;
            Array.Sort(samples, 0, count);
            percentile95 = samples[(int)Math.Floor((count - 1) * .95d)];
        }

        /// <summary>Reads an optional deterministic workload size while keeping the historical defaults.</summary>
        /// <param name="variable">The optional integer workload setting.</param>
        /// <param name="fallback">The default entity count.</param>
        /// <param name="maximum">The largest supported fixture workload.</param>
        /// <returns>The selected nonnegative entity count.</returns>
        /// <exception cref="InvalidOperationException">The setting is not an integer within the supported range.</exception>
        private static int ReadEnvironmentCount(string variable, int fallback, int maximum)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }
            if (!int.TryParse(value, out var result) || result < 0 || result > maximum)
            {
                throw new InvalidOperationException(variable + " must be between 0 and " + maximum + ".");
            }
            return result;
        }

        /// <summary>Reads an optional deterministic benchmark species list without changing the default board.</summary>
        /// <typeparam name="T">The plant or zombie enumeration used by the fixture.</typeparam>
        /// <param name="variable">The optional environment variable containing comma-separated enum names.</param>
        /// <param name="defaults">The existing species cycle retained when the variable is absent.</param>
        /// <returns>The explicitly selected species cycle or the original default cycle.</returns>
        /// <exception cref="InvalidOperationException">A requested species name is empty, unknown, or undefined.</exception>
        private static T[] ReadEnvironmentKinds<T>(string variable, T[] defaults) where T : struct, Enum
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaults;
            }

            var names = value.Split(',');
            var selected = new T[names.Length];
            for (var index = 0; index < names.Length; index++)
            {
                var name = names[index].Trim();
                if (name.Length == 0 || !Enum.TryParse<T>(name, true, out var kind) || !Enum.IsDefined(typeof(T), kind))
                {
                    throw new InvalidOperationException(variable + " contains an unknown species: " + name);
                }

                selected[index] = kind;
            }

            return selected;
        }

        /// <summary>Reads optional batch diagnostics while remaining usable against the pre-optimization snapshot.</summary>
        /// <param name="animations">The animation renderer owning the measured scene command pool.</param>
        /// <param name="measurement">Receives the source and actual mesh workload.</param>
        private static void ReadBatchStatistics(ReanimationRenderer animations, Measurement measurement)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var renderer = typeof(ReanimationRenderer).GetField("_objectRenderer", flags)?.GetValue(animations);
            var assets = typeof(ReanimationRenderer).GetField("_assets", flags)?.GetValue(animations);
            if (assets != null)
            {
                measurement.LoadedAtlasPages = Convert.ToInt32(assets.GetType().GetProperty("LoadedPageCount", flags)!.GetValue(assets));
                measurement.AtlasPageLimit = Convert.ToInt32(assets.GetType().GetProperty("MaxPageSize", flags)!.GetValue(assets));
            }
            if (renderer != null)
            {
                measurement.NativeRegionCapacity = ReadOptionalInt(renderer, "NativeRegionCapacity", flags);
                measurement.NativeVertexCapacity = ReadOptionalInt(renderer, "NativeVertexCapacity", flags);
            }
            var statistics = renderer?.GetType().GetProperty("LastFrameStatistics", flags)?.GetValue(renderer);
            if (statistics == null)
            {
                return;
            }
            measurement.SubmittedSprites = Convert.ToInt32(statistics.GetType().GetProperty("SubmittedSpriteCount", flags)!.GetValue(statistics));
            measurement.SpriteBatches = Convert.ToInt32(statistics.GetType().GetProperty("BatchCount", flags)!.GetValue(statistics));
            measurement.UploadedVertices = Convert.ToInt32(statistics.GetType().GetProperty("VertexCount", flags)!.GetValue(statistics));
            measurement.CulledSprites = Convert.ToInt32(statistics.GetType().GetProperty("CulledSpriteCount", flags)!.GetValue(statistics));
            measurement.CoveredPixelArea = Convert.ToDouble(statistics.GetType().GetProperty("CoveredPixelArea", flags)!.GetValue(statistics));
            measurement.MaterialSplits = Convert.ToInt32(statistics.GetType().GetProperty("MaterialSplitCount", flags)!.GetValue(statistics));
            measurement.OrderSplits = Convert.ToInt32(statistics.GetType().GetProperty("PainterOrderSplitCount", flags)!.GetValue(statistics));
            measurement.CapacitySplits = Convert.ToInt32(statistics.GetType().GetProperty("CapacitySplitCount", flags)!.GetValue(statistics));
        }

        /// <summary>Reads a capacity counter when the measured implementation exposes it.</summary>
        /// <param name="owner">The renderer containing optional native arena diagnostics.</param>
        /// <param name="name">The capacity property name.</param>
        /// <param name="flags">The member visibility required by the fixture.</param>
        /// <returns>The retained capacity, or zero for the pre-batching implementation.</returns>
        private static int ReadOptionalInt(object owner, string name, BindingFlags flags)
        {
            var value = owner.GetType().GetProperty(name, flags)?.GetValue(owner);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        /// <summary>Records resident texture objects before allocating diagnostic heatmaps, keeping each memory counter's meaning explicit.</summary>
        /// <param name="output">The ignored benchmark output directory.</param>
        /// <param name="state">The fixed feedback state being measured.</param>
        /// <param name="animations">The provider owner whose actual resident atlas page references are inspected.</param>
        /// <param name="measurement">Receives separate logical texel and native-object totals without replacing the engine texture counter.</param>
        private static void SaveTextureMemory(string output, int state, ReanimationRenderer animations, Measurement measurement)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var provider = typeof(ReanimationRenderer).GetField("_assets", flags)?.GetValue(animations);
            var pages = provider?.GetType().GetField("_pages", flags)?.GetValue(provider) as IDictionary;
            var atlas = new System.Collections.Generic.HashSet<Texture2D>();
            var atlasPages = new System.Collections.Generic.List<(int Index, Texture2D Texture)>();
            if (pages != null)
            {
                foreach (DictionaryEntry entry in pages)
                {
                    if (entry.Value is Texture2D texture && texture != null)
                    {
                        atlasPages.Add((Convert.ToInt32(entry.Key), texture));
                        if (atlas.Add(texture))
                        {
                            measurement.AtlasLogicalRGBABytes += LogicalRGBABytes(texture);
                            measurement.AtlasNativeObjectBytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
                            if (texture.isReadable)
                            {
                                measurement.AtlasReadablePageCount++;
                            }
                        }
                    }
                }
            }

            const string columns = "name,width,height,loadedNativeBytes,format,graphicsFormat,isReadable,mipCount,logicalRGBABytes,isAtlasPage";
            atlasPages.Sort((left, right) => left.Index.CompareTo(right.Index));
            var pageReport = new System.Text.StringBuilder();
            pageReport.Append("pageIndex,").AppendLine(columns);
            foreach (var page in atlasPages)
            {
                pageReport.Append(page.Index).Append(',');
                AppendTextureMemory(pageReport, page.Texture, true);
            }

            File.WriteAllText(Path.Combine(output, "atlas-pages-" + state + ".csv"), pageReport.ToString());
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            Array.Sort(textures, (left, right) => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(right)
                .CompareTo(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(left)));
            var report = new System.Text.StringBuilder();
            report.AppendLine(columns);
            for (var index = 0; index < textures.Length; index++)
            {
                measurement.AllTexture2DNativeObjectBytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(textures[index]);
                if (index < 30 || atlas.Contains(textures[index]))
                {
                    AppendTextureMemory(report, textures[index], atlas.Contains(textures[index]));
                }
            }

            File.WriteAllText(Path.Combine(output, "textures-" + state + ".csv"), report.ToString());
        }

        /// <summary>Writes one already-resident texture's format and distinct memory observations.</summary>
        /// <param name="report">The CSV buffer receiving this texture.</param>
        /// <param name="texture">The resident texture; this method never loads a resource.</param>
        /// <param name="atlasPage">Whether the provider currently retains this exact texture as a page.</param>
        private static void AppendTextureMemory(System.Text.StringBuilder report, Texture2D texture, bool atlasPage)
        {
            report.Append(texture.name.Replace(",", "_")).Append(',').Append(texture.width).Append(',').Append(texture.height)
                .Append(',').Append(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture))
                .Append(',').Append(texture.format).Append(',').Append(texture.graphicsFormat)
                .Append(',').Append(texture.isReadable).Append(',').Append(texture.mipmapCount)
                .Append(',').Append(LogicalRGBABytes(texture)).Append(',').Append(atlasPage).AppendLine();
        }

        /// <summary>Counts logical 32-bit RGBA texels across all mip levels, independently of actual GPU format and native-object storage.</summary>
        /// <param name="texture">The resident texture whose logical dimensions are counted.</param>
        /// <returns>The logical RGBA byte count; this is not a driver-allocation or process-memory measurement.</returns>
        private static ulong LogicalRGBABytes(Texture2D texture)
        {
            ulong bytes = 0;
            var width = texture.width;
            var height = texture.height;
            var mipCount = texture.mipmapCount;
            for (var mip = 0; mip < mipCount; mip++)
            {
                bytes += (ulong)Math.Max(1, width >> mip) * (ulong)Math.Max(1, height >> mip) * 4UL;
            }

            return bytes;
        }

        /// <summary>Exports a CPU-rasterized coverage heatmap of the actual submitted scene and Canvas meshes.</summary>
        /// <param name="camera">The camera defining the measured viewport.</param>
        /// <param name="width">The target width in pixels.</param>
        /// <param name="height">The target height in pixels.</param>
        /// <param name="output">The ignored diagnostic output directory.</param>
        /// <param name="state">The fixed feedback state being captured.</param>
        /// <param name="measurement">Receives geometric coverage statistics, excluding retired offscreen surfaces.</param>
        private static void SaveCoverage(Camera camera, int width, int height, string output, int state, Measurement measurement)
        {
            var coverage = new int[width * height];
            foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || !filter.gameObject.activeInHierarchy || !filter.gameObject.scene.IsValid() || (camera.cullingMask & 1 << filter.gameObject.layer) == 0 || filter.sharedMesh == null)
                {
                    continue;
                }
                AddCoverage(camera, filter.transform, filter.sharedMesh, width, height, coverage);
            }
            foreach (var canvas in Resources.FindObjectsOfTypeAll<CanvasRenderer>())
            {
                if (canvas.cull || !canvas.gameObject.activeInHierarchy || !canvas.gameObject.scene.IsValid() || (camera.cullingMask & 1 << canvas.gameObject.layer) == 0)
                {
                    continue;
                }
                var canvasMesh = canvas.GetMesh();
                if (canvasMesh != null)
                {
                    AddCoverage(camera, canvas.transform, canvasMesh, width, height, coverage);
                }
            }
            var colors = new Color32[coverage.Length];
            long total = 0;
            var peak = 0;
            for (var index = 0; index < coverage.Length; index++)
            {
                total += coverage[index];
                peak = Math.Max(peak, coverage[index]);
                var intensity = Mathf.Clamp01(coverage[index] / 24f);
                colors[index] = coverage[index] == 0 ? new Color32(0, 0, 0, 255)
                    : (Color32)Color.HSVToRGB((1f - intensity) * .66f, 1f, 1f);
            }
            measurement.MeanGeometryCoverage = total / (double)coverage.Length;
            measurement.PeakGeometryCoverage = peak;
            var heatmap = new Texture2D(width, height, TextureFormat.RGBA32, false);
            heatmap.SetPixels32(colors);
            heatmap.Apply();
            File.WriteAllBytes(Path.Combine(output, "coverage-" + state + ".png"), heatmap.EncodeToPNG());
            UnityEngine.Object.Destroy(heatmap);
        }

        /// <summary>Counts sample-center triangle coverage in the measured camera viewport.</summary>
        /// <param name="camera">The projection camera.</param>
        /// <param name="transform">The owner of the mesh's local coordinates.</param>
        /// <param name="mesh">The actual geometry sent to the GPU.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="coverage">The accumulated pixel coverage counts.</param>
        private static void AddCoverage(Camera camera, Transform transform, Mesh mesh, int width, int height, int[] coverage)
        {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            for (var index = 0; index < vertices.Length; index++)
            {
                var point = camera.WorldToViewportPoint(transform.TransformPoint(vertices[index]));
                vertices[index] = new Vector3(point.x * width, point.y * height, point.z);
            }
            for (var index = 0; index + 2 < indices.Length; index += 3)
            {
                var a = vertices[indices[index]];
                var b = vertices[indices[index + 1]];
                var c = vertices[indices[index + 2]];
                if (a.z < camera.nearClipPlane || a.z > camera.farClipPlane)
                {
                    continue;
                }
                var area = Edge(a, b, c.x, c.y);
                if (Math.Abs(area) < .000001f)
                {
                    continue;
                }
                if (area < 0f)
                {
                    var swap = b;
                    b = c;
                    c = swap;
                }
                var left = Math.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
                var right = Math.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
                var bottom = Math.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
                var top = Math.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));
                for (var y = bottom; y <= top; y++)
                {
                    for (var x = left; x <= right; x++)
                    {
                        if (InsideEdge(a, b, x + .5f, y + .5f) && InsideEdge(b, c, x + .5f, y + .5f) && InsideEdge(c, a, x + .5f, y + .5f))
                        {
                            coverage[y * width + x]++;
                        }
                    }
                }
            }
        }

        /// <summary>Evaluates one oriented triangle edge.</summary>
        /// <param name="a">The first edge vertex.</param>
        /// <param name="b">The second edge vertex.</param>
        /// <param name="x">The horizontal sample coordinate.</param>
        /// <param name="y">The vertical sample coordinate.</param>
        /// <returns>The signed edge distance scaled by edge length.</returns>
        private static float Edge(Vector3 a, Vector3 b, float x, float y)
        {
            return (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x);
        }

        /// <summary>Applies a half-open edge rule so adjacent triangles do not count their shared edge twice.</summary>
        /// <param name="a">The first edge vertex.</param>
        /// <param name="b">The second edge vertex.</param>
        /// <param name="x">The horizontal sample coordinate.</param>
        /// <param name="y">The vertical sample coordinate.</param>
        /// <returns>Whether this sample belongs to the triangle side of the edge.</returns>
        private static bool InsideEdge(Vector3 a, Vector3 b, float x, float y)
        {
            var edge = Edge(a, b, x, y);
            return edge > 0f || edge == 0f && (b.y > a.y || b.y == a.y && b.x < a.x);
        }

        /// <summary>Stores averaged per-frame counters for one fixed board state.</summary>
        [Serializable]
        private sealed class Measurement
        {
            /// <summary>The fixed feedback state: neutral, hit, or chilled.</summary>
            public int State;
            /// <summary>The deterministic plant species cycle used by this fixed board.</summary>
            public string PlantKinds = "";
            /// <summary>The deterministic zombie species cycle used by this fixed board.</summary>
            public string ZombieKinds = "";
            /// <summary>The initial plant workload.</summary>
            public int PlantCount;
            /// <summary>The initial zombie workload.</summary>
            public int ZombieCount;
            /// <summary>Whether each sample includes one deterministic 100 Hz simulation step.</summary>
            public bool AdvancesSimulation;
            /// <summary>The plant count remaining after measurement.</summary>
            public int FinalPlantCount;
            /// <summary>The zombie count remaining after measurement.</summary>
            public int FinalZombieCount;
            /// <summary>The final simulation clock, allowing equivalent runs to be checked.</summary>
            public float FinalSimulationSeconds;
            /// <summary>The terminal match state for the deterministic workload.</summary>
            public string FinalMatchState = "";
            /// <summary>The exact Editor version used by this run.</summary>
            public string UnityVersion = "";
            /// <summary>The measured host operating system.</summary>
            public string OperatingSystem = "";
            /// <summary>The host processor reported by Unity.</summary>
            public string Processor = "";
            /// <summary>The logical screen width used by the presenter viewport transform.</summary>
            public int ScreenWidth;
            /// <summary>The logical screen height used by the presenter viewport transform.</summary>
            public int ScreenHeight;
            /// <summary>The active graphics backend.</summary>
            public string GraphicsApi = "";
            /// <summary>The active graphics adapter.</summary>
            public string GraphicsDevice = "";
            /// <summary>The requested immediate, managed batch, or Burst batch geometry execution.</summary>
            public string GeometryExecution = "";
            /// <summary>Whether the real production streaming asset path and generated atlas were selected.</summary>
            public bool UsesBakedAssets;
            /// <summary>The optional marker names corresponding to the measured marker averages.</summary>
            public string[] ProfileNames = Array.Empty<string>();
            /// <summary>The current-thread GC.Alloc events in all measured simulation steps.</summary>
            public long SimulationAllocationEvents;
            /// <summary>The current-thread GC.Alloc events in all measured complete presentation traversals.</summary>
            public long PresentationAllocationEvents;
            /// <summary>The most current-thread GC.Alloc events in one measured traversal.</summary>
            public int PeakFrameAllocationEvents;
            /// <summary>Explains why allocation events are reported instead of unsupported managed-byte counters.</summary>
            public string AllocationCounter = "Unity GC.Alloc events (current thread); zero events proves zero bytes; event byte sizes unavailable";
            /// <summary>The measured traversals that allocated at least one managed byte.</summary>
            public int AllocatingFrames;
            /// <summary>The retained native input region capacity, zero before native batching exists.</summary>
            public int NativeRegionCapacity;
            /// <summary>The retained native output vertex capacity, zero before native batching exists.</summary>
            public int NativeVertexCapacity;
            /// <summary>The average complete simulation and presentation CPU time.</summary>
            public double UpdateAndPresentationMilliseconds;
            /// <summary>The 95th percentile complete simulation and presentation CPU time.</summary>
            public double UpdateAndPresentationP95Milliseconds;
            /// <summary>The fixed premeasurement resource and compilation warmup.</summary>
            public int WarmupFrames = 120;
            /// <summary>The simulation random seed shared by every benchmark execution.</summary>
            public int RandomSeed = 7529;
            /// <summary>The event classification rule; paired baselines may reuse optimized frame indices.</summary>
            public string EventFrameClassification = "Current-thread allocation events in this workload";
            /// <summary>The CPU samples in original deterministic frame order.</summary>
            public double[] PresentationSamples = Array.Empty<double>();
            /// <summary>The fixed-step simulation and presentation CPU samples in original frame order.</summary>
            public double[] UpdateAndPresentationSamples = Array.Empty<double>();
            /// <summary>The frames that allocated during this execution, before optional paired classification.</summary>
            public bool[] AllocationEventFrameFlags = Array.Empty<bool>();
            /// <summary>The scoped simulation allocation counts in original frame order.</summary>
            public int[] SimulationAllocationEventSamples = Array.Empty<int>();
            /// <summary>The scoped presentation allocation counts in original frame order.</summary>
            public int[] PresentationAllocationEventSamples = Array.Empty<int>();
            /// <summary>The event-free or paired steady-state observation count.</summary>
            public int SteadyFrames;
            /// <summary>The resource or event observation count.</summary>
            public int EventFrames;
            /// <summary>The allocation events in frames classified as steady state.</summary>
            public long SteadyFrameAllocationEvents;
            /// <summary>The allocation events in frames classified as event frames.</summary>
            public long EventFrameAllocationEvents;
            /// <summary>The mean presentation CPU time in steady-state frames.</summary>
            public double SteadyPresentationMilliseconds;
            /// <summary>The 95th percentile presentation CPU time in steady-state frames.</summary>
            public double SteadyPresentationP95Milliseconds;
            /// <summary>The mean presentation CPU time in resource or event frames.</summary>
            public double EventPresentationMilliseconds;
            /// <summary>The 95th percentile presentation CPU time in resource or event frames.</summary>
            public double EventPresentationP95Milliseconds;
            /// <summary>The mean simulation and presentation CPU time in steady-state frames.</summary>
            public double SteadyUpdateAndPresentationMilliseconds;
            /// <summary>The 95th percentile simulation and presentation CPU time in steady-state frames.</summary>
            public double SteadyUpdateAndPresentationP95Milliseconds;
            /// <summary>The mean simulation and presentation CPU time in resource or event frames.</summary>
            public double EventUpdateAndPresentationMilliseconds;
            /// <summary>The 95th percentile simulation and presentation CPU time in resource or event frames.</summary>
            public double EventUpdateAndPresentationP95Milliseconds;
            /// <summary>The number of measured frames.</summary>
            public int Frames;
            /// <summary>The target width in pixels.</summary>
            public int Width;
            /// <summary>The target height in pixels.</summary>
            public int Height;
            /// <summary>The average production presentation CPU time.</summary>
            public double PresentationMilliseconds;
            /// <summary>The optional CPU regions, submission, upload, hierarchy, and inclusive animation marker averages.</summary>
            public double[] ProfileMilliseconds = Array.Empty<double>();
            /// <summary>The average GPU frame time, or zero when unavailable.</summary>
            public double GpuMilliseconds;
            /// <summary>The number of nonzero GPU timing observations returned by the engine.</summary>
            public int GpuTimingSamples;
            /// <summary>Whether at least half the measured frames had usable GPU timing observations.</summary>
            public bool GpuTimingAvailable;
            /// <summary>The 95th percentile production presentation CPU time.</summary>
            public double PresentationP95Milliseconds;
            /// <summary>The 95th percentile reported GPU frame time, or zero when unavailable.</summary>
            public double GpuP95Milliseconds;
            /// <summary>The retained actor-local capture surfaces.</summary>
            public int CaptureSurfaces;
            /// <summary>The actual native render requests in an untimed representative presentation.</summary>
            public int CaptureRequests;
            /// <summary>The summed rasterized geometric samples across all offscreen targets.</summary>
            public long CaptureCoveredPixelSamples;
            /// <summary>The total pixels in all offscreen target submissions.</summary>
            public long CaptureTargetPixels;
            /// <summary>The weighted mean coverage per independent native target pixel.</summary>
            public double CaptureMeanGeometryCoverage;
            /// <summary>The visible original sprite commands in the main animation pool.</summary>
            public int SubmittedSprites;
            /// <summary>The number of atlas pages resident for the shared resource provider.</summary>
            public int LoadedAtlasPages;
            /// <summary>The selected device-compatible atlas page limit.</summary>
            public int AtlasPageLimit;
            /// <summary>The actual mesh batches in the main animation pool.</summary>
            public int SpriteBatches;
            /// <summary>The vertices uploaded by that pool.</summary>
            public int UploadedVertices;
            /// <summary>The commands removed before mesh upload.</summary>
            public int CulledSprites;
            /// <summary>The sum of clipped polygon areas, counting overlap.</summary>
            public double CoveredPixelArea;
            /// <summary>The splits caused by changed texture pages or blend state.</summary>
            public int MaterialSplits;
            /// <summary>The splits preserving another ordered renderer.</summary>
            public int OrderSplits;
            /// <summary>The splits preventing 16-bit index overflow.</summary>
            public int CapacitySplits;
            /// <summary>The mean geometric coverage per main-target pixel, before texture alpha.</summary>
            public double MeanGeometryCoverage;
            /// <summary>The maximum geometric coverage at a main-target pixel.</summary>
            public int PeakGeometryCoverage;
            /// <summary>The average engine draw-call counter.</summary>
            public long DrawCalls;
            /// <summary>The average engine SetPass counter.</summary>
            public long SetPassCalls;
            /// <summary>The logical 32-bit RGBA texel bytes across all resident atlas-page mip levels, independent of physical GPU format.</summary>
            public ulong AtlasLogicalRGBABytes;
            /// <summary>The provider's page native-object memory reported by the Unity Profiler, which is not a dedicated VRAM counter.</summary>
            public long AtlasNativeObjectBytes;
            /// <summary>The total native-object memory of all resident Texture2D objects, including Editor and test resources.</summary>
            public long AllTexture2DNativeObjectBytes;
            /// <summary>The resident atlas pages exposing CPU-readable pixel copies.</summary>
            public int AtlasReadablePageCount;
            /// <summary>The total engine texture memory, including the test runner.</summary>
            public ulong TextureBytes;
        }
    }
}
