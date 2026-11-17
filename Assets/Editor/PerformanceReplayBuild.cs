#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Editor
{
    /// <summary>Builds opt-in replay Players only from explicitly isolated project copies.</summary>
    internal static class PerformanceReplayBuild
    {
        /// <summary>Builds a Windows replay Player using explicit command-line controls.</summary>
        /// <exception cref="InvalidOperationException">The caller did not select an isolated replay project or the build failed.</exception>
        /// <exception cref="ArgumentException">The output, backend or graphics API argument is invalid.</exception>
        public static void BuildFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            if (Array.IndexOf(arguments, "-pvz-replay-isolated") < 0 || !File.Exists(Path.Combine(project, ".pvz-replay-isolated")))
            {
                throw new InvalidOperationException("Replay builds require an explicit .pvz-replay-isolated marker and command-line flag in a separate project copy.");
            }
            var output = ReadArgument(arguments, "-pvz-replay-build-output")
                ?? throw new ArgumentException("The isolated replay Player output path is required.");
            var backend = ReadArgument(arguments, "-pvz-replay-backend") ?? "CoreCLR";
            var graphics = ReadArgument(arguments, "-pvz-replay-graphics") ?? "Vulkan";
            if (backend != "Mono" && backend != "CoreCLR" && backend != "IL2CPP")
            {
                throw new ArgumentException("The replay backend must be CoreCLR or IL2CPP; Mono is a legacy CoreCLR alias in Unity 7.");
            }
            if (graphics != "Vulkan" && graphics != "D3D11")
            {
                throw new ArgumentException("The replay graphics API must be Vulkan or D3D11.");
            }
            var implementation = backend == "IL2CPP" ? ScriptingImplementation.IL2CPP : ScriptingImplementation.CoreCLR;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, implementation);
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != implementation)
            {
                throw new InvalidOperationException("The isolated project did not select the requested scripting backend.");
            }
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { graphics == "Vulkan" ? GraphicsDeviceType.Vulkan : GraphicsDeviceType.Direct3D11 });
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.companyName = "PvZ Performance Replay";
            PlayerSettings.productName = "Isolated Replay";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var generated = Path.Combine(Application.dataPath, "PerformanceReplayGenerated");
            Directory.CreateDirectory(generated);
            File.WriteAllText(Path.Combine(generated, "link.xml"), "<linker><assembly fullname=\"PlantsVsZombies.Runtime\" preserve=\"all\" /></linker>");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string scenePath = "Assets/PerformanceReplayGenerated/Replay.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException("The isolated replay bootstrap scene could not be saved.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.GetFullPath(output),
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "PVZ_PERFORMANCE_REPLAY", "ENABLE_PROFILER" }
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Replay Player build failed: " + report.summary.result);
            }
            Debug.Log("Isolated replay Player built: " + report.summary.outputPath);
        }

        /// <summary>Reads one explicit build option.</summary>
        /// <param name="arguments">The process arguments.</param>
        /// <param name="name">The requested option.</param>
        /// <returns>The following value, or null when absent.</returns>
        private static string? ReadArgument(string[] arguments, string name)
        {
            var index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
        }
    }
}
