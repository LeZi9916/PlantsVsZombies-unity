using System;
using System.Collections.Generic;
using System.IO;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

namespace PvZReplica.UI
{
    /// <summary>Original PC materials. Texture lookups use the host cache; no generated artwork or GUI skins.</summary>
    public static class OriginalUiWidgets
    {
        private static readonly Dictionary<string, ImGuiBitmapFont> fonts = new Dictionary<string, ImGuiBitmapFont>();
        private static readonly HashSet<string> failedFonts = new HashSet<string>();
        public static readonly Color StoneText = new Color(21f / 255f, 175f / 255f, 0f, 1f);
        public static bool Hovered(Rect rect) { return Event.current != null && rect.Contains(Event.current.mousePosition); }
        public static bool Pressed(Rect rect) { return Hovered(rect) && Input.GetMouseButton(0); }

        public static void DialogFrame(Func<string, Texture2D> texture, Rect rect, bool tallBottom = true, bool header = true)
        {
            string bottom = tallBottom ? "dialog_bigbottom" : "dialog_bottom";
            Texture2D tl = texture("dialog_topleft.png"), tr = texture("dialog_topright.png"), bl = texture(bottom + "left.png"), br = texture(bottom + "right.png");
            if (tl == null || tr == null || bl == null || br == null) return;
            float centerWidth = Math.Max(0, rect.width - tl.width - tr.width);
            float middleHeight = Math.Max(0, rect.height - tr.height - bl.height);
            Image(tl, new Rect(rect.x, rect.y, tl.width, tl.height));
            Tile(texture("dialog_topmiddle.png"), new Rect(rect.x + tl.width, rect.y, centerWidth, tl.height));
            Image(tr, new Rect(rect.xMax - tr.width, rect.y, tr.width, tr.height));
            Tile(texture("dialog_centerleft.png"), new Rect(rect.x, rect.y + tr.height, tl.width, middleHeight));
            Tile(texture("dialog_centermiddle.png"), new Rect(rect.x + tl.width, rect.y + tr.height, centerWidth, middleHeight));
            Tile(texture("dialog_centerright.png"), new Rect(rect.xMax - tr.width, rect.y + tr.height, tr.width, middleHeight));
            Image(bl, new Rect(rect.x, rect.yMax - bl.height, bl.width, bl.height));
            Tile(texture(bottom + "middle.png"), new Rect(rect.x + bl.width, rect.yMax - bl.height, Math.Max(0, rect.width - bl.width - br.width), bl.height));
            Image(br, new Rect(rect.xMax - br.width, rect.yMax - br.height, br.width, br.height));
            Texture2D cap = header ? texture("dialog_header.png") : null;
            if (cap != null) Image(cap, new Rect(rect.center.x - cap.width / 2f, rect.y - 8, cap.width, cap.height));
        }
        // PC 0x452720/0x4532B0: preserve end caps, tile the original middle with a final source crop.
        // Pressed graphics shift X by1; caption shifts X/Y by1. Return value is caption displacement.
        public static Vector2 StoneButton(Func<string, Texture2D> texture, Rect rect, bool hovered = false, bool pressed = false, bool enabled = true)
        {
            pressed &= enabled;
            string stem = pressed ? "button_down_" : "button_";
            Texture2D left = texture(stem + "left.png"), middle = texture(stem + "middle.png"), right = texture(stem + "right.png");
            if (left == null || middle == null || right == null) return Vector2.zero;
            Color old = GUI.color;
            try
            {
                if (!enabled) GUI.color = new Color(old.r * .5f, old.g * .5f, old.b * .5f, old.a);
                float x = rect.x + (pressed ? 1 : 0);
                Image(left, new Rect(x, rect.y, left.width, left.height));
                Tile(middle, new Rect(x + left.width, rect.y, Math.Max(0, rect.width - left.width - right.width), middle.height));
                Image(right, new Rect(x + rect.width - right.width, rect.y, right.width, right.height));
            }
            finally { GUI.color = old; }
            return pressed ? Vector2.one : Vector2.zero;
        }
        public static void ImageButton(Func<string, Texture2D> texture, Rect rect, string normal, string hover = null, string down = null,
            bool hovered = false, bool pressed = false, bool enabled = true)
        {
            string name = enabled && pressed && down != null ? down : enabled && hovered && hover != null ? hover : normal;
            Texture2D image = texture(name);
            if (image == null && name != normal) image = texture(normal);
            Color old = GUI.color;
            try
            {
                if (!enabled) GUI.color = new Color(old.r * .5f, old.g * .5f, old.b * .5f, old.a);
                Image(image, rect);
            }
            finally { GUI.color = old; }
        }
        // PC LawnCommon DrawEditBox / Graphics.DrawImageBox: thirds of original editbox, outside edit bounds by5/6.
        public static void EditBox(Func<string, Texture2D> texture, Rect field)
        {
            Texture2D image = texture("editbox.png");
            if (image == null) return;
            NineSlice(image, new Rect(field.x - 5, field.y - 6, field.width + 10, field.height + 12), image.width / 3, image.height / 3);
        }
        public static GUIStyle TransparentTextField(GUIStyle source)
        {
            GUIStyle style = new GUIStyle(source);
            foreach (GUIStyleState state in new[] { style.normal, style.hover, style.active, style.focused, style.onNormal, style.onHover, style.onActive, style.onFocused })
            { state.background = null; state.textColor = new Color(240f / 255f, 240f / 255f, 1f); }
            style.border = new RectOffset(0, 0, 0, 0);
            return style;
        }
        // TodDrawString uses a baseline; preserving it matters on the original award plaque.
        public static bool TextAtBaseline(string text, float centerX, float baseline, float maxWidth, string originalFont, Color color)
        {
            if (string.IsNullOrEmpty(text)) return true;
            ImGuiBitmapFont bitmap;
            if (!fonts.TryGetValue(originalFont, out bitmap))
            {
                if (failedFonts.Contains(originalFont)) return false;
                try { bitmap = ImGuiBitmapFont.LoadFromResources(originalFont); fonts.Add(originalFont, bitmap); }
                catch (Exception error) when (error is IOException || error is FormatException) { failedFonts.Add(originalFont); return false; }
            }
            BitmapTextLayout layout = bitmap.Layout(text);
            if (layout.MissingGlyphs.Count != 0 || layout.LineCount != 1 || layout.Width > maxWidth) return false;
            bitmap.Draw(layout, centerX - Mathf.Floor(layout.Width * .5f), baseline - bitmap.Data.Base, color);
            return true;
        }
        public static bool Text(Rect rect, string text, string originalFont, Color color, bool centered = true, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return true;
            ImGuiBitmapFont font;
            if (!fonts.TryGetValue(originalFont, out font))
            {
                if (failedFonts.Contains(originalFont)) return false;
                try { font = ImGuiBitmapFont.LoadFromResources(originalFont); fonts.Add(originalFont, font); }
                catch (Exception e) when (e is IOException || e is FormatException) { failedFonts.Add(originalFont); return false; }
            }
            BitmapTextLayout layout = font.Layout(text, 1f, wrap ? rect.width : float.PositiveInfinity);
            if (layout.MissingGlyphs.Count != 0) return false;
            float x = centered ? rect.center.x - layout.Width / 2f : rect.x;
            float y = rect.center.y - layout.Height / 2f;
            font.Draw(layout, x, y, color);
            return true;
        }
        public static bool StoneLabel(Rect rect, string text, bool hovered = false, bool pressed = false, bool enabled = true)
        {
            string name=hovered&&enabled?"DwarvenTodcraft18BrightGreenInset":"DwarvenTodcraft18GreenInset";
            ImGuiBitmapFont bitmap;
            if(!fonts.TryGetValue(name,out bitmap))
            {
                if(failedFonts.Contains(name))return false;
                try {bitmap=ImGuiBitmapFont.LoadFromResources(name);fonts.Add(name,bitmap);}
                catch(Exception e) when(e is IOException||e is FormatException){failedFonts.Add(name);return false;}
            }
            BitmapTextLayout layout=bitmap.Layout(text??"");
            if(layout.MissingGlyphs.Count!=0)return false;
            int height=bitmap.Data.LineHeight;
            float baseline=rect.y+(pressed?1:0)+(int)((rect.height-height/6-1f+height)/2f)-4f;
            float x=rect.x+(int)((rect.width-layout.Width)/2f)+1+(pressed?1:0);
            bitmap.Draw(layout,x,baseline-bitmap.Data.Base,enabled?Color.white:new Color(.5f,.5f,.5f));
            return true;
        }
        public static void Image(Texture2D texture, Rect destination)
        { if (texture != null && destination.width > 0 && destination.height > 0) GUI.DrawTexture(destination, texture, ScaleMode.StretchToFill, true); }
        public static void Tile(Texture2D texture, Rect destination)
        {
            if (texture == null || destination.width <= 0 || destination.height <= 0) return;
            for (float y = destination.y; y < destination.yMax; y += texture.height)
            for (float x = destination.x; x < destination.xMax; x += texture.width)
            {
                float w = Math.Min(texture.width, destination.xMax - x), h = Math.Min(texture.height, destination.yMax - y);
                GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), texture, new Rect(0, 1 - h / texture.height, w / texture.width, h / texture.height), true);
            }
        }
        public static void NineSlice(Texture2D image, Rect rect, int borderX, int borderY)
        {
            float[] sx = { 0, borderX, image.width - borderX, image.width }, sy = { 0, borderY, image.height - borderY, image.height };
            float bx = Math.Min(borderX, rect.width / 2), by = Math.Min(borderY, rect.height / 2);
            float[] dx = { rect.x, rect.x + bx, rect.xMax - bx, rect.xMax }, dy = { rect.y, rect.y + by, rect.yMax - by, rect.yMax };
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
                GUI.DrawTextureWithTexCoords(new Rect(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]), image,
                    new Rect(sx[col] / image.width, 1 - sy[row + 1] / image.height, (sx[col + 1] - sx[col]) / image.width, (sy[row + 1] - sy[row]) / image.height), true);
        }
        public static void ReleaseFonts()
        { foreach (ImGuiBitmapFont font in fonts.Values) font.Dispose(); fonts.Clear(); failedFonts.Clear(); }
    }
}
