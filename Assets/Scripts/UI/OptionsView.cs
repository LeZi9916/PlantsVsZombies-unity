#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>
    /// A modal Canvas view. Values are drafts; the owner applies them after ReturnToGame.
    /// All drawing and pointer positions use an 800 x 600, top-left-origin coordinate space.
    /// </summary>
    public sealed class OptionsView : IDisposable
    {
        /// <summary>Stores logical width.</summary>
        public const float LogicalWidth = 800f;
        /// <summary>Stores logical height.</summary>
        public const float LogicalHeight = 600f;
        /// <summary>Stores panel x.</summary>
        private const float PanelX = 188f;
        /// <summary>Stores panel y.</summary>
        private const float PanelY = 51f;
        /// <summary>Stores knob width.</summary>
        private const float KnobWidth = 22f;
        /// <summary>Stores knob height.</summary>
        private const float KnobHeight = 29f;
        /// <summary>Stores panel.</summary>
        private static readonly Rect s_panel = new Rect(PanelX, PanelY, 423f, 498f);
        /// <summary>Stores music track.</summary>
        private static readonly Rect s_musicTrack = new Rect(PanelX + 199f, PanelY + 116f, 135f, 40f);
        /// <summary>Stores sound track.</summary>
        private static readonly Rect s_soundTrack = new Rect(PanelX + 199f, PanelY + 143f, 135f, 40f);
        /// <summary>Stores acceleration box.</summary>
        private static readonly Rect s_accelerationBox = new Rect(PanelX + 283f, PanelY + 175f, 46f, 45f);
        /// <summary>Stores full screen box.</summary>
        private static readonly Rect s_fullScreenBox = new Rect(PanelX + 284f, PanelY + 206f, 46f, 45f);
        /// <summary>Stores almanac button.</summary>
        private static readonly Rect s_almanacButton = new Rect(PanelX + 107f, PanelY + 241f, 209f, 46f);
        /// <summary>Stores restart button.</summary>
        private static readonly Rect s_restartButton = new Rect(PanelX + 107f, PanelY + 284f, 209f, 46f);
        /// <summary>Stores main menu button.</summary>
        private static readonly Rect s_mainMenuButton = new Rect(PanelX + 107f, PanelY + 327f, 209f, 46f);
        /// <summary>Stores return button.</summary>
        private static readonly Rect s_returnButton = new Rect(PanelX + 30f, PanelY + 381f, 360f, 100f);
        /// <summary>Stores restart dialog.</summary>
        private static readonly Rect s_restartDialog = new Rect(193f, 154f, 413f, 292f);
        /// <summary>Stores confirm restart button.</summary>
        private static readonly Rect s_confirmRestartButton = new Rect(224f, 366f, 163f, 46f);
        /// <summary>Stores cancel restart button.</summary>
        private static readonly Rect s_cancelRestartButton = new Rect(400f, 366f, 163f, 46f);
        /// <summary>Defines pointer capture.</summary>
        private enum PointerCapture
        {
            /// <summary>Identifies none.</summary>
            None,
            /// <summary>Identifies music.</summary>
            Music,
            /// <summary>Identifies sound.</summary>
            Sound,
            /// <summary>Identifies return.</summary>
            Return,
            /// <summary>Identifies almanac.</summary>
            Almanac,
            /// <summary>Identifies restart.</summary>
            Restart,
            /// <summary>Identifies main menu.</summary>
            MainMenu,
            /// <summary>Identifies confirm restart.</summary>
            ConfirmRestart,
            /// <summary>Identifies cancel restart.</summary>
            CancelRestart
        }

        /// <summary>Stores background.</summary>
        private SpriteImage _background;
        /// <summary>Stores slider slot.</summary>
        private SpriteImage _sliderSlot;
        /// <summary>Stores slider knob.</summary>
        private SpriteImage _sliderKnob;
        /// <summary>Stores unchecked box.</summary>
        private SpriteImage _uncheckedBox;
        /// <summary>Stores checked box.</summary>
        private SpriteImage _checkedBox;
        /// <summary>Stores return normal.</summary>
        private SpriteImage _returnNormal;
        /// <summary>Stores return highlighted.</summary>
        private SpriteImage _returnHighlighted;
        /// <summary>Stores button left.</summary>
        private SpriteImage _buttonLeft;
        /// <summary>Stores button middle.</summary>
        private SpriteImage _buttonMiddle;
        /// <summary>Stores button right.</summary>
        private SpriteImage _buttonRight;
        /// <summary>Stores button down left.</summary>
        private SpriteImage? _buttonDownLeft;
        /// <summary>Stores button down middle.</summary>
        private SpriteImage? _buttonDownMiddle;
        /// <summary>Stores button down right.</summary>
        private SpriteImage? _buttonDownRight;
        /// <summary>Stores dialog parts.</summary>
        private SpriteImage?[]? _dialogParts;
        /// <summary>The shared logical-image acquisition retained by this view.</summary>
        private SpriteAssetProvider? _assets;
        /// <summary>Gets the acquired image provider after successful construction.</summary>
        /// <exception cref="InvalidOperationException">The image provider is unavailable.</exception>
        private SpriteAssetProvider RequiredAssets => _assets ?? throw new InvalidOperationException("The options image provider is unavailable.");
        /// <summary>Stores menu font.</summary>
        private CanvasBitmapFont? _menuFont;
        /// <summary>Stores menu hover font.</summary>
        private CanvasBitmapFont? _menuHoverFont;
        /// <summary>Stores return font.</summary>
        private CanvasBitmapFont? _returnFont;
        /// <summary>Stores return hover font.</summary>
        private CanvasBitmapFont? _returnHoverFont;
        /// <summary>Stores dialog title font.</summary>
        private CanvasBitmapFont? _dialogTitleFont;
        /// <summary>Stores dialog body font.</summary>
        private CanvasBitmapFont? _dialogBodyFont;
        /// <summary>Stores button layouts.</summary>
        private readonly Dictionary<CanvasBitmapFont, Dictionary<string, BitmapTextLayout>> _buttonLayouts = new Dictionary<CanvasBitmapFont, Dictionary<string, BitmapTextLayout>>();
        /// <summary>Stores button fonts loaded.</summary>
        private bool _buttonFontsLoaded;
        /// <summary>Stores font.</summary>
        private Font? _font;
        /// <summary>Stores label style.</summary>
        private GUIStyle? _labelStyle;

        /// <summary>Gets the label style, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private GUIStyle RequiredLabelStyle => _labelStyle ?? throw new System.InvalidOperationException("The member '_labelStyle' is not available.");
        /// <summary>Stores capture.</summary>
        private PointerCapture _capture;
        /// <summary>Stores grab offset.</summary>
        private float _grabOffset;
        /// <summary>Stores pointer.</summary>
        private Vector2 _pointer = new Vector2(-1000f, -1000f);
        /// <summary>Stores selector mode.</summary>
        private bool _selectorMode;
        /// <summary>Stores confirming restart.</summary>
        private bool _confirmingRestart;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets music volume.</summary>
        public float MusicVolume { get; private set; }
        /// <summary>Gets or sets sound volume.</summary>
        public float SoundVolume { get; private set; }
        /// <summary>Gets or sets full screen.</summary>
        public bool FullScreen { get; private set; }
        /// <summary>Gets or sets is open.</summary>
        public bool IsOpen { get; private set; }
        /// <summary>Gets or sets close result.</summary>
        public OptionsCloseResult CloseResult { get; private set; }
        /// <summary>Gets or sets gameplay buttons enabled.</summary>
        public bool GameplayButtonsEnabled { get; set; }
        /// <summary>Gets or sets original right label.</summary>
        public Func<string, float, float, Color, bool>? OriginalRightLabel { get; set; }

        /// <param name = "resourceRoot">Directory containing the raw images subdirectory.</param>
        public OptionsView(string resourceRoot)
        {
            if (string.IsNullOrEmpty(resourceRoot))
            {
                throw new ArgumentException("A resource root containing images is required.", "resourceRoot");
            }

            try
            {
                _assets = SpriteAssetProvider.ForRoot(resourceRoot);
                _background = RequiredAssets.LoadAlphaGrid("options_menuback.jpg", "options_menuback_.png");
                _sliderSlot = LoadTexture("options_sliderslot.png");
                _sliderKnob = LoadTexture("options_sliderknob2.png");
                _uncheckedBox = LoadTexture("options_checkbox0.png");
                _checkedBox = LoadTexture("options_checkbox1.png");
                _returnNormal = LoadTexture("options_backtogamebutton0.png");
                _returnHighlighted = LoadTexture("options_backtogamebutton2.png");
                _buttonLeft = LoadTexture("button_left.png");
                _buttonMiddle = LoadTexture("button_middle.png");
                _buttonRight = LoadTexture("button_right.png");
                var downLeft = TryLoadTexture("button_down_left.png");
                var downMiddle = TryLoadTexture("button_down_middle.png");
                var downRight = TryLoadTexture("button_down_right.png");
                if (downLeft != null && downMiddle != null && downRight != null)
                {
                    _buttonDownLeft = downLeft;
                    _buttonDownMiddle = downMiddle;
                    _buttonDownRight = downRight;
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
            if (IsOpen)
            {
                throw new InvalidOperationException("The options view is already open.");
            }

            var music = ClampVolume(musicVolume, "musicVolume");
            var sound = ClampVolume(soundVolume, "soundVolume");
            MusicVolume = music;
            SoundVolume = sound;
            FullScreen = fullScreen;
            _selectorMode = !GameplayButtonsEnabled;
            _confirmingRestart = false;
            CloseResult = OptionsCloseResult.None;
            _capture = PointerCapture.None;
            _pointer = new Vector2(-1000f, -1000f);
            IsOpen = true;
        }

        /// <summary>Cancels a pending confirmation, or returns the current draft to the owner.</summary>
        public void Close()
        {
            ThrowIfDisposed();
            if (_confirmingRestart)
            {
                _confirmingRestart = false;
                _capture = PointerCapture.None;
                return;
            }

            Finish(OptionsCloseResult.ReturnToGame);
        }

        /// <summary>Restores the suspended options draft after its modal almanac closes.</summary>
        public void ResumeFromAlmanac()
        {
            ThrowIfDisposed();
            if (IsOpen || CloseResult != OptionsCloseResult.OpenAlmanac)
            {
                throw new InvalidOperationException("The options view is not suspended for the almanac.");
            }

            _capture = PointerCapture.None;
            _pointer = new Vector2(-1000f, -1000f);
            CloseResult = OptionsCloseResult.None;
            IsOpen = true;
        }

        /// <summary>Performs finish.</summary>
        /// <param name = "result">The result.</param>
        private void Finish(OptionsCloseResult result)
        {
            if (!IsOpen)
            {
                return;
            }

            _capture = PointerCapture.None;
            _confirmingRestart = false;
            IsOpen = false;
            CloseResult = result;
        }

        /// <summary>Submits the retained Canvas elements in the owner's logical viewport.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return;
            }

            EnsureLabelStyle();
            EnsureButtonFonts();
            var oldColor = CanvasUI.Color;
            var oldContentColor = CanvasUI.ContentColor;
            var oldEnabled = CanvasUI.Enabled;
            try
            {
                CanvasUI.Color = Color.white;
                CanvasUI.ContentColor = Color.white;
                CanvasUI.Enabled = true;
                Image(s_panel, _background, Color.white);
                var labelColor = new Color(107f / 255f, 109f / 255f, 145f / 255f, 1f);
                var musicOffset = _selectorMode ? 5f : 0f;
                var soundOffset = _selectorMode ? 10f : 0f;
                var accelerationOffset = _selectorMode ? 15f : 0f;
                var fullScreenOffset = _selectorMode ? 20f : 0f;
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 119f + musicOffset, 186f, 23f), "音乐", PanelX + 186f, PanelY + 140f + musicOffset, labelColor);
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 146f + soundOffset, 186f, 23f), "音效", PanelX + 186f, PanelY + 167f + soundOffset, labelColor);
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 176f + accelerationOffset, 274f, 23f), "3D加速", PanelX + 274f, PanelY + 197f + accelerationOffset, new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f));
                NativeOrSystemLabel(new Rect(PanelX, PanelY + 208f + fullScreenOffset, 274f, 23f), "全屏幕显示", PanelX + 274f, PanelY + 229f + fullScreenOffset, labelColor);
                var musicTrack = OffsetY(s_musicTrack, musicOffset);
                var soundTrack = OffsetY(s_soundTrack, soundOffset);
                var accelerationBox = OffsetY(s_accelerationBox, accelerationOffset);
                var fullScreenBox = OffsetY(s_fullScreenBox, fullScreenOffset);
                if (!_selectorMode)
                {
                    DrawMenuButton(s_almanacButton, "查看图鉴", PointerCapture.Almanac);
                    DrawMenuButton(s_restartButton, "重新开始本关卡", PointerCapture.Restart);
                    DrawMenuButton(s_mainMenuButton, "主菜单", PointerCapture.MainMenu);
                }

                DrawSlider(musicTrack, MusicVolume);
                DrawSlider(soundTrack, SoundVolume);
                Image(new Rect(accelerationBox.x, accelerationBox.y, 42f, 39f), _uncheckedBox, new Color(0.55f, 0.55f, 0.55f, 0.7f));
                Image(new Rect(fullScreenBox.x, fullScreenBox.y, 42f, 39f), FullScreen ? _checkedBox : _uncheckedBox, Color.white);
                DrawReturnButton(_selectorMode ? "确定" : "返回游戏");
                if (_confirmingRestart)
                {
                    DrawRestartConfirmation();
                }
            }
            finally
            {
                CanvasUI.Color = oldColor;
                CanvasUI.ContentColor = oldContentColor;
                CanvasUI.Enabled = oldEnabled;
            }
        }

        /// <summary>
        /// Handles a left-button MouseDown/MouseDrag/MouseUp or a MouseMove in logical coordinates.
        /// Returns true for these events while open, including outside the panel, to preserve modality.
        /// </summary>
        public bool HandlePointer(Vector2 position, PointerPhase type)
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return false;
            }

            if (type != PointerPhase.Down && type != PointerPhase.Drag && type != PointerPhase.Up && type != PointerPhase.Move)
            {
                return false;
            }

            _pointer = position;
            if (_confirmingRestart)
            {
                if (type == PointerPhase.Down)
                {
                    _capture = s_confirmRestartButton.Contains(position) ? PointerCapture.ConfirmRestart : s_cancelRestartButton.Contains(position) ? PointerCapture.CancelRestart : PointerCapture.None;
                }
                else if (type == PointerPhase.Up)
                {
                    var released = _capture;
                    _capture = PointerCapture.None;
                    var underPointer = ButtonUnderPointer();
                    if (released == PointerCapture.ConfirmRestart && underPointer == released)
                    {
                        Finish(OptionsCloseResult.RestartLevel);
                    }
                    else if (released == PointerCapture.CancelRestart && underPointer == released)
                    {
                        _confirmingRestart = false;
                    }
                }

                return true;
            }

            if (type == PointerPhase.Down)
            {
                _capture = PointerCapture.None;
                var musicTrack = OffsetY(s_musicTrack, _selectorMode ? 5f : 0f);
                var soundTrack = OffsetY(s_soundTrack, _selectorMode ? 10f : 0f);
                var accelerationBox = OffsetY(s_accelerationBox, _selectorMode ? 15f : 0f);
                var fullScreenBox = OffsetY(s_fullScreenBox, _selectorMode ? 20f : 0f);
                // Child widgets receive input in reverse draw order, including overlapping bounds.
                if (s_returnButton.Contains(position))
                {
                    _capture = PointerCapture.Return;
                }
                else if (fullScreenBox.Contains(position))
                {
                    FullScreen = !FullScreen;
                }
                else if (accelerationBox.Contains(position))
                {
                    return true;
                }
                else if (soundTrack.Contains(position))
                {
                    BeginSlider(PointerCapture.Sound, soundTrack, SoundVolume, position);
                }
                else if (musicTrack.Contains(position))
                {
                    BeginSlider(PointerCapture.Music, musicTrack, MusicVolume, position);
                }
                else if (!_selectorMode && s_mainMenuButton.Contains(position))
                {
                    _capture = PointerCapture.MainMenu;
                }
                else if (!_selectorMode && s_restartButton.Contains(position))
                {
                    _capture = PointerCapture.Restart;
                }
                else if (!_selectorMode && s_almanacButton.Contains(position))
                {
                    _capture = PointerCapture.Almanac;
                }
            }
            else if (type == PointerPhase.Drag)
            {
                if (_capture == PointerCapture.Music)
                {
                    MusicVolume = SliderDragValue(s_musicTrack, position.x);
                }
                else if (_capture == PointerCapture.Sound)
                {
                    SoundVolume = SliderDragValue(s_soundTrack, position.x);
                }
            }
            else if (type == PointerPhase.Up)
            {
                var released = _capture;
                _capture = PointerCapture.None;
                var underPointer = ButtonUnderPointer();
                if (released == PointerCapture.Return && underPointer == released)
                {
                    Close();
                }
                else if (released == PointerCapture.Almanac && underPointer == released)
                {
                    Finish(OptionsCloseResult.OpenAlmanac);
                }
                else if (released == PointerCapture.Restart && underPointer == released)
                {
                    _confirmingRestart = true;
                    LoadDialogParts();
                }
                else if (released == PointerCapture.MainMenu && underPointer == released)
                {
                    Finish(OptionsCloseResult.MainMenu);
                }
            }

            return true;
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
            _capture = PointerCapture.None;
            _assets?.Dispose();
            _assets = null;
            _dialogParts = null;

            if (_menuFont != null)
            {
                _menuFont.Dispose();
            }

            if (_menuHoverFont != null)
            {
                _menuHoverFont.Dispose();
            }

            if (_returnFont != null)
            {
                _returnFont.Dispose();
            }

            if (_returnHoverFont != null)
            {
                _returnHoverFont.Dispose();
            }

            if (_dialogTitleFont != null)
            {
                _dialogTitleFont.Dispose();
            }

            if (_dialogBodyFont != null)
            {
                _dialogBodyFont.Dispose();
            }

            _buttonLayouts.Clear();
            Release(_font);
        }

        /// <summary>Performs begin slider.</summary>
        /// <param name = "target">The target.</param>
        /// <param name = "track">The track.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "position">The position.</param>
        private void BeginSlider(PointerCapture target, Rect track, float value, Vector2 position)
        {
            var knob = KnobRect(track, value);
            if (position.x >= knob.x && position.x < knob.xMax)
            {
                _capture = target;
                _grabOffset = position.x - knob.x;
            }
            else
            {
                var updated = Mathf.Clamp01((position.x - track.x) / track.width);
                if (target == PointerCapture.Music)
                {
                    MusicVolume = updated;
                }
                else
                {
                    SoundVolume = updated;
                }
            }
        }

        /// <summary>Performs slider drag value.</summary>
        /// <param name = "track">The track.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting slider drag value.</returns>
        private float SliderDragValue(Rect track, float x)
        {
            return Mathf.Clamp01((x - track.x - _grabOffset) / (track.width - KnobWidth));
        }

        /// <summary>Performs knob rect.</summary>
        /// <param name = "track">The track.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting knob rect.</returns>
        private static Rect KnobRect(Rect track, float value)
        {
            return new Rect(track.x + (int)((track.width - KnobWidth) * value), track.y + (int)((track.height - KnobHeight) / 2f), KnobWidth, KnobHeight);
        }

        /// <summary>Performs offset y.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "offset">The offset.</param>
        /// <returns>The resulting offset y.</returns>
        private static Rect OffsetY(Rect rect, float offset)
        {
            rect.y += offset;
            return rect;
        }

        /// <summary>Performs draw slider.</summary>
        /// <param name = "track">The track.</param>
        /// <param name = "value">The value.</param>
        private void DrawSlider(Rect track, float value)
        {
            Image(new Rect(track.x, track.y + 15f, track.width, 10f), _sliderSlot, Color.white);
            Image(KnobRect(track, value), _sliderKnob, Color.white);
        }

        /// <summary>Performs draw menu button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "target">The target.</param>
        private void DrawMenuButton(Rect rect, string text, PointerCapture target)
        {
            var hovered = ButtonUnderPointer() == target;
            var pressed = hovered && _capture == target;
            var x = rect.x + (pressed ? 1f : 0f);
            var left = pressed && _buttonDownLeft != null ? _buttonDownLeft : _buttonLeft;
            var middle = pressed && _buttonDownMiddle != null ? _buttonDownMiddle : _buttonMiddle;
            var right = pressed && _buttonDownRight != null ? _buttonDownRight : _buttonRight;
            Image(new Rect(x, rect.y, left.Width, left.Height), left, Color.white);
            var count = Mathf.RoundToInt((rect.width - left.Width - right.Width) / middle.Width);
            for (var i = 0; i < count; i++)
            {
                Image(new Rect(x + left.Width + i * middle.Width, rect.y, middle.Width, middle.Height), middle, Color.white);
            }

            Image(new Rect(x + left.Width + count * middle.Width, rect.y, right.Width, right.Height), right, Color.white);
            var bitmap = hovered ? _menuHoverFont : _menuFont;
            var height = bitmap != null ? bitmap.Data.LineHeight : 18;
            var baseline = rect.y + (pressed ? 1f : 0f) + (int)((rect.height - height / 6 - 1f + height) / 2f) - 4f;
            if (!DrawCenteredBitmap(bitmap, text, rect, 1f + (pressed ? 1f : 0f), baseline, Color.white))
            {
                Label(rect, text, 16, TextAnchor.MiddleCenter, hovered ? new Color(0.32f, 0.78f, 0.18f) : new Color(0.19f, 0.52f, 0.12f));
            }
        }

        /// <summary>Performs draw return button.</summary>
        /// <param name = "text">The text.</param>
        private void DrawReturnButton(string text)
        {
            var hovered = ButtonUnderPointer() == PointerCapture.Return;
            var pressed = hovered && _capture == PointerCapture.Return;
            Image(s_returnButton, pressed ? _returnHighlighted : _returnNormal, Color.white);
            var bitmap = hovered ? _returnHoverFont : _returnFont;
            var height = bitmap != null ? bitmap.Data.LineHeight : 41;
            var baseline = s_returnButton.y - 5f + (int)((s_returnButton.height - height / 6 - 1f + height) / 2f) + (pressed ? 1f : 0f);
            if (!DrawCenteredBitmap(bitmap, text, s_returnButton, -2f, baseline, Color.white))
            {
                Label(new Rect(s_returnButton.x - 2f, s_returnButton.y - 5f, s_returnButton.width, s_returnButton.height), text, 30, TextAnchor.MiddleCenter, hovered ? new Color(0.32f, 0.78f, 0.18f) : new Color(0.19f, 0.52f, 0.12f));
            }
        }

        /// <summary>Performs button under pointer.</summary>
        /// <returns>The resulting button under pointer.</returns>
        private PointerCapture ButtonUnderPointer()
        {
            if (_confirmingRestart)
            {
                if (s_confirmRestartButton.Contains(_pointer))
                {
                    return PointerCapture.ConfirmRestart;
                }

                if (s_cancelRestartButton.Contains(_pointer))
                {
                    return PointerCapture.CancelRestart;
                }

                return PointerCapture.None;
            }

            if (s_returnButton.Contains(_pointer))
            {
                return PointerCapture.Return;
            }

            if (OffsetY(s_fullScreenBox, _selectorMode ? 20f : 0f).Contains(_pointer) || OffsetY(s_accelerationBox, _selectorMode ? 15f : 0f).Contains(_pointer) || OffsetY(s_soundTrack, _selectorMode ? 10f : 0f).Contains(_pointer) || OffsetY(s_musicTrack, _selectorMode ? 5f : 0f).Contains(_pointer) || _selectorMode)
            {
                return PointerCapture.None;
            }

            if (s_mainMenuButton.Contains(_pointer))
            {
                return PointerCapture.MainMenu;
            }

            if (s_restartButton.Contains(_pointer))
            {
                return PointerCapture.Restart;
            }

            if (s_almanacButton.Contains(_pointer))
            {
                return PointerCapture.Almanac;
            }

            return PointerCapture.None;
        }

        /// <summary>Performs ensure button fonts.</summary>
        private void EnsureButtonFonts()
        {
            if (_buttonFontsLoaded)
            {
                return;
            }

            _buttonFontsLoaded = true;
            try
            {
                _menuFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft18GreenInset");
                _menuHoverFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft18BrightGreenInset");
                _returnFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft36GreenInset");
                _returnHoverFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft36BrightGreenInset");
                _dialogTitleFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft24");
                _dialogBodyFont = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft15");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Options bitmap font unavailable: " + error.Message);
            }
        }

        /// <summary>Performs draw centered bitmap.</summary>
        /// <param name = "bitmap">The bitmap.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "offset">The offset.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "color">The color.</param>
        /// <returns>The resulting draw centered bitmap.</returns>
        private bool DrawCenteredBitmap(CanvasBitmapFont? bitmap, string text, Rect rect, float offset, float baseline, Color color)
        {
            if (bitmap == null)
            {
                return false;
            }

            Dictionary<string, BitmapTextLayout> layouts;
            if (!_buttonLayouts.TryGetValue(bitmap, out layouts))
            {
                layouts = new Dictionary<string, BitmapTextLayout>();
                _buttonLayouts.Add(bitmap, layouts);
            }

            BitmapTextLayout layout;
            if (!layouts.TryGetValue(text, out layout))
            {
                layout = bitmap.Layout(text);
                layouts.Add(text, layout);
            }

            if (layout.MissingGlyphs.Count != 0 || layout.LineCount != 1)
            {
                return false;
            }

            var x = rect.x + (int)((rect.width - layout.Width) / 2f) + offset;
            bitmap.Draw(layout, x, baseline - bitmap.Data.Base, color);
            return true;
        }

        /// <summary>Performs load dialog parts.</summary>
        private void LoadDialogParts()
        {
            if (_dialogParts != null)
            {
                return;
            }

            string[] names =
            {
                "dialog_topleft.png",
                "dialog_topmiddle.png",
                "dialog_topright.png",
                "dialog_bigbottomleft.png",
                "dialog_bigbottommiddle.png",
                "dialog_bigbottomright.png",
                "dialog_header.png",
                "dialog_centerleft.png",
                "dialog_centermiddle.png",
                "dialog_centerright.png"
            };
            _dialogParts = new SpriteImage?[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                try
                {
                    _dialogParts[i] = TryLoadTexture(names[i]);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Options dialog image unavailable: " + error.Message);
                }
            }
        }

        /// <summary>Performs restart dialog texture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting restart dialog texture.</returns>
        private SpriteImage? RestartDialogTexture(string name)
        {
            string[] names =
            {
                "dialog_topleft.png",
                "dialog_topmiddle.png",
                "dialog_topright.png",
                "dialog_bigbottomleft.png",
                "dialog_bigbottommiddle.png",
                "dialog_bigbottomright.png",
                "dialog_header.png",
                "dialog_centerleft.png",
                "dialog_centermiddle.png",
                "dialog_centerright.png"
            };
            var index = Array.IndexOf(names, name);
            return _dialogParts != null && index >= 0 && index < _dialogParts.Length ? _dialogParts[index] : null;
        }

        /// <summary>Performs draw restart confirmation.</summary>
        private void DrawRestartConfirmation()
        {
            LoadDialogParts();
            OriginalUiWidgets.DialogFrame(RestartDialogTexture, new Rect(s_restartDialog.x, s_restartDialog.y + 45f, s_restartDialog.width, s_restartDialog.height - 45f), true, false);
            OriginalUiWidgets.Image(RestartDialogTexture("dialog_header.png"), new Rect(s_restartDialog.x + (int)((s_restartDialog.width - 187f) / 2f) - 5f, s_restartDialog.y, 187f, 64f));
            var color = new Color(224f / 255f, 187f / 255f, 98f / 255f);
            if (!DrawCenteredBitmap(_dialogTitleFont, "重新开始本关？", s_restartDialog, 0f, 252f, color))
            {
                Label(new Rect(210f, 222f, 380f, 40f), "重新开始本关？", 24, TextAnchor.MiddleCenter, color);
            }

            if (!DrawCenteredBitmap(_dialogBodyFont, "你想要再次从头", s_restartDialog, 0f, 286f, color))
            {
                Label(new Rect(210f, 265f, 380f, 25f), "你想要再次从头", 16, TextAnchor.MiddleCenter, color);
            }

            if (!DrawCenteredBitmap(_dialogBodyFont, "进行这一关卡么？", s_restartDialog, 0f, 305f, color))
            {
                Label(new Rect(210f, 284f, 380f, 25f), "进行这一关卡么？", 16, TextAnchor.MiddleCenter, color);
            }

            DrawMenuButton(s_confirmRestartButton, "重新开始", PointerCapture.ConfirmRestart);
            DrawMenuButton(s_cancelRestartButton, "取消", PointerCapture.CancelRestart);
        }

        /// <summary>Performs image.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "texture">The texture.</param>
        /// <param name = "color">The color.</param>
        private static void Image(Rect rect, SpriteImage texture, Color color)
        {
            var old = CanvasUI.Color;
            CanvasUI.Color = color;
            CanvasUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            CanvasUI.Color = old;
        }

        /// <summary>Performs ensure label style.</summary>
        private void EnsureLabelStyle()
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
            }, 20);
            _labelStyle = new GUIStyle();
            if (_font != null)
            {
                _labelStyle.font = _font;
            }

            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.wordWrap = false;
            _labelStyle.richText = false;
            _labelStyle.clipping = TextClipping.Clip;
            _labelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        /// <summary>Performs label.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "size">The size.</param>
        /// <param name = "alignment">The alignment.</param>
        /// <param name = "color">The color.</param>
        private void Label(Rect rect, string text, int size, TextAnchor alignment, Color color)
        {
            RequiredLabelStyle.fontSize = size;
            RequiredLabelStyle.alignment = alignment;
            RequiredLabelStyle.normal.textColor = color;
            CanvasUI.Label(rect, text, _labelStyle);
        }

        /// <summary>Performs native or system label.</summary>
        /// <param name = "fallback">The fallback.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "right">The right.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "color">The color.</param>
        private void NativeOrSystemLabel(Rect fallback, string text, float right, float baseline, Color color)
        {
            if (OriginalRightLabel == null || !OriginalRightLabel(text, right, baseline, color))
            {
                Label(fallback, text, 18, TextAnchor.MiddleRight, color);
            }
        }

        /// <summary>Loads a logical source image from the shared atlas provider.</summary>
        /// <param name="name">The original images filename.</param>
        /// <returns>The retained image handle with its original logical dimensions.</returns>
        /// <exception cref="FileNotFoundException">The original artwork is unavailable.</exception>
        private SpriteImage LoadTexture(string name)
        {
            return RequiredAssets.LoadImage(name);
        }

        /// <summary>Looks up optional original dialog artwork without requiring loose filesystem files.</summary>
        /// <param name="name">The original optional image filename.</param>
        /// <returns>The retained image, or null when the original artwork is absent.</returns>
        private SpriteImage? TryLoadTexture(string name)
        {
            return RequiredAssets.TryLoadImage(name, out var image) ? image : null;
        }

        /// <summary>Performs clamp volume.</summary>
        /// <param name = "value">The value.</param>
        /// <param name = "parameter">The parameter.</param>
        /// <returns>The resulting clamp volume.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static float ClampVolume(float value, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameter, "Volume must be finite.");
            }

            return Mathf.Clamp01(value);
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
                throw new ObjectDisposedException("OptionsView");
            }
        }
    }
}
