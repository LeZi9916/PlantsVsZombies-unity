#nullable enable
using System;
using System.Collections.Generic;

using System.Globalization;
using System.Text;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines missing bitmap glyph exception.</summary>
    public sealed class MissingBitmapGlyphException : Exception
    {
        /// <summary>Gets or sets missing glyphs.</summary>
        public IReadOnlyList<MissingBitmapGlyph> MissingGlyphs { get; private set; }

        /// <summary>Initializes missing bitmap glyph exception.</summary>
        /// <param name = "glyphs">The glyphs.</param>
        internal MissingBitmapGlyphException(IReadOnlyList<MissingBitmapGlyph> glyphs) : base(Describe(glyphs))
        {
            MissingGlyphs = glyphs;
        }

        /// <summary>Performs describe.</summary>
        /// <param name = "glyphs">The glyphs.</param>
        /// <returns>The resulting describe.</returns>
        private static string Describe(IReadOnlyList<MissingBitmapGlyph> glyphs)
        {
            var message = new StringBuilder("Missing bitmap glyphs: ");
            for (var i = 0; i < glyphs.Count; i++)
            {
                if (i != 0)
                {
                    message.Append(", ");
                }

                message.Append("U+").Append(glyphs[i].CodePoint.ToString("X4", CultureInfo.InvariantCulture));
                message.Append(" at UTF-16 index ").Append(glyphs[i].Utf16Index);
            }

            return message.ToString();
        }
    }
}
