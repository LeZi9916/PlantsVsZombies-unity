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
    /// <summary>Checks original Balloon and Pogo recordings are ready before gameplay and reach the actual Foley voices.</summary>
    public sealed class OriginalZombieArrivalAudioTests
    {
        /// <summary>Checks startup preloading and the real normal-wave and bounce sound paths without injecting decoded clips.</summary>
        /// <returns>Original audio decoding and native audio-source playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator OriginalEntryAndPogoSoundsAreReadyBeforeTheFirstNormalWave()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "sounds")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            var host = new GameObject("Inactive original zombie audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original arrival and Pogo audio sources");
            var source = audio.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audio.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            var voices = new AudioVoicePool(audio, source, 64);
            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            try
            {
                owner._root = root;
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = voices;
                yield return effects.LoadSounds();
                Assert.That(effects._audioLoadingFinished, Is.True);
                Assert.That(effects._presentationSounds.ContainsKey("ballooninflate"), Is.True,
                    "LoadingSounds must finish the original entry Foley before the loading screen admits normal waves.");
                Assert.That(effects._presentationSounds.ContainsKey("pogo_zombie"), Is.True);
                var balloonClip = effects._presentationSounds["ballooninflate"];
                var pogoClip = effects._presentationSounds["pogo_zombie"];
                AssertRecording(balloonClip);
                AssertRecording(pogoClip);
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 7529);
                owner._currentView = PvZGame.ScreenView.Board;
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GameSimulation).GetMethod("SpawnScheduledAdventureWave", flags)!.Invoke(owner._game,
                    new object[] { new AdventureWave { Index = 0, Zombies = new[] { ZombieKind.Balloon } } });
                Assert.That(owner._game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 60), Has.Count.EqualTo(1));
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                AssertPlaying(audio, balloonClip, 1.79f);
                var pogo = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.Pogo, 1, 9.5f, 0 })!;
                pogo.PogoState!.PhaseTicks = 6;
                owner._game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(owner._game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 26), Has.Count.EqualTo(1));
                AssertPlaying(audio, pogoClip, 1.26f);
                effects.UpdateAdventureZombiePartAudio();
                Assert.That(CountPlaying(audio, balloonClip), Is.EqualTo(1), "Consuming the same normal-wave event twice must not restart its entry sound.");
            }
            finally
            {
                voices.Dispose();
                var clips = new HashSet<AudioClip>(effects._presentationSounds.Values);
                clips.UnionWith(effects._sounds.Values);
                var rewards = (Dictionary<string, AudioClip>)typeof(GameSoundEffects)
                    .GetField("_awardSounds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(effects)!;
                clips.UnionWith(rewards.Values);
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

        /// <summary>Checks a decoded original recording contains real, non-silent samples.</summary>
        /// <param name="clip">The recording decoded by the production resource loader.</param>
        private static void AssertRecording(AudioClip clip)
        {
            Assert.That(clip.samples, Is.GreaterThan(0));
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True);
            Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .001f), Is.True);
        }

        /// <summary>Checks one original recording reaches a single pitched production voice.</summary>
        /// <param name="audio">The fixture owning the production voice pool.</param>
        /// <param name="clip">The original recording expected to play.</param>
        /// <param name="maximumPitch">The upper bound from the native Foley pitch range.</param>
        private static void AssertPlaying(GameObject audio, AudioClip clip, float maximumPitch)
        {
            Assert.That(CountPlaying(audio, clip), Is.EqualTo(1));
            foreach (var voice in audio.GetComponents<AudioSource>())
            {
                if (voice.clip == clip && voice.isPlaying)
                {
                    Assert.That(voice.pitch, Is.InRange(1f, maximumPitch));
                    Assert.That(voice.volume, Is.EqualTo(.7f));
                }
            }
        }

        /// <summary>Counts active production voices playing one exact original clip.</summary>
        /// <param name="audio">The owner of the fixture's voice pool.</param>
        /// <param name="clip">The clip identity to count.</param>
        /// <returns>The number of actually playing matching sources.</returns>
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
