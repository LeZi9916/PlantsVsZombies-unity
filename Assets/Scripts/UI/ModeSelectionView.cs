#nullable enable
using PlantsVsZombies.Modes;
using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private readonly Func<string, Texture2D?> _texture;
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
        public ModeSelectionView(string root, Func<string, Texture2D?> textureLoader, Font displayFont)
        {
            _texture = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
            _font = displayFont;
            var path = Path.Combine(root, "properties", "LawnStrings.txt");
            if (File.Exists(path))
            {
                ReadStrings(File.ReadAllLines(path, Encoding.UTF8));
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
        }

        /// <summary>Performs handle click.</summary>
        /// <param name = "point">The point.</param>
        public void HandleClick(Vector2 point)
        {
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
