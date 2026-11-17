#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks retained GPU capacities, independent topology updates, and differential scene visibility.</summary>
    public sealed class SpriteMeshPublicationTests
    {
        /// <summary>The original backend restored after each test.</summary>
        private SpriteRenderingBackend _previousBackend;
        /// <summary>The isolated parent of the tested mesh.</summary>
        private GameObject _owner = null!;
        /// <summary>The tested interleaved mesh batch.</summary>
        private SpriteMeshBatch _batch = null!;
        /// <summary>The unlit source material used by the batch.</summary>
        private Material _material = null!;
        /// <summary>The source texture used by scene lifecycle checks.</summary>
        private Texture2D _texture = null!;
        /// <summary>The source pool used by scene lifecycle checks.</summary>
        private ObjectSpriteRenderer _renderer = null!;

        /// <summary>Creates a GPU batch and forces the independent mesh backend.</summary>
        /// <returns>A frame initializing the actual graphics pipeline.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _previousBackend = ObjectSpriteRenderer.Backend;
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            _owner = new GameObject("Stable mesh publication test");
            _batch = new SpriteMeshBatch(_owner.transform, 0);
            _material = new Material(Resources.Load<Shader>("Shaders/PvZSpriteBatch"));
            _texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _texture.SetPixel(0, 0, Color.white);
            _texture.Apply();
            _renderer = new ObjectSpriteRenderer();
        }

        /// <summary>Releases every owned resource and restores scene backend selection.</summary>
        /// <returns>A frame completing deferred destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.EndSceneFrame();
            ObjectSpriteRenderer.Backend = _previousBackend;
            _renderer.Dispose();
            UnityEngine.Object.Destroy(_batch.Mesh);
            UnityEngine.Object.Destroy(_material);
            UnityEngine.Object.Destroy(_owner);
            UnityEngine.Object.Destroy(_texture);
            yield return null;
        }

        /// <summary>Checks rebuilding identical values does not touch GPU buffers or their capacities.</summary>
        [Test]
        public void RepeatedContentSkipsUploadsAndGpuReconfiguration()
        {
            BuildQuad(Color.red, 10f);
            var vertices = _batch.VertexUploadCount;
            var indices = _batch.IndexUploadCount;
            var buffers = _batch.BufferReconfigurationCount;
            BuildQuad(Color.red, 10f);
            Assert.That(_batch.VertexUploadCount, Is.EqualTo(vertices));
            Assert.That(_batch.IndexUploadCount, Is.EqualTo(indices));
            Assert.That(_batch.BufferReconfigurationCount, Is.EqualTo(buffers));
        }

        /// <summary>Checks process diagnostics count exactly the live Float32 vertex and UInt16 index uploads without resetting between frames.</summary>
        [Test]
        public void CumulativeUploadBytesCountOnlyActualActivePrefixes()
        {
            var uploaded = ObjectSpriteRenderer.MeshUploadBytes;
            var configurations = ObjectSpriteRenderer.MeshBufferReconfigurationCount;
            BuildQuad(Color.red, 10f);
            Assert.That(ObjectSpriteRenderer.MeshUploadBytes, Is.EqualTo(uploaded + 4 * 68 + 6 * 2));
            Assert.That(ObjectSpriteRenderer.MeshBufferReconfigurationCount, Is.EqualTo(configurations + 2));
            uploaded = ObjectSpriteRenderer.MeshUploadBytes;
            BuildQuad(Color.red, 10f);
            Assert.That(ObjectSpriteRenderer.MeshUploadBytes, Is.EqualTo(uploaded));
            BuildQuad(Color.blue, 10f);
            Assert.That(ObjectSpriteRenderer.MeshUploadBytes, Is.EqualTo(uploaded + 4 * 68));
        }

        /// <summary>Checks changed tint updates only the Float32 vertex stream while preserving topology.</summary>
        [Test]
        public void TintChangeDoesNotUploadIndicesOrReconfigureBuffers()
        {
            BuildQuad(Color.red, 10f);
            var vertices = _batch.VertexUploadCount;
            var indices = _batch.IndexUploadCount;
            var buffers = _batch.BufferReconfigurationCount;
            var tint = new Color(1.25f, -.125f, .1234567f, .8123456f);
            BuildQuad(tint, 10f);
            Assert.That(_batch.VertexUploadCount, Is.EqualTo(vertices + 1));
            Assert.That(_batch.IndexUploadCount, Is.EqualTo(indices));
            Assert.That(_batch.BufferReconfigurationCount, Is.EqualTo(buffers));
            Assert.That(_batch.Mesh.colors[0], Is.EqualTo(tint));
        }

        /// <summary>Checks a smaller active range preserves capacity and excludes the old geometry from drawing and bounds.</summary>
        [Test]
        public void ShrinkingMeshUsesOnlyActiveIndicesAndBounds()
        {
            _batch.Reset(_material, 0, false);
            for (var index = 0; index < 600; index++)
            {
                AddQuad(Color.red, 1000f + index);
            }
            _batch.Publish();
            var capacity = _batch.Mesh.vertexCount;
            var buffers = _batch.BufferReconfigurationCount;
            BuildQuad(Color.blue, 3f);
            Assert.That(_batch.Mesh.vertexCount, Is.EqualTo(capacity));
            Assert.That(_batch.BufferReconfigurationCount, Is.EqualTo(buffers));
            Assert.That(_batch.Mesh.GetSubMesh(0).vertexCount, Is.EqualTo(4));
            Assert.That(_batch.Mesh.GetSubMesh(0).indexCount, Is.EqualTo(6));
            Assert.That(_batch.Mesh.bounds.min.x, Is.EqualTo(3f));
            Assert.That(_batch.Mesh.bounds.max.x, Is.EqualTo(4f));
        }

        /// <summary>Checks a stable visible scene pool avoids redundant native enabled setters.</summary>
        [Test]
        public void StableScenePrefixDoesNotToggleRendererVisibility()
        {
            DrawScene();
            var transitions = _renderer.RendererVisibilityChangeCount;
            DrawScene();
            Assert.That(_renderer.RendererVisibilityChangeCount, Is.EqualTo(transitions));
        }

        /// <summary>Checks a pool omitted from the next complete scene is hidden before camera rendering.</summary>
        [Test]
        public void EmptySceneRetiresPreviousVisiblePrefix()
        {
            DrawScene();
            var transitions = _renderer.RendererVisibilityChangeCount;
            using (ObjectSpriteRenderer.BeginSceneFrameScope())
            {
            }
            Assert.That(_renderer.RendererVisibilityChangeCount, Is.EqualTo(transitions + 1));
        }

        /// <summary>Checks a complete prepared source budget records a new live population without source construction misses.</summary>
        [Test]
        public void PreparedSourceBudgetAvoidsRecordSourceCapacityMisses()
        {
            Assert.That(_renderer.PrepareSourceCapacityStep(512, 64), Is.False);
            Assert.That(_renderer.PreparedSourceCapacity, Is.EqualTo(64));
            while (!_renderer.PrepareSourceCapacityStep(512, 64))
            {
            }
            _renderer.BeginBatch();
            var misses = _renderer.SourceCapacityMissCount;
            for (var index = 0; index < 512; index++)
            {
                _renderer.DrawQuad(_texture, 2f, 2f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
            }
            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(512));
            Assert.That(_renderer.SourceCapacityMissCount, Is.EqualTo(misses));
        }

        /// <summary>Creates and publishes one fixed-geometry quad.</summary>
        /// <param name="color">The unquantized vertex tint.</param>
        /// <param name="x">The left position of the quad.</param>
        private void BuildQuad(Color color, float x)
        {
            _batch.Reset(_material, 0, false);
            AddQuad(color, x);
            _batch.Publish();
        }

        /// <summary>Appends one fan with the original six-index winding.</summary>
        /// <param name="color">The unquantized vertex tint.</param>
        /// <param name="x">The left position of the quad.</param>
        private void AddQuad(Color color, float x)
        {
            var first = _batch.VertexCount;
            _batch.AddVertex(new Vector3(x, 0f), Vector2.zero, color, Vector4.zero, Vector4.zero);
            _batch.AddVertex(new Vector3(x + 1f, 0f), Vector2.right, color, Vector4.zero, Vector4.zero);
            _batch.AddVertex(new Vector3(x + 1f, 1f), Vector2.one, color, Vector4.zero, Vector4.zero);
            _batch.AddVertex(new Vector3(x, 1f), Vector2.up, color, Vector4.zero, Vector4.zero);
            _batch.CompletePolygon(first, 4, 0);
        }

        /// <summary>Draws one actual scene pool using its complete frame lifecycle.</summary>
        private void DrawScene()
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(_texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
            }
        }
    }
}
