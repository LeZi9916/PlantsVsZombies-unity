#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Keeps one primary finger stable across touch reordering and releases interrupted gestures.</summary>
    internal sealed class PrimaryTouchTracker
    {
        /// <summary>The finger owning the current primary pointer gesture.</summary>
        private int? _fingerId;
        /// <summary>Whether this frame contains any native touches.</summary>
        private bool _hasTouches;
        /// <summary>Whether the tracked primary finger was found in this frame.</summary>
        private bool _foundPrimary;
        /// <summary>Whether remaining secondary fingers must be released before another gesture begins.</summary>
        private bool _waitForAllReleased;
        /// <summary>The primary transition assembled from this frame's native touches.</summary>
        private TouchPointerSample _sample;

        /// <summary>Starts sampling a new native touch frame without changing the captured finger.</summary>
        internal void BeginFrame()
        {
            _hasTouches = false;
            _foundPrimary = false;
            _sample = default;
        }

        /// <summary>Adds one native touch without relying on its position in Unity's touch list.</summary>
        /// <param name="fingerId">The stable native finger identity.</param>
        /// <param name="phase">The native touch transition.</param>
        /// <param name="screenPosition">The touch position in bottom-left screen pixels.</param>
        internal void AddTouch(int fingerId, TouchPhase phase, Vector2 screenPosition)
        {
            _hasTouches = true;
            if (!_fingerId.HasValue && !_waitForAllReleased && phase == TouchPhase.Began)
            {
                _fingerId = fingerId;
            }

            if (_fingerId != fingerId)
            {
                return;
            }

            _foundPrimary = true;
            var canceled = phase == TouchPhase.Canceled;
            var released = phase == TouchPhase.Ended || canceled;
            _sample = new TouchPointerSample(screenPosition, phase == TouchPhase.Began, released, !released, canceled);
            if (released)
            {
                _fingerId = null;
                _waitForAllReleased = true;
            }
        }

        /// <summary>Completes sampling and cancels a primary finger that disappeared without an end event.</summary>
        /// <returns>The transitions belonging to the original captured primary finger.</returns>
        internal TouchPointerSample CompleteFrame()
        {
            if (_fingerId.HasValue && !_foundPrimary)
            {
                _fingerId = null;
                _waitForAllReleased = true;
                _sample = new TouchPointerSample(Vector2.zero, false, true, false, true);
            }

            if (!_hasTouches)
            {
                _waitForAllReleased = false;
            }

            return _sample;
        }
    }
}
