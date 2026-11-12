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
using System.Runtime.Serialization;
using UnityEngine;
using PlantsVsZombies.Diagnostics;

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
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[CRAZY_DAVE_2000]\nFirst.\n[CRAZY_DAVE_2001]\n{NO_SOUND}{NO_CLICK}Second.\n" +
                "[CRAZY_DAVE_301]\nOne.\n[CRAZY_DAVE_302]\nTwo.\n[CRAZY_DAVE_303]\nThree.\n[CRAZY_DAVE_304]\nFour.\n" +
                "[CRAZY_DAVE_601]\nOne.\n[CRAZY_DAVE_602]\nTwo.\n[CRAZY_DAVE_603]\nThree.\n[CRAZY_DAVE_604]\nFour.\n[CRAZY_DAVE_605]\nFive.\n[CRAZY_DAVE_606]\nSix.\n" +
                "[CRAZY_DAVE_3100]\nOne.\n[CRAZY_DAVE_2500]\nOne.\n");
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
            _game.Campaign._adventureStore = new AdventureProgressStore(Path.Combine(_root, "fixture.adventure"));
            _game.Campaign._adventureStore.Import(profile.Id, 25, 0);
            _game.Accounts._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
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
            _game.Dave._crazyDaveAnimation = "anim_idle";
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
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2000));
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("First."));
            Assert.That(_voice.clip, Is.SameAs(_clip));
            Assert.That(_voice.isPlaying, Is.True);

            SetPointer(new Vector2(447, 311));
            Tick(.02f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2001));
            Assert.That(_game.Dave._crazyDaveText, Is.EqualTo("Second."));
            Assert.That(_voice.isPlaying, Is.True);

            SetPointer(new Vector2(100, 500));
            Tick(1.1f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(2001));
            _voice.Stop();
            Tick(.02f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game.Dave._crazyDaveAnimation, Is.EqualTo("anim_idle"));
        }

        /// <summary>Checks the original reward screen's first-adventure store interludes.</summary>
        /// <param name="level">The level whose reward was collected.</param>
        /// <param name="replay">Whether this is a later adventure.</param>
        /// <param name="expected">The first original message index, or -1.</param>
        [TestCase(24, false, 301)]
        [TestCase(34, false, 601)]
        [TestCase(41, false, 3100)]
        [TestCase(24, true, -1)]
        [TestCase(34, true, -1)]
        [TestCase(41, true, -1)]
        [TestCase(23, false, -1)]
        public void SelectsRewardStoreIntroduction(int level, bool replay, int expected)
        {
            Assert.That(CrazyDaveStoreDialog.AdventureRewardIntroduction(level, replay), Is.EqualTo(expected));
        }

        /// <summary>Checks click-controlled original reward introductions without hover interruption.</summary>
        /// <param name="first">The first original message index.</param>
        /// <param name="last">The last original message index.</param>
        [TestCase(301, 304)]
        [TestCase(601, 606)]
        [TestCase(3100, 3100)]
        public void RewardIntroductionAdvancesOnlyOnClick(int first, int last)
        {
            var id = _game._profiles.SelectedId!.Value;
            if (first == 601)
            {
                _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            }

            Invoke("OpenStoreForAdventureIntro", first);
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(.02f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(first));
            Assert.That(_game.Store._storeHatchOpen, Is.False);
            SetPointer(new Vector2(373, 311));
            Tick(3f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(first));
            for (var index = first + 1; index <= last; index++)
            {
                Invoke("HandleProgressionClick", new Vector2(400, 300));
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(index));
                if (index == 303 || index == 606)
                {
                    Assert.That(_game.Store._storeHatchOpen, Is.True);
                    Assert.That(_game.Store._storeHatchTicks, Is.EqualTo(150));
                }
            }

            Invoke("HandleProgressionClick", new Vector2(400, 300));
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(-1));
            if (first == 3100)
            {
                Assert.That(_game.Store._storeHatchOpen, Is.True);
                Assert.That(_game.Store._storeHatchTicks, Is.EqualTo(150));
            }
            if (first == 601)
            {
                Assert.That(_game.Store._storeTacoDiamondVisible, Is.True);
                Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(0));
                Invoke("HandleProgressionClick", new Vector2(90, 530));
                Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
                Assert.That(_game.Accounts._accountStore.Get(id).HasPendingTacoReward, Is.False);
                Invoke("HandleProgressionClick", new Vector2(90, 480));
                Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
            }
        }

        /// <summary>Checks that the taco diamond persists and automatically collects after its original flight.</summary>
        [Test]
        public void TacoDiamondAutomaticallyCollectsOnce()
        {
            var id = _game._profiles.SelectedId!.Value;
            _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            var reopened = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            Assert.That(reopened.Get(id).HasPendingTacoReward, Is.True);
            Invoke("OpenStoreForAdventureIntro", 601);
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(.02f);
            Invoke("HandleProgressionClick", new Vector2(400, 300));
            Invoke("HandleProgressionClick", new Vector2(400, 300));
            Tick(.81f);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
            Assert.That(_game.Store._storeTacoDiamondVisible, Is.False);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(603));
            reopened = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            Assert.That(reopened.Get(id).HasPendingTacoReward, Is.False);
            Assert.That(reopened.Get(id).Coins, Is.EqualTo(100));
        }

        /// <summary>Checks that entering the ordinary store cannot skip the pending taco conversation.</summary>
        [Test]
        public void OrdinaryStoreDoesNotRedeemTacoBeforeDavePresentsIt()
        {
            var id = _game._profiles.SelectedId!.Value;
            _game.Campaign._adventureStore.Import(id, 35, 0);
            _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            Invoke("OpenStore");
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(1f);
            Assert.That(_game.Store._storeTacoDiamondVisible, Is.False);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.Zero);
            Assert.That(_game.Accounts._accountStore.Get(id).HasPendingTacoReward, Is.True);
            Assert.That(_game.Accounts._accountStore.CollectTacoReward(id), Is.False);
            Invoke("StartAdventureCampaign");
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(.02f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(601));
        }

        /// <summary>Checks that a presented but uncollected diamond survives interruption exactly once.</summary>
        [Test]
        public void PresentedTacoDiamondSurvivesStoreReentry()
        {
            var id = _game._profiles.SelectedId!.Value;
            _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            _game.Accounts._accountStore.PresentTacoReward(id);
            _game.Accounts._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            Invoke("OpenStore");
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Assert.That(_game.Store._storeTacoDiamondVisible, Is.True);
            Tick(.81f);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
            Assert.That(_game.Accounts._accountStore.Get(id).HasPendingTacoReward, Is.False);
            Invoke("OpenStore");
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(1f);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
        }

        /// <summary>Checks that failed taco persistence remains retryable without advancing or double crediting.</summary>
        [Test]
        public void TacoPersistenceFailuresCanBeRetried()
        {
            var id = _game._profiles.SelectedId!.Value;
            _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            Invoke("OpenStoreForAdventureIntro", 601);
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(.02f);
            Invoke("HandleProgressionClick", new Vector2(400, 300));
            var lockPath = Path.Combine(_root, "fixture.account.lock");
            using (new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                Invoke("HandleProgressionClick", new Vector2(400, 300));
                Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(602));
                Assert.That(_game.Store._storeTacoDiamondVisible, Is.False);
                Assert.That(_game.Accounts._accountStore.Get(id).TacoDiamondPresented, Is.False);
            }

            Invoke("HandleProgressionClick", new Vector2(400, 300));
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(603));
            using (new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                Tick(.81f);
                Assert.That(_game.Store._storeTacoDiamondVisible, Is.True);
                Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.Zero);
            }

            Tick(.02f);
            Assert.That(_game.Store._storeTacoDiamondVisible, Is.False);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
            Tick(1f);
            Assert.That(_game.Accounts._accountStore.Get(id).Coins, Is.EqualTo(100));
        }

        /// <summary>Checks that an interrupted taco reward resumes before the next adventure board.</summary>
        [Test]
        public void PendingTacoResumesStoreBeforeAdventure()
        {
            var id = _game._profiles.SelectedId!.Value;
            _game.Campaign._adventureStore.Import(id, 35, 0);
            _game.Accounts._accountStore.ClaimAdventureReward(id, 1, 34);
            Invoke("StartAdventureCampaign");
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Store));
            Assert.That(_game.Store._storeAdventureIntro, Is.True);
            Assert.That(_game._game.Adventure.Level.Number, Is.EqualTo(35));
            _game.Dave._crazyDaveAnimation = "anim_idle";
            Tick(.02f);
            Assert.That(_game.Dave._crazyDaveMessageIndex, Is.EqualTo(601));
            for (var index = 0; index < 6; index++)
            {
                Invoke("HandleProgressionClick", new Vector2(400, 300));
            }

            Tick(1.51f);
            Invoke("LeaveStore");
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Board));
            Assert.That(_game._game.Adventure.Level.Number, Is.EqualTo(35));
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
            GameMemberReflection.Invoke(_game, name, arguments);
        }

        /// <summary>Reads a private production field.</summary>
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
    }
}
