#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PlantsVsZombies.UI
{
    /// <summary>Uses the official Input System UI module while canceling interrupted native captures without clicks.</summary>
    public sealed class CanvasInputModule : InputSystemUIInputModule
    {
        /// <summary>Stores captures independently of the UI module's reused pointer event data.</summary>
        private readonly List<CanvasPointerCapture> _captures = new List<CanvasPointerCapture>();
        /// <summary>Remembers terminal touch controls already handled by the preceding UI process.</summary>
        private List<(TouchControl Control, int Id)> _previousCanceledTouches = new List<(TouchControl, int)>();
        /// <summary>Reuses storage for the current terminal touch controls.</summary>
        private List<(TouchControl Control, int Id)> _currentCanceledTouches = new List<(TouchControl, int)>();
        /// <summary>Waits for held devices to become neutral after interruption.</summary>
        private bool _waitForRelease;

        /// <summary>Connects native InputField composition to the Input System keyboard.</summary>
        protected override void Awake()
        {
            base.Awake();
            inputOverride = gameObject.AddComponent<CanvasKeyboardInput>();
            scrollDeltaPerTick = 1f;
        }

        /// <summary>Registers a native control while it owns a press or drag.</summary>
        /// <param name="capture">The control receiving the original pointer events.</param>
        internal void Track(CanvasPointerCapture capture)
        {
            if (!_captures.Contains(capture))
            {
                _captures.Add(capture);
            }
        }

        /// <summary>Forgets a native control after its pointer is released or disabled.</summary>
        /// <param name="capture">The released control.</param>
        internal void Untrack(CanvasPointerCapture capture)
        {
            _captures.Remove(capture);
        }

        /// <summary>Routes ordinary UI actions and suppresses interrupted touch and focus releases.</summary>
        public override void Process()
        {
            var canceledTouch = HasNewCanceledTouch();
            if (!eventSystem.isFocused)
            {
                CancelCaptures();
                _waitForRelease = true;
                return;
            }
            if (_waitForRelease)
            {
                if (!AnyPointerHeld())
                {
                    // Discard queued transitions before accepting a fresh gesture.
                    ResetActionsAndPointers();
                    _waitForRelease = false;
                }
                return;
            }
            if (canceledTouch || HasInterruptedCapture())
            {
                CancelCaptures();
                _waitForRelease = true;
                return;
            }
            base.Process();
        }

        /// <summary>Releases captured controls when the EventSystem is hidden or destroyed.</summary>
        protected override void OnDisable()
        {
            ReleaseCaptures();
            base.OnDisable();
        }

        /// <summary>Checks original Input System touch phases and device removal for active captures.</summary>
        /// <returns>Whether a capture lost its terminal release or was canceled.</returns>
        private bool HasInterruptedCapture()
        {
            foreach (var capture in _captures)
            {
                if (capture.Interrupted)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Detects each cancellation once, including controls that retain a terminal phase until reuse.</summary>
        /// <returns>Whether a new native touch cancellation was observed.</returns>
        private bool HasNewCanceledTouch()
        {
            _currentCanceledTouches.Clear();
            var canceled = false;
            var devices = InputSystem.devices;
            for (var device = 0; device < devices.Count; device++)
            {
                if (devices[device] is not Touchscreen screen)
                {
                    continue;
                }
                foreach (var touch in screen.touches)
                {
                    if (touch.phase.ReadValue() != TouchPhase.Canceled)
                    {
                        continue;
                    }
                    var terminal = (touch, touch.touchId.ReadValue());
                    _currentCanceledTouches.Add(terminal);
                    canceled |= !_previousCanceledTouches.Contains(terminal);
                }
            }
            var previous = _previousCanceledTouches;
            _previousCanceledTouches = _currentCanceledTouches;
            _currentCanceledTouches = previous;
            return canceled;
        }

        /// <summary>Sends pointer-up and end-drag without clicking, then clears pending UI action state.</summary>
        private void CancelCaptures()
        {
            if (_waitForRelease)
            {
                return;
            }
            ReleaseCaptures();
            ResetActionsAndPointers();
        }

        /// <summary>Releases controls before removing the official module's pointer records.</summary>
        private void ReleaseCaptures()
        {
            while (_captures.Count > 0)
            {
                var index = _captures.Count - 1;
                var capture = _captures[index];
                _captures.RemoveAt(index);
                capture.Cancel();
            }
        }

        /// <summary>Uses the official module lifecycle to discard canceled action and pointer state.</summary>
        private void ResetActionsAndPointers()
        {
            eventSystem.SetSelectedGameObject(null);
            enabled = false;
            enabled = true;
        }

        /// <summary>Checks held mouse buttons and live native touches before resuming input.</summary>
        /// <returns>Whether any pointer still belongs to the interrupted gesture.</returns>
        private static bool AnyPointerHeld()
        {
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                return true;
            }
            var devices = InputSystem.devices;
            for (var device = 0; device < devices.Count; device++)
            {
                if (devices[device] is not Touchscreen screen)
                {
                    continue;
                }
                foreach (var touch in screen.touches)
                {
                    var phase = touch.phase.ReadValue();
                    if (phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
