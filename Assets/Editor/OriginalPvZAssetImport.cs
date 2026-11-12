#nullable enable
using System;

using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    // Keep source dimensions and decoded image/audio quality when Unity imports the archive.
    /// <summary>Defines original pv zasset import.</summary>
    internal sealed class OriginalPvZAssetImport : AssetPostprocessor
    {
        /// <summary>Stores root.</summary>
        private const string Root = "Assets/OriginalPvZ/";
        /// <summary>Performs on preprocess texture.</summary>
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = false;
        }

        /// <summary>Performs on preprocess audio.</summary>
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = false;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
        }
    }
}
