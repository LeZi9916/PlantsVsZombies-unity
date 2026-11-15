#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PlantsVsZombies.UI
{
    /// <summary>Supplies the legacy uGUI InputField's IME interface from the Input System keyboard.</summary>
    internal sealed class CanvasKeyboardInput : BaseInput
    {
        /// <summary>Stores the keyboard whose composition event is currently subscribed.</summary>
        private Keyboard? _keyboard;
        /// <summary>Stores the latest uncommitted composition text.</summary>
        private string _composition = string.Empty;
        /// <summary>Stores the IME mode requested by the selected native field.</summary>
        private IMECompositionMode _mode;
        /// <summary>Stores the candidate window position requested by the native field.</summary>
        private Vector2 _cursorPosition;

        /// <summary>Gets the Input System keyboard's current uncommitted composition.</summary>
        public override string compositionString
        {
            get
            {
                RefreshKeyboard();
                return _composition;
            }
        }

        /// <summary>Gets or sets IME activation while the native field gains or loses focus.</summary>
        public override IMECompositionMode imeCompositionMode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (value != IMECompositionMode.On)
                {
                    _composition = string.Empty;
                }
                RefreshKeyboard();
                _keyboard?.SetIMEEnabled(value == IMECompositionMode.On);
            }
        }

        /// <summary>Gets or sets the candidate window location using the field's existing screen coordinates.</summary>
        public override Vector2 compositionCursorPos
        {
            get => _cursorPosition;
            set
            {
                _cursorPosition = value;
                RefreshKeyboard();
                _keyboard?.SetIMECursorPosition(value);
            }
        }

        /// <summary>Subscribes composition events as soon as a native module is enabled.</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InputSystem.onDeviceChange += OnDeviceChanged;
            RefreshKeyboard();
        }

        /// <summary>Removes keyboard callbacks when the UI module is disabled or destroyed.</summary>
        protected override void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChanged;
            if (_keyboard != null)
            {
                _keyboard.onIMECompositionChange -= OnCompositionChanged;
                _keyboard.SetIMEEnabled(false);
            }
            _keyboard = null;
            _composition = string.Empty;
            base.OnDisable();
        }

        /// <summary>Adopts keyboard hot-plug changes before a new composition event can arrive.</summary>
        /// <param name="device">The connected, removed or reconfigured device.</param>
        /// <param name="change">The Input System's device lifecycle notification.</param>
        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard)
            {
                RefreshKeyboard();
            }
        }

        /// <summary>Adopts a replacement keyboard without retaining callbacks to a removed device.</summary>
        private void RefreshKeyboard()
        {
            var keyboard = Keyboard.current;
            if (_keyboard == keyboard)
            {
                return;
            }
            if (_keyboard != null)
            {
                _keyboard.onIMECompositionChange -= OnCompositionChanged;
            }
            _keyboard = keyboard;
            _composition = string.Empty;
            if (_keyboard != null)
            {
                _keyboard.onIMECompositionChange += OnCompositionChanged;
                _keyboard.SetIMEEnabled(_mode == IMECompositionMode.On);
                _keyboard.SetIMECursorPosition(_cursorPosition);
            }
        }

        /// <summary>Stores new composition text only when the native IME changes it.</summary>
        /// <param name="composition">The keyboard's current composition sequence.</param>
        private void OnCompositionChanged(IMECompositionString composition)
        {
            _composition = composition.ToString();
        }
    }
}
