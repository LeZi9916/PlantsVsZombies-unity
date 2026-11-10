using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PvZReplica.VerifiedData;
using UnityEngine;

namespace PvZReplica.UI
{
    public sealed class ModeSelectionView
    {
        private static readonly string[] Titles = { "生存模式", "小游戏", "其他挑战", "解谜模式" };
        private readonly Func<string, Texture2D> texture;
        private readonly Font font;
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private GUIStyle label;
        private int category;
        private string unavailable;
        public Func<string, bool, float, float, float, Color, bool> OriginalCenteredLabel { get; set; }

        public bool Closed { get; private set; }

        public ModeSelectionView(string root, Func<string, Texture2D> textureLoader, Font displayFont)
        {
            texture = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
            font = displayFont;
            string path = Path.Combine(root, "properties", "LawnStrings.txt");
            if (File.Exists(path)) ReadStrings(File.ReadAllLines(path, Encoding.UTF8));
        }

        public void Open(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= Titles.Length) throw new ArgumentOutOfRangeException(nameof(categoryIndex));
            category = categoryIndex;
            Closed = false;
            unavailable = null;
        }

        public void HandleClick(Vector2 point)
        {
            if (unavailable != null)
            {
                if (new Rect(320, 330, 160, 42).Contains(point)) unavailable = null;
                return;
            }
            for (int i = 0; i < Titles.Length; i++)
            {
                if (TabRect(i).Contains(point)) { category = i; return; }
            }
            if (new Rect(18, 568, 111, 26).Contains(point))
            {
                Closed = true;
                return;
            }
            foreach (ChallengeModeDefinition mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex != category || !CardRect(mode).Contains(point)) continue;
                unavailable = Name(mode);
                return;
            }
        }

        public void Draw()
        {
            DrawTexture("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            Color titleColor = new Color(220f / 255f, 220f / 255f, 220f / 255f);
            if (OriginalCenteredLabel == null || !OriginalCenteredLabel(Titles[category], true, 400f, 58f, 560f, titleColor))
                Label(new Rect(120, 28, 560, 58), Titles[category], 28, Color.white, TextAnchor.MiddleCenter, false);
            Texture2D thumbnails = texture("Challenge_Thumbnails.jpg");
            foreach (ChallengeModeDefinition mode in VerifiedDataCatalog.ChallengeModes)
            {
                if (mode.UiCategoryIndex != category) continue;
                Rect card = CardRect(mode);
                int image = mode.Unidentified0x04;
                if (thumbnails != null && image >= 0 && image < 22)
                    GUI.DrawTextureWithTexCoords(new Rect(card.x + 13, card.y + 4, 80, 65), thumbnails,
                        new Rect(image / 22f, 0, 1f / 22f, 1));
                DrawTexture("Challenge_Window.png", new Rect(card.x - 6, card.y - 2, 118, 120));
                Color textColor = new Color(42f / 255f, 42f / 255f, 90f / 255f);
                string name = Name(mode);
                if (OriginalCenteredLabel == null || !OriginalCenteredLabel(name, false, card.x + 52f, card.y + 96f, 98f, textColor))
                    Label(new Rect(card.x + 3, card.y + 70, card.width - 6, 39), name, 13,
                        textColor, TextAnchor.MiddleCenter, true);
            }
            DrawTexture("Store_MainMenuButton.png", new Rect(18, 568, 111, 26));
            Label(new Rect(18, 568, 111, 26), "返回主菜单", 14, Color.white, TextAnchor.MiddleCenter, false);
            for (int i = 0; i < Titles.Length; i++)
                Label(TabRect(i), Titles[i], 16, i == category ? Color.gray : Color.yellow, TextAnchor.MiddleCenter, false);
            if (unavailable != null)
            {
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, 0.58f));
                Fill(new Rect(210, 210, 380, 180), new Color(0.28f, 0.17f, 0.1f, 0.98f));
                Label(new Rect(235, 235, 330, 65), unavailable + "\n尚未实现", 21, Color.white, TextAnchor.MiddleCenter, true);
                Fill(new Rect(320, 330, 160, 42), new Color(0.38f, 0.5f, 0.2f));
                Label(new Rect(320, 330, 160, 42), "确定", 18, Color.white, TextAnchor.MiddleCenter, false);
            }
        }

        private static Rect CardRect(ChallengeModeDefinition mode)
        {
            return mode.UiCategoryIndex == 0
                ? new Rect(38 + mode.UiGridColumn * 155, 125 + mode.UiGridRow * 145, 104, 115)
                : new Rect(38 + mode.UiGridColumn * 155, 93 + mode.UiGridRow * 119, 104, 115);
        }

        private static Rect TabRect(int index) { return new Rect(100 * (index + 2), 540, 100, 60); }

        private string Name(ChallengeModeDefinition mode)
        {
            string key = mode.LocalizationKey.Trim('[', ']');
            string value;
            return strings.TryGetValue(key, out value) && value.Length > 0 ? value : key;
        }

        private void ReadStrings(string[] lines)
        {
            string key = null;
            StringBuilder value = new StringBuilder();
            foreach (string line in lines)
            {
                string text = line.Trim();
                if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
                {
                    if (key != null) strings[key] = value.ToString().Trim();
                    key = text.Substring(1, text.Length - 2);
                    value.Length = 0;
                }
                else if (key != null && text.Length > 0)
                {
                    if (value.Length > 0) value.Append('\n');
                    value.Append(text);
                }
            }
            if (key != null) strings[key] = value.ToString().Trim();
        }

        private void DrawTexture(string name, Rect rect)
        {
            Texture2D image = texture(name);
            if (image != null) GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, true);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void Label(Rect rect, string text, int size, Color color, TextAnchor alignment, bool wrap)
        {
            if (label == null) label = new GUIStyle(GUI.skin.label);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.wordWrap = wrap;
            label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }
    }
}
