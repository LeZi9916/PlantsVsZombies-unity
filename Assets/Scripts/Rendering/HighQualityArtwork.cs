#nullable enable
using System;
using System.IO;
using PlantsVsZombies.Infrastructure;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlantsVsZombies.Rendering
{
    /// <summary>Provides the artwork quality selected for the lifetime of the current game session.</summary>
    internal static class HighQualityArtwork
    {
        /// <summary>The persisted option used on the next startup.</summary>
        internal const string PreferenceKey = GamePreferences.KeyPrefix + "HighQualityArtwork";

        /// <summary>The generated editor assets used to build the optional pack.</summary>
        internal const string ImportRoot = "Assets/Generated/PvZRendering/HighQuality/";

        /// <summary>The file to install in the Player's persistent data directory.</summary>
        internal const string PackFileName = "pvz-hq.bundle";

        /// <summary>The bundle address of the base catalog fingerprint.</summary>
        internal const string CompatibilityAddress = "base-source-hash";

        /// <summary>The catalog and page address prefix within an HQ pack.</summary>
        internal const string ResourceRoot = "PvZRenderingHighQuality/";

        /// <summary>The extensionless prefix of standalone companion textures.</summary>
        private const string StandaloneResourceRoot = "PvZHighQualityArtwork/";

        /// <summary>The pixel density of the supplied replacement artwork.</summary>
        internal const int PixelScale = 4;

        /// <summary>The installed and compatible pack, kept open for lazy page loading.</summary>
        private static AssetBundle? s_pack;

        /// <summary>Whether the installed pack has been checked during this game session.</summary>
        private static bool s_checkedPack;

        /// <summary>Gets the installation path in the Player's writable data directory.</summary>
        internal static string PackPath => Path.Combine(Application.persistentDataPath, PackFileName);

        /// <summary>Gets whether this session should use the high-resolution artwork.</summary>
        internal static bool Enabled { get; private set; }

        /// <summary>Reads the saved option, which is off for new installations.</summary>
        /// <returns>Whether high-resolution artwork is requested for the next session.</returns>
        internal static bool ReadPreference()
        {
            return GamePreferences.ReadInt(PreferenceKey, 0) == 1;
        }

        /// <summary>Writes the next-session setting for the calling menu to save with its other preferences.</summary>
        /// <param name="enabled">Whether to use the 4x artwork after restarting.</param>
        internal static void WritePreference(bool enabled)
        {
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
        }

        /// <summary>Captures the option before any view or shared texture cache loads artwork.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (s_pack != null)
            {
                s_pack.Unload(true);
                s_pack = null;
            }
            s_checkedPack = false;
            Enabled = ReadPreference();
        }

        /// <summary>Loads the HQ catalog only after checking compatibility with the original artwork.</summary>
        /// <param name="baseSourceHash">The fingerprint of the Player's packaged standard catalog.</param>
        /// <returns>The optional catalog, or null to keep the standard layout.</returns>
        internal static TextAsset? LoadCatalog(string baseSourceHash)
        {
            OpenPack(baseSourceHash);
            return LoadAsset<TextAsset>(ResourceRoot + "catalog", ".json");
        }

        /// <summary>Loads a texture page lazily from the selected HQ content source.</summary>
        /// <param name="address">The page address recorded in the HQ catalog.</param>
        /// <returns>The page texture, or null when the page cannot be loaded.</returns>
        internal static Texture2D? LoadPage(string address)
        {
            return LoadAsset<Texture2D>(address, ".png");
        }

        /// <summary>Checks the local pack once and verifies it matches this Player's source assets.</summary>
        /// <param name="baseSourceHash">The fingerprint expected inside the optional pack.</param>
        private static void OpenPack(string baseSourceHash)
        {
            if (s_checkedPack || !Enabled)
            {
                return;
            }
            s_checkedPack = true;

            var path = PackPath;
            if (!File.Exists(path))
            {
#if !UNITY_EDITOR
                Debug.LogWarning($"{nameof(HighQualityArtwork)}: Using standard artwork. Install the optional pack at {path}.");
#endif
                return;
            }

            var pack = AssetBundle.LoadFromFile(path);
            if (pack != null)
            {
                var compatibility = pack.LoadAsset<TextAsset>(CompatibilityAddress);
                if (compatibility != null && string.Equals(compatibility.text, baseSourceHash, StringComparison.Ordinal)
                    && pack.Contains(ResourceRoot + "catalog"))
                {
                    s_pack = pack;
                    return;
                }
                pack.Unload(true);
            }
            Debug.LogWarning($"{nameof(HighQualityArtwork)}: The pack could not be loaded or does not match the standard artwork: {path}");
        }

        /// <summary>Loads an asset by its pack address or uses the baked imports while authoring in the Editor.</summary>
        /// <typeparam name="T">The Unity asset type to load.</typeparam>
        /// <param name="address">The extensionless address recorded in the pack.</param>
        /// <param name="extension">The file extension of the Editor import.</param>
        /// <returns>The optional asset if it exists and HQ is enabled.</returns>
        private static T? LoadAsset<T>(string address, string extension) where T : UnityEngine.Object
        {
            if (!Enabled)
            {
                return null;
            }
            if (s_pack != null)
            {
                return s_pack.LoadAsset<T>(address);
            }
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(ImportRoot + address + extension);
#else
            return null;
#endif
        }

        /// <summary>Loads an optional high-resolution companion texture.</summary>
        /// <param name="path">The extensionless identity of the original resource.</param>
        /// <param name="width">The intended original width, or zero when it is unknown.</param>
        /// <param name="height">The intended original height, or zero when it is unknown.</param>
        /// <returns>The supported replacement texture, or null when the original should be used.</returns>
        internal static Texture2D? LoadTexture(string path, int width = 0, int height = 0)
        {
            if (!Enabled || Math.Max(width, height) * PixelScale > SystemInfo.maxTextureSize)
            {
                return null;
            }

            var texture = LoadPage(StandaloneResourceRoot + path);
            if (texture == null || Math.Max(texture.width, texture.height) > SystemInfo.maxTextureSize
                || (width > 0 && texture.width != width * PixelScale)
                || (height > 0 && texture.height != height * PixelScale))
            {
                return null;
            }
            return texture;
        }
    }
}
