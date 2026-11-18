#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using PlantsVsZombies.Infrastructure;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Packages 4x artwork whose consumers require standalone textures.</summary>
    internal static class HighQualityArtworkBaker
    {
        /// <summary>The generated Resources directory owned by this baker.</summary>
        internal const string OutputRoot = HighQualityArtwork.ImportRoot + "PvZHighQualityArtwork";

        /// <summary>The names of startup textures supplied as standalone Resources images.</summary>
        private static readonly string[] s_loadingImages = { "title", "popcap", "logo", "grass", "dirt", "sodroll" };

        /// <summary>The original .bytes extension and high-resolution suffix on a bitmap-font companion.</summary>
        private const string FontCompanionSuffix = ".bytes" + RuntimeAssetPaths.HighQualityImageSuffix;

        /// <summary>Resolves the high-resolution PNG stored next to an original.</summary>
        /// <param name="root">The original resource directory, or null when baking originals.</param>
        /// <param name="relative">The original relative asset path.</param>
        /// <returns>The existing replacement file, if supplied.</returns>
        internal static string? FindReplacement(string? root, string relative)
        {
            if (root == null)
            {
                return null;
            }

            var path = Path.Combine(root, relative);
            if (Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                path = Path.ChangeExtension(path, null);
            }
            path += RuntimeAssetPaths.HighQualityImageSuffix;
            return File.Exists(path) ? path : null;
        }

        /// <summary>Updates startup, bitmap-font, and water-effect textures.</summary>
        /// <param name="root">The original resource directory containing the 4x companions.</param>
        /// <param name="force">Whether to regenerate even when the inputs appear current.</param>
        /// <exception cref="IOException">An input or generated output cannot be read or written.</exception>
        /// <exception cref="InvalidDataException">A pool image or mask has invalid pixels or dimensions.</exception>
        internal static void Prepare(string root, bool force)
        {
            var outputs = new HashSet<string>(StringComparer.Ordinal);
            var images = Path.Combine(root, "images");
            foreach (var name in s_loadingImages)
            {
                var source = FindReplacement(images, name + ".png");
                if (source != null)
                {
                    CopyCompanion(source, "PvZLoading/" + name, outputs, force);
                }
            }

            var fonts = Path.Combine(root, "fonts");
            if (Directory.Exists(fonts))
            {
                foreach (var path in Directory.EnumerateFiles(fonts, "*" + FontCompanionSuffix))
                {
                    var name = Path.GetFileName(path);
                    CopyCompanion(path, "PvZBitmapFonts/Atlases/" + name.Substring(0, name.Length - FontCompanionSuffix.Length), outputs, force);
                }
            }
            ComposePoolLayer(images, "pool_base.jpg", "pool_base_.png", outputs, force);
            ComposePoolLayer(images, "pool_base_night.jpg", "pool_base_.png", outputs, force);
            ComposePoolLayer(images, "pool_shading.jpg", "pool_shading_.jpg", outputs, force);
            ComposePoolLayer(images, "pool_shading_night.jpg", "pool_shading_.jpg", outputs, force);

            if (Directory.Exists(OutputRoot))
            {
                foreach (var path in Directory.EnumerateFiles(OutputRoot, "*.png", SearchOption.AllDirectories))
                {
                    var assetPath = path.Replace('\\', '/');
                    if (!outputs.Contains(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>Copies a companion PNG to the Resources identity used by its original consumer.</summary>
        /// <param name="source">The high-resolution source PNG in StreamingAssets.</param>
        /// <param name="resourcePath">The extensionless destination identity within the generated output.</param>
        /// <param name="outputs">The generated assets to retain.</param>
        /// <param name="force">Whether to recopy an unchanged file.</param>
        /// <exception cref="IOException">The source or destination cannot be accessed.</exception>
        private static void CopyCompanion(string source, string resourcePath, HashSet<string> outputs, bool force)
        {
            var destination = OutputRoot + "/" + resourcePath + ".png";
            outputs.Add(destination);
            var sourceTime = File.GetLastWriteTimeUtc(source);
            if (force || !File.Exists(destination)
                || new FileInfo(source).Length != new FileInfo(destination).Length
                || sourceTime != File.GetLastWriteTimeUtc(destination))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);
                File.SetLastWriteTimeUtc(destination, sourceTime);
            }
        }

        /// <summary>Composes matching high-resolution color and red-channel alpha for a water mesh.</summary>
        /// <param name="root">The source directory holding exported images.</param>
        /// <param name="colorName">The original color filename.</param>
        /// <param name="maskName">The original alpha filename.</param>
        /// <param name="outputs">The generated assets to retain.</param>
        /// <param name="force">Whether to regenerate unchanged inputs.</param>
        /// <exception cref="IOException">A source or destination cannot be accessed.</exception>
        /// <exception cref="InvalidDataException">The supplied pool image and mask cannot be combined.</exception>
        private static void ComposePoolLayer(string root, string colorName, string maskName, HashSet<string> outputs, bool force)
        {
            var color = FindReplacement(root, colorName);
            var mask = FindReplacement(root, maskName);
            if (color == null || mask == null)
            {
                return;
            }

            var directory = OutputRoot + "/Pool";
            Directory.CreateDirectory(directory);
            var destination = directory + "/" + Path.GetFileNameWithoutExtension(colorName) + ".png";
            outputs.Add(destination);
            var colorTime = File.GetLastWriteTimeUtc(color);
            var maskTime = File.GetLastWriteTimeUtc(mask);
            var sourceTime = colorTime > maskTime ? colorTime : maskTime;
            if (!force && File.Exists(destination) && File.GetLastWriteTimeUtc(destination) == sourceTime)
            {
                return;
            }
            var texture = TextureCache.LoadMaskedImage(color, mask);
            try
            {
                File.WriteAllBytes(destination, texture.EncodeToPNG());
                File.SetLastWriteTimeUtc(destination, sourceTime);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
