#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks gallery-style frozen captures and live scene composites reuse complete multi-part meshes safely.</summary>
    public sealed class SpriteCaptureBatchReuseTests
    {
        /// <summary>The primary colors stored in four independent source atlas cells.</summary>
        private static readonly Color[] s_palette = { Color.red, Color.green, Color.blue, Color.yellow };
        /// <summary>The unreadable synthetic source atlas shared by every part and capture.</summary>
        private Texture2D _atlas = null!;
        /// <summary>The actual native mesh pool reused by sequential portrait captures.</summary>
        private ObjectSpriteRenderer _capture = null!;
        /// <summary>The scene mesh pool displaying all retained portrait surfaces together.</summary>
        private ObjectSpriteRenderer _scene = null!;
        /// <summary>The independent frozen surfaces retained while the same capture meshes are rewritten.</summary>
        private readonly List<RenderTexture> _portraits = new List<RenderTexture>();
        /// <summary>The scene destination receiving the gallery composite.</summary>
        private RenderTexture _sceneTarget = null!;
        /// <summary>The readable capture inspection texture.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The readable scene inspection texture.</summary>
        private Texture2D _scenePixels = null!;
        /// <summary>The shared production scene camera.</summary>
        private Camera _camera = null!;
        /// <summary>The camera destination restored after this fixture.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>The camera clear color restored after this fixture.</summary>
        private Color _previousBackground;
        /// <summary>The SRP Batcher configuration restored after this fixture.</summary>
        private bool _previousBatcher;

        /// <summary>Creates unreadable atlas data and independent targets after graphics initialization.</summary>
        /// <returns>A frame allowing the production render pipeline to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _previousBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            _atlas = new Texture2D(8, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    _atlas.SetPixel(x, y, s_palette[x / 2]);
                }
            }
            _atlas.Apply(false, true);
            _capture = new ObjectSpriteRenderer();
            _scene = new ObjectSpriteRenderer();
            for (var index = 0; index < 8; index++)
            {
                var target = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                target.Create();
                _portraits.Add(target);
            }
            _sceneTarget = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _sceneTarget.Create();
            _pixels = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            _scenePixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false, true);
            _camera = ObjectSpriteRenderer.SceneCamera;
            _previousTarget = _camera.targetTexture;
            _previousBackground = _camera.backgroundColor;
            _camera.targetTexture = _sceneTarget;
            _camera.backgroundColor = Color.clear;
        }

        /// <summary>Releases the fixture's renderer pools, GPU surfaces, and source data while restoring shared state.</summary>
        /// <returns>A frame allowing deferred Unity destruction to complete.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = _previousBatcher;
            if (_camera != null)
            {
                _camera.targetTexture = _previousTarget;
                _camera.backgroundColor = _previousBackground;
            }
            _capture.Dispose();
            _scene.Dispose();
            foreach (var target in _portraits)
            {
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
            _portraits.Clear();
            _sceneTarget.Release();
            UnityEngine.Object.Destroy(_sceneTarget);
            UnityEngine.Object.Destroy(_atlas);
            UnityEngine.Object.Destroy(_pixels);
            UnityEngine.Object.Destroy(_scenePixels);
            yield return null;
        }

        /// <summary>Checks rewriting one mesh pool for eight portraits preserves every frozen surface and its scene composite.</summary>
        /// <param name="batcher">Whether the engine's SRP Batcher is active during every actual camera request.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SameFramePortraitsPreserveIndependentPartsAndCameraMatrices(bool batcher)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = batcher;
            for (var index = 0; index < _portraits.Count; index++)
            {
                CapturePortrait(index, index);
            }
            // Delay all readbacks until every capture has reused the original mesh and material.
            for (var index = 0; index < _portraits.Count; index++)
            {
                AssertPortrait(_portraits[index], index);
            }
            PresentGallery(0);
        }

        /// <summary>Checks live preview updates and frozen thumbnails remain independent after crossing real frame boundaries.</summary>
        /// <returns>Frames between repeated complete capture and presentation passes.</returns>
        [UnityTest]
        public IEnumerator CrossFramePortraitReuseKeepsAllVertexStreamsAndPremultipliedSurfaces()
        {
            for (var frame = 0; frame < 3; frame++)
            {
                var firstVariant = frame * _portraits.Count;
                for (var index = 0; index < _portraits.Count; index++)
                {
                    CapturePortrait(index, firstVariant + index);
                }
                yield return null;
                for (var index = 0; index < _portraits.Count; index++)
                {
                    AssertPortrait(_portraits[index], firstVariant + index);
                }
                PresentGallery(firstVariant);
                yield return null;
            }
        }

        /// <summary>Checks adjacent eight-pixel-grid pieces share exact geometry edges and match the original unsplit translucent source.</summary>
        /// <param name="reversed">Whether both requested source UV axes are inverted.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void TightPieceSharedEdgesMatchOriginalSkewedCaptureWithoutRepeatedAlpha(bool reversed)
        {
            var source = new Texture2D(13, 37, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var colors = new Color32[13 * 37];
            for (var index = 0; index < colors.Length; index++)
            {
                colors[index] = new Color32(51, 102, 153, 128);
            }
            source.SetPixels32(colors);
            source.Apply(false, true);
            var metadata = new SpriteAtlasImage
            {
                SourceIdentity = "images/thin-pole.png",
                Width = 13,
                Height = 37,
                VisiblePixels = new[] { new Rect(0f, 0f, 13f, 8f), new Rect(0f, 8f, 13f, 16f), new Rect(0f, 24f, 13f, 13f) },
                Tiles = new[] { new SpriteAtlasTile { Page = 0, PageWidth = 13, PageHeight = 37,
                    SourcePixels = new Rect(0f, 0f, 13f, 37f), TextureUv = new Rect(0f, 0f, 1f, 1f) } }
            };
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[] { new SpriteAtlasLayout { MaximumPageSize = 256,
                    Pages = new[] { "thin-pole-page" }, Images = new[] { metadata } } }
            };
            try
            {
                using var provider = new SpriteAssetProvider(catalog, 256, _ => source,
                    _ =>
                    {
                    });
                var image = provider.LoadImage(metadata.SourceIdentity);
                var uv = reversed ? new Rect(1f, 1f, -1f, -1f) : new Rect(0f, 0f, 1f, 1f);
                var matrix = Matrix4x4.identity;
                matrix.m01 = .25f;
                matrix.m03 = 2f;
                matrix.m13 = -.5f;
                var clip = new Rect(0f, 0f, 32f, 32f);
                _capture.BeginBatch();
                _capture.DrawQuad(source, 13f, 37f, uv, matrix, Color.white, 0, clipOverride: clip);
                _capture.RenderTo(_portraits[0]);
                Read(_portraits[0], _pixels);
                var expected = _pixels.GetPixels();
                _capture.BeginBatch();
                _capture.DrawQuad(image, 13f, 37f, uv, matrix, Color.white, 0, clipOverride: clip);
                _capture.RenderTo(_portraits[1]);
                Assert.That(_capture.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_capture.BatchCount, Is.EqualTo(1));
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var batches = (List<SpriteMeshBatch>)typeof(ObjectSpriteRenderer).GetField("_batches", flags)!.GetValue(_capture)!;
                var boundary = reversed ? -23.5f : -12.5f;
                var sharedVertices = 0;
                foreach (var vertex in batches[0].Mesh.vertices)
                {
                    if (Mathf.Abs(vertex.y - boundary) < .001f)
                    {
                        Assert.That(vertex.y, Is.EqualTo(boundary), "Neighboring pieces must preserve the same exact source-pixel edge.");
                        sharedVertices++;
                    }
                }
                Assert.That(sharedVertices, Is.GreaterThanOrEqualTo(4));
                var tightVertices = _capture.LastFrameStatistics.VertexCount;
                using var native = new ObjectSpriteRenderer(new TextureOpacityCache(), false);
                native.BeginBatch();
                native.DrawQuad(image, 13f, 37f, uv, matrix, Color.white, 0, clipOverride: clip);
                native.RenderTo(_portraits[2]);
                Assert.That(native.BatchCount, Is.EqualTo(1));
                Assert.That(native.LastFrameStatistics.VertexCount, Is.LessThan(tightVertices),
                    "Native composition must retain one tile quad while ordinary source rendering keeps its tight pieces.");
                for (var target = 1; target <= 2; target++)
                {
                    Read(_portraits[target], _pixels);
                    var actual = _pixels.GetPixels();
                    for (var index = 0; index < expected.Length; index++)
                    {
                        Assert.That(actual[index].r, Is.EqualTo(expected[index].r).Within(.035f));
                        Assert.That(actual[index].g, Is.EqualTo(expected[index].g).Within(.035f));
                        Assert.That(actual[index].b, Is.EqualTo(expected[index].b).Within(.035f));
                        Assert.That(actual[index].a, Is.EqualTo(expected[index].a).Within(.035f));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>Submits a small multi-part pose with a changing vertex count to the reused native capture pool.</summary>
        /// <param name="target">The frozen portrait slot receiving this pose.</param>
        /// <param name="variant">The independent color cycle and part-count identity.</param>
        private void CapturePortrait(int target, int variant)
        {
            _capture.BeginBatch();
            DrawPart(variant, new Rect(2f, 2f, 12f, 10f), 1f);
            DrawPart(variant + 1, new Rect(4f, 13f, 16f, 10f), variant % 2 == 0 ? 1f : .5f);
            DrawPart(variant + 2, new Rect(22f, 4f, 8f, 20f), 1f);
            for (var part = 0; part < variant % 6; part++)
            {
                DrawPart(variant + part, new Rect(1f + part * 5f, 26f, 3f, 3f), .5f);
            }
            _capture.RenderTo(_portraits[target]);
            Assert.That(_capture.BatchCount, Is.EqualTo(1), "All pose parts must actually share one combined GPU mesh.");
        }

        /// <summary>Appends one independently sampled atlas cell with its own geometry and alpha.</summary>
        /// <param name="color">The source cell selected by this part.</param>
        /// <param name="rectangle">The local destination bounds in capture pixels.</param>
        /// <param name="alpha">The source alpha used by the actual shader.</param>
        private void DrawPart(int color, Rect rectangle, float alpha)
        {
            _capture.DrawQuad(_atlas, rectangle.width, rectangle.height, new Rect((color % 4) * .25f, 0f, .25f, 1f),
                Matrix4x4.Translate(new Vector3(rectangle.x, rectangle.y, 0f)), new Color(1f, 1f, 1f, alpha), 0,
                clipOverride: new Rect(0f, 0f, 32f, 32f));
        }

        /// <summary>Checks independent body-part samples after every later portrait has overwritten the shared CPU and GPU mesh buffers.</summary>
        /// <param name="target">The previously captured frozen surface.</param>
        /// <param name="variant">The expected original pose identity.</param>
        private void AssertPortrait(RenderTexture target, int variant)
        {
            Read(target, _pixels);
            AssertColor(_pixels.GetPixel(8, 24), s_palette[variant % 4], 1f);
            AssertColor(_pixels.GetPixel(12, 13), s_palette[(variant + 1) % 4], variant % 2 == 0 ? 1f : .5f);
            AssertColor(_pixels.GetPixel(26, 17), s_palette[(variant + 2) % 4], 1f);
        }

        /// <summary>Switches back to the real scene camera and checks every retained premultiplied portrait together.</summary>
        /// <param name="firstVariant">The pose identity assigned to the first portrait.</param>
        private void PresentGallery(int firstVariant)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            using (_scene.BeginFrame())
            {
                for (var index = 0; index < _portraits.Count; index++)
                {
                    _scene.DrawQuad(_portraits[index], 32f, 32f, new Rect(0f, 0f, 1f, 1f),
                        Matrix4x4.Translate(new Vector3(index * 32f, 0f, 0f)), Color.white, 3);
                }
            }
            RenderPipeline.SubmitRenderRequest(_camera, new RenderPipeline.StandardRequest { destination = _sceneTarget });
            Read(_sceneTarget, _scenePixels);
            for (var index = 0; index < _portraits.Count; index++)
            {
                var variant = firstVariant + index;
                AssertColor(_scenePixels.GetPixel(index * 32 + 8, _sceneTarget.height - 8), s_palette[variant % 4], 1f);
                AssertColor(_scenePixels.GetPixel(index * 32 + 12, _sceneTarget.height - 19), s_palette[(variant + 1) % 4], variant % 2 == 0 ? 1f : .5f);
            }
        }

        /// <summary>Reads a completed target without changing the caller's active render texture.</summary>
        /// <param name="target">The completed camera destination.</param>
        /// <param name="pixels">The owned readable inspection texture.</param>
        private static void Read(RenderTexture target, Texture2D pixels)
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Checks both premultiplied color and alpha using the renderer fixture's existing pixel tolerance.</summary>
        /// <param name="actual">The pixel stored by the actual GPU capture or scene composite.</param>
        /// <param name="source">The original unpremultiplied source color.</param>
        /// <param name="alpha">The independent original source opacity.</param>
        private static void AssertColor(Color actual, Color source, float alpha)
        {
            Assert.That(actual.r, Is.EqualTo(source.r * alpha).Within(.035f));
            Assert.That(actual.g, Is.EqualTo(source.g * alpha).Within(.035f));
            Assert.That(actual.b, Is.EqualTo(source.b * alpha).Within(.035f));
            Assert.That(actual.a, Is.EqualTo(alpha).Within(.035f));
        }
    }
}
