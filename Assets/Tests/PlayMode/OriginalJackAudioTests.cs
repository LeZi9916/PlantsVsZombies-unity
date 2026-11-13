#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the original box tune starts on entry and follows the original shared music lifetime.</summary>
    public sealed class OriginalJackAudioTests
    {
        /// <summary>Exercises real decoded original box music through production audio synchronization.</summary>
        /// <returns>Frame boundaries for original sample decoding and actual audio-source playback.</returns>
        [UnityTest]
        public IEnumerator BoxMusicStartsOnEntryAndStopsOnlyWhenLastRunningBoxStops()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            var path = Path.Combine(root, "sounds", "jackinthebox.ogg");
            if (!File.Exists(path))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS);
            yield return request.SendWebRequest();
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
            var clip = DownloadHandlerAudioClip.GetContent(request);
            var host = new GameObject("Inactive original box audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original box tune source fixture");
            var source = audio.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audio.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            effects.Initialize(source, .7f);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = typeof(GameSoundEffects).GetMethod("UpdateJackInTheBoxMusic", flags)!;
            try
            {
                effects._presentationSounds["jackinthebox"] = clip;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
                owner._currentView = PvZGame.ScreenView.Board;
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                var first = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.JackInTheBox, 2, 8f, 5 })!;
                update.Invoke(effects, null);
                yield return null;
                var loop = (AudioSource)typeof(GameSoundEffects).GetField("_jackInTheBoxMusic", flags)!.GetValue(effects)!;
                Assert.That(loop.clip, Is.SameAs(clip));
                Assert.That(loop.loop && loop.isPlaying, Is.True, "A box zombie starts the actual original tune on entry.");
                var second = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.JackInTheBox, 3, 8f, 5 })!;
                first.PresentationNativePhase = 16;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.True, "Another running box retains the shared original tune.");
                owner._game.TogglePause();
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False);
                owner._game.TogglePause();
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                second.Headless = true;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "Losing the last headed running box releases the loop.");
                second.Headless = false;
                second.PresentationNativePhase = 0;
                second.UsedAbility = true;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "A magnetically removed box cannot retain the tune.");
                second.PresentationNativePhase = 15;
                second.UsedAbility = false;
                update.Invoke(effects, null);
                yield return null;
                Assert.That(loop.isPlaying, Is.True);
                owner._currentView = PvZGame.ScreenView.Selector;
                update.Invoke(effects, null);
                Assert.That(loop.isPlaying, Is.False, "Leaving the board releases the original music lifetime.");
            }
            finally
            {
                effects.DisposeAdventureCherryBombAudio();
                effects._presentationSounds.Clear();
                UnityEngine.Object.Destroy(clip);
                UnityEngine.Object.Destroy(audio);
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}
