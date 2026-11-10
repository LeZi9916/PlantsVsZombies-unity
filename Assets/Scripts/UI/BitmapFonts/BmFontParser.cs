#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines bm font parser.</summary>
    /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
    /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
    internal static class BmFontParser
    {
        /// <summary>Performs parse.</summary>
        /// <param name = "text">The text.</param>
        /// <returns>The resulting parse.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
        public static BmFontData Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            string face = null;
            int pointSize = 0, lineHeight = 0, baseline = 0, atlasWidth = 0, atlasHeight = 0;
            int pageCount = -1, glyphCount = -1, kerningCount = -1;
            bool hasInfo = false, hasCommon = false;
            var pageMap = new Dictionary<int, string>();
            var glyphs = new Dictionary<int, BmFontGlyph>();
            var kernings = new Dictionary<long, int>();
            var lineNumber = 0;
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (lineNumber == 1)
                    {
                        line = line.TrimStart('\uFEFF');
                    }

                    var r = BmFontRecord.Read(line, lineNumber);
                    if (r == null)
                    {
                        continue;
                    }

                    switch (r.Name)
                    {
                        case "info":
                            if (hasInfo)
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate info record");
                            }

                            hasInfo = true;
                            face = r.Field("face");
                            if (face.Length == 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "empty face");
                            }

                            pointSize = r.Number("size");
                            if (r.Number("unicode") != 1)
                            {
                                throw BmFontRecord.Error(lineNumber, "only Unicode fonts are supported");
                            }

                            break;
                        case "common":
                            if (hasCommon)
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate common record");
                            }

                            hasCommon = true;
                            lineHeight = r.Number("lineHeight");
                            baseline = r.Number("base");
                            atlasWidth = r.Number("scaleW");
                            atlasHeight = r.Number("scaleH");
                            pageCount = r.Number("pages");
                            if (lineHeight <= 0 || baseline < 0 || atlasWidth <= 0 || atlasHeight <= 0 || pageCount <= 0 || pageCount > 4096)
                            {
                                throw BmFontRecord.Error(lineNumber, "invalid common metrics");
                            }

                            if (r.Number("packed") != 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "packed channel fonts are not supported");
                            }

                            break;
                        case "page":
                            var pageId = r.Number("id");
                            var file = r.Field("file");
                            if (pageId < 0 || file.Length == 0 || file == "." || file == ".." || file.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            {
                                throw BmFontRecord.Error(lineNumber, "invalid PNG page name or id");
                            }

                            if (pageMap.ContainsKey(pageId))
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate page id");
                            }

                            pageMap.Add(pageId, file);
                            break;
                        case "chars":
                            if (glyphCount >= 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate chars record");
                            }

                            glyphCount = r.Number("count");
                            if (glyphCount < 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "negative char count");
                            }

                            break;
                        case "char":
                            var glyph = new BmFontGlyph
                            {
                                CodePoint = r.Number("id"),
                                X = r.Number("x"),
                                Y = r.Number("y"),
                                Width = r.Number("width"),
                                Height = r.Number("height"),
                                XOffset = r.Number("xoffset"),
                                YOffset = r.Number("yoffset"),
                                XAdvance = r.Number("xadvance"),
                                Page = r.Number("page")
                            };
                            if (!IsScalar(glyph.CodePoint) || glyph.X < 0 || glyph.Y < 0 || glyph.Width < 0 || glyph.Height < 0 || glyph.Page < 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "invalid glyph metrics");
                            }

                            if (r.Number("chnl") != 15)
                            {
                                throw BmFontRecord.Error(lineNumber, "channel-specific glyphs are not supported");
                            }

                            if (glyphs.ContainsKey(glyph.CodePoint))
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate glyph");
                            }

                            glyphs.Add(glyph.CodePoint, glyph);
                            break;
                        case "kernings":
                            if (kerningCount >= 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate kernings record");
                            }

                            kerningCount = r.Number("count");
                            if (kerningCount < 0)
                            {
                                throw BmFontRecord.Error(lineNumber, "negative kerning count");
                            }

                            break;
                        case "kerning":
                            int first = r.Number("first"), second = r.Number("second"), amount = r.Number("amount");
                            if (!IsScalar(first) || !IsScalar(second))
                            {
                                throw BmFontRecord.Error(lineNumber, "invalid kerning code point");
                            }

                            var key = BmFontData.PairKey(first, second);
                            if (kernings.ContainsKey(key))
                            {
                                throw BmFontRecord.Error(lineNumber, "duplicate kerning pair");
                            }

                            kernings.Add(key, amount);
                            break;
                        default:
                            throw BmFontRecord.Error(lineNumber, "unsupported record '" + r.Name + "'");
                    }
                }
            }

            if (!hasInfo || !hasCommon || glyphCount < 0 || kerningCount < 0)
            {
                throw new FormatException("BMFont is missing a required info, common, chars or kernings record.");
            }

            if (glyphCount != glyphs.Count || kerningCount != kernings.Count || pageCount != pageMap.Count)
            {
                throw new FormatException("BMFont declared counts do not match its records.");
            }

            var pageNames = new string[pageCount];
            for (var i = 0; i < pageNames.Length; i++)
            {
                if (!pageMap.TryGetValue(i, out pageNames[i]))
                {
                    throw new FormatException("BMFont page ids must be contiguous from zero.");
                }
            }

            foreach (BmFontGlyph glyph in glyphs.Values)
            {
                if (glyph.Page >= pageCount || (long)glyph.X + glyph.Width > atlasWidth || (long)glyph.Y + glyph.Height > atlasHeight)
                {
                    throw new FormatException("Glyph U+" + glyph.CodePoint.ToString("X4", CultureInfo.InvariantCulture) + " is outside its atlas page.");
                }
            }

            return new BmFontData(face, pointSize, lineHeight, baseline, atlasWidth, atlasHeight, pageNames, glyphs, kernings);
        }

        /// <summary>Performs is scalar.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting is scalar.</returns>
        private static bool IsScalar(int value)
        {
            return value >= 0 && value <= 0x10ffff && (value < 0xd800 || value > 0xdfff);
        }
    }
}
