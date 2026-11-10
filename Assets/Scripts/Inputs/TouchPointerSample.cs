#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Describes the primary touch selected from one native input frame.</summary>
    internal readonly struct TouchPointerSample
    {
        /// <summary>Gets the tracked touch position in bottom-left screen pixels.</summary>
        internal Vector2 ScreenPosition { get; }
        /// <summary>Gets whether the primary touch began this frame.</summary>
        internal bool Pressed { get; }
        /// <summary>Gets whether the primary touch ended or was canceled this frame.</summary>
        internal bool Released { get; }
        /// <summary>Gets whether the primary touch remains active.</summary>
        internal bool Held { get; }
        /// <summary>Gets whether cancellation must release captures outside all interactive rectangles.</summary>
        internal bool Canceled { get; }

        /// <summary>Creates the tracked touch transitions for a native frame.</summary>
        /// <param name="screenPosition">The tracked bottom-left pixel position.</param>
        /// <param name="pressed">Whether the touch began.</param>
        /// <param name="released">Whether the touch ended or was canceled.</param>
        /// <param name="held">Whether the touch remains active.</param>
        /// <param name="canceled">Whether interaction must be canceled rather than clicked.</param>
        internal TouchPointerSample(Vector2 screenPosition, bool pressed, bool released, bool held, bool canceled)
        {
            ScreenPosition = screenPosition;
            Pressed = pressed;
            Released = released;
            Held = held;
            Canceled = canceled;
        }
    }
}
