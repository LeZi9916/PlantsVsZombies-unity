#nullable enable

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines bm font glyph.</summary>
    public sealed class BmFontGlyph
    {
        /// <summary>Gets or sets code point.</summary>
        public int CodePoint { get; internal set; }
        /// <summary>Gets or sets x.</summary>
        public int X { get; internal set; }
        /// <summary>Gets or sets y.</summary>
        public int Y { get; internal set; }
        /// <summary>Gets or sets width.</summary>
        public int Width { get; internal set; }
        /// <summary>Gets or sets height.</summary>
        public int Height { get; internal set; }
        /// <summary>Gets or sets xoffset.</summary>
        public int XOffset { get; internal set; }
        /// <summary>Gets or sets yoffset.</summary>
        public int YOffset { get; internal set; }
        /// <summary>Gets or sets xadvance.</summary>
        public int XAdvance { get; internal set; }
        /// <summary>Gets or sets page.</summary>
        public int Page { get; internal set; }
    }
}
