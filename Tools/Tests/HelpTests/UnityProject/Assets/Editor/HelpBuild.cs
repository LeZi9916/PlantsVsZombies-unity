using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HelpBuild
{
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/HelpSmoke.unity");
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/HelpSmoke.unity" },
            locationPathName = "SmokeBuild/HelpSmoke.exe",
            target = BuildTarget.StandaloneWindows64
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("HELP_BUILD_FAIL " + report.summary.result);
        Debug.Log("HELP_BUILD_PASS");
    }
}
