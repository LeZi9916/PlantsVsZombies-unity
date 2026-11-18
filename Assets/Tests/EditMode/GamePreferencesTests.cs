#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Infrastructure;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Verifies volume settings remain available across the preference-key rename.</summary>
    public sealed class GamePreferencesTests
    {
        /// <summary>Checks the default, legacy fallback, and priority of a saved current volume.</summary>
        /// <param name="key">The renamed preference key for the volume.</param>
        /// <param name="legacyKey">The key that older versions wrote.</param>
        /// <param name="defaultValue">The volume for a new installation.</param>
        [TestCase(GamePreferences.MusicVolumeKey, "PvZReplica.Music", .35f)]
        [TestCase(GamePreferences.SoundVolumeKey, "PvZReplica.Sound", .7f)]
        public void VolumesPreferNewKeysAndReadLegacyValues(string key, string legacyKey, float defaultValue)
        {
            var previous = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : (float?)null;
            var legacy = PlayerPrefs.HasKey(legacyKey) ? PlayerPrefs.GetFloat(legacyKey) : (float?)null;
            try
            {
                Assert.That(key, Does.StartWith("PlantsVsZombies."));
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.DeleteKey(legacyKey);
                Assert.That(GamePreferences.ReadFloat(key, defaultValue), Is.EqualTo(defaultValue));

                PlayerPrefs.SetFloat(legacyKey, .25f);
                Assert.That(GamePreferences.ReadFloat(key, defaultValue), Is.EqualTo(.25f));

                PlayerPrefs.SetFloat(key, 0f);
                Assert.That(GamePreferences.ReadFloat(key, defaultValue), Is.Zero);
            }
            finally
            {
                RestoreVolume(key, previous);
                RestoreVolume(legacyKey, legacy);
            }
        }

        /// <summary>Restores a volume preference or removes the key if it was absent.</summary>
        /// <param name="key">The setting temporarily used by a test.</param>
        /// <param name="value">The saved volume, or null when the setting was originally absent.</param>
        private static void RestoreVolume(string key, float? value)
        {
            if (value.HasValue)
            {
                PlayerPrefs.SetFloat(key, value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
