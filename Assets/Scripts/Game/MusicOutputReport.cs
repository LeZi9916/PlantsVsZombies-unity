#nullable enable

using PlantsVsZombies.Audio;

using System;

namespace PlantsVsZombies.Game
{
    /// <summary>Defines music output report.</summary>
    [Serializable]
    public sealed class MusicOutputReport
    {
        /// <summary>Stores status, cue, output path, explanation.</summary>
        public string status = string.Empty, cue = string.Empty, outputPath = string.Empty, explanation = string.Empty;
        /// <summary>Stores loading ready, listener paused, game paused, source pcm nonzero, listener pcm nonzero, music only pcm nonzero.</summary>
        public bool loadingReady, listenerPaused, gamePaused, sourcePcmNonzero, listenerPcmNonzero, musicOnlyPcmNonzero;
        /// <summary>Stores listeners, decoded clips, decode failures, output sample rate, windows, music only windows.</summary>
        public int listeners, decodedClips, decodeFailures, outputSampleRate, windows, musicOnlyWindows;
        /// <summary>Stores saved music volume, effective music volume, listener volume, seconds.</summary>
        public float savedMusicVolume, effectiveMusicVolume, listenerVolume, seconds;
        /// <summary>Stores dsp start, dsp end, source energy, listener energy, music only energy.</summary>
        public double dspStart, dspEnd, sourceEnergy, listenerEnergy, musicOnlyEnergy;
        /// <summary>Stores source samples, listener samples, music only samples.</summary>
        public long sourceSamples, listenerSamples, musicOnlySamples;
        /// <summary>Stores source peak, listener peak, music only peak.</summary>
        public float sourcePeak, listenerPeak, musicOnlyPeak;
        /// <summary>Stores stems.</summary>
        public OriginalMusicStemState[] stems = System.Array.Empty<OriginalMusicStemState>();
    }
}
