using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PvZReplica.VerifiedData;
using PvZReplica.Modes;
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
        private string lockedNotice;
        public Func<int,bool> LaunchRequested { get; set; }
        public Func<int,bool> CanLaunch { get; set; }
        public Func<int,string> LockReasonLookup { get; set; }
        public Func<int,int> ProgressLookup { get; set; }
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
            lockedNotice=null;
        }

        public void HandleClick(Vector2 point)
        {
            if(lockedNotice!=null){if(new Rect(310,340,180,40).Contains(point))lockedNotice=null;return;}
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
                if (mode.UiCategoryIndex != category || !Playable(mode.Id) || !CardRect(mode).Contains(point)) continue;
                if(CanLaunch!=null&&!CanLaunch(mode.Id)){lockedNotice=LockReasonLookup==null?"此挑战尚未解锁。":LockReasonLookup(mode.Id);return;}
                if (LaunchRequested != null) LaunchRequested(mode.Id);
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
                if (mode.UiCategoryIndex != category || !Playable(mode.Id)) continue;
                Rect card = CardRect(mode);
                int image = mode.Unidentified0x04;
                if (thumbnails != null && image >= 0 && image < 22)
                    GUI.DrawTextureWithTexCoords(new Rect(card.x + 13, card.y + 4, 80, 65), thumbnails,
                        new Rect(image / 22f, 0, 1f / 22f, 1));
                DrawTexture(card.Contains(Event.current.mousePosition) && (CanLaunch == null || CanLaunch(mode.Id)) ? "Challenge_Window_Highlight.png" : "Challenge_Window.png", new Rect(card.x - 6, card.y - 2, 118, 120));
                Color textColor = new Color(42f / 255f, 42f / 255f, 90f / 255f);
                string name = Name(mode);
                if (OriginalCenteredLabel == null || !OriginalCenteredLabel(name, false, card.x + 52f, card.y + 96f, 98f, textColor))
                    Label(new Rect(card.x + 3, card.y + 70, card.width - 6, 39), name, 13,
                        textColor, TextAnchor.MiddleCenter, true);
                if(CanLaunch!=null&&!CanLaunch(mode.Id))
                {
                    DrawTexture("Challenge_Blank.png",new Rect(card.x+13,card.y+4,80,65));
                    Texture2D lockImage=texture("lock.png");
                    if(lockImage!=null)GUI.DrawTexture(new Rect(card.x+32,card.y+10,40,48),lockImage,ScaleMode.ScaleToFit,true);
                }
                int record=ProgressLookup==null?0:ProgressLookup(mode.Id);
                if(record>0)
                {
                    ModeDefinition definition; bool known=ModeCatalog.TryGet(mode.Id,out definition);
                    bool complete=known&&!definition.Endless&&(definition.Survival?record>=(definition.Hard?10:5):true);
                    string progress=complete?"已完成":known&&definition.Endless?"纪录 "+record:"旗帜 "+record;
                    Label(new Rect(card.x-3,card.y+108,card.width+6,20),progress,12,Color.yellow,TextAnchor.MiddleCenter,false);
                }
            }
            Rect back = new Rect(18, 568, 111, 26);
            OriginalUiWidgets.ImageButton(texture, back, "Store_MainMenuButton.png", "Store_MainMenuButtonHighlight.png", "Store_MainMenuButtonDown.png", OriginalUiWidgets.Hovered(back), OriginalUiWidgets.Pressed(back));
            Label(new Rect(18, 568, 111, 26), "返回主菜单", 14, Color.white, TextAnchor.MiddleCenter, false);
            for (int i = 0; i < Titles.Length; i++)
            {
                Rect tab = TabRect(i); Color text = OriginalUiWidgets.Hovered(tab) ? new Color(220f/255f,220f/255f,0) : new Color(1,240f/255f,0);
                if (!OriginalUiWidgets.Text(tab,Titles[i],"BrianneTod12",text)) Label(tab,Titles[i],16,text,TextAnchor.MiddleCenter,false);
            }
            if(lockedNotice!=null)
            {
                Fill(new Rect(0,0,800,600),new Color(0,0,0,.5f));
                OriginalUiWidgets.DialogFrame(texture,new Rect(210,185,380,260),true,false);
                if(!OriginalUiWidgets.Text(new Rect(235,235,330,88),lockedNotice,"DwarvenTodcraft15",new Color(224f/255f,187f/255f,98f/255f),true,true))
                    Label(new Rect(235,235,330,88),lockedNotice,20,Color.white,TextAnchor.MiddleCenter,true);
                Rect okay = new Rect(310,340,180,40);
                OriginalUiWidgets.StoneButton(texture,okay,OriginalUiWidgets.Hovered(okay),OriginalUiWidgets.Pressed(okay));
                if(!OriginalUiWidgets.StoneLabel(okay,"知道了"))Label(okay,"知道了",18,OriginalUiWidgets.StoneText,TextAnchor.MiddleCenter,false);
            }
        }

        private bool Playable(int id)
        {
            ModeDefinition definition;if(ModeCatalog.TryGet(id,out definition))return true;return (id==43||id==50)&&CanLaunch!=null&&CanLaunch(id);
        }

        private static Rect CardRect(ChallengeModeDefinition mode)
        {
            return mode.UiCategoryIndex == 0
                ? new Rect(38 + mode.UiGridColumn * 155, 125 + mode.UiGridRow * 145, 104, 115)
                : new Rect(38 + mode.UiGridColumn * 155, 93 + mode.UiGridRow * 119, 104, 115);
        }

        private static Rect TabRect(int index) { return new Rect(100 * (index + 2), 540, 100, 75); }

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
