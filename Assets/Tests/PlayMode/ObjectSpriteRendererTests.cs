#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Rendering.Pipeline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Verifies rendered pixels and resource ownership through the actual render pipeline camera path.</summary>
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
        /// <returns>A frame boundary allowing Unity to initialize the render pipeline before rendering.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "GPU tests require a graphics device.");
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<PvZRenderPipelineAsset>());
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

        /// <summary>Checks standard source-alpha blending through the sprite shader.</summary>
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

        /// <summary>Checks direct camera presentation, shared painter order, and retirement of an unused secondary pool.</summary>
        /// <returns>Frame boundaries allowing the scene camera to render the published scene objects.</returns>
        [UnityTest]
        public IEnumerator PresentsSceneObjectsWithoutFrameCompositing()
        {
            using var overlay = new ObjectSpriteRenderer();
            ObjectSpriteRenderer.BeginSceneFrame();
            using (_renderer.BeginFrame())
            {
                Draw(Color.red, Screen.width, Screen.height, Matrix4x4.identity);
            }

            using (overlay.BeginFrame())
            {
                overlay.DrawQuad(_white, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.identity, Color.green, 0);
            }

            var camera = ObjectSpriteRenderer.SceneCamera;
            Assert.That(camera.enabled, Is.True);
            Assert.That(camera.targetTexture, Is.Null, "Production presentation must render directly to the display.");
            camera.targetTexture = _target;
            try
            {
                yield return null;
                ReadTargetPixels();
                AssertColor(_pixels.GetPixel(16, 16), Color.green);

                ObjectSpriteRenderer.BeginSceneFrame();
                using (_renderer.BeginFrame())
                {
                    Draw(Color.blue, Screen.width, Screen.height, Matrix4x4.identity);
                }

                yield return null;
                ReadTargetPixels();
                AssertColor(_pixels.GetPixel(16, 16), Color.blue);
                Assert.That(_renderer.PooledObjectCount, Is.EqualTo(1));
            }
            finally
            {
                camera.targetTexture = null;
            }
        }

        /// <summary>Checks that cropped native rendering preserves published scene geometry and the direct camera.</summary>
        /// <returns>A frame boundary allowing the scene camera to render after native capture.</returns>
        [UnityTest]
        public IEnumerator NativeCapturePreservesPublishedSceneObjects()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            using (_renderer.BeginFrame())
            {
                Draw(Color.red, Screen.width, Screen.height, Matrix4x4.identity);
            }

            var camera = ObjectSpriteRenderer.SceneCamera;
            var position = camera.transform.position;
            _renderer.RenderTo(_target);
            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.targetTexture, Is.Null);
            camera.targetTexture = _target;
            try
            {
                yield return null;
                ReadTargetPixels();
                AssertColor(_pixels.GetPixel(16, 16), Color.red);
            }
            finally
            {
                camera.targetTexture = null;
            }
        }

        /// <summary>Checks that oversized scene sprites cannot paint outside the shared top-left display clip.</summary>
        /// <returns>A frame boundary allowing the direct camera to render the clipped geometry.</returns>
        [UnityTest]
        public IEnumerator ClipsOversizedSceneSpritesToViewport()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(Screen.width * .25f, Screen.height * .125f, Screen.width * .5f, Screen.height * .375f);
            using (_renderer.BeginFrame())
            {
                Draw(Color.red, Screen.width * 2f, Screen.height * 2f,
                    Matrix4x4.Translate(new Vector3(-Screen.width * .5f, -Screen.height * .5f)));
            }

            var camera = ObjectSpriteRenderer.SceneCamera;
            camera.targetTexture = _target;
            try
            {
                yield return null;
                ReadTargetPixels();
                AssertColor(_pixels.GetPixel(16, 24), Color.red);
                AssertColor(_pixels.GetPixel(2, 24), Color.black);
                AssertColor(_pixels.GetPixel(16, 8), Color.black);
            }
            finally
            {
                camera.targetTexture = null;
            }
        }

        /// <summary>Checks that native cropped surfaces use local coordinates independently of the display clip.</summary>
        [Test]
        public void NativeCaptureIgnoresDisplayClip()
        {
            SpriteDrawState.Clip = new Rect(1000f, 1000f, 2f, 2f);
            _renderer.BeginBatch();
            Draw(Color.red, 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), Color.red);
        }

        /// <summary>Checks a local capture excludes a different pool's published display and retired native geometry.</summary>
        [Test]
        public void CaptureIsolatesOtherSceneAndNativePools()
        {
            using var overlay = new ObjectSpriteRenderer();
            using var native = new ObjectSpriteRenderer();
            ObjectSpriteRenderer.BeginSceneFrame();
            using (overlay.BeginFrame())
            {
                overlay.DrawQuad(_white, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.identity, Color.green, 0);
            }

            native.BeginBatch();
            native.DrawQuad(_white, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.blue, 0);
            native.RenderTo(_target);
            _renderer.BeginBatch();
            Draw(Color.red, 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), Color.red);
            Assert.That(ObjectSpriteRenderer.SceneCamera.targetTexture, Is.Null);
        }

        /// <summary>Checks that a thousand compatible source commands use one actual mesh draw.</summary>
        [Test]
        public void CombinesOneThousandAdjacentSpritesIntoOneBatch()
        {
            _renderer.BeginBatch();
            for (var index = 0; index < 1000; index++)
            {
                Draw(new Color(index / 999f, 0.25f, 0.5f, 0.5f), 32f, 32f, Matrix4x4.identity);
            }

            Capture();
            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1000));
            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
            Assert.That(_renderer.LastFrameStatistics.VertexCount, Is.EqualTo(4000));
            Assert.That(_renderer.LastFrameStatistics.TriangleCount, Is.EqualTo(2000));
            Assert.That(_renderer.LastFrameStatistics.CoveredPixelArea, Is.EqualTo(1024000d));
            Assert.That(_renderer.SubmittedSprites[0].GameObject, Is.Null, "Synthetic commands must not create a GameObject per source sprite.");
            Assert.That(_pixels.GetPixel(16, 16).r, Is.GreaterThan(0.98f));
        }

        /// <summary>Checks different per-source effects can coexist in one mesh without material property overrides.</summary>
        [Test]
        public void BatchesDistinctGrayscaleTintAndProducerEffects()
        {
            _renderer.BeginBatch();
            _renderer.DrawQuad(_white, 16f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                Color.red, 0, grayscale: 1f);
            _renderer.DrawQuad(_white, 16f, 32f, new Rect(0f, 0f, 1f, 1f),
                Matrix4x4.Translate(new Vector3(16f, 0f, 0f)), new Color(0.2f, 0.4f, 0.6f, 1f), 0,
                producerAdditive: 1f);
            Capture();
            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
            AssertColor(_pixels.GetPixel(8, 16), new Color(0.299f, 0.299f, 0.299f, 1f));
            AssertColor(_pixels.GetPixel(24, 16), new Color(0.4f, 0.8f, 1f, 1f));
        }

        /// <summary>Checks a shared painter-order gap prevents merging across an intervening Canvas or pool layer.</summary>
        [Test]
        public void SplitsBatchesAtSharedPainterOrderBarrier()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            using (_renderer.BeginFrame())
            {
                Draw(new Color(1f, 0f, 0f, 0.5f), 32f, 32f, Matrix4x4.identity);
                _ = SpriteDrawState.NextOrder();
                Draw(new Color(0f, 0f, 1f, 0.5f), 32f, 32f, Matrix4x4.identity);
            }

            Assert.That(_renderer.BatchCount, Is.EqualTo(2));
            Assert.That(_renderer.LastFrameStatistics.PainterOrderSplitCount, Is.EqualTo(1));
        }

        /// <summary>Checks hardware blend changes preserve submission order between otherwise compatible sprites.</summary>
        [Test]
        public void PreservesOrderAcrossAlphaAndAdditiveBatches()
        {
            _renderer.BeginBatch();
            Draw(new Color(1f, 0f, 0f, 0.5f), 32f, 32f, Matrix4x4.identity);
            _renderer.DrawQuad(_white, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                new Color(0f, 0.25f, 0f, 1f), 2);
            Draw(new Color(0f, 0f, 1f, 0.5f), 32f, 32f, Matrix4x4.identity);
            Capture();
            Assert.That(_renderer.BatchCount, Is.EqualTo(3));
            Assert.That(_renderer.LastFrameStatistics.MaterialSplitCount, Is.EqualTo(2));
            AssertColor(_pixels.GetPixel(16, 16), new Color(0.25f, 0.125f, 0.5f, 0.75f));
        }

        /// <summary>Checks fused source-color hits preserve translucent edges, independent tint, and grayscale.</summary>
        [Test]
        public void FusedHitMatchesSeparateNormalAndAdditivePasses()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                source.SetPixels(new[]
                {
                    new Color(0.8f, 0.6f, 0.4f, 0f), new Color(0.8f, 0.6f, 0.4f, 1f),
                    new Color(0.8f, 0.6f, 0.4f, 0f), new Color(0.8f, 0.6f, 0.4f, 1f)
                });
                source.Apply();
                var tint = new Color(0.2f, 0.8f, 0.4f, 0.6f);
                var hit = new Color(0.4f, 0.2f, 0.1f, 1f);
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                    tint, 0, grayscale: 0.75f, producerAdditive: 0.5f, additive: 0.5f, addColor: hit);
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                    tint, 1, additive: 0.5f, addColor: hit);
                Capture();
                var reference = _pixels.GetPixels();
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                    tint, 0, grayscale: 0.75f, producerAdditive: 0.5f, additive: 0.5f, addColor: hit, combineHit: true);
                Capture();
                var actual = _pixels.GetPixels();
                for (var index = 0; index < actual.Length; index++)
                {
                    Assert.That(actual[index].r, Is.EqualTo(reference[index].r).Within(3f / 255f));
                    Assert.That(actual[index].g, Is.EqualTo(reference[index].g).Within(3f / 255f));
                    Assert.That(actual[index].b, Is.EqualTo(reference[index].b).Within(3f / 255f));
                    Assert.That(actual[index].a, Is.EqualTo(reference[index].a).Within(1f / 255f));
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Checks fixed-range target alpha clamping does not brighten the original straight-alpha source color.</summary>
        /// <param name="alpha">The source tint alpha at or above the fully opaque boundary.</param>
        [TestCase(1f)]
        [TestCase(2f)]
        public void PreservesOpaqueSourceColorWhenTintAlphaExceedsOne(float alpha)
        {
            _renderer.BeginBatch();
            Draw(new Color(0.2f, 0.4f, 0.6f, alpha), 32f, 32f, Matrix4x4.identity);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), new Color(0.2f, 0.4f, 0.6f, 1f));
        }

        /// <summary>Checks standalone textured addition keeps its source hue even with a black RGB tint.</summary>
        [Test]
        public void StandaloneHitRetainsOriginalSourceColor()
        {
            _renderer.BeginBatch();
            Draw(new Color(0.1f, 0f, 0f, 1f), 32f, 32f, Matrix4x4.identity);
            _renderer.DrawQuad(_white, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                new Color(0f, 0f, 0f, 0.5f), 1, additive: 0.5f);
            Capture();
            AssertColor(_pixels.GetPixel(16, 16), new Color(0.35f, 0.25f, 0.25f, 1f));
        }

        /// <summary>Checks an already premultiplied surface is not multiplied by its alpha for a second time.</summary>
        [Test]
        public void PreservesPremultipliedSurfaceColor()
        {
            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                source.SetPixel(0, 0, new Color(0.2f, 0.1f, 0f, 0.5f));
                source.Apply();
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 3);
                Capture();
                AssertColor(_pixels.GetPixel(16, 16), new Color(0.2f, 0.1f, 0.5f, 1f));
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Checks premultiplied RGB retains the existing independent tint-alpha semantics.</summary>
        [Test]
        public void PremultipliedSurfaceDoesNotMultiplyRgbByTintAlpha()
        {
            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                source.SetPixel(0, 0, new Color(0.2f, 0.1f, 0f, 0.5f));
                source.Apply();
                _renderer.BeginBatch();
                Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity,
                    new Color(0.5f, 1f, 1f, 0f), 3);
                Capture();
                AssertColor(_pixels.GetPixel(16, 16), new Color(0.1f, 0.1f, 1f, 1f));
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Checks negative source UV axes preserve both mirrored geometry and original texture sampling.</summary>
        [Test]
        public void PreservesReversedSourceUvAxes()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
                source.Apply();
                _renderer.BeginBatch();
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.TRS(new Vector3(32f, 32f, 0f), Quaternion.identity, new Vector3(-1f, -1f, 1f)), Color.white, 0);
                Capture();
                var reference = _pixels.GetPixels();
                _renderer.BeginBatch();
                _renderer.DrawQuad(source, 32f, 32f, new Rect(1f, 1f, -1f, -1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                var actual = _pixels.GetPixels();
                for (var index = 0; index < actual.Length; index++)
                {
                    AssertColor(actual[index], reference[index]);
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Compares CPU clipping with the original shader at inclusive pixel-center bounds and original quad edges.</summary>
        /// <param name="originalSize">The original quad extent before any destination clipping.</param>
        /// <param name="clipMaximum">The inclusive original fragment clipping boundary.</param>
        /// <param name="cornerVisible">Whether the original shader retains the sample centered at logical 10.5,10.5.</param>
        [TestCase(32f, 10.49f, false)]
        [TestCase(32f, 10.499f, false)]
        [TestCase(32f, 10.5f, true)]
        [TestCase(32f, 10.501f, true)]
        [TestCase(32f, 10.51f, true)]
        [TestCase(10.5f, 10.5f, false)]
        public void DestinationClipMatchesOriginalFragmentBoundaryAtPixelCenters(float originalSize, float clipMaximum, bool cornerVisible)
        {
            AssertOriginalFragmentClip(new Vector2(originalSize, originalSize), new Rect(0f, 0f, clipMaximum, clipMaximum),
                Matrix4x4.identity, false, cornerVisible);
        }

        /// <summary>Checks both minimum clipping edges retain exactly the original inclusive sample-center comparisons.</summary>
        /// <param name="minimum">The inclusive minimum on both destination axes.</param>
        /// <param name="cornerVisible">Whether the sample centered at logical 10.5,10.5 remains visible.</param>
        [TestCase(10.499f, true)]
        [TestCase(10.5f, true)]
        [TestCase(10.501f, false)]
        public void DestinationClipMinimumMatchesOriginalFragmentBoundary(float minimum, bool cornerVisible)
        {
            AssertOriginalFragmentClip(new Vector2(32f, 32f), Rect.MinMaxRect(minimum, minimum, 32f, 32f),
                Matrix4x4.identity, false, cornerVisible);
        }

        /// <summary>Checks all four clipping edges preserve source samples even when either quad axis is inverted.</summary>
        /// <param name="mirrorX">Whether the original horizontal geometry axis is reversed.</param>
        /// <param name="mirrorY">Whether the original vertical geometry axis is reversed.</param>
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void DestinationClipPreservesInclusiveEdgesForInvertedQuads(bool mirrorX, bool mirrorY)
        {
            var matrix = Matrix4x4.TRS(new Vector3(mirrorX ? 32f : 0f, mirrorY ? 32f : 0f, 0f),
                Quaternion.identity, new Vector3(mirrorX ? -1f : 1f, mirrorY ? -1f : 1f, 1f));
            AssertOriginalFragmentClip(new Vector2(32f, 32f), new Rect(10.5f, 10.5f, 11f, 11f), matrix, false, true);
        }

        /// <summary>Checks a quad beginning exactly on an inclusive maximum edge is not rejected by bounding-box culling.</summary>
        /// <param name="tieX">Whether the quad begins on the maximum horizontal clip edge.</param>
        /// <param name="tieY">Whether the quad begins on the maximum vertical clip edge.</param>
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void DestinationClipRetainsQuadMinimumTiedWithInclusiveMaximum(bool tieX, bool tieY)
        {
            var matrix = Matrix4x4.Translate(new Vector3(tieX ? 10.5f : 0f, tieY ? 10.5f : 0f, 0f));
            // The original quad's top edge remains half-open in the render-texture fill rule.
            AssertOriginalFragmentClip(new Vector2(32f, 32f), new Rect(0f, 0f, 10.5f, 10.5f), matrix, false, !tieY);
        }

        /// <summary>Checks scene clipping follows the actual target sample grid when screen and render-target dimensions differ.</summary>
        /// <param name="pixelOffset">The clipping displacement from target pixel center 10.5.</param>
        [TestCase(-.001f)]
        [TestCase(0f)]
        [TestCase(.001f)]
        public void SceneClipUsesTargetPixelGridBeforeSubmission(float pixelOffset)
        {
            var size = new Vector2(Screen.width, Screen.height);
            var maximum = new Vector2((10.5f + pixelOffset) * size.x / _target.width,
                (10.5f + pixelOffset) * size.y / _target.height);
            AssertOriginalFragmentClip(size, new Rect(Vector2.zero, maximum), Matrix4x4.identity, true, pixelOffset >= 0f);
        }

        /// <summary>Renders the original fragment shader and current combined renderer independently and compares every target sample.</summary>
        /// <param name="size">The source quad's dimensions before clipping.</param>
        /// <param name="clip">The original inclusive destination-coordinate clip.</param>
        /// <param name="matrix">The source-to-destination transform, including inverted axes.</param>
        /// <param name="scene">Whether to use the scene camera's scaled target rather than the native unit-pixel capture contract.</param>
        /// <param name="cornerVisible">Whether original target sample 10,21 is expected to survive.</param>
        private void AssertOriginalFragmentClip(Vector2 size, Rect clip, Matrix4x4 matrix, bool scene, bool cornerVisible)
        {
            var worldSize = scene ? new Vector2(Screen.width, Screen.height) : new Vector2(32f, 32f);
            var referenceObject = new GameObject("Original fragment clip reference")
            {
                layer = 30
            };
            referenceObject.transform.position = new Vector3(200000f, 200000f, 0f);
            var cameraObject = new GameObject("Original fragment clip camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(200000f + worldSize.x * .5f, 200000f - worldSize.y * .5f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = worldSize.y * .5f;
            camera.aspect = worldSize.x / worldSize.y;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 20f;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            var corners = new[] { Vector3.zero, new Vector3(size.x, 0f, 0f),
                new Vector3(size.x, size.y, 0f), new Vector3(0f, size.y, 0f) };
            for (var index = 0; index < corners.Length; index++)
            {
                var point = matrix.MultiplyPoint3x4(corners[index]);
                corners[index] = new Vector3(point.x, -point.y, 0f);
            }

            var mesh = new Mesh
            {
                vertices = corners,
                uv = new[] { Vector2.up, Vector2.one, Vector2.right, Vector2.zero },
                colors = new[] { Color.white, Color.white, Color.white, Color.white },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            Assert.That(shader, Is.Not.Null, "The reference must use the original fragment-clipping shader.");
            var material = new Material(shader)
            {
                mainTexture = _white,
                renderQueue = (int)RenderQueue.Geometry
            };
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            material.SetInt("_DstBlendAlpha", (int)BlendMode.Zero);
            material.SetFloat("_ClipEnabled", 1f);
            material.SetVector("_ClipRect", new Vector4(clip.xMin, clip.yMin, clip.xMax, clip.yMax));
            referenceObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var referenceRenderer = referenceObject.AddComponent<MeshRenderer>();
            referenceRenderer.sharedMaterial = material;
            var sceneCamera = scene ? ObjectSpriteRenderer.SceneCamera : null;
            var previousTarget = sceneCamera == null ? null : sceneCamera.targetTexture;
            var previousBackground = sceneCamera == null ? Color.black : sceneCamera.backgroundColor;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = _target });
                ReadTargetPixels();
                var expected = _pixels.GetPixels();
                AssertColor(expected[21 * 32 + 10], cornerVisible ? Color.white : Color.clear);
                if (sceneCamera != null)
                {
                    sceneCamera.targetTexture = _target;
                    sceneCamera.backgroundColor = Color.clear;
                    ObjectSpriteRenderer.BeginSceneFrame();
                    using (_renderer.BeginFrame())
                    {
                        _renderer.DrawQuad(_white, size.x, size.y, new Rect(0f, 0f, 1f, 1f), matrix, Color.white, 0, clipOverride: clip);
                    }

                    RenderPipeline.SubmitRenderRequest(sceneCamera, new RenderPipeline.StandardRequest { destination = _target });
                    ReadTargetPixels();
                }
                else
                {
                    _renderer.BeginBatch();
                    _renderer.DrawQuad(_white, size.x, size.y, new Rect(0f, 0f, 1f, 1f), matrix, Color.white, 0, clipOverride: clip);
                    Capture();
                }

                var actual = _pixels.GetPixels();
                for (var index = 0; index < actual.Length; index++)
                {
                    AssertColor(actual[index], expected[index]);
                }
            }
            finally
            {
                if (sceneCamera != null)
                {
                    sceneCamera.targetTexture = previousTarget;
                    sceneCamera.backgroundColor = previousBackground;
                }

                referenceRenderer.enabled = false;
                UnityEngine.Object.Destroy(referenceObject);
                UnityEngine.Object.Destroy(cameraObject);
                UnityEngine.Object.Destroy(mesh);
                UnityEngine.Object.Destroy(material);
            }
        }

        /// <summary>Checks rotated destination clipping interpolates UVs without changing visible source pixels.</summary>
        [Test]
        public void ClipsRotatedGeometryWithoutChangingInteriorTextureSamples()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
                source.Apply();
                var matrix = Matrix4x4.TRS(new Vector3(16f, -6f, 0f), Quaternion.Euler(0f, 0f, 45f), Vector3.one);
                _renderer.BeginBatch();
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), matrix, Color.white, 0);
                Capture();
                var reference = _pixels.GetPixels();
                _renderer.BeginBatch();
                _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), matrix, Color.white, 0,
                    clipOverride: new Rect(8f, 8f, 16f, 16f));
                Capture();
                for (var y = 0; y < 32; y++)
                {
                    for (var x = 0; x < 32; x++)
                    {
                        var expected = x >= 8 && x < 24 && y >= 8 && y < 24 ? reference[y * 32 + x] : Color.clear;
                        AssertColor(_pixels.GetPixel(x, y), expected);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Checks alpha-zero and fully excluded commands contribute no GPU geometry.</summary>
        [Test]
        public void OmitsTransparentAndFullyClippedCommands()
        {
            _renderer.BeginBatch();
            Draw(Color.clear, 32f, 32f, Matrix4x4.identity);
            _renderer.DrawQuad(_white, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0,
                clipOverride: new Rect(100f, 100f, 8f, 8f));
            Capture();
            Assert.That(_renderer.SubmittedObjectCount, Is.Zero);
            Assert.That(_renderer.BatchCount, Is.Zero);
            Assert.That(_renderer.LastFrameStatistics.CulledSpriteCount, Is.EqualTo(2));
            AssertColor(_pixels.GetPixel(16, 16), Color.clear);
        }

        /// <summary>Checks large compatible command streams split before exceeding portable 16-bit mesh indices.</summary>
        [Test]
        public void SplitsLargeMeshesAtPortableVertexLimit()
        {
            _renderer.BeginBatch();
            for (var index = 0; index < 17000; index++)
            {
                Draw(new Color(1f, 0f, 0f, 0.5f), 1f, 1f, Matrix4x4.identity);
            }

            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(17000));
            Assert.That(_renderer.BatchCount, Is.EqualTo(2));
            Assert.That(_renderer.LastFrameStatistics.CapacitySplitCount, Is.EqualTo(1));
            Capture();
            AssertColor(_pixels.GetPixel(0, 31), Color.red);
        }

        /// <summary>Checks lossless multi-page tiles preserve logical bounds, transparent trim, and reversed sampling.</summary>
        [Test]
        public void RendersTrimmedLogicalImageAcrossIndependentAtlasPages()
        {
            var firstPage = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
            var secondPage = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    firstPage.SetPixel(x, y, Color.red);
                    secondPage.SetPixel(x, y, Color.green);
                }
            }

            firstPage.Apply();
            secondPage.Apply();
            var source = new SpriteAtlasImage
            {
                SourceIdentity = "images/synthetic-tiled.png",
                Width = 8,
                Height = 4,
                VisiblePixels = new[] { new Rect(1f, 0f, 6f, 4f) },
                Tiles = new[]
                {
                    new SpriteAtlasTile { Page = 0, SourcePixels = new Rect(0f, 0f, 4f, 4f),
                        TextureUv = new Rect(0f, 0f, 1f, 1f), PageWidth = 4, PageHeight = 4 },
                    new SpriteAtlasTile { Page = 1, SourcePixels = new Rect(4f, 0f, 4f, 4f),
                        TextureUv = new Rect(0f, 0f, 1f, 1f), PageWidth = 4, PageHeight = 4 }
                }
            };
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[] { new SpriteAtlasLayout { MaximumPageSize = 256,
                    Pages = new[] { "first", "second" }, Images = new[] { source } } }
            };
            try
            {
                using var provider = new SpriteAssetProvider(catalog, 256,
                    path => path == "first" ? firstPage : secondPage,
                    _ =>
                    {
                    });
                var image = provider.LoadImage(source.SourceIdentity);
                _renderer.BeginBatch();
                _renderer.DrawQuad(image, 32f, 16f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.BatchCount, Is.EqualTo(2));
                Assert.That(_renderer.SubmittedSprites[0].SourceImage, Is.SameAs(image));
                Assert.That(_renderer.SubmittedSprites[0].Vertices[0], Is.EqualTo(Vector3.zero));
                Assert.That(_renderer.SubmittedSprites[0].Vertices[2].x, Is.EqualTo(32f));
                Assert.That(image.SourceTexture, Is.Null, "Baked images must never reconstruct a GPU source texture.");
                AssertColor(_pixels.GetPixel(2, 24), Color.clear);
                AssertColor(_pixels.GetPixel(8, 24), Color.red);
                AssertColor(_pixels.GetPixel(24, 24), Color.green);
                AssertColor(_pixels.GetPixel(30, 24), Color.clear);
                _renderer.BeginBatch();
                _renderer.DrawQuad(image, 32f, 16f, new Rect(1f, 0f, -1f, 1f), Matrix4x4.identity, Color.white, 0);
                Capture();
                AssertColor(_pixels.GetPixel(2, 24), Color.clear);
                AssertColor(_pixels.GetPixel(8, 24), Color.green);
                AssertColor(_pixels.GetPixel(24, 24), Color.red);
                AssertColor(_pixels.GetPixel(30, 24), Color.clear);
            }
            finally
            {
                UnityEngine.Object.Destroy(firstPage);
                UnityEngine.Object.Destroy(secondPage);
            }
        }

        /// <summary>Checks repeated native surface destruction cannot leave unbounded cached material bindings.</summary>
        /// <returns>A frame boundary allowing the previous source texture to be destroyed.</returns>
        [UnityTest]
        public IEnumerator RetiresBindingsAfterTheirTextureIsDestroyed()
        {
            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            source.SetPixel(0, 0, Color.red);
            source.Apply();
            _renderer.BeginBatch();
            _renderer.DrawQuad(source, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
            Capture();
            Assert.That(_renderer.CachedMaterialCount, Is.EqualTo(3));
            UnityEngine.Object.Destroy(source);
            yield return null;
            _renderer.BeginBatch();
            Assert.That(_renderer.CachedMaterialCount, Is.Zero);
            Draw(Color.blue, 32f, 32f, Matrix4x4.identity);
            Capture();
            Assert.That(_renderer.CachedMaterialCount, Is.EqualTo(3));
            AssertColor(_pixels.GetPixel(16, 16), Color.blue);
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

        /// <summary>Renders through the pipeline and reads back pixels while preserving the active target.</summary>
        private void Capture()
        {
            _renderer.RenderTo(_target);
            ReadTargetPixels();
        }

        /// <summary>Reads pixels from the current test target without issuing a render request.</summary>
        private void ReadTargetPixels()
        {
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
