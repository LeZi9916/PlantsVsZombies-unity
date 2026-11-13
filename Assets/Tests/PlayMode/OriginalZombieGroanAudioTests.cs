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
    /// <summary>Checks unchanged original vocal samples decode and reach the production Foley voice admission path.</summary>
    public sealed class OriginalZombieGroanAudioTests
    {
        /// <summary>Checks vocal variations, native pitch and gain, eight voices, ten-tick suppression, and paused or preview gates.</summary>
        /// <returns>The original audio decoding and playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator OriginalGroansReachBoundedProductionVoicesOnlyDuringPlay()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "sounds", "groan.ogg")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            var host = new GameObject("Inactive original zombie vocals fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audioHost = new GameObject("Original zombie vocal voices");
            var template = audioHost.AddComponent<AudioSource>();
            template.playOnAwake = false;
            template.ignoreListenerPause = true;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audioHost.AddComponent<AudioListener>();
            }

            using var voices = new AudioVoicePool(audioHost, template, 64);
            var effects = owner.Sounds;
            effects._audioSource = template;
            effects._soundVolume = .6f;
            effects._soundVoices = voices;
            owner._currentView = PvZGame.ScreenView.Board;
            owner._game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
            Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            var play = typeof(GameSoundEffects).GetMethod("PlayAdventureZombieFoley", BindingFlags.Instance | BindingFlags.NonPublic)!;
            try
            {
                foreach (var id in new[] { 11, 12, 70 })
                {
                    foreach (var sample in AdventureZombieFoleyData.Definitions[id].Samples)
                    {
                        if (effects._presentationSounds.ContainsKey(sample))
                        {
                            continue;
                        }

                        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(root, "sounds", sample + ".ogg")).AbsoluteUri, AudioType.OGGVORBIS);
                        yield return request.SendWebRequest();
                        Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), sample + ": " + request.error);
                        var clip = DownloadHandlerAudioClip.GetContent(request);
                        var pcm = new float[clip.samples * clip.channels];
                        Assert.That(clip.GetData(pcm, 0), Is.True);
                        Assert.That(Array.Exists(pcm, value => Math.Abs(value) > .001f), Is.True, sample);
                        effects._presentationSounds[sample] = clip;
                    }
                }

                foreach (var id in new[] { 11, 12, 70 })
                {
                    voices.StopAll();
                    var definition = AdventureZombieFoleyData.Definitions[id];
                    play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = id, Tick = 100 } });
                    yield return null;
                    var sources = audioHost.GetComponents<AudioSource>();
                    var playing = Array.Find(sources, voice => voice.isPlaying && voice.clip != null);
                    Assert.That(playing, Is.Not.Null, "Original vocal " + id + " must reach a real production voice.");
                    Assert.That(playing!.volume, Is.EqualTo(.6f).Within(.0001f));
                    Assert.That(Array.Exists(definition.Samples, sample => ReferenceEquals(effects._presentationSounds[sample], playing.clip)), Is.True);
                    if (id == 70)
                    {
                        Assert.That(playing.pitch, Is.InRange(1f, 1.13f));
                    }
                    else
                    {
                        Assert.That(playing.pitch, Is.EqualTo(1f));
                    }
                    play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = id, Tick = 109 } });
                    Assert.That(Leased(sources), Is.EqualTo(1), "The original Foley interval rejects the ninth tick.");
                    for (var index = 1; index <= 8; index++)
                    {
                        play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = id, Tick = 100 + index * 10 } });
                    }

                    Assert.That(Leased(sources), Is.EqualTo(8), "Original vocal categories admit eight concurrent voices without stealing a ninth.");
                }

                voices.StopAll();
                owner._game.TogglePause();
                play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = 11, Tick = 1000 } });
                Assert.That(Leased(audioHost.GetComponents<AudioSource>()), Is.Zero);
                owner._game.TogglePause();
                owner._choosingSeeds = true;
                play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = 11, Tick = 1000 } });
                Assert.That(Leased(audioHost.GetComponents<AudioSource>()), Is.Zero);
                owner._choosingSeeds = false;
                owner._currentView = PvZGame.ScreenView.Selector;
                play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = 11, Tick = 1000 } });
                Assert.That(Leased(audioHost.GetComponents<AudioSource>()), Is.Zero);
            }
            finally
            {
                voices.StopAll();
                foreach (var clip in effects._presentationSounds.Values)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                effects._presentationSounds.Clear();
                AudioListener.pause = previousPause;
                UnityEngine.Object.Destroy(audioHost);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Counts voice leases without treating the routing template as a played sound.</summary>
        /// <param name="sources">The template followed by all prewarmed production voices.</param>
        /// <returns>The number of voices retaining an admitted original sample.</returns>
        private static int Leased(AudioSource[] sources)
        {
            var count = 0;
            foreach (var source in sources)
            {
                if (source.clip != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
