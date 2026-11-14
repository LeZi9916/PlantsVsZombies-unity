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
    /// <summary>Checks original catapult Foley recordings reach real production audio voices at their native events.</summary>
    public sealed class OriginalCatapultAudioTests
    {
        /// <summary>Checks launch, plant impact, tire puncture and final explosion samples with their original pitch ranges.</summary>
        /// <returns>Production audio-load and real voice playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator BasketballPunctureAndExplosionPlayOriginalSamples()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "sounds")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            var host = new GameObject("Inactive original catapult audio fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audio = new GameObject("Original catapult Foley voices");
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
                owner._game.Plants.Clear();
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = new AudioVoicePool(audio, source, 32);
                yield return effects.LoadSounds();
                var basketball = effects._presentationSounds["basketball"];
                var samples = new float[basketball.samples * basketball.channels];
                Assert.That(basketball.GetData(samples, 0), Is.True);
                Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .001f), Is.True);
                var game = owner._game;
                var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Catapult, 1,
                    AdventureZombieNativeRules.GridX(650f), 0)!;
                Invoke(game, "FireAdventureBasketball", zombie, null!);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                var launchVoice = FindVoice(audio, basketball);
                Assert.That(launchVoice, Is.Not.Null);
                Assert.That(launchVoice!.pitch, Is.InRange(1f, Mathf.Pow(2f, 10f / 12f)));
                Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 1, 0);
                var impact = new Pea { Basketball = true, Row = 1, Damage = 75f,
                    PresentationLobNativeX = 60f, PresentationLobNativeY = 100f, PresentationLobNativeZ = 61f, PresentationLobVelocity = 0f };
                Assert.That(Invoke(game, "AdvanceAdventureBasketball", impact, true), Is.True);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(FindVoice(audio, effects._presentationSounds["splat"]) != null
                    || FindVoice(audio, effects._presentationSounds["splat2"]) != null
                    || FindVoice(audio, effects._presentationSounds["splat3"]) != null, Is.True);
                Invoke(game, "HitAdventureZombie", zombie, 20f, true, 32u);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(FindVoice(audio, effects._presentationSounds["balloon_pop"]), Is.Not.Null);
                zombie.CatapultState!.DeathTicks = 1;
                game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(FindVoice(audio, effects._presentationSounds["explosion"]), Is.Not.Null);
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
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
