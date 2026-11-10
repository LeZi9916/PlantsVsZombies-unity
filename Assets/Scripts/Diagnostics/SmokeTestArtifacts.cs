#nullable enable
using PlantsVsZombies.Game;
using System;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Diagnostics
{
    // Explicit test switches only; normal game assets and profiles do not use this path.
    /// <summary>Defines smoke test artifacts.</summary>
    internal static class SmokeTestArtifacts
    {
        /// <summary>Stores directory path.</summary>
        private static string s_directoryPath;
        // Invoke after WaitForEndOfFrame. The asynchronous screenshot API can publish
        // the filename before Unity closes its writer, racing a following image read.
        /// <summary>Performs capture frame.</summary>
        /// <param name = "path">The path.</param>
        public static void CaptureFrame(string path)
        {
            Texture2D image = null;
            try
            {
                image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            catch (Exception error)
            {
                Debug.LogError("PVZ_CAPTURE_FAIL: " + error);
                Quit(1);
            }
            finally
            {
                if (image != null)
                {
                    UnityEngine.Object.Destroy(image);
                }
            }
        }

        /// <summary>Performs quit.</summary>
        /// <param name = "exitCode">The exit code.</param>
        public static void Quit(int exitCode)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<PvZGame>();
            if (controller != null)
            {
                controller.RequestQuit(exitCode);
            }
            else
            {
                Application.Quit(exitCode);
            }
        }

        /// <summary>Gets or sets directory path.</summary>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public static string DirectoryPath
        {
            get
            {
                if (s_directoryPath != null)
                {
                    return s_directoryPath;
                }

                var arguments = Environment.GetCommandLineArgs();
                var index = Array.IndexOf(arguments, "-pvz-smoke-output");
                var requested = Path.Combine(Application.dataPath, "..");
                if (index >= 0)
                {
                    if (index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]) || arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new ArgumentException("-pvz-smoke-output requires an output directory.");
                    }

                    requested = arguments[index + 1];
                }

                var resolved = Path.GetFullPath(requested);
                Directory.CreateDirectory(resolved);
                s_directoryPath = resolved;
                return s_directoryPath;
            }
        }
    }
}
