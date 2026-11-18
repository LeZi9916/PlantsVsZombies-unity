#nullable enable
using System;
using System.IO;
using PlantsVsZombies.Infrastructure;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Excludes optional source images while Unity gathers the ordinary Player's StreamingAssets.</summary>
    [InitializeOnLoad]
    public static class OriginalPlayerBuild
    {
        /// <summary>The ignored directory that survives an interrupted build or Editor restart.</summary>
        private static string StagingRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../.unity-runs/original-player-hq-staging"));

        /// <summary>Routes the Unity build window through the same exclusion used by scripted builds.</summary>
        static OriginalPlayerBuild()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildFromWindow);
            EditorApplication.delayCall += RecoverInterruptedBuild;
        }

        /// <summary>Builds a Player with original StreamingAssets, restoring HQ sources after success or failure.</summary>
        /// <param name="options">The normal Unity build options.</param>
        /// <returns>The Unity Player build report.</returns>
        /// <exception cref="IOException">An HQ asset cannot be staged or restored without overwriting a file.</exception>
        public static BuildReport Build(BuildPlayerOptions options)
        {
            return WithOriginalStreamingAssets(Application.streamingAssetsPath, StagingRoot, () => BuildPipeline.BuildPlayer(options));
        }

        /// <summary>Checks whether the supplied root contains HQ source images.</summary>
        /// <param name="root">The StreamingAssets directory to inspect.</param>
        /// <returns>Whether at least one HQ source is present.</returns>
        internal static bool HasHighQualityFiles(string root)
        {
            if (!Directory.Exists(root))
            {
                return false;
            }
            using var images = Directory.EnumerateFiles(root, "*" + RuntimeAssetPaths.HighQualityImageSuffix,
                new EnumerationOptions { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive }).GetEnumerator();
            return images.MoveNext();
        }

        /// <summary>Preserves the build window's normal build and launch behavior.</summary>
        /// <param name="options">The options selected in the build window.</param>
        private static void BuildFromWindow(BuildPlayerOptions options)
        {
            WithOriginalStreamingAssets(Application.streamingAssetsPath, StagingRoot, () =>
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                return true;
            });
        }

        /// <summary>Moves HQ source assets out of Unity's import root for the duration of one build.</summary>
        /// <typeparam name="T">The result returned by the Unity build operation.</typeparam>
        /// <param name="root">The original StreamingAssets directory.</param>
        /// <param name="staging">The directory that holds excluded source assets and their metadata.</param>
        /// <param name="build">The build operation to invoke once the HQ sources are excluded.</param>
        /// <returns>The result of the build operation.</returns>
        /// <exception cref="IOException">An HQ path cannot be staged or safely restored.</exception>
        internal static T WithOriginalStreamingAssets<T>(string root, string staging, Func<T> build)
        {
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.LockReloadAssemblies();
            try
            {
                RestoreHighQualityFiles(root, staging);
                if (Directory.Exists(root))
                {
                    var images = Directory.GetFiles(root, "*" + RuntimeAssetPaths.HighQualityImageSuffix,
                        new EnumerationOptions { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive });
                    foreach (var image in images)
                    {
                        MoveFile(image, root, staging);
                        if (File.Exists(image + ".meta"))
                        {
                            MoveFile(image + ".meta", root, staging);
                        }
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return build();
            }
            finally
            {
                try
                {
                    RestoreHighQualityFiles(root, staging);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                finally
                {
                    EditorApplication.UnlockReloadAssemblies();
                    AssetDatabase.AllowAutoRefresh();
                }
            }
        }

        /// <summary>Restores source files staged by an Editor process that exited before its cleanup could run.</summary>
        private static void RecoverInterruptedBuild()
        {
            if (BuildPipeline.isBuildingPlayer || !Directory.Exists(StagingRoot))
            {
                return;
            }
            RestoreHighQualityFiles(Application.streamingAssetsPath, StagingRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>Restores only known HQ files from staging without overwriting existing sources.</summary>
        /// <param name="root">The original StreamingAssets directory.</param>
        /// <param name="staging">The build's private staging directory.</param>
        /// <exception cref="IOException">Staging contains an unexpected file or a source already exists.</exception>
        private static void RestoreHighQualityFiles(string root, string staging)
        {
            if (!Directory.Exists(staging))
            {
                return;
            }
            var files = Directory.GetFiles(staging, "*", SearchOption.AllDirectories);
            foreach (var path in files)
            {
                var image = path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(path, null) : path;
                if (!RuntimeAssetPaths.IsHighQualityImage(image))
                {
                    throw new IOException("Unexpected file in HQ build staging: " + path);
                }
                MoveFile(path, staging, root);
            }
            Directory.Delete(staging, true);
        }

        /// <summary>Moves one file between parallel directory trees, preserving its original relative path.</summary>
        /// <param name="path">The source file to move.</param>
        /// <param name="from">The source directory tree's root.</param>
        /// <param name="to">The destination directory tree's root.</param>
        /// <exception cref="IOException">A destination already exists or a source cannot be moved.</exception>
        private static void MoveFile(string path, string from, string to)
        {
            var destination = Path.Combine(to, Path.GetRelativePath(from, path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(path, destination);
        }
    }
}
