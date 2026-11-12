#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises original opening dialogue for independent puzzle modes.</summary>
    public sealed class IndependentPuzzleDialogueTests
    {
        /// <summary>Owns the inactive game coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the dialogue audio source.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the production opening logic.</summary>
        private PvZGame _game = null!;
        /// <summary>Draws the production puzzle interface.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides a synthetic voice sample.</summary>
        private AudioClip _clip = null!;
        /// <summary>Contains original-format fixture strings.</summary>
        private string _root = null!;

        /// <summary>Creates an isolated game host and original-format dialogue table.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "puzzle-dialogue-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_2200]\nOne.\n[CRAZY_DAVE_2201]\nTwo.\n[CRAZY_DAVE_2202]\nThree.\n[CRAZY_DAVE_2203]\nFour.\n" +
                "[CRAZY_DAVE_3000]\nOne.\n[CRAZY_DAVE_3001]\nTwo.\n[CRAZY_DAVE_3002]\nThree.\n");
            _host = new GameObject("Independent puzzle dialogue fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Puzzle dialogue audio fixture");
            var voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Puzzle dialogue voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._currentView = PvZGame.ScreenView.Board;
            _game._animations = new ReanimationRenderer(_root);
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", voice);
            SetField("_soundVolume", .7f);
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
            }
        }

        /// <summary>Releases fixture resources and the temporary string table.</summary>
        [TearDown]
        public void TearDown()
        {
            _game._animations.Dispose();
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audioHost);
            UnityEngine.Object.DestroyImmediate(_host);
            Directory.Delete(_root, true);
        }

        /// <summary>Checks the original mode and completion conditions at the actual opening entry point.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="firstClear">Whether the level has never been beaten.</param>
        /// <param name="expected">The first original message, or -1 when no opening is shown.</param>
        [TestCase(51, true, 3000)]
        [TestCase(51, false, -1)]
        [TestCase(52, true, -1)]
        [TestCase(61, true, 2200)]
        [TestCase(61, false, 2200)]
        [TestCase(62, true, -1)]
        [TestCase(17, true, -1)]
        [TestCase(33, true, -1)]
        public void SelectsOnlyOriginalPuzzleOpenings(int mode, bool firstClear, int expected)
        {
            StartOpening(mode, firstClear);
            if (expected < 0)
            {
                Assert.That(_game._adventureIntro, Is.Null);
                return;
            }

            Assert.That(_game._adventureIntro, Is.Not.Null);
            Assert.That(_game._adventureIntro.Plan.NonScrolling, Is.True);
            Assert.That(_game._adventureIntro.Plan.ChooseSeeds, Is.False);
            Assert.That(GetField<int>("_crazyDaveStartIndex"), Is.EqualTo(expected));
        }

        /// <summary>Checks that rendering starts the clock and every original entry blocks the mode until dismissed.</summary>
        /// <param name="mode">The original puzzle mode identifier.</param>
        /// <param name="first">The first original dialogue index.</param>
        /// <param name="last">The last original dialogue index.</param>
        [TestCase(51, 3000, 3002)]
        [TestCase(61, 2200, 2203)]
        public void PuzzleDialogueRunsThroughOpeningAndResumes(int mode, int first, int last)
        {
            StartOpening(mode, true);
            Assert.That(Invoke(_presenter, "DrawModesInterface"), Is.True);
            Assert.That(_game._adventureIntroDrawn, Is.True);
            Assert.That(Invoke(_game, "TryHandleModeClick", new Vector2(400f, 300f)), Is.True);
            Tick(2.75f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(first));
            var frozenTime = _game._adventureIntro.NativeTime;
            Tick(.2f);
            Assert.That(_game._adventureIntro.NativeTime, Is.EqualTo(frozenTime));
            for (var index = first + 1; index <= last; index++)
            {
                Invoke(_game, "AdvanceCrazyDaveDialogue");
                Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(index));
            }

            Invoke(_game, "AdvanceCrazyDaveDialogue");
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(-1));
            Tick(1.26f);
            Assert.That(_game._adventureIntro.NativeTime, Is.GreaterThanOrEqualTo(_game._adventureIntro.Plan.PanLeftEnd));
        }

        /// <summary>Creates a mode and invokes its real opening initialization.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="firstClear">Whether the puzzle has not been completed.</param>
        private void StartOpening(int mode, bool firstClear)
        {
            _game._modeSession = new ModeSession(mode);
            _game._game = _game._modeSession.Game;
            SetField("_modeFirstClear", firstClear);
            Invoke(_game, "InitializeAdventureOpening");
        }

        /// <summary>Advances the production opening clock.</summary>
        /// <param name="seconds">The elapsed time in seconds.</param>
        private void Tick(float seconds)
        {
            Invoke(_game, "UpdateAdventureOpening", seconds);
        }

        /// <summary>Sets a private game dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        private void SetField(string name, object value)
        {
            typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, value);
        }

        /// <summary>Reads a private game field.</summary>
        /// <typeparam name="T">The expected field type.</typeparam>
        /// <param name="name">The field name.</param>
        /// <returns>The field value.</returns>
        private T GetField<T>(string name)
        {
            return (T)typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
        }

        /// <summary>Invokes a private method on the production object.</summary>
        /// <param name="target">The production object.</param>
        /// <param name="name">The method name.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The method's result, if any.</returns>
        private static object? Invoke(object target, string name, params object[] arguments)
        {
            return target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(target, arguments);
        }
    }
}
