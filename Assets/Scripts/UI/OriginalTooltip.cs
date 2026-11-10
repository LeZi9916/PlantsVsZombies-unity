#nullable enable
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>
    /// Local PC ToolTipWidget0x52FB10/0x52FD00/0x530180. Draws native program rectangles and
    /// original bitmap glyphs. No invented background texture, pulse, system-font fallback or input behavior.
    /// Own one per host and Dispose it at host destruction; prior UI helpers are unchanged.
    /// </summary>
    public sealed class OriginalTooltip : IDisposable
    {
        /// <summary>Stores title font.</summary>
        public const string TitleFont = "Pix118Bold"; // Resources.xml FONT_TINYBOLD, not a guessed font name.
        /// <summary>Stores body font.</summary>
        public const string BodyFont = "Pico129";
        /// <summary>Stores background.</summary>
        public static readonly Color Background = new Color32(255, 255, 200, 255);
        /// <summary>Stores title font,  body font.</summary>
        private ImGuiBitmapFont _titleFont, _bodyFont;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets last error.</summary>
        public string LastError { get; private set; }
        /// <summary>Gets or sets last layout.</summary>
        public OriginalTooltipLayout LastLayout { get; private set; }

        /// <summary>Performs ensure fonts.</summary>
        /// <returns>The resulting ensure fonts.</returns>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private bool EnsureFonts()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OriginalTooltip));
            }

            try
            {
                if (_titleFont == null)
                {
                    _titleFont = ImGuiBitmapFont.LoadFromResources(TitleFont);
                }

                if (_bodyFont == null)
                {
                    _bodyFont = ImGuiBitmapFont.LoadFromResources(BodyFont);
                }

                return true;
            }
            catch (Exception error)when (error is IOException || error is FormatException)
            {
                LastError = error.Message;
                return false;
            }
        }

        /// <summary>Performs try layout.</summary>
        /// <param name = "request">The request.</param>
        /// <param name = "result">The result.</param>
        /// <returns>The resulting try layout.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public bool TryLayout(OriginalTooltipRequest request, out OriginalTooltipLayout result)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            result = null;
            LastError = null;
            if (!EnsureFonts())
            {
                return false;
            }

            try
            {
                string title = request.Title ?? "", warning = request.Warning ?? "", body = request.Body ?? "";
                BitmapTextLayout header = _titleFont.Layout(title), alert = _bodyFont.Layout(warning);
                header.ThrowIfMissing();
                alert.ThrowIfMissing();
                var maximum = Math.Max((int)header.Width, (int)alert.Width);
                var minimumWrap = request.UseSeedChooserLineWidth ? request.SeedChooserMaxLineWidth : 100;
                var wrapWidth = Math.Max(minimumWrap, maximum - 30);
                var lines = WrapBody(body, wrapWidth, _bodyFont.Data);
                var bodyLayouts = new List<BitmapTextLayout>();
                foreach (string line in lines)
                {
                    var layout = _bodyFont.Layout(line);
                    layout.ThrowIfMissing();
                    bodyLayouts.Add(layout);
                    maximum = Math.Max(maximum, (int)layout.Width);
                }

                var leading = title.Length == 0 ? 6 : _titleFont.Data.LineHeight + 8;
                // Exact PC measurement uses the header font's height for the warning contribution.
                if (warning.Length > 0)
                {
                    leading += _titleFont.Data.LineHeight + 2;
                }

                var width = maximum + 10;
                var height = leading + lines.Count * (_bodyFont.Data.LineHeight + 2) - 2;
                var bounds = Place(request, width, height);
                result = new OriginalTooltipLayout
                {
                    Bounds = bounds,
                    BodyWrapWidth = wrapWidth,
                    BodyLines = lines.AsReadOnly(),
                    _title = header,
                    _warning = alert,
                    _body = bodyLayouts
                };
                LastLayout = result;
                return true;
            }
            catch (Exception error)when (error is MissingBitmapGlyphException || error is ArgumentException)
            {
                LastError = error.Message;
                return false;
            }
        }

        // Native0x52FB10 accumulates character advances until space/newline, then checks the word boundary.
        // A long word is kept whole. Do not use the existing generic per-character BMFont wrapping here.
        /// <summary>Performs wrap body.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "maxWidth">The max width.</param>
        /// <param name = "font">The font.</param>
        /// <returns>The resulting wrap body.</returns>
        private static List<string> WrapBody(string text, int maxWidth, BmFontData font)
        {
            var result = new List<string>();
            int start = 0, cursor = 0;
            while (cursor < text.Length)
            {
                var advance = 0;
                while (true)
                {
                    while (cursor < text.Length && text[cursor] != ' ' && text[cursor] != '\n')
                    {
                        advance += CharacterAdvance(font, text, ref cursor);
                    }

                    if (cursor == text.Length || advance >= maxWidth || text[cursor] == '\n')
                    {
                        break;
                    }

                    advance += CharacterAdvance(font, text, ref cursor);
                }

                result.Add(text.Substring(start, cursor - start));
                if (cursor < text.Length && text[cursor] == '\n')
                {
                    cursor++;
                }

                while (cursor < text.Length && text[cursor] == ' ')
                {
                    cursor++;
                }

                start = cursor;
            }

            return result;
        }

        /// <summary>Performs character advance.</summary>
        /// <param name = "font">The font.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "cursor">The cursor.</param>
        /// <returns>The resulting character advance.</returns>
        private static int CharacterAdvance(BmFontData font, string text, ref int cursor)
        {
            int index = cursor, codePoint = text[cursor++];
            if (char.IsHighSurrogate((char)codePoint) && cursor < text.Length && char.IsLowSurrogate(text[cursor]))
            {
                codePoint = char.ConvertToUtf32((char)codePoint, text[cursor++]);
            }

            BmFontGlyph glyph;
            if (font.TryGetGlyph(codePoint, out glyph))
            {
                return glyph.XAdvance;
            }

            // Let the real bitmap layout report the precise missing glyph; never substitute new artwork.
            return 0;
        }

        /// <summary>Performs place.</summary>
        /// <param name = "request">The request.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <returns>The resulting place.</returns>
        public static Rect Place(OriginalTooltipRequest request, int width, int height)
        {
            int x = request.X - (request.Centered ? width / 2 : 0), y = request.Y;
            float tx = request.GraphicsTranslationX, ty = request.GraphicsTranslationY;
            if (request.MinScreenX - tx <= x)
            {
                if (x + width + tx > request.CanvasWidth)
                {
                    x = request.CanvasWidth - (int)tx - width;
                }
            }
            else
            {
                x = request.MinScreenX - (int)tx;
            }

            if (-ty <= y)
            {
                if (request.MaxScreenY < y + height + ty)
                {
                    y = request.MaxScreenY - (int)ty - height;
                }
            }
            else
            {
                y = (int)(-ty);
            }

            return new Rect(x, y, width, height);
        }

        /// <summary>Call after existing bank/store controls, under the host's native800x600 GUI matrix.</summary>
        public bool Draw(OriginalTooltipRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Visible)
            {
                return true;
            }

            OriginalTooltipLayout layout;
            if (!TryLayout(request, out layout))
            {
                return false;
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return true;
            }

            var bounds = layout.Bounds;
            var old = GUI.color;
            try
            {
                GUI.color = Color.white;
                Fill(bounds, Background);
                // Native DrawRect(x,y,w-1,h-1) covers these same inclusive1px outer edges.
                Fill(new Rect(bounds.x, bounds.y, bounds.width, 1), Color.black);
                Fill(new Rect(bounds.x, bounds.yMax - 1, bounds.width, 1), Color.black);
                Fill(new Rect(bounds.x, bounds.y, 1, bounds.height), Color.black);
                Fill(new Rect(bounds.xMax - 1, bounds.y, 1, bounds.height), Color.black);
                var y = (int)bounds.y + 1;
                if (!string.IsNullOrEmpty(request.Title))
                {
                    DrawLine(_titleFont, layout._title, bounds, y + _titleFont.Data.LineHeight, Color.black);
                    y += _titleFont.Data.LineHeight + 2;
                }

                if (!string.IsNullOrEmpty(request.Warning))
                {
                    var warning = request.WarningFlashCounter > 0 && request.WarningFlashCounter % 20 < 10 ? Color.black : Color.red;
                    DrawLine(_bodyFont, layout._warning, bounds, y + _bodyFont.Data.LineHeight, warning);
                    y += _bodyFont.Data.LineHeight + 2;
                }

                foreach (BitmapTextLayout line in layout._body)
                {
                    DrawLine(_bodyFont, line, bounds, y + _bodyFont.Data.LineHeight, Color.black);
                    y += _bodyFont.Data.LineHeight + 2;
                }
            }
            finally
            {
                GUI.color = old;
            }

            return true;
        }

        /// <summary>Performs draw line.</summary>
        /// <param name = "font">The font.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "color">The color.</param>
        private static void DrawLine(ImGuiBitmapFont font, BitmapTextLayout text, Rect bounds, int baseline, Color color)
        {
            var x = (int)bounds.x + ((int)bounds.width - (int)text.Width) / 2;
            font.Draw(text, x, baseline - font.Data.Base, color);
        }

        /// <summary>Performs fill.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        private static void Fill(Rect rect, Color color)
        {
            var before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = before;
        }

        /// <summary>Performs dispose.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_titleFont != null)
            {
                _titleFont.Dispose();
            }

            if (_bodyFont != null)
            {
                _bodyFont.Dispose();
            }

            _titleFont = _bodyFont = null;
            LastLayout = null;
        }
    }
}
