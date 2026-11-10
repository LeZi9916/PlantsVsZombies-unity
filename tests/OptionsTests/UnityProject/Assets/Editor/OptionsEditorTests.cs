using System;
using System.IO;
using System.Reflection;
using PvZReplica.UI;
using UnityEditor;
using UnityEngine;

public static class OptionsEditorTests
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void Near(float expected, float actual, string message)
    {
        Check(Math.Abs(expected - actual) < 0.0001f, message + ": " + actual);
    }

    private static void Mouse(OptionsView view, float x, float y, EventType type)
    {
        Check(view.HandlePointer(new Vector2(x, y), type), "modal pointer must be consumed");
    }

    public static void Run()
    {
        try
        {
            string root = Environment.GetEnvironmentVariable("PVZ_OPTIONS_RESOURCE_ROOT");
            Check(Directory.Exists(Path.Combine(root, "images")), "raw asset directory");
            bool missing = false;
            try { new OptionsView(Path.Combine(root, "not-present")); }
            catch (DirectoryNotFoundException) { missing = true; }
            Check(missing, "missing assets must throw");
            bool screenWasFull = Screen.fullScreen;
            using (OptionsView view = new OptionsView(root))
            {
                Texture2D background = (Texture2D)typeof(OptionsView).GetField("background",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                Check(background.width == 423 && background.height == 498, "background dimensions");
                Check(background.GetPixel(0, 0).a < 0.01f, "background mask makes corner transparent");
                Check(background.GetPixel(211, 250).a > 0.99f, "background mask keeps center opaque");
                view.Open(0.25f, 0.75f, false);
                Check(view.IsOpen && view.CloseResult == OptionsCloseResult.None, "opening session");
                Mouse(view, 387f + 113f * .25f + 4f, 177f, EventType.MouseDown);
                Mouse(view, 800f, 10f, EventType.MouseDrag);
                Near(1f, view.MusicVolume, "drag music to maximum outside widget");
                Mouse(view, 800f, 10f, EventType.MouseUp);
                Mouse(view, 387f, 215f, EventType.MouseDown);
                Near(0f, view.SoundVolume, "sound track click");
                Mouse(view, 522f, 215f, EventType.MouseDrag);
                Near(0f, view.SoundVolume, "track click does not start drag");
                Mouse(view, 522f, 215f, EventType.MouseUp);
                Mouse(view, 475f, 260f, EventType.MouseDown);
                Check(view.FullScreen, "fullscreen toggles on mouse down");
                Mouse(view, 10f, 10f, EventType.MouseUp);
                Check(view.FullScreen, "release outside does not undo checkbox");
                Mouse(view, 475f, 230f, EventType.MouseDown);
                Mouse(view, 475f, 230f, EventType.MouseUp);
                Mouse(view, 320f, 300f, EventType.MouseDown);
                Mouse(view, 320f, 300f, EventType.MouseUp);
                Check(view.IsOpen && view.FullScreen, "disabled controls do nothing");
                Mouse(view, 300f, 460f, EventType.MouseDown);
                Mouse(view, 20f, 20f, EventType.MouseUp);
                Check(view.IsOpen, "return release outside cancels");
                Mouse(view, 300f, 460f, EventType.MouseDown);
                Mouse(view, 300f, 460f, EventType.MouseUp);
                Check(!view.IsOpen && view.CloseResult == OptionsCloseResult.ReturnToGame, "return result");
                Check(!view.HandlePointer(Vector2.zero, EventType.MouseDown), "closed view ignores pointer");
                Check(Screen.fullScreen == screenWasFull, "view did not apply screen mode");
                view.Open(-1f, 2f, false);
                Near(0f, view.MusicVolume, "open clamps music");
                Near(1f, view.SoundVolume, "open clamps sound");
                Check(view.CloseResult == OptionsCloseResult.None, "reopen clears result");
                view.Close();
                Check(view.CloseResult == OptionsCloseResult.ReturnToGame, "programmatic close result");
                bool bad = false;
                try { view.Open(float.NaN, 0f, false); }
                catch (ArgumentOutOfRangeException) { bad = true; }
                Check(bad && !view.IsOpen, "non-finite values rejected atomically");
            }
            Debug.Log("OPTIONS_TESTS_PASS assertions=" + assertions);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("OPTIONS_TESTS_FAIL: " + error);
            EditorApplication.Exit(1);
        }
    }
}
