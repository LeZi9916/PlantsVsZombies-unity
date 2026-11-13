#nullable enable
using System.IO;
using PlantsVsZombies.Infrastructure;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Ensures ordinary Play Mode and every Player build use current generated rendering assets.</summary>
    [InitializeOnLoad]
    internal sealed class SpriteAtlasBuildPreparation : IPreprocessBuildWithReport
    {
        /// <summary>Gets the early preparation order used before the build gathers referenced resources.</summary>
        public int callbackOrder => -1000;

        /// <summary>Registers generation before entering Play Mode without changing scenes or player settings.</summary>
        static SpriteAtlasBuildPreparation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>Updates generated rendering assets before a Player build for any target starts.</summary>
        /// <param name="report">The build report supplied by Unity.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            SpriteAtlasBaker.EnsureCurrent();
        }

        /// <summary>Generates production resources before Play Mode while allowing isolated synthetic test projects.</summary>
        /// <param name="state">The Editor's current Play Mode transition.</param>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }
            var root = RuntimeAssetPaths.ResolveRoot(Application.streamingAssetsPath);
            if (!Directory.Exists(Path.Combine(root, "reanim")) || !Directory.Exists(Path.Combine(root, "images")))
            {
                return;
            }
            try
            {
                SpriteAtlasBaker.EnsureCurrent();
            }
            catch
            {
                EditorApplication.isPlaying = false;
                throw;
            }
        }
    }
}
