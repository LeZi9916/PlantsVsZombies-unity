#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Waves;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;

namespace PlantsVsZombies.Audio
{
    /// <summary>Loads the original tracker stems and plays the music cue selected from the current game state.</summary>
    internal sealed class DynamicMusicPlayer
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose view and simulation select the music cue.</param>
        internal DynamicMusicPlayer(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Adopts the looping music source and applies the persisted volume.</summary>
        /// <param name="source">The source that plays the first music stem.</param>
        /// <param name="volume">The persisted music volume in the range zero to one.</param>
        internal void Initialize(AudioSource source, float volume)
        {
            _musicSource = source;
            source.loop = true;
            SetVolume(volume);
        }

        /// <summary>Applies a new music volume to the music source.</summary>
        /// <param name="volume">The music volume in the range zero to one.</param>
        internal void SetVolume(float volume)
        {
            _musicVolume = volume;
            RequiredMusicSource.volume = volume;
        }

        /// <summary>Stores music source.</summary>
        internal AudioSource? _musicSource;

        /// <summary>Gets the music source, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal AudioSource RequiredMusicSource => _musicSource ?? throw new System.InvalidOperationException("The member '_musicSource' is not available.");
        /// <summary>Stores music volume.</summary>
        internal float _musicVolume;

        /// <summary>Performs try resolve adventure reward music.</summary>
        /// <param name = "cue">The cue.</param>
        /// <returns>The resulting try resolve adventure reward music.</returns>
        private bool TryResolveAdventureRewardMusic(out MusicCue cue)
        {
            cue = MusicCue.None;
            if (_owner._currentView != PvZGame.ScreenView.Board || _owner._game == null || !_owner._game.IsAdventure || _owner.IndependentModes.IsModeBoard)
            {
                return false;
            }

            var reward = _owner._game.Adventure.RewardPresentation;
            if (reward == null || reward.Stage != AdventureAwardStage.Introduction)
            {
                return false;
            }

            var paper = reward.NativeType == 15 || reward.NativeType == 18 && _owner._game.Adventure.Level.Number == 50;
            // PC AwardScreen ctor 0x40FB7A..97 starts tune8 at order221; paper remains silent.
            cue = paper ? MusicCue.None : MusicCue.ZenGarden;
            return true;
        }

        // Modes and garden pages may set this on entry, then clear it on return to the selector.
        /// <summary>Stores dynamic music override.</summary>
        public MusicCue? DynamicMusicOverride;
        /// <summary>Stores dynamic music clips.</summary>
        private readonly Dictionary<string, AudioClip> _dynamicMusicClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        /// <summary>Stores dynamic music info.</summary>
        private readonly Dictionary<string, NativeMusicClip> _dynamicMusicInfo = new Dictionary<string, NativeMusicClip>(StringComparer.Ordinal);
        /// <summary>Stores dynamic music stems.</summary>
        private OriginalMusicStem[]? _dynamicMusicStems;

        /// <summary>Gets the dynamic music stems, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private OriginalMusicStem[] RequiredDynamicMusicStems => _dynamicMusicStems ?? throw new System.InvalidOperationException("The member '_dynamicMusicStems' is not available.");
        /// <summary>Stores dynamic burst.</summary>
        private readonly OriginalMusicBurst _dynamicBurst = new OriginalMusicBurst();
        /// <summary>Stores playing music cue.</summary>
        private MusicCue _playingMusicCue = MusicCue.None;
        /// <summary>Stores subscribed audio game.</summary>
        private GameSimulation? _subscribedAudioGame;
        /// <summary>Stores dynamic audio ready.</summary>
        internal bool _dynamicAudioReady;
        /// <summary>Stores dynamic stem info.</summary>
        private readonly NativeMusicClip[] _dynamicStemInfo = new NativeMusicClip[3];
        /// <summary>Stores dynamic music next tick dsp.</summary>
        private double _dynamicMusicNextTickDsp = double.PositiveInfinity;
        /// <summary>Stores dynamic music clock paused.</summary>
        private bool _dynamicMusicClockPaused;
        /// <summary>Stores dynamic music paused at.</summary>
        private double _dynamicMusicPausedAt;
        /// <summary>Stores dynamic audio tasks,  dynamic audio completed.</summary>
        private int _dynamicAudioTasks, _dynamicAudioCompleted;
        /// <summary>Stores owned music listener.</summary>
        private AudioListener? _ownedMusicListener;
        /// <summary>Stores next audio listener check.</summary>
        private float _nextAudioListenerCheck;
        /// <summary>Stores dynamic audio failures.</summary>
        private int _dynamicAudioFailures;
        /// <summary>Stores music output probe running.</summary>
        private bool _musicOutputProbeRunning;
        /// <summary>Gets or sets dynamic audio ready.</summary>
        public bool DynamicAudioReady
        {
            get
            {
                return _dynamicAudioReady;
            }
        }

        /// <summary>Gets or sets dynamic audio progress.</summary>
        internal float DynamicAudioProgress
        {
            get
            {
                return _dynamicAudioReady ? 1f : _dynamicAudioCompleted / (float)Math.Max(1, _dynamicAudioTasks);
            }
        }

        /// <summary>Performs initialize dynamic audio.</summary>
        internal void InitializeDynamicAudio()
        {
            if (_dynamicMusicStems != null)
            {
                return;
            }

            EnsureOriginalAudioListener();
            _dynamicMusicStems = new[]
            {
                new OriginalMusicStem(_owner.gameObject, _musicSource),
                new OriginalMusicStem(_owner.gameObject),
                new OriginalMusicStem(_owner.gameObject)
            };
            _owner.StartCoroutine(LoadDynamicOriginalAudio());
            StartMusicProbeFromArguments();
        }

        /// <summary>Performs load dynamic original audio.</summary>
        /// <returns>The resulting load dynamic original audio.</returns>
        private IEnumerator LoadDynamicOriginalAudio()
        {
            var json = Resources.Load<TextAsset>("PvZMusic/native_music");
            var manifest = json == null ? null : JsonUtility.FromJson<NativeMusicManifest>(json.text);
            string[] cues =
            {
                "hugewave",
                "finalwave",
                "siren",
                "awooga",
                "loadingbar_flower",
                "loadingbar_zombie",
                "buttonclick",
                "reverse_explosion",
                "cherrybomb",
                "juicy",
                "dirt_rise",
                "swing",
                "bonk",
                "buzzer",
                "seedlift",
                "shovel",
                "plant",
                "coin",
                "diamond",
                "points",
                "hatchback_open",
                "crazydaveshort1", "crazydaveshort2", "crazydaveshort3",
                "crazydavelong1", "crazydavelong2", "crazydavelong3",
                "crazydaveextralong1", "crazydaveextralong2", "crazydaveextralong3",
                "crazydavecrazy", "crazydavescream", "crazydavescream2", "bungee_scream"
            };
            _dynamicAudioTasks = (manifest == null || manifest.clips == null ? 0 : manifest.clips.Length) + cues.Length;
            if (manifest != null && manifest.clips != null)
            {
                foreach (var entry in manifest.clips)
                {
                    var request = Resources.LoadAsync<AudioClip>(entry.resource);
                    yield return request;
                    var clip = request.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        if (clip.loadState == AudioDataLoadState.Loaded && clip.samples > 0)
                        {
                            _dynamicMusicClips[entry.key] = clip;
                            _dynamicMusicInfo[entry.key] = entry;
                        }
                        else
                        {
                            _dynamicAudioFailures++;
                            Debug.LogWarning("Original tracker failed to decode: " + entry.resource + " (" + clip.loadState + ")");
                        }
                    }
                    else
                    {
                        _dynamicAudioFailures++;
                        Debug.LogWarning("Original tracker stem is missing: " + entry.resource);
                    }

                    _dynamicAudioCompleted++;
                }
            }
            else
            {
                _dynamicAudioFailures++;
                Debug.LogWarning("Original music manifest is missing. Run Audio/Tools/prepare_original_music.py before building.");
            }

            foreach (string cue in cues)
            {
                var diamond = cue == "diamond";
                var path = Path.Combine(_owner._root, "sounds", cue + (diamond ? ".wav" : ".ogg"));
#if UNITY_EDITOR
                if (!File.Exists(path) && (cue.StartsWith("crazydave", StringComparison.Ordinal) || cue == "bungee_scream" || cue == "hatchback_open" || cue == "swing" || cue == "bonk"))
                {
                    path = Path.Combine(Application.dataPath, "OriginalPvZ", "Raw", "sounds", cue + ".ogg");
                }
#endif
                if (File.Exists(path))
                {
                    using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, diamond ? AudioType.WAV : AudioType.OGGVORBIS))
                    {
                        yield return request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            _owner.Sounds._presentationSounds[cue] = DownloadHandlerAudioClip.GetContent(request);
                        }
                    }
                }

                if (!_owner.Sounds._presentationSounds.ContainsKey(cue))
                {
                    // The shipped StreamingAssets set is intentionally small. Wave cues must also travel
                    // as original Resources audio, otherwise an event can fire successfully into silence.
                    var cherry = cue == "reverse_explosion" || cue == "cherrybomb" || cue == "juicy";
                    var bundled = Resources.LoadAsync<AudioClip>((cherry ? "PvZCherryBombSounds/" : cue == "dirt_rise" ? "PvZMineSounds/" : "PvZWaveNotices/") + cue);
                    yield return bundled;
                    var clip = bundled.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        if (clip.loadState == AudioDataLoadState.Loaded)
                        {
                            _owner.Sounds._presentationSounds[cue] = clip;
                            _owner.Sounds._presentationResourceClips.Add(clip);
                        }
                    }
                }

                _dynamicAudioCompleted++;
            }

            _dynamicAudioReady = true;
            // A context may start while only its base clip is loaded. Restart that same original cue once
            // the complete stem set is resident so the drums/hihats are not permanently left unassigned.
            if (_playingMusicCue != MusicCue.None)
            {
                PlayOriginalMusicCue(_playingMusicCue);
            }
        }

        /// <summary>Performs update dynamic audio.</summary>
        internal void UpdateDynamicAudio()
        {
            if (_dynamicMusicStems == null)
            {
                return;
            }

            _owner.Sounds.UpdateAdventureCherryBombAudio();
            _owner.Sounds.UpdateAdventurePotatoMineAudio();
            if (Time.unscaledTime >= _nextAudioListenerCheck)
            {
                EnsureOriginalAudioListener();
                _nextAudioListenerCheck = Time.unscaledTime + 2f;
            }

            if (_subscribedAudioGame != _owner._game)
            {
                if (_subscribedAudioGame != null)
                {
                    _subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
                }

                _subscribedAudioGame = _owner._game;
                if (_owner._game != null)
                {
                    _owner._game.WaveScheduleEventRaised += HandleScheduledWaveAudio;
                }
            }

            var desired = ResolveOriginalMusicCue();
            if (desired != _playingMusicCue || desired != MusicCue.None && !_dynamicMusicStems[0].IsPlaying)
            {
                PlayOriginalMusicCue(desired);
            }

            var paused = _owner._optionsView != null && _owner._optionsView.IsOpen || _owner._currentView == PvZGame.ScreenView.Board && _owner._game != null && _owner._game.State == MatchState.Paused;
            var now = AudioSettings.dspTime;
            var volume = _owner._optionsView != null && _owner._optionsView.IsOpen ? _owner._optionsView.MusicVolume : _musicVolume;
            if (paused)
            {
                if (!_dynamicMusicClockPaused)
                {
                    _dynamicMusicClockPaused = true;
                    _dynamicMusicPausedAt = now;
                }

                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Update(volume, true);
                }

                return;
            }

            if (_dynamicMusicClockPaused)
            {
                _dynamicMusicNextTickDsp += Math.Max(0, now - _dynamicMusicPausedAt) + OriginalMusicStem.SchedulingLeadSeconds;
                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Update(volume, false);
                }

                _dynamicMusicClockPaused = false;
            }

            var living = 0;
            if (_owner._game != null)
            {
                foreach (Zombie z in _owner._game.Zombies)
                {
                    if (z.Health > 0f && !z.Headless && !z.Hypnotized && z.Kind != ZombieKind.Bungee)
                    {
                        living++;
                    }
                }
            }

            if (IsTerrainCue(desired) && _dynamicStemInfo[0] != null && _dynamicMusicStems[0].IsPlaying)
            {
                while (_dynamicMusicNextTickDsp <= now + 0.00000001)
                {
                    var info = _dynamicStemInfo[0];
                    var sample = _dynamicMusicStems[0].NativeSampleAtDsp(_dynamicMusicNextTickDsp, info.sampleRate);
                    _dynamicBurst.Tick(living, desired == MusicCue.Night, info.PositionAtSample(sample));
                    if (_dynamicBurst.RequestedNightOrder != -1)
                    {
                        // Apply the native order jump with its original time origin. The seek includes the
                        // elapsed part of the row, so a main-thread frame cannot restart a late drum attack.
                        var playbackDsp = now + OriginalMusicStem.SchedulingLeadSeconds;
                        var key = _dynamicBurst.RequestedNightOrder == 76 ? "night_burst" : "night_burst77";
                        NativeMusicClip jump;
                        if (_dynamicMusicInfo.TryGetValue(key, out jump))
                        {
                            var phase = (int)Math.Floor(Math.Max(0, playbackDsp - _dynamicMusicNextTickDsp) * jump.sampleRate);
                            PlayOriginalStem(1, key, playbackDsp, phase);
                        }
                    }

                    _dynamicMusicNextTickDsp += 0.01;
                }
            }

            var bossGrabFade = _owner.Opening._bossDaveGrabbed && _owner.Dave._crazyDaveAnimation == "anim_grab" && _owner.Opening.AdventureIntroActive;
            _dynamicMusicStems[0].Gain = bossGrabFade ? Mathf.Clamp01(1f - _owner.Dave._crazyDaveAnimationAge / .5f) : desired == MusicCue.Night ? _dynamicBurst.NightBase : 1f;
            _dynamicMusicStems[1].Gain = _dynamicBurst.Drums;
            _dynamicMusicStems[2].Gain = _dynamicBurst.HiHats;
            foreach (var stem in _dynamicMusicStems)
            {
                stem.Update(volume, false);
            }
        }

        /// <summary>Performs resolve original music cue.</summary>
        /// <returns>The resulting resolve original music cue.</returns>
        private MusicCue ResolveOriginalMusicCue()
        {
            if (_owner.Loading.OriginalLoadingActive)
            {
                return MusicCue.Title;
            }

            // PC CutScene::StartLevelIntro and AnimateBoard select Dave's theme, silence,
            // and finally boss music during the final-boss opening.
            if (_owner.Opening.AdventureIntroActive && _owner._currentView == PvZGame.ScreenView.Board && _owner.RequiredGame.Adventure.Level.Mode == AdventureMode.Boss)
            {
                if (_owner.Opening.RequiredAdventureIntro.NativeTime >= _owner.Opening.RequiredAdventureIntro.Plan.BankOnStart)
                {
                    return MusicCue.Boss;
                }

                if (_owner.Opening.RequiredAdventureIntro.Plan.RequiredContext.HasCrazyDaveDialog && (!_owner.Opening._bossDaveGrabbed || _owner.Dave._crazyDaveAnimation == "anim_grab" && _owner.Dave._crazyDaveAnimationAge < .5f))
                {
                    return MusicCue.Title;
                }

                return MusicCue.None;
            }

            // PC CutScene::StartLevelIntro 0x446F64 selects tune 6 (ChooseYourSeeds).
            if (_owner.Opening.AdventureIntroActive && _owner._currentView == PvZGame.ScreenView.Board)
            {
                return MusicCue.ChooseSeeds;
            }

            if (_owner._currentView == PvZGame.ScreenView.Board && _owner._game != null)
            {
                MusicCue awardCue;
                if (TryResolveAdventureRewardMusic(out awardCue))
                {
                    return awardCue;
                }

                if (_owner._game.State == MatchState.Won || _owner._game.State == MatchState.Lost)
                {
                    return MusicCue.None;
                }

                if (_owner._choosingSeeds || _owner._game.IsAdventure && _owner._game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
                {
                    return MusicCue.ChooseSeeds;
                }
            }

            if (DynamicMusicOverride.HasValue)
            {
                return DynamicMusicOverride.Value;
            }

            if (_owner._currentView != PvZGame.ScreenView.Board || _owner._game == null)
            {
                return MusicCue.Title;
            }

            if (!_owner._game.IsAdventure)
            {
                return MusicCue.Day;
            }

            var level = _owner._game.Adventure.Level;
            if (level.Mode == AdventureMode.Boss)
            {
                return MusicCue.Boss;
            }

            if (level.Mode == AdventureMode.Storm)
            {
                return MusicCue.None;
            }

            if (level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Bungee)
            {
                return MusicCue.Minigame;
            }

            if (level.Mode == AdventureMode.Vasebreaker)
            {
                return MusicCue.Puzzle;
            }

            if (level.Number == 10 || level.Number == 20 || level.Number == 30)
            {
                return MusicCue.Conveyor;
            }

            if (level.Terrain == AdventureTerrain.Fog)
            {
                return MusicCue.Fog;
            }

            if (level.IsNight)
            {
                return MusicCue.Night;
            }

            if (level.HasPool)
            {
                return MusicCue.Pool;
            }

            if (level.HasRoof)
            {
                return MusicCue.Roof;
            }

            return MusicCue.Day;
        }

        /// <summary>Performs is terrain cue.</summary>
        /// <param name = "cue">The cue.</param>
        /// <returns>The resulting is terrain cue.</returns>
        private static bool IsTerrainCue(MusicCue cue)
        {
            return cue == MusicCue.Day || cue == MusicCue.Night || cue == MusicCue.Pool || cue == MusicCue.Fog || cue == MusicCue.Roof;
        }

        /// <summary>Performs play original music cue.</summary>
        /// <param name = "cue">The cue.</param>
        private void PlayOriginalMusicCue(MusicCue cue)
        {
            foreach (var stem in RequiredDynamicMusicStems)
            {
                stem.Stop();
            }

            _playingMusicCue = cue;
            _dynamicBurst.Reset();
            _dynamicMusicClockPaused = false;
            _dynamicMusicNextTickDsp = double.PositiveInfinity;
            Array.Clear(_dynamicStemInfo, 0, _dynamicStemInfo.Length);
            string key;
            switch (cue)
            {
                case MusicCue.Title:
                    key = "title";
                    break;
                case MusicCue.ChooseSeeds:
                    key = "choose";
                    break;
                case MusicCue.Day:
                    key = "day_base";
                    break;
                case MusicCue.Night:
                    key = "night_base";
                    break;
                case MusicCue.Pool:
                    key = "pool_base";
                    break;
                case MusicCue.Fog:
                    key = "fog_base";
                    break;
                case MusicCue.Roof:
                    key = "roof_base";
                    break;
                case MusicCue.Boss:
                    key = "boss";
                    break;
                case MusicCue.Minigame:
                    key = "minigame";
                    break;
                case MusicCue.Puzzle:
                    key = "puzzle";
                    break;
                case MusicCue.Conveyor:
                    key = "conveyor";
                    break;
                case MusicCue.ZenGarden:
                    key = "garden";
                    break;
                default:
                    return;
            }

            var start = AudioSettings.dspTime + OriginalMusicStem.SchedulingLeadSeconds;
            _dynamicMusicNextTickDsp = start;
            RequiredDynamicMusicStems[0].Gain = 1f;
            RequiredDynamicMusicStems[1].Gain = RequiredDynamicMusicStems[2].Gain = 0f;
            PlayOriginalStem(0, key, start);
            if (IsTerrainCue(cue))
            {
                var prefix = key.Substring(0, key.Length - 5);
                PlayOriginalStem(1, cue == MusicCue.Night ? "night_standby" : prefix + "_drums", start);
                if (cue != MusicCue.Night)
                {
                    PlayOriginalStem(2, prefix + "_hihats", start);
                }
            }
        }

        /// <summary>Performs play original stem.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "key">The key.</param>
        /// <param name = "dsp">The dsp.</param>
        /// <param name = "nativeStartSample">The native start sample.</param>
        private void PlayOriginalStem(int index, string key, double dsp, int nativeStartSample = 0)
        {
            AudioClip clip;
            NativeMusicClip info;
            if (_dynamicMusicClips.TryGetValue(key, out clip) && _dynamicMusicInfo.TryGetValue(key, out info))
            {
                _dynamicStemInfo[index] = info;
                RequiredDynamicMusicStems[index].Play(clip, info.loopStartSample, info.sampleRate, dsp, nativeStartSample);
            }
        }

        /// <summary>Performs handle scheduled wave audio.</summary>
        /// <param name = "e">The e.</param>
        private void HandleScheduledWaveAudio(WaveScheduleEvent e)
        {
            if (_owner._currentView != PvZGame.ScreenView.Board || _owner.Loading.OriginalLoadingActive)
            {
                return;
            }

            if (e.Kind == WaveScheduleEventKind.HugeWaveSound)
            {
                _owner.Sounds.PlayPresentationSound("hugewave");
            }
            else if (e.Kind == WaveScheduleEventKind.FinalWaveSound)
            {
                _owner.Sounds.PlayPresentationSound("finalwave");
            }
            else if (e.Kind == WaveScheduleEventKind.SirenSound)
            {
                _owner.Sounds.PlayPresentationSound("siren");
            }
            else if (e.Kind == WaveScheduleEventKind.FirstWaveApproaching)
            {
                _owner.Sounds.PlayPresentationSound("awooga");
            }
            else if (e.Kind == WaveScheduleEventKind.MusicBurst)
            {
                _dynamicBurst.ForceBurst();
            }
        }

        /// <summary>Performs ensure original audio listener.</summary>
        private void EnsureOriginalAudioListener()
        {
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != _ownedMusicListener && listener.isActiveAndEnabled)
                {
                    if (_ownedMusicListener != null)
                    {
                        _ownedMusicListener.enabled = false;
                    }

                    return;
                }
            }

            if (_ownedMusicListener == null)
            {
                // Lawn stores the game bootstrap; the presentation camera does not own an audio listener.
                // A 2D source still needs one listener to reach Unity's output mixer.
                var listenerObject = new GameObject("PvZ Audio Listener");
                listenerObject.transform.SetParent(_owner.transform, false);
                _ownedMusicListener = listenerObject.AddComponent<AudioListener>();
                Debug.Log("Original audio installed the missing listener for the presentation scene.");
            }

            _ownedMusicListener.enabled = true;
        }

        /// <summary>Performs get music output report.</summary>
        /// <returns>The resulting get music output report.</returns>
        public MusicOutputReport GetMusicOutputReport()
        {
            var listeners = 0;
            foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener.isActiveAndEnabled)
                {
                    listeners++;
                }
            }

            var states = _dynamicMusicStems == null ? new OriginalMusicStemState[0] : new OriginalMusicStemState[_dynamicMusicStems.Length];
            for (var i = 0; i < states.Length; i++)
            {
                states[i] = RequiredDynamicMusicStems[i].Describe();
            }

            return new MusicOutputReport
            {
                cue = _playingMusicCue.ToString(),
                loadingReady = _dynamicAudioReady,
                listeners = listeners,
                decodedClips = _dynamicMusicClips.Count,
                decodeFailures = _dynamicAudioFailures,
                savedMusicVolume = _musicVolume,
                effectiveMusicVolume = _owner._optionsView != null && _owner._optionsView.IsOpen ? _owner._optionsView.MusicVolume : _musicVolume,
                listenerVolume = AudioListener.volume,
                listenerPaused = AudioListener.pause,
                gamePaused = _dynamicMusicClockPaused,
                outputSampleRate = AudioSettings.outputSampleRate,
                stems = states
            };
        }

        /// <summary>Performs begin music output probe.</summary>
        /// <param name = "reportPath">The report path.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "quitWhenFinished">The quit when finished.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void BeginMusicOutputProbe(string reportPath, float seconds = 6f, bool quitWhenFinished = false)
        {
            if (string.IsNullOrEmpty(reportPath))
            {
                throw new ArgumentException("A report path is required.", nameof(reportPath));
            }

            if (_musicOutputProbeRunning)
            {
                throw new InvalidOperationException("A music output probe is already running.");
            }

            _musicOutputProbeRunning = true;
            _owner.StartCoroutine(ProbeMusicOutput(Path.GetFullPath(reportPath), Mathf.Clamp(seconds, 1f, 30f), quitWhenFinished));
        }

        /// <summary>Performs start music probe from arguments.</summary>
        private void StartMusicProbeFromArguments()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-pvz-audio-probe");
            if (index < 0)
            {
                return;
            }

            var path = index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal) ? args[index + 1] : Path.Combine(Application.persistentDataPath, "music-output.json");
            var seconds = 6f;
            var duration = Array.IndexOf(args, "-pvz-audio-probe-seconds");
            if (duration >= 0 && duration + 1 < args.Length)
            {
                float.TryParse(args[duration + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            }

            BeginMusicOutputProbe(path, seconds <= 0 ? 6f : seconds, Array.IndexOf(args, "-pvz-audio-probe-quit") >= 0);
        }

        /// <summary>Performs probe music output.</summary>
        /// <param name = "reportPath">The report path.</param>
        /// <param name = "duration">The duration.</param>
        /// <param name = "quitWhenFinished">The quit when finished.</param>
        /// <returns>The resulting probe music output.</returns>
        private IEnumerator ProbeMusicOutput(string reportPath, float duration, bool quitWhenFinished)
        {
            var deadline = Time.realtimeSinceStartup + 45f;
            while (!_dynamicAudioReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            // Allow the actual scheduled start, decoder and at least one DSP output block to finish.
            yield return new WaitForSecondsRealtime(0.4f);
            var start = Time.realtimeSinceStartup;
            var report = GetMusicOutputReport();
            report.dspStart = AudioSettings.dspTime;
            var pcm = new float[1024];
            var probeInterval = new WaitForSecondsRealtime(0.02f);
            while (Time.realtimeSinceStartup - start < duration)
            {
                var musicVoice = false;
                if (_dynamicMusicStems != null)
                {
                    foreach (var stem in _dynamicMusicStems)
                    {
                        float peak;
                        int samples;
                        report.sourceEnergy += stem.ReadOutputEnergy(pcm, out peak, out samples);
                        report.sourceSamples += samples;
                        report.sourcePeak = Math.Max(report.sourcePeak, peak);
                        if (samples > 0 && stem.Gain > 0f)
                        {
                            musicVoice = true;
                        }
                    }
                }

                AudioListener.GetOutputData(pcm, 0);
                var musicOnly = musicVoice && _owner.Sounds._audioSource != null && !_owner.Sounds._audioSource.isPlaying && !_dynamicMusicClockPaused;
                foreach (float sample in pcm)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        continue;
                    }

                    var peak = Math.Abs(sample);
                    var energy = sample * (double)sample;
                    report.listenerEnergy += energy;
                    report.listenerSamples++;
                    report.listenerPeak = Math.Max(report.listenerPeak, peak);
                    if (musicOnly)
                    {
                        report.musicOnlyEnergy += energy;
                        report.musicOnlySamples++;
                        report.musicOnlyPeak = Math.Max(report.musicOnlyPeak, peak);
                    }
                }

                report.windows++;
                if (musicOnly)
                {
                    report.musicOnlyWindows++;
                }

                yield return probeInterval;
            }

            var end = GetMusicOutputReport();
            report.cue = end.cue;
            report.loadingReady = end.loadingReady;
            report.listeners = end.listeners;
            report.decodedClips = end.decodedClips;
            report.decodeFailures = end.decodeFailures;
            report.listenerPaused = end.listenerPaused;
            report.gamePaused = end.gamePaused;
            report.savedMusicVolume = end.savedMusicVolume;
            report.effectiveMusicVolume = end.effectiveMusicVolume;
            report.listenerVolume = end.listenerVolume;
            report.outputSampleRate = end.outputSampleRate;
            report.stems = end.stems;
            report.dspEnd = AudioSettings.dspTime;
            report.seconds = Time.realtimeSinceStartup - start;
            report.outputPath = reportPath;
            report.sourcePcmNonzero = report.sourcePeak > 0.000001f;
            report.listenerPcmNonzero = report.listenerPeak > 0.000001f;
            report.musicOnlyPcmNonzero = report.musicOnlyPeak > 0.000001f;
            report.status = report.sourcePcmNonzero && report.musicOnlyPcmNonzero ? "PASS" : report.effectiveMusicVolume <= 0f || report.listenerVolume <= 0f ? "MUTED_BY_VOLUME" : report.listenerPaused || report.gamePaused ? "PAUSED" : "NO_MUSIC_OUTPUT";
            report.explanation = "PCM sampled from native AudioSource outputs and final AudioListener mix; music-only windows exclude presentation/SFX playback. User volumes were not changed. Physical OS/device output is outside this probe.";
            var parent = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Debug.Log("Original music PCM probe " + report.status + ": " + reportPath);
            _musicOutputProbeRunning = false;
            if (quitWhenFinished)
            {
                Application.Quit(report.status == "PASS" ? 0 : 2);
            }
        }

        /// <summary>Performs dispose dynamic audio.</summary>
        internal void DisposeDynamicAudio()
        {
            if (_subscribedAudioGame != null)
            {
                _subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
            }

            if (_dynamicMusicStems != null)
            {
                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Dispose();
                }
            }

            foreach (var clip in _owner.Sounds._presentationSounds.Values)
            {
                if (clip != null && !_owner.Sounds._presentationResourceClips.Contains(clip))
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }

            _owner.Sounds._presentationSounds.Clear();
            _owner.Sounds._presentationResourceClips.Clear();
            _owner.WaveNotices.DisposeAdventureWaveNotices();
            _owner.Sounds.DisposeAdventureCherryBombAudio();
            // Resources owns the imported music clips. They are not the runtime clips in LoadSoundsCore.
            _dynamicMusicClips.Clear();
            _dynamicMusicInfo.Clear();
            _dynamicMusicStems = null;
            if (_ownedMusicListener != null)
            {
                UnityEngine.Object.Destroy(_ownedMusicListener.gameObject);
                _ownedMusicListener = null;
            }
        }

        /// <summary>Performs update progression music context.</summary>
        internal void UpdateProgressionMusicContext()
        {
            _owner.Music.DynamicMusicOverride = _owner._currentView == PvZGame.ScreenView.Garden || _owner._currentView == PvZGame.ScreenView.Tree || _owner._currentView == PvZGame.ScreenView.Store && (_owner.Store._storeReturnView == PvZGame.ScreenView.Garden || _owner.Store._storeReturnView == PvZGame.ScreenView.Tree) ? (global::PlantsVsZombies.Audio.MusicCue? )global::PlantsVsZombies.Audio.MusicCue.ZenGarden : _owner.IndependentModes.IndependentModeMusicCue;
        }
    }
}
