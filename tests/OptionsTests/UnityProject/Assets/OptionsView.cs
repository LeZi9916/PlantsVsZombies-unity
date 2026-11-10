using System;
using System.IO;
using UnityEngine;

namespace PvZReplica.UI
{
    public enum OptionsCloseResult
    {
        None,
        ReturnToGame,
        OpenAlmanac,
        RestartLevel,
        MainMenu
    }

    /// <summary>
    /// A modal IMGUI view. Values are drafts; the owner applies them after ReturnToGame.
    /// All drawing and pointer positions use an 800 x 600, top-left-origin coordinate space.
    /// </summary>
    public sealed class OptionsView : IDisposable
    {
        public const float LogicalWidth = 800f;
        public const float LogicalHeight = 600f;

        private const float PanelX = 188f;
        private const float PanelY = 51f;
        private const float KnobWidth = 22f;
        private const float KnobHeight = 29f;

        private static readonly Rect Panel = new Rect(PanelX, PanelY, 423f, 498f);
        private static readonly Rect MusicTrack = new Rect(PanelX + 199f, PanelY + 116f, 135f, 40f);
        private static readonly Rect SoundTrack = new Rect(PanelX + 199f, PanelY + 143f, 135f, 40f);
        private static readonly Rect AccelerationBox = new Rect(PanelX + 283f, PanelY + 175f, 46f, 45f);
        private static readonly Rect FullScreenBox = new Rect(PanelX + 284f, PanelY + 206f, 46f, 45f);
        private static readonly Rect AlmanacButton = new Rect(PanelX + 107f, PanelY + 241f, 209f, 46f);
        private static readonly Rect RestartButton = new Rect(PanelX + 107f, PanelY + 284f, 209f, 46f);
        private static readonly Rect MainMenuButton = new Rect(PanelX + 107f, PanelY + 327f, 209f, 46f);
        private static readonly Rect ReturnButton = new Rect(PanelX + 30f, PanelY + 381f, 360f, 100f);

        private enum PointerCapture
        {
            None,
            Music,
            Sound,
            Return,
            Almanac,
            Restart,
            MainMenu
        }

        private Texture2D background;
        private Texture2D sliderSlot;
        private Texture2D sliderKnob;
        private Texture2D uncheckedBox;
        private Texture2D checkedBox;
        private Texture2D returnNormal;
        private Texture2D returnHighlighted;
        private Texture2D buttonLeft;
        private Texture2D buttonMiddle;
        private Texture2D buttonRight;
        private Font font;
        private GUIStyle labelStyle;
        private PointerCapture capture;
        private float grabOffset;
        private Vector2 pointer = new Vector2(-1000f, -1000f);
        private bool disposed;

        public float MusicVolume { get; private set; }
        public float SoundVolume { get; private set; }
        public bool FullScreen { get; private set; }
        public bool IsOpen { get; private set; }
        public OptionsCloseResult CloseResult { get; private set; }
        public bool GameplayButtonsEnabled { get; set; }

        /// <param name="resourceRoot">Directory containing the raw images subdirectory.</param>
        public OptionsView(string resourceRoot)
        {
            if (string.IsNullOrEmpty(resourceRoot))
                throw new ArgumentException("A resource root containing images is required.", "resourceRoot");

            string images = Path.Combine(resourceRoot, "images");
            try
            {
                background = LoadMaskedBackground(images);
                sliderSlot = LoadTexture(images, "options_sliderslot.png");
                sliderKnob = LoadTexture(images, "options_sliderknob2.png");
                uncheckedBox = LoadTexture(images, "options_checkbox0.png");
                checkedBox = LoadTexture(images, "options_checkbox1.png");
                returnNormal = LoadTexture(images, "options_backtogamebutton0.png");
                returnHighlighted = LoadTexture(images, "options_backtogamebutton2.png");
                buttonLeft = LoadTexture(images, "button_left.png");
                buttonMiddle = LoadTexture(images, "button_middle.png");
                buttonRight = LoadTexture(images, "button_right.png");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>Starts a new edit session without changing the owner's settings.</summary>
        public void Open(float musicVolume, float soundVolume, bool fullScreen)
        {
            ThrowIfDisposed();
            if (IsOpen) throw new InvalidOperationException("The options view is already open.");
            float music = ClampVolume(musicVolume, "musicVolume");
            float sound = ClampVolume(soundVolume, "soundVolume");
            MusicVolume = music;
            SoundVolume = sound;
            FullScreen = fullScreen;
            CloseResult = OptionsCloseResult.None;
            capture = PointerCapture.None;
            pointer = new Vector2(-1000f, -1000f);
            IsOpen = true;
        }

        /// <summary>Returns the current draft to the owner, including when invoked by Escape.</summary>
        public void Close()
        {
            ThrowIfDisposed();
            Finish(OptionsCloseResult.ReturnToGame);
        }

        private void Finish(OptionsCloseResult result)
        {
            if (!IsOpen) return;
            capture = PointerCapture.None;
            IsOpen = false;
            CloseResult = result;
        }

        /// <summary>Call inside OnGUI after applying the owner's 800 x 600 GUI matrix.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen) return;
            EnsureLabelStyle();
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                GUI.enabled = true;
                Image(Panel, background, Color.white);
                Color labelColor = new Color(107f / 255f, 109f / 255f, 145f / 255f, 1f);
                Label(new Rect(PanelX, PanelY + 119f, 186f, 23f), "音乐", 18, TextAnchor.MiddleRight, labelColor);
                Label(new Rect(PanelX, PanelY + 146f, 186f, 23f), "音效", 18, TextAnchor.MiddleRight, labelColor);
                Label(new Rect(PanelX, PanelY + 176f, 274f, 23f), "3D加速", 18, TextAnchor.MiddleRight,
                    new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f));
                Label(new Rect(PanelX, PanelY + 208f, 274f, 23f), "全屏幕显示", 18, TextAnchor.MiddleRight, labelColor);

                DrawSlider(MusicTrack, MusicVolume);
                DrawSlider(SoundTrack, SoundVolume);
                Image(new Rect(AccelerationBox.x, AccelerationBox.y, 42f, 39f), uncheckedBox,
                    new Color(0.55f, 0.55f, 0.55f, 0.7f));
                Image(new Rect(FullScreenBox.x, FullScreenBox.y, 42f, 39f), FullScreen ? checkedBox : uncheckedBox, Color.white);
                DrawMenuButton(AlmanacButton, "查看图鉴", GameplayButtonsEnabled);
                DrawMenuButton(RestartButton, "重新开始本关卡", GameplayButtonsEnabled);
                DrawMenuButton(MainMenuButton, "主菜单", GameplayButtonsEnabled);
                bool highlight = ReturnButton.Contains(pointer) &&
                    (capture == PointerCapture.None || capture == PointerCapture.Return);
                Image(ReturnButton, highlight ? returnHighlighted : returnNormal, Color.white);
                Label(new Rect(ReturnButton.x - 2f, ReturnButton.y - 5f, ReturnButton.width, ReturnButton.height),
                    "返回游戏", 30, TextAnchor.MiddleCenter,
                    highlight ? new Color(0.32f, 0.78f, 0.18f) : new Color(0.19f, 0.52f, 0.12f));
            }
            finally
            {
                GUI.color = oldColor;
                GUI.contentColor = oldContentColor;
                GUI.enabled = oldEnabled;
            }
        }

        /// <summary>
        /// Handles a left-button MouseDown/MouseDrag/MouseUp or a MouseMove in logical coordinates.
        /// Returns true for these events while open, including outside the panel, to preserve modality.
        /// </summary>
        public bool HandlePointer(Vector2 position, EventType type)
        {
            ThrowIfDisposed();
            if (!IsOpen) return false;
            if (type != EventType.MouseDown && type != EventType.MouseDrag &&
                type != EventType.MouseUp && type != EventType.MouseMove) return false;
            pointer = position;
            if (type == EventType.MouseDown)
            {
                capture = PointerCapture.None;
                if (FullScreenBox.Contains(position)) FullScreen = !FullScreen;
                else if (!AccelerationBox.Contains(position) && MusicTrack.Contains(position))
                    BeginSlider(PointerCapture.Music, MusicTrack, MusicVolume, position);
                else if (!AccelerationBox.Contains(position) && SoundTrack.Contains(position))
                    BeginSlider(PointerCapture.Sound, SoundTrack, SoundVolume, position);
                else if (GameplayButtonsEnabled && AlmanacButton.Contains(position)) capture = PointerCapture.Almanac;
                else if (GameplayButtonsEnabled && RestartButton.Contains(position)) capture = PointerCapture.Restart;
                else if (GameplayButtonsEnabled && MainMenuButton.Contains(position)) capture = PointerCapture.MainMenu;
                else if (ReturnButton.Contains(position)) capture = PointerCapture.Return;
            }
            else if (type == EventType.MouseDrag)
            {
                if (capture == PointerCapture.Music) MusicVolume = SliderDragValue(MusicTrack, position.x);
                else if (capture == PointerCapture.Sound) SoundVolume = SliderDragValue(SoundTrack, position.x);
            }
            else if (type == EventType.MouseUp)
            {
                PointerCapture released = capture;
                capture = PointerCapture.None;
                if (released == PointerCapture.Return && ReturnButton.Contains(position)) Close();
                else if (released == PointerCapture.Almanac && AlmanacButton.Contains(position)) Finish(OptionsCloseResult.OpenAlmanac);
                else if (released == PointerCapture.Restart && RestartButton.Contains(position)) Finish(OptionsCloseResult.RestartLevel);
                else if (released == PointerCapture.MainMenu && MainMenuButton.Contains(position)) Finish(OptionsCloseResult.MainMenu);
            }
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsOpen = false;
            capture = PointerCapture.None;
            Release(background);
            Release(sliderSlot);
            Release(sliderKnob);
            Release(uncheckedBox);
            Release(checkedBox);
            Release(returnNormal);
            Release(returnHighlighted);
            Release(buttonLeft);
            Release(buttonMiddle);
            Release(buttonRight);
            Release(font);
        }

        private void BeginSlider(PointerCapture target, Rect track, float value, Vector2 position)
        {
            Rect knob = KnobRect(track, value);
            if (position.x >= knob.x && position.x < knob.xMax)
            {
                capture = target;
                grabOffset = position.x - knob.x;
            }
            else
            {
                float updated = Mathf.Clamp01((position.x - track.x) / track.width);
                if (target == PointerCapture.Music) MusicVolume = updated;
                else SoundVolume = updated;
            }
        }

        private float SliderDragValue(Rect track, float x)
        {
            return Mathf.Clamp01((x - track.x - grabOffset) / (track.width - KnobWidth));
        }

        private static Rect KnobRect(Rect track, float value)
        {
            return new Rect(track.x + (int)((track.width - KnobWidth) * value),
                track.y + (int)((track.height - KnobHeight) / 2f), KnobWidth, KnobHeight);
        }

        private void DrawSlider(Rect track, float value)
        {
            Image(new Rect(track.x, track.y + 15f, track.width, 10f), sliderSlot, Color.white);
            Image(KnobRect(track, value), sliderKnob, Color.white);
        }

        private void DrawMenuButton(Rect rect, string text, bool enabled)
        {
            Color tint = enabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
            Image(new Rect(rect.x, rect.y, 36f, 46f), buttonLeft, tint);
            for (int i = 0; i < 3; i++)
                Image(new Rect(rect.x + 36f + i * 46f, rect.y, 46f, 46f), buttonMiddle, tint);
            Image(new Rect(rect.x + 174f, rect.y, 35f, 46f), buttonRight, tint);
            Label(rect, text, 16, TextAnchor.MiddleCenter,
                enabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f));
        }

        private static void Image(Rect rect, Texture2D texture, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private void EnsureLabelStyle()
        {
            if (labelStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS"
            }, 20);
            labelStyle = new GUIStyle(GUI.skin.label);
            if (font != null) labelStyle.font = font;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.wordWrap = false;
            labelStyle.richText = false;
            labelStyle.clipping = TextClipping.Clip;
            labelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        private void Label(Rect rect, string text, int size, TextAnchor alignment, Color color)
        {
            labelStyle.fontSize = size;
            labelStyle.alignment = alignment;
            labelStyle.normal.textColor = color;
            GUI.Label(rect, text, labelStyle);
        }

        private static Texture2D LoadMaskedBackground(string images)
        {
            Texture2D color = LoadTexture(images, "options_menuback.jpg");
            Texture2D mask = null;
            Texture2D result = null;
            try
            {
                mask = LoadTexture(images, "options_menuback_.png");
                if (color.width != mask.width || color.height != mask.height)
                    throw new InvalidDataException("The options background and alpha mask have different dimensions.");
                Color32[] pixels = color.GetPixels32();
                Color32[] alpha = mask.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i].a = alpha[i].r;
                result = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false);
                result.name = "options_menuback_rgba";
                result.hideFlags = HideFlags.HideAndDontSave;
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                result.SetPixels32(pixels);
                result.Apply(false, false);
                return result;
            }
            catch
            {
                Release(result);
                throw;
            }
            finally
            {
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
                if (!texture.LoadImage(bytes)) throw new InvalidDataException("Unable to decode image: " + path);
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

        private static float ClampVolume(float value, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter, "Volume must be finite.");
            return Mathf.Clamp01(value);
        }

        private static void Release(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException("OptionsView");
        }
    }
}
