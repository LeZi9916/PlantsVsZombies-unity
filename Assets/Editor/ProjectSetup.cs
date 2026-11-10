using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PvZReplica
{
    public static class ProjectSetup
    {
        [MenuItem("PvZ Replica/Prepare Scene")]
        public static void PrepareScene()
        {
            const string scenePath = "Assets/Scenes/Lawn.unity";
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(scenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("PvZ Game").AddComponent<PvZGame>();
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new Exception("Could not save " + scenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.companyName = "PvZ Replica";
            PlayerSettings.productName = "PvZ Unity Prototype";
            PlayerSettings.defaultScreenWidth = 800;
            PlayerSettings.defaultScreenHeight = 600;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("PvZ Replica/Build Windows")]
        public static void BuildWindows()
        {
            PrepareScene();
            Directory.CreateDirectory("Build");
            BuildReport report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,
                "Build/PvZUnity.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("PvZ Windows build complete: " + report.summary.outputPath);
        }
    }
}
