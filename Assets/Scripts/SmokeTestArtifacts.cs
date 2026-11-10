using System;
using System.IO;
using UnityEngine;

namespace PvZReplica
{
    // Explicit test switches only; normal game assets and profiles do not use this path.
    internal static class SmokeTestArtifacts
    {
        private static string directoryPath;

        // Invoke after WaitForEndOfFrame. The asynchronous screenshot API can publish
        // the filename before Unity closes its writer, racing a following image read.
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
            finally { if (image != null) UnityEngine.Object.Destroy(image); }
        }

        public static void Quit(int exitCode)
        {
            PvZGame controller = UnityEngine.Object.FindFirstObjectByType<PvZGame>();
            if (controller != null) controller.RequestQuit(exitCode);
            else Application.Quit(exitCode);
        }

        public static string DirectoryPath
        {
            get
            {
                if (directoryPath != null) return directoryPath;
                string[] arguments = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(arguments, "-pvz-smoke-output");
                string requested = Path.Combine(Application.dataPath, "..");
                if (index >= 0)
                {
                    if (index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]) ||
                        arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                        throw new ArgumentException("-pvz-smoke-output requires an output directory.");
                    requested = arguments[index + 1];
                }
                string resolved = Path.GetFullPath(requested);
                Directory.CreateDirectory(resolved);
                directoryPath = resolved;
                return directoryPath;
            }
        }
    }
}
