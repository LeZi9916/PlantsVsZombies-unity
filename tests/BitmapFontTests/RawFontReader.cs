using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Independent reader for the raw ImageFont lists used to cross-check the converted layers.
internal sealed class RawFontReader
{
    private readonly string source;
    private int position;
    private string pending;

    public RawFontReader(string source) { this.source = source; }

    public List<object> Definition(string name)
    {
        Seek("Define " + name);
        Expect("Define");
        Expect(name);
        List<object> values = List();
        Expect(";");
        return values;
    }

    public int LayerNumber(string property)
    {
        Seek(property);
        Expect(property);
        Expect("Main");
        int value = int.Parse(Next(), CultureInfo.InvariantCulture);
        Expect(";");
        return value;
    }

    private void Seek(string marker)
    {
        position = source.IndexOf(marker, StringComparison.Ordinal);
        pending = null;
        if (position < 0) throw new Exception("Missing raw font declaration: " + marker);
    }

    private List<object> List()
    {
        Expect("(");
        var list = new List<object>();
        while (Peek() != ")")
        {
            if (Peek() == ",") { Next(); continue; }
            if (Peek() == "(") list.Add(List());
            else list.Add(Next());
        }
        Expect(")");
        return list;
    }

    private void Expect(string expected)
    {
        string actual = Next();
        if (actual != expected) throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
    }

    private string Peek()
    {
        if (pending == null) pending = Read();
        return pending;
    }

    private string Next()
    {
        if (pending == null) return Read();
        string value = pending;
        pending = null;
        return value;
    }

    private string Read()
    {
        while (position < source.Length && char.IsWhiteSpace(source[position])) position++;
        if (position == source.Length) throw new Exception("Unexpected end of raw font descriptor.");
        char c = source[position++];
        if (c == '(' || c == ')' || c == ',' || c == ';') return c.ToString();
        if (c == '\'' || c == '"')
        {
            char quote = c;
            var value = new StringBuilder();
            while (position < source.Length)
            {
                c = source[position++];
                if (c == quote) return "\u0001" + value;
                if (c == '\\')
                {
                    if (position == source.Length) break;
                    c = source[position++];
                    if (c != '\\' && c != '\'' && c != '"') throw new Exception("Unsupported raw font escape.");
                }
                value.Append(c);
            }
            throw new Exception("Unterminated raw font string.");
        }
        int start = position - 1;
        while (position < source.Length && !char.IsWhiteSpace(source[position]) &&
            source[position] != '(' && source[position] != ')' && source[position] != ',' && source[position] != ';')
            position++;
        return source.Substring(start, position - start);
    }
}
