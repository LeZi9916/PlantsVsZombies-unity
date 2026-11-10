#nullable enable

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines missing bitmap glyph.</summary>
    public struct MissingBitmapGlyph
    {
        /// <summary>Gets or sets code point.</summary>
        public int CodePoint { get; private set; }
        /// <summary>Gets or sets utf16 index.</summary>
        public int Utf16Index { get; private set; }

        /// <summary>Initializes missing bitmap glyph.</summary>
        /// <param name = "codePoint">The code point.</param>
        /// <param name = "index">The index.</param>
        internal MissingBitmapGlyph(int codePoint, int index) : this()
        {
            CodePoint = codePoint;
            Utf16Index = index;
        }
    }
}
