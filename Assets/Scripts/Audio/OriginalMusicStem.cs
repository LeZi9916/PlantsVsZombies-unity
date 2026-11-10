#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Audio
{
    /// <summary>
    /// Two alternating AudioSources schedule the decoded native intro once and its tracker loop thereafter.
    /// Seeking and pausing cancel pending sources so a stale scheduled voice cannot resume behind a new cue.
    /// </summary>
    public sealed class OriginalMusicStem : IDisposable
    {
        /// <summary>Stores sources.</summary>
        private readonly AudioSource[] _sources;
        /// <summary>Stores clip.</summary>
        private AudioClip _clip;
        /// <summary>Stores loop start.</summary>
        private int _loopStart;
        /// <summary>Stores active.</summary>
        private int _active;
        /// <summary>Stores next start sample.</summary>
        private int _nextStartSample;
        /// <summary>Stores start dsp,  end dsp.</summary>
        private double _startDsp, _endDsp;
        /// <summary>Stores queued,  playing,  paused.</summary>
        private bool _queued, _playing, _paused;
        /// <summary>Stores paused sample.</summary>
        private int _pausedSample;
        /// <summary>Stores paused lead,  playback origin dsp.</summary>
        private double _pausedLead, _playbackOriginDsp;
        /// <summary>Stores playback origin sample.</summary>
        private int _playbackOriginSample;
        /// <summary>Stores reported playback recovery.</summary>
        private bool _reportedPlaybackRecovery;
        /// <summary>Gets or sets gain.</summary>
        public float Gain { get; set; }

        /// <summary>Gets or sets is playing.</summary>
        public bool IsPlaying
        {
            get
            {
                return _playing;
            }
        }

        /// <summary>Initializes original music stem.</summary>
        /// <param name = "owner">The owner.</param>
        /// <param name = "first">The first.</param>
        public OriginalMusicStem(GameObject owner, AudioSource first = null)
        {
            _sources = new[]
            {
                first == null ? owner.AddComponent<AudioSource>() : first,
                owner.AddComponent<AudioSource>()
            };
            foreach (var source in _sources)
            {
                source.Stop();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.spatialize = false;
                source.dopplerLevel = 0f;
                source.panStereo = 0f;
                source.pitch = 1f;
                source.mute = false;
                source.priority = 0;
                source.outputAudioMixerGroup = null;
            }
        }

        /// <summary>Performs play.</summary>
        /// <param name = "audio">The audio.</param>
        /// <param name = "originalLoopStart">The original loop start.</param>
        /// <param name = "originalSampleRate">The original sample rate.</param>
        /// <param name = "dsp">The dsp.</param>
        /// <param name = "originalStartSample">The original start sample.</param>
        public void Play(AudioClip audio, int originalLoopStart, int originalSampleRate, double dsp, int originalStartSample = 0)
        {
            Stop();
            if (audio == null || audio.samples <= 0 || audio.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            _clip = audio;
            _loopStart = Mathf.Clamp((int)Math.Round(originalLoopStart * (double)audio.frequency / Math.Max(1, originalSampleRate)), 0, audio.samples - 1);
            _active = 0;
            _playing = true;
            _paused = false;
            var initial = Math.Max(0, (int)Math.Round(originalStartSample * (double)audio.frequency / Math.Max(1, originalSampleRate)));
            if (initial >= audio.samples)
            {
                initial = _loopStart + (initial - audio.samples) % Math.Max(1, audio.samples - _loopStart);
            }

            ScheduleCurrent(initial, dsp);
        }

        /// <summary>Performs schedule current.</summary>
        /// <param name = "sample">The sample.</param>
        /// <param name = "dsp">The dsp.</param>
        private void ScheduleCurrent(int sample, double dsp)
        {
            _nextStartSample = sample;
            _startDsp = dsp;
            _playbackOriginSample = sample;
            _playbackOriginDsp = dsp;
            _endDsp = dsp + (_clip.samples - sample) / (double)_clip.frequency;
            var voice = _sources[_active];
            voice.clip = _clip;
            voice.timeSamples = sample;
            voice.PlayScheduled(dsp);
            voice.SetScheduledEndTime(_endDsp);
            _queued = false;
        }

        /// <summary>Performs update.</summary>
        /// <param name = "volume">The volume.</param>
        /// <param name = "shouldPause">The should pause.</param>
        public void Update(float volume, bool shouldPause)
        {
            foreach (var source in _sources)
            {
                source.volume = Mathf.Clamp01(volume * Gain);
            }

            if (!_playing || _clip == null)
            {
                return;
            }

            if (shouldPause && !_paused)
            {
                var now = AudioSettings.dspTime;
                _pausedSample = SourceSampleAtDsp(now);
                _pausedLead = Math.Max(0, _playbackOriginDsp - now);
                foreach (var source in _sources)
                {
                    source.Stop();
                }

                _paused = true;
                _queued = false;
                return;
            }

            if (!shouldPause && _paused)
            {
                _paused = false;
                ScheduleCurrent(_pausedSample, AudioSettings.dspTime + SchedulingLeadSeconds + _pausedLead);
            }

            if (_paused)
            {
                return;
            }

            var clock = AudioSettings.dspTime;
            // A request is not proof of native playback. Recover a voice lost to a device reset or an
            // already-missed scheduling window, preserving the original tracker phase and user volume.
            if (clock > _startDsp + 0.25 && !_sources[0].isPlaying && !_sources[1].isPlaying)
            {
                var restart = clock + SchedulingLeadSeconds;
                var sample = SourceSampleAtDsp(restart);
                foreach (var source in _sources)
                {
                    source.Stop();
                }

                ScheduleCurrent(sample, restart);
                if (!_reportedPlaybackRecovery)
                {
                    _reportedPlaybackRecovery = true;
                    Debug.LogWarning("Original music recovered a scheduled voice with no native playback.");
                }

                return;
            }

            if (!_queued && clock >= _endDsp - 0.5)
            {
                var next = _sources[1 - _active];
                next.clip = _clip;
                next.timeSamples = _loopStart;
                next.PlayScheduled(_endDsp);
                next.SetScheduledEndTime(_endDsp + (_clip.samples - _loopStart) / (double)_clip.frequency);
                _queued = true;
            }

            if (_queued && clock >= _endDsp)
            {
                _active = 1 - _active;
                _startDsp = _endDsp;
                _nextStartSample = _loopStart;
                _endDsp += (_clip.samples - _loopStart) / (double)_clip.frequency;
                _queued = false;
            }
        }

        /// <summary>Gets or sets scheduling lead seconds.</summary>
        public static double SchedulingLeadSeconds
        {
            get
            {
                int length, buffers;
                AudioSettings.GetDSPBufferSize(out length, out buffers);
                return Math.Max(0.06, length * 2.0 / Math.Max(1, AudioSettings.outputSampleRate) + 0.01);
            }
        }

        /// <summary>Performs read output energy.</summary>
        /// <param name = "buffer">The buffer.</param>
        /// <param name = "peak">The peak.</param>
        /// <param name = "count">The count.</param>
        /// <returns>The resulting read output energy.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public double ReadOutputEnergy(float[] buffer, out float peak, out int count)
        {
            if (buffer == null || buffer.Length == 0)
            {
                throw new ArgumentException("A reusable PCM buffer is required.", nameof(buffer));
            }

            peak = 0f;
            count = 0;
            double energy = 0;
            foreach (var source in _sources)
            {
                if (!source.isActiveAndEnabled || !source.isPlaying)
                {
                    continue;
                }

                source.GetOutputData(buffer, 0);
                foreach (float sample in buffer)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        continue;
                    }

                    var a = Math.Abs(sample);
                    if (a > peak)
                    {
                        peak = a;
                    }

                    energy += sample * (double)sample;
                    count++;
                }
            }

            return energy;
        }

        /// <summary>Performs describe.</summary>
        /// <returns>The resulting describe.</returns>
        public OriginalMusicStemState Describe()
        {
            var voices = new OriginalMusicVoiceState[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                voices[i] = new OriginalMusicVoiceState
                {
                    enabled = source.isActiveAndEnabled,
                    muted = source.mute,
                    nativePlaying = source.isPlaying,
                    virtualized = source.isVirtual,
                    timeSamples = source.clip == null ? 0 : source.timeSamples,
                    volume = source.volume,
                    clip = source.clip == null ? null : source.clip.name,
                    loadState = source.clip == null ? "none" : source.clip.loadState.ToString(),
                    mixer = source.outputAudioMixerGroup == null ? "master (direct)" : source.outputAudioMixerGroup.name
                };
            }

            return new OriginalMusicStemState
            {
                requestedPlaying = _playing,
                paused = _paused,
                gain = Gain,
                startDsp = _startDsp,
                endDsp = _endDsp,
                voices = voices
            };
        }

        /// <summary>Performs source sample at dsp.</summary>
        /// <param name = "dsp">The dsp.</param>
        /// <returns>The resulting source sample at dsp.</returns>
        private int SourceSampleAtDsp(double dsp)
        {
            if (_clip == null || !_playing)
            {
                return 0;
            }

            if (_paused)
            {
                return _pausedSample;
            }

            var sample = _playbackOriginSample + (long)Math.Floor(Math.Max(0, dsp - _playbackOriginDsp) * _clip.frequency + 0.000001);
            if (sample >= _clip.samples)
            {
                sample = _loopStart + (sample - _clip.samples) % Math.Max(1, _clip.samples - _loopStart);
            }

            return (int)Math.Max(0, Math.Min(_clip.samples - 1L, sample));
        }

        /// <summary>Performs native sample at dsp.</summary>
        /// <param name = "dsp">The dsp.</param>
        /// <param name = "nativeFrequency">The native frequency.</param>
        /// <returns>The resulting native sample at dsp.</returns>
        public int NativeSampleAtDsp(double dsp, int nativeFrequency)
        {
            return _clip == null ? 0 : (int)((long)SourceSampleAtDsp(dsp) * Math.Max(1, nativeFrequency) / Math.Max(1, _clip.frequency));
        }

        /// <summary>Performs stop.</summary>
        public void Stop()
        {
            foreach (var source in _sources)
            {
                source.Stop();
                source.clip = null;
            }

            _playing = false;
            _paused = false;
            _queued = false;
            _clip = null;
        }

        /// <summary>Performs dispose.</summary>
        public void Dispose()
        {
            Stop();
            foreach (var source in _sources)
            {
                if (source != null)
                {
                    UnityEngine.Object.Destroy(source);
                }
            }
        }
    }
}
