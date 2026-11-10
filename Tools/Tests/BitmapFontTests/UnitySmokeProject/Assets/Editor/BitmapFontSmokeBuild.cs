using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BitmapFontSmokeBuild
{
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Smoke Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cameraObject.tag = "MainCamera";
        new GameObject("Bitmap Font Smoke").AddComponent<BitmapFontSmoke>();
        if (!EditorSceneManager.SaveScene(scene, "Assets/BitmapFontSmoke.unity"))
            throw new Exception("Could not save the isolated smoke scene.");
        PlayerSettings.companyName = "PvZ Bitmap Font Tests";
        PlayerSettings.productName = "Bitmap Font Smoke";
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        Directory.CreateDirectory("Build");
        BuildReport result = BuildPipeline.BuildPlayer(new[] { "Assets/BitmapFontSmoke.unity" },
            "Build/BitmapFontSmoke.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Smoke player build failed: " + result.summary.result);
        Debug.Log("Bitmap font smoke player built successfully.");
    }
}
