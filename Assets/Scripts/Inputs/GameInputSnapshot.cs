#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Contains one frame of mouse, touch, and navigation input.</summary>
    public readonly struct GameInputSnapshot
    {
        /// <summary>Gets the pointer position in original top-left coordinates.</summary>
        public Vector2 Position { get; }
        /// <summary>Gets whether the pointer is inside the playable viewport.</summary>
        public bool InsideViewport { get; }
        /// <summary>Gets whether the primary pointer was pressed this frame.</summary>
        public bool Pressed { get; }
        /// <summary>Gets whether the primary pointer was released this frame.</summary>
        public bool Released { get; }
        /// <summary>Gets whether the primary pointer remains pressed.</summary>
        public bool Held { get; }
        /// <summary>Gets whether the secondary mouse button was pressed this frame.</summary>
        public bool RightPressed { get; }
        /// <summary>Gets the wheel displacement, positive when scrolling down.</summary>
        public float ScrollDelta { get; }
        /// <summary>Gets the first primary pointer transition for compatibility with single-transition consumers.</summary>
        public PointerPhase Phase => Pressed ? PointerPhase.Down : Released ? PointerPhase.Up : Held ? PointerPhase.Drag : PointerPhase.Move;
        /// <summary>Gets the number of ordered pointer transitions, including a press and release sampled in one frame.</summary>
        public int PhaseCount => Pressed && Released ? 2 : 1;
        /// <summary>Gets whether Escape was pressed this frame.</summary>
        public bool EscapePressed { get; }
        /// <summary>Gets whether Return or keypad Enter was pressed this frame.</summary>
        public bool EnterPressed { get; }

        /// <summary>Creates a fully sampled input frame.</summary>
        /// <param name="position">The logical pointer position.</param>
        /// <param name="insideViewport">Whether the pointer lies inside the viewport.</param>
        /// <param name="pressed">Whether the primary pointer was pressed.</param>
        /// <param name="released">Whether the primary pointer was released.</param>
        /// <param name="held">Whether the primary pointer is held.</param>
        /// <param name="rightPressed">Whether the secondary mouse button was pressed.</param>
        /// <param name="scrollDelta">The downward wheel displacement.</param>
        /// <param name="escapePressed">Whether Escape was pressed.</param>
        /// <param name="enterPressed">Whether Return or keypad Enter was pressed.</param>
        public GameInputSnapshot(Vector2 position, bool insideViewport, bool pressed, bool released, bool held, bool rightPressed, float scrollDelta, bool escapePressed, bool enterPressed)
        {
            Position = position;
            InsideViewport = insideViewport;
            Pressed = pressed;
            Released = released;
            Held = held;
            RightPressed = rightPressed;
            ScrollDelta = scrollDelta;
            EscapePressed = escapePressed;
            EnterPressed = enterPressed;
        }

        /// <summary>Retrieves a pointer transition without allocating a temporary collection.</summary>
        /// <param name="index">The zero-based transition index within this snapshot.</param>
        /// <returns>The press before the release for a complete click, or the frame's single transition.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside this snapshot's transition count.</exception>
        public PointerPhase GetPhase(int index)
        {
            if (index < 0 || index >= PhaseCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index == 0 ? Phase : PointerPhase.Up;
        }
    }
}
