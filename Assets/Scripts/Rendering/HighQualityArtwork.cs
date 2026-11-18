#nullable enable
using System;
using PlantsVsZombies.Infrastructure;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Provides the artwork quality selected for the lifetime of the current game session.</summary>
    internal static class HighQualityArtwork
    {
        /// <summary>The persisted option used on the next startup.</summary>
        internal const string PreferenceKey = GamePreferences.KeyPrefix + "HighQualityArtwork";

        /// <summary>The extensionless prefix of optional high-resolution resources.</summary>
        internal const string ResourceRoot = "PvZRenderingHighQuality/";

        /// <summary>The extensionless prefix of standalone companion textures.</summary>
        private const string StandaloneResourceRoot = "PvZHighQualityArtwork/";

        /// <summary>The pixel density of the supplied replacement artwork.</summary>
        internal const int PixelScale = 4;

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
            Enabled = ReadPreference();
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

            var texture = Resources.Load<Texture2D>(StandaloneResourceRoot + path);
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
