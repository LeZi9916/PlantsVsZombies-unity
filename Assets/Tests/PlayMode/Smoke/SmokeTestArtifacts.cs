#nullable enable
using System.Collections;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode.Smoke
{
    /// <summary>Writes captures into the current Test Runner case's artifact directory.</summary>
    internal static class SmokeTestArtifacts
    {
        /// <summary>Stores the real production camera target for this case.</summary>
        private static RenderTexture s_target = null!;
        /// <summary>Gets the artifact directory owned by the current test.</summary>
        internal static string DirectoryPath { get; private set; } = string.Empty;

        /// <summary>Starts a fresh artifact directory for one case.</summary>
        /// <param name="directory">The absolute path selected by the test fixture.</param>
        /// <param name="target">The production camera target owned by this test.</param>
        internal static void Begin(string directory, RenderTexture target)
        {
            Directory.CreateDirectory(directory);
            DirectoryPath = directory;
            s_target = target;
        }

        /// <summary>Waits for production presentation and camera rendering in interactive and batchmode Test Runner.</summary>
        /// <returns>Two frame boundaries, as used by the existing graphics fixtures.</returns>
        internal static IEnumerator WaitForFrame()
        {
            yield return null;
            yield return null;
        }

        /// <summary>Captures a completed frame and finishes writing its PNG before returning.</summary>
        /// <param name="path">The PNG path inside the current case's artifact directory.</param>
        internal static void CaptureFrame(string path)
        {
            var image = new Texture2D(s_target.width, s_target.height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = s_target;
                image.ReadPixels(new Rect(0f, 0f, s_target.width, s_target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
