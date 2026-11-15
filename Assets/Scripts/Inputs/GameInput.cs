#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Samples Input System devices into the original game's logical input frame.</summary>
    public static class GameInput
    {
        /// <summary>Keeps the primary finger stable while other fingers enter and leave.</summary>
        private static readonly PrimaryTouchTracker s_touchTracker = new PrimaryTouchTracker();
        /// <summary>Suppresses mouse input for the frame after the final touch.</summary>
        private static bool s_previousTouchFrame;
        /// <summary>Tracks the Enhanced Touch reference owned by this game.</summary>
        private static bool s_ownsEnhancedTouch;
        /// <summary>Tracks application focus without accepting device reset releases as clicks.</summary>
        private static bool s_focused = true;
        /// <summary>Waits for a neutral mouse after an interrupted gesture.</summary>
        private static bool s_waitForMouseRelease;
        /// <summary>Gets the most recently sampled input frame.</summary>
        public static GameInputSnapshot Current { get; private set; }

        /// <summary>Releases input subscriptions and clears state between Play Mode sessions.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            Application.focusChanged -= SetFocus;
            Application.quitting -= Reset;
            if (s_ownsEnhancedTouch && EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();
            }
            s_ownsEnhancedTouch = false;
            s_previousTouchFrame = false;
            s_focused = true;
            s_waitForMouseRelease = false;
            s_touchTracker.Reset();
            Current = default;
        }

        /// <summary>Cancels pointer ownership when focus changes, waiting for held devices to become neutral.</summary>
        /// <param name="focused">Whether the application currently owns input focus.</param>
        internal static void SetFocus(bool focused)
        {
            s_focused = focused;
            s_waitForMouseRelease = true;
            s_touchTracker.Reset(waitForAllReleased: true);
        }

        /// <summary>Starts touch history before the first native input update can deliver a press.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Initialize()
        {
            if (!s_ownsEnhancedTouch)
            {
                EnhancedTouchSupport.Enable();
                s_ownsEnhancedTouch = true;
                Application.focusChanged += SetFocus;
                Application.quitting += Reset;
            }
        }

        /// <summary>Samples mouse, primary touch, wheel, and existing keyboard shortcuts without allocations.</summary>
        /// <returns>The original logical frame dispatched before simulation and presentation.</returns>
        public static GameInputSnapshot Capture()
        {
            Initialize();

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var screen = mouse?.position.ReadValue() ?? new Vector2(-1f, -1f);
            var pressed = mouse?.leftButton.wasPressedThisFrame ?? false;
            var released = mouse?.leftButton.wasReleasedThisFrame ?? false;
            var held = mouse?.leftButton.isPressed ?? false;
            if (s_waitForMouseRelease)
            {
                s_waitForMouseRelease = held;
                pressed = false;
                released = false;
                held = false;
            }

            var touches = EnhancedTouch.activeTouches;
            s_touchTracker.BeginFrame();
            for (var index = 0; index < touches.Count; index++)
            {
                var touch = touches[index];
                s_touchTracker.AddTouch(touch.touchId, touch.phase, touch.screenPosition);
            }
            var touchSample = s_touchTracker.CompleteFrame();
            var touchFrame = touches.Count > 0 || touchSample.Released || s_previousTouchFrame;
            var position = mouse == null ? new Vector2(-1f, -1f) : GameViewport.ScreenToLogical(screen);
            if (touchFrame)
            {
                position = touchSample.Canceled || (!touchSample.Held && !touchSample.Released)
                    ? new Vector2(-1f, -1f) : GameViewport.ScreenToLogical(touchSample.ScreenPosition);
                pressed = touchSample.Pressed;
                released = touchSample.Released;
                held = touchSample.Held;
            }

            if (!s_focused || Current.Held && !held && !released)
            {
                position = new Vector2(-1f, -1f);
                pressed = false;
                released = Current.Held;
                held = false;
            }

            s_previousTouchFrame = touches.Count > 0;
            Current = new GameInputSnapshot(position,
                new Rect(0f, 0f, GameViewport.Width, GameViewport.Height).Contains(position), pressed, released, held,
                s_focused && !touchFrame && (mouse?.rightButton.wasPressedThisFrame ?? false),
                !s_focused || touchFrame ? 0f : -(mouse?.scroll.ReadValue().y ?? 0f) * 3f,
                s_focused && (keyboard?.escapeKey.wasPressedThisFrame ?? false),
                s_focused && ((keyboard?.enterKey.wasPressedThisFrame ?? false) || (keyboard?.numpadEnterKey.wasPressedThisFrame ?? false)),
                s_focused && (keyboard?.spaceKey.wasPressedThisFrame ?? false),
                s_focused && (keyboard?.f9Key.wasPressedThisFrame ?? false));
            return Current;
        }
    }
}
