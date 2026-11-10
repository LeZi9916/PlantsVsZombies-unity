using System;
using UnityEngine;

namespace PvZReplica.Audio
{
    [Serializable] public sealed class OriginalMusicVoiceState
    {
        public bool enabled, muted, nativePlaying, virtualized;
        public int timeSamples;
        public float volume;
        public string clip, loadState, mixer;
    }
    [Serializable] public sealed class OriginalMusicStemState
    {
        public bool requestedPlaying, paused;
        public float gain;
        public double startDsp, endDsp;
        public OriginalMusicVoiceState[] voices;
    }

    /// <summary>
    /// Two alternating AudioSources schedule the decoded native intro once and its tracker loop thereafter.
    /// Seeking and pausing cancel pending sources so a stale scheduled voice cannot resume behind a new cue.
    /// </summary>
    public sealed class OriginalMusicStem : IDisposable
    {
        private readonly AudioSource[] sources;
        private AudioClip clip;
        private int loopStart;
        private int active;
        private int nextStartSample;
        private double startDsp, endDsp;
        private bool queued, playing, paused;
        private int pausedSample;
        private double pausedLead, playbackOriginDsp;
        private int playbackOriginSample;
        private bool reportedPlaybackRecovery;
        public float Gain { get; set; }
        public bool IsPlaying { get { return playing; } }
        public OriginalMusicStem(GameObject owner, AudioSource first = null)
        {
            sources = new[] { first == null ? owner.AddComponent<AudioSource>() : first, owner.AddComponent<AudioSource>() };
            foreach (var source in sources)
            {
                source.Stop(); source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 0f; source.spatialize = false; source.dopplerLevel = 0f;
                source.panStereo = 0f; source.pitch = 1f; source.mute = false;
                source.priority = 0; source.outputAudioMixerGroup = null;
            }
        }
        public void Play(AudioClip audio, int originalLoopStart, int originalSampleRate, double dsp, int originalStartSample = 0)
        {
            Stop();
            if (audio == null || audio.samples <= 0 || audio.loadState != AudioDataLoadState.Loaded) return;
            clip = audio;
            loopStart = Mathf.Clamp((int)Math.Round(originalLoopStart * (double)audio.frequency / Math.Max(1, originalSampleRate)), 0, audio.samples - 1);
            active = 0; playing = true; paused = false;
            int initial = Math.Max(0, (int)Math.Round(originalStartSample * (double)audio.frequency / Math.Max(1, originalSampleRate)));
            if (initial >= audio.samples) initial = loopStart + (initial - audio.samples) % Math.Max(1, audio.samples - loopStart);
            ScheduleCurrent(initial, dsp);
        }
        private void ScheduleCurrent(int sample, double dsp)
        {
            nextStartSample = sample; startDsp = dsp;
            playbackOriginSample = sample; playbackOriginDsp = dsp;
            endDsp = dsp + (clip.samples - sample) / (double)clip.frequency;
            var voice = sources[active]; voice.clip = clip; voice.timeSamples = sample;
            voice.PlayScheduled(dsp); voice.SetScheduledEndTime(endDsp); queued = false;
        }
        public void Update(float volume, bool shouldPause)
        {
            foreach (var source in sources) source.volume = Mathf.Clamp01(volume * Gain);
            if (!playing || clip == null) return;
            if (shouldPause && !paused)
            {
                double now = AudioSettings.dspTime;
                pausedSample = SourceSampleAtDsp(now);
                pausedLead = Math.Max(0, playbackOriginDsp - now);
                foreach (var source in sources) source.Stop();
                paused = true; queued = false; return;
            }
            if (!shouldPause && paused)
            { paused = false; ScheduleCurrent(pausedSample, AudioSettings.dspTime + SchedulingLeadSeconds + pausedLead); }
            if (paused) return;
            double clock = AudioSettings.dspTime;
            // A request is not proof of native playback. Recover a voice lost to a device reset or an
            // already-missed scheduling window, preserving the original tracker phase and user volume.
            if (clock > startDsp + 0.25 && !sources[0].isPlaying && !sources[1].isPlaying)
            {
                double restart = clock + SchedulingLeadSeconds;
                int sample = SourceSampleAtDsp(restart);
                foreach (var source in sources) source.Stop();
                ScheduleCurrent(sample, restart);
                if (!reportedPlaybackRecovery)
                { reportedPlaybackRecovery = true; Debug.LogWarning("Original music recovered a scheduled voice with no native playback."); }
                return;
            }
            if (!queued && clock >= endDsp - 0.5)
            {
                var next = sources[1 - active]; next.clip = clip; next.timeSamples = loopStart;
                next.PlayScheduled(endDsp);
                next.SetScheduledEndTime(endDsp + (clip.samples - loopStart) / (double)clip.frequency);
                queued = true;
            }
            if (queued && clock >= endDsp)
            {
                active = 1 - active; startDsp = endDsp; nextStartSample = loopStart;
                endDsp += (clip.samples - loopStart) / (double)clip.frequency; queued = false;
            }
        }
        public static double SchedulingLeadSeconds
        {
            get
            {
                int length, buffers; AudioSettings.GetDSPBufferSize(out length, out buffers);
                return Math.Max(0.06, length * 2.0 / Math.Max(1, AudioSettings.outputSampleRate) + 0.01);
            }
        }
        public double ReadOutputEnergy(float[] buffer, out float peak, out int count)
        {
            if (buffer == null || buffer.Length == 0) throw new ArgumentException("A reusable PCM buffer is required.", nameof(buffer));
            peak = 0f; count = 0; double energy = 0;
            foreach (var source in sources)
            {
                if (!source.isActiveAndEnabled || !source.isPlaying) continue;
                source.GetOutputData(buffer, 0);
                foreach (float sample in buffer)
                { if (float.IsNaN(sample) || float.IsInfinity(sample)) continue; float a = Math.Abs(sample); if (a > peak) peak = a; energy += sample * (double)sample; count++; }
            }
            return energy;
        }
        public OriginalMusicStemState Describe()
        {
            var voices = new OriginalMusicVoiceState[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                voices[i] = new OriginalMusicVoiceState {
                    enabled = source.isActiveAndEnabled, muted = source.mute, nativePlaying = source.isPlaying,
                    virtualized = source.isVirtual, timeSamples = source.clip == null ? 0 : source.timeSamples,
                    volume = source.volume, clip = source.clip == null ? null : source.clip.name,
                    loadState = source.clip == null ? "none" : source.clip.loadState.ToString(),
                    mixer = source.outputAudioMixerGroup == null ? "master (direct)" : source.outputAudioMixerGroup.name
                };
            }
            return new OriginalMusicStemState { requestedPlaying = playing, paused = paused, gain = Gain, startDsp = startDsp, endDsp = endDsp, voices = voices };
        }
        private int SourceSampleAtDsp(double dsp)
        {
            if (clip == null || !playing) return 0;
            if (paused) return pausedSample;
            long sample = playbackOriginSample + (long)Math.Floor(Math.Max(0, dsp - playbackOriginDsp) * clip.frequency + 0.000001);
            if (sample >= clip.samples) sample = loopStart + (sample - clip.samples) % Math.Max(1, clip.samples - loopStart);
            return (int)Math.Max(0, Math.Min(clip.samples - 1L, sample));
        }
        public int NativeSampleAtDsp(double dsp, int nativeFrequency)
        {
            return clip == null ? 0 : (int)((long)SourceSampleAtDsp(dsp) * Math.Max(1, nativeFrequency) / Math.Max(1, clip.frequency));
        }
        public void Stop()
        { foreach (var source in sources) { source.Stop(); source.clip = null; } playing = false; paused = false; queued = false; clip = null; }
        public void Dispose()
        { Stop(); foreach (var source in sources) if (source != null) UnityEngine.Object.Destroy(source); }
    }

}
