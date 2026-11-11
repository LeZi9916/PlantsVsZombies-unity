#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises store hover, original dialogue selection, and voice playback together.</summary>
    public sealed class CrazyDaveStoreIntegrationTests
    {
        /// <summary>Owns the inactive production coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the real audio source used by the coordinator.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the production store presentation.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads the fixture's original-format strings.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives the selected original voice.</summary>
        private AudioSource _voice = null!;
        /// <summary>Provides a playable voice sample.</summary>
        private AudioClip _clip = null!;
        /// <summary>Owns fixture resources and account files.</summary>
        private string _root = null!;

        /// <summary>Creates an isolated unlocked store with a playable voice fixture.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "dave-store-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), "[CRAZY_DAVE_2000]\nFirst.\n[CRAZY_DAVE_2001]\n{NO_SOUND}{NO_CLICK}Second.\n");
            _host = new GameObject("Inactive Dave store fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Dave store audio fixture");
            _voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Dave store voice", 441000, 1, 44100, false);
            _game._root = _root;
            _game._animations = new ReanimationRenderer(_root);
            _game._profiles = new ProfileCatalog(Path.Combine(_root, "profiles.dat"));
            var profile = _game._profiles.Create("Tester");
            _game._adventureStore = new AdventureProgressStore(Path.Combine(_root, "fixture.adventure"));
            _game._adventureStore.Import(profile.Id, 25, 0);
            _game._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", _voice);
            SetField("_soundVolume", .7f);
            var sounds = (Dictionary<string, AudioClip>)GetField("_presentationSounds");
            sounds["crazydaveshort1"] = _clip;
            sounds["crazydaveshort2"] = _clip;
            sounds["crazydaveshort3"] = _clip;
            Invoke("OpenStore");
            _game._crazyDaveAnimation = "anim_idle";
        }

        /// <summary>Releases fixture objects and deletes only fixture-owned files.</summary>
        [TearDown]
        public void TearDown()
        {
            if (_voice != null)
            {
                _voice.Stop();
            }

            _game?._animations?.Dispose();
            _presenter?.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audioHost);
            UnityEngine.Object.DestroyImmediate(_host);
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        /// <summary>Hovering an item starts a voice, and a silent description preserves it until the bubble can close.</summary>
        [Test]
        public void HoverStartsVoiceAndSilentDescriptionPreservesIt()
        {
            SetPointer(new Vector2(373, 311));
            Tick(1.21f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2000));
            Assert.That(_game._crazyDaveText, Is.EqualTo("First."));
            Assert.That(_voice.clip, Is.SameAs(_clip));
            Assert.That(_voice.isPlaying, Is.True);

            SetPointer(new Vector2(447, 311));
            Tick(.02f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2001));
            Assert.That(_game._crazyDaveText, Is.EqualTo("Second."));
            Assert.That(_voice.isPlaying, Is.True);

            SetPointer(new Vector2(100, 500));
            Tick(1.1f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2001));
            _voice.Stop();
            Tick(.02f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game._crazyDaveAnimation, Is.EqualTo("anim_idle"));
        }

        /// <summary>Supplies a pointer snapshot to the real store hover calculation.</summary>
        /// <param name="point">The pointer position in the original viewport.</param>
        private void SetPointer(Vector2 point)
        {
            var input = new GameInputSnapshot(point, true, false, false, false, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            SetField("_adventurePointer", point);
        }

        /// <summary>Advances the real store speech coordinator.</summary>
        /// <param name="seconds">Elapsed unscaled time.</param>
        private void Tick(float seconds)
        {
            Invoke("UpdateCrazyDaveStore", seconds);
        }

        /// <summary>Runs a private production method with its supplied arguments.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="arguments">Arguments passed to the method.</param>
        private void Invoke(string name, params object[] arguments)
        {
            typeof(PvZGame).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, arguments);
        }

        /// <summary>Reads a private production field.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>The field value.</returns>
        private object GetField(string name)
        {
            return typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
        }

        /// <summary>Sets a private production dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        private void SetField(string name, object value)
        {
            typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, value);
        }
    }
}
