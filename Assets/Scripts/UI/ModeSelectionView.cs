#nullable enable
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Modes;
using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Defines mode selection view.</summary>
    public sealed class ModeSelectionView
    {
        /// <summary>Stores titles.</summary>
        private static readonly string[] s_titles =
        {
            "生存模式",
            "小游戏",
            "其他挑战",
            "解谜模式"
        };
        /// <summary>Stores texture.</summary>
        private readonly Func<string, SpriteImage?> _texture;
        /// <summary>Stores font.</summary>
        private readonly Font _font;
        /// <summary>Stores strings.</summary>
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores label.</summary>
        private GUIStyle? _label;
        /// <summary>Stores category.</summary>
        private int _category;
        /// <summary>Stores locked notice.</summary>
        private string? _lockedNotice;
        /// <summary>Stores the original Layout.xml character-count split, with native zero meaning no automatic split.</summary>
        private readonly int _survivalAutoWrap;
        /// <summary>Reuses the original centered tooltip request for locked survival previews.</summary>
        private readonly OriginalTooltipRequest _survivalTooltip = new OriginalTooltipRequest { Centered = true };
        /// <summary>Gets or sets the original locked-card tooltip key lookup.</summary>
        public Func<int, string>? LockTooltipKeyLookup { get; set; }
        /// <summary>Gets or sets the host-owned original tooltip renderer.</summary>
        public Func<OriginalTooltipRequest, bool>? TooltipRenderer { get; set; }

        /// <summary>Stores the enabled survival button owning the current native press.</summary>
        private int _pressedSurvivalButton = -1;
        /// <summary>Gets whether the native survival directory is open.</summary>
        internal bool IsSurvivalPage => _category == 0;
        /// <summary>Gets or sets the PC 439C20 accomplishment distance for each challenge.</summary>
        public Func<int, int>? AccomplishmentsNeededLookup { get; set; }

        /// <summary>Gets or sets PC 43AC50's enabled-button press sample callback.</summary>
        public Action? ButtonPressed { get; set; }
        /// <summary>Gets or sets launch requested.</summary>
        public Func<int, bool>? LaunchRequested { get; set; }
        /// <summary>Gets or sets can launch.</summary>
        public Func<int, bool>? CanLaunch { get; set; }
        /// <summary>Gets or sets lock reason lookup.</summary>
        public Func<int, string?>? LockReasonLookup { get; set; }
        /// <summary>Gets or sets progress lookup.</summary>
        public Func<int, int>? ProgressLookup { get; set; }
        /// <summary>Gets or sets original centered label.</summary>
        public Func<string, bool, float, float, float, Color, bool>? OriginalCenteredLabel { get; set; }
        /// <summary>Gets or sets closed.</summary>
        public bool Closed { get; private set; }

        /// <summary>Initializes mode selection view.</summary>
        /// <param name = "root">The root.</param>
        /// <param name = "textureLoader">The texture loader.</param>
        /// <param name = "displayFont">The display font.</param>
        /// <summary>Creates a mode view from a legacy externally owned texture lookup.</summary>
        /// <param name="root">The original resource directory.</param>
        /// <param name="textureLoader">The externally owned texture lookup.</param>
        /// <param name="displayFont">The fallback display font.</param>
        public ModeSelectionView(string root, Func<string, Texture2D?> textureLoader, Font displayFont)
            : this(root, (Func<string, SpriteImage?>)(name =>
            {
                var texture = textureLoader(name);
                return texture == null ? null : SpriteImage.FromTexture(texture);
            }), displayFont)
        {
        }

        public ModeSelectionView(string root, Func<string, SpriteImage?> textureLoader, Font displayFont)
        {
            _texture = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
            _font = displayFont;
            using var assets = SpriteAssetProvider.ForRoot(root);
            if (assets.TryReadText("properties/Layout.xml", out var layout))
            {
                foreach (var property in XElement.Parse(layout).Descendants("Integer"))
                {
                    if ((string?)property.Attribute("id") == "CHALLENGE_SCREEN_BUTTON_AUTO_WRAP_NUM"
                        && int.TryParse(property.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                    {
                        _survivalAutoWrap = Math.Max(0, count);
                    }
                }
            }
            if (assets.TryReadText("properties/LawnStrings.txt", out var text))
            {
                ReadStrings(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
            }
        }

        /// <summary>Performs open.</summary>
        /// <param name = "categoryIndex">The category index.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public void Open(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= s_titles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(categoryIndex));
            }

            _category = categoryIndex;
            Closed = false;
            _lockedNotice = null;
            _pressedSurvivalButton = -1;
        }

        /// <summary>Performs handle click.</summary>
        /// <param name = "point">The point.</param>
        public void HandleClick(Vector2 point)
        {
            if (IsSurvivalPage)
            {
                HandlePointer(point, PointerPhase.Down);
                HandlePointer(point, PointerPhase.Up);
                return;
            }
            if (_lockedNotice != null)
            {
                if (new Rect(310, 340, 180, 40).Contains(point))
                {
                    _lockedNotice = null;
                }

                return;
            }

            for (var i = 0; i < s_titles.Length; i++)
            {
                if (TabRect(i).Contains(point))
                {
                    _category = i;
                    return;
                }
            }

            if (new Rect(18, 568, 111, 26).Contains(point))
            {
                Closed = true;
                return;
            }

            foreach (ChallengeModeDefinition mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex != _category || !Playable(mode.Id) || !CardRect(mode).Contains(point))
                {
                    continue;
                }

                if (CanLaunch != null && !CanLaunch(mode.Id))
                {
                    _lockedNotice = LockReasonLookup == null ? "此挑战尚未解锁。" : LockReasonLookup(mode.Id);
                    return;
                }

                if (LaunchRequested != null)
                {
                    LaunchRequested(mode.Id);
                }

                return;
            }
        }

        /// <summary>Performs draw.</summary>
        public void Draw()
        {
            if (IsSurvivalPage)
            {
                DrawSurvivalDirectory();
                return;
            }
            DrawTexture("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            var titleColor = new Color(220f / 255f, 220f / 255f, 220f / 255f);
            if (OriginalCenteredLabel == null || !OriginalCenteredLabel(s_titles[_category], true, 400f, 58f, 560f, titleColor))
            {
                Label(new Rect(120, 28, 560, 58), s_titles[_category], 28, Color.white, TextAnchor.MiddleCenter, false);
            }

            var thumbnails = _texture("Challenge_Thumbnails.jpg");
            foreach (ChallengeModeDefinition mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex != _category || !Playable(mode.Id))
                {
                    continue;
                }

                var card = CardRect(mode);
                var image = mode.Unidentified0x04;
                if (thumbnails != null && image >= 0 && image < 22)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(card.x + 13, card.y + 4, 80, 65), thumbnails, new Rect(image / 22f, 0, 1f / 22f, 1));
                }

                DrawTexture(card.Contains(CanvasUI.PointerPosition) && (CanLaunch == null || CanLaunch(mode.Id)) ? "Challenge_Window_Highlight.png" : "Challenge_Window.png", new Rect(card.x - 6, card.y - 2, 118, 120));
                var textColor = new Color(42f / 255f, 42f / 255f, 90f / 255f);
                var name = Name(mode);
                if (OriginalCenteredLabel == null || !OriginalCenteredLabel(name, false, card.x + 52f, card.y + 96f, 98f, textColor))
                {
                    Label(new Rect(card.x + 3, card.y + 70, card.width - 6, 39), name, 13, textColor, TextAnchor.MiddleCenter, true);
                }

                if (CanLaunch != null && !CanLaunch(mode.Id))
                {
                    DrawTexture("Challenge_Blank.png", new Rect(card.x + 13, card.y + 4, 80, 65));
                    var lockImage = _texture("lock.png");
                    if (lockImage != null)
                    {
                        CanvasUI.DrawTexture(new Rect(card.x + 32, card.y + 10, 40, 48), lockImage, ScaleMode.ScaleToFit, true);
                    }
                }

                var record = ProgressLookup == null ? 0 : ProgressLookup(mode.Id);
                if (record > 0)
                {
                    ModeCatalog.TryGet(mode.Id, out var definition);
                    var complete = definition != null && !definition.Endless && (definition.Survival ? record >= (definition.Hard ? 10 : 5) : true);
                    var progress = complete ? "已完成" : definition != null && definition.Endless ? "纪录 " + record : "旗帜 " + record;
                    Label(new Rect(card.x - 3, card.y + 108, card.width + 6, 20), progress, 12, Color.yellow, TextAnchor.MiddleCenter, false);
                }
            }

            var back = new Rect(18, 568, 111, 26);
            OriginalUiWidgets.ImageButton(_texture, back, "Store_MainMenuButton.png", "Store_MainMenuButtonHighlight.png", "Store_MainMenuButtonDown.png", OriginalUiWidgets.Hovered(back), OriginalUiWidgets.Pressed(back));
            Label(new Rect(18, 568, 111, 26), "返回主菜单", 14, Color.white, TextAnchor.MiddleCenter, false);
            for (var i = 0; i < s_titles.Length; i++)
            {
                var tab = TabRect(i);
                var text = OriginalUiWidgets.Hovered(tab) ? new Color(220f / 255f, 220f / 255f, 0) : new Color(1, 240f / 255f, 0);
                if (!OriginalUiWidgets.Text(tab, s_titles[i], "BrianneTod12", text))
                {
                    Label(tab, s_titles[i], 16, text, TextAnchor.MiddleCenter, false);
                }
            }

            if (_lockedNotice != null)
            {
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, .5f));
                OriginalUiWidgets.DialogFrame(_texture, new Rect(210, 185, 380, 260), true, false);
                if (!OriginalUiWidgets.Text(new Rect(235, 235, 330, 88), _lockedNotice, "DwarvenTodcraft15", new Color(224f / 255f, 187f / 255f, 98f / 255f), true, true))
                {
                    Label(new Rect(235, 235, 330, 88), _lockedNotice, 20, Color.white, TextAnchor.MiddleCenter, true);
                }

                var okay = new Rect(310, 340, 180, 40);
                OriginalUiWidgets.StoneButton(_texture, okay, OriginalUiWidgets.Hovered(okay), OriginalUiWidgets.Pressed(okay));
                if (!OriginalUiWidgets.StoneLabel(okay, "知道了"))
                {
                    Label(okay, "知道了", 18, OriginalUiWidgets.StoneText, TextAnchor.MiddleCenter, false);
                }
            }
        }

        /// <summary>Preserves PC ButtonWidget press, release and drag-out semantics on the survival page.</summary>
        /// <param name="point">The original top-left logical pointer position.</param>
        /// <param name="phase">The ordered pointer transition.</param>
        public void HandlePointer(Vector2 point, PointerPhase phase)
        {
            var button = SurvivalButtonAt(point);
            if (phase == PointerPhase.Down)
            {
                _pressedSurvivalButton = button;
                if (button >= 0)
                {
                    ButtonPressed?.Invoke();
                }
            }
            else if (phase == PointerPhase.Up)
            {
                var pressed = _pressedSurvivalButton;
                _pressedSurvivalButton = -1;
                if (pressed < 0 || pressed != button)
                {
                    return;
                }
                if (button == 100)
                {
                    Closed = true;
                }
                else
                {
                    LaunchRequested?.Invoke(button);
                }
            }
        }

        /// <summary>Finds an enabled native survival button without making locked cards clickable.</summary>
        /// <param name="point">The logical pointer position.</param>
        /// <returns>The mode identifier, 100 for the back button, or minus one.</returns>
        private int SurvivalButtonAt(Vector2 point)
        {
            if (new Rect(18, 568, 111, 26).Contains(point))
            {
                return 100;
            }
            foreach (var mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.Id <= 15 && CardRect(mode).Contains(point) && (CanLaunch == null || CanLaunch(mode.Id)))
                {
                    return mode.Id;
                }
            }
            return -1;
        }

        /// <summary>Draws PC 43A780/439CB0 using the unchanged fifteen-entry PC challenge table.</summary>
        private void DrawSurvivalDirectory()
        {
            DrawTexture("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            var titleColor = new Color(220f / 255f, 220f / 255f, 220f / 255f);
            DrawDirectoryText(Text("PICK_AREA"), true, 400, 58, 560, titleColor);
            var trophies = 0;
            for (var id = 1; id <= 10; id++)
            {
                if ((ProgressLookup?.Invoke(id) ?? 0) >= (id <= 5 ? 5 : 10))
                {
                    trophies++;
                }
            }
            var counter = Text("NUMBER_OF_TROPHIES");
            var first = counter.IndexOf("%d", StringComparison.Ordinal);
            if (first >= 0)
            {
                counter = counter.Substring(0, first) + trophies + counter.Substring(first + 2);
                counter = counter.Replace("%d", "10");
            }
            OriginalUiWidgets.TextAtBaseline(counter, 739, 73, 110, "DwarvenTodcraft15", new Color(1f, 240f / 255f, 0f));
            var trophy = _texture("trophy.png");
            if (trophy != null)
            {
                CanvasUI.DrawTexture(new Rect(718, 26, trophy.Width * .5f, trophy.Height * .5f), trophy);
            }
            foreach (var mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.Id <= 15)
                {
                    DrawSurvivalCard(mode);
                }
            }
            var back = new Rect(18, 568, 111, 26);
            var down = _pressedSurvivalButton == 100 && back.Contains(CanvasUI.PointerPosition);
            OriginalUiWidgets.ImageButton(_texture, back, "SeedChooser_Button2.png", "SeedChooser_Button2_Glow.png", "SeedChooser_Button2_Glow.png", back.Contains(CanvasUI.PointerPosition), down);
            OriginalUiWidgets.TextAtBaseline(Text("BACK_TO_MENU"), back.center.x + (down ? 1 : 0), 586 + (down ? 1 : 0), 111,
                "BrianneTod12", new Color(42f / 255f, 42f / 255f, 90f / 255f));
            if (TooltipRenderer != null && LockTooltipKeyLookup != null)
            {
                foreach (var mode in VerifiedDataCatalog.ChallengeModes)
                {
                    if (mode.Id > 15 || CanLaunch == null || CanLaunch(mode.Id) || !CardRect(mode).Contains(CanvasUI.PointerPosition)
                        || (AccomplishmentsNeededLookup?.Invoke(mode.Id) ?? 2) > 1)
                    {
                        continue;
                    }
                    var card = CardRect(mode);
                    _survivalTooltip.Title = Text(LockTooltipKeyLookup(mode.Id));
                    _survivalTooltip.X = (int)card.center.x;
                    _survivalTooltip.Y = (int)card.y;
                    TooltipRenderer(_survivalTooltip);
                    break;
                }
            }
        }

        /// <summary>Draws one native card, including blank distant locks, gray next locks and original record overlays.</summary>
        /// <param name="mode">The unchanged PC menu metadata.</param>
        private void DrawSurvivalCard(ChallengeModeDefinition mode)
        {
            var card = CardRect(mode);
            var enabled = CanLaunch == null || CanLaunch(mode.Id);
            var over = enabled && card.Contains(CanvasUI.PointerPosition);
            var down = over && _pressedSurvivalButton == mode.Id;
            var x = card.x + (down ? 1 : 0);
            var y = card.y + (down ? 1 : 0);
            var needed = enabled ? 0 : Math.Max(1, AccomplishmentsNeededLookup?.Invoke(mode.Id) ?? 1);
            if (needed > 1)
            {
                DrawTexture("Challenge_Blank.png", new Rect(x, y, 104, 115));
                return;
            }
            var prior = CanvasUI.Color;
            if (!enabled)
            {
                CanvasUI.Color = prior * new Color(92f / 255f, 92f / 255f, 92f / 255f, 1f);
            }
            var image = mode.Unidentified0x04;
            var single = image == 11 ? "endless_day.jpg" : image == 12 ? "endless_night.jpg"
                : image == 13 ? "endless_fog.jpg" : image == 14 ? "endless_roof.jpg" : null;
            if (single != null)
            {
                DrawTexture(single, new Rect(x + 13, y + 4, 80, 65));
            }
            else
            {
                var thumbnails = _texture("Survival_Thumbnails.jpg");
                if (thumbnails != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(x + 13, y + 4, 80, 65), thumbnails, new Rect(image / 11f, 0, 1f / 11f, 1));
                }
            }
            CanvasUI.Color = prior;
            DrawTexture(over ? "Challenge_Window_Highlight.png" : "Challenge_Window.png", new Rect(x - 6, y - 2, 118, 120));
            var color = over ? new Color(250f / 255f, 40f / 255f, 40f / 255f) : new Color(42f / 255f, 42f / 255f, 90f / 255f);
            var caption = enabled ? Name(mode) : Text("CHALLENGE_DISABLED_BUTTON");
            var split = caption.IndexOf('&');
            var separator = split >= 0 ? 1 : 0;
            if (split < 0 && enabled && _survivalAutoWrap > 0 && caption.Length > _survivalAutoWrap)
            {
                split = _survivalAutoWrap;
            }
            if (split >= 0)
            {
                DrawDirectoryText(caption.Substring(0, split), false, x + 52, y + 88, 104, color);
                DrawDirectoryText(caption.Substring(split + separator), false, x + 52, y + 102, 104, color);
            }
            else
            {
                DrawDirectoryText(caption, false, x + 52, y + 96, 104, color);
            }
            var record = ProgressLookup?.Invoke(mode.Id) ?? 0;
            if (!enabled)
            {
                var imageLock = _texture("lock.png");
                if (imageLock != null)
                {
                    CanvasUI.DrawTexture(new Rect(x + 24, y + 9, imageLock.Width * .7f, imageLock.Height * .7f), imageLock);
                }
            }
            else if (record >= (mode.Id <= 5 ? 5 : 10) && mode.Id <= 10)
            {
                DrawTexture("MiniGame_trophy.png", new Rect(x - 6, y - 2, 72, 71));
            }
            else if (record > 0)
            {
                var flags = record == 1 ? Text("ONE_FLAG") : Text("COUNT_FLAGS").Replace("{COUNT}", record.ToString());
                OriginalUiWidgets.TextAtBaseline(flags, x + 48, y + 48, 104, "ContinuumBold14Outline", Color.white);
                OriginalUiWidgets.TextAtBaseline(flags, x + 48, y + 48, 104, "ContinuumBold14", Color.red);
            }
        }

        /// <summary>Draws the original challenge title or wrapped card caption at its PC baseline.</summary>
        /// <param name="text">The original localized caption.</param>
        /// <param name="title">Whether to use HouseofTerror28 rather than BrianneTod12.</param>
        /// <param name="center">The horizontal center.</param>
        /// <param name="baseline">The final baseline.</param>
        /// <param name="width">The original text width.</param>
        /// <param name="color">The original tint.</param>
        private void DrawDirectoryText(string text, bool title, float center, float baseline, float width, Color color)
        {
            if (OriginalCenteredLabel != null && OriginalCenteredLabel(text, title, center, baseline, width, color))
            {
                return;
            }
            if (!OriginalUiWidgets.TextAtBaseline(text, center, baseline, width, title ? "HouseofTerror28" : "BrianneTod12", color))
            {
                OriginalUiWidgets.Text(new Rect(center - width / 2, baseline - 30, width, 38), text, title ? "HouseofTerror28" : "BrianneTod12", color, true, true);
            }
        }

        /// <summary>Looks up original localized menu text without substituting invented captions.</summary>
        /// <param name="key">The unchanged LawnStrings key.</param>
        /// <returns>The original translation, or its resource key when unavailable.</returns>
        private string Text(string key)
        {
            return _strings.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>Performs playable.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting playable.</returns>
        private bool Playable(int id)
        {
            ModeDefinition? definition;
            if (ModeCatalog.TryGet(id, out definition))
            {
                return true;
            }

            return (id == 43 || id == 50) && CanLaunch != null && CanLaunch(id);
        }

        /// <summary>Performs card rect.</summary>
        /// <param name = "mode">The mode.</param>
        /// <returns>The resulting card rect.</returns>
        private static Rect CardRect(ChallengeModeDefinition mode)
        {
            return mode.UiCategoryIndex == 0 ? new Rect(38 + mode.UiGridColumn * 155, 125 + mode.UiGridRow * 145, 104, 115) : new Rect(38 + mode.UiGridColumn * 155, 93 + mode.UiGridRow * 119, 104, 115);
        }

        /// <summary>Performs tab rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting tab rect.</returns>
        private static Rect TabRect(int index)
        {
            return new Rect(100 * (index + 2), 540, 100, 75);
        }

        /// <summary>Performs name.</summary>
        /// <param name = "mode">The mode.</param>
        /// <returns>The resulting name.</returns>
        private string Name(ChallengeModeDefinition mode)
        {
            var key = mode.LocalizationKey.Trim('[', ']');
            string value;
            return _strings.TryGetValue(key, out value) && value.Length > 0 ? value : key;
        }

        /// <summary>Performs read strings.</summary>
        /// <param name = "lines">The lines.</param>
        private void ReadStrings(string[] lines)
        {
            string? key = null;
            var value = new StringBuilder();
            foreach (string line in lines)
            {
                var text = line.Trim();
                if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
                {
                    if (key != null)
                    {
                        _strings[key] = value.ToString().Trim();
                    }

                    key = text.Substring(1, text.Length - 2);
                    value.Length = 0;
                }
                else if (key != null && text.Length > 0)
                {
                    if (value.Length > 0)
                    {
                        value.Append('\n');
                    }

                    value.Append(text);
                }
            }

            if (key != null)
            {
                _strings[key] = value.ToString().Trim();
            }
        }

        /// <summary>Performs draw texture.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "rect">The rect.</param>
        private void DrawTexture(string name, Rect rect)
        {
            var image = _texture(name);
            if (image != null)
            {
                CanvasUI.DrawTexture(rect, image, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Performs fill.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        private static void Fill(Rect rect, Color color)
        {
            var old = CanvasUI.Color;
            CanvasUI.Color = color;
            CanvasUI.DrawTexture(rect, Texture2D.whiteTexture);
            CanvasUI.Color = old;
        }

        /// <summary>Performs label.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "size">The size.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "alignment">The alignment.</param>
        /// <param name = "wrap">The wrap.</param>
        private void Label(Rect rect, string text, int size, Color color, TextAnchor alignment, bool wrap)
        {
            if (_label == null)
            {
                _label = new GUIStyle();
            }

            _label.font = _font;
            _label.fontSize = size;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = alignment;
            _label.wordWrap = wrap;
            _label.normal.textColor = color;
            CanvasUI.Label(rect, text, _label);
        }
    }
}
