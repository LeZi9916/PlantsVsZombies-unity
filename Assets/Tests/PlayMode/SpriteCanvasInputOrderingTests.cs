#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks native event priority while the ordered instance and Canvas material bridge is active.</summary>
    public sealed class SpriteCanvasInputOrderingTests
    {
        /// <summary>Owns isolated input devices throughout all coroutine frame boundaries.</summary>
        private readonly InputTestFixture _input = new InputTestFixture();
        /// <summary>Whether the isolated input runtime requires restoration.</summary>
        private bool _inputActive;
        /// <summary>The backend restored after the scenario.</summary>
        private SpriteRenderingBackend _backend;
        /// <summary>The production pool interleaved with the two native UI layers.</summary>
        private ObjectSpriteRenderer _sprites = null!;
        /// <summary>The isolated native mouse.</summary>
        private Mouse _mouse = null!;
        /// <summary>The production official-action UI module.</summary>
        private CanvasInputModule _module = null!;
        /// <summary>The retained native button layout.</summary>
        private GUIStyle _style = null!;
        /// <summary>The upper input layer's visual and hit-test mask.</summary>
        private CanvasGroup _overlay = null!;
        /// <summary>The original source material modified during a native rebuild.</summary>
        private Material? _sourceMaterial;
        /// <summary>The lower click result consumed during the latest presentation.</summary>
        private bool _lowerClicked;
        /// <summary>The upper click result consumed during the latest presentation.</summary>
        private bool _upperClicked;

        /// <summary>Creates overlapping real controls with a GPU sprite between their independently sorted canvases.</summary>
        /// <returns>Frames initializing the actual input module and native rendering.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            GameInput.Reset();
            _backend = ObjectSpriteRenderer.Backend;
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            _input.Setup();
            _inputActive = true;
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Touchscreen>();
            _sprites = new ObjectSpriteRenderer();
            _style = new GUIStyle { font = CanvasUI.DefaultFont, fontSize = 14 };
            DrawControls();
            _module = EventSystem.current.GetComponent<CanvasInputModule>();
            var layers = Layers();
            _overlay = layers[1].gameObject.AddComponent<CanvasGroup>();
            yield return null;
            yield return null;
            if (!ObjectSpriteRenderer.InstancingAvailable)
            {
                Assert.Ignore("The current device cannot execute the structured-buffer instance shader.");
            }
        }

        /// <summary>Releases controls and subscriptions before restoring the input runtime.</summary>
        /// <returns>A frame completing native destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CanvasUI.Dispose();
            _sprites?.Dispose();
            UnityEngine.Object.Destroy(_sourceMaterial);
            ObjectSpriteRenderer.Backend = _backend;
            yield return null;
            if (_inputActive)
            {
                GameInput.Reset();
                _input.TearDown();
                _inputActive = false;
            }
        }

        /// <summary>Checks upper hits survive a same-frame material rebuild and masking the overlay restores the lower button.</summary>
        /// <returns>Real mouse press and release frames processed by the production UI module.</returns>
        [UnityTest]
        public IEnumerator OverlappingButtonsKeepNativeHitPriorityThroughMaterialRebuildAndMaskRemoval()
        {
            var layers = Layers();
            var lowerOrder = layers[0].sortingOrder;
            var upperOrder = layers[1].sortingOrder;
            Assert.That(upperOrder, Is.GreaterThan(lowerOrder));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.GreaterThan(0));
            var lower = layers[0].GetComponentInChildren<Button>();
            var upper = layers[1].GetComponentInChildren<Button>();
            var point = new Vector2(60f, Screen.height - 30f);
            AssertTopHit(point, upper);

            _input.Set(_mouse.position, point);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(_module.GetLastRaycastResult(_mouse.deviceId).gameObject, Is.EqualTo(upper.gameObject));
            _sourceMaterial = new Material(upper.targetGraphic.material) { hideFlags = HideFlags.HideAndDontSave };
            _sourceMaterial.SetColor("_Color", new Color(.3f, .6f, .9f, 1f));
            upper.targetGraphic.material = _sourceMaterial;
            upper.targetGraphic.SetMaterialDirty();
            Canvas.ForceUpdateCanvases();
            ObjectSpriteRenderer.PrepareSceneQueuesForCamera(ObjectSpriteRenderer.SceneCamera);
            Assert.That(layers[0].sortingOrder, Is.EqualTo(lowerOrder));
            Assert.That(layers[1].sortingOrder, Is.EqualTo(upperOrder));
            AssertTopHit(point, upper);
            _input.Release(_mouse.leftButton);
            yield return null;
            _module.Process();
            DrawControls();
            Assert.That(_upperClicked, Is.True);
            Assert.That(_lowerClicked, Is.False);

            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;
            Canvas.ForceUpdateCanvases();
            ObjectSpriteRenderer.PrepareSceneQueuesForCamera(ObjectSpriteRenderer.SceneCamera);
            AssertTopHit(point, lower);
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(_module.GetLastRaycastResult(_mouse.deviceId).gameObject, Is.EqualTo(lower.gameObject));
            _input.Release(_mouse.leftButton);
            yield return null;
            _module.Process();
            DrawControls();
            Assert.That(_lowerClicked, Is.True);
            Assert.That(_upperClicked, Is.False);
            Assert.That(layers[0].sortingOrder, Is.EqualTo(lowerOrder));
            Assert.That(layers[1].sortingOrder, Is.EqualTo(upperOrder));
        }

        /// <summary>Presents two native buttons at the same bounds around a complete GPU sprite span.</summary>
        private void DrawControls()
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            CanvasUI.BeginFrame();
            var bounds = new Rect(10f, 10f, 100f, 40f);
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(bounds, Texture2D.whiteTexture);
            _lowerClicked = CanvasUI.Button(bounds, GUIContent.none, _style);
            using (_sprites.BeginFrame())
            {
                _sprites.DrawQuad(Texture2D.whiteTexture, 130f, 60f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.green, 0);
            }
            CanvasUI.Color = Color.blue;
            CanvasUI.DrawTexture(bounds, Texture2D.whiteTexture);
            _upperClicked = CanvasUI.Button(bounds, GUIContent.none, _style);
            CanvasUI.EndFrame();
        }

        /// <summary>Gets the actual independently sorted native Canvas layer pool outside measured code.</summary>
        /// <returns>The existing production layers.</returns>
        private static IReadOnlyList<Canvas> Layers()
        {
            return (IReadOnlyList<Canvas>)typeof(CanvasUI).GetField("s_layers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        }

        /// <summary>Checks the real EventSystem and GraphicRaycaster return the intended first hit.</summary>
        /// <param name="point">The physical bottom-left mouse position.</param>
        /// <param name="expected">The native control that must receive the press.</param>
        private static void AssertTopHit(Vector2 point, Button expected)
        {
            var results = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = point };
            EventSystem.current.RaycastAll(pointer, results);
            Assert.That(results, Is.Not.Empty);
            Assert.That(results[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(expected));
        }
    }
}
