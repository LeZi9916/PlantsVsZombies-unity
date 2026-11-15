#nullable enable
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises actual retained Canvas controls through the new device and official UI module path.</summary>
    public sealed class CanvasInputSystemTests
    {
        /// <summary>Owns input isolation for the full Unity setup, test and teardown coroutine lifetime.</summary>
        private readonly InputTestFixture _input = new InputTestFixture();
        /// <summary>Tracks whether the isolated input runtime needs restoration.</summary>
        private bool _inputActive;
        /// <summary>Stores the isolated mouse.</summary>
        private Mouse _mouse = null!;
        /// <summary>Stores the isolated touch screen.</summary>
        private Touchscreen _screen = null!;
        /// <summary>Stores the isolated keyboard.</summary>
        private Keyboard _keyboard = null!;
        /// <summary>Owns the production scene camera resources.</summary>
        private ObjectSpriteRenderer _renderer = null!;
        /// <summary>Routes native UI actions.</summary>
        private CanvasInputModule _module = null!;
        /// <summary>Contains the visible native text layout.</summary>
        private GUIStyle _style = null!;

        /// <summary>Creates real native controls and allows the EventSystem to activate the official input module.</summary>
        /// <returns>Frame boundaries for camera and Canvas initialization.</returns>
        [UnitySetUp]
        public IEnumerator CreateCanvas()
        {
            yield return null;
            CanvasUI.Dispose();
            GameInput.Reset();
            _input.Setup();
            _inputActive = true;
            _mouse = InputSystem.AddDevice<Mouse>();
            _screen = InputSystem.AddDevice<Touchscreen>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _renderer = new ObjectSpriteRenderer();
            _style = new GUIStyle { font = CanvasUI.DefaultFont, fontSize = 14 };
            Draw();
            _module = EventSystem.current.GetComponent<CanvasInputModule>();
            yield return null;
            yield return null;
            Assert.That(EventSystem.current.currentInputModule, Is.SameAs(_module));
            Assert.That(_module, Is.InstanceOf<InputSystemUIInputModule>());
        }

        /// <summary>Releases Canvas and action subscriptions before restoring devices.</summary>
        /// <returns>A frame for destruction of the owned native objects.</returns>
        [UnityTearDown]
        public IEnumerator ReleaseCanvas()
        {
            CanvasUI.Dispose();
            _renderer?.Dispose();
            yield return null;
            if (_inputActive)
            {
                GameInput.Reset();
                _input.TearDown();
                _inputActive = false;
            }
        }

        /// <summary>Builds the same retained button and text field for each input frame.</summary>
        /// <returns>Whether the original native button completed a click.</returns>
        private bool Draw()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            var clicked = CanvasUI.Button(new Rect(10f, 10f, 100f, 40f), GUIContent.none, _style);
            CanvasUI.SetNextControlName("input-system-name");
            CanvasUI.TextField(new Rect(10f, 70f, 200f, 40f), "Initial", 30, _style);
            CanvasUI.EndFrame();
            Canvas.ForceUpdateCanvases();
            return clicked;
        }

        /// <summary>Gets the native button's center in physical bottom-left coordinates.</summary>
        private static Vector2 ButtonPoint => new Vector2(60f, Screen.height - 30f);

        /// <summary>Checks a real device press releases exactly one native button click.</summary>
        /// <returns>One frame after the completed native click.</returns>
        [UnityTest]
        public IEnumerator MouseClickIsDispatchedOnce()
        {
            _input.Set(_mouse.position, ButtonPoint);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(_module.GetLastRaycastResult(_mouse.deviceId).gameObject, Is.Not.Null);
            Assert.That(Draw(), Is.False);
            _input.Release(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.True);
            Assert.That(Draw(), Is.False);
            yield return null;
        }

        /// <summary>Checks moving outside a native button cancels click eligibility.</summary>
        /// <returns>One frame after the rejected native click.</returns>
        [UnityTest]
        public IEnumerator DraggingOutDoesNotClick()
        {
            _input.Set(_mouse.position, ButtonPoint);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            _input.Set(_mouse.position, new Vector2(300f, Screen.height - 300f));
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.False);
            yield return null;
        }

        /// <summary>Checks canceled native touches do not click and remaining fingers cannot resume the interrupted capture.</summary>
        /// <returns>One frame after a fresh gesture is admitted.</returns>
        [UnityTest]
        public IEnumerator CanceledTouchWaitsForAllFingersWithoutClicking()
        {
            _input.BeginTouch(10, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            _input.BeginTouch(20, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            _input.CancelTouch(10, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.False);
            _input.EndTouch(20, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.False);
            yield return null;
            _module.Process();
            _input.BeginTouch(30, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            Assert.That(_module.GetLastRaycastResult(30).gameObject, Is.Not.Null);
            _input.EndTouch(30, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.True);
            yield return null;
        }

        /// <summary>Checks removing a captured screen cancels its press without a synthesized native click.</summary>
        /// <returns>One frame after the canceled native capture.</returns>
        [UnityTest]
        public IEnumerator RemovedTouchDeviceDoesNotClick()
        {
            _input.BeginTouch(10, ButtonPoint, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            InputSystem.RemoveDevice(_screen);
            _module.Process();
            Assert.That(Draw(), Is.False);
            yield return null;
        }

        /// <summary>Checks loss of EventSystem focus clears the press and requires a fresh mouse gesture.</summary>
        /// <returns>One frame after input focus returns.</returns>
        [UnityTest]
        public IEnumerator FocusLossDoesNotClickOnReturn()
        {
            _input.Set(_mouse.position, ButtonPoint);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            SetEventSystemFocus(false);
            _module.Process();
            _input.Release(_mouse.leftButton);
            yield return null;
            SetEventSystemFocus(true);
            _module.Process();
            Assert.That(Draw(), Is.False);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _module.Process();
            _input.Release(_mouse.leftButton);
            yield return null;
            _module.Process();
            Assert.That(Draw(), Is.True);
            yield return null;
        }

        /// <summary>Checks IME composition and retained text focus use the new keyboard without legacy input reads.</summary>
        /// <returns>A frame for native field focus activation.</returns>
        [UnityTest]
        public IEnumerator TextFieldFocusAndImeUseInputSystemKeyboard()
        {
            CanvasUI.FocusControl("input-system-name");
            yield return null;
            var field = Resources.FindObjectsOfTypeAll<InputField>().Single(item => item.gameObject.activeInHierarchy
                && item.transform.root.name == "PvZ Canvas UI");
            Assert.That(field.isFocused, Is.True);
            var composition = IMECompositionEvent.Create(_keyboard.deviceId, "测试", InputState.currentTime);
            InputSystem.QueueEvent(ref composition);
            yield return null;
            Assert.That(_module.input.compositionString, Is.EqualTo("测试"));
            field.text = "Edited";
            Draw();
            Assert.That(field.text, Is.EqualTo("Edited"));
            Assert.That(field.isFocused, Is.True);
            InputSystem.RemoveDevice(_keyboard);
            Assert.That(_module.input.compositionString, Is.Empty);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            composition = IMECompositionEvent.Create(_keyboard.deviceId, "新", InputState.currentTime);
            InputSystem.QueueEvent(ref composition);
            yield return null;
            Assert.That(_module.input.compositionString, Is.EqualTo("新"));
            field.DeactivateInputField();
            Assert.That(_module.input.compositionString, Is.Empty);
        }

        /// <summary>Checks touch cancellation ends native text-selection dragging.</summary>
        /// <returns>Frames allowing the native editable field to activate.</returns>
        [UnityTest]
        public IEnumerator CanceledTextSelectionEndsNativeDrag()
        {
            var point = new Vector2(60f, Screen.height - 90f);
            _input.BeginTouch(10, point, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            yield return null;
            _input.MoveTouch(10, point + new Vector2(80f, 0f), queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            var field = Resources.FindObjectsOfTypeAll<InputField>().Single(item => item.gameObject.activeInHierarchy
                && item.transform.root.name == "PvZ Canvas UI");
            var drag = typeof(InputField).GetField("m_UpdateDrag", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.That(drag.GetValue(field), Is.EqualTo(true));
            _input.CancelTouch(10, point, queueEventOnly: true, screen: _screen);
            yield return null;
            _module.Process();
            Assert.That(drag.GetValue(field), Is.EqualTo(false));
            Assert.That(field.isFocused, Is.False);
        }

        /// <summary>Simulates the EventSystem's native focus notification.</summary>
        /// <param name="focused">Whether the test application owns input focus.</param>
        private static void SetEventSystemFocus(bool focused)
        {
            typeof(EventSystem).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(EventSystem.current, new object[] { focused });
        }
    }
}
