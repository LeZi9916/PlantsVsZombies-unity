#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises the production opening dialogue, input routing, and voice playback together.</summary>
    public sealed class CrazyDaveOpeningIntegrationTests
    {
        /// <summary>Owns the inactive production coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the active audio source.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the production opening and input code.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads the fixture's original-format string table.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives the production voice playback.</summary>
        private AudioSource _voice = null!;
        /// <summary>Provides a synthetic voice sample for the fixture.</summary>
        private AudioClip _clip = null!;
        /// <summary>Owns the temporary resource directory.</summary>
        private string _root = null!;

        /// <summary>Initializes the isolated production coordinator, original strings, and synthetic voice.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "dave-opening-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), "[CRAZY_DAVE_201]\nFirst.\n[CRAZY_DAVE_202]\nSecond.\n");
            _host = new GameObject("Inactive Dave opening fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Dave voice fixture");
            _voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Dave test voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._currentView = PvZGame.ScreenView.Board;
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 1);
            _game._animations = new ReanimationRenderer(_root);
            _game._selectorView = new SelectorView(_root);
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", _voice);
            SetField("_soundVolume", .7f);
            SetField("_crazyDaveStartIndex", 201);
            _game._adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = 11, HasCrazyDaveDialog = true }));
            _game._adventureIntroDrawn = true;
        }

        /// <summary>Destroys only the fixture's objects and temporary original-string table.</summary>
        /// <summary>Releases the isolated coordinator, audio, renderer, and temporary files.</summary>
        [TearDown]
        public void TearDown()
        {
            _voice.Stop();
            _game._animations.Dispose();
            _game._selectorView.Dispose();
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audioHost);
            UnityEngine.Object.DestroyImmediate(_host);
            Directory.Delete(_root, true);
        }

        /// <summary>Starts at the native time, advances through actual input, and resumes the clock after the last entry.</summary>
        [Test]
        public void OpeningPausesForDialogueAndResumesAfterClick()
        {
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Assert.That(_game._adventureIntro.NativeTime, Is.EqualTo(2750));
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(201));
            Assert.That(_game._crazyDaveText, Is.EqualTo("First."));
            Assert.That(_game._crazyDaveAnimation, Is.EqualTo("anim_smalltalk"));
            Assert.That(_voice.clip, Is.SameAs(_clip));
            Tick(.5f);
            Assert.That(_game._adventureIntro.NativeTime, Is.EqualTo(2750));
            Click();
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(202));
            Assert.That(_game._crazyDaveText, Is.EqualTo("Second."));
            Click();
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game._crazyDaveAnimation, Is.EqualTo("anim_leave"));
            Assert.That(_voice.isPlaying, Is.False);
            Tick(.1f);
            Assert.That(_game._adventureIntro.NativeTime, Is.GreaterThan(2750));
        }

        /// <summary>Missing original voice data is reported and does not trap the opening.</summary>
        [Test]
        public void MissingVoiceStillAllowsDialogueToFinish()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Original Crazy Dave sound crazydaveshort[123] is missing"));
            Tick(2.75f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(201));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Original Crazy Dave sound crazydaveshort[123] is missing"));
            Click();
            Click();
            Tick(.1f);
            Assert.That(_game._adventureIntro.NativeTime, Is.GreaterThan(2750));
        }

        /// <summary>Decodes and plays each original short voice through the real dialogue coordinator.</summary>
        /// <returns>Frame boundaries for asynchronous original audio decoding.</returns>
        [UnityTest]
        public IEnumerator OriginalVoiceReachesAudioSource()
        {
            var sourceDirectory = Environment.GetEnvironmentVariable("PVZ_DAVE_AUDIO_ASSETS");
            if (string.IsNullOrEmpty(sourceDirectory))
            {
                Assert.Ignore("Set PVZ_DAVE_AUDIO_ASSETS to the original sounds directory.");
            }

            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            var loaded = new List<AudioClip>();
            try
            {
                for (var variant = 1; variant <= 3; variant++)
                {
                    var name = "crazydaveshort" + variant;
                    var path = Path.Combine(sourceDirectory!, name + ".ogg");
                    Assert.That(File.Exists(path), Is.True, path);
                    using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS);
                    yield return request.SendWebRequest();
                    Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    loaded.Add(clip);
                    var samples = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(samples, 0), Is.True);
                    Assert.That(Array.Exists(samples, sample => Math.Abs(sample) > .0001f), Is.True, name + " must contain audible data");
                    sounds[name] = clip;
                }

                Tick(2.75f);
                Assert.That(loaded, Does.Contain(_voice.clip));
                Assert.That(_voice.isPlaying, Is.True);
            }
            finally
            {
                _voice.Stop();
                foreach (var clip in loaded)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }
            }
        }

        /// <summary>Checks the level 31 mouth markers and the native blink timer during dialogue.</summary>
        [Test]
        public void LevelThirtyOneUpdatesMouthAndBlink()
        {
            var nativeRoot = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(nativeRoot, "reanim", "CrazyDave.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_801]\nFirst.\n[CRAZY_DAVE_802]\nSecond.{MOUTH_BIG_SMILE}\n[CRAZY_DAVE_803]\nThird.{MOUTH_SMALL_SMILE}\n");
            _game._animations.Dispose();
            _game._animations = new ReanimationRenderer(nativeRoot);
            SetField("_crazyDaveStartIndex", 801);
            SetField("_crazyDaveBlinkTicks", 1000);
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Click();
            Assert.That(_game._crazyDaveIdleMouth, Is.EqualTo("IMAGE_REANIM_CRAZYDAVE_MOUTH1"));
            Click();
            Assert.That(_game._crazyDaveIdleMouth, Is.EqualTo("IMAGE_REANIM_CRAZYDAVE_MOUTH5"));
            SetField("_crazyDaveBlinkTicks", 1);
            Tick(.01f);
            Assert.That(_game._crazyDaveBlinkAge, Is.EqualTo(0f));
            Tick(1f);
            Assert.That(_game._crazyDaveBlinkAge, Is.LessThan(0f));
        }

        /// <summary>Checks the Vasebreaker dialogue and non-scrolling handoff through the host opening loop.</summary>
        [Test]
        public void VasebreakerDialogueSkipsTheStreetPanAfterDeparture()
        {
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_2500]\nFirst.\n[CRAZY_DAVE_2501]\nSecond.\n");
            _game._adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 35,
                HasCrazyDaveDialog = true,
                ChooseSeedsOverride = false
            }));
            Assert.That(_game._adventureIntro.Plan.NonScrolling, Is.True);
            SetField("_crazyDaveStartIndex", 2500);
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2500));
            Click();
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2501));
            Click();
            Tick(1.26f);
            Assert.That(_game._adventureIntro.NativeTime, Is.GreaterThanOrEqualTo(_game._adventureIntro.Plan.PanLeftEnd));
        }

        /// <summary>Runs the real opening update with elapsed game time.</summary>
        /// <param name="seconds">The unpaused elapsed seconds.</param>
        private void Tick(float seconds)
        {
            typeof(PvZGame).GetMethod("UpdateAdventureOpening", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, new object[] { seconds });
        }

        /// <summary>Sends a primary-pointer press through the production input handler.</summary>
        private void Click()
        {
            var input = new GameInputSnapshot(new Vector2(400, 300), true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            typeof(PvZGame).GetMethod("HandleInput", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, null);
        }

        /// <summary>Supplies one private dependency of the inactive production coordinator.</summary>
        /// <param name="name">The coordinator field name.</param>
        /// <param name="value">The dependency value.</param>
        private void SetField(string name, object value)
        {
            typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, value);
        }
    }
}
