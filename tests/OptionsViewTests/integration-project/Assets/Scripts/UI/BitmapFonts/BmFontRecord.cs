using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PvZReplica.UI.BitmapFonts
{
    internal sealed class BmFontRecord
    {
        private readonly Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly int line;
        public string Name { get; private set; }

        private BmFontRecord(int lineNumber) { line = lineNumber; }

        public static FormatException Error(int line, string message)
        {
            return new FormatException("BMFont line " + line.ToString(CultureInfo.InvariantCulture) + ": " + message + ".");
        }

        public string Field(string name)
        {
            string value;
            if (!fields.TryGetValue(name, out value)) throw Error(line, "missing '" + name + "'");
            return value;
        }

        public int Number(string name)
        {
            int value;
            if (!int.TryParse(Field(name), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                throw Error(line, "invalid integer '" + name + "'");
            return value;
        }

        public static BmFontRecord Read(string text, int lineNumber)
        {
            int i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i == text.Length) return null;
            int start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
            var record = new BmFontRecord(lineNumber) { Name = text.Substring(start, i - start) };
            while (i < text.Length)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                if (i == text.Length) break;
                start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '=') i++;
                if (i == start || i == text.Length || text[i] != '=') throw Error(lineNumber, "expected key=value");
                string key = text.Substring(start, i - start);
                i++;
                if (i == text.Length) throw Error(lineNumber, "missing value for '" + key + "'");
                string value;
                if (text[i] == '"')
                {
                    i++;
                    var buffer = new StringBuilder();
                    bool closed = false;
                    while (i < text.Length)
                    {
                        char c = text[i++];
                        if (c == '"') { closed = true; break; }
                        if (c == '\\')
                        {
                            if (i == text.Length || (text[i] != '\\' && text[i] != '"'))
                                throw Error(lineNumber, "invalid quoted escape");
                            c = text[i++];
                        }
                        buffer.Append(c);
                    }
                    if (!closed || (i < text.Length && !char.IsWhiteSpace(text[i])))
                        throw Error(lineNumber, "unterminated or invalid quoted value");
                    value = buffer.ToString();
                }
                else
                {
                    start = i;
                    while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
                    value = text.Substring(start, i - start);
                }
                if (record.fields.ContainsKey(key)) throw Error(lineNumber, "duplicate key '" + key + "'");
                record.fields.Add(key, value);
            }
            return record;
        }
    }
}
