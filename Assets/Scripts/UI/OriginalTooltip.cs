using System;
using System.Collections.Generic;
using System.IO;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

namespace PvZReplica.UI
{
    /// <summary>Native ToolTipWidget inputs. X/Y are the host's native desired position, not a plant grid.</summary>
    public sealed class OriginalTooltipRequest
    {
        public string Title = "", Body = "", Warning = "";
        public int X, Y, WarningFlashCounter;
        public bool Visible = true, Centered;
        public int MinScreenX, MaxScreenY = 600, CanvasWidth = 800;
        public float GraphicsTranslationX, GraphicsTranslationY;
        // PC +0x78 changes the minimum body line width through this original property (default100).
        public bool UseSeedChooserLineWidth;
        public int SeedChooserMaxLineWidth = 100;
    }
    public sealed class OriginalTooltipLayout
    {
        public Rect Bounds { get; internal set; }
        public int BodyWrapWidth { get; internal set; }
        public IReadOnlyList<string> BodyLines { get; internal set; }
        internal BitmapTextLayout Title, Warning;
        internal List<BitmapTextLayout> Body;
    }
    /// <summary>
    /// Local PC ToolTipWidget0x52FB10/0x52FD00/0x530180. Draws native program rectangles and
    /// original bitmap glyphs. No invented background texture, pulse, system-font fallback or input behavior.
    /// Own one per host and Dispose it at host destruction; prior UI helpers are unchanged.
    /// </summary>
    public sealed class OriginalTooltip : IDisposable
    {
        public const string TitleFont = "Pix118Bold"; // Resources.xml FONT_TINYBOLD, not a guessed font name.
        public const string BodyFont = "Pico129";
        public static readonly Color Background = new Color32(255, 255, 200, 255);
        private ImGuiBitmapFont titleFont, bodyFont;
        private bool disposed;
        public string LastError { get; private set; }
        public OriginalTooltipLayout LastLayout { get; private set; }

        private bool EnsureFonts()
        {
            if (disposed) throw new ObjectDisposedException(nameof(OriginalTooltip));
            try
            {
                if (titleFont == null) titleFont = ImGuiBitmapFont.LoadFromResources(TitleFont);
                if (bodyFont == null) bodyFont = ImGuiBitmapFont.LoadFromResources(BodyFont);
                return true;
            }
            catch (Exception error) when (error is IOException || error is FormatException)
            { LastError = error.Message; return false; }
        }
        public bool TryLayout(OriginalTooltipRequest request, out OriginalTooltipLayout result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            result = null; LastError = null;
            if (!EnsureFonts()) return false;
            try
            {
                string title = request.Title ?? "", warning = request.Warning ?? "", body = request.Body ?? "";
                BitmapTextLayout header = titleFont.Layout(title), alert = bodyFont.Layout(warning);
                header.ThrowIfMissing(); alert.ThrowIfMissing();
                int maximum = Math.Max((int)header.Width, (int)alert.Width);
                int minimumWrap = request.UseSeedChooserLineWidth ? request.SeedChooserMaxLineWidth : 100;
                int wrapWidth = Math.Max(minimumWrap, maximum - 30);
                List<string> lines = WrapBody(body, wrapWidth, bodyFont.Data);
                var bodyLayouts = new List<BitmapTextLayout>();
                foreach (string line in lines)
                {
                    BitmapTextLayout layout = bodyFont.Layout(line);
                    layout.ThrowIfMissing(); bodyLayouts.Add(layout);
                    maximum = Math.Max(maximum, (int)layout.Width);
                }
                int leading = title.Length == 0 ? 6 : titleFont.Data.LineHeight + 8;
                // Exact PC measurement uses the header font's height for the warning contribution.
                if (warning.Length > 0) leading += titleFont.Data.LineHeight + 2;
                int width = maximum + 10;
                int height = leading + lines.Count * (bodyFont.Data.LineHeight + 2) - 2;
                Rect bounds = Place(request, width, height);
                result = new OriginalTooltipLayout { Bounds = bounds, BodyWrapWidth = wrapWidth,
                    BodyLines = lines.AsReadOnly(), Title = header, Warning = alert, Body = bodyLayouts };
                LastLayout = result;
                return true;
            }
            catch (Exception error) when (error is MissingBitmapGlyphException || error is ArgumentException)
            { LastError = error.Message; return false; }
        }
        // Native0x52FB10 accumulates character advances until space/newline, then checks the word boundary.
        // A long word is kept whole. Do not use the existing generic per-character BMFont wrapping here.
        private static List<string> WrapBody(string text, int maxWidth, BmFontData font)
        {
            var result = new List<string>();
            int start = 0, cursor = 0;
            while (cursor < text.Length)
            {
                int advance = 0;
                while (true)
                {
                    while (cursor < text.Length && text[cursor] != ' ' && text[cursor] != '\n')
                    { advance += CharacterAdvance(font, text, ref cursor); }
                    if (cursor == text.Length || advance >= maxWidth || text[cursor] == '\n') break;
                    advance += CharacterAdvance(font, text, ref cursor);
                }
                result.Add(text.Substring(start, cursor - start));
                if (cursor < text.Length && text[cursor] == '\n') cursor++;
                while (cursor < text.Length && text[cursor] == ' ') cursor++;
                start = cursor;
            }
            return result;
        }
        private static int CharacterAdvance(BmFontData font, string text, ref int cursor)
        {
            int index = cursor, codePoint = text[cursor++];
            if (char.IsHighSurrogate((char)codePoint) && cursor < text.Length && char.IsLowSurrogate(text[cursor]))
                codePoint = char.ConvertToUtf32((char)codePoint, text[cursor++]);
            BmFontGlyph glyph;
            if (font.TryGetGlyph(codePoint, out glyph)) return glyph.XAdvance;
            // Let the real bitmap layout report the precise missing glyph; never substitute new artwork.
            return 0;
        }
        public static Rect Place(OriginalTooltipRequest request, int width, int height)
        {
            int x = request.X - (request.Centered ? width / 2 : 0), y = request.Y;
            float tx = request.GraphicsTranslationX, ty = request.GraphicsTranslationY;
            if (request.MinScreenX - tx <= x)
            {
                if (x + width + tx > request.CanvasWidth) x = request.CanvasWidth - (int)tx - width;
            }
            else x = request.MinScreenX - (int)tx;
            if (-ty <= y)
            {
                if (request.MaxScreenY < y + height + ty) y = request.MaxScreenY - (int)ty - height;
            }
            else y = (int)(-ty);
            return new Rect(x, y, width, height);
        }
        /// <summary>Call after existing bank/store controls, under the host's native800x600 GUI matrix.</summary>
        public bool Draw(OriginalTooltipRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!request.Visible) return true;
            OriginalTooltipLayout layout;
            if (!TryLayout(request, out layout)) return false;
            if (Event.current != null && Event.current.type != EventType.Repaint) return true;
            Rect bounds = layout.Bounds;
            Color old = GUI.color;
            try
            {
                GUI.color = Color.white;
                Fill(bounds, Background);
                // Native DrawRect(x,y,w-1,h-1) covers these same inclusive1px outer edges.
                Fill(new Rect(bounds.x, bounds.y, bounds.width, 1), Color.black);
                Fill(new Rect(bounds.x, bounds.yMax - 1, bounds.width, 1), Color.black);
                Fill(new Rect(bounds.x, bounds.y, 1, bounds.height), Color.black);
                Fill(new Rect(bounds.xMax - 1, bounds.y, 1, bounds.height), Color.black);
                int y = (int)bounds.y + 1;
                if (!string.IsNullOrEmpty(request.Title))
                {
                    DrawLine(titleFont, layout.Title, bounds, y + titleFont.Data.LineHeight, Color.black);
                    y += titleFont.Data.LineHeight + 2;
                }
                if (!string.IsNullOrEmpty(request.Warning))
                {
                    Color warning = request.WarningFlashCounter > 0 && request.WarningFlashCounter % 20 < 10 ? Color.black : Color.red;
                    DrawLine(bodyFont, layout.Warning, bounds, y + bodyFont.Data.LineHeight, warning);
                    y += bodyFont.Data.LineHeight + 2;
                }
                foreach (BitmapTextLayout line in layout.Body)
                {
                    DrawLine(bodyFont, line, bounds, y + bodyFont.Data.LineHeight, Color.black);
                    y += bodyFont.Data.LineHeight + 2;
                }
            }
            finally { GUI.color = old; }
            return true;
        }
        private static void DrawLine(ImGuiBitmapFont font, BitmapTextLayout text, Rect bounds, int baseline, Color color)
        {
            int x = (int)bounds.x + ((int)bounds.width - (int)text.Width) / 2;
            font.Draw(text, x, baseline - font.Data.Base, color);
        }
        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true); GUI.color = before;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (titleFont != null) titleFont.Dispose();
            if (bodyFont != null) bodyFont.Dispose();
            titleFont = bodyFont = null; LastLayout = null;
        }
    }
}
