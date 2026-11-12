#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>One unpacked, Unicode BMFont text-format layer.</summary>
    public sealed class BmFontData
    {
        /// <summary>Stores glyphs.</summary>
        private readonly Dictionary<int, BmFontGlyph> _glyphs;
        /// <summary>Stores kernings.</summary>
        private readonly Dictionary<long, int> _kernings;
        /// <summary>Stores pages.</summary>
        private readonly ReadOnlyCollection<string> _pages;
        /// <summary>Gets or sets face.</summary>
        public string? Face { get; private set; } = string.Empty;
        /// <summary>Gets or sets point size.</summary>
        public int PointSize { get; private set; }
        /// <summary>Gets or sets line height.</summary>
        public int LineHeight { get; private set; }
        /// <summary>Gets or sets base.</summary>
        public int Base { get; private set; }
        /// <summary>Gets or sets atlas width.</summary>
        public int AtlasWidth { get; private set; }
        /// <summary>Gets or sets atlas height.</summary>
        public int AtlasHeight { get; private set; }

        /// <summary>Gets or sets glyph count.</summary>
        public int GlyphCount
        {
            get
            {
                return _glyphs.Count;
            }
        }

        /// <summary>Gets or sets kerning count.</summary>
        public int KerningCount
        {
            get
            {
                return _kernings.Count;
            }
        }

        /// <summary>Gets or sets pages.</summary>
        public IReadOnlyList<string> Pages
        {
            get
            {
                return _pages;
            }
        }

        /// <summary>Initializes bm font data.</summary>
        /// <param name = "face">The face.</param>
        /// <param name = "pointSize">The point size.</param>
        /// <param name = "lineHeight">The line height.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "atlasWidth">The atlas width.</param>
        /// <param name = "atlasHeight">The atlas height.</param>
        /// <param name = "pageNames">The page names.</param>
        /// <param name = "glyphs">The glyphs.</param>
        /// <param name = "kernings">The kernings.</param>
        internal BmFontData(string? face, int pointSize, int lineHeight, int baseline, int atlasWidth, int atlasHeight, string[] pageNames, Dictionary<int, BmFontGlyph> glyphs, Dictionary<long, int> kernings)
        {
            Face = face;
            PointSize = pointSize;
            LineHeight = lineHeight;
            Base = baseline;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
            _pages = Array.AsReadOnly(pageNames);
            this._glyphs = glyphs;
            this._kernings = kernings;
        }

        /// <summary>Performs try get glyph.</summary>
        /// <param name = "codePoint">The code point.</param>
        /// <param name = "glyph">The glyph.</param>
        /// <returns>The resulting try get glyph.</returns>
        public bool TryGetGlyph(int codePoint, out BmFontGlyph glyph)
        {
            return _glyphs.TryGetValue(codePoint, out glyph);
        }

        /// <summary>Performs get kerning.</summary>
        /// <param name = "first">The first.</param>
        /// <param name = "second">The second.</param>
        /// <returns>The resulting get kerning.</returns>
        public int GetKerning(int first, int second)
        {
            int amount;
            return _kernings.TryGetValue(PairKey(first, second), out amount) ? amount : 0;
        }

        /// <summary>Performs pair key.</summary>
        /// <param name = "first">The first.</param>
        /// <param name = "second">The second.</param>
        /// <returns>The resulting pair key.</returns>
        internal static long PairKey(int first, int second)
        {
            return ((long)(uint)first << 32) | (uint)second;
        }

        /// <summary>Performs parse.</summary>
        /// <param name = "text">The text.</param>
        /// <returns>The resulting parse.</returns>
        public static BmFontData Parse(string text)
        {
            return BmFontParser.Parse(text);
        }
    }
}
