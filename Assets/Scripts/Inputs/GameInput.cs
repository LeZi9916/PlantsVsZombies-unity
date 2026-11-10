#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Samples native input once per game update without relying on GUI events.</summary>
    public static class GameInput
    {
        /// <summary>The stable primary finger and interruption state shared between game updates.</summary>
        private static readonly PrimaryTouchTracker s_touchTracker = new PrimaryTouchTracker();
        /// <summary>Whether the previous frame used native touches rather than mouse emulation.</summary>
        private static bool s_previousTouchFrame;
        /// <summary>Gets the most recently sampled input frame.</summary>
        public static GameInputSnapshot Current { get; private set; }

        /// <summary>Samples the primary touch or mouse, wheel, and navigation keys.</summary>
        /// <returns>The input snapshot to dispatch before simulation and presentation.</returns>
        public static GameInputSnapshot Capture()
        {
            var screen = (Vector2)UnityEngine.Input.mousePosition;
            var pressed = UnityEngine.Input.GetMouseButtonDown(0);
            var released = UnityEngine.Input.GetMouseButtonUp(0);
            var held = UnityEngine.Input.GetMouseButton(0);
            var touchCount = UnityEngine.Input.touchCount;
            s_touchTracker.BeginFrame();
            for (var index = 0; index < touchCount; index++)
            {
                var touch = UnityEngine.Input.GetTouch(index);
                s_touchTracker.AddTouch(touch.fingerId, touch.phase, touch.position);
            }

            var touchSample = s_touchTracker.CompleteFrame();
            var touchFrame = touchCount > 0 || touchSample.Released || s_previousTouchFrame;
            var position = GameViewport.ScreenToLogical(screen);
            if (touchFrame)
            {
                position = touchSample.Canceled || (!touchSample.Held && !touchSample.Released)
                    ? new Vector2(-1f, -1f)
                    : GameViewport.ScreenToLogical(touchSample.ScreenPosition);
                pressed = touchSample.Pressed;
                released = touchSample.Released;
                held = touchSample.Held;
            }

            if (Current.Held && !held && !released)
            {
                position = new Vector2(-1f, -1f);
                released = true;
            }

            s_previousTouchFrame = touchCount > 0;
            Current = new GameInputSnapshot(position, new Rect(0f, 0f, GameViewport.Width, GameViewport.Height).Contains(position),
                pressed, released, held, !touchFrame && UnityEngine.Input.GetMouseButtonDown(1), touchFrame ? 0f : -UnityEngine.Input.mouseScrollDelta.y * 3f,
                UnityEngine.Input.GetKeyDown(KeyCode.Escape), UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter));
            return Current;
        }
    }
}
