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
    /// <summary>Checks the original drill loop and rising Foley through the production audio sources.</summary>
    public sealed class OriginalDiggerAudioTests
    {
        /// <summary>Checks shared drill playback, original rise samples and all drill stop/resume paths.</summary>
        /// <returns>Original sample decoding and actual audio playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator TunnelingSoundSharesOneVoiceAndFollowsOriginalLifetime()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "sounds", "digger_zombie.ogg")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var clips = new Dictionary<string, AudioClip>();
            var host = new GameObject("Inactive original digger audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original drill audio sources");
            var source = audio.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audio.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            effects._audioSource = source;
            effects._soundVolume = .7f;
            effects._soundVoices = new AudioVoicePool(audio, source, 8);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = typeof(GameSoundEffects).GetMethod("UpdateDiggerTunnelingSound", flags)!;
            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            try
            {
                foreach (var name in new[] { "digger_zombie", "dirt_rise", "wakeup" })
                {
                    using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(root, "sounds", name + ".ogg")).AbsoluteUri, AudioType.OGGVORBIS);
                    yield return request.SendWebRequest();
                    Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    clips.Add(name, clip);
                    effects._presentationSounds[name] = clip;
                }

                owner._game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
                owner._currentView = PvZGame.ScreenView.Board;
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                var first = new Zombie { Kind = ZombieKind.Digger, Health = 270f, PresentationNativePhase = 32 };
                var second = new Zombie { Kind = ZombieKind.Digger, Health = 270f, PresentationNativePhase = 32 };
                owner._game.Zombies.Add(first);
                owner._game.Zombies.Add(second);
                update.Invoke(effects, null);
                yield return null;
                var loop = (AudioSource)typeof(GameSoundEffects).GetField("_diggerTunnelingSound", flags)!.GetValue(effects)!;
                Assert.That(loop.clip, Is.SameAs(clips["digger_zombie"]));
                Assert.That(loop.loop && loop.isPlaying, Is.True);
                Assert.That(loop.pitch, Is.EqualTo(1f), "PC Foley 33 has no pitch variation.");
                first.PresentationNativePhase = 33;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.True, "Another tunneling digger retains the shared sample.");
                second.PresentationNativePhase = 34;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "Losing the last pickaxe stops drilling before the rise.");

                second.PresentationNativePhase = 32;
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                effects.SetVolume(.3f);
                Assert.That(loop.volume, Is.EqualTo(.3f).Within(.001f));
                owner._game.TogglePause();
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False);
                owner._game.TogglePause();
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);

                second.FrozenRemaining = 1f;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "Immobilization stops the drill Foley.");
                second.FrozenRemaining = 0f;
                second.PresentationButter = 1f;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False);
                second.PresentationButter = 0f;
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                second.Headless = true;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False);
                second.Headless = false;
                second.Health = 0f;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False);
                second.Health = 270f;
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                owner._currentView = PvZGame.ScreenView.Selector;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "Leaving the board releases the drill sample.");

                var playFoley = typeof(GameSoundEffects).GetMethod("PlayAdventureZombieFoley", flags)!;
                playFoley.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = 80, Tick = 100 } });
                playFoley.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = 83, Tick = 100 } });
                yield return null;
                var riseVoices = 0;
                var wakeupVoices = 0;
                foreach (var voice in audio.GetComponents<AudioSource>())
                {
                    if (voice.clip == clips["dirt_rise"] && voice.isPlaying)
                    {
                        riseVoices++;
                        Assert.That(voice.pitch, Is.InRange(1f, Mathf.Pow(2f, 5f / 12f)));
                    }

                    if (voice.clip == clips["wakeup"] && voice.isPlaying)
                    {
                        wakeupVoices++;
                        Assert.That(voice.pitch, Is.EqualTo(1f));
                    }
                }

                Assert.That(riseVoices, Is.EqualTo(1), "PC Foley 80 plays the original dirt sample once.");
                Assert.That(wakeupVoices, Is.EqualTo(1), "PC Foley 83 plays the original wakeup sample once.");
                Assert.That(AdventureZombieFoleyData.Definitions[80].PitchRange, Is.EqualTo(5f));
            }
            finally
            {
                effects.DisposeAdventureCherryBombAudio();
                effects._presentationSounds.Clear();
                foreach (var clip in clips.Values)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                AudioListener.pause = previousPause;
                UnityEngine.Object.Destroy(audio);
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}
