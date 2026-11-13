#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares the real almanac's complete baked portraits with original loose artwork through public navigation.</summary>
    public sealed class BakedAlmanacGraphicsTests
    {
        /// <summary>The independently rendered original loose artwork.</summary>
        private ReanimationRenderer _original = null!;
        /// <summary>The production catalog and its shared atlas pages.</summary>
        private ReanimationRenderer _baked = null!;
        /// <summary>The original almanac view used as the pixel reference.</summary>
        private AlmanacView _originalView = null!;
        /// <summary>The production almanac view being checked.</summary>
        private AlmanacView _bakedView = null!;
        /// <summary>The actual scene target receiving Canvas and source renderers.</summary>
        private RenderTexture _target = null!;
        /// <summary>The reusable GPU readback texture.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The previous shared scene camera destination.</summary>
        private RenderTexture? _previousTarget;

        /// <summary>Creates both production UI routes after graphics initialization.</summary>
        /// <returns>The first available graphics frame.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "reanim"))
                || Resources.Load<TextAsset>(SpriteAssetProvider.CatalogResourcePath) == null)
            {
                Assert.Ignore("Provide original artwork and the generated production catalog to check baked almanac portraits.");
            }
            yield return null;
            CanvasUI.Dispose();
            _original = new ReanimationRenderer(root!);
            _baked = new ReanimationRenderer(Application.streamingAssetsPath);
            _originalView = new AlmanacView(root!, _original);
            _bakedView = new AlmanacView(Application.streamingAssetsPath, _baked);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
        }

        /// <summary>Releases both independent views and restores the host camera destination.</summary>
        /// <returns>A frame allowing deferred native destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_target != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                CanvasUI.Dispose();
                _originalView.Dispose();
                _bakedView.Dispose();
                _original.Dispose();
                _baked.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
            }
            yield return null;
        }

        /// <summary>Checks the reported direct-entry home page using production baked portraits.</summary>
        /// <returns>Real Canvas and actor presentation frames for both home-page sources.</returns>
        [UnityTest]
        public IEnumerator DirectEntryHomePortraitsMatchOriginalArtwork()
        {
            yield return Capture(_originalView, _original);
            var reference = _pixels.GetPixels32();
            Save(true, -1, "home-original");
            yield return Capture(_bakedView, _baked);
            Assert.That(_bakedView.Diagnostics, Is.Empty);
            var actual = _pixels.GetPixels32();
            var different = 0;
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var point = GameViewport.ScreenToLogical(new Vector2(x + .5f, y + .5f));
                    if (!new Rect(120f, 150f, 210f, 205f).Contains(point) && !new Rect(480f, 140f, 200f, 215f).Contains(point))
                    {
                        continue;
                    }
                    var index = y * _pixels.width + x;
                    var a = reference[index];
                    var b = actual[index];
                    if (Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b))) > 9)
                    {
                        different++;
                    }
                }
            }
            Save(true, -1, "home-baked");
            Assert.That(different, Is.Zero, "Both direct-entry home portraits must match the unchanged original artwork.");
        }

        /// <summary>Checks every selected plant and all concurrently drawn thumbnails against original artwork.</summary>
        /// <returns>Real presentation frames for every original plant entry.</returns>
        [UnityTest]
        public IEnumerator EveryBakedPlantPreviewMatchesOriginalAlmanac()
        {
            yield return CompareCategory(true, 49);
        }

        /// <summary>Checks every selected zombie, frozen thumbnail, attachment and clipped preview against original artwork.</summary>
        /// <returns>Real presentation frames for every original zombie entry.</returns>
        [UnityTest]
        public IEnumerator EveryBakedZombiePreviewMatchesOriginalAlmanac()
        {
            yield return CompareCategory(false, 26);
        }

        /// <summary>Navigates the actual almanac and compares the actor preview rather than isolated source quads.</summary>
        /// <param name="plants">Whether to navigate the plant or zombie category.</param>
        /// <param name="count">The complete original category entry count.</param>
        /// <returns>GPU presentation frames between original and baked snapshots.</returns>
        private IEnumerator CompareCategory(bool plants, int count)
        {
            var navigation = plants ? new Vector2(208f, 366f) : new Vector2(592f, 369f);
            Assert.That(_originalView.HandleClick(navigation), Is.True);
            Assert.That(_bakedView.HandleClick(navigation), Is.True);
            for (var id = 0; id < count; id++)
            {
                var slot = plants ? id == 48 ? new Rect(20, 23, 36, 46) : new Rect(26 + id % 8 * 52, 92 + id / 8 * 78, 50, 70)
                    : id == 25 ? new Rect(192, 486, 76, 76) : new Rect(22 + id % 5 * 85, 86 + id / 5 * 80, 76, 76);
                Assert.That(_originalView.HandleClick(slot.center), Is.True);
                Assert.That(_bakedView.HandleClick(slot.center), Is.True);
                Assert.That(_originalView.SelectedId, Is.EqualTo(id));
                Assert.That(_bakedView.SelectedId, Is.EqualTo(id));
                yield return Capture(_originalView, _original);
                var reference = _pixels.GetPixels32();
                Save(plants, id, "original");
                yield return Capture(_bakedView, _baked);
                Assert.That(_bakedView.Diagnostics, Is.Empty, "The production almanac must resolve every original portrait.");
                var actual = _pixels.GetPixels32();
                var peak = 0;
                var different = 0;
                var region = new Rect(516f, 108f, 204f, plants ? 160f : 234f);
                for (var y = 0; y < _pixels.height; y++)
                {
                    for (var x = 0; x < _pixels.width; x++)
                    {
                        var point = GameViewport.ScreenToLogical(new Vector2(x + .5f, y + .5f));
                        if (!region.Contains(point))
                        {
                            continue;
                        }
                        var index = y * _pixels.width + x;
                        var a = reference[index];
                        var b = actual[index];
                        var delta = Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b)));
                        peak = Math.Max(peak, delta);
                        if (delta > 9)
                        {
                            different++;
                        }
                    }
                }
                Save(plants, id, "baked");
                Assert.That(different, Is.Zero, (plants ? "plant " : "zombie ") + id + " whole portrait differs from original source; peak=" + peak);
            }
        }

        /// <summary>Publishes a complete real almanac frame using the game host's object and Canvas boundaries.</summary>
        /// <param name="view">The almanac view being submitted.</param>
        /// <param name="animations">The view's original source renderer.</param>
        /// <returns>Frames allowing both native scene and UI geometry to render.</returns>
        private IEnumerator Capture(AlmanacView view, ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            var bounds = GameViewport.ScreenRect;
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(bounds.x, bounds.y), Quaternion.identity, new Vector3(GameViewport.Scale, GameViewport.Scale, 1f));
            animations.BeginObjectFrame();
            try
            {
                view.Draw(0f);
            }
            finally
            {
                animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }
            yield return null;
            yield return null;
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            _pixels.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            _pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Saves opt-in full almanac evidence outside tracked artwork.</summary>
        /// <param name="plants">Whether the evidence belongs to the plant category.</param>
        /// <param name="id">The selected original entry.</param>
        /// <param name="source">The original or baked capture identity.</param>
        private void Save(bool plants, int id, string source)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_ALMANAC_GRAPHICS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, (plants ? "plant-" : "zombie-") + id + "-" + source + ".png"), _pixels.EncodeToPNG());
            }
        }
    }
}
