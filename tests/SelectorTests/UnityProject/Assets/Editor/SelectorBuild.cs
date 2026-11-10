using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SelectorBuild
{
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Selector smoke test").AddComponent<SelectorSmoke>();
        if (!EditorSceneManager.SaveScene(scene, "Assets/SelectorSmoke.unity"))
            throw new Exception("Could not save smoke test scene.");
        PlayerSettings.companyName = "Selector Tests";
        PlayerSettings.productName = "Selector Smoke";
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        BuildReport report = BuildPipeline.BuildPlayer(new[] { "Assets/SelectorSmoke.unity" },
            "Build/SelectorSmoke.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Selector smoke build failed: " + report.summary.result);
        Debug.Log("SELECTOR_BUILD_PASS");
    }
}
