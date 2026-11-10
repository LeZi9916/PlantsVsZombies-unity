using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

public sealed class BitmapFontSmoke : MonoBehaviour
{
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr state);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    private readonly ImGuiBitmapFont[] fonts = new ImGuiBitmapFont[3];
    private readonly string[] names = { "DwarvenTodcraft12", "DwarvenTodcraft15", "DwarvenTodcraft18" };
    private readonly string[] characters = { "\u4e2d", "\u6587", "\u690d" };
    private readonly Color[] colors = { Color.white, new Color(1f, 0.35f, 0.2f, 1f), new Color(0.25f, 1f, 0.5f, 1f) };
    private readonly int[] positions = { 80, 220, 400 };
    private string output;
    private string failure;
    private int repaints;

    private void Start()
    {
        Application.runInBackground = true;
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++)
            if (arguments[i] == "-bitmap-font-output") output = arguments[i + 1];
        if (string.IsNullOrEmpty(output)) { Debug.LogError("Missing output directory."); Application.Quit(2); return; }
        Directory.CreateDirectory(output);
        try
        {
            MoveWindowOffscreen();
            for (int i = 0; i < fonts.Length; i++) fonts[i] = ImGuiBitmapFont.LoadFromResources(names[i]);
            var missing = fonts[0].Layout("A\U0001f600");
            bool rejected = false;
            try { fonts[0].Draw(missing, 0, 0, Color.white); }
            catch (MissingBitmapGlyphException e) { rejected = e.MissingGlyphs[0].CodePoint == 0x1f600; }
            if (!rejected) throw new Exception("Missing glyph was not rejected.");
            var multiline = fonts[2].Layout("\u4e2d\u6587\n\u690d\u7269", 2f);
            if (multiline.LineCount != 2 || multiline.MissingGlyphs.Count != 0)
                throw new Exception("Unexpected multiline layout.");
            StartCoroutine(Validate());
        }
        catch (Exception e) { Fail(e.ToString()); }
    }

    private static void MoveWindowOffscreen()
    {
        uint ownProcess = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr target = IntPtr.Zero;
        EnumWindows((window, state) =>
        {
            uint processId;
            GetWindowThreadProcessId(window, out processId);
            if (processId != ownProcess) return true;
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() != "UnityWndClass") return true;
            target = window;
            return false;
        }, IntPtr.Zero);
        if (target == IntPtr.Zero) throw new Exception("Could not find this player's window.");
        const uint noSizeZOrderOrActivation = 0x0001 | 0x0004 | 0x0010;
        if (!SetWindowPos(target, IntPtr.Zero, -10000, -10000, 0, 0, noSizeZOrderOrActivation))
            throw new Exception("Could not move the test window offscreen.");
        ShowWindow(target, 4); // SW_SHOWNOACTIVATE, after moving it outside the desktop.
    }

    private void OnGUI()
    {
        if (fonts[2] == null || failure != null) return;
        try
        {
            Color before = GUI.color;
            for (int i = 0; i < fonts.Length; i++)
                fonts[i].Draw(characters[i], positions[i], 100, colors[i], i + 1);
            fonts[2].Draw("\u4e2d\u6587\n\u690d\u7269", 80, 260, Color.white, 2f);
            fonts[1].Draw("\u4e2d\u6587\u690d\u7269", 320, 260, Color.white, 2f, 56f);
            if (GUI.color != before) throw new Exception("GUI.color was not restored.");
            if (Event.current.type == EventType.Repaint) repaints++;
        }
        catch (Exception e) { failure = e.ToString(); }
    }

    private IEnumerator Validate()
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = null;
        try
        {
            if (failure != null) throw new Exception(failure);
            if (repaints == 0) throw new Exception("OnGUI never repainted.");
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null || screenshot.width != 800 || screenshot.height != 600)
                throw new Exception("Unexpected screenshot size: " + Screen.width + "x" + Screen.height);
            File.WriteAllBytes(Path.Combine(output, "bitmap-font-smoke.png"), screenshot.EncodeToPNG());
            var report = new StringBuilder();
            for (int i = 0; i < fonts.Length; i++)
                CompareGlyph(screenshot, i, report);
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS\n" + report, Encoding.UTF8);
            Debug.Log("BITMAP_FONT_SMOKE_PASS\n" + report);
            Application.Quit(0);
        }
        catch (Exception e) { Fail(e.ToString()); }
        finally { if (screenshot != null) Destroy(screenshot); }
    }

    private void CompareGlyph(Texture2D screen, int index, StringBuilder report)
    {
        ImGuiBitmapFont font = fonts[index];
        BmFontGlyph glyph;
        if (!font.Data.TryGetGlyph(characters[index][0], out glyph)) throw new Exception("Missing test glyph.");
        string atlasName = Path.GetFileNameWithoutExtension(font.Data.Pages[glyph.Page]);
        TextAsset bytes = Resources.Load<TextAsset>(ImGuiBitmapFont.DefaultResourceRoot + "/Atlases/" + atlasName);
        var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (bytes == null || !ImageConversion.LoadImage(atlas, bytes.bytes, false)) throw new Exception("Test atlas decode failed.");
            int scale = index + 1;
            int left = positions[index] + glyph.XOffset * scale;
            int top = 100 + glyph.YOffset * scale;
            double error = 0;
            int samples = 0, ink = 0;
            for (int y = 0; y < glyph.Height; y++)
            for (int x = 0; x < glyph.Width; x++)
            {
                Color source = atlas.GetPixel(glyph.X + x, atlas.height - 1 - glyph.Y - y);
                Color actual = screen.GetPixel(left + x * scale + scale / 2,
                    screen.height - 1 - (top + y * scale + scale / 2));
                Color expected = new Color(source.r * colors[index].r * source.a,
                    source.g * colors[index].g * source.a, source.b * colors[index].b * source.a, 1f);
                error += (Math.Abs(expected.r - actual.r) + Math.Abs(expected.g - actual.g) + Math.Abs(expected.b - actual.b)) / 3.0;
                if (source.a > 0.1f && source.r + source.g + source.b > 0.1f) ink++;
                samples++;
            }
            double mean = error / samples;
            report.Append(names[index]).Append(" scale=").Append(scale).Append(" ink=").Append(ink)
                .Append(" meanRGBError=").Append(mean.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            if (ink < 5 || mean > 0.06) throw new Exception("Pixel comparison failed: " + report);
        }
        finally { Destroy(atlas); }
    }

    private void Fail(string message)
    {
        failure = message;
        Debug.LogError(message);
        if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL\n" + message, Encoding.UTF8);
        Application.Quit(1);
    }

    private void OnDestroy()
    {
        foreach (ImGuiBitmapFont font in fonts) if (font != null) font.Dispose();
    }
}
