#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using PlantsVsZombies.Diagnostics;

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
            _game.Opening._adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = 11, HasCrazyDaveDialog = true }));
            _game.Opening._adventureIntroDrawn = true;
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
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.EqualTo(2750));
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(201));
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("First."));
            Assert.That(_game.Dave._crazyDaveAnimation, Is.EqualTo("anim_smalltalk"));
            Assert.That(_voice.clip, Is.SameAs(_clip));
            Tick(.5f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.EqualTo(2750));
            Click();
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(202));
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("Second."));
            Click();
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game.Dave._crazyDaveAnimation, Is.EqualTo("anim_leave"));
            Assert.That(_voice.isPlaying, Is.False);
            Tick(.1f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.GreaterThan(2750));
        }

        /// <summary>Missing original voice data is reported and does not trap the opening.</summary>
        [Test]
        public void MissingVoiceStillAllowsDialogueToFinish()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Original Crazy Dave sound crazydaveshort[123] is missing"));
            Tick(2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(201));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Original Crazy Dave sound crazydaveshort[123] is missing"));
            Click();
            Click();
            Tick(.1f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.GreaterThan(2750));
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

            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
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
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Click();
            Assert.That(_game.Dave._crazyDaveIdleMouth, Is.EqualTo("IMAGE_REANIM_CRAZYDAVE_MOUTH1"));
            Click();
            Assert.That(_game.Dave._crazyDaveIdleMouth, Is.EqualTo("IMAGE_REANIM_CRAZYDAVE_MOUTH5"));
            SetField("_crazyDaveBlinkTicks", 1);
            Tick(.01f);
            Assert.That(_game.Dave._crazyDaveBlinkAge, Is.EqualTo(0f));
            Tick(1f);
            Assert.That(_game.Dave._crazyDaveBlinkAge, Is.LessThan(0f));
        }

        /// <summary>Checks the Vasebreaker dialogue and non-scrolling handoff through the host opening loop.</summary>
        [Test]
        public void VasebreakerDialogueSkipsTheStreetPanAfterDeparture()
        {
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_2500]\nFirst.\n[CRAZY_DAVE_2501]\nSecond.\n");
            _game.Opening._adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 35,
                HasCrazyDaveDialog = true,
                ChooseSeedsOverride = false
            }));
            Assert.That(_game.Opening._adventureIntro.Plan.NonScrolling, Is.True);
            SetField("_crazyDaveStartIndex", 2500);
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Tick(2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2500));
            Click();
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2501));
            Click();
            Tick(1.26f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.GreaterThanOrEqualTo(_game.Opening._adventureIntro.Plan.PanLeftEnd));
        }

        /// <summary>Checks the production level 2-5 opening and the original grave reveal.</summary>
        [Test]
        public void WhackOpeningShowsOriginalConversationBeforeRevealingGraves()
        {
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_401]\nOne.\n[CRAZY_DAVE_402]\nTwo.\n[CRAZY_DAVE_403]\nThree.\n" +
                "[CRAZY_DAVE_404]\nFour.\n[CRAZY_DAVE_405]\n{SHOW_HAMMER}Five.\n[CRAZY_DAVE_406]\nSix.\n");
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 15 }, 1);
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            foreach (var prefix in new[] { "crazydaveshort", "crazydavelong", "crazydaveextralong" })
            {
                for (var variant = 1; variant <= 3; variant++)
                {
                    sounds[prefix + variant] = _clip;
                }
            }

            GameMemberReflection.Invoke(_game, "InitializeAdventureOpening", null);
            Assert.That(_game.Opening._adventureIntro.Plan.NonScrolling, Is.True);
            Assert.That(_game._game.Adventure.GravesEnabled, Is.False);
            _game.Opening._adventureIntroDrawn = true;
            Tick(2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(401));
            for (var expected = 402; expected <= 405; expected++)
            {
                Click();
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(expected));
                Assert.That(_game._game.Adventure.GravesEnabled, Is.False);
            }

            Assert.That(_game.Dave._crazyDaveHeldItem, Is.EqualTo("HAMMER"));
            Click();
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(406));
            Assert.That(_game.Dave._crazyDaveHeldItem, Is.Null);
            Assert.That(_game._game.Adventure.GravesEnabled, Is.True);
            Click();
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            Tick(1.26f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.GreaterThanOrEqualTo(_game.Opening._adventureIntro.Plan.PanLeftEnd));
        }

        /// <summary>Checks both Vasebreaker conversations through the production host and input routing.</summary>
        [Test]
        public void VaseStagesPopulateAtTheirOriginalDialogueEntries()
        {
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_2700]\nFirst.\n[CRAZY_DAVE_2701]\nSecond.\n[CRAZY_DAVE_2702]\nThird.\n" +
                "[CRAZY_DAVE_2703]\nFourth.\n[CRAZY_DAVE_2704]\nFifth.\n" +
                "[CRAZY_DAVE_2800]\nFirst.\n[CRAZY_DAVE_2801]\nSecond.\n[CRAZY_DAVE_2802]\nThird.\n");
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 1);
            _game._game.Adventure.VaseInterludeDialogueEnabled = true;
            var notice = GameMemberReflection.BindMethod(_game, "AdventureNotice")!;
            _game._game.Adventure.NoticeRaised = item => notice.Invoke(_game, new object[] { item });
            _game.Opening._adventureIntro = null;
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
            }

            for (var stage = 0; stage < 2; stage++)
            {
                var start = stage == 0 ? 2700 : 2800;
                var populate = stage == 0 ? 2702 : 2801;
                var end = stage == 0 ? 2704 : 2802;
                _game._game.Adventure._vases.Clear();
                _game._game.Update(2.01f);
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(start));
                Assert.That(_game._game.Adventure.Vases, Is.Empty);
                for (var index = start + 1; index <= end; index++)
                {
                    Click();
                    Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(index));
                    Assert.That(_game._game.Adventure.Vases.Count > 0, Is.EqualTo(index >= populate));
                }

                Click();
                Assert.That(_game._game.Adventure.VaseInterludeActive, Is.True);
                GameMemberReflection.Invoke(_game, "UpdateVaseInterlude", new object[] { 1f });
                Assert.That(_game._game.Adventure.VaseInterludeActive, Is.False);
                Assert.That(_game.Opening._vaseDialogueActive, Is.False);
            }
        }

        /// <summary>Checks that the native handing marker preserves an existing item until normal speech resumes.</summary>
        [Test]
        public void HandingContinuationKeepsTheHammerUntilOrdinarySpeech()
        {
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_405]\n{SHOW_HAMMER}Hammer.\n[CRAZY_DAVE_406]\n{HANDING}Keep.\n[CRAZY_DAVE_407]\nDone.\n");
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydavelong" + variant] = _clip;
                sounds["crazydaveshort" + variant] = _clip;
            }

            var show = GameMemberReflection.BindMethod(_game, "ShowCrazyDaveMessage")!;
            show.Invoke(_game, new object[] { 405 });
            Assert.That(_game.Dave._crazyDaveHeldItem, Is.EqualTo("HAMMER"));
            show.Invoke(_game, new object[] { 406 });
            Assert.That(_game.Dave._crazyDaveHeldItem, Is.EqualTo("HAMMER"));
            show.Invoke(_game, new object[] { 407 });
            Assert.That(_game.Dave._crazyDaveHeldItem, Is.Null);
        }

        /// <summary>Checks the final-boss conversation, abduction, music transition, and restart condition.</summary>
        [Test]
        public void FinalBossOpeningPlaysDialogueAndSkipsItAfterRestart()
        {
            var strings = new System.Text.StringBuilder();
            for (var index = 2300; index <= 2310; index++)
            {
                strings.Append("[CRAZY_DAVE_").Append(index).Append("]\nText.\n");
            }

            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), strings.ToString());
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 50 }, 1);
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
            }

            var initialize = GameMemberReflection.BindMethod(_game, "InitializeAdventureOpening")!;
            var music = GameMemberReflection.BindMethod(_game, "ResolveOriginalMusicCue")!;
            initialize.Invoke(_game, null);
            Assert.That(_game.Opening._adventureIntro.Plan.DaveTime, Is.EqualTo(6500));
            Assert.That(music.Invoke(_game, null), Is.EqualTo(MusicCue.Title));
            _game.Opening._adventureIntroDrawn = true;
            Tick(2.75f);
            for (var index = 2300; index <= 2310; index++)
            {
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(index));
                Click();
            }

            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game.Dave._crazyDaveAnimation, Is.EqualTo("anim_grab"));
            Tick(.51f);
            Assert.That(music.Invoke(_game, null), Is.EqualTo(MusicCue.None));
            SetField("_adventureOpeningIsRestart", true);
            initialize.Invoke(_game, null);
            Assert.That(_game.Opening._adventureIntro.Plan.DaveTime, Is.Zero);
            Assert.That(music.Invoke(_game, null), Is.EqualTo(MusicCue.None));
        }

        /// <summary>Runs the real opening update with elapsed game time.</summary>
        /// <param name="seconds">The unpaused elapsed seconds.</param>
        private void Tick(float seconds)
        {
            GameMemberReflection.Invoke(_game, "UpdateAdventureOpening", new object[] { seconds });
        }

        /// <summary>Sends a primary-pointer press through the production input handler.</summary>
        private void Click()
        {
            var input = new GameInputSnapshot(new Vector2(400, 300), true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            GameMemberReflection.Invoke(_game, "HandleInput", null);
        }

        /// <summary>Supplies one private dependency of the inactive production coordinator.</summary>
        /// <param name="name">The coordinator field name.</param>
        /// <param name="value">The dependency value.</param>
        private void SetField(string name, object value)
        {
            GameMemberReflection.SetField(_game, name, value);
        }
    }
}
