#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises production baked almanac portraits across menu transitions and actual atlas page retirement.</summary>
    public sealed class BakedAlmanacResourceLifetimeTests
    {
        /// <summary>The production-root provider shared by the menu and native animation renderer.</summary>
        private SpriteAssetProvider _provider = null!;
        /// <summary>The real renderer owning direct plant geometry and frozen zombie thumbnail captures.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>The native almanac view used by the game menu.</summary>
        private AlmanacView _view = null!;
        /// <summary>The camera target preserving the runner's actual screen pixel grid.</summary>
        private RenderTexture _target = null!;
        /// <summary>The readable copy of the real scene and Canvas result.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The runner camera target restored during cleanup.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>The scale mapping original logical pixels to the camera target.</summary>
        private float _scale;
        /// <summary>The letterbox offset mapping original pixels to the camera target.</summary>
        private Vector2 _offset;

        /// <summary>Uses packaged Resources with the production root so this fixture cannot silently test loose sprites instead.</summary>
        /// <returns>A graphics initialization frame before creating the real menu resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Environment.GetEnvironmentVariable("PVZ_RENDER_USE_BAKED") != "1")
            {
                Assert.Ignore("Set PVZ_RENDER_USE_BAKED=1 and include the generated production Resources catalog.");
            }

            Assert.That(Resources.Load<TextAsset>(SpriteAssetProvider.CatalogResourcePath), Is.Not.Null,
                "An explicitly baked graphics run must include its real generated catalog.");
            yield return null;
            CanvasUI.Dispose();
            _provider = SpriteAssetProvider.ForRoot(Application.streamingAssetsPath);
            Assert.That(_provider.IsBaked, Is.True);
            _animations = new ReanimationRenderer(Application.streamingAssetsPath);
            _view = new AlmanacView(Application.streamingAssetsPath, _animations);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
        }

        /// <summary>Releases only this view's acquisitions, captures, Canvas, and camera readback resources.</summary>
        /// <returns>A frame allowing Unity to complete deferred scene-object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_target != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                CanvasUI.Dispose();
                _view?.Dispose();
                _animations?.Dispose();
                _provider?.Dispose();
                _target.Release();
                Object.Destroy(_target);
                Object.Destroy(_pixels);
            }

            yield return null;
        }

        /// <summary>Keeps direct plants and retained zombie thumbnails correct after their source pages are actually unloaded and reloaded.</summary>
        /// <returns>Real graphics frames for both categories, idle menu retirement, and category reentry.</returns>
        [UnityTest]
        public IEnumerator BakedPortraitsSurviveIdlePageEvictionAndMenuReentry()
        {
            // Prime the cold Pole-vaulter page before any idle retirement can corrupt its reference capture.
            var selections = new[] { (Plants: false, Id: 3), (Plants: false, Id: 0), (Plants: false, Id: 1),
                (Plants: false, Id: 2), (Plants: false, Id: 7), (Plants: false, Id: 18), (Plants: true, Id: 0) };
            var references = new Color32[selections.Length][];
            for (var category = 0; category < selections.Length; category++)
            {
                var selection = selections[category];
                var region = selection.Plants ? new Rect(550f, 120f, 160f, 140f) : AlmanacPortraitLayout.ZombiePreviewClip;
                OpenCategory(selection.Plants, selection.Id);
                yield return DrawView();
                references[category] = _pixels.GetPixels32();
                Assert.That(_view.Diagnostics, Is.Empty);
                Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.Null,
                    "A menu portrait must not inherit an actor hierarchy scope from another view.");

                // Keep the exact same Canvas, retiring only native scene portraits as the existing graphics oracle does.
                ObjectSpriteRenderer.BeginSceneFrame();
                yield return null;
                yield return null;
                ReadPixels();
                Assert.That(ChangedPixels(references[category], _pixels.GetPixels32(), region), Is.GreaterThan(100),
                    "The selected production baked portrait must contain actual visible actor pixels.");
            }

            for (var frame = 0; frame < 5; frame++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                CanvasUI.BeginFrame();
                CanvasUI.Color = Color.white;
                CanvasUI.Matrix = ViewportMatrix();
                CanvasUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
                CanvasUI.EndFrame();
                yield return null;
            }

            Assert.That(_provider.LoadedPageCount, Is.Zero, "This case must exercise actual atlas unloading rather than a warm-cache redraw.");
            for (var category = 0; category < selections.Length; category++)
            {
                var selection = selections[category];
                var region = selection.Plants ? new Rect(550f, 120f, 160f, 140f) : AlmanacPortraitLayout.ZombiePreviewClip;
                OpenCategory(selection.Plants, selection.Id);
                yield return DrawView();
                Assert.That(ChangedPixels(references[category], _pixels.GetPixels32(), region), Is.Zero,
                    "Reentering the almanac must preserve the same complete portrait and source transforms.");
                if (!selection.Plants)
                {
                    Assert.That(ChangedPixels(references[category], _pixels.GetPixels32(), new Rect(22f, 86f, 416f, 476f)), Is.Zero,
                        "Frozen original zombie thumbnail captures must survive retirement of their former source pages.");
                }

                Assert.That(_view.Diagnostics, Is.Empty);
                Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.Null);
            }
        }

        /// <summary>Uses public navigation to reopen a remembered first entry with its animation epoch reset.</summary>
        /// <param name="plants">Whether to navigate to the plant category.</param>
        /// <param name="id">The selected common or cold-page original entry.</param>
        private void OpenCategory(bool plants, int id)
        {
            _view.Open();
            Assert.That(_view.HandleClick(plants ? new Vector2(208f, 366f) : new Vector2(592f, 369f)), Is.True);
            var point = plants ? new Vector2(26f + id % 8 * 52f + 25f, 92f + id / 8 * 78f + 35f)
                : new Vector2(22f + id % 5 * 85f + 38f, 86f + id / 5 * 80f + 38f);
            Assert.That(_view.HandleClick(point), Is.True);
            Assert.That(_view.SelectedId, Is.EqualTo(id));
        }

        /// <summary>Draws through the same native frame and Canvas boundaries as the production presenter.</summary>
        /// <returns>Frames allowing both direct scene meshes and Canvas geometry to render.</returns>
        private IEnumerator DrawView()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = ViewportMatrix();
            _animations.BeginObjectFrame();
            try
            {
                _view.Draw(0f);
            }
            finally
            {
                _animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            yield return null;
            yield return null;
            ReadPixels();
        }

        /// <summary>Maps native almanac pixels to the actual screen-sized test target.</summary>
        /// <returns>The production-compatible viewport transform.</returns>
        private Matrix4x4 ViewportMatrix()
        {
            return Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0f), Quaternion.identity, new Vector3(_scale, _scale, 1f));
        }

        /// <summary>Counts visibly changed pixels within an original-coordinate portrait rectangle.</summary>
        /// <param name="first">The reference complete camera image.</param>
        /// <param name="second">The camera image being compared.</param>
        /// <param name="region">The original logical rectangle to inspect.</param>
        /// <returns>The number of pixels whose summed RGB difference exceeds the existing almanac oracle's tolerance.</returns>
        private int ChangedPixels(Color32[] first, Color32[] second, Rect region)
        {
            var changed = 0;
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var point = new Vector2((x - _offset.x) / _scale, (_pixels.height - y - 1f - _offset.y) / _scale);
                    if (region.Contains(point))
                    {
                        var index = y * _pixels.width + x;
                        if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                            + Math.Abs(first[index].b - second[index].b) > 12)
                        {
                            changed++;
                        }
                    }
                }
            }

            return changed;
        }

        /// <summary>Reads the actual scene camera target without leaking its active RenderTexture state.</summary>
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
    }
}
