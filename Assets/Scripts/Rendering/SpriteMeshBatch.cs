#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Owns one reusable, explicitly combined mesh with a single material binding.</summary>
    internal sealed class SpriteMeshBatch
    {
        /// <summary>The maximum vertices addressable without requiring 32-bit GPU indices.</summary>
        internal const int MaximumVertexCount = 65532;
        /// <summary>The retained transformed-position buffer; only its populated prefix is uploaded.</summary>
        private Vector3[] _vertices = new Vector3[256];
        /// <summary>The accumulated atlas coordinates.</summary>
        private Vector2[] _coordinates = new Vector2[256];
        /// <summary>The accumulated source tint and alpha.</summary>
        private Color[] _colors = new Color[256];
        /// <summary>The grayscale, producer brightness, render mode, and fused-hit switch per vertex.</summary>
        private Vector4[] _effects = new Vector4[256];
        /// <summary>The unfiltered source-color highlight per vertex.</summary>
        private Vector4[] _highlights = new Vector4[256];
        /// <summary>The ordered triangle indices.</summary>
        private int[] _indices = new int[384];
        /// <summary>The populated vertex prefix shared by all attribute buffers.</summary>
        private int _vertexCount;
        /// <summary>The populated triangle-index prefix.</summary>
        private int _indexCount;
        /// <summary>Whether the CPU buffers differ from the uploaded mesh.</summary>
        private bool _dirty;
        /// <summary>Gets the reusable geometry drawn by this batch.</summary>
        internal Mesh Mesh { get; }
        /// <summary>Gets the renderer for the combined geometry.</summary>
        internal MeshRenderer Renderer { get; }
        /// <summary>Gets the managed material binding without a renderer native-property lookup.</summary>
        internal Material? Material { get; private set; }
        /// <summary>The sorting order last published to the native renderer.</summary>
        private int _sortingOrder;
        /// <summary>Gets the last logical painter order appended to this batch.</summary>
        internal int LastOrder { get; private set; }
        /// <summary>Gets the number of vertices currently buffered.</summary>
        internal int VertexCount => _vertexCount;
        /// <summary>Gets the number of indices currently buffered.</summary>
        internal int IndexCount => _indexCount;

        /// <summary>Creates the persistent mesh and renderer below their owning pool.</summary>
        /// <param name="parent">The transform that owns the renderer pool.</param>
        /// <param name="index">The batch slot displayed in the hierarchy.</param>
        internal SpriteMeshBatch(Transform parent, int index)
        {
            var owner = new GameObject($"Batch {index}")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            owner.transform.SetParent(parent, false);
            Mesh = new Mesh
            {
                name = $"PvZ combined sprites {index}",
                hideFlags = HideFlags.DontSave,
                indexFormat = IndexFormat.UInt16
            };
            Mesh.MarkDynamic();
            owner.AddComponent<MeshFilter>().sharedMesh = Mesh;
            Renderer = owner.AddComponent<MeshRenderer>();
            Renderer.shadowCastingMode = ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
            Renderer.lightProbeUsage = LightProbeUsage.Off;
            Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            Renderer.enabled = false;
        }

        /// <summary>Starts a new contiguous run while retaining all allocated buffers.</summary>
        /// <param name="material">The single texture and blend-state binding for this run.</param>
        /// <param name="order">The first source painter order.</param>
        /// <param name="opaque">Whether depth ordering replaces transparent sorting.</param>
        internal void Reset(Material material, int order, bool opaque)
        {
            _vertexCount = 0;
            _indexCount = 0;
            if (!ReferenceEquals(Material, material))
            {
                Material = material;
                Renderer.sharedMaterial = material;
            }

            var sortingOrder = opaque ? 0 : order;
            if (_sortingOrder != sortingOrder)
            {
                _sortingOrder = sortingOrder;
                Renderer.sortingOrder = sortingOrder;
            }
            LastOrder = order;
            _dirty = true;
        }

        /// <summary>Checks material, painter continuity, and the portable vertex limit.</summary>
        /// <param name="material">The binding required by the next source polygon.</param>
        /// <param name="order">The source polygon's global painter order.</param>
        /// <param name="vertexCount">The number of vertices about to be appended.</param>
        /// <returns>True when appending cannot cross another renderer or overflow 16-bit indices.</returns>
        internal bool CanAppend(Material material, int order, int vertexCount)
        {
            return ReferenceEquals(Material, material) && (order == LastOrder || order == LastOrder + 1)
                && _vertexCount + vertexCount <= MaximumVertexCount;
        }

        /// <summary>Appends one vertex to the current polygon.</summary>
        /// <param name="position">The destination position and painter depth.</param>
        /// <param name="coordinates">The mapped source texture coordinates.</param>
        /// <param name="color">The original source tint.</param>
        /// <param name="effects">The source grayscale, brightness, mode, and fused-hit switch.</param>
        /// <param name="highlight">The source-colored hit multiplier.</param>
        internal void AddVertex(Vector3 position, Vector2 coordinates, Color color, Vector4 effects, Vector4 highlight)
        {
            if (_vertexCount == _vertices.Length)
            {
                var capacity = Math.Min(MaximumVertexCount, _vertices.Length * 2);
                Array.Resize(ref _vertices, capacity);
                Array.Resize(ref _coordinates, capacity);
                Array.Resize(ref _colors, capacity);
                Array.Resize(ref _effects, capacity);
                Array.Resize(ref _highlights, capacity);
            }
            var index = _vertexCount++;
            _vertices[index] = position;
            _coordinates[index] = coordinates;
            _colors[index] = color;
            _effects[index] = effects;
            _highlights[index] = highlight;
            _dirty = true;
        }

        /// <summary>Triangulates the last convex polygon without changing primitive order.</summary>
        /// <param name="firstVertex">The index of the polygon's first buffered vertex.</param>
        /// <param name="vertexCount">The polygon's number of vertices.</param>
        /// <param name="order">The original source painter order.</param>
        internal void CompletePolygon(int firstVertex, int vertexCount, int order)
        {
            var required = _indexCount + (vertexCount - 2) * 3;
            if (required > _indices.Length)
            {
                Array.Resize(ref _indices, Math.Max(required, _indices.Length * 2));
            }
            for (var index = 1; index < vertexCount - 1; index++)
            {
                _indices[_indexCount++] = firstVertex;
                _indices[_indexCount++] = firstVertex + index;
                _indices[_indexCount++] = firstVertex + index + 1;
            }

            LastOrder = order;
        }

        /// <summary>Uploads the completed batch once, without reallocating per source sprite.</summary>
        internal void Publish()
        {
            if (!_dirty)
            {
                return;
            }

            using var upload = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Upload);
            Mesh.Clear();
            Mesh.SetVertices(_vertices, 0, _vertexCount);
            Mesh.SetUVs(0, _coordinates, 0, _vertexCount);
            Mesh.SetColors(_colors, 0, _vertexCount);
            Mesh.SetUVs(1, _effects, 0, _vertexCount);
            Mesh.SetUVs(2, _highlights, 0, _vertexCount);
            Mesh.SetTriangles(_indices, 0, _indexCount, 0, true);
            _dirty = false;
        }
    }
}
