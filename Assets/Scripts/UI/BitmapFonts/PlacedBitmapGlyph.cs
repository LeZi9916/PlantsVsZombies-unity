#nullable enable

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines placed bitmap glyph.</summary>
    public struct PlacedBitmapGlyph
    {
        /// <summary>Gets or sets glyph.</summary>
        public BmFontGlyph Glyph { get; private set; }
        /// <summary>Gets or sets utf16 index.</summary>
        public int Utf16Index { get; private set; }
        /// <summary>Gets or sets x.</summary>
        public float X { get; private set; }
        /// <summary>Gets or sets y.</summary>
        public float Y { get; private set; }
        /// <summary>Gets or sets width.</summary>
        public float Width { get; private set; }
        /// <summary>Gets or sets height.</summary>
        public float Height { get; private set; }

        /// <summary>Initializes placed bitmap glyph.</summary>
        /// <param name = "glyph">The glyph.</param>
        /// <param name = "index">The index.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "scale">The scale.</param>
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
}
