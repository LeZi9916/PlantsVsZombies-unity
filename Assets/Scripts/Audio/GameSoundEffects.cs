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
            try
            {
                yield return LoadSoundsCore();
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
            var directSample = evt.Foley switch
            {
                -1 => "gulp",
                -2 => "blover",
                -3 => "plantern",
                -4 => "zombiesplash",
                -5 => "mindcontrolled",
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
            if (!projectileImpact && _zombieFoleyLastTick.TryGetValue(evt.Foley, out previous) && evt.Tick - previous < 10)
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
            var semitones = (float)_zombieFoleyRandom.NextDouble() * definition.PitchRange;
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
                "mindcontrolled"
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
