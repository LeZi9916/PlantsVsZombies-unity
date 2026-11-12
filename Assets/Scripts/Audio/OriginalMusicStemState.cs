#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines original music stem state.</summary>
    [Serializable]
    public sealed class OriginalMusicStemState
    {
        /// <summary>Stores requested playing, paused.</summary>
        public bool requestedPlaying, paused;
        /// <summary>Stores gain.</summary>
        public float gain;
        /// <summary>Stores start dsp, end dsp.</summary>
        public double startDsp, endDsp;
        /// <summary>Stores voices.</summary>
        public OriginalMusicVoiceState[] voices = System.Array.Empty<OriginalMusicVoiceState>();
    }
}
