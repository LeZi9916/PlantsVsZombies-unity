#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlantsVsZombies.UI
{
    /// <summary>Routes native mouse, touch, and editable-field input without requiring legacy named axes.</summary>
    public sealed class CanvasInputModule : StandaloneInputModule
    {
        /// <summary>Whether remaining touches must end after an interrupted native gesture.</summary>
        private bool _waitForAllTouchesReleased;

        /// <summary>Activates the native pointer module without looking up optional InputManager axes.</summary>
        /// <returns>Whether this module is enabled in the active scene.</returns>
        public override bool ShouldActivateModule()
        {
            return isActiveAndEnabled;
        }

        /// <summary>Releases native captures without clicking when the application loses focus.</summary>
        public override void UpdateModule()
        {
            if (!eventSystem.isFocused)
            {
                CancelCapturedPointers();
                _waitForAllTouchesReleased = UnityEngine.Input.touchCount > 0;
                return;
            }

            base.UpdateModule();
        }

        /// <summary>Cancels interrupted touch captures before Unity can convert cancellation into a click.</summary>
        public override void Process()
        {
            if (_waitForAllTouchesReleased)
            {
                if (UnityEngine.Input.touchCount > 0)
                {
                    return;
                }

                _waitForAllTouchesReleased = false;
            }

            if (HasInterruptedTouch())
            {
                CancelCapturedPointers();
                _waitForAllTouchesReleased = UnityEngine.Input.touchCount > 0;
                return;
            }

            base.Process();
        }

        /// <summary>Ends native press and drag captures while clearing click eligibility.</summary>
        private void CancelCapturedPointers()
        {
            foreach (var pointer in m_PointerData.Values)
            {
                pointer.eligibleForClick = false;
                ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                if (pointer.dragging)
                {
                    ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
                }

                pointer.pointerPress = null;
                pointer.pointerClick = null;
                pointer.pointerDrag = null;
                pointer.dragging = false;
            }

            DeactivateModule();
        }

        /// <summary>Checks for native cancellation or a captured finger missing its terminal event.</summary>
        /// <returns>Whether native pointer captures must be released without clicking.</returns>
        private bool HasInterruptedTouch()
        {
            var count = UnityEngine.Input.touchCount;
            for (var index = 0; index < count; index++)
            {
                if (UnityEngine.Input.GetTouch(index).phase == TouchPhase.Canceled)
                {
                    return true;
                }
            }

            foreach (var pointer in m_PointerData)
            {
                if (pointer.Key < 0)
                {
                    continue;
                }

                var found = false;
                for (var index = 0; index < count; index++)
                {
                    if (UnityEngine.Input.GetTouch(index).fingerId == pointer.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
