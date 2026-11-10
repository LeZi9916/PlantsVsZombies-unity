using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PlantsVsZombies.UI.BitmapFonts;

internal static class Program
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static string Workspace()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !Directory.Exists(Path.Combine(current.FullName, "extracted_assets")))
            current = current.Parent;
        if (current == null) throw new Exception("Workspace root not found.");
        return current.FullName;
    }

    private static void Main()
    {
        string folder = Path.Combine(Workspace(), "extracted_assets", "converted", "fonts");
        string[] files = Directory.GetFiles(folder, "*.fnt");
        Check(files.Length == 25, "Expected 25 converted layers.");
        foreach (string file in files)
        {
            BmFontData font = BmFontData.Parse(File.ReadAllText(file, new UTF8Encoding(false, true)));
            Check(font.GlyphCount >= 1514, "Unexpected glyph count: " + file);
            foreach (string page in font.Pages)
            {
                string pagePath = Path.Combine(folder, page);
                Check(File.Exists(pagePath), "Missing atlas: " + page);
                byte[] png = File.ReadAllBytes(pagePath);
                Check(png.Length >= 24 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71 &&
                    png[4] == 13 && png[5] == 10 && png[6] == 26 && png[7] == 10 &&
                    png[12] == 73 && png[13] == 72 && png[14] == 68 && png[15] == 82 &&
                    BigEndian(png, 16) == font.AtlasWidth && BigEndian(png, 20) == font.AtlasHeight,
                    "PNG header or atlas dimensions: " + page);
            }
        }
        for (int i = 0; i < 3; i++)
        {
            int size = new[] { 12, 15, 18 }[i];
            int advance = new[] { 12, 14, 16 }[i];
            int height = new[] { 13, 16, 18 }[i];
            string name = "DwarvenTodcraft" + size + "__Main.fnt";
            BmFontData font = BmFontData.Parse(File.ReadAllText(Path.Combine(folder, name)));
            CompareRawFont(Workspace(), font, size);
            BmFontGlyph glyph;
            Check(font.TryGetGlyph(0x4e2d, out glyph) && glyph.XAdvance == advance, name + " Chinese advance");
            BitmapTextLayout layout = BitmapTextLayout.Create(font, "\u4e2d\u6587\r\nA", 2f);
            Check(layout.MissingGlyphs.Count == 0 && layout.Width == advance * 4 &&
                layout.Height == height * 4 && layout.LineCount == 2, name + " multiline metrics");
            layout = BitmapTextLayout.Create(font, "\u4e2d\u6587\u4e00", 1f, advance * 2);
            Check(layout.LineCount == 2 && layout.LineWidths[0] == advance * 2 &&
                layout.LineWidths[1] == advance, name + " wrapping");
            layout = BitmapTextLayout.Create(font, "A\U0001f600\u9f98");
            Check(layout.MissingGlyphs.Count == 2 && layout.MissingGlyphs[0].CodePoint == 0x1f600 &&
                layout.MissingGlyphs[0].Utf16Index == 1 && layout.MissingGlyphs[1].Utf16Index == 3,
                name + " missing glyph report");
            bool threw = false;
            try { layout.ThrowIfMissing(); } catch (MissingBitmapGlyphException) { threw = true; }
            Check(threw, name + " missing glyph exception");
        }
        BmFontData pix = BmFontData.Parse(File.ReadAllText(Path.Combine(folder, "Pix118Bold__Main.fnt")));
        Check(pix.KerningCount == 128 && pix.GetKerning('A', 'V') == -1, "Kerning table");
        BitmapTextLayout kerned = BitmapTextLayout.Create(pix, "AV");
        BmFontGlyph a, v;
        Check(pix.TryGetGlyph('A', out a) && pix.TryGetGlyph('V', out v) &&
            kerned.Width == a.XAdvance + v.XAdvance - 1, "Kerning layout");
        VerifyMalformedInput(folder);
        VerifySyntheticData();
        Console.WriteLine("PASS: " + checks + " checks across " + files.Length + " converted layers.");
    }

    private static int Number(object value)
    {
        return int.Parse((string)value, CultureInfo.InvariantCulture);
    }

    private static int BigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static void CompareRawFont(string root, BmFontData font, int size)
    {
        string name = "DwarvenTodcraft" + size;
        string rawPath = Path.Combine(root, "extracted_assets", "raw", "data", name + ".txt");
        var raw = new RawFontReader(File.ReadAllText(rawPath, new UTF8Encoding(false, true)));
        List<object> chars = raw.Definition("CharList0");
        List<object> widths = raw.Definition("WidthList0");
        List<object> rects = raw.Definition("RectList0");
        List<object> offsets = raw.Definition("OffsetList0");
        Check(chars.Count == font.GlyphCount && chars.Count == widths.Count && chars.Count == rects.Count &&
            chars.Count == offsets.Count, name + " raw list lengths");
        Check(raw.LayerNumber("LayerSetAscent") == font.Base && raw.LayerNumber("LayerSetHeight") == font.LineHeight &&
            raw.LayerNumber("LayerSetPointSize") == font.PointSize, name + " raw line metrics");
        for (int i = 0; i < chars.Count; i++)
        {
            string text = (string)chars[i];
            Check(text.Length > 1 && text[0] == '\u0001', name + " quoted character");
            int scalar = char.ConvertToUtf32(text, 1);
            var rect = (List<object>)rects[i];
            var offset = (List<object>)offsets[i];
            BmFontGlyph glyph;
            Check(font.TryGetGlyph(scalar, out glyph), name + " missing converted scalar " + scalar);
            Check(rect.Count == 4 && offset.Count == 2 && glyph.X == Number(rect[0]) && glyph.Y == Number(rect[1]) &&
                glyph.Width == Number(rect[2]) && glyph.Height == Number(rect[3]) &&
                glyph.XOffset == Number(offset[0]) && glyph.YOffset == Number(offset[1]) &&
                glyph.XAdvance == Number(widths[i]), name + " converted metrics for U+" + scalar.ToString("X4"));
        }
        string rawPng = Path.Combine(root, "extracted_assets", "raw", "data", name + ".png");
        string convertedPng = Path.Combine(root, "extracted_assets", "converted", "fonts", font.Pages[0]);
        Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(rawPng))) ==
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(convertedPng))), name + " atlas SHA-256");
    }

    private static void ExpectFailure<T>(Action action, string message) where T : Exception
    {
        bool threw = false;
        try { action(); } catch (T) { threw = true; }
        Check(threw, message);
    }

    private static void VerifyMalformedInput(string folder)
    {
        string valid = File.ReadAllText(Path.Combine(folder, "DwarvenTodcraft12__Main.fnt"));
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid.Replace("chars count=1514", "chars count=1513")), "Wrong count rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid.Replace("packed=0", "packed=1")), "Packed atlas rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid.Replace("file=\"DwarvenTodcraft12.png\"", "file=\"../bad.png\"")), "Atlas traversal rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid.Replace("id=32 x=0", "id=33 x=0")), "Duplicate glyph rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid.Replace("width=19 height=19", "width=99999 height=19")), "Out-of-bounds glyph rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(valid + "unknown foo=1\n"), "Unknown records rejected");
        BmFontData font = BmFontData.Parse(valid);
        ExpectFailure<ArgumentException>(() => BitmapTextLayout.Create(font, "\ud800"), "Unpaired surrogate rejected");
        ExpectFailure<ArgumentOutOfRangeException>(() => BitmapTextLayout.Create(font, "A", 0), "Zero scale rejected");
        ExpectFailure<ArgumentOutOfRangeException>(() => BitmapTextLayout.Create(font, "A", 1, 0), "Zero wrap width rejected");
        Check(BitmapTextLayout.Create(font, "").LineCount == 1, "Empty text has one line");
    }

    private static void VerifySyntheticData()
    {
        const string synthetic = "\ufeffinfo face=\"Face \\\"Two\\\"\" size=12 unicode=1\n" +
            "common lineHeight=10 base=8 scaleW=32 scaleH=32 pages=2 packed=0\n" +
            "page id=0 file=\"page one.png\"\npage id=1 file=\"page two.png\"\n" +
            "chars count=4\n" +
            "char id=65 x=0 y=0 width=4 height=6 xoffset=-1 yoffset=-2 xadvance=6 page=0 chnl=15\n" +
            "char id=86 x=4 y=0 width=4 height=6 xoffset=1 yoffset=0 xadvance=7 page=1 chnl=15\n" +
            "char id=32 x=0 y=0 width=0 height=0 xoffset=0 yoffset=0 xadvance=3 page=0 chnl=15\n" +
            "char id=128512 x=8 y=0 width=4 height=6 xoffset=0 yoffset=0 xadvance=8 page=1 chnl=15\n" +
            "kernings count=1\nkerning first=65 second=86 amount=-2\n";
        BmFontData font = BmFontData.Parse(synthetic);
        Check(font.Face == "Face \"Two\"" && font.Pages.Count == 2 && font.Pages[1] == "page two.png", "Quoted data and pages");
        BitmapTextLayout layout = BitmapTextLayout.Create(font, "AV\r\nA\n", 2f);
        Check(layout.LineCount == 3 && layout.Width == 22 && layout.Height == 60 &&
            layout.LineWidths[0] == 22 && layout.LineWidths[1] == 12 && layout.LineWidths[2] == 0,
            "CRLF, trailing newline, scale, and kerning");
        Check(layout.Glyphs[0].X == -2 && layout.Glyphs[0].Y == -4 && layout.Glyphs[1].X == 10 &&
            layout.MinX == -2 && layout.MinY == -4 && layout.MaxX == 18 && layout.MaxY == 28,
            "Scaled offsets and union of glyph rectangles");
        layout = BitmapTextLayout.Create(font, "AVA", 1f, 11f);
        Check(layout.LineCount == 2 && layout.LineWidths[0] == 11 && layout.LineWidths[1] == 6 &&
            layout.Glyphs[2].Y == 8, "Character wrap and kerning reset");
        layout = BitmapTextLayout.Create(font, "\U0001f600A", 1f, 3f);
        Check(layout.MissingGlyphs.Count == 0 && layout.LineCount == 2 && layout.LineWidths[0] == 8 &&
            layout.Glyphs[0].Glyph.Page == 1 && layout.Glyphs[1].Utf16Index == 2,
            "Supplementary scalar, oversized first glyph, and page selection");
        layout = BitmapTextLayout.Create(font, "A\u4e2dVA");
        Check(layout.MissingGlyphs.Count == 1 && layout.MissingGlyphs[0].Utf16Index == 1 &&
            layout.Width == 19, "Missing scalar breaks kerning without implicit fallback");
        Check(BitmapTextLayout.Create(font, " ").HasGlyphBounds == false, "Space has no glyph rectangle");
        ExpectFailure<FormatException>(() => BmFontData.Parse(synthetic.Replace("id=1 file=", "id=2 file=")), "Noncontiguous pages rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(synthetic.Replace("chnl=15", "chnl=4")), "Unsupported channel rejected");
        ExpectFailure<FormatException>(() => BmFontData.Parse(synthetic.Replace("kernings count=1", "kernings count=2")), "Kerning count mismatch rejected");
        ExpectFailure<ArgumentException>(() => BitmapTextLayout.Create(font, "\udc00"), "Unpaired low surrogate rejected");
    }
}
