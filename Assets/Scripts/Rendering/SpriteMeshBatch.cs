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
        /// <summary>The interleaved CPU staging buffer, retained across changing populations.</summary>
        private SpriteBatchVertex[] _vertices = new SpriteBatchVertex[256];
        /// <summary>The portable fan indices retained independently of their active prefix.</summary>
        private ushort[] _indices = new ushort[384];
        /// <summary>The fixed Float32 shader attribute layout, allocated once.</summary>
        private static readonly VertexAttributeDescriptor[] s_layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4)
        };
        /// <summary>The populated vertex count.</summary>
        private int _vertexCount;
        /// <summary>The populated index count.</summary>
        private int _indexCount;
        /// <summary>The GPU vertex capacity, changed only when staging capacity grows.</summary>
        private int _gpuVertexCapacity;
        /// <summary>The GPU index capacity, changed only when staging capacity grows.</summary>
        private int _gpuIndexCapacity;
        /// <summary>The previously published active vertex count.</summary>
        private int _publishedVertexCount;
        /// <summary>The previously published active index count.</summary>
        private int _publishedIndexCount;
        /// <summary>Whether any retained vertex value changed.</summary>
        private bool _verticesDirty;
        /// <summary>Whether any retained fan index changed.</summary>
        private bool _indicesDirty;
        /// <summary>The minimum live position of this completed batch.</summary>
        private Vector3 _minimum;
        /// <summary>The maximum live position of this completed batch.</summary>
        private Vector3 _maximum;
        /// <summary>Gets the cumulative number of vertex buffer uploads for diagnostics.</summary>
        internal int VertexUploadCount { get; private set; }
        /// <summary>Gets the cumulative number of index buffer uploads for diagnostics.</summary>
        internal int IndexUploadCount { get; private set; }
        /// <summary>Gets the cumulative number of GPU buffer reconfigurations.</summary>
        internal int BufferReconfigurationCount { get; private set; }
        /// <summary>Gets all mesh objects created during this process, including subsequently disposed batches.</summary>
        internal static long TotalMeshCreationCount { get; private set; }
        /// <summary>Gets all actual vertex and index buffer reconfigurations during this process.</summary>
        internal static long TotalBufferReconfigurationCount { get; private set; }
        /// <summary>Gets all actual vertex and index upload bytes during this process.</summary>
        internal static long TotalUploadBytes { get; private set; }
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
            TotalMeshCreationCount++;
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
                GrowVertexBuffers(_vertexCount + 1);
            }
            var index = _vertexCount++;
            var vertex = new SpriteBatchVertex
            {
                Position = position, Coordinates = coordinates, Color = color, Effects = effects, Highlight = highlight
            };
            if (!_vertices[index].Equals(vertex))
            {
                _vertices[index] = vertex;
                _verticesDirty = true;
            }
            if (index == 0)
            {
                _minimum = position;
                _maximum = position;
            }
            else
            {
                _minimum = Vector3.Min(_minimum, position);
                _maximum = Vector3.Max(_maximum, position);
            }
        }

        /// <summary>Appends a completed numeric polygon with one capacity check and one copy of its shared attributes.</summary>
        /// <param name="vertices">The synchronously completed compact clipping output.</param>
        /// <param name="offset">The first completed vertex of this polygon.</param>
        /// <param name="count">The number of visible polygon vertices.</param>
        /// <param name="depth">The original painter depth applied to every vertex.</param>
        /// <param name="color">The full-precision source tint and opacity.</param>
        /// <param name="effects">The grayscale, brightness, mode and fused-hit state.</param>
        /// <param name="highlight">The source-colored highlight applied to every vertex.</param>
        /// <param name="order">The original source painter order.</param>
        internal void AppendClippedPolygon(SpriteClipVertex[] vertices, int offset, int count,
            float depth, Color color, Vector4 effects, Vector4 highlight, int order)
        {
            var first = _vertexCount;
            var required = first + count;
            if (required > _vertices.Length)
            {
                GrowVertexBuffers(required);
            }
            for (var index = 0; index < count; index++)
            {
                var vertex = vertices[offset + index];
                AddVertex(new Vector3(vertex.Position.x, -vertex.Position.y, depth), vertex.Coordinates,
                    color, effects, highlight);
            }
            CompletePolygon(first, count, order);
        }

        /// <summary>Grows all attribute buffers together only when a new population exceeds their retained capacity.</summary>
        /// <param name="required">The populated vertex capacity required by the next complete polygon.</param>
        private void GrowVertexBuffers(int required)
        {
            var capacity = Math.Min(MaximumVertexCount, Math.Max(required, _vertices.Length * 2));
            Array.Resize(ref _vertices, capacity);
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
                AppendIndex(firstVertex);
                AppendIndex(firstVertex + index);
                AppendIndex(firstVertex + index + 1);
            }

            LastOrder = order;
        }

        /// <summary>Stores one fan index and detects topology changes without allocating a comparison snapshot.</summary>
        /// <param name="value">The validated portable vertex index.</param>
        private void AppendIndex(int value)
        {
            var index = _indexCount++;
            if (_indices[index] != (ushort)value)
            {
                _indices[index] = (ushort)value;
                _indicesDirty = true;
            }
        }

        /// <summary>Publishes changed live prefixes without clearing or resizing stable GPU buffers.</summary>
        internal void Publish()
        {
            var vertexCapacityChanged = _gpuVertexCapacity != _vertices.Length;
            var indexCapacityChanged = _gpuIndexCapacity != _indices.Length;
            var rangeChanged = _publishedVertexCount != _vertexCount || _publishedIndexCount != _indexCount;
            if (!vertexCapacityChanged && !indexCapacityChanged && !rangeChanged && !_verticesDirty && !_indicesDirty)
            {
                return;
            }
            using var upload = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Upload);
            if (vertexCapacityChanged)
            {
                Mesh.SetVertexBufferParams(_vertices.Length, s_layout);
                _gpuVertexCapacity = _vertices.Length;
                BufferReconfigurationCount++;
                TotalBufferReconfigurationCount++;
            }
            if (indexCapacityChanged)
            {
                Mesh.SetIndexBufferParams(_indices.Length, IndexFormat.UInt16);
                _gpuIndexCapacity = _indices.Length;
                BufferReconfigurationCount++;
                TotalBufferReconfigurationCount++;
            }
            if (_vertexCount > 0 && (vertexCapacityChanged || _verticesDirty || _vertexCount > _publishedVertexCount))
            {
                Mesh.SetVertexBufferData(_vertices, 0, 0, _vertexCount, 0, MeshUpdateFlags.DontRecalculateBounds);
                VertexUploadCount++;
                TotalUploadBytes += _vertexCount * 68L;
            }
            if (_indexCount > 0 && (indexCapacityChanged || _indicesDirty || _indexCount > _publishedIndexCount))
            {
                Mesh.SetIndexBufferData(_indices, 0, 0, _indexCount, MeshUpdateFlags.DontRecalculateBounds);
                IndexUploadCount++;
                TotalUploadBytes += _indexCount * 2L;
            }
            var bounds = _vertexCount == 0 ? new Bounds() : new Bounds((_minimum + _maximum) * .5f, _maximum - _minimum);
            Mesh.subMeshCount = 1;
            Mesh.SetSubMesh(0, new SubMeshDescriptor(0, _indexCount, MeshTopology.Triangles)
            {
                firstVertex = 0, vertexCount = _vertexCount, bounds = bounds
            }, MeshUpdateFlags.DontRecalculateBounds);
            Mesh.bounds = bounds;
            _publishedVertexCount = _vertexCount;
            _publishedIndexCount = _indexCount;
            _verticesDirty = false;
            _indicesDirty = false;
        }
    }
}
