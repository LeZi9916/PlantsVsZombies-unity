#nullable enable
using System;
using System.IO;
using System.Text;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Applies exact atlas import settings before the first generated PNG import allocates or compresses a texture.</summary>
    internal sealed class SpriteAtlasTextureImport : AssetPostprocessor
    {
        /// <summary>The platform overrides shared by Play Mode and Player builds.</summary>
        private static readonly string[] s_platforms = { "Standalone", "Android", "iPhone", "WebGL" };

        /// <summary>Runs after general original-art importers so atlas-specific dimension and sampling settings win.</summary>
        /// <returns>The late preprocessing order reserved for generated rendering pages.</returns>
        public override int GetPostprocessOrder()
        {
            return 1000;
        }

        /// <summary>Invalidates previously imported generated page artifacts when this exact import contract changes.</summary>
        /// <returns>The atlas texture importer policy version.</returns>
        public override uint GetVersion()
        {
            return 1;
        }

        /// <summary>Configures newly generated production atlas pages before their initial import.</summary>
        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(HighQualityArtworkBaker.OutputRoot + "/", StringComparison.Ordinal))
            {
                ConfigureCompanion(importer);
                return;
            }
            var highQuality = assetPath.Contains("/Resources/PvZRenderingHighQuality/Layouts/", StringComparison.Ordinal);
            if (!highQuality && !assetPath.Contains("/Resources/PvZRendering/Layouts/", StringComparison.Ordinal))
            {
                return;
            }
            var directory = Path.GetFileName(Path.GetDirectoryName(assetPath));
            var maximum = int.TryParse(directory, out var size) && size > 0 && size <= SpriteGeometryUtility.MaximumPageSize
                ? size : SpriteGeometryUtility.MaximumPageSize;
            Configure(importer, maximum, highQuality);
        }

        /// <summary>Imports standalone 4x textures with platform compression and their original sampling policy.</summary>
        /// <param name="importer">The high-resolution PNG's importer.</param>
        private void ConfigureCompanion(TextureImporter importer)
        {
            var font = assetPath.Contains("/PvZBitmapFonts/Atlases/", StringComparison.Ordinal);
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 16384;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.filterMode = font ? FilterMode.Point : FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (font && Path.GetFileName(assetPath).StartsWith("_", StringComparison.Ordinal))
            {
                importer.swizzleR = TextureImporterSwizzle.One;
                importer.swizzleG = TextureImporterSwizzle.One;
                importer.swizzleB = TextureImporterSwizzle.One;
                importer.swizzleA = TextureImporterSwizzle.R;
            }
        }

        /// <summary>Sets nonreadable, nonrescaled atlas imports consistently before and after generation.</summary>
        /// <param name="importer">The generated atlas page importer.</param>
        /// <param name="maximum">The layout's maximum allowed page edge.</param>
        /// <param name="highQuality">Whether the page supports block compression.</param>
        internal static void Configure(TextureImporter importer, int maximum, bool highQuality)
        {
            var compression = AtlasCompression(highQuality);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureCompression = compression;
            importer.maxTextureSize = maximum;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            foreach (var platform in s_platforms)
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = maximum,
                    format = AtlasFormat(platform, highQuality),
                    textureCompression = compression
                });
            }
        }

        /// <summary>Chooses block compression for the denser 4x pages.</summary>
        /// <param name="highQuality">Whether the page is authored in compression blocks.</param>
        /// <returns>The selected texture compression policy.</returns>
        private static TextureImporterCompression AtlasCompression(bool highQuality)
        {
            return highQuality ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
        }

        /// <summary>Chooses the GPU texture format for the selected quality and build target.</summary>
        /// <param name="platform">The Unity texture-import target name.</param>
        /// <param name="highQuality">Whether the page is authored in compression blocks.</param>
        /// <returns>The exact runtime texture storage format for that combination.</returns>
        private static TextureImporterFormat AtlasFormat(string platform, bool highQuality)
        {
            if (!highQuality)
            {
                return TextureImporterFormat.RGBA32;
            }
            return platform switch
            {
                "Android" or "iPhone" => TextureImporterFormat.ASTC_4x4,
                "Standalone" => TextureImporterFormat.BC7,
                _ => TextureImporterFormat.DXT5
            };
        }

        /// <summary>Checks the metadata contract even when source hashing permits reuse of existing generated PNG bytes.</summary>
        /// <param name="importer">The generated texture's current importer, including cached or copied metadata.</param>
        /// <param name="maximum">The selected layout's maximum page edge.</param>
        /// <param name="highQuality">Whether the page supports block compression.</param>
        /// <param name="differences">Receives concise names of settings requiring repair, or an empty string.</param>
        /// <returns>True when every dimension, sampling, alpha, and platform format setting is already valid.</returns>
        internal static bool IsConfigured(TextureImporter importer, int maximum, bool highQuality, out string differences)
        {
            var compression = AtlasCompression(highQuality);
            StringBuilder? invalid = null;
            Require(importer.textureType == TextureImporterType.Default, "textureType", ref invalid);
            Require(importer.textureShape == TextureImporterShape.Texture2D, "textureShape", ref invalid);
            Require(importer.textureCompression == compression, "compression", ref invalid);
            Require(importer.maxTextureSize == maximum, "maxTextureSize", ref invalid);
            Require(importer.npotScale == TextureImporterNPOTScale.None, "npotScale", ref invalid);
            Require(!importer.mipmapEnabled, "mipmaps", ref invalid);
            Require(!importer.isReadable, "readWrite", ref invalid);
            Require(importer.alphaSource == TextureImporterAlphaSource.FromInput, "alphaSource", ref invalid);
            Require(!importer.alphaIsTransparency, "alphaIsTransparency", ref invalid);
            Require(importer.sRGBTexture, "sRGB", ref invalid);
            Require(importer.filterMode == FilterMode.Bilinear, "filterMode", ref invalid);
            Require(importer.wrapMode == TextureWrapMode.Clamp, "wrapMode", ref invalid);
            foreach (var name in s_platforms)
            {
                var platform = importer.GetPlatformTextureSettings(name);
                Require(platform.overridden && platform.maxTextureSize == maximum && platform.format == AtlasFormat(name, highQuality)
                    && platform.textureCompression == compression, name, ref invalid);
            }
            differences = invalid?.ToString() ?? string.Empty;
            return invalid == null;
        }

        /// <summary>Records only failing metadata settings, allocating no diagnostic builder for a healthy page.</summary>
        /// <param name="matches">Whether the setting satisfies the generated page contract.</param>
        /// <param name="setting">The concise setting name used to locate a repair.</param>
        /// <param name="invalid">The lazily created diagnostic accumulator.</param>
        private static void Require(bool matches, string setting, ref StringBuilder? invalid)
        {
            if (matches)
            {
                return;
            }
            invalid ??= new StringBuilder();
            if (invalid.Length > 0)
            {
                invalid.Append(',');
            }
            invalid.Append(setting);
        }
    }
}
