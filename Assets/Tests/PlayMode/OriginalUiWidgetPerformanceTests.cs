#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects retained native widget artwork and bitmap caption layouts from recurring allocations.</summary>
    public sealed class OriginalUiWidgetPerformanceTests
    {
        /// <summary>Owns the scene camera used by the retained Canvas widgets.</summary>
        private ObjectSpriteRenderer _renderer = null!;
        /// <summary>Supplies the same small source image for synthetic widget artwork.</summary>
        private Texture2D _image = null!;
        /// <summary>Reuses the host's texture lookup delegate during every measured frame.</summary>
        private Func<string, Texture2D> _lookup = null!;

        /// <summary>Creates independent Canvas state, synthetic artwork, and a cached host lookup.</summary>
        /// <returns>A frame boundary before creating native rendering resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            OriginalUiWidgets.ReleaseFonts();
            _renderer = new ObjectSpriteRenderer();
            _image = new Texture2D(12, 12, TextureFormat.RGBA32, false);
            _image.Apply();
            _lookup = LookupTexture;
        }

        /// <summary>Releases only resources owned by the widget fixture.</summary>
        /// <returns>A frame boundary allowing Unity to finish deferred destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CanvasUI.Dispose();
            OriginalUiWidgets.ReleaseFonts();
            _renderer.Dispose();
            UnityEngine.Object.Destroy(_image);
            yield return null;
        }

        /// <summary>Checks buttons, baseline captions, wrapped labels, dialog tiles, and nine-slice borders after warmup.</summary>
        [Test]
        public void WarmOriginalWidgetsDoNotAllocateManagedMemory()
        {
            var supported = true;
            for (var i = 0; i < 8; i++)
            {
                supported &= PresentWidgets();
            }

            Assert.That(supported, Is.True, "The bundled widget fonts must support the measured captions.");
            var retained = CanvasUI.PooledImageCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                supported &= PresentWidgets();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(supported, Is.True);
            Assert.That(allocated, Is.Zero);
            Assert.That(CanvasUI.PooledImageCount, Is.EqualTo(retained));
        }

        /// <summary>Checks cached baseline captions still apply each caller's width and single-line requirements.</summary>
        [Test]
        public void CachedBaselineCaptionPreservesFitChecksAndFontReload()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            Assert.That(OriginalUiWidgets.TextAtBaseline("中文", 100f, 50f, 200f, "DwarvenTodcraft18", Color.white), Is.True);
            Assert.That(OriginalUiWidgets.TextAtBaseline("中文", 100f, 50f, 1f, "DwarvenTodcraft18", Color.white), Is.False);
            Assert.That(OriginalUiWidgets.TextAtBaseline("中\n文", 100f, 50f, 200f, "DwarvenTodcraft18", Color.white), Is.False);
            CanvasUI.EndFrame();
            OriginalUiWidgets.ReleaseFonts();

            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            Assert.That(OriginalUiWidgets.TextAtBaseline("中文", 100f, 50f, 200f, "DwarvenTodcraft18", Color.white), Is.True);
            CanvasUI.EndFrame();
        }

        /// <summary>Submits a stable frame covering normal, pressed, disabled, and wrapped widget paths.</summary>
        /// <returns>Whether all bundled bitmap captions were successfully drawn.</returns>
        private bool PresentWidgets()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            OriginalUiWidgets.StoneButton(_lookup, new Rect(10f, 10f, 120f, 24f));
            var supported = OriginalUiWidgets.StoneLabel(new Rect(10f, 10f, 120f, 24f), "中文");
            OriginalUiWidgets.StoneButton(_lookup, new Rect(150f, 10f, 120f, 24f), true, true);
            supported &= OriginalUiWidgets.StoneLabel(new Rect(150f, 10f, 120f, 24f), "中文", true, true);
            OriginalUiWidgets.StoneButton(_lookup, new Rect(290f, 10f, 120f, 24f), enabled: false);
            supported &= OriginalUiWidgets.StoneLabel(new Rect(290f, 10f, 120f, 24f), "中文", enabled: false);
            supported &= OriginalUiWidgets.TextAtBaseline("中文", 100f, 70f, 200f, "DwarvenTodcraft18", Color.white);
            supported &= OriginalUiWidgets.Text(new Rect(10f, 90f, 44f, 100f), "中文中文中文", "DwarvenTodcraft18", Color.white, false, true);
            supported &= OriginalUiWidgets.Text(new Rect(100f, 90f, 200f, 100f), "中文中文中文", "DwarvenTodcraft18", Color.white, false, true);
            OriginalUiWidgets.NineSlice(_image, new Rect(10f, 200f, 100f, 60f), 4, 4);
            OriginalUiWidgets.DialogFrame(_lookup, new Rect(150f, 200f, 80f, 60f), true, false);
            OriginalUiWidgets.DialogFrame(_lookup, new Rect(250f, 200f, 80f, 60f), false, false);
            CanvasUI.EndFrame();
            return supported;
        }

        /// <summary>Provides synthetic original-widget artwork without loading game resources.</summary>
        /// <param name="name">The native artwork identity requested by the widget.</param>
        /// <returns>The shared synthetic source image.</returns>
        private Texture2D LookupTexture(string name)
        {
            return _image;
        }
    }
}
