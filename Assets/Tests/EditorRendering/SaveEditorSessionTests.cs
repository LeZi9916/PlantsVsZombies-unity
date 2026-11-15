#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Editor;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks isolated save drafts, complete persistence, validation, and stale-write protection.</summary>
    public sealed class SaveEditorSessionTests
    {
        /// <summary>Stores the unique temporary directory owned by the current test.</summary>
        private string _directory = string.Empty;
        /// <summary>Stores the first profile selected by the persisted catalog.</summary>
        private Guid _first;
        /// <summary>Stores a second profile whose progress must remain unchanged.</summary>
        private Guid _second;

        /// <summary>Gets the replica catalog path inside the test directory.</summary>
        private string CatalogPath => Path.Combine(_directory, "profiles.dat");
        /// <summary>Gets the replica account path inside the test directory.</summary>
        private string AccountPath => Path.Combine(_directory, "player.account");
        /// <summary>Gets the replica adventure path inside the test directory.</summary>
        private string AdventurePath => Path.Combine(_directory, "campaign.adventure");

        /// <summary>Creates two profiles without creating account or adventure sidecars.</summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pvz-save-editor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var catalog = new ProfileCatalog(CatalogPath);
            _first = catalog.Create("First").Id;
            _second = catalog.Create("Second").Id;
            catalog.Select(_first);
        }

        /// <summary>Removes only the uniquely named temporary directory created by this fixture.</summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>Checks loading defaults and choosing a profile do not mutate the game selection or create files.</summary>
        [Test]
        public void OpeningAndSelectingProfilesDoNotWriteFiles()
        {
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var files = Directory.GetFiles(_directory);
            var session = new SaveEditorSession(_directory);
            Assert.That(session.DirectoryPath, Is.EqualTo(Path.GetFullPath(_directory)));
            Assert.That(session.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(session.ProfileName, Is.EqualTo("First"));
            Assert.That(session.Profiles.Count, Is.EqualTo(2));
            Assert.That(session.Account.Coins, Is.Zero);
            Assert.That(session.Adventure.NextLevel, Is.EqualTo(1));

            session.SelectProfile(_second);

            Assert.That(session.SelectedProfileId, Is.EqualTo(_second));
            Assert.That(session.Account.ProfileId, Is.EqualTo(_second));
            Assert.That(session.Adventure.ProfileId, Is.EqualTo(_second));
            Assert.That(new ProfileCatalog(CatalogPath).SelectedId, Is.EqualTo(_first));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(Directory.GetFiles(_directory), Is.EquivalentTo(files));
            Assert.That(File.Exists(AccountPath), Is.False);
            Assert.That(File.Exists(AdventurePath), Is.False);
        }

        /// <summary>Checks discarded drafts never leak into files or another selected profile.</summary>
        [Test]
        public void UnsavedDraftsRemainDetachedFromStoresAndOtherProfiles()
        {
            CreatePersistedState();
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = 900;
            session.Account._purchases[0] = 1;
            session.Adventure.AttemptSeed = -321;
            session.SelectProfile(_second);
            Assert.That(session.Account.Coins, Is.EqualTo(202));
            session.SelectProfile(_first);
            Assert.That(session.Account.Coins, Is.EqualTo(101));
            Assert.That(session.Account.PurchaseCount(0), Is.Zero);
            Assert.That(session.Adventure.AttemptSeed, Is.EqualTo(12345));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks every account field, garden detail, purchase, record, and receipt survives a targeted save.</summary>
        [Test]
        public void AccountSaveRoundTripsCompleteSnapshotAndPreservesOtherFiles()
        {
            CreatePersistedState();
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var priorAccountBytes = File.ReadAllBytes(AccountPath);
            var session = new SaveEditorSession(_directory);
            var draft = session.Account;
            draft.Coins = 54321;
            draft.TreeHeight = 98765;
            draft.SnailAwakeUntilUtc = 1800000300;
            draft.GardenGiftClaimed = true;
            draft.MinigamesUnlocked = true;
            draft.PuzzleUnlocked = true;
            draft.SurvivalUnlocked = true;
            draft.PacketUpgradeDeclines = 7;
            draft.TreeFoodGiftClaimed = false;
            draft._rewardCycle = 4;
            draft._rewardMask = (1UL << 25) - 1;
            draft._receipts["test:existing"] = 42;
            draft._receipts["zen:tutorial"] = 6;
            draft._receipts["zen:fertilizer"] = 1;
            for (var index = 0; index < draft._purchases.Length; index++)
            {
                draft._purchases[index] = index == 21 ? 4 : index + 1;
            }

            for (var index = 0; index < draft._modeRecords.Length; index++)
            {
                draft._modeRecords[index] = index * 3;
            }

            var plant = CreateGardenPlant();
            draft._garden.Add(plant);
            session.SaveAccount();

            var restored = new PlayerAccountStore(AccountPath).Get(_first);
            Assert.That(restored.ProfileId, Is.EqualTo(_first));
            Assert.That(restored.Coins, Is.EqualTo(54321));
            Assert.That(restored.TreeHeight, Is.EqualTo(98765));
            Assert.That(restored.SnailAwakeUntilUtc, Is.EqualTo(1800000300));
            Assert.That(restored.GardenGiftClaimed, Is.True);
            Assert.That(restored.MinigamesUnlocked, Is.True);
            Assert.That(restored.PuzzleUnlocked, Is.True);
            Assert.That(restored.SurvivalUnlocked, Is.True);
            Assert.That(restored.PacketUpgradeDeclines, Is.EqualTo(7));
            Assert.That(restored.TreeFoodGiftClaimed, Is.False);
            Assert.That(restored._rewardCycle, Is.EqualTo(4));
            Assert.That(restored._rewardMask, Is.EqualTo(draft._rewardMask));
            Assert.That(restored._purchases, Is.EqualTo(draft._purchases));
            Assert.That(restored._modeRecords, Is.EqualTo(draft._modeRecords));
            Assert.That(restored._receipts, Is.EquivalentTo(draft._receipts));
            Assert.That(restored.Plants.Count, Is.EqualTo(1));
            AssertGardenPlant(restored.Plants[0], plant);
            Assert.That(new PlayerAccountStore(AccountPath).Get(_second).Coins, Is.EqualTo(202));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
            Assert.That(File.ReadAllBytes(AccountPath + ".backup"), Is.EqualTo(priorAccountBytes));

            draft.Coins = 1;
            plant.Growth = 0;
            Assert.That(session.Account.Coins, Is.EqualTo(54321));
            Assert.That(session.Account.Plants[0].Growth, Is.EqualTo(3));
            session.Account.Coins = 2;
            Assert.That(new PlayerAccountStore(AccountPath).Get(_first).Coins, Is.EqualTo(54321));
        }

        /// <summary>Checks adventure snapshots retain attempts, reward masks, seed choices, and unrelated profiles.</summary>
        [Test]
        public void AdventureSaveRoundTripsLoadoutAndPreservesOtherFiles()
        {
            CreatePersistedState();
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var accountBytes = File.ReadAllBytes(AccountPath);
            var priorAdventureBytes = File.ReadAllBytes(AdventurePath);
            var session = new SaveEditorSession(_directory);
            var draft = session.Adventure;
            draft.NextLevel = 29;
            draft.Cycle = 3;
            draft.CollectedRewards = (1UL << 28) - 1;
            draft.PacketSlots = 8;
            draft.AttemptSeed = -123456;
            draft.ImitaterTarget = PlantKind.Sunflower;
            draft.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.Sunflower, PlantKind.Peashooter, PlantKind.Imitater });
            session.SaveAdventure();

            var restored = new AdventureProgressStore(AdventurePath).Get(_first);
            Assert.That(restored.NextLevel, Is.EqualTo(29));
            Assert.That(restored.Cycle, Is.EqualTo(3));
            Assert.That(restored.CollectedRewards, Is.EqualTo(draft.CollectedRewards));
            Assert.That(restored.PacketSlots, Is.EqualTo(8));
            Assert.That(restored.AttemptSeed, Is.EqualTo(-123456));
            Assert.That(restored.ImitaterTarget, Is.EqualTo(PlantKind.Sunflower));
            Assert.That(restored.SelectedSeeds, Is.EqualTo(draft.SelectedSeeds));
            Assert.That(new AdventureProgressStore(AdventurePath).Get(_second).NextLevel, Is.EqualTo(31));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            var backups = Directory.GetFiles(_directory, "campaign.adventure.backup-*");
            Assert.That(backups.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(Array.Exists(backups, path => EqualBytes(File.ReadAllBytes(path), priorAdventureBytes)), Is.True);

            draft.AttemptSeed = 1;
            Assert.That(session.Adventure.AttemptSeed, Is.EqualTo(-123456));
            session.Adventure.AttemptSeed = 2;
            Assert.That(new AdventureProgressStore(AdventurePath).Get(_first).AttemptSeed, Is.EqualTo(-123456));
        }

        /// <summary>Checks invalid coin balances are rejected before replacing the account file.</summary>
        /// <param name="coins">The balance outside the supported account range.</param>
        [TestCase(-1)]
        [TestCase(100000)]
        public void InvalidAccountBalanceCannotChangePersistedBytes(int coins)
        {
            CreatePersistedState();
            var bytes = File.ReadAllBytes(AccountPath);
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = coins;
            Assert.Throws<InvalidDataException>(() => session.SaveAccount());
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(bytes));
        }

        /// <summary>Checks duplicate garden occupancy is rejected without changing the original account.</summary>
        [Test]
        public void DuplicateGardenSlotsCannotChangePersistedBytes()
        {
            CreatePersistedState();
            var bytes = File.ReadAllBytes(AccountPath);
            var session = new SaveEditorSession(_directory);
            session.Account._garden.Add(CreateGardenPlant());
            session.Account._garden.Add(CreateGardenPlant());
            Assert.Throws<InvalidDataException>(() => session.SaveAccount());
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(bytes));
        }

        /// <summary>Checks inconsistent reward masks cannot produce an unreadable adventure checkpoint.</summary>
        [Test]
        public void InvalidAdventureCheckpointCannotChangePersistedBytes()
        {
            CreatePersistedState();
            var bytes = File.ReadAllBytes(AdventurePath);
            var session = new SaveEditorSession(_directory);
            session.Adventure.NextLevel = 30;
            Assert.Throws<InvalidDataException>(() => session.SaveAdventure());
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(bytes));
        }

        /// <summary>Checks changing a draft identity cannot redirect a save to a different existing profile.</summary>
        [Test]
        public void DraftIdentitiesCannotRedirectSaves()
        {
            CreatePersistedState();
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var session = new SaveEditorSession(_directory);
            session.Account.ProfileId = _second;
            session.Adventure.ProfileId = _second;
            Assert.Throws<InvalidDataException>(() => session.SaveAccount());
            Assert.Throws<InvalidDataException>(() => session.SaveAdventure());
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks another session's commit is detected separately for each sidecar.</summary>
        [Test]
        public void StaleSidecarWritersCannotOverwriteNewerSaves()
        {
            CreatePersistedState();
            var stale = new SaveEditorSession(_directory);
            var current = new SaveEditorSession(_directory);
            current.Account.Coins = 777;
            current.SaveAccount();
            current.Adventure.AttemptSeed = 888;
            current.SaveAdventure();
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);

            stale.Account.Coins = 999;
            stale.Adventure.AttemptSeed = 999;
            Assert.Throws<IOException>(() => stale.SaveAccount());
            Assert.Throws<IOException>(() => stale.SaveAdventure());
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks changing or deleting a profile externally invalidates both pending save operations.</summary>
        /// <param name="delete">Whether the external operation removes the profile instead of renaming it.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ExternalCatalogChangesBlockPendingSaves(bool delete)
        {
            CreatePersistedState();
            var session = new SaveEditorSession(_directory);
            var catalog = new ProfileCatalog(CatalogPath);
            if (delete)
            {
                catalog.Delete(_first);
            }
            else
            {
                catalog.Rename(_first, "External");
            }

            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            session.Account.Coins = 432;
            session.Adventure.AttemptSeed = 543;
            Assert.Throws<IOException>(() => session.SaveAccount());
            Assert.Throws<IOException>(() => session.SaveAdventure());
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks renaming retains identity and both drafts while updating the catalog conflict baseline.</summary>
        [Test]
        public void RenamePreservesIdentityAndPendingProgress()
        {
            CreatePersistedState();
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = 555;
            session.Adventure.AttemptSeed = 666;
            session.RenameProfile(" Renamed ");
            Assert.That(session.ProfileName, Is.EqualTo("Renamed"));
            Assert.That(session.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(new ProfileCatalog(CatalogPath).Profiles[0].Id, Is.EqualTo(_first));
            Assert.That(session.Account.Coins, Is.EqualTo(555));
            Assert.That(session.Adventure.AttemptSeed, Is.EqualTo(666));
            session.SaveAccount();
            session.SaveAdventure();
            Assert.That(new PlayerAccountStore(AccountPath).Get(_first).Coins, Is.EqualTo(555));
            Assert.That(new AdventureProgressStore(AdventurePath).Get(_first).AttemptSeed, Is.EqualTo(666));
        }

        /// <summary>Checks absent and corrupted catalogs are rejected without being created or repaired.</summary>
        [Test]
        public void MissingAndCorruptCatalogsAreNotRewritten()
        {
            File.Delete(CatalogPath);
            Assert.Throws<InvalidDataException>(() => new SaveEditorSession(_directory));
            Assert.That(File.Exists(CatalogPath), Is.False);
            var corrupt = new byte[] { 1, 2, 3, 4 };
            File.WriteAllBytes(CatalogPath, corrupt);
            Assert.Throws<InvalidDataException>(() => new SaveEditorSession(_directory));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(corrupt));
        }

        /// <summary>Checks an empty catalog is reported without manufacturing an editable user.</summary>
        [Test]
        public void EmptyCatalogIsRejectedWithoutCreatingSidecars()
        {
            var catalog = new ProfileCatalog(CatalogPath);
            catalog.Delete(_first);
            catalog.Delete(_second);
            var bytes = File.ReadAllBytes(CatalogPath);
            Assert.Throws<InvalidDataException>(() => new SaveEditorSession(_directory));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(bytes));
            Assert.That(File.Exists(AccountPath), Is.False);
            Assert.That(File.Exists(AdventurePath), Is.False);
        }

        /// <summary>Checks corrupted sidecars remain untouched instead of being replaced with default snapshots.</summary>
        /// <param name="account">Whether the malformed file is the account sidecar rather than adventure progress.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void CorruptSidecarsAreNotRewritten(bool account)
        {
            CreatePersistedState();
            var path = account ? AccountPath : AdventurePath;
            var corrupt = new byte[] { 1, 2, 3, 4 };
            File.WriteAllBytes(path, corrupt);
            Assert.Throws<InvalidDataException>(() => new SaveEditorSession(_directory));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(corrupt));
        }

        /// <summary>Checks an unknown editor selection cannot redirect either draft.</summary>
        [Test]
        public void UnknownProfileSelectionIsRejected()
        {
            var session = new SaveEditorSession(_directory);
            Assert.Catch<ArgumentException>(() => session.SelectProfile(Guid.NewGuid()));
            Assert.That(session.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(session.Account.ProfileId, Is.EqualTo(_first));
            Assert.That(session.Adventure.ProfileId, Is.EqualTo(_first));
        }

        /// <summary>Checks Unity serialization retains complete pending edits and the editor-only selected profile.</summary>
        [Test]
        public void SerializedDraftRestoresCompletePendingChangesAfterDomainReload()
        {
            CreatePersistedState();
            var original = new SaveEditorSession(_directory);
            original.SelectProfile(_second);
            var account = original.Account;
            account.Coins = 45678;
            account.TreeHeight = 9876;
            account.SnailAwakeUntilUtc = 1800000123;
            account.GardenGiftClaimed = true;
            account.MinigamesUnlocked = true;
            account.PuzzleUnlocked = true;
            account.SurvivalUnlocked = true;
            account.PacketUpgradeDeclines = 2;
            account.TreeFoodGiftClaimed = false;
            account._rewardCycle = 7;
            account._rewardMask = (1UL << 49) - 1;
            account._receipts["zen:fertilizer"] = 1;
            account._receipts["zen:tutorial"] = 4;
            account._receipts["taco:pending"] = 2;
            account._receipts["domain:receipt"] = 123;
            for (var index = 0; index < account._purchases.Length; index++)
            {
                account._purchases[index] = index == 21 ? 3 : index + 1;
            }

            for (var index = 0; index < account._modeRecords.Length; index++)
            {
                account._modeRecords[index] = index + 100;
            }

            var plant = CreateGardenPlant();
            account._garden.Add(plant);
            var adventure = original.Adventure;
            adventure.NextLevel = 50;
            adventure.Cycle = 8;
            adventure.CollectedRewards = (1UL << 50) - 1;
            adventure.AttemptSeed = -456789;
            adventure.PacketSlots = 9;
            adventure.ImitaterTarget = PlantKind.Sunflower;
            adventure.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Imitater });
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var captured = SaveEditorDraft.Capture(original, "Pending name", true, true);
            var restoredDraft = JsonUtility.FromJson<SaveEditorDraft>(JsonUtility.ToJson(captured));
            var restored = new SaveEditorSession(_directory);

            Assert.That(restoredDraft.Restore(restored), Is.True);

            Assert.That(restoredDraft.DirectoryPath, Is.EqualTo(original.DirectoryPath));
            Assert.That(restoredDraft.SelectedProfileId, Is.EqualTo(_second));
            Assert.That(restoredDraft.ProfileName, Is.EqualTo("Pending name"));
            Assert.That(restoredDraft.AccountDirty, Is.True);
            Assert.That(restoredDraft.AdventureDirty, Is.True);
            Assert.That(restored.SelectedProfileId, Is.EqualTo(_second));
            Assert.That(restored.Account.ProfileId, Is.EqualTo(_second));
            Assert.That(restored.Account.Coins, Is.EqualTo(account.Coins));
            Assert.That(restored.Account.TreeHeight, Is.EqualTo(account.TreeHeight));
            Assert.That(restored.Account.SnailAwakeUntilUtc, Is.EqualTo(account.SnailAwakeUntilUtc));
            Assert.That(restored.Account.GardenGiftClaimed, Is.True);
            Assert.That(restored.Account.MinigamesUnlocked, Is.True);
            Assert.That(restored.Account.PuzzleUnlocked, Is.True);
            Assert.That(restored.Account.SurvivalUnlocked, Is.True);
            Assert.That(restored.Account.PacketUpgradeDeclines, Is.EqualTo(2));
            Assert.That(restored.Account.TreeFoodGiftClaimed, Is.False);
            Assert.That(restored.Account._rewardCycle, Is.EqualTo(account._rewardCycle));
            Assert.That(restored.Account._rewardMask, Is.EqualTo(account._rewardMask));
            Assert.That(restored.Account._purchases, Is.EqualTo(account._purchases));
            Assert.That(restored.Account._modeRecords, Is.EqualTo(account._modeRecords));
            Assert.That(restored.Account._receipts, Is.EquivalentTo(account._receipts));
            Assert.That(restored.Account.Plants.Count, Is.EqualTo(1));
            AssertGardenPlant(restored.Account.Plants[0], plant);
            Assert.That(restored.Adventure.ProfileId, Is.EqualTo(_second));
            Assert.That(restored.Adventure.NextLevel, Is.EqualTo(adventure.NextLevel));
            Assert.That(restored.Adventure.Cycle, Is.EqualTo(adventure.Cycle));
            Assert.That(restored.Adventure.CollectedRewards, Is.EqualTo(adventure.CollectedRewards));
            Assert.That(restored.Adventure.AttemptSeed, Is.EqualTo(adventure.AttemptSeed));
            Assert.That(restored.Adventure.PacketSlots, Is.EqualTo(adventure.PacketSlots));
            Assert.That(restored.Adventure.ImitaterTarget, Is.EqualTo(adventure.ImitaterTarget));
            Assert.That(restored.Adventure.SelectedSeeds, Is.EqualTo(adventure.SelectedSeeds));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));

            account._purchases[0] = 999;
            plant.Growth = 0;
            Assert.That(restored.Account.PurchaseCount(0), Is.EqualTo(1));
            Assert.That(restored.Account.Plants[0].Growth, Is.EqualTo(3));
            restored.SaveAccount();
            restored.SaveAdventure();
            Assert.That(new PlayerAccountStore(AccountPath).Get(_second).Coins, Is.EqualTo(45678));
            Assert.That(new AdventureProgressStore(AdventurePath).Get(_second).AttemptSeed, Is.EqualTo(-456789));
        }

        /// <summary>Checks serialized clean drafts preserve absent sidecars and their original missing-file baseline.</summary>
        [Test]
        public void SerializedCleanDraftAcceptsUnchangedMissingSidecars()
        {
            var original = new SaveEditorSession(_directory);
            var captured = SaveEditorDraft.Capture(original, original.ProfileName, false, false);
            var restoredDraft = JsonUtility.FromJson<SaveEditorDraft>(JsonUtility.ToJson(captured));
            var restored = new SaveEditorSession(_directory);
            Assert.That(restoredDraft.Restore(restored), Is.True);
            Assert.That(restoredDraft.AccountDirty, Is.False);
            Assert.That(restoredDraft.AdventureDirty, Is.False);
            Assert.That(restored.Account.Coins, Is.Zero);
            Assert.That(restored.Adventure.NextLevel, Is.EqualTo(1));
            Assert.That(File.Exists(AccountPath), Is.False);
            Assert.That(File.Exists(AdventurePath), Is.False);
        }

        /// <summary>Checks capturing after an external write retains the original baseline and the user's pending edits.</summary>
        /// <param name="changedFile">The externally changed file: zero for account, one for adventure, or two for catalog.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SerializedDraftDetectsExternalChangesBeforeCapture(int changedFile)
        {
            CreatePersistedState();
            var original = new SaveEditorSession(_directory);
            original.Account.Coins = 998;
            original.Account._receipts["pending:receipt"] = 7;
            original.Adventure.AttemptSeed = 776;
            if (changedFile == 0)
            {
                var external = new PlayerAccountStore(AccountPath);
                external.Import(_first, 333, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
            }
            else if (changedFile == 1)
            {
                new AdventureProgressStore(AdventurePath).Import(_first, 21, 2);
            }
            else
            {
                new ProfileCatalog(CatalogPath).Rename(_first, "External");
            }

            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var captured = SaveEditorDraft.Capture(original, "Pending name", true, true);
            var restoredDraft = JsonUtility.FromJson<SaveEditorDraft>(JsonUtility.ToJson(captured));
            var restored = new SaveEditorSession(_directory);

            Assert.That(restoredDraft.Restore(restored), Is.False);

            Assert.That(restored.Account.Coins, Is.EqualTo(998));
            Assert.That(restored.Account._receipts["pending:receipt"], Is.EqualTo(7));
            Assert.That(restored.Adventure.AttemptSeed, Is.EqualTo(776));
            Assert.That(restoredDraft.ProfileName, Is.EqualTo("Pending name"));
            Assert.That(restoredDraft.AccountDirty, Is.True);
            Assert.That(restoredDraft.AdventureDirty, Is.True);
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
        }

        /// <summary>Checks domain reload does not attach an orphaned draft to a different remaining user.</summary>
        [Test]
        public void SerializedDraftRejectsRemovedProfileWithoutChangingTheRemainingDraft()
        {
            CreatePersistedState();
            var original = new SaveEditorSession(_directory);
            original.Account.Coins = 998;
            var captured = SaveEditorDraft.Capture(original, original.ProfileName, true, false);
            var restoredDraft = JsonUtility.FromJson<SaveEditorDraft>(JsonUtility.ToJson(captured));
            new ProfileCatalog(CatalogPath).Delete(_first);
            var restored = new SaveEditorSession(_directory);

            Assert.That(restoredDraft.Restore(restored), Is.False);

            Assert.That(restored.SelectedProfileId, Is.EqualTo(_second));
            Assert.That(restored.Account.Coins, Is.EqualTo(202));
            Assert.That(restoredDraft.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(restoredDraft.AccountDirty, Is.True);
        }

        /// <summary>Creates existing sidecars with distinct progress for both fixture profiles.</summary>
        private void CreatePersistedState()
        {
            var accounts = new PlayerAccountStore(AccountPath);
            accounts.Import(_first, 101, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
            accounts.Import(_second, 202, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
            var adventures = new AdventureProgressStore(AdventurePath);
            adventures.Import(_first, 14, 1);
            adventures.Import(_second, 31, 3);
        }

        /// <summary>Creates a valid garden snapshot containing distinct values for every persisted field.</summary>
        /// <returns>A detached mature mushroom plant with complete care and reward metadata.</returns>
        private static GardenPlant CreateGardenPlant()
        {
            return new GardenPlant
            {
                Id = Guid.NewGuid(),
                OriginalIndex = 17,
                Kind = PlantKind.PuffShroom,
                Garden = GardenKind.Mushroom,
                Slot = 3,
                Growth = 3,
                Waterings = 4,
                FeedingsPerGrow = 5,
                MatureNeed = GardenNeed.Music,
                NextCareUtc = 1800000001,
                HappyUntilUtc = 1800000002,
                LastWaterUtc = 1800000003,
                LastNeedFulfilledUtc = 1800000004,
                LastFertilizedUtc = 1800000005,
                ChocolateUntilUtc = 1800000006,
                LastCoinUtc = 1800000007,
                PendingCoins = 29,
                CareCycle = 37
            };
        }

        /// <summary>Checks the complete persisted garden data against its detached source snapshot.</summary>
        /// <param name="actual">The plant read from the saved account.</param>
        /// <param name="expected">The original edited plant snapshot.</param>
        private static void AssertGardenPlant(GardenPlant actual, GardenPlant expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.OriginalIndex, Is.EqualTo(expected.OriginalIndex));
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.Garden, Is.EqualTo(expected.Garden));
            Assert.That(actual.Slot, Is.EqualTo(expected.Slot));
            Assert.That(actual.Growth, Is.EqualTo(expected.Growth));
            Assert.That(actual.Waterings, Is.EqualTo(expected.Waterings));
            Assert.That(actual.FeedingsPerGrow, Is.EqualTo(expected.FeedingsPerGrow));
            Assert.That(actual.MatureNeed, Is.EqualTo(expected.MatureNeed));
            Assert.That(actual.NextCareUtc, Is.EqualTo(expected.NextCareUtc));
            Assert.That(actual.HappyUntilUtc, Is.EqualTo(expected.HappyUntilUtc));
            Assert.That(actual.LastWaterUtc, Is.EqualTo(expected.LastWaterUtc));
            Assert.That(actual.LastNeedFulfilledUtc, Is.EqualTo(expected.LastNeedFulfilledUtc));
            Assert.That(actual.LastFertilizedUtc, Is.EqualTo(expected.LastFertilizedUtc));
            Assert.That(actual.ChocolateUntilUtc, Is.EqualTo(expected.ChocolateUntilUtc));
            Assert.That(actual.LastCoinUtc, Is.EqualTo(expected.LastCoinUtc));
            Assert.That(actual.PendingCoins, Is.EqualTo(expected.PendingCoins));
            Assert.That(actual.CareCycle, Is.EqualTo(expected.CareCycle));
        }

        /// <summary>Compares two binary snapshots without allocating additional collections.</summary>
        /// <param name="first">The first complete file contents.</param>
        /// <param name="second">The second complete file contents.</param>
        /// <returns>Whether the lengths and every byte match.</returns>
        private static bool EqualBytes(byte[] first, byte[] second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            for (var index = 0; index < first.Length; index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
