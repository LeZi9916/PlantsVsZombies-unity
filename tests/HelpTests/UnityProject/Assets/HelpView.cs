using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PvZReplica.UI
{
    public enum HelpCloseResult { None, ReturnToMainMenu }

    /// <summary>
    /// Standalone main-menu help screen. The host owns navigation, input routing and
    /// the GUI matrix; all coordinates use a top-left-origin 800 x 600 canvas.
    /// </summary>
    public sealed class HelpView : IDisposable
    {
        public const float LogicalWidth = 800f;
        public const float LogicalHeight = 600f;
        public const int PageCount = 1;

        private static readonly Rect Canvas = new Rect(0f, 0f, 800f, 600f);
        private static readonly Rect ReturnBounds = new Rect(324f, 500f, 156f, 42f);

        private Texture2D lawn;
        private Texture2D paper;
        private Texture2D writing;
        private Texture2D button;
        private Texture2D buttonGlow;
        private readonly string returnLabel;
        private Font font;
        private GUIStyle labelStyle;
        private bool hovered;
        private bool captured;
        private bool disposed;

        public bool IsOpen { get; private set; }
        public HelpCloseResult CloseResult { get; private set; }
        public bool IsReturnHovered { get { return hovered; } }
        public bool IsReturnPressed { get { return captured && hovered; } }
        public static Rect ReturnButtonBounds { get { return ReturnBounds; } }

        /// <param name="resourceRoot">Filesystem directory containing images/ and properties/.</param>
        public HelpView(string resourceRoot)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            string root = Path.GetFullPath(resourceRoot);
            string images = Path.Combine(root, "images");
            try
            {
                returnLabel = ReadReturnLabel(Path.Combine(root, "properties", "LawnStrings.txt"));
                lawn = LoadTexture(images, "background1.jpg");
                paper = LoadMaskedTexture(images, "ZombieNote.jpg", "ZombieNote_.png");
                writing = LoadMaskedTexture(images, "ZombieNoteHelpBlack.png", "ZombieNoteHelp.png");
                button = LoadTexture(images, "SeedChooser_Button.png");
                buttonGlow = LoadTexture(images, "SeedChooser_Button_Glow.png");
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
            if (IsOpen) throw new InvalidOperationException("The help view is already open.");
            CloseResult = HelpCloseResult.None;
            hovered = false;
            captured = false;
            IsOpen = true;
        }

        /// <summary>Draw only during OnGUI, after the host maps the logical canvas to its viewport.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen) return;
            EnsureStyle();
            Color previous = GUI.color;
            GUI.BeginGroup(Canvas);
            try
            {
                GUI.color = Color.white;
                Image(new Rect(-700f, -300f, 2800f, 1200f), lawn);
                Image(new Rect(80f, 80f, paper.width, paper.height), paper);
                Image(new Rect(131f, 132f, writing.width, writing.height), writing);

                float offset = IsReturnPressed ? 1f : 0f;
                Rect buttonRect = new Rect(ReturnBounds.x + offset, ReturnBounds.y + offset,
                    ReturnBounds.width, ReturnBounds.height);
                Image(buttonRect, button);
                if (hovered) Image(buttonRect, buttonGlow);
                labelStyle.normal.textColor = new Color32(213, 159, 43, 255);
                GUI.Label(new Rect(buttonRect.x, buttonRect.y - 1f, buttonRect.width, buttonRect.height),
                    returnLabel, labelStyle);
            }
            finally
            {
                GUI.color = previous;
                GUI.EndGroup();
            }
        }

        /// <summary>
        /// Forward primary-button down, drag and up, plus mouse movement, in logical
        /// coordinates. All supported pointer events are consumed while open.
        /// </summary>
        public bool HandlePointer(Vector2 position, EventType type)
        {
            ThrowIfDisposed();
            if (!IsOpen) return false;
            if (type != EventType.MouseMove && type != EventType.MouseDown &&
                type != EventType.MouseDrag && type != EventType.MouseUp) return false;
            hovered = ReturnBounds.Contains(position);
            if (type == EventType.MouseDown) captured = hovered;
            else if (type == EventType.MouseUp)
            {
                bool activate = captured && hovered;
                captured = false;
                if (activate) Close();
            }
            return true;
        }

        /// <summary>Forward a key-down. Escape, Enter and Space return to the selector.</summary>
        public bool HandleKey(KeyCode key)
        {
            ThrowIfDisposed();
            if (!IsOpen) return false;
            if (key != KeyCode.Escape && key != KeyCode.Return &&
                key != KeyCode.KeypadEnter && key != KeyCode.Space) return false;
            Close();
            return true;
        }

        /// <summary>Requests return to the main menu, without navigating the host.</summary>
        public void Close()
        {
            ThrowIfDisposed();
            if (!IsOpen) return;
            IsOpen = false;
            CloseResult = HelpCloseResult.ReturnToMainMenu;
            hovered = false;
            captured = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsOpen = false;
            hovered = false;
            captured = false;
            Release(lawn);
            Release(paper);
            Release(writing);
            Release(button);
            Release(buttonGlow);
            Release(font);
            lawn = paper = writing = button = buttonGlow = null;
            font = null;
            labelStyle = null;
        }

        private void EnsureStyle()
        {
            if (labelStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS"
            }, 16);
            labelStyle = new GUIStyle(GUI.skin.label);
            if (font != null) labelStyle.font = font;
            labelStyle.fontSize = 16;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.wordWrap = false;
            labelStyle.richText = false;
            labelStyle.clipping = TextClipping.Clip;
            labelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        private static void Image(Rect bounds, Texture2D texture)
        {
            GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
        }

        private static string ReadReturnLabel(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            bool found = false;
            foreach (string line in lines)
            {
                string value = line.Trim();
                if (found)
                {
                    if (value.Length == 0) continue;
                    if (value[0] == '[' && value[value.Length - 1] == ']') break;
                    return value;
                }
                if (value == "[MAIN_MENU_BUTTON]") found = true;
            }
            throw new InvalidDataException("Missing [MAIN_MENU_BUTTON] in " + path);
        }

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
                    throw new InvalidDataException("Image and alpha mask dimensions differ: " + colorName);
                Color32[] colors = color.GetPixels32();
                Color32[] alphas = mask.GetPixels32();
                for (int i = 0; i < colors.Length; i++) colors[i].a = alphas[i].r;
                result = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false);
                result.name = colorName + "_masked";
                result.hideFlags = HideFlags.HideAndDontSave;
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                result.SetPixels32(colors);
                result.Apply(false, false);
                Texture2D completed = result;
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

        private static Texture2D LoadTexture(string images, string name)
        {
            string path = Path.Combine(images, name);
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                    throw new InvalidDataException("Unable to decode image: " + path);
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

        private static void Release(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(HelpView));
        }
    }
}
