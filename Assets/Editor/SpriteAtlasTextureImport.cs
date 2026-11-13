#nullable enable
using System;
using System.IO;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Applies exact atlas import settings before the first generated PNG import allocates or compresses a texture.</summary>
    internal sealed class SpriteAtlasTextureImport : AssetPostprocessor
    {
        /// <summary>Configures newly generated production atlas pages before their initial import.</summary>
        private void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Resources/PvZRendering/Layouts/", StringComparison.Ordinal) < 0)
            {
                return;
            }
            var directory = Path.GetFileName(Path.GetDirectoryName(assetPath));
            var maximum = int.TryParse(directory, out var size) && size > 0 && size <= SpriteGeometryUtility.MaximumPageSize
                ? size : SpriteGeometryUtility.MaximumPageSize;
            Configure((TextureImporter)assetImporter, maximum);
        }

        /// <summary>Sets lossless, nonreadable, nonrescaled imports consistently before and after generation.</summary>
        /// <param name="importer">The generated atlas page importer.</param>
        /// <param name="maximum">The layout's maximum allowed page edge.</param>
        internal static void Configure(TextureImporter importer, int maximum)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maximum;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = maximum,
                    format = TextureImporterFormat.RGBA32,
                    textureCompression = TextureImporterCompression.Uncompressed
                });
            }
        }
    }
}
