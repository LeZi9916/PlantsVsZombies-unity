using UnityEngine;
using PvZReplica.UI;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        // Shared hosts can call these without changing pointer handling or business behavior.
        private void DrawOriginalUiDialog(Rect panel, string title, string body, bool tallBottom = true, bool shade = true)
        {
            if (shade) Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, .5f));
            OriginalUiWidgets.DialogFrame(Texture, panel, tallBottom, !string.IsNullOrEmpty(title));
            if (!string.IsNullOrEmpty(title))
            {
                Rect heading = new Rect(panel.x + 40, panel.y + 35, panel.width - 80, 36);
                if (!OriginalUiWidgets.Text(heading, title, "DwarvenTodcraft24", new Color(224f / 255f, 187f / 255f, 98f / 255f)))
                    Label(heading, title, 22, new Color(224f / 255f, 187f / 255f, 98f / 255f), TextAnchor.MiddleCenter, true);
            }
            if (!string.IsNullOrEmpty(body))
            {
                Rect lines = new Rect(panel.x + 40, panel.y + 80, panel.width - 80, System.Math.Max(30, panel.height - (tallBottom ? 165 : 120)));
                if (!OriginalUiWidgets.Text(lines, body, "DwarvenTodcraft15", new Color(224f / 255f, 187f / 255f, 98f / 255f), true, true))
                    Label(lines, body, 17, new Color(224f / 255f, 187f / 255f, 98f / 255f), TextAnchor.MiddleCenter, false);
            }
        }
        private void DrawOriginalUiButton(Rect rect, string text, bool enabled = true)
        {
            bool hover = enabled && OriginalUiWidgets.Hovered(rect), down = hover && OriginalUiWidgets.Pressed(rect);
            OriginalUiWidgets.StoneButton(Texture, rect, hover, down, enabled);
            if (!OriginalUiWidgets.StoneLabel(rect, text, hover, down, enabled))
                Label(new Rect(rect.x + (down ? 1 : 0), rect.y + (down ? 1 : 0), rect.width, rect.height), text, 15,
                    enabled ? OriginalUiWidgets.StoneText : Color.gray, TextAnchor.MiddleCenter, true);
        }
        private bool OriginalUiButton(Rect rect, string text, bool enabled = true)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked;
            try { clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); DrawOriginalUiButton(rect, text, previous && enabled); }
            finally { GUI.enabled = previous; }
            return clicked;
        }
        private string OriginalUiTextField(Rect rect, string value, int maxLength = 1024)
        {
            OriginalUiWidgets.EditBox(Texture, rect);
            GUIStyle style = OriginalUiWidgets.TransparentTextField(new GUIStyle(GUI.skin.textField));
            style.font = font; style.fontSize = 16; style.padding = new RectOffset(6, 6, 5, 5);
            return GUI.TextField(rect, value, maxLength, style);
        }
        private bool OriginalUiListRow(Rect rect,string text,bool selected)
        {
            // PC UserDialog/ListWidget palette at0x74A9C4, not a temporary button skin.
            Fill(rect,selected?new Color(20f/255f,180f/255f,15f/255f):new Color(23f/255f,24f/255f,35f/255f));
            Color color=new Color(235f/255f,225f/255f,180f/255f);
            if(!OriginalUiWidgets.Text(new Rect(rect.x+5,rect.y,rect.width-10,rect.height),text,"BrianneTod16",color,false))
                Label(new Rect(rect.x+5,rect.y,rect.width-10,rect.height),text,15,color,TextAnchor.MiddleLeft,false);
            return GUI.Button(rect,GUIContent.none,GUIStyle.none);
        }
        private void DrawOriginalUiAdvice(Rect rect, string text)
        {
            // Native advice is original font text with a dark shadow; no invented panel texture.
            Rect shadow = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
            if (OriginalUiWidgets.Text(shadow, text, "DwarvenTodcraft18", Color.black, true, true) &&
                OriginalUiWidgets.Text(rect, text, "DwarvenTodcraft18", new Color(1f, 1f, 0f), true, true)) return;
            Label(shadow, text, 18, Color.black, TextAnchor.MiddleCenter, true);
            Label(rect, text, 18, Color.yellow, TextAnchor.MiddleCenter, true);
        }
    }
}
