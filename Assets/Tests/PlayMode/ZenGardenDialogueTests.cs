#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises Dave's first garden conversation and original seedling gift.</summary>
    public sealed class ZenGardenDialogueTests
    {
        /// <summary>Owns fixture resources.</summary>
        private string _root = null!;
        /// <summary>Owns the coordinator object.</summary>
        private GameObject _host = null!;
        /// <summary>Owns the voice source.</summary>
        private GameObject _audio = null!;
        /// <summary>Runs the production garden coordinator.</summary>
        private PvZGame _game = null!;
        /// <summary>Loads original-format fixture text.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Provides audible fixture speech.</summary>
        private AudioClip _clip = null!;
        /// <summary>Identifies the fixture player.</summary>
        private Guid _profile;

        /// <summary>Creates an unlocked garden with no prior tutorial gift.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "garden-dave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), "[CRAZY_DAVE_2100]\nOne.\n[CRAZY_DAVE_2101]\nTwo.\n[CRAZY_DAVE_2102]\nThree.\n[CRAZY_DAVE_2103]\nFour.\n[CRAZY_DAVE_2104]\nFive.\n[ADVICE_ZEN_GARDEN_PICK_UP_WATER]\nPick up water.\n[CRAZY_DAVE_1301]\nNext level.\n");
            _host = new GameObject("Garden dialogue fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
            _audio = new GameObject("Garden voice fixture");
            var voice = _audio.AddComponent<AudioSource>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                _audio.AddComponent<AudioListener>();
            }

            _clip = AudioClip.Create("Garden voice", 44100, 1, 44100, false);
            _game._root = _root;
            _game._animations = new ReanimationRenderer(_root);
            _game._selectorView = new SelectorView(_root);
            _game._profiles = new ProfileCatalog(Path.Combine(_root, "profiles.dat"));
            _profile = _game._profiles.Create("Tester").Id;
            _game._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            _game._adventureStore = new AdventureProgressStore(Path.Combine(_root, "fixture.adventure"));
            _game._adventureStore.Import(_profile, 45, 0);
#pragma warning disable SYSLIB0050
            _game._optionsView = (OptionsView)FormatterServices.GetUninitializedObject(typeof(OptionsView));
#pragma warning restore SYSLIB0050
            _presenter = new GamePresenter(_game);
            typeof(PvZGame).GetField("_presenter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, _presenter);
            typeof(PvZGame).GetField("_crazyDaveVoice", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, voice);
            var sounds = (Dictionary<string, AudioClip>)typeof(PvZGame).GetField("_presentationSounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            for (var variant = 1; variant <= 3; variant++)
            {
                sounds["crazydaveshort" + variant] = _clip;
            }
        }

        /// <summary>Releases fixture resources.</summary>
        [TearDown]
        public void TearDown()
        {
            _game?._animations?.Dispose();
            _game?._selectorView?.Dispose();
            _presenter?.Dispose();
            UnityEngine.Object.DestroyImmediate(_clip);
            UnityEngine.Object.DestroyImmediate(_audio);
            UnityEngine.Object.DestroyImmediate(_host);
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        /// <summary>Only the original 2102 entry creates the seedlings, and completing the introduction persists.</summary>
        [Test]
        public void FirstIntroductionGrantsSeedlingsAtOriginalEntry()
        {
            Invoke("OpenZenGarden");
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2100));
            Assert.That(_game._accountStore.Get(_profile).Plants.Count, Is.Zero);
            Invoke("AdvanceZenGardenIntroduction");
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2101));
            Assert.That(_game._accountStore.Get(_profile).Plants.Count, Is.Zero);
            Invoke("AdvanceZenGardenIntroduction");
            var account = _game._accountStore.Get(_profile);
            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(2102));
            Assert.That(account.Plants.Count, Is.EqualTo(2));
            Assert.That(account.Plants[0].FeedingsPerGrow, Is.EqualTo(3));
            Assert.That(account.PurchaseCount(14), Is.Zero);
            for (var index = 0; index < 3; index++)
            {
                Invoke("AdvanceZenGardenIntroduction");
            }

            Assert.That(_game._crazyDaveMessageIndex, Is.EqualTo(-1));
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(1));
            Assert.That(new PlayerAccountStore(Path.Combine(_root, "fixture.account")).Get(_profile).GardenGiftClaimed, Is.True);
            Invoke("OpenZenGarden");
            Assert.That(_game._accountStore.Get(_profile).Plants.Count, Is.EqualTo(2));
        }

        /// <summary>Restores an interrupted garden tutorial without repeating Dave's seedling gift.</summary>
        [Test]
        public void ReopeningRestoresThePersistedTutorialStep()
        {
            Invoke("OpenZenGarden");
            Invoke("AdvanceZenGardenIntroduction");
            Invoke("AdvanceZenGardenIntroduction");
            Invoke("AdvanceZenGardenIntroduction");
            Invoke("AdvanceZenGardenIntroduction");
            Invoke("AdvanceZenGardenIntroduction");
            _game._accountStore.SetZenGardenTutorialStage(_profile, 3);
            _game._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            Invoke("OpenZenGarden");
            Assert.That(_game._gardenDaveIntroActive, Is.False);
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(2));
            Assert.That(_game._gardenReturnToAdventure, Is.True);
            Assert.That(_game._accountStore.Get(_profile).Plants.Count, Is.EqualTo(2));
            _game._currentView = PvZGame.ScreenView.Selector;
            Invoke("StartAdventureCampaign");
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Garden));
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(2));
        }

        /// <summary>Restarts the original store conversation after an interrupted tutorial store visit.</summary>
        [Test]
        public void ReopeningRestoresPendingGardenStoreDialogue()
        {
            Invoke("OpenZenGarden");
            for (var index = 0; index < 5; index++)
            {
                Invoke("AdvanceZenGardenIntroduction");
            }

            _game._accountStore.GrantZenGardenTutorialFertilizer(_profile);
            _game._accountStore = new PlayerAccountStore(Path.Combine(_root, "fixture.account"));
            Invoke("OpenZenGarden");
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Store));
            var pending = (int)typeof(PvZGame).GetField("_storeIntroPending", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_game)!;
            Assert.That(pending, Is.EqualTo(2600));
            Assert.That(_game._accountStore.Get(_profile).PurchaseCount(14), Is.EqualTo(5));
        }

        /// <summary>Finishes the garden tutorial when one seedling reaches the store stage before the other.</summary>
        [Test]
        public void UnevenWateringStillCompletesTheTutorialAndReturnsToAdventure()
        {
            Invoke("OpenZenGarden");
            for (var index = 0; index < 5; index++)
            {
                Invoke("AdvanceZenGardenIntroduction");
            }

            Click(new Vector2(20, 20));
            var plants = _game._accountStore.Get(_profile).Plants;
            var first = plants[0].Id;
            var second = plants[1].Id;
            for (var index = 0; index < 3; index++)
            {
                ClickPlant(first);
                ClearFixtureCareDelay();
            }

            typeof(PvZGame).GetField("_gardenUpdateUtc", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, -1L);
            Invoke("UpdateProgression");
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(4));
            Assert.That(_game._accountStore.Get(_profile).Plants[1].Waterings, Is.Zero);
            Click(new Vector2(700, 560));
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Store));
            typeof(PvZGame).GetField("_storeIntroPending", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, -1);
            typeof(PvZGame).GetField("_storeIntroTalking", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, false);
            typeof(PvZGame).GetField("_storeHatchTicks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_game, 0);
            Invoke("LeaveStore");
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(5));
            Click(new Vector2(20, 20));
            for (var index = 0; index < 3; index++)
            {
                ClickPlant(second);
                ClearFixtureCareDelay();
            }

            Assert.That(_game._accountStore.Get(_profile).Plants[1].Waterings, Is.EqualTo(3));
            Click(new Vector2(100, 20));
            ClickPlant(first);
            ClickPlant(second);
            Assert.That(_game._gardenTutorialStage, Is.EqualTo(6));
            Click(new Vector2(700, 20));
            Assert.That(_game._currentView, Is.EqualTo(PvZGame.ScreenView.Board));
            Assert.That(_game._game.Adventure.Level.Number, Is.EqualTo(45));
            Assert.That(_game._accountStore.Get(_profile).ZenGardenTutorialStage, Is.Zero);
        }

        /// <summary>Checks that genuine pointer input cannot select the sale tool during the required tutorial.</summary>
        /// <param name="storedStage">The persisted original tutorial step to resume.</param>
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void TutorialInputCannotSellRequiredSeedlings(int storedStage)
        {
            Invoke("OpenZenGarden");
            for (var index = 0; index < 5; index++)
            {
                Invoke("AdvanceZenGardenIntroduction");
            }

            _game._accountStore.SetZenGardenTutorialStage(_profile, storedStage);
            Invoke("OpenZenGarden");
            var firstPlant = _game._accountStore.Get(_profile).Plants[0];
            SendPointer(new Vector2(420, 20));
            Assert.That(_game._gardenTool, Is.Not.EqualTo(GardenTool.Sell));
            SendPointer(_game.GardenPlantRect(firstPlant).center);
            Assert.That(_game._gardenSalePlant.HasValue, Is.False);
            Assert.That(_game._accountStore.Get(_profile).Plants.Count, Is.EqualTo(2));
        }

        /// <summary>Dispatches a primary-pointer press through the production input route.</summary>
        /// <param name="point">The logical viewport position.</param>
        private void SendPointer(Vector2 point)
        {
            var snapshot = new GameInputSnapshot(point, true, true, false, true, false, 0, false, false);
            typeof(GameInput).GetProperty("Current")!.GetSetMethod(true)!.Invoke(null, new object[] { snapshot });
            Invoke("HandleInput");
        }

        /// <summary>Calls the real garden click handler at a logical point.</summary>
        /// <param name="point">The logical viewport position.</param>
        private void Click(Vector2 point)
        {
            typeof(PvZGame).GetMethod("HandleProgressionClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, new object[] { point });
        }

        /// <summary>Clicks a fixture plant in its current garden slot.</summary>
        /// <param name="plantId">The stable plant identifier.</param>
        private void ClickPlant(Guid plantId)
        {
            foreach (var plant in _game._accountStore.Get(_profile).Plants)
            {
                if (plant.Id == plantId)
                {
                    Click(_game.GardenPlantRect(plant).center);
                    return;
                }
            }

            Assert.Fail("Fixture plant is missing.");
        }

        /// <summary>Advances fixture care time without waiting for wall-clock cooldowns.</summary>
        private void ClearFixtureCareDelay()
        {
            var account = _game._accountStore.Get(_profile);
            foreach (var plant in account.Plants)
            {
                plant.NextCareUtc = 0;
            }

            _game._accountStore.Import(_profile, account.Coins, account._purchases, account._modeRecords, account.TreeHeight, account.Plants,
                account.MinigamesUnlocked, account.PuzzleUnlocked, account.SurvivalUnlocked, account.PacketUpgradeDeclines,
                account.TreeFoodGiftClaimed, account.GardenGiftClaimed);
        }

        /// <summary>Invokes a production method in the isolated coordinator.</summary>
        /// <param name="name">The private method name.</param>
        private void Invoke(string name)
        {
            typeof(PvZGame).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_game, null);
        }
    }
}
