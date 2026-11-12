#nullable enable

using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Rendering.Pipeline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Verifies the project render pipeline: render requests, clearing, camera order, blend modes, clipping, and depth.</summary>
    public sealed class PvZRenderPipelineTests
    {
        /// <summary>The edge length of the offscreen targets used by these tests.</summary>
        private const int Size = 32;

        /// <summary>The offscreen target receiving each rendered result.</summary>
        private RenderTexture? _target;

        /// <summary>The readable texture receiving GPU results.</summary>
        private Texture2D? _pixels;

        /// <summary>The readable white texture whose opacity selects the depth-writing opaque path.</summary>
        private Texture2D? _white;

        /// <summary>The sprite pool owned by the current test.</summary>
        private ObjectSpriteRenderer? _renderer;

        /// <summary>The camera objects created by the current test.</summary>
        private GameObject? _cameraA;

        /// <summary>The second camera object created by camera ordering tests.</summary>
        private GameObject? _cameraB;

        /// <summary>Gets the offscreen target of the current test.</summary>
        private RenderTexture Target => _target ?? throw new System.InvalidOperationException("The test target is not allocated.");

        /// <summary>Gets the readback texture of the current test.</summary>
        private Texture2D Pixels => _pixels ?? throw new System.InvalidOperationException("The readback texture is not allocated.");

        /// <summary>Gets the readable white texture of the current test.</summary>
        private Texture2D White => _white ?? throw new System.InvalidOperationException("The white texture is not allocated.");

        /// <summary>Gets the sprite pool of the current test.</summary>
        private ObjectSpriteRenderer Renderer => _renderer ?? throw new System.InvalidOperationException("The sprite pool is not allocated.");

        /// <summary>Waits for the pipeline to initialize and allocates the shared test resources.</summary>
        /// <returns>A frame boundary allowing Unity to create the render pipeline.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "GPU tests require a graphics device.");
            _target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _target.Create();
            _pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            _white = CreateSolidTexture(Color.white);
            _renderer = new ObjectSpriteRenderer();
        }

        /// <summary>Releases the test cameras, sprite pool, and textures.</summary>
        /// <returns>A frame boundary allowing Unity to destroy the owned objects.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer?.Dispose();
            if (_cameraA != null)
            {
                Object.Destroy(_cameraA);
            }

            if (_cameraB != null)
            {
                Object.Destroy(_cameraB);
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            if (_pixels != null)
            {
                Object.Destroy(_pixels);
            }

            if (_white != null)
            {
                Object.Destroy(_white);
            }

            yield return null;
        }

        /// <summary>Checks that the project pipeline is active and that the sprite shader targets it.</summary>
        [Test]
        public void ProjectPipelineIsActive()
        {
            var asset = GraphicsSettings.currentRenderPipeline as PvZRenderPipelineAsset;
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset != null ? asset.renderPipelineShaderTag : null, Is.EqualTo(PvZRenderPipelineAsset.ShaderTag));
            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader != null && shader.isSupported, Is.True);
        }

        /// <summary>Checks that only standard render requests are accepted.</summary>
        [Test]
        public void SupportsOnlyStandardRenderRequests()
        {
            var camera = CreateCamera(ref _cameraA, 0f, Color.clear);
            Assert.That(RenderPipeline.SupportsRenderRequest(camera, new RenderPipeline.StandardRequest()), Is.True);
            Assert.That(RenderPipeline.SupportsRenderRequest(camera, "unsupported"), Is.False);
        }

        /// <summary>Checks that a render request clears its destination and restores the camera target.</summary>
        [Test]
        public void RenderRequestClearsDestinationAndRestoresCameraTarget()
        {
            var camera = CreateCamera(ref _cameraA, 0f, new Color(.25f, .5f, .75f, 1f));
            var request = new RenderPipeline.StandardRequest
            {
                destination = Target
            };
            RenderPipeline.SubmitRenderRequest(camera, request);
            Assert.That(camera.targetTexture, Is.Null);
            ReadTarget();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.25f, .5f, .75f, 1f));
        }

        /// <summary>Checks that a request for an unsupported mip level is rejected with an error and leaves the destination untouched.</summary>
        [Test]
        public void RenderRequestForMipLevelLogsErrorWithoutRendering()
        {
            var camera = CreateCamera(ref _cameraA, 0f, Color.blue);
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest
            {
                destination = Target
            });
            camera.backgroundColor = Color.red;
            LogAssert.Expect(LogType.Error, new Regex("two-dimensional destination texture"));
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest
            {
                destination = Target,
                mipLevel = 1
            });
            Assert.That(camera.targetTexture, Is.Null);
            ReadTarget();
            AssertColor(Pixels.GetPixel(16, 16), Color.blue);
        }

        /// <summary>Checks camera depth order, viewport rectangles, and per-camera solid-color clearing.</summary>
        /// <returns>A frame boundary allowing both enabled cameras to render.</returns>
        [UnityTest]
        public IEnumerator RendersCamerasByDepthWithinTheirViewports()
        {
            var front = CreateCamera(ref _cameraB, 11f, Color.green);
            var back = CreateCamera(ref _cameraA, 10f, Color.red);
            front.rect = new Rect(0f, 0f, .5f, 1f);
            front.targetTexture = Target;
            back.targetTexture = Target;
            front.enabled = true;
            back.enabled = true;
            yield return null;
            front.enabled = false;
            back.enabled = false;
            ReadTarget();
            AssertColor(Pixels.GetPixel(4, 16), Color.green);
            AssertColor(Pixels.GetPixel(28, 16), Color.red);
        }

        /// <summary>Checks white and colored hit addition, which adds the tinted source to the destination color.</summary>
        [Test]
        public void HitAdditionAddsTintedSource()
        {
            Renderer.BeginBatch();
            DrawSolid(new Color(.2f, .2f, .2f, 1f), 0);
            Renderer.DrawQuad(White, Size, Size, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 1, 0f, 0f, .5f, new Color(.25f, 0f, 0f, 0f));
            Capture();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.95f, .7f, .7f, 1f));
        }

        /// <summary>Checks particle addition, which adds the source color weighted by its alpha.</summary>
        [Test]
        public void ParticleAdditionAddsAlphaWeightedSource()
        {
            Renderer.BeginBatch();
            DrawSolid(new Color(.2f, .2f, .2f, 1f), 0);
            DrawSolid(new Color(.4f, .2f, .1f, .5f), 2);
            Capture();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.4f, .3f, .25f, 1f));
        }

        /// <summary>Checks premultiplied composition of native offscreen surfaces.</summary>
        [Test]
        public void PremultipliedSurfaceUsesOneMinusSourceAlpha()
        {
            Renderer.BeginBatch();
            DrawSolid(new Color(.2f, .2f, .2f, 1f), 0);
            DrawSolid(new Color(.5f, .25f, 0f, .5f), 3);
            Capture();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.6f, .35f, .1f, 1f));
        }

        /// <summary>Checks imitation-plant grayscale and producer brightness in the normal mode.</summary>
        [Test]
        public void NormalModeAppliesGrayscaleAndProducerBrightness()
        {
            var red = CreateSolidTexture(Color.red);
            var warm = CreateSolidTexture(new Color(.4f, .2f, .1f, 1f));
            try
            {
                Renderer.BeginBatch();
                Renderer.DrawQuad(red, Size / 2f, Size, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0, 1f);
                Renderer.DrawQuad(warm, Size / 2f, Size, new Rect(0f, 0f, 1f, 1f), Matrix4x4.Translate(new Vector3(Size / 2f, 0f, 0f)), Color.white, 0, 0f, .5f);
                Capture();
                AssertColor(Pixels.GetPixel(8, 16), new Color(.299f, .299f, .299f, 1f));
                AssertColor(Pixels.GetPixel(24, 16), new Color(.6f, .3f, .15f, 1f));
            }
            finally
            {
                Object.Destroy(red);
                Object.Destroy(warm);
            }
        }

        /// <summary>Checks that a translucent sprite submitted after an opaque sprite still blends over it.</summary>
        [Test]
        public void LaterTranslucentSpriteBlendsOverOpaqueSprite()
        {
            Renderer.BeginBatch();
            DrawSolid(Color.red, 0);
            DrawSolid(new Color(0f, 1f, 0f, .5f), 0);
            Capture();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.5f, .5f, 0f, 1f));
        }

        /// <summary>Checks that depth written by a later opaque sprite hides an earlier translucent sprite drawn in the later queue.</summary>
        [Test]
        public void OpaqueDepthPreservesPainterOrderAcrossQueues()
        {
            Renderer.BeginBatch();
            DrawSolid(new Color(0f, 0f, 1f, .5f), 0);
            DrawSolid(Color.red, 0);
            DrawSolid(new Color(0f, 1f, 0f, .5f), 0);
            Capture();
            AssertColor(Pixels.GetPixel(16, 16), new Color(.5f, .5f, 0f, 1f));
        }

        /// <summary>Checks that the top-left display clip also limits additive scene sprites.</summary>
        /// <returns>A frame boundary allowing the scene camera to render the clipped sprite.</returns>
        [UnityTest]
        public IEnumerator ClipsAdditiveSceneSprites()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, Screen.width * .5f, Screen.height);
            using (Renderer.BeginFrame())
            {
                Renderer.DrawQuad(White, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, new Color(0f, 1f, 0f, 1f), 2);
            }

            var camera = ObjectSpriteRenderer.SceneCamera;
            camera.targetTexture = Target;
            try
            {
                yield return null;
                ReadTarget();
                AssertColor(Pixels.GetPixel(8, 16), Color.green);
                AssertColor(Pixels.GetPixel(24, 16), Color.black);
            }
            finally
            {
                camera.targetTexture = null;
            }
        }

        /// <summary>Creates a disabled orthographic camera that renders nothing but its clear color.</summary>
        /// <param name="owner">The field receiving the camera object for teardown.</param>
        /// <param name="depth">The camera depth used for ordering.</param>
        /// <param name="background">The solid clear color.</param>
        /// <returns>The configured camera.</returns>
        private static Camera CreateCamera(ref GameObject? owner, float depth, Color background)
        {
            owner = new GameObject("Pipeline test camera");
            var camera = owner.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.cullingMask = 0;
            camera.depth = depth;
            return camera;
        }

        /// <summary>Creates a readable one-pixel texture of a solid color.</summary>
        /// <param name="color">The texel color.</param>
        /// <returns>The texture, owned by the caller.</returns>
        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>Draws a full-target quad of the readable white texture.</summary>
        /// <param name="color">The vertex tint and opacity.</param>
        /// <param name="mode">The sprite blend mode.</param>
        private void DrawSolid(Color color, int mode)
        {
            Renderer.DrawQuad(White, Size, Size, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, color, mode);
        }

        /// <summary>Renders the current batch into the target and reads it back.</summary>
        private void Capture()
        {
            Renderer.RenderTo(Target);
            ReadTarget();
        }

        /// <summary>Reads the target pixels while preserving the active render texture.</summary>
        private void ReadTarget()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = Target;
                Pixels.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
                Pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Compares a GPU result with an expected color within 8-bit storage tolerance.</summary>
        /// <param name="actual">The color read from the GPU.</param>
        /// <param name="expected">The expected color.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.035f), "red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.035f), "green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.035f), "blue");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.035f), "alpha");
        }
    }
}
