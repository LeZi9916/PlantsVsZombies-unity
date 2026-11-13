#nullable enable
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects grouped multiline rich descriptions independently of portrait or source-data tests.</summary>
    public sealed class CanvasGroupedTextGraphicsTests
    {
        /// <summary>The rich paragraph with the same leading newline and color-tag structure as almanac descriptions.</summary>
        private const string Description = "\n<color=#28325A>属性：防御中等\n范围：前方区域</color>\n\n"
            + "<color=#624C42>第一段说明保留换行。\n第二段说明显示文字。\n第三段说明测试裁剪。\n第四段说明用于检查。</color>";
        /// <summary>The shared scene camera used by the actual Canvas output.</summary>
        private Camera _camera = null!;
        /// <summary>The destination restored after this fixture.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>The actual GPU destination receiving both labels.</summary>
        private RenderTexture _target = null!;
        /// <summary>The readable GPU output.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The independently owned dynamic font with the same fallback list as the almanac.</summary>
        private Font _font = null!;

        /// <summary>Creates a fresh dynamic font and real camera target after graphics initialization.</summary>
        /// <returns>A frame allowing the production pipeline to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            _font = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial"
            }, 16);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _camera = ObjectSpriteRenderer.SceneCamera;
            _previousTarget = _camera.targetTexture;
            _camera.targetTexture = _target;
        }

        /// <summary>Releases only this fixture's Canvas, font, and GPU resources.</summary>
        /// <returns>A frame allowing deferred font and Canvas destruction to complete.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _camera.targetTexture = _previousTarget;
            CanvasUI.Dispose();
            _target.Release();
            Object.Destroy(_target);
            Object.Destroy(_pixels);
            Object.Destroy(_font);
            yield return null;
        }

        /// <summary>Checks grouped rich paragraphs remain visible and match an absolute label before and after font atlas rebuilding.</summary>
        /// <returns>Real Canvas frames before and after glyph-cache growth.</returns>
        [UnityTest]
        public IEnumerator GroupedRichDescriptionMatchesAbsoluteLabelAfterDynamicFontRebuild()
        {
            var style = new GUIStyle
            {
                font = _font,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                clipping = TextClipping.Clip,
                padding = new RectOffset(),
                margin = new RectOffset()
            };
            style.normal.textColor = Color.black;
            const float height = 260f;
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.white;
            CanvasUI.ContentColor = Color.white;
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            CanvasUI.Label(new Rect(30f, 20f, 250f, 35f), "HEADER 描述检查", style);
            CanvasUI.BeginGroup(new Rect(30f, 80f, 250f, 180f));
            CanvasUI.Label(new Rect(0f, 0f, 250f, height), Description, style);
            CanvasUI.EndGroup();
            CanvasUI.Label(new Rect(320f, 80f, 250f, height), Description, style);
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            AssertParagraphPixels();
            _font.RequestCharactersInTexture("Font atlas growth 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz", 72, FontStyle.Bold);
            yield return null;
            yield return null;
            AssertParagraphPixels();
        }

        /// <summary>Checks the full paragraph rather than only its header or generated-character count.</summary>
        private void AssertParagraphPixels()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
            var grouped = 0;
            var absolute = 0;
            var lowerParagraph = 0;
            for (var y = 0; y < 180; y++)
            {
                for (var x = 0; x < 250; x++)
                {
                    var first = _pixels.GetPixel(30 + x, _target.height - 81 - y);
                    var second = _pixels.GetPixel(320 + x, _target.height - 81 - y);
                    if (first.r + first.g + first.b < 2.5f)
                    {
                        grouped++;
                        if (y > 60)
                        {
                            lowerParagraph++;
                        }
                    }
                    if (second.r + second.g + second.b < 2.5f)
                    {
                        absolute++;
                    }
                    Assert.That(first.r, Is.EqualTo(second.r).Within(.035f), "Grouped and absolute paragraph red samples must agree.");
                    Assert.That(first.g, Is.EqualTo(second.g).Within(.035f), "Grouped and absolute paragraph green samples must agree.");
                    Assert.That(first.b, Is.EqualTo(second.b).Within(.035f), "Grouped and absolute paragraph blue samples must agree.");
                }
            }
            Assert.That(absolute, Is.GreaterThan(100), "The control paragraph must render its rich multiline glyphs.");
            Assert.That(grouped, Is.GreaterThan(100), "The clipped group must retain the same paragraph.");
            Assert.That(lowerParagraph, Is.GreaterThan(20), "Body visibility must cover the detailed text, not only its first short line.");
        }
    }
}
