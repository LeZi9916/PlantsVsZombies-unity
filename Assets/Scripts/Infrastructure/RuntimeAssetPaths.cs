#nullable enable
using System;
using System.IO;

namespace PlantsVsZombies.Infrastructure
{
    /// <summary>Resolves the original game's loose runtime resource directory.</summary>
    public static class RuntimeAssetPaths
    {
        /// <summary>Resolves the legacy PvZ subdirectory or the current StreamingAssets layout.</summary>
        /// <param name = "streamingAssetsPath">The platform's StreamingAssets directory.</param>
        /// <returns>The resource directory containing the images and animation folders.</returns>
        /// <exception cref = "ArgumentException">The supplied path is empty.</exception>
        public static string ResolveRoot(string streamingAssetsPath)
        {
            if (string.IsNullOrWhiteSpace(streamingAssetsPath))
            {
                throw new ArgumentException("A StreamingAssets path is required.", nameof(streamingAssetsPath));
            }

            var legacyRoot = Path.Combine(streamingAssetsPath, "PvZ");
            return Directory.Exists(Path.Combine(legacyRoot, "images")) ? legacyRoot : streamingAssetsPath;
        }
    }
}
