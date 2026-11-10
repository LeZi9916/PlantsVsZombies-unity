using System;
using System.IO;
using System.Reflection;
using PvZReplica.UI;
using UnityEditor;
using UnityEngine;

public static class HelpEditorTests
{
    private static int assertions;

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception(description);
    }

    private static void Throws<T>(Action action, string description) where T : Exception
    {
        bool matched = false;
        try { action(); }
        catch (T) { matched = true; }
        Check(matched, description);
    }

    private static bool Pointer(HelpView view, float x, float y, EventType type)
    {
        return view.HandlePointer(new Vector2(x, y), type);
    }

    private static Texture2D Texture(HelpView view, string name)
    {
        return (Texture2D)typeof(HelpView).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
    }

    private static void ErrorCases(string root, string work)
    {
        Throws<ArgumentException>(() => new HelpView(null), "null root");
        Throws<DirectoryNotFoundException>(() => new HelpView(Path.Combine(work, "missing")), "missing root");
        string badStrings = Path.Combine(work, "bad-strings");
        Directory.CreateDirectory(Path.Combine(badStrings, "properties"));
        File.WriteAllText(Path.Combine(badStrings, "properties", "LawnStrings.txt"), "[OTHER]\nvalue\n");
        Throws<InvalidDataException>(() => new HelpView(badStrings), "missing label is reported");

        string badMask = Path.Combine(work, "bad-mask");
        string images = Path.Combine(badMask, "images");
        Directory.CreateDirectory(images);
        Directory.CreateDirectory(Path.Combine(badMask, "properties"));
        File.Copy(Path.Combine(root, "properties", "LawnStrings.txt"),
            Path.Combine(badMask, "properties", "LawnStrings.txt"), true);
        File.Copy(Path.Combine(root, "images", "background1.jpg"), Path.Combine(images, "background1.jpg"), true);
        File.Copy(Path.Combine(root, "images", "ZombieNote.jpg"), Path.Combine(images, "ZombieNote.jpg"), true);
        File.Copy(Path.Combine(root, "images", "ZombieNoteHelp.png"), Path.Combine(images, "ZombieNote_.png"), true);
        Throws<InvalidDataException>(() => new HelpView(badMask), "mask size mismatch is reported");
        File.WriteAllText(Path.Combine(images, "ZombieNote_.png"), "not an image");
        Throws<InvalidDataException>(() => new HelpView(badMask), "corrupt mask is reported");
    }

    public static void Run()
    {
        try
        {
            string root = Environment.GetEnvironmentVariable("PVZ_HELP_RESOURCE_ROOT");
            string work = Environment.GetEnvironmentVariable("PVZ_HELP_TEST_DIR");
            Check(Directory.Exists(root) && Directory.Exists(work), "test environment");
            ErrorCases(root, work);

            HelpView view = new HelpView(root);
            Check(!view.IsOpen && view.CloseResult == HelpCloseResult.None, "initially closed");
            Check(HelpView.PageCount == 1, "single help page");
            Rect bounds = HelpView.ReturnButtonBounds;
            Check(bounds.x == 324 && bounds.y == 500 && bounds.width == 156 && bounds.height == 42, "return bounds");
            Check(!view.HandleKey(KeyCode.Escape) && !Pointer(view, 400, 520, EventType.MouseDown), "closed input");
            view.Draw();

            Texture2D paper = Texture(view, "paper");
            Texture2D writing = Texture(view, "writing");
            Check(paper.width == 654 && paper.height == 427, "paper dimensions");
            Check(paper.GetPixel(0, 0).a == 0 && paper.GetPixel(320, 213).a > .99f, "paper alpha mask");
            Check(writing.width == 529 && writing.height == 323, "writing dimensions");
            Color32[] pixels = writing.GetPixels32();
            int ink = 0, clear = 0;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a > 200) { ink++; Check(pixel.r == 0 && pixel.g == 0 && pixel.b == 0, "ink is black"); }
                if (pixel.a == 0) clear++;
            }
            Check(ink > 1000 && clear > 1000, "writing uses grayscale alpha");
            string label = (string)typeof(HelpView).GetField("returnLabel",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            Check(label == "主菜单", "label from original Chinese strings");

            view.Open();
            Throws<InvalidOperationException>(() => view.Open(), "double open rejected");
            Check(!view.HandleKey(KeyCode.A), "unhandled key");
            Check(!view.HandlePointer(Vector2.zero, EventType.ScrollWheel), "unsupported pointer event");
            Check(Pointer(view, 0, 0, EventType.MouseDown), "modal outside click consumed");
            Check(Pointer(view, 400, 520, EventType.MouseUp) && view.IsOpen, "down outside cannot activate");
            Check(Pointer(view, 324, 500, EventType.MouseMove) && view.IsReturnHovered, "inclusive top-left");
            Check(Pointer(view, 480, 520, EventType.MouseMove) && !view.IsReturnHovered, "exclusive right edge");
            Check(Pointer(view, 400, 542, EventType.MouseMove) && !view.IsReturnHovered, "exclusive bottom edge");
            Check(Pointer(view, 400, 520, EventType.MouseDown) && view.IsReturnPressed, "press captures");
            Check(Pointer(view, 10, 10, EventType.MouseDrag) && !view.IsReturnPressed, "drag out");
            Check(Pointer(view, 10, 10, EventType.MouseUp) && view.IsOpen, "release outside cancels");
            Pointer(view, 400, 520, EventType.MouseDown);
            Pointer(view, 10, 10, EventType.MouseDrag);
            Check(Pointer(view, 400, 520, EventType.MouseDrag) && view.IsReturnPressed, "drag back restores press");
            Check(Pointer(view, 400, 520, EventType.MouseUp), "release consumed");
            Check(!view.IsOpen && view.CloseResult == HelpCloseResult.ReturnToMainMenu, "button returns to menu");
            Check(!Pointer(view, 400, 520, EventType.MouseUp), "no stale activation");

            foreach (KeyCode key in new[] { KeyCode.Escape, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space })
            {
                view.Open();
                Check(view.CloseResult == HelpCloseResult.None && view.IsOpen, "reopen clears result");
                Check(view.HandleKey(key) && !view.IsOpen && view.CloseResult == HelpCloseResult.ReturnToMainMenu,
                    "keyboard return " + key);
            }
            view.Open();
            Pointer(view, 400, 520, EventType.MouseDown);
            view.Close();
            view.Open();
            Check(!view.IsReturnPressed && !view.IsReturnHovered, "reopen resets capture and hover");
            view.Dispose();
            view.Dispose();
            Throws<ObjectDisposedException>(() => view.Open(), "open after dispose");
            Throws<ObjectDisposedException>(() => view.Draw(), "draw after dispose");
            Throws<ObjectDisposedException>(() => view.Close(), "close after dispose");
            Throws<ObjectDisposedException>(() => view.HandleKey(KeyCode.Escape), "key after dispose");
            Throws<ObjectDisposedException>(() => Pointer(view, 0, 0, EventType.MouseDown), "pointer after dispose");
            Debug.Log("HELP_EDITOR_TESTS_PASS assertions=" + assertions + " ink=" + ink + " clear=" + clear);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("HELP_EDITOR_TESTS_FAIL " + error);
            EditorApplication.Exit(1);
        }
    }
}
