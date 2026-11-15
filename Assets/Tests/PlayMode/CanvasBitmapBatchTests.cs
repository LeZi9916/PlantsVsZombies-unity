#nullable enable
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares retained font-page runs with the original per-glyph Canvas submission.</summary>
    public sealed class CanvasBitmapBatchTests
    {
        /// <summary>The font with two translucent pages and overlapping glyph advances.</summary>
        private CanvasBitmapFont _font = null!;
        /// <summary>The pages retained for the independent per-glyph reference draw.</summary>
        private Texture2D[] _pages = null!;
        /// <summary>The shared scene renderer used to exercise ordering boundaries.</summary>
        private ObjectSpriteRenderer _renderer = null!;
        /// <summary>The real graphics target used for pixel comparisons.</summary>
        private RenderTexture _target = null!;
        /// <summary>The reusable readback texture.</summary>
        private Texture2D _pixels = null!;

        /// <summary>Creates a minimal multi-page font with visible overlaps and a zero-ink space.</summary>
        /// <returns>A frame boundary for graphics initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            var data = BmFontParser.Parse("info face=\"Fixture\" size=12 unicode=1\n"
                + "common lineHeight=12 base=10 scaleW=16 scaleH=16 pages=2 packed=0\n"
                + "page id=0 file=\"a.png\"\npage id=1 file=\"b.png\"\nchars count=3\n"
                + "char id=65 x=0 y=0 width=12 height=12 xoffset=0 yoffset=0 xadvance=7 page=0 chnl=15\n"
                + "char id=66 x=0 y=0 width=12 height=12 xoffset=0 yoffset=0 xadvance=7 page=1 chnl=15\n"
                + "char id=32 x=0 y=0 width=0 height=0 xoffset=0 yoffset=0 xadvance=4 page=0 chnl=15\nkernings count=0\n");
            _pages = new[] { Page(new Color(1f, 0f, 0f, .55f)), Page(new Color(0f, 1f, 0f, .7f)) };
            _font = (CanvasBitmapFont)Activator.CreateInstance(typeof(CanvasBitmapFont), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { data, _pages }, null)!;
            _renderer = new ObjectSpriteRenderer();
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
        }

        /// <summary>Releases only fixture-owned UI and graphics resources.</summary>
        /// <returns>A boundary for deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.SceneCamera.targetTexture = null;
            CanvasUI.Dispose();
            _renderer?.Dispose();
            _font?.Dispose();
            if (_target != null)
            {
                _target.Release();
            }
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            yield return null;
        }

        /// <summary>Checks multi-page overlap, transformed clipping and scene/UI interleaving against individual glyphs.</summary>
        /// <returns>Frame boundaries for both reference and retained draws to reach the graphics target.</returns>
        [UnityTest]
        public IEnumerator PageRunsMatchPerGlyphPixelsWithClippingAndInterleaving()
        {
            var layout = _font.Layout("AABBA A");
            for (var variant = 0; variant < 3; variant++)
            {
                Draw(layout, false, variant);
                yield return null;
                yield return null;
                var expected = Capture();
                Draw(layout, true, variant);
                yield return null;
                yield return null;
                var actual = Capture();
                var maximum = 0;
                for (var index = 0; index < expected.Length; index++)
                {
                    maximum = Math.Max(maximum, Math.Abs(expected[index].r - actual[index].r));
                    maximum = Math.Max(maximum, Math.Abs(expected[index].g - actual[index].g));
                    maximum = Math.Max(maximum, Math.Abs(expected[index].b - actual[index].b));
                }
                Assert.That(maximum, Is.LessThanOrEqualTo(1), "Grouping must preserve original page, layer and glyph order.");
            }
            Assert.That(CanvasUI.PooledBitmapRunCount, Is.EqualTo(3));
        }

        /// <summary>Retains the native mesh for unchanged text and releases the run when text disappears.</summary>
        [Test]
        public void UnchangedGlyphRunDoesNotDirtyAndUnusedRunsHide()
        {
            var layout = _font.Layout("AAAA");
            Draw(layout, true, 0);
            Canvas.ForceUpdateCanvases();
            var graphics = Resources.FindObjectsOfTypeAll<CanvasBitmapGraphic>();
            Assert.That(graphics.Length, Is.EqualTo(1));
            var dirty = 0;
            graphics[0].RegisterDirtyVerticesCallback(() => dirty++);
            Draw(layout, true, 0);
            Assert.That(dirty, Is.Zero);
            Draw(layout, true, 1);
            Assert.That(dirty, Is.GreaterThan(0));
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.EndFrame();
            Assert.That(graphics[0].gameObject.activeSelf, Is.False);
        }

        /// <summary>Keeps each clipped Canvas mesh below the portable index limit for very long text.</summary>
        [Test]
        public void LongSamePageTextUsesBoundedRuns()
        {
            Draw(_font.Layout(new string('A', 2500)), true, 0);
            Assert.That(CanvasUI.PooledBitmapRunCount, Is.EqualTo(3));
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Draws identical background, text and scene overlays through the requested submission path.</summary>
        /// <param name="layout">The immutable text layout.</param>
        /// <param name="batched">Whether to use retained page runs.</param>
        /// <param name="variant">The transform and opacity variant.</param>
        private void Draw(BitmapTextLayout layout, bool batched, int variant)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.black;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(31.25f, 30.5f), Quaternion.Euler(0f, 0f, variant * 7f),
                new Vector3(1f + variant * .25f, 1f + variant * .1f, 1f));
            CanvasUI.BeginGroup(new Rect(2f, 1f, 48f, 18f));
            CanvasUI.Color = new Color(.8f, .7f, 1f, .9f - variant * .2f);
            if (batched)
            {
                _font.Draw(layout, 0f, 0f, Color.white);
            }
            else
            {
                for (var index = 0; index < layout.Glyphs.Count; index++)
                {
                    var placed = layout.Glyphs[index];
                    var glyph = placed.Glyph;
                    if (glyph.Width == 0 || glyph.Height == 0)
                    {
                        continue;
                    }
                    var uv = new Rect((float)glyph.X / 16, 1f - (float)(glyph.Y + glyph.Height) / 16,
                        (float)glyph.Width / 16, (float)glyph.Height / 16);
                    CanvasUI.DrawTextureWithTexCoords(new Rect(placed.X, placed.Y, placed.Width, placed.Height), _pages[glyph.Page], uv);
                }
            }
            CanvasUI.EndGroup();
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(Texture2D.whiteTexture, 4f, 5f, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.Translate(new Vector3(48f, 34f)), Color.blue, 0);
            }
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.yellow;
            CanvasUI.DrawTexture(new Rect(49f, 35f, 2f, 2f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
        }

        /// <summary>Reads the completed screen target while restoring the prior active target.</summary>
        /// <returns>The rendered pixel array.</returns>
        private Color32[] Capture()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                _pixels.Apply();
                return _pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Creates a synthetic translucent font page.</summary>
        /// <param name="color">The original page pixel color.</param>
        /// <returns>A texture owned and released by the fixture font.</returns>
        private static Texture2D Page(Color color)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[256];
            Array.Fill(pixels, color);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
