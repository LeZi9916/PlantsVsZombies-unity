#nullable enable

using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks unrestricted original almanac entries reach the real Canvas and scene camera.</summary>
    public sealed class OriginalAlmanacGraphicsTests
    {
        /// <summary>Owns the original portrait animations submitted by the production almanac.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Owns the original category, selection, and restricted-entry drawing paths.</summary>
        private AlmanacView _view = null!;
        /// <summary>Receives the composited scene and Canvas artwork.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives readable camera pixels for actor-only comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the surrounding runner's camera target.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Maps the original 800 by 600 layout to the current screen.</summary>
        private float _scale;
        /// <summary>Stores the centered viewport's horizontal and vertical offsets.</summary>
        private Vector2 _offset;

        /// <summary>Creates a real-asset almanac without loading profiles or starting the game lifecycle.</summary>
        /// <returns>A frame boundary allowing the graphics pipeline to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            CanvasUI.Dispose();
            _animations = new ReanimationRenderer(root);
            _view = new AlmanacView(root, _animations);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
        }

        /// <summary>Releases only the textures, Canvas graphics, and portrait resources owned by this fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction to finish.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_view != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                CanvasUI.Dispose();
                _view.Dispose();
                _animations.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
            }

            yield return null;
        }

        /// <summary>Checks default plant thumbnails and the selected preview remain visible while an explicit empty restriction still locks them.</summary>
        /// <returns>Frame boundaries for actual original plant portrait and Canvas pixel comparisons.</returns>
        [UnityTest]
        public IEnumerator DefaultPlantEntriesAndPreviewRenderOriginalArtwork()
        {
            yield return CheckCategory(true);
        }

        /// <summary>Checks default zombie thumbnails and the clipped preview remain visible while an explicit empty restriction still locks them.</summary>
        /// <returns>Frame boundaries for actual original zombie portrait and Canvas pixel comparisons.</returns>
        [UnityTest]
        public IEnumerator DefaultZombieEntriesAndPreviewRenderOriginalArtwork()
        {
            yield return CheckCategory(false);
        }

        /// <summary>Checks default category selection, real actor pixels, and the retained explicit restriction contract.</summary>
        /// <param name="plants">Whether the original plant category is being checked.</param>
        /// <returns>Frame boundaries publishing the default portraits and their unchanged Canvas without scene actors.</returns>
        private IEnumerator CheckCategory(bool plants)
        {
            var navigation = plants ? new Vector2(208f, 366f) : new Vector2(592f, 369f);
            Assert.That(_view.HandleClick(navigation), Is.True);
            Assert.That(_view.SelectedId, Is.Zero, "An unrestricted category opens its remembered first original entry.");
            Assert.That(_view.Screen, Is.EqualTo(plants ? AlmanacScreen.PlantDetail : AlmanacScreen.ZombieDetail));
            yield return Capture();
            Assert.That(_view.Diagnostics, Is.Empty, "All original category resources must load without missing-portrait errors.");
            var withPortraits = _pixels.GetPixels32();

            // Retire only scene actors; the exact same native Canvas backgrounds, cards, and text stay visible.
            ObjectSpriteRenderer.BeginSceneFrame();
            yield return null;
            yield return null;
            ReadPixels();
            var withoutPortraits = _pixels.GetPixels32();
            var thumbnail = plants ? new Rect(26f, 92f, 50f, 53f) : new Rect(24f, 88f, 72f, 72f);
            var preview = plants ? new Rect(550f, 120f, 160f, 140f) : AlmanacPortraitLayout.ZombiePreviewClip;
            Assert.That(ChangedPixels(withPortraits, withoutPortraits, thumbnail), Is.GreaterThan(20),
                "The first entry must contain visible original actor pixels in the actual thumbnail window.");
            Assert.That(ChangedPixels(withPortraits, withoutPortraits, preview), Is.GreaterThan(100),
                "The selected actor must remain visible above its original preview background and behind the frame.");

            if (plants)
            {
                _view.SetUnlockedPlants(Array.Empty<int>());
            }
            else
            {
                _view.SetUnlockedZombies(Array.Empty<int>());
            }

            Assert.That(_view.SelectedId, Is.EqualTo(-1), "An explicit empty restriction still clears the selected portrait.");
            Assert.That(_view.Screen, Is.EqualTo(plants ? AlmanacScreen.PlantIndex : AlmanacScreen.ZombieIndex));
            Assert.That(_view.HandleClick(thumbnail.center), Is.False, "Explicitly locked entries cannot be selected.");
            yield return Capture();
            var locked = _pixels.GetPixels32();
            ObjectSpriteRenderer.BeginSceneFrame();
            yield return null;
            yield return null;
            ReadPixels();
            Assert.That(ChangedPixels(locked, _pixels.GetPixels32(), new Rect(0f, 0f, 800f, 600f)), Is.Zero,
                "An explicit empty restriction submits no thumbnail or preview actors.");
        }

        /// <summary>Submits the production almanac through the same Canvas and object-frame boundaries as the game host.</summary>
        /// <returns>Frame boundaries allowing native Canvas geometry and original portraits to reach the display camera.</returns>
        private IEnumerator Capture()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0f), Quaternion.identity, new Vector3(_scale, _scale, 1f));
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

        /// <summary>Counts actor pixels changed within an original top-left logical rectangle.</summary>
        /// <param name="first">The camera image including the submitted portraits.</param>
        /// <param name="second">The same Canvas image after the scene actors were hidden.</param>
        /// <param name="region">The original logical rectangle being inspected.</param>
        /// <returns>The number of changed camera pixels inside the requested original region.</returns>
        private int ChangedPixels(Color32[] first, Color32[] second, Rect region)
        {
            var count = 0;
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var point = new Vector2((x - _offset.x) / _scale, (_pixels.height - y - 1f - _offset.y) / _scale);
                    if (!region.Contains(point))
                    {
                        continue;
                    }

                    var index = y * _pixels.width + x;
                    if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                        + Math.Abs(first[index].b - second[index].b) > 12)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Reads actual camera output while restoring the caller's active render target.</summary>
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
