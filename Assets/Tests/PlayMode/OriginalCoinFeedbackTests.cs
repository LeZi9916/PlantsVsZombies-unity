#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using PlantsVsZombies.Diagnostics;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original money samples reach production audio sources on actual pickup notifications.</summary>
    public sealed class OriginalCoinFeedbackTests
    {
        /// <summary>Owns the inactive game coordinator without running startup or loading a user profile.</summary>
        private GameObject _host = null!;
        /// <summary>Owns active two-dimensional audio sources for this isolated test.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Coordinates the real production pickup sound handler.</summary>
        private PvZGame _owner = null!;
        /// <summary>Supplies the production one-shot source used by diamond pickups.</summary>
        private AudioSource _source = null!;
        /// <summary>Owns the reusable production voices used by silver and gold Foley.</summary>
        private AudioVoicePool _voices = null!;
        /// <summary>Receives the original wallet-show notification without opening the game lifecycle.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Retains the decoded original samples until all playback has stopped.</summary>
        private readonly List<AudioClip> _clips = new List<AudioClip>();

        /// <summary>Releases only the sources, clips, and inactive coordinator created by this fixture.</summary>
        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                _source.Stop();
                _voices.Dispose();
                _presenter.Dispose();
                foreach (var clip in _clips)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }

                UnityEngine.Object.DestroyImmediate(_audioHost);
                UnityEngine.Object.DestroyImmediate(_host);
            }
        }

        /// <summary>Decodes the actual original samples and routes silver, gold, and diamond clicks through the host notification.</summary>
        /// <returns>Frame boundaries for original audio decoding and source playback.</returns>
        [UnityTest]
        public IEnumerator OriginalMoneyPickupPlaysItsOwnSample()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "sounds")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            _host = new GameObject("Inactive original money pickup fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Original money audio fixture");
            _source = _audioHost.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.ignoreListenerPause = true;
            _source.volume = .7f;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _voices = new AudioVoicePool(_audioHost, _source, 8);
            _presenter = new GamePresenter(_owner);
            SetField("_audioSource", _source);
            SetField("_soundVoices", _voices);
            SetField("_soundVolume", .7f);
            SetField("_presenter", _presenter);
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_owner, "_presentationSounds")!;
            foreach (var cue in new[] { "coin", "diamond" })
            {
                var diamond = cue == "diamond";
                var path = Path.Combine(root, "sounds", cue + (diamond ? ".wav" : ".ogg"));
                Assert.That(File.Exists(path), Is.True, "The original pickup sample must be prepared: " + path);
                using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, diamond ? AudioType.WAV : AudioType.OGGVORBIS);
                yield return request.SendWebRequest();
                Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
                var clip = DownloadHandlerAudioClip.GetContent(request);
                _clips.Add(clip);
                sounds[cue] = clip;
                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True);
                var peak = 0f;
                foreach (var sample in samples)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }

                Assert.That(peak, Is.GreaterThan(.01f), "The decoded original sample must contain non-silent audio.");
            }

            var noticeHandler = GameMemberReflection.BindMethod(_owner, "AdventureNotice")!;
            _owner._game.Adventure.NoticeRaised = notice => noticeHandler.Invoke(_owner, new object[] { notice });
            foreach (var kind in new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold, AdventureCoinKind.Diamond })
            {
                _source.Stop();
                foreach (var voice in _audioHost.GetComponents<AudioSource>())
                {
                    voice.Stop();
                }

                var coin = _owner._game.SpawnAdventureCoin(kind, 4f, 1f);
                Assert.That(_owner._game.CollectAdventureCoin(coin), Is.True);
                yield return null;
                if (kind == AdventureCoinKind.Diamond)
                {
                    Assert.That(_source.isPlaying, Is.True, "The diamond uses the original unpitched one-shot sample.");
                }
                else
                {
                    var playingCoin = false;
                    foreach (var voice in _audioHost.GetComponents<AudioSource>())
                    {
                        playingCoin |= voice.clip == sounds["coin"] && voice.isPlaying && voice.pitch >= 1f;
                    }

                    Assert.That(playingCoin, Is.True, "Silver and gold use the original pitched Coin Foley sample.");
                }

                Assert.That(_owner._game.CollectAdventureCoin(coin), Is.False, "The same pickup cannot replay its sound.");
            }

            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            _owner._game.Adventure.NoticeRaised = notice => noticeHandler.Invoke(_owner, new object[] { notice });
            _owner.IndependentModes._modeSaveError = "金币尚未存入，请再次拾取：fixture-save-failure";
            _owner._game.Rules.AdventureCoinCommit = total => false;
            var pending = _owner._game.SpawnAdventureCoin(AdventureCoinKind.Gold, 4f, 1f);
            Assert.That(_owner._game.CollectAdventureCoin(pending), Is.True);
            _owner._game.Update(1.5f);
            Assert.That(pending.PersistencePending && !pending.IsBeingCollected, Is.True);
            Assert.That(_owner.AdventureBoard._adventureFeedback, Is.EqualTo(_owner.IndependentModes._modeSaveError),
                "A failed arrival-time save must reach the existing host retry diagnostic even though the initial click succeeded.");
            Assert.That(_owner.AdventureBoard._adventureFeedbackUntil, Is.GreaterThan(Time.unscaledTime));
            _owner._game.Rules.AdventureCoinCommit = total => true;
            Assert.That(_owner._game.CollectAdventureCoin(pending), Is.True);
            _owner._game.Update(.01f);
            Assert.That(_owner._game.Adventure.MoneyCollectedUnits, Is.EqualTo(5), "The displayed error leaves the coin retryable.");
        }

        /// <summary>Supplies a private production dependency without invoking game startup or profile persistence.</summary>
        /// <param name="name">The production field being supplied.</param>
        /// <param name="value">The isolated fixture dependency.</param>
        private void SetField(string name, object value)
        {
            GameMemberReflection.SetField(_owner, name, value);
        }
    }
}
