#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PlantsVsZombies.UI
{
    /// <summary>Retains native press identities so interruption can release each control without a click.</summary>
    internal sealed class CanvasPointerCapture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
    {
        /// <summary>Stores one native pointer independently of the module's reused button event state.</summary>
        private struct Capture
        {
            /// <summary>Stores the official module's pointer event.</summary>
            internal PointerEventData Data;
            /// <summary>Stores the physical source device.</summary>
            internal InputDevice? Device;
            /// <summary>Stores the original touch identifier, or zero for a mouse.</summary>
            internal int TouchId;
            /// <summary>Stores whether native dragging has begun.</summary>
            internal bool Dragging;
        }

        /// <summary>Stores simultaneous pointers, including multiple fingers over the same control.</summary>
        private readonly List<Capture> _captures = new List<Capture>();
        /// <summary>Stores the owning official UI module.</summary>
        private CanvasInputModule? _module;

        /// <summary>Gets whether any captured device or touch disappeared or was explicitly canceled.</summary>
        internal bool Interrupted
        {
            get
            {
                foreach (var capture in _captures)
                {
                    if (capture.Device == null || !capture.Device.added || !capture.Device.enabled)
                    {
                        return true;
                    }
                    if (capture.Device is not Touchscreen screen)
                    {
                        continue;
                    }
                    var found = false;
                    foreach (var touch in screen.touches)
                    {
                        if (touch.touchId.ReadValue() != capture.TouchId)
                        {
                            continue;
                        }
                        found = true;
                        var phase = touch.phase.ReadValue();
                        if (phase == TouchPhase.Canceled || phase == TouchPhase.None)
                        {
                            return true;
                        }
                        break;
                    }
                    if (!found)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Retains the physical pointer while the control receives a left-button press.</summary>
        /// <param name="eventData">The official module's pointer event.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            _module = EventSystem.current?.currentInputModule as CanvasInputModule;
            _captures.Add(new Capture
            {
                Data = eventData,
                Device = (eventData as ExtendedPointerEventData)?.device,
                TouchId = (eventData as ExtendedPointerEventData)?.touchId ?? 0
            });
            _module?.Track(this);
        }

        /// <summary>Forgets a normally released press.</summary>
        /// <param name="eventData">The released pointer event.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            for (var i = _captures.Count - 1; i >= 0; i--)
            {
                if (_captures[i].Data.pointerId == eventData.pointerId)
                {
                    _captures.RemoveAt(i);
                }
            }
            if (_captures.Count == 0)
            {
                _module?.Untrack(this);
                _module = null;
            }
        }

        /// <summary>Retains drag ownership independently of reused UI event data.</summary>
        /// <param name="eventData">The native drag event.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            SetDragging(eventData.pointerId, true);
        }

        /// <summary>Clears the retained drag state.</summary>
        /// <param name="eventData">The completed native drag event.</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            SetDragging(eventData.pointerId, false);
        }

        /// <summary>Updates the drag state belonging to one pointer.</summary>
        /// <param name="pointerId">The official module's pointer identifier.</param>
        /// <param name="dragging">Whether that pointer is dragging the control.</param>
        private void SetDragging(int pointerId, bool dragging)
        {
            for (var i = 0; i < _captures.Count; i++)
            {
                var capture = _captures[i];
                if (capture.Data.pointerId == pointerId)
                {
                    capture.Dragging = dragging;
                    _captures[i] = capture;
                }
            }
        }

        /// <summary>Ends native presses and drags without invoking clicks or drops.</summary>
        internal void Cancel()
        {
            while (_captures.Count > 0)
            {
                var index = _captures.Count - 1;
                var capture = _captures[index];
                _captures.RemoveAt(index);
                capture.Data.button = PointerEventData.InputButton.Left;
                capture.Data.eligibleForClick = false;
                ExecuteEvents.Execute(gameObject, capture.Data, ExecuteEvents.pointerUpHandler);
                if (capture.Dragging)
                {
                    ExecuteEvents.Execute(gameObject, capture.Data, ExecuteEvents.endDragHandler);
                }
            }
            _module = null;
        }

        /// <summary>Releases captures before pooled controls are hidden or destroyed.</summary>
        private void OnDisable()
        {
            _module?.Untrack(this);
            Cancel();
        }
    }
}
