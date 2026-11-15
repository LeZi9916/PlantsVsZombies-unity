#nullable enable

using System;
using System.Collections;
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
    /// <summary>Checks original giant Foley recordings reach real production audio voices at their native events.</summary>
    public sealed class OriginalGargantuarAudioTests
    {
        /// <summary>Checks smash groans, thump, squish, swing and imp recordings at real native action events.</summary>
        /// <returns>Production audio-load and real voice playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator SmashSquishAndThrowPlayOriginalSamples()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "sounds")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var host = new GameObject("Inactive original giant audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original giant Foley voices");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 49 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                SimulationFixture.Plants(owner._game).Clear();
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = new AudioVoicePool(audio, source, 32);
                yield return effects.LoadSounds();
                foreach (var name in new[] { "lowgroan", "lowgroan2", "gargantuar_thump", "chomp", "chomp2", "swing", "imp", "imp2", "gargantudeath" })
                {
                    var clip = effects._presentationSounds[name];
                    var samples = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(samples, 0), Is.True);
                    Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .001f), Is.True, name);
                }

                var game = owner._game;
                var giant = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Gargantuar, 1,
                    AdventureZombieNativeRules.GridX(330f), 0)!;
                Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 1, 3);
                game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(FindVoice(audio, effects._presentationSounds["lowgroan"]) != null
                    || FindVoice(audio, effects._presentationSounds["lowgroan2"]) != null, Is.True);
                for (var tick = 0; tick < 132; tick++)
                {
                    game.Update(.01f);
                }

                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                var thump = FindVoice(audio, effects._presentationSounds["gargantuar_thump"]);
                Assert.That(thump, Is.Not.Null);
                Assert.That(thump!.pitch, Is.InRange(1f, Mathf.Pow(2f, 2f / 12f)));
                Assert.That(FindVoice(audio, effects._presentationSounds["chomp"]) != null
                    || FindVoice(audio, effects._presentationSounds["chomp2"]) != null, Is.True);
                for (var tick = 0; tick < 75; tick++)
                {
                    game.Update(.01f);
                }

                giant.X = AdventureZombieNativeRules.GridX(650f);
                giant.Health = 1499f;
                for (var tick = 0; tick < 106; tick++)
                {
                    game.Update(.01f);
                }

                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(FindVoice(audio, effects._presentationSounds["swing"]), Is.Not.Null);
                var imp = FindVoice(audio, effects._presentationSounds["imp"])
                    ?? FindVoice(audio, effects._presentationSounds["imp2"]);
                Assert.That(imp, Is.Not.Null);
                Assert.That(imp!.pitch, Is.InRange(1f, Mathf.Pow(2f, 5f / 12f)));
                Invoke(game, "HitAdventureZombie", giant, 10000f, true, 0U);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                var death = FindVoice(audio, effects._presentationSounds["gargantudeath"]);
                Assert.That(death, Is.Not.Null, "The original giant death Foley must reach a production voice immediately.");
                Assert.That(death!.pitch, Is.InRange(1f, Mathf.Pow(2f, 3f / 12f)));
            }
            finally
            {
                effects._soundVoices?.Dispose();
                foreach (var clip in effects._presentationSounds.Values)
                {
                    if (clip != null)
                    {
                        UnityEngine.Object.Destroy(clip);
                    }
                }

                effects._presentationSounds.Clear();
                effects._sounds.Clear();
                AudioListener.pause = previousPause;
                UnityEngine.Object.Destroy(audio);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Finds a real voice currently playing an exact decoded original recording.</summary>
        /// <param name="owner">The production voice owner.</param>
        /// <param name="clip">The unchanged original recording.</param>
        /// <returns>The playing voice, or null when the expected event was not audible to the engine.</returns>
        private static AudioSource? FindVoice(GameObject owner, AudioClip clip)
        {
            foreach (var voice in owner.GetComponents<AudioSource>())
            {
                if (voice.clip == clip && voice.isPlaying)
                {
                    return voice;
                }
            }

            return null;
        }

        /// <summary>Invokes the original action to produce real simulation audio events.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The retained original action name.</param>
        /// <param name="arguments">The original action parameters.</param>
        /// <returns>The production action result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
