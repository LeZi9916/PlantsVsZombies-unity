#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Infrastructure
{
    /// <summary>Reads current settings with a fallback to values saved by older releases.</summary>
    internal static class GamePreferences
    {
        /// <summary>The prefix used for new settings.</summary>
        internal const string KeyPrefix = "PlantsVsZombies.";

        /// <summary>The preference key for music volume.</summary>
        internal const string MusicVolumeKey = KeyPrefix + "Music";

        /// <summary>The preference key for sound-effect volume.</summary>
        internal const string SoundVolumeKey = KeyPrefix + "Sound";

        /// <summary>The prefix written by older releases.</summary>
        private const string LegacyKeyPrefix = "PvZReplica.";

        /// <summary>Reads a floating-point preference, using the legacy value if needed.</summary>
        /// <param name="key">The key prefixed by <see cref="KeyPrefix"/>.</param>
        /// <param name="defaultValue">The value to use when neither key exists.</param>
        /// <returns>The saved value or the supplied default.</returns>
        internal static float ReadFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(ResolveReadKey(key), defaultValue);
        }

        /// <summary>Reads an integer preference, using the legacy value if needed.</summary>
        /// <param name="key">The key prefixed by <see cref="KeyPrefix"/>.</param>
        /// <param name="defaultValue">The value to use when neither key exists.</param>
        /// <returns>The saved value or the supplied default.</returns>
        internal static int ReadInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(ResolveReadKey(key), defaultValue);
        }

        /// <summary>Chooses a saved current key in preference to its legacy counterpart.</summary>
        /// <param name="key">The key prefixed by <see cref="KeyPrefix"/>.</param>
        /// <returns>The current or legacy key to read.</returns>
        private static string ResolveReadKey(string key)
        {
            return PlayerPrefs.HasKey(key) ? key : LegacyKeyPrefix + key.Substring(KeyPrefix.Length);
        }
    }
}
