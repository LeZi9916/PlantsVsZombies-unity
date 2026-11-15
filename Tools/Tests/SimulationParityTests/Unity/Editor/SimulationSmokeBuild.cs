#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlantsVsZombies.Tests.Editor
{
    /// <summary>Builds the real Lawn scene only inside an explicitly selected isolated verification project.</summary>
    public static class SimulationSmokeBuild
    {
        /// <summary>Builds a macOS Mono smoke Player at the explicitly supplied artifact path.</summary>
        /// <exception cref="InvalidOperationException">The project or output path is not an isolated verification artifact.</exception>
        public static void Build()
        {
            var arguments = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(arguments, "-pvz-simulation-player");
            if (index < 0 || index + 1 >= arguments.Length
                || !Application.dataPath.Contains(".unity-runs/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Select an isolated .unity-runs project and an explicit smoke Player output.");
            }
            var output = Path.GetFullPath(arguments[index + 1]);
            if (!output.Contains(".unity-runs/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The smoke Player must remain in ignored verification artifacts.");
            }
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 800;
            PlayerSettings.defaultScreenHeight = 600;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Lawn.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("The isolated smoke Player build failed: " + report.summary.result);
            }
            Debug.Log("SIMULATION_SMOKE_BUILD_PASS: " + output);
        }
    }
}
