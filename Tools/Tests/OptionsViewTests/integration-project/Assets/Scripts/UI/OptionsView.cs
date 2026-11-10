using System;
using System.Collections.Generic;
using System.IO;
using PvZReplica.UI.BitmapFonts;
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
        private static readonly Rect RestartDialog = new Rect(193f, 154f, 413f, 292f);
        private static readonly Rect ConfirmRestartButton = new Rect(224f, 366f, 163f, 46f);
        private static readonly Rect CancelRestartButton = new Rect(400f, 366f, 163f, 46f);

        private enum PointerCapture
        {
            None,
            Music,
            Sound,
            Return,
            Almanac,
            Restart,
            MainMenu,
            ConfirmRestart,
            CancelRestart
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
        private Texture2D buttonDownLeft;
        private Texture2D buttonDownMiddle;
        private Texture2D buttonDownRight;
        private Texture2D[] dialogParts;
        private readonly string imageDirectory;
        private ImGuiBitmapFont menuFont;
        private ImGuiBitmapFont menuHoverFont;
        private ImGuiBitmapFont returnFont;
        private ImGuiBitmapFont returnHoverFont;
        private ImGuiBitmapFont dialogTitleFont;
        private ImGuiBitmapFont dialogBodyFont;
        private readonly Dictionary<ImGuiBitmapFont, Dictionary<string, BitmapTextLayout>> buttonLayouts =
            new Dictionary<ImGuiBitmapFont, Dictionary<string, BitmapTextLayout>>();
        private bool buttonFontsLoaded;
        private Font font;
        private GUIStyle labelStyle;
        private PointerCapture capture;
        private float grabOffset;
        private Vector2 pointer = new Vector2(-1000f, -1000f);
        private bool selectorMode;
        private bool confirmingRestart;
        private bool disposed;

        public float MusicVolume { get; private set; }
        public float SoundVolume { get; private set; }
        public bool FullScreen { get; private set; }
        public bool IsOpen { get; private set; }
        public OptionsCloseResult CloseResult { get; private set; }
        public bool GameplayButtonsEnabled { get; set; }
        public Func<string, float, float, Color, bool> OriginalRightLabel { get; set; }

        /// <param name="resourceRoot">Directory containing the raw images subdirectory.</param>
        public OptionsView(string resourceRoot)
        {
            if (string.IsNullOrEmpty(resourceRoot))
                throw new ArgumentException("A resource root containing images is required.", "resourceRoot");

            string images = Path.Combine(resourceRoot, "images");
            imageDirectory = images;
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
                if (File.Exists(Path.Combine(images, "button_down_left.png")) &&
                    File.Exists(Path.Combine(images, "button_down_middle.png")) &&
                    File.Exists(Path.Combine(images, "button_down_right.png")))
                {
                    buttonDownLeft = LoadTexture(images, "button_down_left.png");
                    buttonDownMiddle = LoadTexture(images, "button_down_middle.png");
                    buttonDownRight = LoadTexture(images, "button_down_right.png");
                }
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
            selectorMode = !GameplayButtonsEnabled;
            confirmingRestart = false;
            CloseResult = OptionsCloseResult.None;
            capture = PointerCapture.None;
            pointer = new Vector2(-1000f, -1000f);
            IsOpen = true;
        }

        /// <summary>Cancels a pending confirmation, or returns the current draft to the owner.</summary>
        public void Close()
        {
            ThrowIfDisposed();
            if (confirmingRestart)
            {
                confirmingRestart = false;
                capture = PointerCapture.None;
                return;
            }
            Finish(OptionsCloseResult.ReturnToGame);
        }

        private void Finish(OptionsCloseResult result)
        {
            if (!IsOpen) return;
            capture = PointerCapture.None;
            confirmingRestart = false;
            IsOpen = false;
            CloseResult = result;
        }

        /// <summary>Call inside OnGUI after applying the owner's 800 x 600 GUI matrix.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen) return;
            EnsureLabelStyle();
            EnsureButtonFonts();
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
                float musicOffset = selectorMode ? 5f : 0f;
                float soundOffset = selectorMode ? 10f : 0f;
                float accelerationOffset = selectorMode ? 15f : 0f;
                float fullScreenOffset = selectorMode ? 20f : 0f;
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 119f + musicOffset, 186f, 23f), "音乐", PanelX + 186f, PanelY + 140f + musicOffset, labelColor);
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 146f + soundOffset, 186f, 23f), "音效", PanelX + 186f, PanelY + 167f + soundOffset, labelColor);
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 176f + accelerationOffset, 274f, 23f), "3D加速", PanelX + 274f, PanelY + 197f + accelerationOffset,
                    new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f));
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 208f + fullScreenOffset, 274f, 23f), "全屏幕显示", PanelX + 274f, PanelY + 229f + fullScreenOffset, labelColor);

                Rect musicTrack = OffsetY(MusicTrack, musicOffset);
                Rect soundTrack = OffsetY(SoundTrack, soundOffset);
                Rect accelerationBox = OffsetY(AccelerationBox, accelerationOffset);
                Rect fullScreenBox = OffsetY(FullScreenBox, fullScreenOffset);
                if (!selectorMode)
                {
                    DrawMenuButton(AlmanacButton, "查看图鉴", PointerCapture.Almanac);
                    DrawMenuButton(RestartButton, "重新开始本关卡", PointerCapture.Restart);
                    DrawMenuButton(MainMenuButton, "主菜单", PointerCapture.MainMenu);
                }
                DrawSlider(musicTrack, MusicVolume);
                DrawSlider(soundTrack, SoundVolume);
                Image(new Rect(accelerationBox.x, accelerationBox.y, 42f, 39f), uncheckedBox,
                    new Color(0.55f, 0.55f, 0.55f, 0.7f));
                Image(new Rect(fullScreenBox.x, fullScreenBox.y, 42f, 39f), FullScreen ? checkedBox : uncheckedBox, Color.white);
                DrawReturnButton(selectorMode ? "确定" : "返回游戏");
                if (confirmingRestart) DrawRestartConfirmation();
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
            if (confirmingRestart)
            {
                if (type == EventType.MouseDown)
                {
                    capture = ConfirmRestartButton.Contains(position) ? PointerCapture.ConfirmRestart :
                        CancelRestartButton.Contains(position) ? PointerCapture.CancelRestart : PointerCapture.None;
                }
                else if (type == EventType.MouseUp)
                {
                    PointerCapture released = capture;
                    capture = PointerCapture.None;
                    PointerCapture underPointer = ButtonUnderPointer();
                    if (released == PointerCapture.ConfirmRestart && underPointer == released)
                        Finish(OptionsCloseResult.RestartLevel);
                    else if (released == PointerCapture.CancelRestart && underPointer == released)
                        confirmingRestart = false;
                }
                return true;
            }
            if (type == EventType.MouseDown)
            {
                capture = PointerCapture.None;
                Rect musicTrack = OffsetY(MusicTrack, selectorMode ? 5f : 0f);
                Rect soundTrack = OffsetY(SoundTrack, selectorMode ? 10f : 0f);
                Rect accelerationBox = OffsetY(AccelerationBox, selectorMode ? 15f : 0f);
                Rect fullScreenBox = OffsetY(FullScreenBox, selectorMode ? 20f : 0f);
                // Child widgets receive input in reverse draw order, including overlapping bounds.
                if (ReturnButton.Contains(position)) capture = PointerCapture.Return;
                else if (fullScreenBox.Contains(position)) FullScreen = !FullScreen;
                else if (accelerationBox.Contains(position)) return true;
                else if (soundTrack.Contains(position))
                    BeginSlider(PointerCapture.Sound, soundTrack, SoundVolume, position);
                else if (musicTrack.Contains(position))
                    BeginSlider(PointerCapture.Music, musicTrack, MusicVolume, position);
                else if (!selectorMode && MainMenuButton.Contains(position)) capture = PointerCapture.MainMenu;
                else if (!selectorMode && RestartButton.Contains(position)) capture = PointerCapture.Restart;
                else if (!selectorMode && AlmanacButton.Contains(position)) capture = PointerCapture.Almanac;
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
                PointerCapture underPointer = ButtonUnderPointer();
                if (released == PointerCapture.Return && underPointer == released) Close();
                else if (released == PointerCapture.Almanac && underPointer == released) Finish(OptionsCloseResult.OpenAlmanac);
                else if (released == PointerCapture.Restart && underPointer == released)
                {
                    confirmingRestart = true;
                    LoadDialogParts();
                }
                else if (released == PointerCapture.MainMenu && underPointer == released) Finish(OptionsCloseResult.MainMenu);
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
            Release(buttonDownLeft);
            Release(buttonDownMiddle);
            Release(buttonDownRight);
            if (dialogParts != null)
                foreach (Texture2D part in dialogParts) Release(part);
            if (menuFont != null) menuFont.Dispose();
            if (menuHoverFont != null) menuHoverFont.Dispose();
            if (returnFont != null) returnFont.Dispose();
            if (returnHoverFont != null) returnHoverFont.Dispose();
            if (dialogTitleFont != null) dialogTitleFont.Dispose();
            if (dialogBodyFont != null) dialogBodyFont.Dispose();
            buttonLayouts.Clear();
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

        private static Rect OffsetY(Rect rect, float offset)
        {
            rect.y += offset;
            return rect;
        }

        private void DrawSlider(Rect track, float value)
        {
            Image(new Rect(track.x, track.y + 15f, track.width, 10f), sliderSlot, Color.white);
            Image(KnobRect(track, value), sliderKnob, Color.white);
        }

        private void DrawMenuButton(Rect rect, string text, PointerCapture target)
        {
            bool hovered = ButtonUnderPointer() == target;
            bool pressed = hovered && capture == target;
            float x = rect.x + (pressed ? 1f : 0f);
            Texture2D left = pressed && buttonDownLeft != null ? buttonDownLeft : buttonLeft;
            Texture2D middle = pressed && buttonDownMiddle != null ? buttonDownMiddle : buttonMiddle;
            Texture2D right = pressed && buttonDownRight != null ? buttonDownRight : buttonRight;
            Image(new Rect(x, rect.y, left.width, left.height), left, Color.white);
            int count = Mathf.RoundToInt((rect.width - left.width - right.width) / middle.width);
            for (int i = 0; i < count; i++)
                Image(new Rect(x + left.width + i * middle.width, rect.y, middle.width, middle.height), middle, Color.white);
            Image(new Rect(x + left.width + count * middle.width, rect.y, right.width, right.height), right, Color.white);
            ImGuiBitmapFont bitmap = hovered ? menuHoverFont : menuFont;
            int height = bitmap != null ? bitmap.Data.LineHeight : 18;
            float baseline = rect.y + (pressed ? 1f : 0f) + (int)((rect.height - height / 6 - 1f + height) / 2f) - 4f;
            if (!DrawCenteredBitmap(bitmap, text, rect, 1f + (pressed ? 1f : 0f), baseline, Color.white))
                Label(rect, text, 16, TextAnchor.MiddleCenter,
                    hovered ? new Color(0.32f, 0.78f, 0.18f) : new Color(0.19f, 0.52f, 0.12f));
        }

        private void DrawReturnButton(string text)
        {
            bool hovered = ButtonUnderPointer() == PointerCapture.Return;
            bool pressed = hovered && capture == PointerCapture.Return;
            Image(ReturnButton, pressed ? returnHighlighted : returnNormal, Color.white);
            ImGuiBitmapFont bitmap = hovered ? returnHoverFont : returnFont;
            int height = bitmap != null ? bitmap.Data.LineHeight : 41;
            float baseline = ReturnButton.y - 5f + (int)((ReturnButton.height - height / 6 - 1f + height) / 2f) + (pressed ? 1f : 0f);
            if (!DrawCenteredBitmap(bitmap, text, ReturnButton, -2f, baseline, Color.white))
                Label(new Rect(ReturnButton.x - 2f, ReturnButton.y - 5f, ReturnButton.width, ReturnButton.height),
                    text, 30, TextAnchor.MiddleCenter,
                    hovered ? new Color(0.32f, 0.78f, 0.18f) : new Color(0.19f, 0.52f, 0.12f));
        }

        private PointerCapture ButtonUnderPointer()
        {
            if (confirmingRestart)
            {
                if (ConfirmRestartButton.Contains(pointer)) return PointerCapture.ConfirmRestart;
                if (CancelRestartButton.Contains(pointer)) return PointerCapture.CancelRestart;
                return PointerCapture.None;
            }
            if (ReturnButton.Contains(pointer)) return PointerCapture.Return;
            if (OffsetY(FullScreenBox, selectorMode ? 20f : 0f).Contains(pointer) ||
                OffsetY(AccelerationBox, selectorMode ? 15f : 0f).Contains(pointer) ||
                OffsetY(SoundTrack, selectorMode ? 10f : 0f).Contains(pointer) ||
                OffsetY(MusicTrack, selectorMode ? 5f : 0f).Contains(pointer) || selectorMode)
                return PointerCapture.None;
            if (MainMenuButton.Contains(pointer)) return PointerCapture.MainMenu;
            if (RestartButton.Contains(pointer)) return PointerCapture.Restart;
            if (AlmanacButton.Contains(pointer)) return PointerCapture.Almanac;
            return PointerCapture.None;
        }

        private void EnsureButtonFonts()
        {
            if (buttonFontsLoaded) return;
            buttonFontsLoaded = true;
            try
            {
                menuFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft18GreenInset");
                menuHoverFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft18BrightGreenInset");
                returnFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft36GreenInset");
                returnHoverFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft36BrightGreenInset");
                dialogTitleFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft24");
                dialogBodyFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft15");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Options bitmap font unavailable: " + error.Message);
            }
        }

        private bool DrawCenteredBitmap(ImGuiBitmapFont bitmap, string text, Rect rect, float offset, float baseline, Color color)
        {
            if (bitmap == null) return false;
            Dictionary<string, BitmapTextLayout> layouts;
            if (!buttonLayouts.TryGetValue(bitmap, out layouts))
            {
                layouts = new Dictionary<string, BitmapTextLayout>();
                buttonLayouts.Add(bitmap, layouts);
            }
            BitmapTextLayout layout;
            if (!layouts.TryGetValue(text, out layout))
            {
                layout = bitmap.Layout(text);
                layouts.Add(text, layout);
            }
            if (layout.MissingGlyphs.Count != 0 || layout.LineCount != 1) return false;
            float x = rect.x + (int)((rect.width - layout.Width) / 2f) + offset;
            bitmap.Draw(layout, x, baseline - bitmap.Data.Base, color);
            return true;
        }

        private void LoadDialogParts()
        {
            if (dialogParts != null) return;
            string[] names = { "dialog_topleft.png", "dialog_topmiddle.png", "dialog_topright.png",
                "dialog_bigbottomleft.png", "dialog_bigbottommiddle.png", "dialog_bigbottomright.png", "dialog_header.png" };
            dialogParts = new Texture2D[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                if (!File.Exists(Path.Combine(imageDirectory, names[i]))) continue;
                try { dialogParts[i] = LoadTexture(imageDirectory, names[i]); }
                catch (Exception error) { Debug.LogWarning("Options dialog image unavailable: " + error.Message); }
            }
        }

        private void DrawRestartConfirmation()
        {
            if (dialogParts == null || Array.Exists(dialogParts, part => part == null))
            {
                Image(RestartDialog, Texture2D.whiteTexture, new Color(0.20f, 0.19f, 0.15f, 1f));
            }
            else
            {
                float x = RestartDialog.x;
                float top = RestartDialog.y + 45f;
                Image(new Rect(x, top, 107f, 97f), dialogParts[0], Color.white);
                for (int i = 0; i < 2; i++)
                    Image(new Rect(x + 107f + 93f * i, top, 93f, 97f), dialogParts[1], Color.white);
                Image(new Rect(x + 293f, top, 120f, 97f), dialogParts[2], Color.white);
                Image(new Rect(x, top + 97f, 107f, 148f), dialogParts[3], Color.white);
                for (int i = 0; i < 2; i++)
                    Image(new Rect(x + 107f + 93f * i, top + 97f, 93f, 150f), dialogParts[4], Color.white);
                Image(new Rect(x + 293f, top + 97f, 120f, 150f), dialogParts[5], Color.white);
                Image(new Rect(x + (int)((RestartDialog.width - 187f) / 2f) - 5f, RestartDialog.y, 187f, 64f), dialogParts[6], Color.white);
            }
            Color color = new Color(224f / 255f, 187f / 255f, 98f / 255f);
            if (!DrawCenteredBitmap(dialogTitleFont, "重新开始本关？", RestartDialog, 0f, 252f, color))
                Label(new Rect(210f, 222f, 380f, 40f), "重新开始本关？", 24, TextAnchor.MiddleCenter, color);
            if (!DrawCenteredBitmap(dialogBodyFont, "你想要再次从头", RestartDialog, 0f, 286f, color))
                Label(new Rect(210f, 265f, 380f, 25f), "你想要再次从头", 16, TextAnchor.MiddleCenter, color);
            if (!DrawCenteredBitmap(dialogBodyFont, "进行这一关卡么？", RestartDialog, 0f, 305f, color))
                Label(new Rect(210f, 284f, 380f, 25f), "进行这一关卡么？", 16, TextAnchor.MiddleCenter, color);
            DrawMenuButton(ConfirmRestartButton, "重新开始", PointerCapture.ConfirmRestart);
            DrawMenuButton(CancelRestartButton, "取消", PointerCapture.CancelRestart);
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

        private void NativeOrSystemLabel(Rect fallback, string text, float right, float baseline, Color color)
        {
            if (OriginalRightLabel == null || !OriginalRightLabel(text, right, baseline, color))
                Label(fallback, text, 18, TextAnchor.MiddleRight, color);
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
