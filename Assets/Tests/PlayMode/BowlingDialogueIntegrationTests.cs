#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using PlantsVsZombies.Diagnostics;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises the original adventure bowling opening and shovel lesson together.</summary>
    public sealed class BowlingDialogueIntegrationTests
    {
        /// <summary>Owns the inactive coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the dialogue audio source.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the production opening logic.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads the original-format strings.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides a short synthetic voice clip.</summary>
        private AudioClip _clip = null!;
        /// <summary>Owns the fixture string table.</summary>
        private string _root = null!;

        /// <summary>Creates an isolated production coordinator and synthetic original-format dialogue.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "bowling-dialogue-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            using (var writer = new StreamWriter(Path.Combine(_root, "properties", "LawnStrings.txt")))
            {
                for (var index = 2400; index <= 2406; index++)
                {
                    writer.WriteLine("[CRAZY_DAVE_" + index + "]");
                    writer.WriteLine("First " + index + ".");
                }

                for (var index = 2410; index <= 2415; index++)
                {
                    writer.WriteLine("[CRAZY_DAVE_" + index + "]");
                    writer.WriteLine(index == 2412 ? "{SHOW_WALLNUT}Nut." : "Second " + index + ".");
                }
            }

            _host = new GameObject("Bowling dialogue fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Bowling dialogue voice");
            var voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Bowling Dave voice", 44100, 1, 44100, false);
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
            var sounds = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(_game, "_presentationSounds")!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
                sounds["crazydavelong" + variant] = _clip;
            }

            sounds["crazydavescream2"] = _clip;
        }

        /// <summary>Releases the test objects, voice, and fixture strings.</summary>
        [TearDown]
        public void TearDown()
        {
            _game.RequiredAnimations.Dispose();
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audioHost);
            UnityEngine.Object.DestroyImmediate(_host);
            Directory.Delete(_root, true);
        }

        /// <summary>Requires all three plants to be shoveled before Dave returns with the bowling conversation.</summary>
        [Test]
        public void FirstAdventurePausesForShovelAndResumesAtOriginalMessage()
        {
            StartOpening(1);
            Assert.That(_game.Opening._bowlingLineVisible, Is.False);
            Tick(2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2400));
            for (var index = 2401; index <= 2406; index++)
            {
                Invoke("AdvanceCrazyDaveDialogue");
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(index));
            }

            Invoke("AdvanceCrazyDaveDialogue");
            Assert.That(_game.Opening._bowlingShovelLessonActive, Is.True);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            var pausedTime = _game.Opening.RequiredAdventureIntro.NativeTime;
            Tick(1f);
            Assert.That(_game.Opening.RequiredAdventureIntro.NativeTime, Is.EqualTo(pausedTime));
            Assert.That(_game.Dave._crazyDaveAnimation, Is.Null);
            Assert.That(_game.RequiredGame.Plants.Count, Is.EqualTo(3));
            for (var column = 3; column <= 5; column++)
            {
                Invoke("HandleAdventureClick", new Vector2(620, 30));
                Assert.That(_game._shovel, Is.True);
                Invoke("HandleAdventureClick", new Vector2(80 + 80 * column, 320));
                Assert.That(_game.RequiredGame.Plants.Count, Is.EqualTo(5 - column));
            }

            Assert.That(_game.Opening._bowlingShovelLessonActive, Is.False);
            Assert.That(_game.Opening.RequiredAdventureIntro.NativeTime, Is.EqualTo(1500));
            Tick(1.25f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2410));
            Assert.That(_game.Opening._bowlingLineVisible, Is.False);
            Invoke("AdvanceCrazyDaveDialogue");
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2411));
            Assert.That(_game.Opening._bowlingLineVisible, Is.True);
            for (var index = 2412; index <= 2415; index++)
            {
                Invoke("AdvanceCrazyDaveDialogue");
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(index));
            }

            Invoke("AdvanceCrazyDaveDialogue");
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            Tick(1.26f);
            Assert.That(_game.Opening.RequiredAdventureIntro.NativeTime, Is.GreaterThanOrEqualTo(_game.Opening.RequiredAdventureIntro.Plan.PanLeftEnd));
        }

        /// <summary>Starts the replay at the original second conversation without the shovel lesson.</summary>
        [Test]
        public void ReplayStartsAtBowlingIntroduction()
        {
            StartOpening(2);
            Assert.That(_game.RequiredGame.Plants.Count, Is.Zero);
            Assert.That(_game.Opening._bowlingLineVisible, Is.True);
            Tick(2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2411));
            Assert.That(_game.Opening._bowlingShovelLessonActive, Is.False);
        }

        /// <summary>Creates an adventure attempt and initializes its real opening.</summary>
        /// <param name="cycle">The one-based adventure run.</param>
        private void StartOpening(int cycle)
        {
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 5, AdventureCycle = cycle }, 1);
            Invoke("InitializeAdventureOpening");
            _game.Opening._adventureIntroDrawn = true;
        }

        /// <summary>Advances the real opening by elapsed seconds.</summary>
        /// <param name="seconds">The elapsed time.</param>
        private void Tick(float seconds)
        {
            Invoke("UpdateAdventureOpening", seconds);
        }

        /// <summary>Sets a private production dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The dependency value.</param>
        private void SetField(string name, object value)
        {
            GameMemberReflection.SetField(_game, name, value);
        }

        /// <summary>Invokes an actual game coordinator method.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The method result, if any.</returns>
        private object? Invoke(string name, params object[] arguments)
        {
            return GameMemberReflection.Invoke(_game, name, arguments);
        }
    }
}
