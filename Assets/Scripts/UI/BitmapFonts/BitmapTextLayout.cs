using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace PvZReplica.UI.BitmapFonts
{
    public struct MissingBitmapGlyph
    {
        public int CodePoint { get; private set; }
        public int Utf16Index { get; private set; }
        internal MissingBitmapGlyph(int codePoint, int index) : this()
        {
            CodePoint = codePoint;
            Utf16Index = index;
        }
    }

    public struct PlacedBitmapGlyph
    {
        public BmFontGlyph Glyph { get; private set; }
        public int Utf16Index { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        internal PlacedBitmapGlyph(BmFontGlyph glyph, int index, float x, float y, float scale) : this()
        {
            Glyph = glyph;
            Utf16Index = index;
            X = x + glyph.XOffset * scale;
            Y = y + glyph.YOffset * scale;
            Width = glyph.Width * scale;
            Height = glyph.Height * scale;
        }
    }

    public sealed class MissingBitmapGlyphException : Exception
    {
        public IReadOnlyList<MissingBitmapGlyph> MissingGlyphs { get; private set; }

        internal MissingBitmapGlyphException(IReadOnlyList<MissingBitmapGlyph> glyphs)
            : base(Describe(glyphs))
        {
            MissingGlyphs = glyphs;
        }

        private static string Describe(IReadOnlyList<MissingBitmapGlyph> glyphs)
        {
            var message = new StringBuilder("Missing bitmap glyphs: ");
            for (int i = 0; i < glyphs.Count; i++)
            {
                if (i != 0) message.Append(", ");
                message.Append("U+").Append(glyphs[i].CodePoint.ToString("X4", CultureInfo.InvariantCulture));
                message.Append(" at UTF-16 index ").Append(glyphs[i].Utf16Index);
            }
            return message.ToString();
        }
    }

    /// <summary>Immutable placement relative to a top-left line origin, in scaled GUI units.</summary>
    public sealed class BitmapTextLayout
    {
        public BmFontData Font { get; private set; }
        public float Scale { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public int LineCount { get { return LineWidths.Count; } }
        public IReadOnlyList<float> LineWidths { get; private set; }
        public IReadOnlyList<PlacedBitmapGlyph> Glyphs { get; private set; }
        public IReadOnlyList<MissingBitmapGlyph> MissingGlyphs { get; private set; }
        public bool HasGlyphBounds { get; private set; }
        public float MinX { get; private set; }
        public float MinY { get; private set; }
        public float MaxX { get; private set; }
        public float MaxY { get; private set; }

        private BitmapTextLayout() { }

        public void ThrowIfMissing()
        {
            if (MissingGlyphs.Count != 0) throw new MissingBitmapGlyphException(MissingGlyphs);
        }

        /// <param name="maxWidth">Maximum scaled advance width; infinity disables character wrapping.</param>
        public static BitmapTextLayout Create(BmFontData font, string text, float scale = 1f,
            float maxWidth = float.PositiveInfinity)
        {
            if (font == null) throw new ArgumentNullException("font");
            if (text == null) throw new ArgumentNullException("text");
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                throw new ArgumentOutOfRangeException("scale");
            if (float.IsNaN(maxWidth) || maxWidth <= 0f)
                throw new ArgumentOutOfRangeException("maxWidth");

            var result = new BitmapTextLayout { Font = font, Scale = scale };
            var glyphs = new List<PlacedBitmapGlyph>();
            var missing = new List<MissingBitmapGlyph>();
            var widths = new List<float>();
            float x = 0f, y = 0f;
            int previous = -1;
            bool hasGlyphOnLine = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    widths.Add(x);
                    x = 0f;
                    y += font.LineHeight * scale;
                    previous = -1;
                    hasGlyphOnLine = false;
                    continue;
                }
                int index = i;
                int codePoint;
                if (char.IsHighSurrogate(ch))
                {
                    if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                        throw new ArgumentException("Unpaired high surrogate at UTF-16 index " + i + ".", "text");
                    codePoint = char.ConvertToUtf32(ch, text[++i]);
                }
                else if (char.IsLowSurrogate(ch))
                    throw new ArgumentException("Unpaired low surrogate at UTF-16 index " + i + ".", "text");
                else codePoint = ch;

                BmFontGlyph glyph;
                if (!font.TryGetGlyph(codePoint, out glyph))
                {
                    missing.Add(new MissingBitmapGlyph(codePoint, index));
                    previous = -1;
                    continue;
                }
                float kerning = previous < 0 ? 0f : font.GetKerning(previous, codePoint) * scale;
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
                        result.MinX = placed.X; result.MinY = placed.Y;
                        result.MaxX = placed.X + placed.Width; result.MaxY = placed.Y + placed.Height;
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
            foreach (float width in widths) result.Width = Math.Max(result.Width, width);
            result.Height = widths.Count * font.LineHeight * scale;
            result.Glyphs = new ReadOnlyCollection<PlacedBitmapGlyph>(glyphs);
            result.MissingGlyphs = new ReadOnlyCollection<MissingBitmapGlyph>(missing);
            result.LineWidths = new ReadOnlyCollection<float>(widths);
            return result;
        }
    }
}
