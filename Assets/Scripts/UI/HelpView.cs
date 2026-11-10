#nullable enable
using PlantsVsZombies.Inputs;
using System;
using System.IO;
using System.Text;
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
        /// <summary>Stores lawn.</summary>
        private Texture2D _lawn;
        /// <summary>Stores paper.</summary>
        private Texture2D _paper;
        /// <summary>Stores writing.</summary>
        private Texture2D _writing;
        /// <summary>Stores button.</summary>
        private Texture2D _button;
        /// <summary>Stores button glow.</summary>
        private Texture2D _buttonGlow;
        /// <summary>Stores return label.</summary>
        private readonly string _returnLabel;
        /// <summary>Stores font.</summary>
        private Font _font;
        /// <summary>Stores label style.</summary>
        private GUIStyle _labelStyle;
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

            var root = Path.GetFullPath(resourceRoot);
            var images = Path.Combine(root, "images");
            try
            {
                _returnLabel = ReadReturnLabel(Path.Combine(root, "properties", "LawnStrings.txt"));
                _lawn = LoadTexture(images, "background1.jpg");
                _paper = LoadMaskedTexture(images, "ZombieNote.jpg", "ZombieNote_.png");
                _writing = LoadMaskedTexture(images, "ZombieNoteHelpBlack.png", "ZombieNoteHelp.png");
                _button = LoadTexture(images, "SeedChooser_Button.png");
                _buttonGlow = LoadTexture(images, "SeedChooser_Button_Glow.png");
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
                Image(new Rect(80f, 80f, _paper.width, _paper.height), _paper);
                Image(new Rect(131f, 132f, _writing.width, _writing.height), _writing);
                var offset = IsReturnPressed ? 1f : 0f;
                var buttonRect = new Rect(s_returnBounds.x + offset, s_returnBounds.y + offset, s_returnBounds.width, s_returnBounds.height);
                Image(buttonRect, _button);
                if (_hovered)
                {
                    Image(buttonRect, _buttonGlow);
                }

                _labelStyle.normal.textColor = new Color32(213, 159, 43, 255);
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
            Release(_lawn);
            Release(_paper);
            Release(_writing);
            Release(_button);
            Release(_buttonGlow);
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
        private static void Image(Rect bounds, Texture2D texture)
        {
            CanvasUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
        }

        /// <summary>Performs read return label.</summary>
        /// <param name = "path">The path.</param>
        /// <returns>The resulting read return label.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static string ReadReturnLabel(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
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

            throw new InvalidDataException("Missing [MAIN_MENU_BUTTON] in " + path);
        }

        /// <summary>Performs load masked texture.</summary>
        /// <param name = "images">The images.</param>
        /// <param name = "colorName">The color name.</param>
        /// <param name = "maskName">The mask name.</param>
        /// <returns>The resulting load masked texture.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static Texture2D LoadMaskedTexture(string images, string colorName, string maskName)
        {
            Texture2D color = null;
            Texture2D mask = null;
            Texture2D result = null;
            try
            {
                color = LoadTexture(images, colorName);
                mask = LoadTexture(images, maskName);
                if (color.width != mask.width || color.height != mask.height)
                {
                    throw new InvalidDataException("Image and alpha mask dimensions differ: " + colorName);
                }

                var colors = color.GetPixels32();
                var alphas = mask.GetPixels32();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i].a = alphas[i].r;
                }

                result = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false);
                result.name = colorName + "_masked";
                result.hideFlags = HideFlags.HideAndDontSave;
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                result.SetPixels32(colors);
                result.Apply(false, false);
                var completed = result;
                result = null;
                return completed;
            }
            finally
            {
                Release(result);
                Release(color);
                Release(mask);
            }
        }

        /// <summary>Performs load texture.</summary>
        /// <param name = "images">The images.</param>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting load texture.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static Texture2D LoadTexture(string images, string name)
        {
            var path = Path.Combine(images, name);
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    throw new InvalidDataException("Unable to decode image: " + path);
                }

                texture.name = name;
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch
            {
                Release(texture);
                throw;
            }
        }

        /// <summary>Performs release.</summary>
        /// <param name = "resource">The resource.</param>
        private static void Release(UnityEngine.Object resource)
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
