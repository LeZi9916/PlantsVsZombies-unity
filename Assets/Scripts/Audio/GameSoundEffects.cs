#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Audio
{
    /// <summary>Loads and plays the one-shot sound effects, pooled pitched voices and presentation cues of one game session.</summary>
    internal sealed class GameSoundEffects
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose simulation and assets the effects follow.</param>
        internal GameSoundEffects(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Adopts the one-shot source, applies the persisted volume and creates the pooled pitched voices.</summary>
        /// <param name="source">The source that plays unpooled effects; also the routing template for pooled voices.</param>
        /// <param name="volume">The persisted effects volume in the range zero to one.</param>
        internal void Initialize(AudioSource source, float volume)
        {
            _audioSource = source;
            _soundVolume = volume;
            source.volume = volume;
            _soundVoices = new AudioVoicePool(_owner.gameObject, source, 64);
        }

        /// <summary>Applies a new effects volume to the one-shot source.</summary>
        /// <param name="volume">The effects volume in the range zero to one.</param>
        internal void SetVolume(float volume)
        {
            _soundVolume = volume;
            RequiredAudioSource.volume = volume;
            if (_jackInTheBoxMusic != null)
            {
                _jackInTheBoxMusic.volume = volume;
            }

            if (_diggerTunnelingSound != null)
            {
                _diggerTunnelingSound.volume = volume;
            }

            if (_stormRainSound != null)
            {
                _stormRainSound.volume = volume;
            }
        }

        /// <summary>Destroys the loaded legacy effect clips.</summary>
        internal void ReleaseClips()
        {
            foreach (var clip in _sounds.Values)
            {
                if (clip != null)
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }
        }

        /// <summary>Stores sounds.</summary>
        internal readonly Dictionary<GameEvent, AudioClip> _sounds = new Dictionary<GameEvent, AudioClip>();
        /// <summary>Stores audio source.</summary>
        internal AudioSource? _audioSource;

        /// <summary>Gets the audio source, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal AudioSource RequiredAudioSource => _audioSource ?? throw new System.InvalidOperationException("The member '_audioSource' is not available.");
        /// <summary>Stores sound volume.</summary>
        internal float _soundVolume;
        /// <summary>Stores audio loading finished.</summary>
        internal bool _audioLoadingFinished;

        /// <summary>Performs load sounds.</summary>
        /// <returns>The resulting load sounds.</returns>
        internal IEnumerator LoadSounds()
        {
            var loadCombatFoley = !_zombiePartAudioLoading;
            _zombiePartAudioLoading = true;
            try
            {
                yield return LoadSoundsCore();
                if (loadCombatFoley)
                {
                    // LoadingSounds includes Balloon/Pogo Foley in the original resource group.
                    // Do not admit the first normal wave while its entry recordings are still unavailable.
                    yield return LoadAdventureZombiePartAudio();
                }
                else
                {
                    while (!_zombieFoleyReady)
                    {
                        yield return null;
                    }
                }
            }
            finally
            {
                _audioLoadingFinished = true;
            }
        }

        /// <summary>Performs load sounds core.</summary>
        /// <returns>The resulting load sounds core.</returns>
        private IEnumerator LoadSoundsCore()
        {
            var files = new Dictionary<GameEvent, string>
            {
                {
                    GameEvent.Planted,
                    "plant.ogg"
                },
                {
                    GameEvent.Shoot,
                    "throw.ogg"
                },
                {
                    GameEvent.SunPickupStarted,
                    "points.ogg"
                },
                {
                    GameEvent.ZombieHit,
                    "splat.ogg"
                },
                {
                    GameEvent.Mower,
                    "lawnmower.ogg"
                },
                {
                    GameEvent.Explosion,
                    "cherrybomb.ogg"
                },
                {
                    GameEvent.DoomExplosion,
                    "doomshroom.ogg"
                },
                {
                    GameEvent.MineExplosion,
                    "potato_mine.ogg"
                },
                {
                    GameEvent.JalapenoExplosion,
                    "jalapeno.ogg"
                },
                {
                    GameEvent.Won,
                    "winmusic.ogg"
                },
                {
                    GameEvent.Lost,
                    "losemusic.ogg"
                }
            };
            foreach (KeyValuePair<GameEvent, string> item in files)
            {
                var path = Path.Combine(_owner._root, "sounds", item.Value);
                if (!File.Exists(path))
                {
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _sounds[item.Key] = DownloadHandlerAudioClip.GetContent(request);
                    }
                    else
                    {
                        Debug.LogWarning("Could not load sound " + item.Value + ": " + request.error);
                    }
                }
            }

            yield return LoadAdventureRewardSounds();
        }

        /// <summary>Performs play sound.</summary>
        /// <param name = "gameEvent">The game event.</param>
        internal void PlaySound(GameEvent gameEvent)
        {
            if (SuppressLegacyZombieHitSound(gameEvent))
            {
                return;
            }

            if (PlayAdventureCherryBombSounds(gameEvent))
            {
                return;
            }

            if (gameEvent == GameEvent.Planted && _owner._game?.Presentation._nativePlantingEffects == true)
            {
                // Board::DoPlantingEffects supplies the square-specific Foley; keep any separate Cherry fuse above.
                return;
            }

            AudioClip clip;
            if (_sounds.TryGetValue(gameEvent, out clip) && clip != null)
            {
                RequiredAudioSource.PlayOneShot(clip, gameEvent == GameEvent.ZombieHit ? 0.35f : 0.7f);
            }
        }

        /// <summary>Reuses bounded sound voices for pitched explosions and zombie Foley.</summary>
        internal AudioVoicePool _soundVoices = null!;

        // Called synchronously by PlaySound for the existing simulation event. Returning
        // true suppresses the old Explosion->cherrybomb mapping for this Cherry only.
        /// <summary>Performs play adventure cherry bomb sounds.</summary>
        /// <param name = "trigger">The trigger.</param>
        /// <returns>The resulting play adventure cherry bomb sounds.</returns>
        private bool PlayAdventureCherryBombSounds(GameEvent trigger)
        {
            if (_owner._game == null || !_owner._game.IsAdventure)
            {
                return false;
            }

            AdventureCherryBombSound sound;
            var detonation = false;
            while (_owner._game.TryTakeAdventureCherryBombSound(trigger, out sound))
            {
                if (!sound.Detonation)
                {
                    PlayPresentationSound("reverse_explosion");
                    continue;
                }

                detonation = true;
                PlayPresentationSound("cherrybomb");
                AudioClip clip;
                if (_audioSource == null || !_presentationSounds.TryGetValue("juicy", out clip) || clip == null)
                {
                    continue;
                }

                // PC 63E850 applies integer DirectSound frequency after semitone scaling.
                var frequency = Math.Max(100.0, Math.Min(100000.0, clip.frequency * Math.Pow(1.0594630943592953, sound.JuicyPitchSteps)));
                _soundVoices.TryPlay(clip, _audioSource.volume * .7f, (float)((int)frequency / (double)clip.frequency), group: -1);
            }

            return trigger == GameEvent.Explosion && detonation;
        }

        /// <summary>Performs update adventure cherry bomb audio.</summary>
        internal void UpdateAdventureCherryBombAudio()
        {
            // Also delivers fuse notifications from generated plants whose creation did
            // not go through the player's Planted event. This reads explicit events only.
            PlayAdventureCherryBombSounds(GameEvent.Planted);
            if (_audioSource != null)
            {
                _soundVoices?.SetVolume(_audioSource.volume * .7f, -1);
            }
        }

        /// <summary>Releases the prewarmed sound voices owned by this session.</summary>
        internal void DisposeAdventureCherryBombAudio()
        {
            _soundVoices?.Dispose();
            if (_stormRainSound != null)
            {
                _stormRainSound.Stop();
                UnityEngine.Object.Destroy(_stormRainSound);
                _stormRainSound = null;
            }

            if (_jackInTheBoxMusic != null)
            {
                _jackInTheBoxMusic.Stop();
                UnityEngine.Object.Destroy(_jackInTheBoxMusic);
                _jackInTheBoxMusic = null;
            }

            if (_diggerTunnelingSound != null)
            {
                _diggerTunnelingSound.Stop();
                UnityEngine.Object.Destroy(_diggerTunnelingSound);
                _diggerTunnelingSound = null;
            }
        }

        /// <summary>Owns the single original looping Rain Foley voice for the active storm board.</summary>
        private AudioSource? _stormRainSound;
        /// <summary>Tracks suspension of the rain loop by the board or its modal UI.</summary>
        private bool _stormRainPaused;

        /// <summary>Synchronizes PC looping, one-at-a-time Foley 49 with the storm board's lifetime.</summary>
        private void UpdateStormRainSound()
        {
            var game = _owner._game;
            if (_owner._currentView != PvZGame.ScreenView.Board || game == null || !game.IsAdventure
                || game.Adventure.Level.Mode != AdventureMode.Storm)
            {
                _stormRainSound?.Stop();
                _stormRainPaused = false;
                return;
            }

            if (_audioSource == null || !_presentationSounds.TryGetValue("rain", out var clip) || clip == null)
            {
                return;
            }

            if (_stormRainSound == null)
            {
                _stormRainSound = _audioSource.gameObject.AddComponent<AudioSource>();
                _stormRainSound.playOnAwake = false;
                _stormRainSound.loop = true;
                _stormRainSound.spatialBlend = 0f;
                _stormRainSound.clip = clip;
                _stormRainSound.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
                _stormRainSound.ignoreListenerPause = _audioSource.ignoreListenerPause;
            }

            _stormRainSound.volume = _soundVolume;
            // PC Board::Pause does not GamePause Foley during LevelIntro; the loss dialog pauses the board later.
            var paused = !_owner.Opening.AdventureIntroActive
                && (game.State == MatchState.Paused || _owner._optionsView != null && _owner._optionsView.IsOpen
                    || _owner._profileDialog != null && _owner._profileDialog.IsOpen || game.Adventure.Storm.LossDialogReady);
            if (paused)
            {
                if (!_stormRainPaused)
                {
                    _stormRainSound.Pause();
                    _stormRainPaused = true;
                }
            }
            else
            {
                if (_stormRainPaused)
                {
                    _stormRainSound.UnPause();
                    _stormRainPaused = false;
                }

                if (!_stormRainSound.isPlaying)
                {
                    _stormRainSound.Play();
                }
            }
        }

        /// <summary>Identifies the simulation whose vase sound serials have been consumed.</summary>
        private GameSimulation? _vaseAudioGame;
        /// <summary>Counts vase swings already delivered to the original Foley player.</summary>
        private int _vaseSwingSoundSerial;
        /// <summary>Counts vase impacts already delivered to the original sample and Foley players.</summary>
        private int _vaseBreakSoundSerial;
        /// <summary>Reuses the native Foley request for event-driven vase feedback.</summary>
        private readonly AdventureZombieSoundEvent _vaseFoley = new AdventureZombieSoundEvent();

        /// <summary>Plays the original swing at mallet creation and both impact sounds when the pot opens.</summary>
        internal void UpdateAdventureVaseAudio()
        {
            var game = _owner._game;
            if (!ReferenceEquals(_vaseAudioGame, game))
            {
                _vaseAudioGame = game;
                _vaseSwingSoundSerial = 0;
                _vaseBreakSoundSerial = 0;
            }

            if (game == null || !game.IsAdventure || _audioSource == null)
            {
                return;
            }

            _vaseFoley.Tick = game.Adventure._ticks;
            while (_vaseSwingSoundSerial < game.Presentation.VaseSwingSoundSerial)
            {
                _vaseSwingSoundSerial++;
                _vaseFoley.Foley = 47;
                PlayAdventureZombieFoley(_vaseFoley);
            }

            while (_vaseBreakSoundSerial < game.Presentation.VaseBreakSoundSerial)
            {
                _vaseBreakSoundSerial++;
                if (_presentationSounds.TryGetValue("bonk", out var bonk) && bonk != null)
                {
                    _audioSource.PlayOneShot(bonk);
                }

                _vaseFoley.Foley = 35;
                PlayAdventureZombieFoley(_vaseFoley);
            }
        }

        /// <summary>Stores potato mine audio game.</summary>
        private GameSimulation? _potatoMineAudioGame;
        /// <summary>Stores potato mine rise audio serial.</summary>
        private int _potatoMineRiseAudioSerial;
        /// <summary>Stores the number of Grave Buster chomp sounds already played.</summary>
        private int _graveBusterChompAudioSerial;

        /// <summary>Performs update adventure potato mine audio.</summary>
        internal void UpdateAdventurePotatoMineAudio()
        {
            if (_potatoMineAudioGame != _owner._game)
            {
                _potatoMineAudioGame = _owner._game;
                _potatoMineRiseAudioSerial = 0;
                _graveBusterChompAudioSerial = 0;
            }

            if (_owner._game == null || !_owner._game.IsAdventure)
            {
                return;
            }

            var serial = _owner._game.Presentation.PotatoMineRiseSoundSerial;
            while (_potatoMineRiseAudioSerial < serial)
            {
                _potatoMineRiseAudioSerial++;
                PlayPresentationSound("dirt_rise");
            }

            var chompSerial = _owner._game.Presentation.GraveBusterChompSoundSerial;
            while (_graveBusterChompAudioSerial < chompSerial)
            {
                _graveBusterChompAudioSerial++;
                PlayPresentationSound("gravebusterchomp");
            }
        }
        /// <summary>Stores award sounds.</summary>
        private readonly Dictionary<string, AudioClip> _awardSounds = new Dictionary<string, AudioClip>();
        /// <summary>Performs load adventure reward sounds.</summary>
        /// <returns>The resulting load adventure reward sounds.</returns>
        private IEnumerator LoadAdventureRewardSounds()
        {
            foreach (string file in new[]
            {
                "seedlift.ogg",
                "tap2.ogg",
                "lightfill.ogg",
                "readysetplant.ogg",
                "roll_in.ogg",
                "paper.ogg",
                "buttonclick.ogg",
                "shovel.ogg"
            }

            )
            {
                var location = Path.Combine(_owner._root, "sounds", file);
                if (!File.Exists(location))
                {
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(location).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _awardSounds[file] = DownloadHandlerAudioClip.GetContent(request);
                    }
                }
            }
        }

        /// <summary>Performs play adventure reward sound.</summary>
        /// <param name = "file">The file.</param>
        internal void PlayAdventureRewardSound(string file)
        {
            AudioClip sound;
            if (_awardSounds.TryGetValue(file, out sound) && sound != null)
            {
                RequiredAudioSource.PlayOneShot(sound);
            }
        }

        /// <summary>Performs handle adventure reward notice sound.</summary>
        /// <param name = "notice">The notice.</param>
        internal void HandleAdventureRewardNoticeSound(AdventureNotice notice)
        {
            if (notice.Kind == AdventureNoticeKind.RewardFade)
            {
                PlayAdventureRewardSound("lightfill.ogg");
                return;
            }

            if (notice.Kind != AdventureNoticeKind.RewardCollected || _owner.RequiredGame.Adventure.RewardPresentation == null)
            {
                return;
            }

            var level = _owner.RequiredGame.Adventure.Level.Number;
            if (!_owner.RequiredGame.Adventure.Level.IsReplay && level == 4)
            {
                PlayAdventureRewardSound("shovel.ogg");
            }
            else if (!_owner.RequiredGame.Adventure.Level.IsReplay && (level == 24 || level == 34 || level == 44))
            {
                PlayAdventureRewardSound("tap2.ogg");
            }
            else
            {
                PlayAdventureRewardSound("seedlift.ogg");
                PlayAdventureRewardSound("tap2.ogg");
            }
        }

        /// <summary>Plays the original rejection sample for an unavailable seed or fixed chooser card.</summary>
        internal void PlaySeedBankBuzzer()
        {
            PlayPresentationSound("buzzer");
        }

        /// <summary>Plays the original pickup sample when a money collection flight begins.</summary>
        /// <param name="kind">The silver, gold, or diamond pickup being collected.</param>
        internal void PlayAdventureCoinPickupSound(AdventureCoinKind kind)
        {
            var diamond = kind == AdventureCoinKind.Diamond;
            if (_audioSource == null || !_presentationSounds.TryGetValue(diamond ? "diamond" : "coin", out var clip) || clip == null)
            {
                return;
            }

            if (diamond)
            {
                _audioSource.PlayOneShot(clip);
                return;
            }

            // Original Coin Foley uses the coin sample with a ten-semitone pitch range.
            var semitones = (float)_zombieFoleyRandom.NextDouble() * 10f;
            var frequency = Math.Max(100d, Math.Min(100000d, clip.frequency * Math.Pow(1.059463094359295d, semitones)));
            _soundVoices.TryPlay(clip, _soundVolume, (float)(frequency / clip.frequency), 31, 8);
        }
        /// <summary>Stores presentation sounds.</summary>
        internal readonly Dictionary<string, AudioClip> _presentationSounds = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        /// <summary>Stores presentation resource clips.</summary>
        internal readonly HashSet<AudioClip> _presentationResourceClips = new HashSet<AudioClip>();

        /// <summary>Performs play presentation sound.</summary>
        /// <param name = "name">The name.</param>
        internal void PlayPresentationSound(string name)
        {
            AudioClip clip;
            if (_audioSource != null && _presentationSounds.TryGetValue(name, out clip) && clip != null)
            {
                _audioSource.PlayOneShot(clip, 0.7f);
            }
        }
        // Called at the top of the host PlaySound; keeps the old classic-game event behavior.
        /// <summary>Performs suppress legacy zombie hit sound.</summary>
        /// <param name = "evt">The evt.</param>
        /// <returns>The resulting suppress legacy zombie hit sound.</returns>
        private bool SuppressLegacyZombieHitSound(GameEvent evt)
        {
            if (_owner._game == null || !_owner._game.IsAdventure)
            {
                return false;
            }

            if (evt == GameEvent.Shoot && _owner._game.Presentation._nativeShootSuppressions > 0)
            {
                // The simulation already queued the original plant-specific Foley for this release.
                _owner._game.Presentation._nativeShootSuppressions--;
                return true;
            }

            if (evt == GameEvent.Explosion && _owner._game.Presentation._nativeExplosionSuppressions > 0)
            {
                _owner._game.Presentation._nativeExplosionSuppressions--;
                return true;
            }

            return evt == GameEvent.ZombieHit;
        }

        /// <summary>Owns the single original looping box tune shared by all headed running box zombies.</summary>
        private AudioSource? _jackInTheBoxMusic;
        /// <summary>Tracks whether the box tune is suspended by the board pause.</summary>
        private bool _jackInTheBoxMusicPaused;

        /// <summary>Synchronizes the original reference-counted box music with live actors and the active board.</summary>
        private void UpdateJackInTheBoxMusic()
        {
            var game = _owner._game;
            var active = false;
            if (_owner._currentView == PvZGame.ScreenView.Board && game != null && game.IsAdventure && (game.State == MatchState.Playing || game.State == MatchState.Paused))
            {
                foreach (var zombie in game.Zombies)
                {
                    if (zombie.Kind == ZombieKind.JackInTheBox && zombie.PresentationNativePhase == 15
                        && !zombie.Headless && zombie.Health > 0f)
                    {
                        active = true;
                        break;
                    }
                }
            }

            if (!active)
            {
                if (_jackInTheBoxMusic != null)
                {
                    _jackInTheBoxMusic.Stop();
                }

                _jackInTheBoxMusicPaused = false;
                return;
            }

            if (_audioSource == null || !_presentationSounds.TryGetValue("jackinthebox", out var clip) || clip == null)
            {
                return;
            }

            if (_jackInTheBoxMusic == null)
            {
                _jackInTheBoxMusic = _audioSource.gameObject.AddComponent<AudioSource>();
                _jackInTheBoxMusic.playOnAwake = false;
                _jackInTheBoxMusic.loop = true;
                _jackInTheBoxMusic.spatialBlend = 0f;
                _jackInTheBoxMusic.clip = clip;
                _jackInTheBoxMusic.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
                _jackInTheBoxMusic.ignoreListenerPause = _audioSource.ignoreListenerPause;
            }

            _jackInTheBoxMusic.volume = _soundVolume;
            if (game!.State == MatchState.Paused)
            {
                if (!_jackInTheBoxMusicPaused)
                {
                    _jackInTheBoxMusic.Pause();
                    _jackInTheBoxMusicPaused = true;
                }
            }
            else
            {
                if (_jackInTheBoxMusicPaused)
                {
                    _jackInTheBoxMusic.UnPause();
                    _jackInTheBoxMusicPaused = false;
                }

                if (!_jackInTheBoxMusic.isPlaying)
                {
                    _jackInTheBoxMusic.Play();
                }
            }
        }

        /// <summary>Owns the original reference-counted drill sample shared by live tunneling diggers.</summary>
        private AudioSource? _diggerTunnelingSound;
        /// <summary>Tracks whether the tunneling sample is suspended by the board pause.</summary>
        private bool _diggerTunnelingSoundPaused;

        /// <summary>Synchronizes PC Foley 33 with tunneling actors, immobilization and the active board.</summary>
        private void UpdateDiggerTunnelingSound()
        {
            var game = _owner._game;
            var active = false;
            if (_owner._currentView == PvZGame.ScreenView.Board && game != null && game.IsAdventure && (game.State == MatchState.Playing || game.State == MatchState.Paused))
            {
                foreach (var zombie in game.Zombies)
                {
                    // PC StartZombieSound/StopZombieSound retains one shared Foley 33 voice.
                    var tunneling = zombie.PresentationNativePhase == 32
                        || zombie.PresentationNativePhase < 0 && zombie.AbilityPhase == 0;
                    if (zombie.Kind == ZombieKind.Digger && tunneling && !zombie.Headless
                        && zombie.Health > 0f && zombie.FrozenRemaining <= 0f && zombie.PresentationButter <= 0f)
                    {
                        active = true;
                        break;
                    }
                }
            }

            if (!active)
            {
                if (_diggerTunnelingSound != null)
                {
                    _diggerTunnelingSound.Stop();
                }

                _diggerTunnelingSoundPaused = false;
                return;
            }

            if (_audioSource == null || !_presentationSounds.TryGetValue("digger_zombie", out var clip) || clip == null)
            {
                return;
            }

            if (_diggerTunnelingSound == null)
            {
                _diggerTunnelingSound = _audioSource.gameObject.AddComponent<AudioSource>();
                _diggerTunnelingSound.playOnAwake = false;
                _diggerTunnelingSound.loop = true;
                _diggerTunnelingSound.spatialBlend = 0f;
                _diggerTunnelingSound.clip = clip;
                _diggerTunnelingSound.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
                _diggerTunnelingSound.ignoreListenerPause = _audioSource.ignoreListenerPause;
            }

            _diggerTunnelingSound.volume = _soundVolume;
            if (game!.State == MatchState.Paused)
            {
                if (!_diggerTunnelingSoundPaused)
                {
                    _diggerTunnelingSound.Pause();
                    _diggerTunnelingSoundPaused = true;
                }
            }
            else
            {
                if (_diggerTunnelingSoundPaused)
                {
                    _diggerTunnelingSound.UnPause();
                    _diggerTunnelingSoundPaused = false;
                }

                if (!_diggerTunnelingSound.isPlaying)
                {
                    _diggerTunnelingSound.Play();
                }
            }
        }

        /// <summary>Stores zombie part audio game.</summary>
        private GameSimulation? _zombiePartAudioGame;
        /// <summary>Stores zombie part audio serial, rise sound serial, and zombie sound read index.</summary>
        private int _zombiePartAudioSerial, _zombieRiseSoundSerial, _zombieSoundReadIndex;
        /// <summary>Stores zombie part audio loading,  zombie foley ready.</summary>
        private bool _zombiePartAudioLoading, _zombieFoleyReady;
        /// <summary>Stores zombie foley random.</summary>
        private readonly System.Random _zombieFoleyRandom = new global::PlantsVsZombies.Randomness.OriginalRandomAdapter(12345U);
        /// <summary>Stores zombie foley last tick.</summary>
        private readonly Dictionary<int, int> _zombieFoleyLastTick = new Dictionary<int, int>(AdventureZombieFoleyData.Definitions.Count);

        /// <summary>Performs update adventure zombie part audio.</summary>
        internal void UpdateAdventureZombiePartAudio()
        {
            if (!ReferenceEquals(_zombiePartAudioGame, _owner._game))
            {
                _zombiePartAudioGame = _owner._game;
                _zombiePartAudioSerial = 0;
                _zombieRiseSoundSerial = 0;
                _zombieSoundReadIndex = 0;
                _zombieFoleyLastTick.Clear();
                _jackInTheBoxMusic?.Stop();
                _jackInTheBoxMusicPaused = false;
                _diggerTunnelingSound?.Stop();
                _diggerTunnelingSoundPaused = false;
                _stormRainSound?.Stop();
                _stormRainPaused = false;
            }

            if (!_zombiePartAudioLoading)
            {
                _zombiePartAudioLoading = true;
                _owner.StartCoroutine(LoadAdventureZombiePartAudio());
            }

            if (_owner._game == null || _audioSource == null)
            {
                return;
            }

            if (_owner._game.IsAdventure)
            {
                var riseSerial = _owner._game.Presentation.ZombieRiseSoundSerial;
                while (_zombieRiseSoundSerial < riseSerial)
                {
                    _zombieRiseSoundSerial++;
                    PlayPresentationSound("dirt_rise");
                }
            }

            if (!_zombieFoleyReady)
            {
                return;
            }

            UpdateJackInTheBoxMusic();
            UpdateDiggerTunnelingSound();
            UpdateStormRainSound();
            while (_zombieSoundReadIndex < _owner._game.Presentation.ZombieSounds.Count)
            {
                var evt = _owner._game.Presentation.ZombieSounds[_zombieSoundReadIndex++];
                if (evt.Foley == 25)
                {
                    _zombiePartAudioSerial++;
                }

                PlayAdventureZombieFoley(evt);
            }
        }

        /// <summary>Performs play adventure zombie foley.</summary>
        /// <param name = "evt">The evt.</param>
        private void PlayAdventureZombieFoley(AdventureZombieSoundEvent evt)
        {
            if ((evt.Foley == 11 || evt.Foley == 12 || evt.Foley == 70)
                && (_owner._currentView != PvZGame.ScreenView.Board || _owner._choosingSeeds
                    || _owner._game == null || _owner._game.State != MatchState.Playing
                    || _owner._game.Adventure.Phase != AdventurePhase.Playing))
            {
                return;
            }

            var directSample = evt.Foley switch
            {
                -1 => "gulp",
                -2 => "blover",
                -3 => "plantern",
                -4 => "zombiesplash",
                -5 => "mindcontrolled",
                -6 => "boing",
                -7 => "ladder_zombie",
                _ => null
            };
            if (directSample != null)
            {
                if (_presentationSounds.TryGetValue(directSample, out var directClip) && directClip != null)
                {
                    RequiredAudioSource.PlayOneShot(directClip);
                }

                return;
            }

            AdventureZombieFoleyDefinition definition;
            if (!AdventureZombieFoleyData.Definitions.TryGetValue(evt.Foley, out definition))
            {
                return;
            }

            // Keep native limits for ambient/chewing Foley; each projectile impact needs its own feedback.
            var projectileImpact = evt.Foley == 1 || evt.Foley == 32 || evt.Foley == 38 || evt.Foley == 43
                || evt.Foley == 46 || evt.Foley == 58 || evt.Foley == 62;
            int previous;
            // Storm countdowns continue while paused, independently of the frozen combat-event tick.
            if (evt.Foley != 17 && !projectileImpact && _zombieFoleyLastTick.TryGetValue(evt.Foley, out previous) && evt.Tick - previous < 10)
            {
                return;
            }

            // PC Foley 100 is OneAtATime: simultaneous dance leaders share one summoning vocal.
            var groupLimit = evt.Foley == 100 ? 1 : 8;
            if (!_soundVoices.CanPlay(evt.Foley, groupLimit, replaceOldest: projectileImpact))
            {
                return;
            }

            // PC table 0x74B3B8: preserve duplicate entries as variation weights.
            var semitones = evt.Pitch + (definition.PitchRange == 0f ? 0f : (float)_zombieFoleyRandom.NextDouble() * definition.PitchRange);
            var sample = definition.Samples[_zombieFoleyRandom.Next(definition.Samples.Length)];
            AudioClip clip;
            if (!_presentationSounds.TryGetValue(sample, out clip) || clip == null)
            {
                return;
            }

            // PC DSoundInstance::AdjustPitch 0x63E850, including native frequency limits.
            var frequency = Math.Max(100d, Math.Min(100000d, clip.frequency * Math.Pow(1.059463094359295d, semitones)));
            if (_soundVoices.TryPlay(clip, _soundVolume, (float)(frequency / clip.frequency), evt.Foley, groupLimit, replaceOldest: projectileImpact))
            {
                _zombieFoleyLastTick[evt.Foley] = evt.Tick;
            }
        }

        /// <summary>Loads the original native Foley recordings shared by combat actors and plant abilities.</summary>
        /// <returns>The original audio decoding requests.</returns>
        private IEnumerator LoadAdventureZombiePartAudio()
        {
            var names = new HashSet<string>
            {
                "gulp",
                "blover",
                "plantern",
                "zombiesplash",
                "mindcontrolled",
                "jackinthebox",
                "digger_zombie",
                "boing",
                "ladder_zombie"
            };
            foreach (var definition in AdventureZombieFoleyData.Definitions.Values)
            {
                foreach (string name in definition.Samples)
                {
                    names.Add(name);
                }
            }

            foreach (string name in names)
            {
                if (_presentationSounds.ContainsKey(name))
                {
                    continue;
                }

                var path = Path.Combine(_owner._root, "sounds", name + ".ogg");
                if (!File.Exists(path))
                {
                    Debug.LogError("[Audio] Missing original combat Foley: " + path);
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _presentationSounds[name] = DownloadHandlerAudioClip.GetContent(request);
                    }
                    else
                    {
                        Debug.LogError("[Audio] Original combat Foley failed: " + name + ": " + request.error);
                    }
                }
            }

            // Existing DisposeDynamicAudio owns the clips; child audio sources share this component's lifetime.
            _zombieFoleyReady = true;
        }
    }
}
