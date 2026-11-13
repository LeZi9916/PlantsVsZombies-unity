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
        /// <summary>Measures three fixed board states without advancing gameplay or animation clocks.</summary>
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
            var profiles = new ProfilerRecorder[5];
            var profileNames = new[] { "PvZ.Atlas.Regions", "PvZ.Sprite.Submit", "PvZ.Sprite.Upload", "PvZ.Hierarchy.Refresh", "PvZ.Animation.Sample" };
            if (Environment.GetEnvironmentVariable("PVZ_RENDER_PROFILE") == "1")
            {
                for (var index = 0; index < profiles.Length; index++)
                {
                    profiles[index] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, profileNames[index]);
                }
            }
            var timings = new FrameTiming[1];
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
                for (var index = 0; index < 30; index++)
                {
                    owner._game.Plants.Add(new Plant
                    {
                        Id = index + 1,
                        Kind = index % 3 == 0 ? PlantKind.Sunflower : PlantKind.Peashooter,
                        Layer = PlantLayer.Main,
                        Row = index / 6,
                        Column = index % 6,
                        Health = 4000f,
                        Age = .5f,
                        PresentationRate = 12f,
                        PresentationAwakeAge = .5f
                    });
                }

                for (var index = 0; index < 50; index++)
                {
                    owner._game.Zombies.Add(new Zombie
                    {
                        Id = 1000 + index,
                        Kind = ZombieKind.Normal,
                        Row = index % 5,
                        X = 6.4f + index / 5 * .2f,
                        Health = 270f,
                        StartingHealth = 270f,
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

                    for (var warm = 0; warm < 60; warm++)
                    {
                        presenter.Present();
                        yield return null;
                    }

                    var result = new Measurement { State = state, Width = target.width, Height = target.height };
                    var cpuSamples = new double[900];
                    var gpuSamples = new double[900];
                    var profileTotals = new long[profiles.Length];
                    for (var round = 0; round < 3; round++)
                    {
                        for (var frame = 0; frame < 300; frame++)
                        {
                            var start = Stopwatch.GetTimestamp();
                            presenter.Present();
                            cpuSamples[result.Frames] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                            result.PresentationMilliseconds += cpuSamples[result.Frames];
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
                    result.GpuTimingAvailable = result.GpuTimingSamples >= result.Frames / 2;
                    result.GpuMilliseconds = result.GpuTimingSamples > 0 ? result.GpuMilliseconds / result.GpuTimingSamples : 0d;
                    result.DrawCalls /= result.Frames;
                    result.SetPassCalls /= result.Frames;
                    Array.Sort(cpuSamples);
                    Array.Sort(gpuSamples, 0, result.GpuTimingSamples);
                    result.PresentationP95Milliseconds = cpuSamples[854];
                    result.GpuP95Milliseconds = result.GpuTimingSamples > 0 ? gpuSamples[(int)((result.GpuTimingSamples - 1) * .95)] : 0d;
                    result.ProfileMilliseconds = new double[profiles.Length];
                    for (var index = 0; index < profiles.Length; index++)
                    {
                        result.ProfileMilliseconds[index] = profileTotals[index] / (double)result.Frames / 1000000d;
                    }
                    result.TextureBytes = Texture.currentTextureMemory;
                    result.CaptureSurfaces = animations.NativeSurfaceCount;
                    ReadBatchStatistics(animations, result);
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
                    SaveTextureMemory(output!, state);
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

        /// <summary>Reads optional batch diagnostics while remaining usable against the pre-optimization snapshot.</summary>
        /// <param name="animations">The animation renderer owning the measured scene command pool.</param>
        /// <param name="measurement">Receives the source and actual mesh workload.</param>
        private static void ReadBatchStatistics(ReanimationRenderer animations, Measurement measurement)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var renderer = typeof(ReanimationRenderer).GetField("_objectRenderer", flags)?.GetValue(animations);
            var assets = typeof(ReanimationRenderer).GetField("_assets", flags)?.GetValue(animations);
            if (assets != null)
            {
                measurement.LoadedAtlasPages = Convert.ToInt32(assets.GetType().GetProperty("LoadedPageCount", flags)!.GetValue(assets));
                measurement.AtlasPageLimit = Convert.ToInt32(assets.GetType().GetProperty("MaxPageSize", flags)!.GetValue(assets));
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

        /// <summary>Lists the largest already-loaded textures without loading additional image assets.</summary>
        /// <param name="output">The ignored benchmark output directory.</param>
        /// <param name="state">The fixed feedback state being measured.</param>
        private static void SaveTextureMemory(string output, int state)
        {
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            Array.Sort(textures, (left, right) => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(right)
                .CompareTo(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(left)));
            var report = new System.Text.StringBuilder();
            report.AppendLine("name,width,height,loadedNativeBytes");
            for (var index = 0; index < Math.Min(30, textures.Length); index++)
            {
                var texture = textures[index];
                report.Append(texture.name.Replace(",", "_")).Append(',').Append(texture.width).Append(',').Append(texture.height)
                    .Append(',').Append(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture)).AppendLine();
            }
            File.WriteAllText(Path.Combine(output, "textures-" + state + ".csv"), report.ToString());
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
            /// <summary>The total engine texture memory, including the test runner.</summary>
            public ulong TextureBytes;
        }
    }
}
