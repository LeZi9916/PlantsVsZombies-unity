#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using PlantsVsZombies.Diagnostics;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises Dave's original Zen Garden sale offer and confirmation.</summary>
    public sealed class GardenSaleDialogueTests
    {
        /// <summary>Owns the isolated fixture directory.</summary>
        private string _root = null!;
        /// <summary>Owns the inactive coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the voice source.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the actual garden input.</summary>
        private PvZGame _game = null!;
        /// <summary>Resolves the fixture's original-format strings.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides a synthetic speech clip.</summary>
        private AudioClip _clip = null!;
        /// <summary>Identifies the fixture account.</summary>
        private Guid _profile;
        /// <summary>Identifies the plant offered for sale.</summary>
        private Guid _plant;

        /// <summary>Creates isolated account, UI, speech and garden fixtures.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "garden-sale-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_1700]\n{NO_CLICK}Offer ${SELL_PRICE} for {PLANT_TYPE}.\n[MARIGOLD_SPROUT]\nSprout\n[MARIGOLD]\nMarigold\n[PEASHOOTER]\nPeashooter\n");
            _host = new GameObject("Inactive garden sale fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Garden sale voice");
            var voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Garden sale voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._currentView = PvZGame.ScreenView.Garden;
            _game._animations = new ReanimationRenderer(_root);
            _game._selectorView = new SelectorView(_root);
            _game._profiles = new ProfileCatalog(Path.Combine(_root, "profiles.dat"));
            _profile = _game._profiles.Create("Tester").Id;
            _game.Accounts._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            _plant = Guid.NewGuid();
            _game.Accounts._accountStore.Import(_profile, 10, new int[80], new int[100], 0,
                new[] { new GardenPlant { Id = _plant, Kind = PlantKind.Marigold, Growth = 0, Garden = GardenKind.Greenhouse, Slot = 0 } });
            _game.Garden._gardenKind = GardenKind.Greenhouse;
            _game.Garden._gardenTool = GardenTool.Sell;
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", voice);
            var sounds = (Dictionary<string, AudioClip>)GetField("_presentationSounds");
            foreach (var prefix in new[] { "crazydaveshort", "crazydavelong", "crazydaveextralong" })
            {
                for (var variant = 1; variant <= 3; variant++)
                {
                    sounds[prefix + variant] = _clip;
                }
            }
        }

        /// <summary>Releases only the isolated fixture resources.</summary>
        [TearDown]
        public void TearDown()
        {
            _game._animations.Dispose();
            _game._selectorView.Dispose();
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audioHost);
            UnityEngine.Object.DestroyImmediate(_host);
            Directory.Delete(_root, true);
        }

        /// <summary>Checks that a sale is only committed after the affirmative choice.</summary>
        /// <param name="accept">Whether to accept the offer.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void SaleWaitsForConfirmationAndUsesOriginalSubstitutions(bool accept)
        {
            var plant = _game.Accounts._accountStore.Get(_profile).Plants[0];
            Invoke("HandleProgressionClick", _game.Garden.GardenPlantRect(plant).center);
            Assert.That(_game.Garden._gardenSalePlant, Is.EqualTo(_plant));
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(1700));
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("Offer $150 for Sprout."));
            Assert.That(_game.Accounts._accountStore.Get(_profile).Plants.Count, Is.EqualTo(1));
            Assert.That(_game.Accounts._accountStore.Get(_profile).Coins, Is.EqualTo(10));
            var rect = accept ? ZenGardenController.s_gardenSaleYes : ZenGardenController.s_gardenSaleNo;
            var input = new GameInputSnapshot(rect.center, true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            Invoke("HandleInput");
            var restored = new PlayerAccountStore(Path.Combine(_root, "fixture.account")).Get(_profile);
            Assert.That(restored.Plants.Count, Is.EqualTo(accept ? 0 : 1));
            Assert.That(restored.Coins, Is.EqualTo(accept ? 160 : 10));
            Assert.That(_game.Garden._gardenSalePlant, Is.Null);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
        }

        /// <summary>Checks that non-marigold plants use their own localized name and native price.</summary>
        [Test]
        public void MaturePlantUsesItsLocalizedNameAndNativePrice()
        {
            _game.Accounts._accountStore.Import(_profile, 10, new int[80], new int[100], 0,
                new[] { new GardenPlant { Id = _plant, Kind = PlantKind.Peashooter, Growth = 3, Garden = GardenKind.Greenhouse, Slot = 0 } });
            var plant = _game.Accounts._accountStore.Get(_profile).Plants[0];
            Invoke("HandleProgressionClick", _game.Garden.GardenPlantRect(plant).center);
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("Offer $800 for Peashooter."));
            Assert.That(_game.Accounts._accountStore.Get(_profile).Plants.Count, Is.EqualTo(1));
        }

        /// <summary>Reads a private production dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>The field value.</returns>
        private object GetField(string name)
        {
            return GameMemberReflection.GetField(_game, name)!;
        }

        /// <summary>Sets a private production dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        private void SetField(string name, object value)
        {
            GameMemberReflection.SetField(_game, name, value);
        }

        /// <summary>Invokes production behavior on the fixture.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="args">The method arguments.</param>
        private void Invoke(string name, params object[] args)
        {
            GameMemberReflection.Invoke(_game, name, args);
        }
    }
}
