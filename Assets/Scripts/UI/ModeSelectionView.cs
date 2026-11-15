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
        /// <summary>Stores texture.</summary>
        private readonly Func<string, SpriteImage?> _texture;
        /// <summary>Stores strings.</summary>
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores category.</summary>
        private int _category;
        /// <summary>Stores the original Layout.xml character-count split, with native zero meaning no automatic split.</summary>
        private readonly int _directoryAutoWrap;
        /// <summary>Reuses the original centered tooltip request for locked challenge previews.</summary>
        private readonly OriginalTooltipRequest _directoryTooltip = new OriginalTooltipRequest { Centered = true };
        /// <summary>Gets or sets the original locked-card tooltip key lookup.</summary>
        public Func<int, string>? LockTooltipKeyLookup { get; set; }
        /// <summary>Gets or sets the host-owned original tooltip renderer.</summary>
        public Func<OriginalTooltipRequest, bool>? TooltipRenderer { get; set; }

        /// <summary>Stores the enabled challenge button owning the current native press.</summary>
        private int _pressedDirectoryButton = -1;
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
            using var assets = SpriteAssetProvider.ForRoot(root);
            if (assets.TryReadText("properties/Layout.xml", out var layout))
            {
                foreach (var property in XElement.Parse(layout).Descendants("Integer"))
                {
                    if ((string?)property.Attribute("id") == "CHALLENGE_SCREEN_BUTTON_AUTO_WRAP_NUM"
                        && int.TryParse(property.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                    {
                        _directoryAutoWrap = Math.Max(0, count);
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
            if (categoryIndex < 0 || categoryIndex >= 4)
            {
                throw new ArgumentOutOfRangeException(nameof(categoryIndex));
            }

            _category = categoryIndex;
            Closed = false;
            _pressedDirectoryButton = -1;
        }

        /// <summary>Performs handle click.</summary>
        /// <param name = "point">The point.</param>
        public void HandleClick(Vector2 point)
        {
            HandlePointer(point, PointerPhase.Down);
            HandlePointer(point, PointerPhase.Up);
        }

        /// <summary>Draws the original PC challenge directory for the selected category.</summary>
        public void Draw()
        {
            DrawDirectory();
        }

        /// <summary>Preserves PC ButtonWidget press, release and drag-out semantics on each challenge page.</summary>
        /// <param name="point">The original top-left logical pointer position.</param>
        /// <param name="phase">The ordered pointer transition.</param>
        public void HandlePointer(Vector2 point, PointerPhase phase)
        {
            var button = DirectoryButtonAt(point);
            if (phase == PointerPhase.Down)
            {
                _pressedDirectoryButton = button;
                if (button >= 0)
                {
                    ButtonPressed?.Invoke();
                }
            }
            else if (phase == PointerPhase.Up)
            {
                var pressed = _pressedDirectoryButton;
                _pressedDirectoryButton = -1;
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

        /// <summary>Finds an enabled native challenge button without making locked cards clickable.</summary>
        /// <param name="point">The logical pointer position.</param>
        /// <returns>The mode identifier, 100 for the back button, or minus one.</returns>
        private int DirectoryButtonAt(Vector2 point)
        {
            if (new Rect(18, 568, 111, 26).Contains(point))
            {
                return 100;
            }
            foreach (var mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex == _category && Playable(mode.Id) && CardRect(mode).Contains(point) && (CanLaunch == null || CanLaunch(mode.Id)))
                {
                    return mode.Id;
                }
            }
            return -1;
        }

        /// <summary>Draws PC 43A780/439CB0 using the unchanged PC challenge table.</summary>
        private void DrawDirectory()
        {
            DrawTexture("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            var titleColor = new Color(220f / 255f, 220f / 255f, 220f / 255f);
            DrawDirectoryText(Text(_category == 0 ? "PICK_AREA" : _category == 3 ? "SCARY_POTTER" : "PICK_CHALLENGE"), true, 400, 58, 560, titleColor);
            var trophies = 0;
            var total = _category == 0 ? 10 : _category == 3 ? 18 : 20;
            foreach (var mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex == _category && IsBeaten(mode.Id, ProgressLookup?.Invoke(mode.Id) ?? 0))
                {
                    trophies++;
                }
            }
            var counter = Text("NUMBER_OF_TROPHIES");
            var first = counter.IndexOf("%d", StringComparison.Ordinal);
            if (first >= 0)
            {
                counter = counter.Substring(0, first) + trophies + counter.Substring(first + 2);
                counter = counter.Replace("%d", total.ToString(CultureInfo.InvariantCulture));
            }
            OriginalUiWidgets.TextAtBaseline(counter, 739, 73, 110, "DwarvenTodcraft15", new Color(1f, 240f / 255f, 0f));
            var trophy = _texture("trophy.png");
            if (trophy != null)
            {
                CanvasUI.DrawTexture(new Rect(718, 26, trophy.Width * .5f, trophy.Height * .5f), trophy);
            }
            foreach (var mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex == _category && Playable(mode.Id))
                {
                    DrawDirectoryCard(mode);
                }
            }
            var back = new Rect(18, 568, 111, 26);
            var down = _pressedDirectoryButton == 100 && back.Contains(CanvasUI.PointerPosition);
            OriginalUiWidgets.ImageButton(_texture, back, "SeedChooser_Button2.png", "SeedChooser_Button2_Glow.png", "SeedChooser_Button2_Glow.png", back.Contains(CanvasUI.PointerPosition), down);
            OriginalUiWidgets.TextAtBaseline(Text("BACK_TO_MENU"), back.center.x + (down ? 1 : 0), 586 + (down ? 1 : 0), 111,
                "BrianneTod12", new Color(42f / 255f, 42f / 255f, 90f / 255f));
            if (TooltipRenderer != null && LockTooltipKeyLookup != null)
            {
                foreach (var mode in VerifiedDataCatalog.ChallengeModes)
                {
                    if (mode.UiCategoryIndex != _category || !Playable(mode.Id) || CanLaunch == null || CanLaunch(mode.Id) || !CardRect(mode).Contains(CanvasUI.PointerPosition)
                        || (AccomplishmentsNeededLookup?.Invoke(mode.Id) ?? 2) > 1)
                    {
                        continue;
                    }
                    var card = CardRect(mode);
                    _directoryTooltip.Title = Text(LockTooltipKeyLookup(mode.Id));
                    _directoryTooltip.X = (int)card.center.x;
                    _directoryTooltip.Y = (int)card.y;
                    TooltipRenderer(_directoryTooltip);
                    break;
                }
            }
        }

        /// <summary>Draws one native card, including blank distant locks, gray next locks and original record overlays.</summary>
        /// <param name="mode">The unchanged PC menu metadata.</param>
        private void DrawDirectoryCard(ChallengeModeDefinition mode)
        {
            var card = CardRect(mode);
            var enabled = CanLaunch == null || CanLaunch(mode.Id);
            var over = enabled && card.Contains(CanvasUI.PointerPosition);
            var down = over && _pressedDirectoryButton == mode.Id;
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
            if (_category != 0)
            {
                var thumbnails = _texture("Challenge_Thumbnails.jpg");
                if (thumbnails != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(x + 13, y + 4, 80, 65), thumbnails, new Rect(image / 22f, 0, 1f / 22f, 1));
                }
            }
            else if (single != null)
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
            if (split < 0 && enabled && _directoryAutoWrap > 0 && caption.Length > _directoryAutoWrap)
            {
                split = _directoryAutoWrap;
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
            else if (IsBeaten(mode.Id, record))
            {
                DrawTexture("MiniGame_trophy.png", new Rect(x - 6, y - 2, 72, 71));
            }
            else if (record > 0)
            {
                if (mode.Id == 60 || mode.Id == 70)
                {
                    var streak = Text("LONGEST_STREAK").Replace("{STREAK}", record.ToString(CultureInfo.InvariantCulture));
                    var bounds = new Rect(x, y + 15, 96, 200);
                    OriginalUiWidgets.Text(bounds, streak, "ContinuumBold14Outline", Color.white, true, true, false);
                    OriginalUiWidgets.Text(bounds, streak, "ContinuumBold14", Color.red, true, true, false);
                }
                else if (mode.Id <= 15)
                {
                    var flags = record == 1 ? Text("ONE_FLAG") : Text("COUNT_FLAGS").Replace("{COUNT}", record.ToString(CultureInfo.InvariantCulture));
                    OriginalUiWidgets.TextAtBaseline(flags, x + 48, y + 48, 104, "ContinuumBold14Outline", Color.white);
                    OriginalUiWidgets.TextAtBaseline(flags, x + 48, y + 48, 104, "ContinuumBold14", Color.red);
                }
            }
        }

        /// <summary>Tests PC LawnApp::HasBeatenChallenge without awarding trophies to endless records.</summary>
        /// <param name="id">The original challenge identifier.</param>
        /// <param name="record">The persisted native record.</param>
        /// <returns>Whether this finite challenge has its original trophy.</returns>
        private static bool IsBeaten(int id, int record)
        {
            if (id <= 10)
            {
                return record >= (id <= 5 ? 5 : 10);
            }
            return record > 0 && !(id >= 11 && id <= 15 || id == 60 || id == 70);
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

    }
}
