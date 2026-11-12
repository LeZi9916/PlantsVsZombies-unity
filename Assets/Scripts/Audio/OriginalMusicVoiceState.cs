#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines original music voice state.</summary>
    [Serializable]
    public sealed class OriginalMusicVoiceState
    {
        /// <summary>Stores enabled, muted, native playing, virtualized.</summary>
        public bool enabled, muted, nativePlaying, virtualized;
        /// <summary>Stores time samples.</summary>
        public int timeSamples;
        /// <summary>Stores volume.</summary>
        public float volume;
        /// <summary>Stores clip, load state, mixer.</summary>
        public string? clip = string.Empty, loadState = string.Empty, mixer = string.Empty;
    }
}
