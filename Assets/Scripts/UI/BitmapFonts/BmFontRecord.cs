#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Defines bm font record.</summary>
    internal sealed class BmFontRecord
    {
        /// <summary>Stores fields.</summary>
        private readonly Dictionary<string, string> _fields = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>Stores line.</summary>
        private readonly int _line;
        /// <summary>Gets or sets name.</summary>
        public string Name { get; private set; }

        /// <summary>Initializes bm font record.</summary>
        /// <param name = "lineNumber">The line number.</param>
        private BmFontRecord(int lineNumber)
        {
            _line = lineNumber;
        }

        /// <summary>Performs error.</summary>
        /// <param name = "line">The line.</param>
        /// <param name = "message">The message.</param>
        /// <returns>The resulting error.</returns>
        public static FormatException Error(int line, string message)
        {
            return new FormatException("BMFont line " + line.ToString(CultureInfo.InvariantCulture) + ": " + message + ".");
        }

        /// <summary>Performs field.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting field.</returns>
        public string Field(string name)
        {
            string value;
            if (!_fields.TryGetValue(name, out value))
            {
                throw Error(_line, "missing '" + name + "'");
            }

            return value;
        }

        /// <summary>Performs number.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting number.</returns>
        public int Number(string name)
        {
            int value;
            if (!int.TryParse(Field(name), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                throw Error(_line, "invalid integer '" + name + "'");
            }

            return value;
        }

        /// <summary>Performs read.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "lineNumber">The line number.</param>
        /// <returns>The resulting read.</returns>
        public static BmFontRecord Read(string text, int lineNumber)
        {
            var i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i == text.Length)
            {
                return null;
            }

            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            var record = new BmFontRecord(lineNumber)
            {
                Name = text.Substring(start, i - start)
            };
            while (i < text.Length)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                if (i == text.Length)
                {
                    break;
                }

                start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '=')
                {
                    i++;
                }

                if (i == start || i == text.Length || text[i] != '=')
                {
                    throw Error(lineNumber, "expected key=value");
                }

                var key = text.Substring(start, i - start);
                i++;
                if (i == text.Length)
                {
                    throw Error(lineNumber, "missing value for '" + key + "'");
                }

                string value;
                if (text[i] == '"')
                {
                    i++;
                    var buffer = new StringBuilder();
                    var closed = false;
                    while (i < text.Length)
                    {
                        var c = text[i++];
                        if (c == '"')
                        {
                            closed = true;
                            break;
                        }

                        if (c == '\\')
                        {
                            if (i == text.Length || (text[i] != '\\' && text[i] != '"'))
                            {
                                throw Error(lineNumber, "invalid quoted escape");
                            }

                            c = text[i++];
                        }

                        buffer.Append(c);
                    }

                    if (!closed || (i < text.Length && !char.IsWhiteSpace(text[i])))
                    {
                        throw Error(lineNumber, "unterminated or invalid quoted value");
                    }

                    value = buffer.ToString();
                }
                else
                {
                    start = i;
                    while (i < text.Length && !char.IsWhiteSpace(text[i]))
                    {
                        i++;
                    }

                    value = text.Substring(start, i - start);
                }

                if (record._fields.ContainsKey(key))
                {
                    throw Error(lineNumber, "duplicate key '" + key + "'");
                }

                record._fields.Add(key, value);
            }

            return record;
        }
    }
}
