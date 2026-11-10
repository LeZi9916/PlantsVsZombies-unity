using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PvZReplica.UI;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

public sealed class OptionsViewSmoke : MonoBehaviour
{
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr state);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);

    private readonly Dictionary<string, Color32[]> captures = new Dictionary<string, Color32[]>();
    private readonly List<float> baselines = new List<float>();
    private OptionsView view;
    private ImGuiBitmapFont labelFont;
    private string output;
    private string failure;
    private int repaints;

    private void Start()
    {
        Application.runInBackground = true;
        string[] args = Environment.GetCommandLineArgs();
        string root = null;
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "-options-output") output = args[i + 1];
            if (args[i] == "-options-root") root = args[i + 1];
        }
        if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(root)) { Fail("Missing test arguments"); return; }
        Directory.CreateDirectory(output);
        try
        {
            MoveWindowOffscreen();
            labelFont = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft18");
            view = new OptionsView(root);
            view.OriginalRightLabel = (text, right, baseline, color) =>
            {
                if (Event.current.type == EventType.Repaint) baselines.Add(baseline);
                BitmapTextLayout layout = labelFont.Layout(text);
                if (layout.MissingGlyphs.Count != 0) return false;
                labelFont.Draw(layout, right - layout.Width, baseline - labelFont.Data.Base, color);
                return true;
            };
            view.GameplayButtonsEnabled = true;
            view.Open(.25f, .75f, false);
            StartCoroutine(Run());
        }
        catch (Exception error) { Fail(error.ToString()); }
    }

    private static void MoveWindowOffscreen()
    {
        uint process = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr target = IntPtr.Zero;
        EnumWindows((window, state) =>
        {
            uint owner;
            GetWindowThreadProcessId(window, out owner);
            if (owner != process) return true;
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() != "UnityWndClass") return true;
            target = window;
            return false;
        }, IntPtr.Zero);
        if (target == IntPtr.Zero || !SetWindowPos(target, IntPtr.Zero, -10000, -10000, 0, 0, 0x0001 | 0x0004 | 0x0010))
            throw new Exception("Unable to position isolated player");
        ShowWindow(target, 4);
    }

    private void OnGUI()
    {
        if (view == null || failure != null) return;
        try
        {
            if (Event.current.type == EventType.Repaint) baselines.Clear();
            Color previous = GUI.color;
            view.Draw();
            if (GUI.color != previous) throw new Exception("Draw did not restore GUI.color");
            if (Event.current.type == EventType.Repaint) repaints++;
        }
        catch (Exception error) { failure = error.ToString(); }
    }

    private void Pointer(float x, float y, EventType type)
    {
        if (!view.HandlePointer(new Vector2(x, y), type)) throw new Exception("Pointer not consumed");
    }

    private IEnumerator Run()
    {
        yield return Capture("board-normal");
        Pointer(300f, 460f, EventType.MouseMove);
        yield return Capture("board-hover");
        Pointer(300f, 460f, EventType.MouseDown);
        yield return Capture("board-pressed");
        Pointer(10f, 10f, EventType.MouseUp);
        Pointer(300f, 350f, EventType.MouseMove);
        Pointer(300f, 350f, EventType.MouseDown);
        yield return Capture("menu-pressed");
        Pointer(10f, 10f, EventType.MouseUp);
        Pointer(300f, 350f, EventType.MouseDown);
        Pointer(300f, 350f, EventType.MouseUp);
        yield return Capture("restart-confirmation");
        Pointer(450f, 380f, EventType.MouseDown);
        Pointer(450f, 380f, EventType.MouseUp);
        view.Close();
        view.GameplayButtonsEnabled = false;
        view.Open(.25f, .75f, false);
        yield return Capture("selector");
        try
        {
            if (failure != null) throw new Exception(failure);
            if (repaints < 6 || captures.Count != 6) throw new Exception("Missing render frames");
            if (baselines.Count != 4 || baselines[0] != 196f || baselines[1] != 228f ||
                baselines[2] != 263f || baselines[3] != 300f) throw new Exception("Incorrect selector label anchors");
            int hoverChanges = Difference("board-normal", "board-hover", 218, 432, 360, 100);
            int hoverBackgroundChanges = Difference("board-normal", "board-hover", 250, 445, 100, 10);
            int pressChanges = Difference("board-hover", "board-pressed", 218, 432, 360, 100);
            int selectorChanges = Difference("board-normal", "selector", 300, 335, 190, 85);
            int dialogChanges = Difference("board-normal", "restart-confirmation", 193, 154, 413, 292);
            if (hoverChanges < 5 || hoverBackgroundChanges != 0 || pressChanges < 100 ||
                selectorChanges < 100 || dialogChanges < 1000)
                throw new Exception("Visual checks failed: hover=" + hoverChanges + " hoverBackground=" + hoverBackgroundChanges +
                    " press=" + pressChanges + " selector=" + selectorChanges + " dialog=" + dialogChanges);
            string result = "PASS\nrepaints=" + repaints + " hover=" + hoverChanges + " hoverBackground=" + hoverBackgroundChanges +
                " press=" + pressChanges + " selector=" + selectorChanges + " dialog=" + dialogChanges + "\n";
            File.WriteAllText(Path.Combine(output, "result.txt"), result, Encoding.UTF8);
            Debug.Log("OPTIONS_VIEW_SMOKE_PASS " + result);
            Application.Quit(0);
        }
        catch (Exception error) { Fail(error.ToString()); }
    }

    private IEnumerator Capture(string name)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Texture2D image = null;
        try
        {
            if (failure != null) throw new Exception(failure);
            image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image == null || image.width != 800 || image.height != 600)
                throw new Exception("Unexpected render size: " + Screen.width + "x" + Screen.height);
            captures.Add(name, image.GetPixels32());
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        }
        catch (Exception error) { Fail(error.ToString()); }
        finally { if (image != null) Destroy(image); }
    }

    private int Difference(string first, string second, int x, int y, int width, int height)
    {
        Color32[] a = captures[first], b = captures[second];
        int count = 0;
        for (int row = y; row < y + height; row++)
            for (int column = x; column < x + width; column++)
            {
                int index = (599 - row) * 800 + column;
                if (!a[index].Equals(b[index])) count++;
            }
        return count;
    }

    private void Fail(string message)
    {
        failure = message;
        Debug.LogError("OPTIONS_VIEW_SMOKE_FAIL " + message);
        if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL\n" + message, Encoding.UTF8);
        Application.Quit(1);
    }

    private void OnDestroy()
    {
        if (view != null) view.Dispose();
        if (labelFont != null) labelFont.Dispose();
    }
}
