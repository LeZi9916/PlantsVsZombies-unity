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
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original plant sound files, native Foley mappings, and the production playback path.</summary>
    public sealed class OriginalPlantSoundTests
    {
        /// <summary>Checks real Fume-shroom and Doom-shroom recordings load and play through their intended production paths.</summary>
        /// <returns>Original audio decoding and playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator NativePlantSamplesLoadAndReachTheirCorrectProductionVoices()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "sounds")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            AssertNativeFoleyMappings();
            var host = new GameObject("Inactive original plant sound fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audioHost = new GameObject("Original plant sound voices");
            var source = audioHost.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            source.volume = .7f;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audioHost.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            var voices = new AudioVoicePool(audioHost, source, 8);
            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            try
            {
                owner._root = root;
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = voices;
                typeof(GameSoundEffects).GetField("_zombiePartAudioLoading", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(effects, true);
                yield return effects.LoadSounds();
                yield return (IEnumerator)typeof(GameSoundEffects)
                    .GetMethod("LoadAdventureZombiePartAudio", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(effects, null)!;
                Assert.That(effects._sounds.ContainsKey(GameEvent.DoomExplosion), Is.True);
                Assert.That(effects._presentationSounds.ContainsKey("fume"), Is.True);
                var doom = effects._sounds[GameEvent.DoomExplosion];
                var fume = effects._presentationSounds["fume"];
                yield return AssertOriginalRecording(root, "doomshroom", doom);
                yield return AssertOriginalRecording(root, "fume", fume);
                Assert.That(doom, Is.Not.SameAs(effects._sounds[GameEvent.Explosion]));

                owner._game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.EventRaised = effects.PlaySound;
                var plant = new Plant { Kind = PlantKind.FumeShroom, Row = 1, Column = 2 };
                typeof(GameSimulation).GetMethod("FireAdventureFume", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(owner._game, new object[] { plant });
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(source.isPlaying, Is.False, "Fume-shroom must suppress the generic throw one-shot.");
                var playingFume = 0;
                foreach (var voice in audioHost.GetComponents<AudioSource>())
                {
                    if (voice.clip == fume && voice.isPlaying)
                    {
                        playingFume++;
                        Assert.That(voice.pitch, Is.InRange(1f, 1.79f), "PC Foley 30 has a ten-semitone pitch range.");
                    }
                }

                Assert.That(playingFume, Is.EqualTo(1), "An actual Fume-shroom release must reach its own original sample once.");
                effects.UpdateAdventureZombiePartAudio();
                foreach (var voice in audioHost.GetComponents<AudioSource>())
                {
                    voice.Stop();
                }

                effects.PlaySound(GameEvent.DoomExplosion);
                yield return null;
                Assert.That(source.isPlaying, Is.True, "The independent original Doom-shroom recording must play.");
            }
            finally
            {
                source.Stop();
                voices.Dispose();
                var clips = new HashSet<AudioClip>(effects._sounds.Values);
                clips.UnionWith(effects._presentationSounds.Values);
                var awardClips = (Dictionary<string, AudioClip>)typeof(GameSoundEffects)
                    .GetField("_awardSounds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(effects)!;
                clips.UnionWith(awardClips.Values);
                foreach (var clip in clips)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                AudioListener.pause = previousPause;
                UnityEngine.Object.Destroy(audioHost);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks the added plant entries against the selected PC executable's literal Foley table.</summary>
        private static void AssertNativeFoleyMappings()
        {
            foreach (var expected in new[]
            {
                (Id: 4, Pitch: 10f, Sample: "throw"), (Id: 29, Pitch: 10f, Sample: "puff"),
                (Id: 30, Pitch: 10f, Sample: "fume"), (Id: 31, Pitch: 10f, Sample: "coin"),
                (Id: 40, Pitch: 2f, Sample: "gargantuar_thump"), (Id: 42, Pitch: 2f, Sample: "magnetshroom"),
                (Id: 53, Pitch: 0f, Sample: "zombie_entering_water"),
                (Id: 57, Pitch: 0f, Sample: "reverse_explosion"), (Id: 63, Pitch: -2f, Sample: "plantgrow"),
                (Id: 64, Pitch: -5f, Sample: "shoop"), (Id: 65, Pitch: 2f, Sample: "juicy"),
                (Id: 69, Pitch: 0f, Sample: "coffee"), (Id: 73, Pitch: 2f, Sample: "throw2"),
                (Id: 76, Pitch: 10f, Sample: "coblaunch"), (Id: 83, Pitch: 0f, Sample: "wakeup")
            })
            {
                var actual = AdventureZombieFoleyData.Definitions[expected.Id];
                Assert.That(actual.PitchRange, Is.EqualTo(expected.Pitch), "PC Foley " + expected.Id);
                Assert.That(actual.Samples, Is.EqualTo(new[] { expected.Sample }), "PC Foley " + expected.Id);
            }

            Assert.That(AdventureZombieFoleyData.Definitions[41].PitchRange, Is.EqualTo(2f));
            Assert.That(AdventureZombieFoleyData.Definitions[41].Samples,
                Is.EqualTo(new[] { "squash_hmm", "squash_hmm", "squash_hmm2" }));
        }

        /// <summary>Compares production-loaded PCM with an independently decoded original file and rejects silent recordings.</summary>
        /// <param name="root">The read-only original resource directory.</param>
        /// <param name="name">The original sound filename without its extension.</param>
        /// <param name="production">The clip loaded by the actual production sound loader.</param>
        /// <returns>The independent original decoding request.</returns>
        private static IEnumerator AssertOriginalRecording(string root, string name, AudioClip production)
        {
            var path = Path.Combine(root, "sounds", name + ".ogg");
            Assert.That(File.Exists(path), Is.True, path);
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS);
            yield return request.SendWebRequest();
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
            var reference = DownloadHandlerAudioClip.GetContent(request);
            try
            {
                Assert.That(production.samples, Is.EqualTo(reference.samples), name);
                Assert.That(production.frequency, Is.EqualTo(reference.frequency), name);
                Assert.That(production.channels, Is.EqualTo(reference.channels), name);
                var expected = new float[reference.samples * reference.channels];
                var actual = new float[production.samples * production.channels];
                Assert.That(reference.GetData(expected, 0), Is.True, name);
                Assert.That(production.GetData(actual, 0), Is.True, name);
                Assert.That(actual, Is.EqualTo(expected), "Production must load the original " + name + " recording.");
                var peak = 0f;
                foreach (var sample in actual)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }

                Assert.That(peak, Is.GreaterThan(.01f), name + " must contain audible PCM.");
            }
            finally
            {
                UnityEngine.Object.Destroy(reference);
            }
        }
    }
}
