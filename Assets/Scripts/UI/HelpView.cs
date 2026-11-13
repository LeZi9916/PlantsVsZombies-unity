#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using System;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>
    /// Standalone main-menu help screen. The host owns navigation, input routing and
    /// the GUI matrix; all coordinates use a top-left-origin 800 x 600 canvas.
    /// </summary>
    public sealed class HelpView : IDisposable
    {
        /// <summary>Stores logical width.</summary>
        public const float LogicalWidth = 800f;
        /// <summary>Stores logical height.</summary>
        public const float LogicalHeight = 600f;
        /// <summary>Stores page count.</summary>
        public const int PageCount = 1;
        /// <summary>Stores canvas.</summary>
        private static readonly Rect s_canvas = new Rect(0f, 0f, 800f, 600f);
        /// <summary>Stores return bounds.</summary>
        private static readonly Rect s_returnBounds = new Rect(324f, 500f, 156f, 42f);
        /// <summary>The shared logical-image acquisition retained for this help visit.</summary>
        private SpriteAssetProvider? _assets;
        /// <summary>Gets the provider after successful view construction.</summary>
        /// <exception cref="InvalidOperationException">The image provider is unavailable.</exception>
        private SpriteAssetProvider RequiredAssets => _assets ?? throw new InvalidOperationException("The help image provider is unavailable.");
        /// <summary>Stores lawn.</summary>
        private SpriteImage? _lawn;
        /// <summary>Stores paper.</summary>
        private SpriteImage? _paper;

        /// <summary>Gets the paper, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private SpriteImage RequiredPaper => _paper ?? throw new System.InvalidOperationException("The member '_paper' is not available.");
        /// <summary>Stores writing.</summary>
        private SpriteImage? _writing;

        /// <summary>Gets the writing, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private SpriteImage RequiredWriting => _writing ?? throw new System.InvalidOperationException("The member '_writing' is not available.");
        /// <summary>Stores button.</summary>
        private SpriteImage? _button;
        /// <summary>Stores button glow.</summary>
        private SpriteImage? _buttonGlow;
        /// <summary>Stores return label.</summary>
        private readonly string _returnLabel;
        /// <summary>Stores font.</summary>
        private Font? _font;
        /// <summary>Stores label style.</summary>
        private GUIStyle? _labelStyle;

        /// <summary>Gets the label style, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private GUIStyle RequiredLabelStyle => _labelStyle ?? throw new System.InvalidOperationException("The member '_labelStyle' is not available.");
        /// <summary>Stores hovered.</summary>
        private bool _hovered;
        /// <summary>Stores captured.</summary>
        private bool _captured;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets is open.</summary>
        public bool IsOpen { get; private set; }
        /// <summary>Gets or sets close result.</summary>
        public HelpCloseResult CloseResult { get; private set; }

        /// <summary>Gets or sets is return hovered.</summary>
        public bool IsReturnHovered
        {
            get
            {
                return _hovered;
            }
        }

        /// <summary>Gets or sets is return pressed.</summary>
        public bool IsReturnPressed
        {
            get
            {
                return _captured && _hovered;
            }
        }

        /// <summary>Gets or sets return button bounds.</summary>
        public static Rect ReturnButtonBounds
        {
            get
            {
                return s_returnBounds;
            }
        }

        /// <param name = "resourceRoot">Filesystem directory containing images/ and properties/.</param>
        public HelpView(string resourceRoot)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
            {
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            }

            try
            {
                _assets = SpriteAssetProvider.ForRoot(resourceRoot);
                _returnLabel = ReadReturnLabel();
                _lawn = RequiredAssets.LoadImage("background1.jpg");
                _paper = RequiredAssets.LoadAlphaGrid("ZombieNote.jpg", "ZombieNote_.png");
                _writing = RequiredAssets.LoadAlphaGrid("ZombieNoteHelpBlack.png", "ZombieNoteHelp.png");
                _button = RequiredAssets.LoadImage("SeedChooser_Button.png");
                _buttonGlow = RequiredAssets.LoadImage("SeedChooser_Button_Glow.png");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>Begins a new visit and clears the previous close result.</summary>
        public void Open()
        {
            ThrowIfDisposed();
            if (IsOpen)
            {
                throw new InvalidOperationException("The help view is already open.");
            }

            CloseResult = HelpCloseResult.None;
            _hovered = false;
            _captured = false;
            IsOpen = true;
        }

        /// <summary>Submits native Canvas elements after the host maps its logical viewport.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return;
            }

            EnsureStyle();
            var previous = CanvasUI.Color;
            CanvasUI.BeginGroup(s_canvas);
            try
            {
                CanvasUI.Color = Color.white;
                Image(new Rect(-700f, -300f, 2800f, 1200f), _lawn);
                Image(new Rect(80f, 80f, RequiredPaper.Width, RequiredPaper.Height), _paper);
                Image(new Rect(131f, 132f, RequiredWriting.Width, RequiredWriting.Height), _writing);
                var offset = IsReturnPressed ? 1f : 0f;
                var buttonRect = new Rect(s_returnBounds.x + offset, s_returnBounds.y + offset, s_returnBounds.width, s_returnBounds.height);
                Image(buttonRect, _button);
                if (_hovered)
                {
                    Image(buttonRect, _buttonGlow);
                }

                RequiredLabelStyle.normal.textColor = new Color32(213, 159, 43, 255);
                CanvasUI.Label(new Rect(buttonRect.x, buttonRect.y - 1f, buttonRect.width, buttonRect.height), _returnLabel, _labelStyle);
            }
            finally
            {
                CanvasUI.Color = previous;
                CanvasUI.EndGroup();
            }
        }

        /// <summary>
        /// Forward primary-button down, drag and up, plus mouse movement, in logical
        /// coordinates. All supported pointer events are consumed while open.
        /// </summary>
        public bool HandlePointer(Vector2 position, PointerPhase type)
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return false;
            }

            if (type != PointerPhase.Move && type != PointerPhase.Down && type != PointerPhase.Drag && type != PointerPhase.Up)
            {
                return false;
            }

            _hovered = s_returnBounds.Contains(position);
            if (type == PointerPhase.Down)
            {
                _captured = _hovered;
            }
            else if (type == PointerPhase.Up)
            {
                var activate = _captured && _hovered;
                _captured = false;
                if (activate)
                {
                    Close();
                }
            }

            return true;
        }

        /// <summary>Forward a key-down. Escape, Enter and Space return to the selector.</summary>
        public bool HandleKey(KeyCode key)
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return false;
            }

            if (key != KeyCode.Escape && key != KeyCode.Return && key != KeyCode.KeypadEnter && key != KeyCode.Space)
            {
                return false;
            }

            Close();
            return true;
        }

        /// <summary>Requests return to the main menu, without navigating the host.</summary>
        public void Close()
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            CloseResult = HelpCloseResult.ReturnToMainMenu;
            _hovered = false;
            _captured = false;
        }

        /// <summary>Performs dispose.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            IsOpen = false;
            _hovered = false;
            _captured = false;
            _assets?.Dispose();
            _assets = null;
            Release(_font);
            _lawn = _paper = _writing = _button = _buttonGlow = null;
            _font = null;
            _labelStyle = null;
        }

        /// <summary>Performs ensure style.</summary>
        private void EnsureStyle()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei",
                "SimHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            }, 16);
            _labelStyle = new GUIStyle();
            if (_font != null)
            {
                _labelStyle.font = _font;
            }

            _labelStyle.fontSize = 16;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.wordWrap = false;
            _labelStyle.richText = false;
            _labelStyle.clipping = TextClipping.Clip;
            _labelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        /// <summary>Performs image.</summary>
        /// <param name = "bounds">The bounds.</param>
        /// <param name = "texture">The texture.</param>
        private static void Image(Rect bounds, SpriteImage? texture)
        {
            CanvasUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
        }

        /// <summary>Performs read return label.</summary>
        /// <returns>The original main-menu return label.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private string ReadReturnLabel()
        {
            if (!RequiredAssets.TryReadText("properties/LawnStrings.txt", out var text))
            {
                throw new FileNotFoundException("The original properties/LawnStrings.txt is required.");
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var found = false;
            foreach (string line in lines)
            {
                var value = line.Trim();
                if (found)
                {
                    if (value.Length == 0)
                    {
                        continue;
                    }

                    if (value[0] == '[' && value[value.Length - 1] == ']')
                    {
                        break;
                    }

                    return value;
                }

                if (value == "[MAIN_MENU_BUTTON]")
                {
                    found = true;
                }
            }

            throw new InvalidDataException("Missing [MAIN_MENU_BUTTON] in properties/LawnStrings.txt");
        }

        /// <summary>Performs release.</summary>
        /// <param name = "resource">The resource.</param>
        private static void Release(UnityEngine.Object? resource)
        {
            if (resource == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HelpView));
            }
        }
    }
}
