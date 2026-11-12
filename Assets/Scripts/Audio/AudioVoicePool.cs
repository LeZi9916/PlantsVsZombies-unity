#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Audio
{
    /// <summary>Reuses a fixed set of two-dimensional sound voices with optional per-group limits.</summary>
    public sealed class AudioVoicePool : IDisposable
    {
        /// <summary>Provides mixer routing and listener settings for newly started sounds.</summary>
        private readonly AudioSource _template;
        /// <summary>Owns the voices created before gameplay starts.</summary>
        private readonly AudioSource[] _voices;
        /// <summary>Identifies the sound category currently occupying each voice.</summary>
        private readonly int[] _groups;
        /// <summary>Reserves voices until their expected end, including sounds requested while paused.</summary>
        private readonly double[] _endTimes;
        /// <summary>Selects a clock that continues while listener-independent sounds play.</summary>
        private readonly bool[] _ignoreListenerPause;
        /// <summary>Orders active leases independently of sample length, pitch, and audio clock selection.</summary>
        private readonly long[] _startVersions;
        /// <summary>Assigns monotonically increasing identities to newly started voices.</summary>
        private long _nextStartVersion;
        /// <summary>Prevents reuse after the owned native components have been released.</summary>
        private bool _disposed;

        /// <summary>Gets the maximum number of simultaneous voices without allocating during playback.</summary>
        public int Capacity => _voices.Length;

        /// <summary>Creates all reusable voices on the owner's existing object.</summary>
        /// <param name="owner">The object whose lifetime contains all pooled voices.</param>
        /// <param name="template">The source supplying mixer routing and listener settings.</param>
        /// <param name="capacity">The fixed maximum number of simultaneous sounds.</param>
        /// <exception cref="ArgumentNullException">The owner or template is missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public AudioVoicePool(GameObject owner, AudioSource template, int capacity)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _template = template;
            _voices = new AudioSource[capacity];
            _groups = new int[capacity];
            _endTimes = new double[capacity];
            _ignoreListenerPause = new bool[capacity];
            _startVersions = new long[capacity];
            for (var i = 0; i < capacity; i++)
            {
                var voice = owner.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                voice.spatialize = false;
                voice.dopplerLevel = 0f;
                _voices[i] = voice;
            }
        }

        /// <summary>Starts a sound on an idle voice, optionally replacing the oldest active voice in the same category.</summary>
        /// <param name="clip">The loaded sound to play.</param>
        /// <param name="volume">The gain applied to this sound.</param>
        /// <param name="pitch">The positive playback rate, clamped to Unity's supported range.</param>
        /// <param name="group">The category used to enforce a native sound-instance limit.</param>
        /// <param name="groupLimit">The category limit, or zero to use only the total pool capacity.</param>
        /// <param name="replaceOldest">Whether a saturated category can reuse its oldest voice without interrupting another category.</param>
        /// <returns>Whether the sound acquired a voice and started playback.</returns>
        public bool TryPlay(AudioClip clip, float volume, float pitch = 1f, int group = 0, int groupLimit = 0, bool replaceOldest = false)
        {
            if (_disposed || _template == null || clip == null || clip.samples <= 0 || clip.frequency <= 0)
            {
                return false;
            }

            var dsp = AudioSettings.dspTime;
            var realtime = Time.realtimeSinceStartupAsDouble;
            var available = FindAvailableVoice(group, groupLimit, dsp, realtime, replaceOldest);
            if (available < 0)
            {
                return false;
            }

            var source = _voices[available];
            source.Stop();
            source.outputAudioMixerGroup = _template.outputAudioMixerGroup;
            source.ignoreListenerPause = _template.ignoreListenerPause;
            source.priority = _template.priority;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, .01f, 3f);
            source.clip = clip;
            _groups[available] = group;
            _startVersions[available] = ++_nextStartVersion;
            _ignoreListenerPause[available] = source.ignoreListenerPause;
            // DSP time freezes with the listener, so paused requests retain their leases.
            var start = _ignoreListenerPause[available] ? realtime : dsp;
            _endTimes[available] = start + clip.samples / ((double)clip.frequency * source.pitch);
            source.Play();
            return true;
        }

        /// <summary>Checks admission before a caller consumes random sample or pitch choices.</summary>
        /// <param name="group">The category used to enforce a native sound-instance limit.</param>
        /// <param name="groupLimit">The category limit, or zero to use only the total pool capacity.</param>
        /// <param name="replaceOldest">Whether the oldest same-category voice may be replaced when admission is otherwise blocked.</param>
        /// <returns>Whether an immediately following sound request can acquire a voice.</returns>
        public bool CanPlay(int group = 0, int groupLimit = 0, bool replaceOldest = false)
        {
            return !_disposed && _template != null
                && FindAvailableVoice(group, groupLimit, AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble, replaceOldest) >= 0;
        }

        /// <summary>Reclaims completed voices and finds a slot respecting both global and category limits.</summary>
        /// <param name="group">The category of the proposed sound.</param>
        /// <param name="groupLimit">The category limit, or zero for no additional category limit.</param>
        /// <param name="dsp">The current listener-sensitive audio clock.</param>
        /// <param name="realtime">The current listener-independent clock.</param>
        /// <param name="replaceOldest">Whether to reuse the oldest matching category when no idle slot can be admitted.</param>
        /// <returns>The admitted idle or replacement slot index, or minus one when the request must be discarded.</returns>
        private int FindAvailableVoice(int group, int groupLimit, double dsp, double realtime, bool replaceOldest)
        {
            var available = -1;
            var groupCount = 0;
            var oldest = -1;
            for (var i = 0; i < _voices.Length; i++)
            {
                var voice = _voices[i];
                if (voice == null)
                {
                    continue;
                }

                var clock = _ignoreListenerPause[i] ? realtime : dsp;
                if (_endTimes[i] > 0d && (voice.isPlaying || _endTimes[i] > clock))
                {
                    if (_groups[i] == group)
                    {
                        groupCount++;
                        if (oldest < 0 || _startVersions[i] < _startVersions[oldest])
                        {
                            oldest = i;
                        }
                    }

                    continue;
                }

                if (_endTimes[i] > 0d)
                {
                    voice.clip = null;
                    _endTimes[i] = 0d;
                }

                if (available < 0)
                {
                    available = i;
                }
            }

            if (available < 0 || (groupLimit > 0 && groupCount >= groupLimit))
            {
                return replaceOldest ? oldest : -1;
            }

            return available;
        }

        /// <summary>Changes the volume of a category without touching unrelated sound voices.</summary>
        /// <param name="volume">The gain applied to matching voices.</param>
        /// <param name="group">The category whose existing sounds should change volume.</param>
        public void SetVolume(float volume, int group)
        {
            if (_disposed)
            {
                return;
            }

            var gain = Mathf.Clamp01(volume);
            for (var i = 0; i < _voices.Length; i++)
            {
                var source = _voices[i];
                if (_endTimes[i] > 0d && _groups[i] == group && source != null && source.volume != gain)
                {
                    source.volume = gain;
                }
            }
        }

        /// <summary>Stops all sounds and immediately makes every retained voice available again.</summary>
        public void StopAll()
        {
            if (_disposed)
            {
                return;
            }

            for (var i = 0; i < _voices.Length; i++)
            {
                var source = _voices[i];
                if (_endTimes[i] > 0d && source != null)
                {
                    source.Stop();
                    source.clip = null;
                }

                _endTimes[i] = 0d;
            }
        }

        /// <summary>Stops playback and destroys only the components owned by this pool.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopAll();
            _disposed = true;
            foreach (var source in _voices)
            {
                if (source != null)
                {
                    UnityEngine.Object.Destroy(source);
                }
            }
        }
    }
}
