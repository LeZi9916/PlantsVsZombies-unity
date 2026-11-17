#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks immutable UI capture reuse against source changes, actor contexts and surface lifetime transitions.</summary>
    public sealed class NativeStaticCaptureTests
    {
        /// <summary>The isolated resource root retained by the production renderer.</summary>
        private string _root = string.Empty;
        /// <summary>The production capture renderer under test.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>The opaque red source used to identify stale captured pixels.</summary>
        private Texture2D _red = null!;
        /// <summary>The opaque green source used for content transitions.</summary>
        private Texture2D _green = null!;
        /// <summary>The real render target receiving completed scene composites.</summary>
        private RenderTexture _target = null!;
        /// <summary>The reusable screen readback texture.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The source currently consumed by the retained synchronous callback.</summary>
        private Texture2D _source = null!;
        /// <summary>The bounds currently consumed by the retained synchronous callback.</summary>
        private Rect _destination;
        /// <summary>The number of source callbacks actually executed.</summary>
        private int _callbacks;
        /// <summary>The common source/capture bounds for ordinary lifecycle checks.</summary>
        private static readonly Rect s_viewport = new Rect(4f, 4f, 32f, 32f);

        /// <summary>Creates opaque sources and a real target after graphics initialization.</summary>
        /// <returns>A frame boundary before allocating graphics resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZStaticCapture-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _renderer = new ReanimationRenderer(_root);
            _red = Source(Color.red);
            _green = Source(Color.green);
            _source = _red;
            _target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            _callbacks = 0;
        }

        /// <summary>Releases only fixture-owned resources and its isolated directory.</summary>
        /// <returns>A boundary for deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer.Dispose();
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            UnityEngine.Object.Destroy(_red);
            UnityEngine.Object.Destroy(_green);
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Keeps crop live while content revisions and grayscale each rebuild otherwise identical source pixels.</summary>
        [Test]
        public void ContentRevisionAndGrayscaleInvalidateWhileCropRemainsLive()
        {
            Present(_red, 1);
            AssertColor(Pixel(8, 8), Color.red);
            Present(_green, 1, topCrop: 8f);
            Assert.That(_callbacks, Is.EqualTo(1), "Changing only the composite crop must retain the original complete capture.");
            AssertColor(Pixel(8, 8), Color.red);
            AssertColor(Pixel(8, 20), Color.black);
            Present(_green, 2);
            Assert.That(_callbacks, Is.EqualTo(2));
            AssertColor(Pixel(8, 20), Color.green);
            Present(_green, 2, grayscale: true);
            Assert.That(_callbacks, Is.EqualTo(3));
            var gray = Pixel(8, 8);
            Assert.That(gray.r, Is.GreaterThan(.1f));
            Assert.That(gray.r, Is.EqualTo(gray.g).Within(.02f));
            Assert.That(gray.g, Is.EqualTo(gray.b).Within(.02f));
            Present(_green, 2);
            Assert.That(_callbacks, Is.EqualTo(4));
            AssertColor(Pixel(8, 8), Color.green);
        }

        /// <summary>Recreates lost native attachments and regenerates their pixels even when the content revision is unchanged.</summary>
        [Test]
        public void ReleasedRenderTextureIsRecreatedAndRecaptured()
        {
            Present(_red, 1);
            var surfaces = (IDictionary)typeof(ReanimationRenderer).GetField("_nativeSurfaces", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
            var surface = surfaces[PlantsVsZombies.Rendering.PresentationIdentity.FromExternal("static")]!;
            var texture = (RenderTexture)surface.GetType().GetField("Texture")!.GetValue(surface)!;
            texture.Release();
            Assert.That(texture.IsCreated(), Is.False);
            Present(_green, 1);
            Assert.That(texture.IsCreated(), Is.True);
            Assert.That(_callbacks, Is.EqualTo(2));
            AssertColor(Pixel(8, 8), Color.green);
        }

        /// <summary>Rejects a formerly static signature after the generic capture path overwrites the shared key.</summary>
        [Test]
        public void GenericOverwriteCannotReuseTheFormerStaticSignature()
        {
            Present(_red, 7);
            Begin();
            Configure(_green, s_viewport);
            Assert.That(_renderer.DrawNativeClipped("static", s_viewport, DrawSource), Is.True);
            EndAndCapture();
            AssertColor(Pixel(8, 8), Color.green);
            Present(_red, 7);
            Assert.That(_callbacks, Is.EqualTo(3));
            AssertColor(Pixel(8, 8), Color.red);
        }

        /// <summary>Uses the live path inside actor scopes and observes a same-frame source-node visibility change.</summary>
        [Test]
        public void ActorContextRedrawsAndHonorsSameFrameVisibility()
        {
            Present(_red, 1);
            Begin();
            var hierarchy = _renderer.Hierarchy;
            GameObject? sourceNode = null;
            Func<bool> drawActor = () =>
            {
                _callbacks++;
                using var source = hierarchy.BeginNamedSprite("live-source");
                sourceNode = SpriteHierarchy.CurrentSpriteObject;
                return _renderer.DrawNativeImage(_green, s_viewport, new Rect(0f, 0f, 1f, 1f));
            };
            using (hierarchy.BeginActor("Plants", 1, "Live source"))
            {
                Assert.That(_renderer.DrawNativeCachedClipped("static", s_viewport, drawActor, 1), Is.True);
                Capture();
                AssertColor(Pixel(8, 8), Color.green);
                Assert.That(sourceNode, Is.Not.Null);
                sourceNode!.SetActive(false);
                Assert.That(_renderer.DrawNativeCachedClipped("static", s_viewport, drawActor, 1), Is.True);
            }
            EndAndCapture();
            Assert.That(_callbacks, Is.EqualTo(3), "Both actor draws must bypass the former immutable UI content.");
            AssertColor(Pixel(8, 8), Color.black);
            Assert.That(sourceNode!.activeSelf, Is.False);
        }

        /// <summary>Preserves already submitted pixels on same-frame size conflicts and accepts a resized capture next frame.</summary>
        [Test]
        public void SizeChangesFailWithinTheFrameAndRecoverAcrossFrames()
        {
            var small = new Rect(4f, 4f, 12f, 12f);
            var large = new Rect(4f, 4f, 32f, 32f);
            Begin();
            Configure(_red, small);
            Assert.That(_renderer.DrawNativeCachedClipped("static", small, DrawSource, 1), Is.True);
            Configure(_green, large);
            Assert.That(_renderer.DrawNativeCachedClipped("static", large, DrawSource, 1), Is.False);
            Assert.That(_callbacks, Is.EqualTo(1));
            Assert.That(_renderer.LastError, Does.Contain("same frame"));
            EndAndCapture();
            AssertColor(Pixel(8, 8), Color.red);
            AssertColor(Pixel(24, 24), Color.black);
            Begin();
            Configure(_green, large);
            Assert.That(_renderer.DrawNativeCachedClipped("static", large, DrawSource, 1), Is.True);
            EndAndCapture();
            Assert.That(_callbacks, Is.EqualTo(2));
            Assert.That(_renderer.NativeSurfaceCount, Is.EqualTo(1));
            AssertColor(Pixel(24, 24), Color.green);
        }

        /// <summary>Draws and reads one complete static presentation using the current source signature.</summary>
        /// <param name="source">The source consumed only if recapture is necessary.</param>
        /// <param name="contentVersion">The immutable-content revision.</param>
        /// <param name="topCrop">The live visible capture height.</param>
        /// <param name="grayscale">Whether this capture must grayscale the source.</param>
        private void Present(Texture2D source, long contentVersion, float topCrop = -1f, bool grayscale = false)
        {
            Begin();
            Configure(source, s_viewport);
            Assert.That(_renderer.DrawNativeCachedClipped("static", s_viewport, DrawSource, contentVersion, topCrop, grayscale), Is.True);
            EndAndCapture();
        }

        /// <summary>Begins a fresh scene and source-renderer frame without changing captured resource ownership.</summary>
        private void Begin()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.Hierarchy.BeginFrame(this);
            _renderer.BeginObjectFrame();
        }

        /// <summary>Configures the source callback consumed synchronously by the production capture path.</summary>
        /// <param name="source">The source image to submit when the capture is rebuilt.</param>
        /// <param name="destination">The source-space destination rectangle.</param>
        private void Configure(Texture2D source, Rect destination)
        {
            _source = source;
            _destination = destination;
        }

        /// <summary>Submits the current source while counting actual capture callback execution.</summary>
        /// <returns>Whether visible source geometry was submitted.</returns>
        private bool DrawSource()
        {
            _callbacks++;
            return _renderer.DrawNativeImage(_source, _destination, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Completes the scene frame and reads its actual native pixels.</summary>
        private void EndAndCapture()
        {
            _renderer.EndObjectFrame();
            Capture();
        }

        /// <summary>Renders the current scene pool through the real graphics device and preserves the active target.</summary>
        private void Capture()
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
            pool.RenderTo(_target);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Reads a scene pixel using the game's top-left logical coordinate convention.</summary>
        /// <param name="x">The target horizontal coordinate.</param>
        /// <param name="y">The target downward coordinate.</param>
        /// <returns>The rendered source color.</returns>
        private Color Pixel(int x, int y)
        {
            return _pixels.GetPixel(x, _pixels.height - y - 1);
        }

        /// <summary>Checks source RGB without depending on the target camera's background alpha.</summary>
        /// <param name="actual">The captured scene pixel.</param>
        /// <param name="expected">The expected source RGB.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.03f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.03f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.03f));
        }

        /// <summary>Creates an opaque fixture source retained until teardown.</summary>
        /// <param name="color">The source pixel color.</param>
        /// <returns>The source texture owned by this test fixture.</returns>
        private static Texture2D Source(Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return texture;
        }
    }
}
