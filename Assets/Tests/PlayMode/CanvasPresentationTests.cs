#nullable enable
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects native Canvas clipping, painter order, editable fields, and pool lifetimes.</summary>
    public sealed class CanvasPresentationTests
    {
        /// <summary>The scene sprite pool interleaved with native Canvas artwork.</summary>
        private ObjectSpriteRenderer _renderer = null!;
        /// <summary>The same-size target used to inspect direct scene camera output.</summary>
        private RenderTexture _target = null!;
        /// <summary>The texture receiving pixel readback from the test target.</summary>
        private Texture2D _pixels = null!;

        /// <summary>Creates the presentation resources after Unity has initialized its graphics pipeline.</summary>
        /// <returns>A frame boundary before allocating native rendering resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            _renderer = new ObjectSpriteRenderer();
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
        }

        /// <summary>Releases native Canvas, sprite, and readback resources after every test.</summary>
        /// <returns>A frame boundary allowing Unity to destroy the owned scene objects.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.SceneCamera.targetTexture = null;
            CanvasUI.Dispose();
            _renderer.Dispose();
            _target.Release();
            Object.Destroy(_target);
            Object.Destroy(_pixels);
            yield return null;
        }

        /// <summary>Preserves UI backgrounds, scene animations, and foreground interface layering.</summary>
        /// <returns>Frame boundaries allowing the native Canvas and scene camera to publish their geometry.</returns>
        [UnityTest]
        public IEnumerator InterleavesCanvasAndSceneRenderers()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.green, 0);
            }

            CanvasUI.Color = Color.blue;
            CanvasUI.DrawTexture(new Rect(20f, 20f, 40f, 40f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            ReadPixels();
            AssertColor(Pixel(10, 10), Color.green);
            AssertColor(Pixel(40, 40), Color.blue);
        }

        /// <summary>Clips group artwork after applying the original logical-to-screen transform.</summary>
        /// <returns>Frame boundaries allowing the transformed Canvas mesh to render.</returns>
        [UnityTest]
        public IEnumerator AppliesTransformAndNestedClipping()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(20f, 30f), Quaternion.identity, new Vector3(2f, 2f, 1f));
            CanvasUI.BeginGroup(new Rect(5f, 5f, 5f, 5f));
            CanvasUI.Color = Color.blue;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 20f, 20f), Texture2D.whiteTexture);
            CanvasUI.EndGroup();
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            ReadPixels();
            AssertColor(Pixel(25, 35), Color.red);
            AssertColor(Pixel(35, 45), Color.blue);
            AssertColor(Pixel(45, 55), Color.red);
        }

        /// <summary>Preserves the legacy horizontal, vertical, and double-axis reversed-UV texture API.</summary>
        /// <returns>Canvas presentation frames for every signed source rectangle.</returns>
        [UnityTest]
        public IEnumerator ExternalTextureCoordinatesPreserveBothMirrorAxes()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.yellow });
            source.Apply();
            try
            {
                foreach (var flipX in new[] { false, true })
                {
                    foreach (var flipY in new[] { false, true })
                    {
                        yield return AssertCanvasMirror(SpriteImage.FromTexture(source), flipX, flipY);
                    }
                }

                yield return AssertCanvasClamp(SpriteImage.FromTexture(source), false);
                yield return AssertCanvasClamp(SpriteImage.FromTexture(source), true);
            }
            finally
            {
                Object.Destroy(source);
            }
        }

        /// <summary>Preserves reversed UVs when a cropped logical source is split between independent atlas pages.</summary>
        /// <returns>Canvas presentation frames for every mirrored two-page crop.</returns>
        [UnityTest]
        public IEnumerator TiledAtlasCoordinatesPreserveMirroredCropsAcrossPages()
        {
            var left = new Texture2D(1, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var right = new Texture2D(1, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            left.SetPixels(new[] { Color.red, Color.blue });
            right.SetPixels(new[] { Color.green, Color.yellow });
            left.Apply();
            right.Apply();
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[]
                {
                    new SpriteAtlasLayout
                    {
                        MaximumPageSize = 256,
                        Pages = new[] { "left", "right" },
                        Images = new[]
                        {
                            new SpriteAtlasImage
                            {
                                SourceIdentity = "images/mirror.png",
                                Width = 2,
                                Height = 2,
                                VisiblePixels = new[] { new Rect(0f, 0f, 2f, 2f) },
                                Tiles = new[]
                                {
                                    new SpriteAtlasTile { Page = 0, SourcePixels = new Rect(0f, 0f, 1f, 2f),
                                        TextureUv = new Rect(0f, 0f, 1f, 1f), PageWidth = 1, PageHeight = 2 },
                                    new SpriteAtlasTile { Page = 1, SourcePixels = new Rect(1f, 0f, 1f, 2f),
                                        TextureUv = new Rect(0f, 0f, 1f, 1f), PageWidth = 1, PageHeight = 2 }
                                }
                            }
                        }
                    }
                }
            };
            using var provider = new SpriteAssetProvider(catalog, 256, path => path == "left" ? left : right, _ => { });
            try
            {
                var image = provider.LoadImage("images/mirror.png");
                foreach (var flipX in new[] { false, true })
                {
                    foreach (var flipY in new[] { false, true })
                    {
                        yield return AssertCanvasMirror(image, flipX, flipY);
                    }
                }

                yield return AssertCanvasClamp(image, false);
                yield return AssertCanvasClamp(image, true);
            }
            finally
            {
                Object.Destroy(left);
                Object.Destroy(right);
            }
        }

        /// <summary>Preserves overlapping source alpha and local clipping when neutral actors skip capture cameras.</summary>
        /// <returns>Frame boundaries publishing the direct and captured versions of the same synthetic pose.</returns>
        [UnityTest]
        public IEnumerator NeutralZombieDirectDrawMatchesCapturedAlphaAndClipping()
        {
            using var animations = new ReanimationRenderer(System.IO.Path.GetTempPath());
            var zombie = new Zombie { Kind = ZombieKind.Normal, Health = 100f };
            var viewport = new Rect(30f, 30f, 40f, 40f);
            System.Func<bool> draw = () =>
            {
                var color = SpriteDrawState.Color;
                SpriteDrawState.Color = new Color(1f, 0f, 0f, .5f);
                var drawn = animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(20f, 20f, 40f, 40f), new Rect(0f, 0f, 1f, 1f));
                SpriteDrawState.Color = new Color(0f, 1f, 0f, .5f);
                drawn |= animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(40f, 40f, 40f, 40f), new Rect(0f, 0f, 1f, 1f));
                SpriteDrawState.Color = color;
                return drawn;
            };
            ObjectSpriteRenderer.BeginSceneFrame();
            Assert.That(animations.DrawNativeZombieCombat(zombie, viewport, draw), Is.True);
            Assert.That(animations.NativeSurfaceCount, Is.Zero, "Neutral actors must not create a local camera or texture.");
            yield return null;
            yield return null;
            ReadPixels();
            var red = Pixel(35, 35);
            var overlap = Pixel(45, 45);
            var green = Pixel(65, 65);
            AssertColor(Pixel(25, 35), Color.black);
            AssertColor(Pixel(75, 65), Color.black);
            Assert.That(red.r, Is.GreaterThan(.4f));
            Assert.That(overlap.g, Is.GreaterThan(.4f));

            ObjectSpriteRenderer.BeginSceneFrame();
            Assert.That(animations.DrawNativeClipped("captured-comparison", viewport, draw), Is.True);
            Assert.That(animations.NativeSurfaceCount, Is.EqualTo(1));
            yield return null;
            yield return null;
            ReadPixels();
            AssertColor(Pixel(35, 35), red);
            AssertColor(Pixel(45, 45), overlap);
            AssertColor(Pixel(65, 65), green);
            AssertColor(Pixel(25, 35), Color.black);
            AssertColor(Pixel(75, 65), Color.black);
        }

        /// <summary>Renders bundled Chinese bitmap glyphs through Canvas with their original atlas opacity and tint.</summary>
        /// <returns>Frame boundaries allowing the native glyph graphics to reach the camera target.</returns>
        [UnityTest]
        public IEnumerator RendersBundledChineseBitmapGlyphPixels()
        {
            using var font = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft18");
            var layout = font.Layout("中文");
            Assert.That(layout.MissingGlyphs, Is.Empty);
            Assert.That(layout.Glyphs.Count, Is.EqualTo(2));
            var placed = layout.Glyphs[0];
            var page = System.IO.Path.GetFileNameWithoutExtension(font.Data.Pages[placed.Glyph.Page]);
            var bytes = Resources.Load<TextAsset>(CanvasBitmapFont.DefaultResourceRoot + "/Atlases/" + page);
            Assert.That(bytes, Is.Not.Null);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(source, bytes.bytes), Is.True);
                var brightest = Vector2Int.zero;
                var clearest = Vector2Int.zero;
                var strongest = -1f;
                var weakest = float.PositiveInfinity;
                var sampleWidth = Mathf.Min(placed.Glyph.Width, Mathf.FloorToInt(layout.Glyphs[1].X - placed.X));
                for (var y = 0; y < placed.Glyph.Height; y++)
                {
                    for (var x = 0; x < sampleWidth; x++)
                    {
                        var atlas = source.GetPixel(placed.Glyph.X + x, font.Data.AtlasHeight - placed.Glyph.Y - y - 1);
                        var intensity = ExpectedGreen(atlas);
                        if (intensity > strongest)
                        {
                            strongest = intensity;
                            brightest = new Vector2Int(x, y);
                        }

                        if (intensity < weakest)
                        {
                            weakest = intensity;
                            clearest = new Vector2Int(x, y);
                        }
                    }
                }

                Assert.That(strongest, Is.GreaterThan(.5f), "The bundled Chinese glyph must contain visible ink.");
                Assert.That(weakest, Is.LessThan(.05f), "The original glyph atlas must retain its empty border.");
                ObjectSpriteRenderer.BeginSceneFrame();
                CanvasUI.BeginFrame();
                font.Draw(layout, 60f, 60f, Color.green);
                CanvasUI.EndFrame();
                yield return null;
                yield return null;
                ReadPixels();
                var brightPixel = Pixel(Mathf.FloorToInt(60f + placed.X) + brightest.x, Mathf.FloorToInt(60f + placed.Y) + brightest.y);
                var clearPixel = Pixel(Mathf.FloorToInt(60f + placed.X) + clearest.x, Mathf.FloorToInt(60f + placed.Y) + clearest.y);
                AssertColor(brightPixel, new Color(0f, strongest, 0f, 1f));
                AssertColor(clearPixel, new Color(0f, weakest, 0f, 1f));
                Assert.That(CanvasUI.PooledImageCount, Is.EqualTo(2), "Each Chinese glyph must submit its original atlas region.");
            }
            finally
            {
                Object.Destroy(source);
            }
        }

        /// <summary>Calculates the green atlas sample after alpha blending over the black camera background.</summary>
        /// <param name="source">The original stored atlas color and opacity.</param>
        /// <returns>The expected stored green channel for the configured rendering color space.</returns>
        private static float ExpectedGreen(Color source)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                return Mathf.LinearToGammaSpace(Mathf.GammaToLinearSpace(source.g) * source.a);
            }

            return source.g * source.a;
        }

        /// <summary>Reuses artwork graphics and hides geometry omitted from the following frame.</summary>
        /// <returns>Frame boundaries allowing the retired artwork to disappear.</returns>
        [UnityTest]
        public IEnumerator ReusesAndRetiresNativeGraphics()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 60f, 60f), Texture2D.whiteTexture);
            CanvasUI.Color = Color.blue;
            CanvasUI.DrawTexture(new Rect(20f, 20f, 20f, 20f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
            Assert.That(CanvasUI.PooledImageCount, Is.EqualTo(2));

            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.green;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 60f, 60f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            ReadPixels();
            AssertColor(Pixel(30, 30), Color.green);
            Assert.That(CanvasUI.PooledImageCount, Is.EqualTo(2));
            Assert.That(System.Array.FindAll(OwnedGraphics(), item => item.gameObject.activeInHierarchy).Length, Is.EqualTo(1));

            CanvasUI.Dispose();
            yield return null;
            Assert.That(OwnedGraphics(), Is.Empty);
        }

        /// <summary>Retains hierarchy order, text bounds, and mesh state when the visible interface has not changed.</summary>
        [Test]
        public void UnchangedInterfaceDoesNotDirtyRetainedGraphics()
        {
            var style = new GUIStyle
            {
                font = CanvasUI.DefaultFont,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.DrawTexture(new Rect(10f, 10f, 80f, 40f), Texture2D.whiteTexture);
            CanvasUI.Label(new Rect(10f, 10f, 80f, 40f), "Stable", style);
            CanvasUI.DrawTexture(new Rect(100f, 10f, 80f, 40f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
            Canvas.ForceUpdateCanvases();
            var images = OwnedGraphics();
            var label = System.Array.Find(Resources.FindObjectsOfTypeAll<CanvasTextGraphic>(),
                item => item != null && item.transform.root.name == "PvZ Canvas UI");
            Assert.That(label, Is.Not.Null);
            var dirty = 0;
            foreach (var image in images)
            {
                image.RegisterDirtyVerticesCallback(() => dirty++);
                image.RegisterDirtyMaterialCallback(() => dirty++);
                image.RegisterDirtyLayoutCallback(() => dirty++);
            }

            label!.RegisterDirtyVerticesCallback(() => dirty++);
            label.RegisterDirtyMaterialCallback(() => dirty++);
            label.RegisterDirtyLayoutCallback(() => dirty++);
            var firstImage = images[0].transform.GetSiblingIndex();
            var secondImage = images[1].transform.GetSiblingIndex();
            var textIndex = label.transform.GetSiblingIndex();
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.DrawTexture(new Rect(10f, 10f, 80f, 40f), Texture2D.whiteTexture);
            CanvasUI.Label(new Rect(10f, 10f, 80f, 40f), "Stable", style);
            CanvasUI.DrawTexture(new Rect(100f, 10f, 80f, 40f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
            Assert.That(dirty, Is.Zero, "An unchanged presentation must preserve the retained Canvas meshes and layout.");
            Assert.That(images[0].transform.GetSiblingIndex(), Is.EqualTo(firstImage));
            Assert.That(images[1].transform.GetSiblingIndex(), Is.EqualTo(secondImage));
            Assert.That(label.transform.GetSiblingIndex(), Is.EqualTo(textIndex));
        }

        /// <summary>Retains native edit state and keyboard focus across presentation traversals.</summary>
        /// <returns>Frame boundaries allowing the native InputField to acquire focus.</returns>
        [UnityTest]
        public IEnumerator PreservesNativeEditableFieldAndFocus()
        {
            var style = new GUIStyle
            {
                font = CanvasUI.DefaultFont,
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset()
            };
            style.normal.textColor = Color.white;
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.SetNextControlName("profile-name");
            Assert.That(CanvasUI.TextField(new Rect(10f, 10f, 200f, 40f), "Initial", 30, style), Is.EqualTo("Initial"));
            CanvasUI.FocusControl("profile-name");
            CanvasUI.EndFrame();
            yield return null;
            var field = ActiveField()!;
            Assert.That(field, Is.Not.Null);
            Assert.That(field.isFocused, Is.True);
            field.text = "Edited";

            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.SetNextControlName("profile-name");
            Assert.That(CanvasUI.TextField(new Rect(10f, 10f, 200f, 40f), "Initial", 30, style), Is.EqualTo("Edited"));
            CanvasUI.EndFrame();
            Assert.That(ActiveField(), Is.SameAs(field));
            Assert.That(field.isFocused, Is.True);

            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.SetNextControlName("profile-name");
            Assert.That(CanvasUI.TextField(new Rect(10f, 10f, 200f, 40f), "Programmatic replacement", 30, style), Is.EqualTo("Programmatic replacement"));
            CanvasUI.EndFrame();
            Assert.That(field.text, Is.EqualTo("Programmatic replacement"));
            Assert.That(ActiveField(), Is.SameAs(field));
            Assert.That(field.isFocused, Is.True);
        }

        /// <summary>Finds retained artwork including objects deliberately hidden from Unity's default object search.</summary>
        /// <returns>The artwork owned by the current native Canvas root.</returns>
        private static CanvasTextureGraphic[] OwnedGraphics()
        {
            return System.Array.FindAll(Resources.FindObjectsOfTypeAll<CanvasTextureGraphic>(),
                item => item != null && item.transform.root.name == "PvZ Canvas UI");
        }

        /// <summary>Finds the active owned native field despite its runtime-only hide flags.</summary>
        /// <returns>The active native editable field, or null if no field was presented.</returns>
        private static InputField? ActiveField()
        {
            return System.Array.Find(Resources.FindObjectsOfTypeAll<InputField>(),
                item => item != null && item.gameObject.activeInHierarchy && item.transform.root.name == "PvZ Canvas UI");
        }

        /// <summary>Checks the four original source quadrants after signed source-to-destination mapping.</summary>
        /// <param name="image">The external or tiled logical source image.</param>
        /// <param name="flipX">Whether its horizontal source axis is reversed.</param>
        /// <param name="flipY">Whether its vertical source axis is reversed.</param>
        /// <returns>Frames allowing the retained Canvas to publish and render the crop.</returns>
        private IEnumerator AssertCanvasMirror(SpriteImage image, bool flipX, bool flipY)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.white;
            var source = new Rect(flipX ? .75f : .25f, flipY ? .75f : .25f, flipX ? -.5f : .5f, flipY ? -.5f : .5f);
            CanvasUI.DrawTextureWithTexCoords(new Rect(20f, 20f, 80f, 80f), image, source);
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            ReadPixels();
            var colors = new[] { Color.red, Color.green, Color.blue, Color.yellow };
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    var sourceX = flipX ? 1 - column : column;
                    var sourceY = flipY ? row : 1 - row;
                    AssertColor(Pixel(40 + column * 40, 40 + row * 40), colors[sourceY * 2 + sourceX]);
                }
            }
        }

        /// <summary>Checks interior, edge, and corner zones preserve Clamp sampling outside the original image bounds.</summary>
        /// <param name="image">The external or tiled image whose edge pixels must extend.</param>
        /// <param name="reverse">Whether both requested UV axes are reversed.</param>
        /// <returns>Frames allowing the complete retained Canvas geometry to render.</returns>
        private IEnumerator AssertCanvasClamp(SpriteImage image, bool reverse)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.white;
            var source = reverse ? new Rect(1.5f, 1.5f, -2f, -2f) : new Rect(-.5f, -.5f, 2f, 2f);
            CanvasUI.DrawTextureWithTexCoords(new Rect(20f, 20f, 80f, 80f), image, source);
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            ReadPixels();
            var colors = new[] { Color.red, Color.green, Color.blue, Color.yellow };
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var sourceX = column < 2 ? 0 : 1;
                    var sourceY = row < 2 ? 1 : 0;
                    if (reverse)
                    {
                        sourceX = 1 - sourceX;
                        sourceY = 1 - sourceY;
                    }

                    AssertColor(Pixel(30 + column * 20, 30 + row * 20), colors[sourceY * 2 + sourceX]);
                }
            }
        }

        /// <summary>Reads the direct camera target while preserving any caller's active render target.</summary>
        private void ReadPixels()
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
        }

        /// <summary>Reads a screenshot pixel using the interface's top-left coordinates.</summary>
        /// <param name="x">The horizontal screen coordinate.</param>
        /// <param name="y">The downward screen coordinate.</param>
        /// <returns>The composited screen color.</returns>
        private Color Pixel(int x, int y)
        {
            return _pixels.GetPixel(x, _target.height - y - 1);
        }

        /// <summary>Checks visible color channels with tolerance for the camera target's storage.</summary>
        /// <param name="actual">The rendered pixel color.</param>
        /// <param name="expected">The expected visible color.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.04f), "red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.04f), "green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.04f), "blue");
        }
    }
}
