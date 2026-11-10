#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Verifies rendered pixels and resource ownership through the actual URP camera path.</summary>
    public sealed class ObjectSpriteRendererTests
    {
        /// <summary>The texture used for solid-color geometry.</summary>
        private Texture2D _white = null!;

        /// <summary>The readable texture used for GPU result inspection.</summary>
        private Texture2D _pixels = null!;

        /// <summary>The offscreen target receiving a complete sprite batch.</summary>
        private RenderTexture _target = null!;

        /// <summary>The renderer owned by the current test.</summary>
        private ObjectSpriteRenderer _renderer = null!;

        /// <summary>Initializes the active render pipeline and allocates isolated synthetic resources.</summary>
        /// <returns>A frame boundary allowing Unity to initialize URP before rendering.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "GPU tests require a graphics device.");
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            _white = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            _pixels = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            _target = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _target.Create();
            _renderer = new ObjectSpriteRenderer();
        }

        /// <summary>Releases the renderer and all textures after the deferred destruction frame.</summary>
        /// <returns>A frame boundary allowing Unity to release the owned objects.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer?.Dispose();
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }

            UnityEngine.Object.Destroy(_white);
            UnityEngine.Object.Destroy(_pixels);
            yield return null;
        }

        /// <summary>Checks that a textured object reaches the render target with correct color and alpha.</summary>
        [Test]
        public void RendersOpaqueTexturedQuad()
        {
            _renderer.BeginBatch();
            Draw(Color.red, 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), Color.red);
            Assert.That(_renderer.PooledObjectCount, Is.EqualTo(1));
        }

        /// <summary>Checks painter order for overlapping scene renderers.</summary>
        [Test]
        public void PreservesSubmissionOrder()
        {
            _renderer.BeginBatch();
            Draw(Color.red, 32f, 32f, Matrix4x4.identity);
            Draw(Color.green, 16f, 16f, Matrix4x4.Translate(new Vector3(8f, 8f, 0f)));
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), Color.green);
            AssertColor(_pixels.GetPixel(2, 2), Color.red);
        }

        /// <summary>Checks standard source-alpha blending through the URP shader.</summary>
        [Test]
        public void BlendsTranslucentObjects()
        {
            _renderer.BeginBatch();
            Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
            Draw(new Color(1f, 0f, 0f, 0.5f), 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), new Color(0.5f, 0f, 0.5f, 1f));
        }

        /// <summary>Checks that a later opaque object occludes an earlier translucent object.</summary>
        [Test]
        public void OpaqueObjectOccludesEarlierTranslucentObject()
        {
            _renderer.BeginBatch();
            Draw(new Color(0f, 1f, 0f, 0.5f), 32f, 32f, Matrix4x4.identity);
            Draw(Color.red, 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), Color.red);
        }

        /// <summary>Checks that binary alpha textures retain filtered fractional edge coverage.</summary>
        [Test]
        public void PreservesFilteredBinaryAlphaEdges()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                texture.SetPixels(new[] { Color.red, new Color(1f, 0f, 0f, 0f), Color.red, new Color(1f, 0f, 0f, 0f) });
                texture.Apply();
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                var edge = _pixels.GetPixel(16, 16);
                Assert.That(edge.r, Is.InRange(0.4f, 0.6f));
                Assert.That(edge.b, Is.InRange(0.4f, 0.6f));
                Assert.That(edge.a, Is.GreaterThan(0.96f));
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>Checks that texture edits invalidate an earlier opaque classification.</summary>
        [Test]
        public void UpdatesCachedOpacityAfterTextureChanges()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixel(0, 0, Color.red);
                texture.Apply();
                _renderer.BeginBatch();
                _renderer.DrawQuad(texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                AssertColor(_pixels.GetPixel(16, 16), Color.red);

                texture.SetPixel(0, 0, Color.clear);
                texture.Apply();
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                AssertColor(_pixels.GetPixel(16, 16), Color.blue);
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>Checks that unreadable textures conservatively retain source-alpha blending.</summary>
        [Test]
        public void RendersUnreadableTextureWithAlpha()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.5f));
                texture.Apply(false, true);
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                AssertColor(_pixels.GetPixel(16, 16), new Color(0.5f, 0.5f, 1f, 1f));
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>Checks source-colored particle addition over opaque geometry.</summary>
        [Test]
        public void AddsParticleColorAfterOpaqueObject()
        {
            _renderer.BeginBatch();
            Draw(new Color(0.5f, 0f, 0f, 1f), 32f, 32f, Matrix4x4.identity);
            _renderer.DrawQuad(_white, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                new Color(0.25f, 0f, 0f, 1f), 2);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), new Color(0.75f, 0f, 0f, 1f));
        }

        /// <summary>Checks transforms, empty batches, renderer reuse, and transparent clearing.</summary>
        [Test]
        public void ReusesObjectsAndClearsUnusedBatch()
        {
            _renderer.BeginBatch();
            Draw(Color.yellow, 32f, 32f, Matrix4x4.Scale(new Vector3(0.5f, 1f, 1f)));
            Capture();
            AssertColor(_pixels.GetPixel(8, 16), Color.yellow);
            AssertColor(_pixels.GetPixel(24, 16), Color.clear);

            _renderer.BeginBatch();
            Draw(Color.cyan, 32f, 32f, Matrix4x4.identity);
            Capture();
            Assert.That(_renderer.PooledObjectCount, Is.EqualTo(1));
            AssertColor(_pixels.GetPixel(24, 16), Color.cyan);

            _renderer.BeginBatch();
            Capture();
            Assert.That(_renderer.SubmittedObjectCount, Is.Zero);
            AssertColor(_pixels.GetPixel(16, 16), Color.clear);
        }

        /// <summary>Checks that disposal invalidates the public command APIs and is idempotent.</summary>
        [Test]
        public void DisposeInvalidatesRenderer()
        {
            _renderer.Dispose();
            _renderer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _renderer.BeginBatch());
            Assert.Throws<ObjectDisposedException>(() => _renderer.RenderTo(_target));
        }

        /// <summary>Checks that a render request restores the caller's active render target.</summary>
        [Test]
        public void PreservesActiveRenderTarget()
        {
            var sentinel = new RenderTexture(8, 8, 0);
            sentinel.Create();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = sentinel;
                _renderer.BeginBatch();
                Draw(Color.red, 32f, 32f, Matrix4x4.identity);
                _renderer.RenderTo(_target);
                Assert.That(RenderTexture.active, Is.SameAs(sentinel));
            }
            finally
            {
                RenderTexture.active = previous;
                sentinel.Release();
                UnityEngine.Object.Destroy(sentinel);
            }
        }

        /// <summary>Submits a tinted white quad in top-left pixel coordinates.</summary>
        /// <param name="color">The quad tint and opacity.</param>
        /// <param name="width">The source width in pixels.</param>
        /// <param name="height">The source height in pixels.</param>
        /// <param name="matrix">The source-to-target transform.</param>
        private void Draw(Color color, float width, float height, Matrix4x4 matrix)
        {
            _renderer.DrawQuad(_white, width, height, new Rect(0f, 0f, 1f, 1f), matrix, color, 0);
        }

        /// <summary>Renders through URP and reads back pixels while preserving the active target.</summary>
        private void Capture()
        {
            _renderer.RenderTo(_target);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, 32f, 32f), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Compares the linear target channels with a tolerance for 8-bit storage.</summary>
        /// <param name="actual">The GPU-produced color.</param>
        /// <param name="expected">The expected composited color.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.035f), "red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.035f), "green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.035f), "blue");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.035f), "alpha");
        }
    }
}
