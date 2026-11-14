#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks shipped bungee recordings reach production Foley voices at the original movement/capture events.</summary>
    public sealed class OriginalBungeeAudioTests
    {
        /// <summary>Checks scream preloading, its native pitch range, landing step, lift Floop and umbrella feedback.</summary>
        /// <returns>Original audio decoding and actual source-playback boundaries.</returns>
        [UnityTest]
        public IEnumerator OriginalBungeeMovementCaptureAndUmbrellaSoundsReachTheVoices()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "sounds", "bungee_scream.ogg")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var host = new GameObject("Inactive original bungee audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original bungee Foley voices");
            var source = audio.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audio.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            try
            {
                owner._root = root;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 48 }, 12345);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = new AudioVoicePool(audio, source, 32);
                yield return effects.LoadSounds();
                var screams = new[] { "bungee_scream", "bungee_scream2", "bungee_scream3" };
                foreach (var name in screams)
                {
                    var clip = effects._presentationSounds[name];
                    var samples = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(samples, 0), Is.True);
                    Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .001f), Is.True);
                }

                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.Bungee, 1, 9.5f, 0 })!;
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(owner._game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 44), Is.False,
                    "The original scream is delayed until the descending bungee passes altitude 1500.");
                zombie.PresentationAltitude = 2600f;
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(Playing(audio, effects._presentationSounds["grassstep"]), Is.EqualTo(1));
                zombie.PresentationAltitude = 1500f;
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                var screamCount = 0;
                foreach (var name in screams)
                {
                    screamCount += Playing(audio, effects._presentationSounds[name]);
                }

                Assert.That(screamCount, Is.EqualTo(1));
                foreach (var voice in audio.GetComponents<AudioSource>())
                {
                    if (voice.clip != null && Array.Exists(screams, name => effects._presentationSounds[name] == voice.clip) && voice.isPlaying)
                    {
                        Assert.That(voice.pitch, Is.InRange(1f, 1.13f), "PC Foley 44 has zero through two semitones of upward pitch.");
                    }
                }

                effects.UpdateAdventureZombiePartAudio();
                Assert.That(owner._game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 44).Count, Is.EqualTo(1));
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(owner._game, new object[] { PlantKind.Wallnut, zombie.Row, zombie.TargetColumn })!;
                zombie.BungeeState!.TargetPlant = plant;
                zombie.BungeeState.Enter(7);
                zombie.BungeeState.AnimationSeconds = 15f / 24f;
                zombie.PresentationNativePhase = 7;
                zombie.PresentationAltitude = 0f;
                plant.BungeeState = 1;
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(Playing(audio, effects._presentationSounds["floop"]), Is.EqualTo(1));
                var second = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.Bungee, 1, 9.5f, 0 })!;
                owner._game.Plants.Add(new Plant { Id = 1000, Kind = PlantKind.UmbrellaLeaf, Row = second.Row,
                    Column = second.TargetColumn, Layer = PlantLayer.Main, Health = 300f });
                second.PresentationAltitude = 48f;
                source.Stop();
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(source.isPlaying, Is.True, "The direct original BOING sample uses the primary one-shot source.");
                Assert.That(Playing(audio, effects._presentationSounds["throw2"]), Is.EqualTo(1));
            }
            finally
            {
                effects.DisposeAdventureCherryBombAudio();
                var clips = new HashSet<AudioClip>(effects._presentationSounds.Values);
                clips.UnionWith(effects._sounds.Values);
                clips.UnionWith(((Dictionary<string, AudioClip>)typeof(GameSoundEffects)
                    .GetField("_awardSounds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(effects)!).Values);
                foreach (var clip in clips)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                effects._presentationSounds.Clear();
                effects._sounds.Clear();
                AudioListener.pause = previousPause;
                UnityEngine.Object.Destroy(audio);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Counts actual voices playing one exact original sample.</summary>
        /// <param name="owner">The object owning production AudioSources.</param>
        /// <param name="clip">The exact decoded original recording.</param>
        /// <returns>The number of sources currently playing the recording.</returns>
        private static int Playing(GameObject owner, AudioClip clip)
        {
            var count = 0;
            foreach (var voice in owner.GetComponents<AudioSource>())
            {
                if (voice.clip == clip && voice.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
