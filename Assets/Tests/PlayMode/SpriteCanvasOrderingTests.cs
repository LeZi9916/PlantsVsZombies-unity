#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Runs production Canvas and board overlap scenarios through explicit rendering backends.</summary>
    public sealed class SpriteCanvasOrderingTests
    {
        /// <summary>The original backend restored after each scenario.</summary>
        private SpriteRenderingBackend _backend;
        /// <summary>The real board fixture when a scenario requests it.</summary>
        private GamePresentationPerformanceTests? _board;
        /// <summary>The real Canvas fixture when a scenario requests it.</summary>
        private CanvasPresentationTests? _canvas;
        /// <summary>The optional source pool used by the native mask and font rebuild scenario.</summary>
        private ObjectSpriteRenderer? _sprites;
        /// <summary>The optional real native stencil panel.</summary>
        private GameObject? _maskPanel;
        /// <summary>The stencil mask whose state changes between complete presentations.</summary>
        private Mask? _mask;
        /// <summary>The native text rebuilt after stencil and material changes.</summary>
        private Text? _label;
        /// <summary>The mutable source font material, never modified by the queue bridge.</summary>
        private Material? _fontMaterial;
        /// <summary>The readable pixels used by native UI paired comparisons.</summary>
        private Texture2D? _pixels;

        /// <summary>Stores the backend and isolates retained interface resources.</summary>
        [SetUp]
        public void SetUp()
        {
            _backend = ObjectSpriteRenderer.Backend;
            _board = null;
            _canvas = null;
            _sprites = null;
            _maskPanel = null;
            _mask = null;
            _label = null;
            _fontMaterial = null;
            _pixels = null;
            CanvasUI.Dispose();
        }

        /// <summary>Releases the selected production fixture and restores backend selection.</summary>
        /// <returns>The fixture's normal resource destruction frames.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _sprites?.Dispose();
            UnityEngine.Object.Destroy(_maskPanel);
            UnityEngine.Object.Destroy(_fontMaterial);
            UnityEngine.Object.Destroy(_pixels);
            if (_board != null)
            {
                yield return _board.TearDown();
            }
            if (_canvas != null)
            {
                yield return _canvas.TearDown();
            }
            CanvasUI.Dispose();
            ObjectSpriteRenderer.Backend = _backend;
        }

        /// <summary>Confirms the mesh oracle preserves the production seed-bank and zombie overlap.</summary>
        /// <returns>The real board fixture's setup and rendering frames.</returns>
        [UnityTest]
        public IEnumerator ProductionBankOverlapUsesIndependentMeshOracle()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            _board = new GamePresentationPerformanceTests();
            yield return _board.SetUp();
            yield return _board.PlayingSeedBankIsBehindZombieArtwork();
        }

        /// <summary>Checks actual ScreenSpaceCamera Canvas graphics remain ordered around instance draws.</summary>
        /// <returns>The real Canvas fixture's setup and rendering frames.</returns>
        [UnityTest]
        public IEnumerator InstanceDrawsInterleaveWithNativeCanvasGraphics()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            _canvas = new CanvasPresentationTests();
            yield return _canvas.SetUp();
            yield return _canvas.InterleavesCanvasAndSceneRenderers();
        }

        /// <summary>Checks the instanced backend preserves the real board's foreground zombie over the bank.</summary>
        /// <returns>The real board fixture's setup and rendering frames.</returns>
        [UnityTest]
        public IEnumerator ProductionBankOverlapUsesOrderedInstances()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            _board = new GamePresentationPerformanceTests();
            yield return _board.SetUp();
            yield return _board.PlayingSeedBankIsBehindZombieArtwork();
        }

        /// <summary>Checks real native stencil pop materials and rebuilt font properties match the mesh oracle.</summary>
        /// <returns>The native setup and camera frames for three stencil and font states.</returns>
        [UnityTest]
        public IEnumerator NativeMaskAndFontRebuildPreserveOrderedMaterialProperties()
        {
            _canvas = new CanvasPresentationTests();
            yield return _canvas.SetUp();
            _sprites = new ObjectSpriteRenderer();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            for (var state = 0; state < 3; state++)
            {
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                DrawMaskedFrame(state);
                yield return null;
                yield return null;
                var expected = ReadScenePixels();
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
                DrawMaskedFrame(state);
                yield return null;
                yield return null;
                Assert.That(ReadScenePixels(), Is.EqualTo(expected));
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.GreaterThan(0));
                Assert.That(_fontMaterial!.renderQueue, Is.EqualTo(3000), "The source font material must remain unmodified.");
            }
        }

        /// <summary>Checks camera preparation preserves native input priorities even after an exception and a second camera.</summary>
        /// <returns>The real native Canvas setup frame.</returns>
        [UnityTest]
        public IEnumerator CameraPreparationPreservesNativeInputPrioritiesAfterFailureAndSecondCamera()
        {
            _canvas = new CanvasPresentationTests();
            yield return _canvas.SetUp();
            _sprites = new ObjectSpriteRenderer();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawMaskedFrame(0);
            var layers = (IReadOnlyList<Canvas>)typeof(CanvasUI).GetField("s_layers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var firstOrder = layers[0].sortingOrder;
            var secondOrder = layers[1].sortingOrder;
            Assert.That(secondOrder, Is.GreaterThan(0));
            Assert.Throws<InvalidOperationException>(() =>
            {
                ObjectSpriteRenderer.PrepareSceneQueuesForCamera(ObjectSpriteRenderer.SceneCamera);
                Assert.That(layers[0].sortingOrder, Is.EqualTo(firstOrder));
                Assert.That(layers[1].sortingOrder, Is.EqualTo(secondOrder));
                throw new InvalidOperationException("Injected failure after native camera ordering.");
            });
            Assert.That(layers[0].sortingOrder, Is.EqualTo(firstOrder));
            Assert.That(layers[1].sortingOrder, Is.EqualTo(secondOrder));
            var secondaryObject = new GameObject("Independent ordered scene view", typeof(Camera));
            try
            {
                var secondary = secondaryObject.GetComponent<Camera>();
                secondary.CopyFrom(ObjectSpriteRenderer.SceneCamera);
                secondary.cameraType = CameraType.SceneView;
                ObjectSpriteRenderer.PrepareSceneQueuesForCamera(secondary);
                Assert.That(layers[0].sortingOrder, Is.EqualTo(firstOrder));
                Assert.That(layers[1].sortingOrder, Is.EqualTo(secondOrder));
            }
            finally
            {
                UnityEngine.Object.Destroy(secondaryObject);
            }
        }

        /// <summary>Draws native UI layers around a full-screen sprite and creates a real stencil-masked child hierarchy.</summary>
        /// <param name="state">The mask and mutable font state to exercise.</param>
        private void DrawMaskedFrame(int state)
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            using (_sprites!.BeginFrame())
            {
                _sprites.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.identity, Color.green, 0);
            }
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(Screen.width - 2f, 0f, 2f, 2f), Texture2D.whiteTexture);
            var layers = (IReadOnlyList<Canvas>)typeof(CanvasUI).GetField("s_layers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            if (_maskPanel == null)
            {
                _maskPanel = new GameObject("Native ordered stencil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
                var rectangle = (RectTransform)_maskPanel.transform;
                rectangle.SetParent(layers[1].transform, false);
                rectangle.anchorMin = new Vector2(0f, 1f);
                rectangle.anchorMax = new Vector2(0f, 1f);
                rectangle.pivot = new Vector2(0f, 1f);
                rectangle.anchoredPosition = new Vector2(Screen.width * .2f, -Screen.height * .2f);
                rectangle.sizeDelta = new Vector2(Screen.width * .5f, Screen.height * .5f);
                _mask = _maskPanel.GetComponent<Mask>();
                _mask.showMaskGraphic = false;
                var fill = new GameObject("Native clipped image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Stretch((RectTransform)fill.transform, rectangle);
                fill.GetComponent<Image>().color = new Color(0f, 0f, 1f, .65f);
                var text = new GameObject("Native rebuilt font", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                Stretch((RectTransform)text.transform, rectangle);
                _label = text.GetComponent<Text>();
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _label.fontSize = 24;
                _label.alignment = TextAnchor.MiddleCenter;
                _label.color = Color.white;
                _fontMaterial = new Material(_label.material) { hideFlags = HideFlags.HideAndDontSave };
                _label.material = _fontMaterial;
            }
            _mask!.enabled = state == 0;
            _label!.text = state == 0 ? "Mask ABC 0123" : state == 1 ? "Font rebuild XYZ 4567" : "Live material 890";
            _fontMaterial!.SetColor("_Color", state == 2 ? Color.red : Color.white);
            _fontMaterial.SetFloat("_UseUIAlphaClip", state == 0 ? 1f : 0f);
            if (state == 0)
            {
                _fontMaterial.EnableKeyword("UNITY_UI_ALPHACLIP");
            }
            else
            {
                _fontMaterial.DisableKeyword("UNITY_UI_ALPHACLIP");
            }
            _label.SetMaterialDirty();
            CanvasUI.EndFrame();
        }

        /// <summary>Stretches a real native graphic inside the mask rectangle.</summary>
        /// <param name="child">The native graphic rectangle.</param>
        /// <param name="parent">The native mask rectangle.</param>
        private static void Stretch(RectTransform child, RectTransform parent)
        {
            child.SetParent(parent, false);
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
        }

        /// <summary>Reads the current native camera target without changing the caller's active target.</summary>
        /// <returns>A complete independent color snapshot.</returns>
        private Color32[] ReadScenePixels()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = ObjectSpriteRenderer.SceneCamera.targetTexture;
                _pixels!.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                _pixels.Apply();
                return _pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }
    }
}
