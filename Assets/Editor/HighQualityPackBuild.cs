#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Exports the imported HQ atlas and companion textures as one independently installable AssetBundle.</summary>
    public static class HighQualityPackBuild
    {
        /// <summary>Builds an optional pack for the target selected in the Editor.</summary>
        [MenuItem("PlantsVsZombies/Build High Quality Asset Pack")]
        public static void BuildFromMenu()
        {
            var output = EditorUtility.OpenFolderPanel("Save High Quality Asset Pack", "", "");
            if (!string.IsNullOrEmpty(output))
            {
                Build(output, EditorUserBuildSettings.activeBuildTarget);
            }
        }

        /// <summary>Builds the pack to the directory specified by -pvz-hq-pack-output for the active build target.</summary>
        /// <exception cref="ArgumentException">The output argument is absent.</exception>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-pvz-hq-pack-output");
            if (index < 0 || index + 1 >= args.Length)
            {
                throw new ArgumentException("Supply -pvz-hq-pack-output followed by the pack's output directory.");
            }
            Build(args[index + 1], EditorUserBuildSettings.activeBuildTarget);
        }

        /// <summary>Builds an HQ pack and records the fingerprint of its matching standard catalog.</summary>
        /// <param name="output">The standalone bundle output directory outside Unity's asset tree.</param>
        /// <param name="target">The target of the Player that will load the pack.</param>
        /// <exception cref="BuildFailedException">HQ sources are missing, the destination is inside Assets, or bundling fails.</exception>
        internal static void Build(string output, BuildTarget target)
        {
            output = Path.GetFullPath(output);
            var relative = Path.GetRelativePath(Application.dataPath, output);
            if (relative == "." || (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                throw new BuildFailedException("Choose a pack output directory outside Assets.");
            }
            if (!OriginalPlayerBuild.HasHighQualityFiles(Application.streamingAssetsPath))
            {
                throw new BuildFailedException("No HQ source images were found in StreamingAssets.");
            }

            SpriteAtlasBaker.EnsureCurrent();
            var catalog = JsonUtility.FromJson<SpriteAtlasCatalog>(File.ReadAllText(SpriteAtlasBaker.OutputRoot + "/catalog.json"));
            var compatibility = HighQualityArtwork.ImportRoot + HighQualityArtwork.CompatibilityAddress + ".txt";
            File.WriteAllText(compatibility, catalog.SourceHash);
            AssetDatabase.ImportAsset(compatibility, ImportAssetOptions.ForceSynchronousImport);

            var files = new List<string>();
            var addresses = new List<string>();
            foreach (var file in Directory.GetFiles(HighQualityArtwork.ImportRoot, "*", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                var extension = Path.GetExtension(path);
                if (extension != ".png" && extension != ".json" && path != compatibility)
                {
                    continue;
                }

                files.Add(path);
                addresses.Add(path.Substring(HighQualityArtwork.ImportRoot.Length, path.Length - HighQualityArtwork.ImportRoot.Length - extension.Length));
            }

            Directory.CreateDirectory(output);
            var bundle = new AssetBundleBuild
            {
                assetBundleName = HighQualityArtwork.PackFileName,
                assetNames = files.ToArray(),
                addressableNames = addresses.ToArray()
            };
            var result = BuildPipeline.BuildAssetBundles(output, new[] { bundle },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            var destination = Path.Combine(output, HighQualityArtwork.PackFileName);
            if (result == null || !File.Exists(destination))
            {
                throw new BuildFailedException("The high-quality AssetBundle could not be built.");
            }
            Debug.Log($"{nameof(HighQualityPackBuild)}: Built {target} pack at {destination}.");
        }
    }
}
