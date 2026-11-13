#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Collects CPU geometric coverage during one untimed presentation without issuing any additional GPU work.</summary>
    internal sealed class RenderingCoverageCapture : IDisposable
    {
        /// <summary>Accumulates coverage for repeated native captures with the same dimensions.</summary>
        internal sealed class TargetCoverage
        {
            /// <summary>Gets the target width in pixels.</summary>
            internal int Width { get; }
            /// <summary>Gets the target height in pixels.</summary>
            internal int Height { get; }
            /// <summary>Gets the summed heatmap across all captures of this size.</summary>
            internal int[] Pixels { get; }
            /// <summary>Gets the scratch coverage for the current capture, reused between observations.</summary>
            internal int[] CurrentPixels { get; }
            /// <summary>Gets or sets the observed render requests of this size.</summary>
            internal int CaptureCount { get; set; }
            /// <summary>Gets or sets the total visible mesh submissions across those render requests.</summary>
            internal int MeshCount { get; set; }
            /// <summary>Gets or sets all covered pixel samples, counting overlapping primitives separately.</summary>
            internal long CoveredPixelSamples { get; set; }
            /// <summary>Gets or sets the maximum geometric coverage within an individual capture.</summary>
            internal int PeakGeometryCoverage { get; set; }
            /// <summary>Gets all destination pixel opportunities, including empty pixels in each capture.</summary>
            internal long TargetPixelCount => (long)Width * Height * CaptureCount;
            /// <summary>Gets coverage averaged across independent capture targets of this size.</summary>
            internal double MeanGeometryCoverage => TargetPixelCount == 0 ? 0d : CoveredPixelSamples / (double)TargetPixelCount;

            /// <summary>Allocates reusable diagnostic buffers for one observed target size.</summary>
            /// <param name="width">The native capture width.</param>
            /// <param name="height">The native capture height.</param>
            internal TargetCoverage(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new int[checked(width * height)];
                CurrentPixels = new int[Pixels.Length];
            }

            /// <summary>Retires the previous snapshot without discarding its allocated heatmap buffers.</summary>
            internal void Reset()
            {
                Array.Clear(Pixels, 0, Pixels.Length);
                CaptureCount = 0;
                MeshCount = 0;
                CoveredPixelSamples = 0;
                PeakGeometryCoverage = 0;
            }
        }

        /// <summary>The reusable coverage buffers indexed by native target dimensions.</summary>
        private readonly Dictionary<(int Width, int Height), TargetCoverage> _cache = new Dictionary<(int, int), TargetCoverage>();
        /// <summary>The target sizes actually observed in the latest snapshot.</summary>
        private readonly List<TargetCoverage> _targets = new List<TargetCoverage>();
        /// <summary>The reused vertex buffer for reading and projecting actual GPU meshes.</summary>
        private readonly List<Vector3> _vertices = new List<Vector3>();
        /// <summary>The reused triangle index buffer.</summary>
        private readonly List<int> _indices = new List<int>();
        /// <summary>The single diagnostic delegate retained across repeated untimed snapshots.</summary>
        private readonly Action<Camera, RenderTexture> _observer;
        /// <summary>The observer restored after this capture scope ends.</summary>
        private Action<Camera, RenderTexture>? _previous;
        /// <summary>Whether this collector currently owns the diagnostic scope.</summary>
        private bool _active;

        /// <summary>Gets the sizes observed by the latest snapshot, in first-observation order.</summary>
        internal IReadOnlyList<TargetCoverage> Targets => _targets;
        /// <summary>Gets the number of observed native render requests.</summary>
        internal int CaptureCount { get; private set; }
        /// <summary>Gets the sum of all observed native destination pixel counts.</summary>
        internal long TargetPixelCount { get; private set; }
        /// <summary>Gets all covered native pixel samples before texture alpha and depth rejection.</summary>
        internal long CoveredPixelSamples { get; private set; }
        /// <summary>Gets the number of geometrically visible native mesh submissions.</summary>
        internal int MeshCount { get; private set; }
        /// <summary>Gets the maximum geometric coverage within one native capture.</summary>
        internal int PeakGeometryCoverage { get; private set; }
        /// <summary>Gets geometric coverage averaged over all independent native target pixels.</summary>
        internal double MeanGeometryCoverage => TargetPixelCount == 0 ? 0d : CoveredPixelSamples / (double)TargetPixelCount;

        /// <summary>Creates a collector whose buffers can be reused between separate benchmark states.</summary>
        internal RenderingCoverageCapture()
        {
            _observer = Observe;
        }

        /// <summary>Begins an untimed diagnostic scope and preserves any preceding observer.</summary>
        /// <returns>This disposable scope; results remain readable after disposal.</returns>
        /// <exception cref="InvalidOperationException">This collector is already observing a presentation.</exception>
        internal RenderingCoverageCapture Begin()
        {
            if (_active)
            {
                throw new InvalidOperationException("A coverage collection scope is already active.");
            }

            foreach (var target in _targets)
            {
                target.Reset();
            }

            _targets.Clear();
            CaptureCount = 0;
            TargetPixelCount = 0;
            CoveredPixelSamples = 0;
            MeshCount = 0;
            PeakGeometryCoverage = 0;
            _previous = ObjectSpriteRenderer.CaptureObserved;
            ObjectSpriteRenderer.CaptureObserved = _observer;
            _active = true;
            return this;
        }

        /// <summary>Ends observation while retaining the completed coverage and reusable buffers.</summary>
        internal void End()
        {
            if (!_active)
            {
                return;
            }

            ObjectSpriteRenderer.CaptureObserved = _previous;
            _previous = null;
            _active = false;
        }

        /// <summary>Restores the observer installed before this diagnostic scope began.</summary>
        public void Dispose()
        {
            End();
        }

        /// <summary>Exports one summed heatmap per target size after the timed performance sample has finished.</summary>
        /// <param name="directory">The ignored diagnostic output directory.</param>
        /// <param name="prefix">The snapshot identifier used in generated filenames.</param>
        internal void SaveHeatmaps(string directory, string prefix)
        {
            Directory.CreateDirectory(directory);
            foreach (var target in _targets)
            {
                var colors = new Color32[target.Pixels.Length];
                for (var index = 0; index < colors.Length; index++)
                {
                    var mean = target.Pixels[index] / (float)target.CaptureCount;
                    var intensity = Mathf.Clamp01(mean / 24f);
                    colors[index] = mean == 0f ? new Color32(0, 0, 0, 255)
                        : (Color32)Color.HSVToRGB((1f - intensity) * .66f, 1f, 1f);
                }

                var heatmap = new Texture2D(target.Width, target.Height, TextureFormat.RGBA32, false);
                try
                {
                    heatmap.SetPixels32(colors);
                    heatmap.Apply();
                    File.WriteAllBytes(Path.Combine(directory, prefix + "-" + target.Width + "x" + target.Height + ".png"), heatmap.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.Destroy(heatmap);
                }
            }
        }

        /// <summary>Reads a submitted camera while its native pool is still enabled at the isolated capture position.</summary>
        /// <param name="camera">The actual native capture camera.</param>
        /// <param name="destination">The actual destination supplying this capture's dimensions.</param>
        private void Observe(Camera camera, RenderTexture destination)
        {
            var key = (destination.width, destination.height);
            if (!_cache.TryGetValue(key, out var target))
            {
                target = new TargetCoverage(key.width, key.height);
                _cache.Add(key, target);
            }

            if (target.CaptureCount == 0)
            {
                _targets.Add(target);
            }

            Array.Clear(target.CurrentPixels, 0, target.CurrentPixels.Length);
            var meshes = 0;
            foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                var owner = filter.gameObject;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || !owner.activeInHierarchy || !owner.scene.IsValid()
                    || !owner.scene.isLoaded || (camera.cullingMask & (1 << owner.layer)) == 0 || filter.sharedMesh == null)
                {
                    continue;
                }

                if (AddCoverage(camera, filter.transform, filter.sharedMesh, target))
                {
                    meshes++;
                }
            }

            long samples = 0;
            var peak = 0;
            for (var index = 0; index < target.CurrentPixels.Length; index++)
            {
                var value = target.CurrentPixels[index];
                target.Pixels[index] += value;
                samples += value;
                peak = Math.Max(peak, value);
            }

            target.CaptureCount++;
            target.MeshCount += meshes;
            target.CoveredPixelSamples += samples;
            target.PeakGeometryCoverage = Math.Max(target.PeakGeometryCoverage, peak);
            CaptureCount++;
            MeshCount += meshes;
            TargetPixelCount += (long)target.Width * target.Height;
            CoveredPixelSamples += samples;
            PeakGeometryCoverage = Math.Max(PeakGeometryCoverage, peak);
            _previous?.Invoke(camera, destination);
        }

        /// <summary>Rasterizes actual mesh triangles with the same half-open edge rule as the main-target benchmark.</summary>
        /// <param name="camera">The camera projecting the submitted native mesh.</param>
        /// <param name="transform">The owner of the local mesh coordinates.</param>
        /// <param name="mesh">The actual mesh submitted by the render request.</param>
        /// <param name="target">The reusable destination buffers.</param>
        /// <returns>True when at least one triangle covers a target pixel.</returns>
        private bool AddCoverage(Camera camera, Transform transform, Mesh mesh, TargetCoverage target)
        {
            mesh.GetVertices(_vertices);
            for (var index = 0; index < _vertices.Count; index++)
            {
                var point = camera.WorldToViewportPoint(transform.TransformPoint(_vertices[index]));
                _vertices[index] = new Vector3(point.x * target.Width, point.y * target.Height, point.z);
            }

            var visible = false;
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
                {
                    continue;
                }

                mesh.GetTriangles(_indices, submesh);
                for (var index = 0; index + 2 < _indices.Count; index += 3)
                {
                    var a = _vertices[_indices[index]];
                    var b = _vertices[_indices[index + 1]];
                    var c = _vertices[_indices[index + 2]];
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

                    var left = Math.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                    var right = Math.Min(target.Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                    var bottom = Math.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                    var top = Math.Min(target.Height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                    for (var y = bottom; y <= top; y++)
                    {
                        for (var x = left; x <= right; x++)
                        {
                            if (InsideEdge(a, b, x + .5f, y + .5f) && InsideEdge(b, c, x + .5f, y + .5f)
                                && InsideEdge(c, a, x + .5f, y + .5f))
                            {
                                target.CurrentPixels[y * target.Width + x]++;
                                visible = true;
                            }
                        }
                    }
                }
            }

            return visible;
        }

        /// <summary>Evaluates one oriented triangle edge in target pixel coordinates.</summary>
        /// <param name="a">The first edge vertex.</param>
        /// <param name="b">The second edge vertex.</param>
        /// <param name="x">The sample's horizontal coordinate.</param>
        /// <param name="y">The sample's vertical coordinate.</param>
        /// <returns>The signed edge distance scaled by edge length.</returns>
        private static float Edge(Vector3 a, Vector3 b, float x, float y)
        {
            return (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x);
        }

        /// <summary>Excludes the shared edge of adjacent triangles from double counting.</summary>
        /// <param name="a">The first edge vertex.</param>
        /// <param name="b">The second edge vertex.</param>
        /// <param name="x">The sample's horizontal coordinate.</param>
        /// <param name="y">The sample's vertical coordinate.</param>
        /// <returns>Whether the sample belongs to this side of the half-open edge.</returns>
        private static bool InsideEdge(Vector3 a, Vector3 b, float x, float y)
        {
            var edge = Edge(a, b, x, y);
            return edge > 0f || edge == 0f && (b.y > a.y || b.y == a.y && b.x < a.x);
        }
    }
}
