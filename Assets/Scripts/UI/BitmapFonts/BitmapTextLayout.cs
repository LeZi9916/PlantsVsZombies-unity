#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Immutable placement relative to a top-left line origin, in scaled GUI units.</summary>
    public sealed class BitmapTextLayout
    {
        /// <summary>Gets or sets font.</summary>
        public BmFontData Font { get; private set; }
        /// <summary>Gets or sets scale.</summary>
        public float Scale { get; private set; }
        /// <summary>Gets or sets width.</summary>
        public float Width { get; private set; }
        /// <summary>Gets or sets height.</summary>
        public float Height { get; private set; }

        /// <summary>Gets or sets line count.</summary>
        public int LineCount
        {
            get
            {
                return LineWidths.Count;
            }
        }

        /// <summary>Gets or sets line widths.</summary>
        public IReadOnlyList<float> LineWidths { get; private set; }
        /// <summary>Gets or sets glyphs.</summary>
        public IReadOnlyList<PlacedBitmapGlyph> Glyphs { get; private set; }
        /// <summary>Gets or sets missing glyphs.</summary>
        public IReadOnlyList<MissingBitmapGlyph> MissingGlyphs { get; private set; }
        /// <summary>Gets or sets has glyph bounds.</summary>
        public bool HasGlyphBounds { get; private set; }
        /// <summary>Gets or sets min x.</summary>
        public float MinX { get; private set; }
        /// <summary>Gets or sets min y.</summary>
        public float MinY { get; private set; }
        /// <summary>Gets or sets max x.</summary>
        public float MaxX { get; private set; }
        /// <summary>Gets or sets max y.</summary>
        public float MaxY { get; private set; }

        /// <summary>Initializes bitmap text layout.</summary>
        private BitmapTextLayout()
        {
        }

        /// <summary>Performs throw if missing.</summary>
        /// <exception cref = "MissingBitmapGlyphException">The operation cannot satisfy its preconditions.</exception>
        public void ThrowIfMissing()
        {
            if (MissingGlyphs.Count != 0)
            {
                throw new MissingBitmapGlyphException(MissingGlyphs);
            }
        }

        /// <param name = "maxWidth">Maximum scaled advance width; infinity disables character wrapping.</param>
        public static BitmapTextLayout Create(BmFontData font, string text, float scale = 1f, float maxWidth = float.PositiveInfinity)
        {
            if (font == null)
            {
                throw new ArgumentNullException("font");
            }

            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                throw new ArgumentOutOfRangeException("scale");
            }

            if (float.IsNaN(maxWidth) || maxWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException("maxWidth");
            }

            var result = new BitmapTextLayout
            {
                Font = font,
                Scale = scale
            };
            var glyphs = new List<PlacedBitmapGlyph>();
            var missing = new List<MissingBitmapGlyph>();
            var widths = new List<float>();
            float x = 0f, y = 0f;
            var previous = -1;
            var hasGlyphOnLine = false;
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    widths.Add(x);
                    x = 0f;
                    y += font.LineHeight * scale;
                    previous = -1;
                    hasGlyphOnLine = false;
                    continue;
                }

                var index = i;
                int codePoint;
                if (char.IsHighSurrogate(ch))
                {
                    if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                    {
                        throw new ArgumentException("Unpaired high surrogate at UTF-16 index " + i + ".", "text");
                    }

                    codePoint = char.ConvertToUtf32(ch, text[++i]);
                }
                else if (char.IsLowSurrogate(ch))
                {
                    throw new ArgumentException("Unpaired low surrogate at UTF-16 index " + i + ".", "text");
                }
                else
                {
                    codePoint = ch;
                }

                BmFontGlyph glyph;
                if (!font.TryGetGlyph(codePoint, out glyph))
                {
                    missing.Add(new MissingBitmapGlyph(codePoint, index));
                    previous = -1;
                    continue;
                }

                var kerning = previous < 0 ? 0f : font.GetKerning(previous, codePoint) * scale;
                if (hasGlyphOnLine && x + kerning + glyph.XAdvance * scale > maxWidth)
                {
                    widths.Add(x);
                    x = 0f;
                    y += font.LineHeight * scale;
                    kerning = 0f;
                }

                x += kerning;
                var placed = new PlacedBitmapGlyph(glyph, index, x, y, scale);
                glyphs.Add(placed);
                if (glyph.Width != 0 && glyph.Height != 0)
                {
                    if (!result.HasGlyphBounds)
                    {
                        result.MinX = placed.X;
                        result.MinY = placed.Y;
                        result.MaxX = placed.X + placed.Width;
                        result.MaxY = placed.Y + placed.Height;
                        result.HasGlyphBounds = true;
                    }
                    else
                    {
                        result.MinX = Math.Min(result.MinX, placed.X);
                        result.MinY = Math.Min(result.MinY, placed.Y);
                        result.MaxX = Math.Max(result.MaxX, placed.X + placed.Width);
                        result.MaxY = Math.Max(result.MaxY, placed.Y + placed.Height);
                    }
                }

                x += glyph.XAdvance * scale;
                previous = codePoint;
                hasGlyphOnLine = true;
            }

            widths.Add(x);
            foreach (float width in widths)
            {
                result.Width = Math.Max(result.Width, width);
            }

            result.Height = widths.Count * font.LineHeight * scale;
            result.Glyphs = new ReadOnlyCollection<PlacedBitmapGlyph>(glyphs);
            result.MissingGlyphs = new ReadOnlyCollection<MissingBitmapGlyph>(missing);
            result.LineWidths = new ReadOnlyCollection<float>(widths);
            return result;
        }
    }
}
