#nullable enable

using System;
using System.Collections;
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
    /// <summary>Checks original boss entrance, RV and lethal-retreat recordings reach production voices at native contacts.</summary>
    public sealed class OriginalBossAudioTests
    {
        /// <summary>Decodes original non-silent recordings and checks the actual entrance, RV landing and death playback paths.</summary>
        /// <returns>Real original-audio decoding and playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator NativeBossContactsPlayOriginalRecordings()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            var owner = fixture.Owner;
            var audio = new GameObject("Original boss audio fixture");
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
                owner._currentView = PvZGame.ScreenView.Board;
                owner._choosingSeeds = false;
                effects._audioSource = source;
                effects._soundVolume = .7f;
                effects._soundVoices = new AudioVoicePool(audio, source, 32);
                yield return effects.LoadSounds();
                foreach (var name in new[] { "gargantuar_thump", "RVthrow", "bossexplosion", "gargantudeath" })
                {
                    var clip = effects._presentationSounds[name];
                    var samples = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(samples, 0), Is.True);
                    Assert.That(Array.Exists(samples, value => Math.Abs(value) > .001f), Is.True, name);
                }
                var game = owner.RequiredGame;
                for (var tick = 0; tick < 81; tick++)
                {
                    game.AdvanceNativeFeedbackClock();
                    game.AdvanceAdventureBossIntro();
                }
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(Playing(audio, effects._presentationSounds["gargantuar_thump"]), Is.True, "original .24 entrance contact");
                var boss = game.Adventure.Boss!;
                boss.Enter(85, "anim_RV_1", 16f, 0);
                boss.Time = .649f;
                game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(Playing(audio, effects._presentationSounds["RVthrow"]), Is.True, "original .65 RV landing sample");
                boss.Enter(87, "anim_head_idle", 12f, 0);
                game.Adventure.BossHeadVulnerable = true;
                SimulationInspection.GetMethod("DamageAdventureBossWithFlags", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(game, new object[] { 50000f, 0x7FU });
                game.Update(.01f);
                effects.UpdateAdventureZombiePartAudio();
                yield return null;
                Assert.That(Playing(audio, effects._presentationSounds["bossexplosion"]), Is.True, "original lethal retreat explosion sample");
                Assert.That(Playing(audio, effects._presentationSounds["gargantudeath"]), Is.True, "original lethal retreat Foley 91");
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
            }
        }

        /// <summary>Finds an active original production voice without depending on physical speaker output.</summary>
        /// <param name="owner">The pooled voice owner.</param>
        /// <param name="clip">The decoded original recording.</param>
        /// <returns>Whether a real source is currently playing that original recording.</returns>
        private static bool Playing(GameObject owner, AudioClip clip)
        {
            foreach (var voice in owner.GetComponents<AudioSource>())
            {
                if (voice.clip == clip && voice.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
