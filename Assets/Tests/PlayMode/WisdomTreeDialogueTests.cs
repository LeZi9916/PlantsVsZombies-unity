#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises Dave's first tree visit and durable original food grant.</summary>
    public sealed class WisdomTreeDialogueTests
    {
        /// <summary>Owns the test's isolated resources.</summary>
        private string _root = null!;
        /// <summary>Owns the inactive coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the voice source.</summary>
        private GameObject _audio = null!;
        /// <summary>Coordinates the actual tree view.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads the original-format text table.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides a playable voice sample.</summary>
        private AudioClip _clip = null!;
        /// <summary>Identifies the fixture's account.</summary>
        private Guid _profile;

        /// <summary>Creates a purchased tree whose original introduction has not yet been acknowledged.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "tree-dave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), "[CRAZY_DAVE_3200]\n{SHOW_TREE_FOOD}Hello {PLAYER_NAME}.\n");
            _host = new GameObject("Tree dialogue fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audio = new GameObject("Tree voice fixture");
            var voice = _audio.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audio.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Dave voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._animations = new ReanimationRenderer(_root);
            _game._selectorView = new SelectorView(_root);
            _game._profiles = new ProfileCatalog(Path.Combine(_root, "profiles.dat"));
            _profile = _game._profiles.Create("Tester").Id;
            _game._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            _game._accountStore.CreditBattleCoins(_profile, "tree", 2000);
            _game._accountStore.Purchase(_profile, 27, 51, 1, 1);
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", voice);
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydavelong" + variant] = _clip;
            }
        }

        /// <summary>Releases only resources belonging to this fixture.</summary>
        [TearDown]
        public void TearDown()
        {
            _game?._animations?.Dispose();
            _game?._selectorView?.Dispose();
            _presenter?.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audio);
            UnityEngine.Object.DestroyImmediate(_host);
            Directory.Delete(_root, true);
        }

        /// <summary>Checks the original talk, the acknowledgment grant, and no repeat after reload.</summary>
        [Test]
        public void FirstVisitGrantsFiveOnlyWhenDialogueIsAdvanced()
        {
            Assert.That(_game._accountStore.Get(_profile).PurchaseCount(28), Is.Zero);
            Invoke("OpenWisdomTree");
            Assert.That(_game._treeDaveIntroPending, Is.True);
            Invoke("UpdateWisdomTreeDialogue", 2.75f);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(3200));
            Assert.That(_game._crazyDaveText, Is.EqualTo("Hello Tester."));
            Assert.That(_game._crazyDaveHeldItem, Is.EqualTo("TREE_FOOD"));
            Assert.That(_game._accountStore.Get(_profile).PurchaseCount(28), Is.Zero);
            var input = new GameInputSnapshot(new Vector2(400, 300), true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            Invoke("HandleInput");
            var reloaded = new PlayerAccountStore(Path.Combine(_root, "fixture.account")).Get(_profile);
            Assert.That(reloaded.TreeFoodGiftClaimed, Is.True);
            Assert.That(reloaded.PurchaseCount(28), Is.EqualTo(5));
            Invoke("OpenWisdomTree");
            Assert.That(_game._treeDaveIntroPending, Is.False);
            Assert.That(_game._accountStore.Get(_profile).PurchaseCount(28), Is.EqualTo(5));
        }

        /// <summary>Sets one private fixture dependency.</summary>
        /// <param name="name">The coordinator field.</param>
        /// <param name="value">The supplied dependency.</param>
        private void SetField(string name, object value)
        {
            typeof(PvZGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, value);
        }

        /// <summary>Invokes a production coordinator method.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="arguments">The method arguments.</param>
        private void Invoke(string name, params object[] arguments)
        {
            typeof(PvZGame).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, arguments);
        }
    }
}
