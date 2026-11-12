#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
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
    /// <summary>Exercises the original packet offer through the production opening and account store.</summary>
    public sealed class PacketUpgradeDialogueTests
    {
        /// <summary>Owns the test directory.</summary>
        private string _root = null!;
        /// <summary>Owns the inactive coordinator.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the voice source.</summary>
        private GameObject _audioHost = null!;
        /// <summary>Runs the production coordinator.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads the fixture's dialogue.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides the voice sample.</summary>
        private AudioClip _clip = null!;
        /// <summary>Identifies the fixture profile.</summary>
        private Guid _profile;

        /// <summary>Creates an isolated original-format string table and account.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "packet-offer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            using (var writer = new StreamWriter(Path.Combine(_root, "properties", "LawnStrings.txt")))
            {
                foreach (var index in new[] { 1501, 1502, 1503, 1510, 1511, 1520, 1551, 1552, 1553, 1560, 1570 })
                {
                    writer.WriteLine("[CRAZY_DAVE_" + index + "]");
                    writer.WriteLine("Message " + index);
                }
            }

            _host = new GameObject("Packet offer fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audioHost = new GameObject("Packet offer audio");
            var voice = _audioHost.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audioHost.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Packet offer voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._currentView = PvZGame.ScreenView.Board;
            _game._animations = new ReanimationRenderer(_root);
            _game._selectorView = new SelectorView(_root);
            _game._profiles = new ProfileCatalog(Path.Combine(_root, "profiles.dat"));
            _profile = _game._profiles.Create("Tester").Id;
            _game.Accounts._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            SetField("_presenter", _presenter);
            SetField("_crazyDaveVoice", voice);
            var sounds = (Dictionary<string, AudioClip>)GetField("_presentationSounds");
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
                sounds["crazydavelong" + variant] = _clip;
            }
        }

        /// <summary>Releases the fixture and its files.</summary>
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

        /// <summary>Checks both original offers and their accept and refuse branches.</summary>
        /// <param name="purchases">Previously purchased packet upgrades.</param>
        /// <param name="accepted">Whether the player accepts the offer.</param>
        /// <param name="start">The original starting entry.</param>
        /// <param name="response">The original response entry.</param>
        [TestCase(0, true, 1501, 1510)]
        [TestCase(0, false, 1501, 1520)]
        [TestCase(1, true, 1551, 1560)]
        [TestCase(1, false, 1551, 1570)]
        public void OffersCorrectBranchAndPersistsChoice(int purchases, bool accepted, int start, int response)
        {
            var values = new int[80];
            values[21] = purchases;
            _game.Accounts._accountStore.Import(_profile, 1000, values, new int[100], 0, Array.Empty<GardenPlant>());
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 16, AdventurePacketSlots = 6 + purchases }, 1);
            Invoke("InitializeAdventureOpening");
            _game.Opening._adventureIntroDrawn = true;
            Invoke("UpdateAdventureOpening", 2.75f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(start));
            Invoke("AdvanceCrazyDaveDialogue");
            Invoke("AdvanceCrazyDaveDialogue");
            Assert.That(_game.Opening._packetUpgradeOfferActive, Is.True);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(start + 2));
            var before = _game.Opening._adventureIntro.NativeTime;
            Invoke("UpdateAdventureOpening", .5f);
            Assert.That(_game.Opening._adventureIntro.NativeTime, Is.EqualTo(before));
            var rect = accepted ? AdventureOpeningController.s_packetOfferYes : AdventureOpeningController.s_packetOfferNo;
            var input = new GameInputSnapshot(rect.center, true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { input });
            Invoke("HandleInput");
            Assert.That(_game.Opening._packetUpgradeOfferActive, Is.False);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(response));
            var account = new PlayerAccountStore(Path.Combine(_root, "fixture.account")).Get(_profile);
            Assert.That(account.PurchaseCount(21), Is.EqualTo(purchases + (accepted ? 1 : 0)));
            Assert.That(account.PacketUpgradeDeclines, Is.EqualTo(accepted ? 0 : 1));
            Assert.That(account.Coins, Is.EqualTo(1000 - (accepted ? purchases == 0 ? 75 : 500 : 0)));
            Assert.That(_game._game.Adventure.Level.PacketSlots, Is.EqualTo(6 + purchases + (accepted ? 1 : 0)));
        }

        /// <summary>Checks the original eligibility restrictions suppress an offer.</summary>
        /// <param name="coins">The wallet balance in original coin units.</param>
        /// <param name="declines">The persisted number of declined offers.</param>
        /// <param name="purchases">The number of earlier upgrades.</param>
        [TestCase(74, 0, 0)]
        [TestCase(1000, 2, 0)]
        [TestCase(1000, 0, 2)]
        public void IneligibleAccountsDoNotStartOffer(int coins, int declines, int purchases)
        {
            var values = new int[80];
            values[21] = purchases;
            _game.Accounts._accountStore.Import(_profile, coins, values, new int[100], 0, Array.Empty<GardenPlant>(), packetUpgradeDeclines: declines);
            _game._game = new GameSimulation(new GameRules { AdventureLevel = 16, AdventurePacketSlots = 6 + purchases }, 1);
            Invoke("InitializeAdventureOpening");
            Assert.That((int)GetField("_crazyDaveStartIndex"), Is.EqualTo(-1));
            Assert.That(_game.Opening._packetUpgradeOfferActive, Is.False);
        }

        /// <summary>Reads a private fixture dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>The field value.</returns>
        private object GetField(string name)
        {
            return GameMemberReflection.GetField(_game, name)!;
        }

        /// <summary>Sets a private fixture dependency.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        private void SetField(string name, object value)
        {
            GameMemberReflection.SetField(_game, name, value);
        }

        /// <summary>Invokes production behavior on the fixture coordinator.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="args">The arguments.</param>
        private void Invoke(string name, params object[] args)
        {
            GameMemberReflection.Invoke(_game, name, args);
        }
    }
}
