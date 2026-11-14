#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original storm recordings, looping rain and thunder through production audio voices.</summary>
    public sealed class OriginalStormAudioTests
    {
        /// <summary>Checks preloading, native thunder thresholds, pause settling and shared rain-loop lifetime.</summary>
        /// <returns>Original audio decoding and actual source-playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator StormPlaysOriginalRainAndThunderWithNativeLifecycle()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "sounds", "rain.ogg")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var host = new GameObject("Inactive original storm audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original storm audio sources");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 40 }, 91);
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = new AudioVoicePool(audio, source, 16);
                yield return effects.LoadSounds();
                Assert.That(effects._audioLoadingFinished, Is.True);
                Assert.That(effects._presentationSounds.ContainsKey("rain"), Is.True);
                Assert.That(effects._presentationSounds.ContainsKey("thunder"), Is.True);
                foreach (var name in new[] { "rain", "thunder" })
                {
                    var clip = effects._presentationSounds[name];
                    var samples = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(samples, 0), Is.True);
                    Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .001f), Is.True);
                }

                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                var rain = effects._presentationSounds["rain"];
                var thunder = effects._presentationSounds["thunder"];
                var loop = (AudioSource)typeof(GameSoundEffects).GetField("_stormRainSound", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(effects)!;
                Assert.That(loop.clip, Is.SameAs(rain));
                Assert.That(loop.loop && loop.isPlaying, Is.True);
                Assert.That(loop.pitch, Is.EqualTo(1f));
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(CountPlaying(audio, rain), Is.EqualTo(1), "PC Foley 49 loops once per board, independently of actor count.");
                for (var tick = 0; tick < 100; tick++)
                {
                    owner._game.Update(.01f);
                }

                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(CountPlaying(audio, thunder), Is.EqualTo(1));
                foreach (var voice in audio.GetComponents<AudioSource>())
                {
                    if (voice.clip == thunder && voice.isPlaying)
                    {
                        Assert.That(voice.pitch, Is.InRange(1f, 1.79f), "PC Thunder Foley has ten semitones of random upward pitch.");
                    }
                }

                owner._game.TogglePause();
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(loop.isPlaying, Is.False);
                owner._game.Adventure.Storm.Countdown = 151;
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(CountPlaying(audio, thunder), Is.EqualTo(2), "Flash settling must not discard thunder because the paused combat-event tick is frozen.");
                owner._game.TogglePause();
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                effects.SetVolume(.3f);
                Assert.That(loop.volume, Is.EqualTo(.3f).Within(.001f));
                typeof(GameSimulation).GetProperty(nameof(GameSimulation.State))!.SetValue(owner._game, MatchState.Lost);
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(loop.isPlaying, Is.True, "The original rain continues over the loss sequence.");
                owner._currentView = PvZGame.ScreenView.Selector;
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(loop.isPlaying, Is.False);
                owner._currentView = PvZGame.ScreenView.Board;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 39 }, 91);
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(loop.isPlaying, Is.False, "Leaving the storm board releases its rain voice.");
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

        /// <summary>Counts production voices actually playing one original clip.</summary>
        /// <param name="audio">The object owning the fixture's sources.</param>
        /// <param name="clip">The exact original recording.</param>
        /// <returns>The number of playing sources sharing that recording.</returns>
        private static int CountPlaying(GameObject audio, AudioClip clip)
        {
            var count = 0;
            foreach (var voice in audio.GetComponents<AudioSource>())
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
