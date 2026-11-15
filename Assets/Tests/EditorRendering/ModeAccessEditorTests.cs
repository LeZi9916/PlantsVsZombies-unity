#nullable enable
using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Tests.Shared;
using PlantsVsZombies.Editor;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks persistent mode access overrides without manufacturing scores or adventure completion.</summary>
    public sealed class ModeAccessEditorTests
    {
        /// <summary>Stores the uniquely owned temporary replica save directory.</summary>
        private string _directory = string.Empty;

        /// <summary>Identifies the synthetic user edited by each test.</summary>
        private Guid _profileId;

        /// <summary>Gets the account file used by the synthetic profile.</summary>
        private string AccountPath => Path.Combine(_directory, "player.account");

        /// <summary>Gets the adventure file used by the synthetic profile.</summary>
        private string AdventurePath => Path.Combine(_directory, "campaign.adventure");

        /// <summary>Creates a fresh catalog while leaving adventure and account data at their default values.</summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pvz-mode-access-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var catalog = new ProfileCatalog(Path.Combine(_directory, "profiles.dat"));
            _profileId = catalog.Create("Mode access fixture").Id;
        }

        /// <summary>Removes only the unique synthetic save directory created for this test.</summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>Checks unlocking every mode on a new profile does not mark adventure or any mode as completed.</summary>
        [Test]
        public void UnlockAllOpensEveryModeWithoutAdventureCompletionOrScores()
        {
            var session = new SaveEditorSession(_directory);
            ModeAccessRules.UnlockAll(session.Account);
            Assert.That(session.Account.MinigamesUnlocked, Is.True);
            Assert.That(session.Account.PuzzleUnlocked, Is.True);
            Assert.That(session.Account.SurvivalUnlocked, Is.True);
            var count = 0;
            foreach (var mode in ModeCatalog.All)
            {
                count++;
                Assert.That(ModeAccessRules.GetOverride(session.Account, mode.Id), Is.True, mode.Name);
                Assert.That(ModeAccessRules.LockReason(mode.Id, session.Account, 1, false), Is.Null, mode.Name);
                Assert.That(session.Account.ModeRecord(mode.RecordIndex), Is.Zero, mode.Name);
            }

            Assert.That(count, Is.EqualTo(55));
            Assert.That(session.Account.Coins, Is.Zero);
            Assert.That(session.Account._rewardMask, Is.Zero);
            Assert.That(session.Adventure.NextLevel, Is.EqualTo(1));
            Assert.That(session.Adventure.Cycle, Is.EqualTo(1));
            Assert.That(session.Adventure.CollectedRewards, Is.Zero);
            session.SaveAccount();
            var restored = new SaveEditorSession(_directory);
            foreach (var mode in ModeCatalog.All)
            {
                Assert.That(ModeAccessRules.LockReason(mode.Id, restored.Account, restored.Adventure.NextLevel, false), Is.Null, mode.Name);
                Assert.That(restored.Account.ModeRecord(mode.RecordIndex), Is.Zero, mode.Name);
            }

            Assert.That(File.Exists(AdventurePath), Is.False);
        }

        /// <summary>Checks force-unlock, force-lock, and returning to native rules persist independently.</summary>
        [Test]
        public void IndividualOverrideStatesPersistAndRemainIndependent()
        {
            var session = new SaveEditorSession(_directory);
            ModeAccessRules.SetOverride(session.Account, 35, true);
            ModeAccessRules.SetOverride(session.Account, 16, false);
            ModeAccessRules.SetOverride(session.Account, 60, true);
            session.SaveAccount();
            var restored = new SaveEditorSession(_directory);
            Assert.That(ModeAccessRules.GetOverride(restored.Account, 35), Is.True);
            Assert.That(ModeAccessRules.GetOverride(restored.Account, 16), Is.False);
            Assert.That(ModeAccessRules.GetOverride(restored.Account, 60), Is.True);
            Assert.That(ModeAccessRules.GetOverride(restored.Account, 17), Is.Null);
            Assert.That(ModeAccessRules.LockReason(35, restored.Account, 1, false), Is.Null);
            Assert.That(ModeAccessRules.LockReason(16, restored.Account, 50, true), Is.Not.Null);
            Assert.That(ModeAccessRules.LockReason(60, restored.Account, 1, false), Is.Null);

            ModeAccessRules.SetOverride(restored.Account, 35, null);
            ModeAccessRules.SetOverride(restored.Account, 60, false);
            restored.SaveAccount();
            var automatic = new SaveEditorSession(_directory);
            Assert.That(ModeAccessRules.GetOverride(automatic.Account, 35), Is.Null);
            Assert.That(automatic.Account._receipts.ContainsKey(OverrideKey(35)), Is.False);
            Assert.That(ModeAccessRules.LockReason(35, automatic.Account, 1, false), Is.Not.Null);
            Assert.That(ModeAccessRules.GetOverride(automatic.Account, 16), Is.False);
            Assert.That(ModeAccessRules.GetOverride(automatic.Account, 60), Is.False);
            Assert.That(ModeAccessRules.LockReason(60, automatic.Account, 50, true), Is.Not.Null);
            Assert.That(ModeAccessRules.GetOverride(automatic.Account, 17), Is.Null);
        }

        /// <summary>Checks a specific mode opens only its own category and an explicit lock does not grant category access.</summary>
        /// <param name="modeId">A representative survival, minigame, or puzzle identifier.</param>
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(51)]
        public void SpecificUnlockOpensOnlyItsCategory(int modeId)
        {
            var account = new SaveEditorSession(_directory).Account;
            ModeAccessRules.SetOverride(account, modeId, false);
            Assert.That(account.SurvivalUnlocked, Is.False);
            Assert.That(account.MinigamesUnlocked, Is.False);
            Assert.That(account.PuzzleUnlocked, Is.False);
            ModeAccessRules.SetOverride(account, modeId, true);
            Assert.That(account.SurvivalUnlocked, Is.EqualTo(modeId == 1));
            Assert.That(account.MinigamesUnlocked, Is.EqualTo(modeId == 16));
            Assert.That(account.PuzzleUnlocked, Is.EqualTo(modeId == 51));
        }

        /// <summary>Checks automatic access exactly retains the legacy category and trophy gates for every shipped mode.</summary>
        /// <param name="nextLevel">The original next adventure level used by category gates.</param>
        /// <param name="finished">Whether adventure has been completed.</param>
        /// <param name="hasTrophies">Whether every finite mode already has its native trophy record.</param>
        [TestCase(1, false, false)]
        [TestCase(23, false, false)]
        [TestCase(37, false, false)]
        [TestCase(50, false, true)]
        [TestCase(1, true, false)]
        [TestCase(50, true, true)]
        public void AutomaticStatePreservesLegacyGatesForEveryMode(int nextLevel, bool finished, bool hasTrophies)
        {
            var account = new SaveEditorSession(_directory).Account;
            if (hasTrophies)
            {
                foreach (var mode in ModeCatalog.All)
                {
                    account._modeRecords[mode.RecordIndex] = mode.Id <= 5 ? 5 : mode.Id <= 10 ? 10 : 1;
                }
            }

            var count = 0;
            foreach (var mode in ModeCatalog.All)
            {
                count++;
                Assert.That(ModeAccessRules.GetOverride(account, mode.Id), Is.Null);
                Assert.That(ModeAccessRules.LockReason(mode.Id, account, nextLevel, finished),
                    Is.EqualTo(LegacyLockReason(mode.Id, account, nextLevel, finished)), mode.Name);
            }

            Assert.That(count, Is.EqualTo(55));
        }

        /// <summary>Checks override edits preserve the wallet, all scores, purchases, and the independent adventure sidecar.</summary>
        [Test]
        public void AccessEditsDoNotAlterCoinsScoresPurchasesOrAdventure()
        {
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = 12345;
            session.Account.TreeHeight = 321;
            session.Account._purchases[21] = 2;
            for (var index = 0; index < session.Account._modeRecords.Length; index++)
            {
                session.Account._modeRecords[index] = index + 7;
            }

            session.Adventure.NextLevel = 24;
            session.Adventure.Cycle = 3;
            session.Adventure.CollectedRewards = (1UL << 23) - 1;
            session.Adventure.AttemptSeed = -9876;
            session.SaveAccount();
            session.SaveAdventure();
            var records = (int[])session.Account._modeRecords.Clone();
            var purchases = (int[])session.Account._purchases.Clone();
            var adventureBytes = File.ReadAllBytes(AdventurePath);

            ModeAccessRules.UnlockAll(session.Account);
            ModeAccessRules.SetOverride(session.Account, 15, false);
            ModeAccessRules.SetOverride(session.Account, 35, null);
            session.SaveAccount();

            var restored = new SaveEditorSession(_directory);
            Assert.That(restored.Account.Coins, Is.EqualTo(12345));
            Assert.That(restored.Account.TreeHeight, Is.EqualTo(321));
            Assert.That(restored.Account._modeRecords, Is.EqualTo(records));
            Assert.That(restored.Account._purchases, Is.EqualTo(purchases));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
            Assert.That(restored.Adventure.NextLevel, Is.EqualTo(24));
            Assert.That(restored.Adventure.Cycle, Is.EqualTo(3));
            Assert.That(restored.Adventure.AttemptSeed, Is.EqualTo(-9876));
        }

        /// <summary>Checks inserting overrides into a full receipt table makes room while retaining durable gameplay markers.</summary>
        /// <param name="allModes">Whether every mode is unlocked instead of one specific mode.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SaturatedReceiptTablesCanPersistNewOverrides(bool allModes)
        {
            var session = new SaveEditorSession(_directory);
            session.Account._receipts["taco:pending"] = 2;
            session.Account._receipts["zen:tutorial"] = 3;
            session.Account._receipts["zen:fertilizer"] = 1;
            for (var index = 0; index < 253; index++)
            {
                session.Account._receipts["battle:existing-" + index] = 1;
            }

            Assert.That(session.Account._receipts.Count, Is.EqualTo(256));
            if (allModes)
            {
                ModeAccessRules.UnlockAll(session.Account);
            }
            else
            {
                ModeAccessRules.SetOverride(session.Account, 35, true);
            }

            Assert.That(session.Account._receipts.Count, Is.LessThanOrEqualTo(256));
            session.SaveAccount();
            var restored = new PlayerAccountStore(AccountPath).Get(_profileId);
            Assert.That(restored._receipts["taco:pending"], Is.EqualTo(2));
            Assert.That(restored._receipts["zen:tutorial"], Is.EqualTo(3));
            Assert.That(restored._receipts["zen:fertilizer"], Is.EqualTo(1));
            Assert.That(ModeAccessRules.GetOverride(restored, 35), Is.True);
            if (allModes)
            {
                foreach (var mode in ModeCatalog.All)
                {
                    Assert.That(ModeAccessRules.GetOverride(restored, mode.Id), Is.True, mode.Name);
                }
            }
        }

        /// <summary>Checks reward-receipt trimming never evicts persistent mode overrides after hundreds of ordinary rewards.</summary>
        [Test]
        public void ModeOverridesSurviveMoreThan256OrdinaryRewardReceipts()
        {
            var session = new SaveEditorSession(_directory);
            ModeAccessRules.UnlockAll(session.Account);
            ModeAccessRules.SetOverride(session.Account, 16, false);
            session.SaveAccount();
            var store = new PlayerAccountStore(AccountPath);
            for (var index = 0; index < 270; index++)
            {
                store.CreditBattleCoins(_profileId, "trim-" + index, 1);
            }

            var restored = new PlayerAccountStore(AccountPath).Get(_profileId);
            Assert.That(restored.Coins, Is.EqualTo(270));
            Assert.That(restored._receipts.Count, Is.LessThanOrEqualTo(256));
            Assert.That(restored._receipts["battle:trim-269"], Is.EqualTo(1));
            foreach (var mode in ModeCatalog.All)
            {
                Assert.That(ModeAccessRules.IsOverrideReceipt(OverrideKey(mode.Id)), Is.True);
                Assert.That(ModeAccessRules.GetOverride(restored, mode.Id), Is.EqualTo(mode.Id != 16), mode.Name);
            }

            Assert.That(ModeAccessRules.IsOverrideReceipt("battle:trim-269"), Is.False);
        }

        /// <summary>Checks native editor draft recovery retains every explicit mode state and category marker.</summary>
        [Test]
        public void SerializedDraftRetainsForcedAndAutomaticModeStates()
        {
            var session = new SaveEditorSession(_directory);
            ModeAccessRules.UnlockAll(session.Account);
            ModeAccessRules.SetOverride(session.Account, 16, false);
            ModeAccessRules.SetOverride(session.Account, 35, null);
            var captured = SaveEditorDraft.Capture(session, session.ProfileName, true, false);
            var serialized = JsonUtility.ToJson(captured);
            var draft = JsonUtility.FromJson<SaveEditorDraft>(serialized);
            var restored = new SaveEditorSession(_directory);
            Assert.That(draft.Restore(restored), Is.True);
            Assert.That(restored.Account.MinigamesUnlocked, Is.True);
            Assert.That(restored.Account.SurvivalUnlocked, Is.True);
            Assert.That(restored.Account.PuzzleUnlocked, Is.True);
            foreach (var mode in ModeCatalog.All)
            {
                Assert.That(ModeAccessRules.GetOverride(restored.Account, mode.Id),
                    Is.EqualTo(ModeAccessRules.GetOverride(session.Account, mode.Id)), mode.Name);
            }

            restored.SaveAccount();
            var persisted = new PlayerAccountStore(AccountPath).Get(_profileId);
            Assert.That(ModeAccessRules.GetOverride(persisted, 16), Is.False);
            Assert.That(ModeAccessRules.GetOverride(persisted, 35), Is.Null);
            Assert.That(ModeAccessRules.GetOverride(persisted, 60), Is.True);
        }

        /// <summary>Checks the production mode-directory controller reads the saved forced states instead of applying native gates first.</summary>
        [Test]
        public void GameModeDirectoryUsesPersistedOverrides()
        {
            var session = new SaveEditorSession(_directory);
            ModeAccessRules.SetOverride(session.Account, 35, true);
            ModeAccessRules.SetOverride(session.Account, 16, false);
            session.SaveAccount();
            var host = new GameObject("Isolated mode access fixture");
            host.SetActive(false);
            try
            {
                var game = host.AddComponent<PvZGame>();
                game._profiles = new ProfileCatalog(Path.Combine(_directory, "profiles.dat"));
                game.Accounts._accountStore = new PlayerAccountStore(AccountPath);
                game.Campaign._adventureStore = new AdventureProgressStore(AdventurePath);
                Assert.That(GameMemberReflection.Invoke(game, "ReadModeLockReason", new object?[] { 35 }), Is.Null);
                Assert.That(GameMemberReflection.Invoke(game, "ReadModeLockReason", new object?[] { 16 }), Is.Not.Null);

                ModeAccessRules.SetOverride(session.Account, 35, null);
                session.SaveAccount();
                game.Accounts._accountStore = new PlayerAccountStore(AccountPath);
                GameMemberReflection.SetField(game, "_modeAccountSnapshot", null);
                Assert.That(GameMemberReflection.Invoke(game, "ReadModeLockReason", new object?[] { 35 }), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        /// <summary>Checks unknown mode identifiers are rejected without changing receipts or category access.</summary>
        /// <param name="modeId">An identifier outside the shipped mode catalog.</param>
        [TestCase(0)]
        [TestCase(36)]
        [TestCase(43)]
        [TestCase(50)]
        [TestCase(71)]
        public void InvalidModeIdentifiersCannotMutateDrafts(int modeId)
        {
            var session = new SaveEditorSession(_directory);
            var before = JsonUtility.ToJson(SaveEditorDraft.Capture(session, session.ProfileName, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => ModeAccessRules.SetOverride(session.Account, modeId, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => ModeAccessRules.GetOverride(session.Account, modeId));
            Assert.Throws<ArgumentOutOfRangeException>(() => ModeAccessRules.LockReason(modeId, session.Account, 1, false));
            Assert.That(JsonUtility.ToJson(SaveEditorDraft.Capture(session, session.ProfileName, false, false)), Is.EqualTo(before));
        }

        /// <summary>Checks public and editor mode access entry points reject a missing account.</summary>
        [Test]
        public void MissingAccountsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ModeAccessRules.GetOverride(null!, 16));
            Assert.Throws<ArgumentNullException>(() => ModeAccessRules.LockReason(16, null!, 1, false));
            Assert.Throws<ArgumentNullException>(() => ModeAccessRules.SetOverride(null!, 16, true));
            Assert.Throws<ArgumentNullException>(() => ModeAccessRules.UnlockAll(null!));
        }

        /// <summary>Evaluates the unchanged pre-override category and trophy behavior from the production directory.</summary>
        /// <param name="modeId">The original supported mode identifier.</param>
        /// <param name="account">The account supplying category flags and score records.</param>
        /// <param name="nextLevel">The one-based adventure checkpoint used by native category gifts.</param>
        /// <param name="finished">Whether the account has completed adventure.</param>
        /// <returns>The legacy locked explanation, or null when native progress already permits access.</returns>
        private static string? LegacyLockReason(int modeId, PlayerAccount account, int nextLevel, bool finished)
        {
            var categoryGift = modeId <= 15 ? account.SurvivalUnlocked : modeId <= 35 ? account.MinigamesUnlocked : account.PuzzleUnlocked;
            if (!categoryGift && !ModeUnlockRules.CategoryUnlocked(modeId, nextLevel, finished))
            {
                return modeId <= 15 ? "通关冒险模式后解锁生存模式。" : modeId <= 35
                    ? "通过冒险模式3-2，领取小游戏礼物后解锁。"
                    : "通过冒险模式4-6，领取解谜模式礼物后解锁。";
            }

            return ModeUnlockRules.LockReason(modeId, finished, account.ModeRecord);
        }

        /// <summary>Builds the persisted receipt key for a supported mode identifier.</summary>
        /// <param name="modeId">The original mode identifier whose override is stored.</param>
        /// <returns>The stable invariant-culture receipt key used by the replica account file.</returns>
        private static string OverrideKey(int modeId)
        {
            return "mode:unlock:" + modeId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
