#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Preserves detached save drafts and their original disk baselines through editor domain reloads.</summary>
    [Serializable]
    internal sealed class SaveEditorDraft
    {
        /// <summary>Stores the absolute directory that owns this recovery snapshot.</summary>
        [SerializeField]
        private string _directoryPath = string.Empty;

        /// <summary>Stores the selected profile identity in a Unity-serializable representation.</summary>
        [SerializeField]
        private string _profileId = string.Empty;

        /// <summary>Preserves the profile name entered in the editor, including unapplied edits.</summary>
        [SerializeField]
        private string _profileName = string.Empty;

        /// <summary>Records whether account changes still require a disk commit.</summary>
        [SerializeField]
        private bool _accountDirty;

        /// <summary>Records whether adventure changes still require a disk commit.</summary>
        [SerializeField]
        private bool _adventureDirty;

        /// <summary>Preserves the stale status of a recovered draft through repeated domain reloads.</summary>
        [SerializeField]
        private bool _isStale;

        /// <summary>Preserves the catalog baseline accepted before these drafts were edited.</summary>
        [SerializeField]
        private byte[] _catalogBytes = Array.Empty<byte>();

        /// <summary>Distinguishes a missing account sidecar from an empty serialized byte array.</summary>
        [SerializeField]
        private bool _accountFileExists;

        /// <summary>Preserves the account baseline accepted by the original store.</summary>
        [SerializeField]
        private byte[] _accountBytes = Array.Empty<byte>();

        /// <summary>Distinguishes a missing adventure sidecar from an empty serialized byte array.</summary>
        [SerializeField]
        private bool _adventureFileExists;

        /// <summary>Preserves the adventure baseline accepted by the original store.</summary>
        [SerializeField]
        private byte[] _adventureBytes = Array.Empty<byte>();

        /// <summary>Stores every editable account field without relying on runtime serialization attributes.</summary>
        [SerializeField]
        private AccountData _account = new AccountData();

        /// <summary>Stores every editable adventure field without relying on runtime serialization attributes.</summary>
        [SerializeField]
        private AdventureData _adventure = new AdventureData();

        /// <summary>Gets the original save directory that must be reopened before applying this snapshot.</summary>
        internal string DirectoryPath
        {
            get
            {
                return _directoryPath;
            }
        }

        /// <summary>Gets the profile identity selected when the draft was captured.</summary>
        /// <exception cref="InvalidOperationException">The serialized recovery identity is invalid.</exception>
        internal Guid SelectedProfileId
        {
            get
            {
                return ParseIdentity(_profileId);
            }
        }

        /// <summary>Gets the preserved profile-name draft.</summary>
        internal string ProfileName
        {
            get
            {
                return _profileName;
            }
        }

        /// <summary>Gets whether the recovered account draft has unsaved changes.</summary>
        internal bool AccountDirty
        {
            get
            {
                return _accountDirty;
            }
        }

        /// <summary>Gets whether the recovered adventure draft has unsaved changes.</summary>
        internal bool AdventureDirty
        {
            get
            {
                return _adventureDirty;
            }
        }

        /// <summary>Copies both drafts and the exact original store baselines into editor-serializable data.</summary>
        /// <param name="session">The session whose detached drafts and accepted baselines are copied.</param>
        /// <param name="profileName">The current text of the profile-name field.</param>
        /// <param name="accountDirty">Whether the account draft still requires saving.</param>
        /// <param name="adventureDirty">Whether the adventure draft still requires saving.</param>
        /// <returns>An independent recovery snapshot suitable for Unity window serialization or JsonUtility.</returns>
        /// <exception cref="ArgumentNullException">The session or profile-name text is null.</exception>
        internal static SaveEditorDraft Capture(SaveEditorSession session, string profileName, bool accountDirty, bool adventureDirty)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (profileName == null)
            {
                throw new ArgumentNullException(nameof(profileName));
            }

            var accountBytes = session.AccountBytes;
            var adventureBytes = session.AdventureBytes;
            return new SaveEditorDraft
            {
                _directoryPath = session.DirectoryPath,
                _profileId = session.SelectedProfileId.ToString("N"),
                _profileName = profileName,
                _accountDirty = accountDirty,
                _adventureDirty = adventureDirty,
                _isStale = session.HasStaleDraft,
                _catalogBytes = session.CatalogBytes,
                _accountFileExists = accountBytes != null,
                _accountBytes = accountBytes ?? Array.Empty<byte>(),
                _adventureFileExists = adventureBytes != null,
                _adventureBytes = adventureBytes ?? Array.Empty<byte>(),
                _account = AccountData.Capture(session.Account),
                _adventure = AdventureData.Capture(session.Adventure)
            };
        }

        /// <summary>Restores the selected profile and detached drafts, while detecting changes made during a domain reload.</summary>
        /// <param name="session">A newly opened session for the original save directory.</param>
        /// <returns>True when all three accepted disk baselines still match. False leaves restored drafts available for inspection when the profile exists; a deleted profile leaves the session untouched.</returns>
        /// <exception cref="ArgumentNullException">The new session is null.</exception>
        /// <exception cref="InvalidOperationException">The directory or a serialized identity does not match the recovery snapshot.</exception>
        /// <exception cref="IOException">The catalog changes while the original profile is selected.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog cannot be accessed.</exception>
        internal bool Restore(SaveEditorSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(_directoryPath, session.DirectoryPath, comparison))
            {
                throw new InvalidOperationException("恢复的草稿属于其他存档目录，请先打开原存档目录。");
            }

            var id = SelectedProfileId;
            var found = false;
            foreach (var profile in session.Profiles)
            {
                if (profile.Id == id)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            var current = !_isStale
                && EqualBytes(_catalogBytes, session.CatalogBytes)
                && EqualBytes(_accountFileExists ? _accountBytes : null, session.AccountBytes)
                && EqualBytes(_adventureFileExists ? _adventureBytes : null, session.AdventureBytes);
            var account = _account.CreateAccount();
            var adventure = _adventure.CreateProgress();
            if (account.ProfileId != id || adventure.ProfileId != id)
            {
                throw new InvalidOperationException("恢复的草稿包含不一致的用户标识。");
            }

            session.SelectProfile(id);
            session.RestoreEditorDraft(account, adventure, !current);
            return current;
        }

        /// <summary>Compares exact serialized bytes while preserving the distinction between a missing and existing file.</summary>
        /// <param name="first">The original accepted baseline, or null for a missing file.</param>
        /// <param name="second">The newly accepted baseline, or null for a missing file.</param>
        /// <returns>True when both files were absent or all their bytes match.</returns>
        private static bool EqualBytes(byte[]? first, byte[]? second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }

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

        /// <summary>Decodes a persistent identity recovered from editor serialization.</summary>
        /// <param name="value">The serialized identity string.</param>
        /// <returns>The nonempty decoded identity.</returns>
        /// <exception cref="InvalidOperationException">The identity cannot be decoded or is empty.</exception>
        private static Guid ParseIdentity(string value)
        {
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            {
                throw new InvalidOperationException("恢复的草稿包含无效的用户或植物标识。");
            }

            return id;
        }

        /// <summary>Stores a reward receipt without relying on Unity dictionary serialization.</summary>
        [Serializable]
        private sealed class ReceiptData
        {
            /// <summary>Preserves the unchanged idempotency receipt key.</summary>
            public string Key = string.Empty;
            /// <summary>Preserves the receipt's exact progress or reward value.</summary>
            public int Value;
        }

        /// <summary>Stores all persisted account values using Unity-serializable fields.</summary>
        [Serializable]
        private sealed class AccountData
        {
            /// <summary>Identifies the profile that owns the account draft.</summary>
            public string ProfileId = string.Empty;
            /// <summary>Preserves the wallet in internal coin units.</summary>
            public int Coins;
            /// <summary>Preserves the Wisdom Tree height.</summary>
            public int TreeHeight;
            /// <summary>Preserves the snail's wake deadline in UTC Unix seconds.</summary>
            public long SnailAwakeUntilUtc;
            /// <summary>Preserves completion of the introductory garden gift.</summary>
            public bool GardenGiftClaimed;
            /// <summary>Preserves the minigame unlock flag.</summary>
            public bool MinigamesUnlocked;
            /// <summary>Preserves the puzzle unlock flag.</summary>
            public bool PuzzleUnlocked;
            /// <summary>Preserves the survival unlock flag.</summary>
            public bool SurvivalUnlocked;
            /// <summary>Preserves declined adventure packet upgrade offers.</summary>
            public int PacketUpgradeDeclines;
            /// <summary>Preserves whether the initial tree food gift was claimed.</summary>
            public bool TreeFoodGiftClaimed;
            /// <summary>Preserves the cycle used by idempotent adventure rewards.</summary>
            public int RewardCycle;
            /// <summary>Preserves the fifty-bit adventure reward ledger.</summary>
            public long RewardMask;
            /// <summary>Preserves every purchase counter, including unknown slots.</summary>
            public int[] Purchases = Array.Empty<int>();
            /// <summary>Preserves every mode record, including unknown slots.</summary>
            public int[] ModeRecords = Array.Empty<int>();
            /// <summary>Preserves every garden plant and its stable identity.</summary>
            public GardenData[] Plants = Array.Empty<GardenData>();
            /// <summary>Preserves every receipt, including unknown future keys.</summary>
            public ReceiptData[] Receipts = Array.Empty<ReceiptData>();

            /// <summary>Copies all account fields and nested mutable collections.</summary>
            /// <param name="account">The detached account draft to preserve.</param>
            /// <returns>An independent Unity-serializable account snapshot.</returns>
            internal static AccountData Capture(PlayerAccount account)
            {
                var plants = new GardenData[account._garden.Count];
                for (var index = 0; index < plants.Length; index++)
                {
                    plants[index] = GardenData.Capture(account._garden[index]);
                }

                var receipts = new ReceiptData[account._receipts.Count];
                var receiptIndex = 0;
                foreach (var receipt in account._receipts)
                {
                    receipts[receiptIndex++] = new ReceiptData { Key = receipt.Key, Value = receipt.Value };
                }

                return new AccountData
                {
                    ProfileId = account.ProfileId.ToString("N"),
                    Coins = account.Coins,
                    TreeHeight = account.TreeHeight,
                    SnailAwakeUntilUtc = account.SnailAwakeUntilUtc,
                    GardenGiftClaimed = account.GardenGiftClaimed,
                    MinigamesUnlocked = account.MinigamesUnlocked,
                    PuzzleUnlocked = account.PuzzleUnlocked,
                    SurvivalUnlocked = account.SurvivalUnlocked,
                    PacketUpgradeDeclines = account.PacketUpgradeDeclines,
                    TreeFoodGiftClaimed = account.TreeFoodGiftClaimed,
                    RewardCycle = account._rewardCycle,
                    RewardMask = unchecked((long)account._rewardMask),
                    Purchases = (int[])account._purchases.Clone(),
                    ModeRecords = (int[])account._modeRecords.Clone(),
                    Plants = plants,
                    Receipts = receipts
                };
            }

            /// <summary>Recreates an account draft without publishing any data to its persistent store.</summary>
            /// <returns>A detached account whose mutable collections do not share the recovery snapshot.</returns>
            /// <exception cref="InvalidOperationException">A recovered profile or plant identity is invalid.</exception>
            /// <exception cref="ArgumentException">The recovery snapshot contains duplicate receipt keys.</exception>
            internal PlayerAccount CreateAccount()
            {
                var account = new PlayerAccount
                {
                    ProfileId = ParseIdentity(ProfileId),
                    Coins = Coins,
                    TreeHeight = TreeHeight,
                    SnailAwakeUntilUtc = SnailAwakeUntilUtc,
                    GardenGiftClaimed = GardenGiftClaimed,
                    MinigamesUnlocked = MinigamesUnlocked,
                    PuzzleUnlocked = PuzzleUnlocked,
                    SurvivalUnlocked = SurvivalUnlocked,
                    PacketUpgradeDeclines = PacketUpgradeDeclines,
                    TreeFoodGiftClaimed = TreeFoodGiftClaimed,
                    _rewardCycle = RewardCycle,
                    _rewardMask = unchecked((ulong)RewardMask),
                    _purchases = (int[])Purchases.Clone(),
                    _modeRecords = (int[])ModeRecords.Clone(),
                    _garden = new List<GardenPlant>(Plants.Length),
                    _receipts = new Dictionary<string, int>(Receipts.Length)
                };
                foreach (var plant in Plants)
                {
                    account._garden.Add(plant.CreatePlant());
                }

                foreach (var receipt in Receipts)
                {
                    account._receipts.Add(receipt.Key, receipt.Value);
                }

                return account;
            }
        }

        /// <summary>Preserves every persisted garden plant field across editor serialization.</summary>
        [Serializable]
        private sealed class GardenData
        {
            /// <summary>Preserves the stable garden plant identity.</summary>
            public string Id = string.Empty;
            /// <summary>Preserves the original-format source record index.</summary>
            public int OriginalIndex;
            /// <summary>Preserves the serialized plant kind.</summary>
            public int Kind;
            /// <summary>Preserves the owning garden identifier.</summary>
            public int Garden;
            /// <summary>Preserves the occupied garden slot.</summary>
            public int Slot;
            /// <summary>Preserves the plant's growth stage.</summary>
            public int Growth;
            /// <summary>Preserves completed waterings in this growth stage.</summary>
            public int Waterings;
            /// <summary>Preserves required waterings for each growth stage.</summary>
            public int FeedingsPerGrow;
            /// <summary>Preserves the mature plant's music or pesticide requirement.</summary>
            public int MatureNeed;
            /// <summary>Preserves the next care deadline.</summary>
            public long NextCareUtc;
            /// <summary>Preserves the happiness expiration time.</summary>
            public long HappyUntilUtc;
            /// <summary>Preserves the last watering time.</summary>
            public long LastWaterUtc;
            /// <summary>Preserves the last fulfilled mature need time.</summary>
            public long LastNeedFulfilledUtc;
            /// <summary>Preserves the last fertilizer application time.</summary>
            public long LastFertilizedUtc;
            /// <summary>Preserves the chocolate boost expiration time.</summary>
            public long ChocolateUntilUtc;
            /// <summary>Preserves the coin production clock.</summary>
            public long LastCoinUtc;
            /// <summary>Preserves uncollected coins in internal coin units.</summary>
            public int PendingCoins;
            /// <summary>Preserves the deterministic care cycle counter.</summary>
            public int CareCycle;

            /// <summary>Copies the complete plant record into Unity-serializable fields.</summary>
            /// <param name="plant">The plant draft to preserve.</param>
            /// <returns>An independent serialized plant snapshot.</returns>
            internal static GardenData Capture(GardenPlant plant)
            {
                return new GardenData
                {
                    Id = plant.Id.ToString("N"),
                    OriginalIndex = plant.OriginalIndex,
                    Kind = (int)plant.Kind,
                    Garden = (int)plant.Garden,
                    Slot = plant.Slot,
                    Growth = plant.Growth,
                    Waterings = plant.Waterings,
                    FeedingsPerGrow = plant.FeedingsPerGrow,
                    MatureNeed = (int)plant.MatureNeed,
                    NextCareUtc = plant.NextCareUtc,
                    HappyUntilUtc = plant.HappyUntilUtc,
                    LastWaterUtc = plant.LastWaterUtc,
                    LastNeedFulfilledUtc = plant.LastNeedFulfilledUtc,
                    LastFertilizedUtc = plant.LastFertilizedUtc,
                    ChocolateUntilUtc = plant.ChocolateUntilUtc,
                    LastCoinUtc = plant.LastCoinUtc,
                    PendingCoins = plant.PendingCoins,
                    CareCycle = plant.CareCycle
                };
            }

            /// <summary>Recreates a detached garden plant with its original stable identity.</summary>
            /// <returns>The recovered plant record.</returns>
            /// <exception cref="InvalidOperationException">The recovered plant identity is invalid.</exception>
            internal GardenPlant CreatePlant()
            {
                return new GardenPlant
                {
                    Id = ParseIdentity(Id),
                    OriginalIndex = OriginalIndex,
                    Kind = (PlantKind)Kind,
                    Garden = (GardenKind)Garden,
                    Slot = Slot,
                    Growth = Growth,
                    Waterings = Waterings,
                    FeedingsPerGrow = FeedingsPerGrow,
                    MatureNeed = (GardenNeed)MatureNeed,
                    NextCareUtc = NextCareUtc,
                    HappyUntilUtc = HappyUntilUtc,
                    LastWaterUtc = LastWaterUtc,
                    LastNeedFulfilledUtc = LastNeedFulfilledUtc,
                    LastFertilizedUtc = LastFertilizedUtc,
                    ChocolateUntilUtc = ChocolateUntilUtc,
                    LastCoinUtc = LastCoinUtc,
                    PendingCoins = PendingCoins,
                    CareCycle = CareCycle
                };
            }
        }

        /// <summary>Preserves the complete adventure checkpoint and saved seed loadout.</summary>
        [Serializable]
        private sealed class AdventureData
        {
            /// <summary>Identifies the profile that owns this checkpoint draft.</summary>
            public string ProfileId = string.Empty;
            /// <summary>Preserves the next adventure level.</summary>
            public int NextLevel;
            /// <summary>Preserves the adventure replay cycle.</summary>
            public int Cycle;
            /// <summary>Preserves the saved attempt's packet capacity.</summary>
            public int PacketSlots;
            /// <summary>Preserves the nullable Imitater target as a plant identifier, or -1.</summary>
            public int ImitaterTarget;
            /// <summary>Preserves the checkpoint's reward mask.</summary>
            public long CollectedRewards;
            /// <summary>Preserves the deterministic attempt seed.</summary>
            public int AttemptSeed;
            /// <summary>Preserves the selected loadout in order.</summary>
            public int[] SelectedSeeds = Array.Empty<int>();

            /// <summary>Copies all checkpoint values and the selected loadout.</summary>
            /// <param name="progress">The adventure draft to preserve.</param>
            /// <returns>An independent serialized checkpoint snapshot.</returns>
            internal static AdventureData Capture(AdventureProgress progress)
            {
                var seeds = new int[progress.SelectedSeeds.Count];
                for (var index = 0; index < seeds.Length; index++)
                {
                    seeds[index] = (int)progress.SelectedSeeds[index];
                }

                return new AdventureData
                {
                    ProfileId = progress.ProfileId.ToString("N"),
                    NextLevel = progress.NextLevel,
                    Cycle = progress.Cycle,
                    PacketSlots = progress.PacketSlots,
                    ImitaterTarget = progress.ImitaterTarget.HasValue ? (int)progress.ImitaterTarget.Value : -1,
                    CollectedRewards = unchecked((long)progress.CollectedRewards),
                    AttemptSeed = progress.AttemptSeed,
                    SelectedSeeds = seeds
                };
            }

            /// <summary>Recreates a detached checkpoint without publishing it to its persistent store.</summary>
            /// <returns>The recovered adventure draft with an independent selected loadout.</returns>
            /// <exception cref="InvalidOperationException">The recovered profile identity is invalid.</exception>
            internal AdventureProgress CreateProgress()
            {
                var seeds = new PlantKind[SelectedSeeds.Length];
                for (var index = 0; index < seeds.Length; index++)
                {
                    seeds[index] = (PlantKind)SelectedSeeds[index];
                }

                return new AdventureProgress
                {
                    ProfileId = ParseIdentity(ProfileId),
                    NextLevel = NextLevel,
                    Cycle = Cycle,
                    PacketSlots = PacketSlots,
                    ImitaterTarget = ImitaterTarget < 0 ? (PlantKind?)null : (PlantKind)ImitaterTarget,
                    CollectedRewards = unchecked((ulong)CollectedRewards),
                    AttemptSeed = AttemptSeed,
                    SelectedSeeds = Array.AsReadOnly(seeds)
                };
            }
        }
    }
}
