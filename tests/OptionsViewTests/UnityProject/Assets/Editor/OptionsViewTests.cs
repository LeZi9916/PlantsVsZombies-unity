using System;
using System.IO;
using PvZReplica.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OptionsViewTests
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
        Check(view.HandlePointer(new Vector2(x, y), type), "open view consumes " + type);
    }

    private static void Click(OptionsView view, float x, float y)
    {
        Mouse(view, x, y, EventType.MouseDown);
        Mouse(view, x, y, EventType.MouseUp);
    }

    private static void OpenGame(OptionsView view)
    {
        view.GameplayButtonsEnabled = true;
        view.Open(.25f, .75f, false);
    }

    public static void RunAndBuild()
    {
        string root = Environment.GetEnvironmentVariable("PVZ_OPTIONS_RESOURCE_ROOT");
        Check(Directory.Exists(Path.Combine(root, "images")), "resource root");
        bool missing = false;
        try { new OptionsView(Path.Combine(root, "absent")); }
        catch (DirectoryNotFoundException) { missing = true; }
        Check(missing, "missing assets throw");
        bool screenWasFull = Screen.fullScreen;
        using (var view = new OptionsView(root))
        {
            OpenGame(view);
            Check(view.CloseResult == OptionsCloseResult.None && view.IsOpen, "open game");
            Mouse(view, 419f, 177f, EventType.MouseDown);
            Mouse(view, 800f, 0f, EventType.MouseDrag);
            Near(1f, view.MusicVolume, "knob drag clamps outside");
            Mouse(view, 800f, 0f, EventType.MouseUp);

            Click(view, 390f, 200f);
            Near(3f / 135f, view.SoundVolume, "sound wins overlapping slider area");
            Near(1f, view.MusicVolume, "overlap does not change music");
            float previous = view.SoundVolume;
            Click(view, 480f, 230f);
            Near(previous, view.SoundVolume, "acceleration box blocks underlying sound slider");
            Click(view, 480f, 295f);
            Check(view.FullScreen && view.IsOpen, "fullscreen wins overlap with almanac");

            Mouse(view, 300f, 460f, EventType.MouseDown);
            Mouse(view, 10f, 10f, EventType.MouseUp);
            Check(view.IsOpen, "release outside cancels return");
            Mouse(view, 300f, 333f, EventType.MouseDown);
            Mouse(view, 300f, 336f, EventType.MouseUp);
            Check(view.IsOpen && view.CloseResult == OptionsCloseResult.None, "release over overlapping top button cancels press");
            Click(view, 300f, 336f);
            Check(view.IsOpen && view.CloseResult == OptionsCloseResult.None, "restart requires confirmation");
            Click(view, 450f, 380f);
            Check(view.IsOpen && view.CloseResult == OptionsCloseResult.None, "restart cancel retains options");
            Click(view, 300f, 350f);
            view.Close();
            Check(view.IsOpen, "escape cancels confirmation first");
            Click(view, 300f, 350f);
            Mouse(view, 250f, 380f, EventType.MouseDown);
            Mouse(view, 20f, 20f, EventType.MouseUp);
            Check(view.IsOpen, "confirmation release outside cancels press");
            Click(view, 250f, 380f);
            Check(!view.IsOpen && view.CloseResult == OptionsCloseResult.RestartLevel, "restart only after confirm");

            OpenGame(view);
            Click(view, 300f, 336f);
            Click(view, 450f, 380f);
            Click(view, 300f, 380f);
            Check(!view.IsOpen && view.CloseResult == OptionsCloseResult.MainMenu,
                "main menu wins overlapping button area");
            OpenGame(view);
            Click(view, 300f, 310f);
            Check(!view.IsOpen && view.CloseResult == OptionsCloseResult.OpenAlmanac, "almanac dispatch");

            view.GameplayButtonsEnabled = false;
            view.Open(.2f, .8f, false);
            Click(view, 300f, 400f);
            Check(view.IsOpen && view.CloseResult == OptionsCloseResult.None, "selector hides game actions");
            Click(view, 512f, 320f);
            Check(view.FullScreen, "selector fullscreen rectangle is shifted down 20");
            Click(view, 512f, 270f);
            Check(view.FullScreen, "selector acceleration rectangle is shifted down 15");
            Click(view, 390f, 210f);
            Near(3f / 135f, view.SoundVolume, "selector sound wins slider overlap");
            Near(.2f, view.MusicVolume, "selector overlap leaves music unchanged");
            Click(view, 300f, 460f);
            Check(!view.IsOpen && view.CloseResult == OptionsCloseResult.ReturnToGame, "selector confirmation closes");
            Check(!view.HandlePointer(Vector2.zero, EventType.MouseDown), "closed view ignores mouse");
            Check(Screen.fullScreen == screenWasFull, "view does not apply screen mode");
            view.Open(-1f, 2f, false);
            Near(0f, view.MusicVolume, "music clamped on open");
            Near(1f, view.SoundVolume, "sound clamped on open");
            view.Close();
            bool rejected = false;
            try { view.Open(float.NaN, 0f, false); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected && !view.IsOpen, "invalid input rejected atomically");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.14f, .19f, .14f);
        camera.gameObject.tag = "MainCamera";
        new GameObject("Options smoke").AddComponent<OptionsViewSmoke>();
        if (!EditorSceneManager.SaveScene(scene, "Assets/OptionsViewSmoke.unity")) throw new Exception("Save scene failed");
        PlayerSettings.companyName = "PvZ Options Tests";
        PlayerSettings.productName = "Options View Smoke";
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        Directory.CreateDirectory("Build");
        BuildReport report = BuildPipeline.BuildPlayer(new[] { "Assets/OptionsViewSmoke.unity" },
            "Build/OptionsViewSmoke.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
        Debug.Log("OPTIONS_VIEW_EDITOR_PASS assertions=" + assertions);
    }
}
