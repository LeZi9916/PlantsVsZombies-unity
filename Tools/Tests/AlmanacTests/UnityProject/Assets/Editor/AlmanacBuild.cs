using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlmanacBuild
{
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/AlmanacSmoke.unity");
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/AlmanacSmoke.unity" },
            locationPathName = "SmokeBuild/AlmanacSmoke.exe",
            target = BuildTarget.StandaloneWindows64
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Almanac smoke build failed: " + report.summary.result);
    }
}
