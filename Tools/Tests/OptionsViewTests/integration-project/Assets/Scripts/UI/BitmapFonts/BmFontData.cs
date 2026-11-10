using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PvZReplica.UI.BitmapFonts
{
    public sealed class BmFontGlyph
    {
        public int CodePoint { get; internal set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int XOffset { get; internal set; }
        public int YOffset { get; internal set; }
        public int XAdvance { get; internal set; }
        public int Page { get; internal set; }
    }

    /// <summary>One unpacked, Unicode BMFont text-format layer.</summary>
    public sealed class BmFontData
    {
        private readonly Dictionary<int, BmFontGlyph> glyphs;
        private readonly Dictionary<long, int> kernings;
        private readonly ReadOnlyCollection<string> pages;

        public string Face { get; private set; }
        public int PointSize { get; private set; }
        public int LineHeight { get; private set; }
        public int Base { get; private set; }
        public int AtlasWidth { get; private set; }
        public int AtlasHeight { get; private set; }
        public int GlyphCount { get { return glyphs.Count; } }
        public int KerningCount { get { return kernings.Count; } }
        public IReadOnlyList<string> Pages { get { return pages; } }

        internal BmFontData(string face, int pointSize, int lineHeight, int baseline,
            int atlasWidth, int atlasHeight, string[] pageNames,
            Dictionary<int, BmFontGlyph> glyphs, Dictionary<long, int> kernings)
        {
            Face = face;
            PointSize = pointSize;
            LineHeight = lineHeight;
            Base = baseline;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
            pages = Array.AsReadOnly(pageNames);
            this.glyphs = glyphs;
            this.kernings = kernings;
        }

        public bool TryGetGlyph(int codePoint, out BmFontGlyph glyph)
        {
            return glyphs.TryGetValue(codePoint, out glyph);
        }

        public int GetKerning(int first, int second)
        {
            int amount;
            return kernings.TryGetValue(PairKey(first, second), out amount) ? amount : 0;
        }

        internal static long PairKey(int first, int second)
        {
            return ((long)(uint)first << 32) | (uint)second;
        }

        public static BmFontData Parse(string text)
        {
            return BmFontParser.Parse(text);
        }
    }
}
