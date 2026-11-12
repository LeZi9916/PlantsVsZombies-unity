#nullable enable
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlantsVsZombies.Progression
{
    // All monetary changes publish their snapshot only after an atomic, checksummed save.
    /// <summary>Defines player account store.</summary>
    public sealed class PlayerAccountStore
    {
        /// <summary>Stores magic.</summary>
        private static readonly byte[] s_magic = Encoding.ASCII.GetBytes("PVZRACC1");
        /// <summary>Stores path.</summary>
        private readonly string _path;
        /// <summary>Stores gate.</summary>
        private readonly object _gate = new object ();
        /// <summary>Stores saved bytes.</summary>
        private byte[]? _savedBytes;
        /// <summary>Stores records.</summary>
        private Dictionary<Guid, PlayerAccount> _records;
        /// <summary>Initializes player account store.</summary>
        /// <param name = "filePath">The file path.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public PlayerAccountStore(string filePath)
        {
            _path = Path.GetFullPath(filePath);
            if (!string.Equals(Path.GetExtension(_path), ".account", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Use a dedicated .account file.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? throw new ArgumentException("The account file path has no directory.", nameof(filePath)));
            _savedBytes = ReadFile(_path);
            _records = _savedBytes == null ? new Dictionary<Guid, PlayerAccount>() : Decode(_savedBytes);
        }

        /// <summary>Performs get.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting get.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public PlayerAccount Get(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("A profile ID is required.");
            }

            lock (_gate)
            {
                return _records.TryGetValue(id, out var p) ? p.Copy() : new PlayerAccount
                {
                    ProfileId = id
                };
            }
        }

        /// <summary>Performs change.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "change">The change.</param>
        private void Change(Guid id, Action<PlayerAccount> change)
        {
            lock (_gate)
            {
                var p = Get(id);
                change(p);
                Commit(p);
            }
        }

        /// <summary>Performs credit battle coins.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "attempt">The attempt.</param>
        /// <param name = "cumulativeUnits">The cumulative units.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void CreditBattleCoins(Guid id, string attempt, int cumulativeUnits)
        {
            if (string.IsNullOrEmpty(attempt) || attempt.Length > 80 || cumulativeUnits < 0)
            {
                throw new ArgumentException("Invalid battle receipt.");
            }

            lock (_gate)
            {
                var p = Get(id);
                int old;
                p._receipts.TryGetValue("battle:" + attempt, out old);
                if (cumulativeUnits <= old)
                {
                    return;
                }

                AddCoins(p, cumulativeUnits - old);
                p._receipts["battle:" + attempt] = cumulativeUnits;
                TrimReceipts(p);
                Commit(p);
            }
        }

        /// <summary>Performs claim adventure reward.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "cycle">The cycle.</param>
        /// <param name = "level">The level.</param>
        /// <param name = "unusedMowers">The unused mowers.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public void ClaimAdventureReward(Guid id, int cycle, int level, int unusedMowers = 0)
        {
            if (cycle < 1 || level < 1 || level > 50)
            {
                throw new ArgumentOutOfRangeException();
            }

            lock (_gate)
            {
                var p = Get(id);
                if (cycle < p._rewardCycle)
                {
                    return;
                }

                if (cycle > p._rewardCycle)
                {
                    p._rewardCycle = cycle;
                    p._rewardMask = 0;
                }

                var bit = 1UL << (level - 1);
                if ((p._rewardMask & bit) != 0)
                {
                    return;
                }

                p._rewardMask |= bit;
                if (level >= 11 || cycle > 1)
                {
                    AddCoins(p, Math.Max(0, Math.Min(6, unusedMowers)) * 5);
                }

                // First-run plants/tools are earned by campaign progress; later runs award money bags.
                if (cycle > 1)
                {
                    AddCoins(p, level == 50 ? 300 : 50);
                }
                else if (level == 50)
                {
                    AddCoins(p, 300);
                }

                if (level == 34 && cycle == 1)
                {
                    p._receipts["taco:pending"] = 1;
                }

                if (level == 44 && cycle == 1 && !p.GardenGiftClaimed)
                {
                    p._receipts["zen:tutorial"] = 1;
                }

                Commit(p);
            }
        }

        /// <summary>Persists presentation of Dave's original magic taco diamond.</summary>
        /// <param name="id">The player receiving the diamond.</param>
        /// <exception cref="ArgumentException">The player identifier is empty.</exception>
        /// <exception cref="IOException">The updated account could not be saved.</exception>
        /// <exception cref="InvalidOperationException">The account changed externally or cannot be saved.</exception>
        public void PresentTacoReward(Guid id)
        {
            Change(id, account =>
            {
                if (account.HasPendingTacoReward)
                {
                    account._receipts["taco:pending"] = 2;
                }
            });
        }

        /// <summary>Collects the one-time original magic taco diamond atomically.</summary>
        /// <param name="id">The player receiving the diamond.</param>
        /// <returns>Whether a pending diamond was collected.</returns>
        /// <exception cref="ArgumentException">The player identifier is empty.</exception>
        /// <exception cref="IOException">The updated account could not be saved.</exception>
        /// <exception cref="InvalidOperationException">The account changed externally or cannot be saved.</exception>
        public bool CollectTacoReward(Guid id)
        {
            lock (_gate)
            {
                var account = Get(id);
                if (!account.TacoDiamondPresented)
                {
                    return false;
                }

                account._receipts.Remove("taco:pending");
                AddCoins(account, 100);
                Commit(account);
                return true;
            }
        }

        /// <summary>Performs record mode result.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "mode">The mode.</param>
        /// <param name = "score">The score.</param>
        /// <param name = "runId">The run id.</param>
        /// <param name = "rewardUnits">The reward units.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void RecordModeResult(Guid id, int mode, int score, string runId, int rewardUnits)
        {
            if (mode < 0 || mode >= 100 || score < 0 || rewardUnits < 0 || rewardUnits > 1000 || string.IsNullOrEmpty(runId) || runId.Length > 80)
            {
                throw new ArgumentException("Invalid challenge result.");
            }

            lock (_gate)
            {
                var p = Get(id);
                var key = "mode:" + runId;
                if (p._receipts.ContainsKey(key))
                {
                    return;
                }

                p._modeRecords[mode] = Math.Max(score, p._modeRecords[mode]);
                AddCoins(p, rewardUnits);
                p._receipts[key] = 1;
                TrimReceipts(p);
                Commit(p);
            }
        }

        /// <summary>Performs import.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "coins">The coins.</param>
        /// <param name = "purchases">The purchases.</param>
        /// <param name = "challenges">The challenges.</param>
        /// <param name = "treeHeight">The tree height.</param>
        /// <param name = "plants">The plants.</param>
        /// <param name = "minigamesUnlocked">The minigames unlocked.</param>
        /// <param name = "puzzleUnlocked">The puzzle unlocked.</param>
        /// <param name = "survivalUnlocked">The survival unlocked.</param>
        /// <param name="packetUpgradeDeclines">The number of original packet upgrade offers declined.</param>
        /// <param name="treeFoodGiftClaimed">Whether the initial tree food gift has already been claimed.</param>
        /// <param name="gardenGiftClaimed">Whether the original garden introduction was completed, or null to infer it from the plants.</param>
        /// <param name="gardenFertilizerInitialized">Whether the original fertilizer counter was initialized, or null for legacy inference.</param>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        public void Import(Guid id, int coins, int[] purchases, int[] challenges, int treeHeight, IEnumerable<GardenPlant> plants, bool minigamesUnlocked = false, bool puzzleUnlocked = false, bool survivalUnlocked = false, int packetUpgradeDeclines = 0, bool treeFoodGiftClaimed = true, bool? gardenGiftClaimed = null, bool? gardenFertilizerInitialized = null)
        {
            if (packetUpgradeDeclines < 0 || coins < 0 || coins > 99999 || treeHeight < 0 || treeHeight > 10000000 || purchases == null || purchases.Length != 80 || challenges == null || challenges.Length != 100)
            {
                throw new InvalidDataException("Invalid original account.");
            }

            Change(id, p =>
            {
                p.Coins = coins;
                p._purchases = (int[])purchases.Clone();
                p._modeRecords = (int[])challenges.Clone();
                p.TreeHeight = treeHeight;
                p._garden = new List<GardenPlant>();
                foreach (var plant in plants)
                {
                    p._garden.Add(plant.Copy());
                }

                p.GardenGiftClaimed = gardenGiftClaimed ?? p._garden.Count > 0;
                if (gardenFertilizerInitialized.HasValue)
                {
                    p._receipts["zen:fertilizer"] = gardenFertilizerInitialized.Value ? 1 : 0;
                }
                else
                {
                    p._receipts.Remove("zen:fertilizer");
                }
                p.MinigamesUnlocked = minigamesUnlocked;
                p.PuzzleUnlocked = puzzleUnlocked;
                p.SurvivalUnlocked = survivalUnlocked;
                p.PacketUpgradeDeclines = packetUpgradeDeclines;
                p.TreeFoodGiftClaimed = treeFoodGiftClaimed;
            });
        }

        /// <summary>Performs add coins.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "amount">The amount.</param>
        private static void AddCoins(PlayerAccount p, int amount)
        {
            p.Coins = (int)Math.Min(99999L, (long)p.Coins + amount);
        }

        /// <summary>Performs trim receipts.</summary>
        /// <param name = "p">The p.</param>
        private static void TrimReceipts(PlayerAccount p)
        {
            while (p._receipts.Count > 256)
            {
                string? oldest = null;
                foreach (var key in p._receipts.Keys)
                {
                    if (key == "taco:pending" || key == "zen:tutorial" || key == "zen:fertilizer")
                    {
                        continue;
                    }

                    oldest = key;
                    break;
                }

                if (oldest == null)
                {
                    return;
                }

                p._receipts.Remove(oldest);
            }
        }

        /// <summary>Performs commit.</summary>
        /// <param name = "account">The account.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "IOException">The operation cannot satisfy its preconditions.</exception>
        private void Commit(PlayerAccount account)
        {
            var next = new Dictionary<Guid, PlayerAccount>(_records);
            next[account.ProfileId] = account;
            if (next.Count > 1024)
            {
                throw new InvalidOperationException("Too many player accounts.");
            }

            var data = Encode(next);
            var temp = _path + ".tmp-" + Guid.NewGuid().ToString("N");
            using (var lease = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                if (!Same(_savedBytes, ReadFile(_path)))
                {
                    throw new IOException("存档已被另一个游戏进程修改，请重新启动游戏后重试。");
                }

                try
                {
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        file.Write(data, 0, data.Length);
                        file.Flush(true);
                    }

                    if (_savedBytes == null)
                    {
                        File.Move(temp, _path);
                    }
                    else
                    {
                        File.Replace(temp, _path, _path + ".backup", true);
                    }

                    _records = next;
                    _savedBytes = data;
                }
                finally
                {
                    if (File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                }
            }
        }

        /// <summary>Performs read file.</summary>
        /// <param name = "filename">The filename.</param>
        /// <returns>The resulting read file.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static byte[]? ReadFile(string filename)
        {
            try
            {
                if (new FileInfo(filename).Length > 8 * 1024 * 1024)
                {
                    throw new InvalidDataException("Account file is too large.");
                }

                return File.ReadAllBytes(filename);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        /// <summary>Performs same.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting same.</returns>
        private static bool Same(byte[]? a, byte[]? b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }

        /// <summary>Performs encode.</summary>
        /// <param name = "values">The values.</param>
        /// <returns>The resulting encode.</returns>
        private static byte[] Encode(Dictionary<Guid, PlayerAccount> values)
        {
            byte[] body;
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8))
                {
                    w.Write(s_magic);
                    w.Write(2);
                    w.Write(values.Count);
                    var ids = new List<Guid>(values.Keys);
                    ids.Sort();
                    foreach (var id in ids)
                    {
                        var p = values[id];
                        w.Write(id.ToByteArray());
                        w.Write(p.Coins);
                        w.Write(p.TreeHeight);
                        w.Write(p.SnailAwakeUntilUtc);
                        w.Write(p.GardenGiftClaimed);
                        w.Write(p.MinigamesUnlocked);
                        w.Write(p.PuzzleUnlocked);
                        w.Write(p.SurvivalUnlocked);
                        w.Write(p._rewardCycle);
                        w.Write(p._rewardMask);
                        w.Write(p.PacketUpgradeDeclines);
                        w.Write(p.TreeFoodGiftClaimed);
                        foreach (int n in p._purchases)
                        {
                            w.Write(n);
                        }

                        foreach (int n in p._modeRecords)
                        {
                            w.Write(n);
                        }

                        w.Write(p._garden.Count);
                        foreach (var g in p._garden)
                        {
                            w.Write(g.Id.ToByteArray());
                            w.Write(g.OriginalIndex);
                            w.Write((int)g.Kind);
                            w.Write((int)g.Garden);
                            w.Write(g.Slot);
                            w.Write(g.Growth);
                            w.Write(g.Waterings);
                            w.Write(g.FeedingsPerGrow);
                            w.Write((int)g.MatureNeed);
                            w.Write(g.NextCareUtc);
                            w.Write(g.HappyUntilUtc);
                            w.Write(g.LastWaterUtc);
                            w.Write(g.LastNeedFulfilledUtc);
                            w.Write(g.LastFertilizedUtc);
                            w.Write(g.ChocolateUntilUtc);
                            w.Write(g.LastCoinUtc);
                            w.Write(g.PendingCoins);
                            w.Write(g.CareCycle);
                        }

                        w.Write(p._receipts.Count);
                        foreach (var r in p._receipts)
                        {
                            w.Write(r.Key);
                            w.Write(r.Value);
                        }
                    }

                    w.Flush();
                    body = stream.ToArray();
                }
            }

            using (var sha = SHA256.Create())
            {
                using (var stream = new MemoryStream())
                {
                    stream.Write(body, 0, body.Length);
                    var sum = sha.ComputeHash(body);
                    stream.Write(sum, 0, sum.Length);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>Performs decode.</summary>
        /// <param name = "bytes">The bytes.</param>
        /// <returns>The resulting decode.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static Dictionary<Guid, PlayerAccount> Decode(byte[] bytes)
        {
            if (bytes.Length < 48)
            {
                throw new InvalidDataException("Truncated account file.");
            }

            var body = new byte[bytes.Length - 32];
            var sum = new byte[32];
            Array.Copy(bytes, body, body.Length);
            Array.Copy(bytes, body.Length, sum, 0, 32);
            using (var sha = SHA256.Create())
            {
                if (!Same(sum, sha.ComputeHash(body)))
                {
                    throw new InvalidDataException("金币档案校验失败；请保留文件及 .backup 备份。");
                }
            }

            try
            {
                using (var stream = new MemoryStream(body))
                {
                    using (var r = new BinaryReader(stream, Encoding.UTF8))
                    {
                        if (!Same(s_magic, r.ReadBytes(8)))
                        {
                            throw new InvalidDataException("Unsupported account format.");
                        }

                        var version = r.ReadInt32();
                        if (version != 1 && version != 2)
                        {
                            throw new InvalidDataException("Unsupported account format.");
                        }

                        var count = Bound(r.ReadInt32(), 0, 1024);
                        var result = new Dictionary<Guid, PlayerAccount>();
                        for (var i = 0; i < count; i++)
                        {
                            var p = new PlayerAccount
                            {
                                ProfileId = new Guid(r.ReadBytes(16)),
                                Coins = Bound(r.ReadInt32(), 0, 99999),
                                TreeHeight = Bound(r.ReadInt32(), 0, 10000000),
                                SnailAwakeUntilUtc = r.ReadInt64(),
                                GardenGiftClaimed = r.ReadBoolean(),
                                MinigamesUnlocked = r.ReadBoolean(),
                                PuzzleUnlocked = r.ReadBoolean(),
                                SurvivalUnlocked = r.ReadBoolean(),
                                _rewardCycle = Bound(r.ReadInt32(), 0, 1000000),
                                _rewardMask = r.ReadUInt64()
                            };
                            if (version >= 2)
                            {
                                p.PacketUpgradeDeclines = Bound(r.ReadInt32(), 0, int.MaxValue);
                                p.TreeFoodGiftClaimed = r.ReadBoolean();
                            }

                            if (p.ProfileId == Guid.Empty || result.ContainsKey(p.ProfileId) || p._rewardMask >= 1UL << 50)
                            {
                                throw new InvalidDataException("Invalid account identity or rewards.");
                            }

                            for (var j = 0; j < 80; j++)
                            {
                                p._purchases[j] = Bound(r.ReadInt32(), 0, int.MaxValue);
                            }

                            for (var j = 0; j < 100; j++)
                            {
                                p._modeRecords[j] = Bound(r.ReadInt32(), 0, int.MaxValue);
                            }

                            var plants = Bound(r.ReadInt32(), 0, 200);
                            var plantIds = new HashSet<Guid>();
                            var slots = new HashSet<int>();
                            for (var j = 0; j < plants; j++)
                            {
                                var g = new GardenPlant
                                {
                                    Id = new Guid(r.ReadBytes(16)),
                                    OriginalIndex = Bound(r.ReadInt32(), -1, 199),
                                    Kind = (PlantKind)Bound(r.ReadInt32(), 0, 48),
                                    Garden = (GardenKind)Bound(r.ReadInt32(), 0, 2),
                                    Slot = Bound(r.ReadInt32(), 0, 31),
                                    Growth = Bound(r.ReadInt32(), 0, 3),
                                    Waterings = Bound(r.ReadInt32(), 0, 5),
                                    FeedingsPerGrow = Bound(r.ReadInt32(), 3, 5),
                                    MatureNeed = (GardenNeed)Bound(r.ReadInt32(), 2, 3),
                                    NextCareUtc = r.ReadInt64(),
                                    HappyUntilUtc = r.ReadInt64(),
                                    LastWaterUtc = r.ReadInt64(),
                                    LastNeedFulfilledUtc = r.ReadInt64(),
                                    LastFertilizedUtc = r.ReadInt64(),
                                    ChocolateUntilUtc = r.ReadInt64(),
                                    LastCoinUtc = r.ReadInt64(),
                                    PendingCoins = Bound(r.ReadInt32(), 0, 99999),
                                    CareCycle = Bound(r.ReadInt32(), 0, int.MaxValue)
                                };
                                if (g.Id == Guid.Empty || !plantIds.Add(g.Id) || !slots.Add((int)g.Garden * 32 + g.Slot))
                                {
                                    throw new InvalidDataException("Invalid garden layout.");
                                }

                                p._garden.Add(g);
                            }

                            var receipts = Bound(r.ReadInt32(), 0, 256);
                            for (var j = 0; j < receipts; j++)
                            {
                                var key = r.ReadString();
                                if (key.Length > 90 || p._receipts.ContainsKey(key))
                                {
                                    throw new InvalidDataException("Invalid receipt.");
                                }

                                p._receipts.Add(key, Bound(r.ReadInt32(), 0, int.MaxValue));
                            }

                            result.Add(p.ProfileId, p);
                        }

                        if (stream.Position != stream.Length)
                        {
                            throw new InvalidDataException("Trailing account data.");
                        }

                        return result;
                    }
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException("Truncated account file.", e);
            }
        }

        /// <summary>Performs bound.</summary>
        /// <param name = "n">The n.</param>
        /// <param name = "min">The min.</param>
        /// <param name = "max">The max.</param>
        /// <returns>The resulting bound.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static int Bound(int n, int min, int max)
        {
            if (n < min || n > max)
            {
                throw new InvalidDataException("Account value out of range.");
            }

            return n;
        }

        /// <summary>Records the player's answer to an eligible adventure packet upgrade offer atomically.</summary>
        /// <param name="id">The profile receiving the offer.</param>
        /// <param name="accepted">Whether the player accepts the offer.</param>
        /// <exception cref="InvalidOperationException">The offer is no longer eligible.</exception>
        /// <exception cref="IOException">The account could not be persisted.</exception>
        public void ResolveAdventurePacketOffer(Guid id, bool accepted)
        {
            Change(id, account =>
            {
                var purchases = account.PurchaseCount(21);
                var price = ShopCatalog.Price(ShopCatalog.Get(21), account);
                if (purchases > 1 || account.PacketUpgradeDeclines >= 2 || account.Coins < price)
                {
                    throw new InvalidOperationException("The packet upgrade offer is no longer available.");
                }

                if (accepted)
                {
                    account.Coins -= price;
                    account._purchases[21]++;
                }
                else
                {
                    account.PacketUpgradeDeclines++;
                }
            });
        }

        /// <summary>Performs purchase.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "itemId">The item id.</param>
        /// <param name = "nextLevel">The next level.</param>
        /// <param name = "completedRuns">The completed runs.</param>
        /// <param name = "utc">The utc.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void Purchase(Guid id, int itemId, int nextLevel, int completedRuns, long utc)
        {
            var item = ShopCatalog.Get(itemId);
            Change(id, p =>
            {
                if (!ShopCatalog.IsUnlocked(item, nextLevel, completedRuns))
                {
                    throw new InvalidOperationException("此商品尚未解锁。");
                }

                if (ShopCatalog.SoldOut(item, p, utc))
                {
                    throw new InvalidOperationException("该商品已售完。");
                }

                if (itemId == 19 && p.PurchaseCount(18) == 0 && p.PurchaseCount(25) == 0)
                {
                    throw new InvalidOperationException("购买其他花园后才能使用手推车。");
                }

                if (itemId == 28 && p.PurchaseCount(27) == 0)
                {
                    throw new InvalidOperationException("请先购买智慧树。");
                }

                var price = ShopCatalog.Price(item, p);
                if (p.Coins < price)
                {
                    throw new InvalidOperationException("金币不足。");
                }

                p.Coins -= price;
                if (itemId >= 10 && itemId <= 12)
                {
                    AddGardenPlant(p, PlantKind.Marigold, utc);
                    p._purchases[itemId] = (int)(utc / 86400);
                }
                else if (itemId == 14 || itemId == 15)
                {
                    p._purchases[itemId] += 5;
                    if (itemId == 14)
                    {
                        p._receipts["zen:fertilizer"] = 1;
                    }
                }
                else if (itemId == 24)
                {
                    p._purchases[itemId] = 3;
                }
                else
                {
                    p._purchases[itemId]++;
                }

                if (itemId == 27)
                {
                    p.TreeHeight = 1;
                    p.TreeFoodGiftClaimed = false;
                }
            });
        }

        /// <summary>Records that the first Zen Garden tutorial has started.</summary>
        /// <param name="id">The player starting the tutorial.</param>
        public void BeginZenGardenTutorial(Guid id)
        {
            Change(id, p =>
            {
                if (!p.GardenGiftClaimed && p.ZenGardenTutorialStage == 0)
                {
                    p._receipts["zen:tutorial"] = 1;
                }
            });
        }

        /// <summary>Persists the current original garden tutorial step.</summary>
        /// <param name="id">The player performing the step.</param>
        /// <param name="stage">The one-based tutorial step, from two through eight.</param>
        /// <exception cref="ArgumentOutOfRangeException">The stage lies outside the supported tutorial steps.</exception>
        public void SetZenGardenTutorialStage(Guid id, int stage)
        {
            if (stage < 2 || stage > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            Change(id, p => p._receipts["zen:tutorial"] = stage);
        }

        /// <summary>Clears the persisted garden tutorial after returning to adventure.</summary>
        /// <param name="id">The player who completed the tutorial.</param>
        public void FinishZenGardenTutorial(Guid id)
        {
            Change(id, p => p._receipts.Remove("zen:tutorial"));
        }

        /// <summary>Introduces the two original garden seedlings if no potted plants exist.</summary>
        /// <param name="id">The player receiving the seedlings.</param>
        /// <param name="utc">The current UTC time in seconds.</param>
        public void GrantZenGardenIntroPlants(Guid id, long utc)
        {
            Change(id, p =>
            {
                if (p._garden.Count == 0)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        AddGardenPlant(p, PlantKind.Marigold, utc);
                        p._garden[p._garden.Count - 1].FeedingsPerGrow = 3;
                    }
                }
            });
        }

        /// <summary>Provides the five fertilizer bags from the original garden tutorial store visit.</summary>
        /// <param name="id">The player receiving the fertilizer.</param>
        /// <param name="startsVisit">Whether the initial tutorial store conversation is starting.</param>
        public void GrantZenGardenTutorialFertilizer(Guid id, bool startsVisit = true)
        {
            Change(id, p =>
            {
                p._purchases[14] = 5;
                p._receipts["zen:fertilizer"] = 1;
                if (startsVisit)
                {
                    p._receipts["zen:tutorial"] = 8;
                }
            });
        }

        /// <summary>Records that Dave's first garden introduction has been completed.</summary>
        /// <param name="id">The player who completed the introduction.</param>
        public void CompleteZenGardenIntroduction(Guid id)
        {
            Change(id, p =>
            {
                p.GardenGiftClaimed = true;
                p._receipts["zen:tutorial"] = 2;
            });
        }

        /// <summary>Provides Dave's original first-visit tree food once.</summary>
        /// <param name="id">The player receiving the five bags.</param>
        /// <exception cref="InvalidOperationException">The player has not purchased the tree.</exception>
        public void ClaimWisdomTreeFoodGift(Guid id)
        {
            Change(id, p =>
            {
                if (p.PurchaseCount(27) == 0)
                {
                    throw new InvalidOperationException("尚未种下智慧树。");
                }

                if (!p.TreeFoodGiftClaimed)
                {
                    p._purchases[28] = 5;
                    p.TreeFoodGiftClaimed = true;
                }
            });
        }

        /// <summary>Performs feed wisdom tree.</summary>
        /// <param name = "id">The id.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void FeedWisdomTree(Guid id)
        {
            Change(id, p =>
            {
                if (p.PurchaseCount(27) == 0)
                {
                    throw new InvalidOperationException("尚未种下智慧树。");
                }

                if (p._purchases[28] <= 0)
                {
                    throw new InvalidOperationException("没有树肥，请到商店购买。");
                }

                if (p.TreeHeight >= 10000000)
                {
                    throw new InvalidOperationException("智慧树已经长得足够高了。");
                }

                p._purchases[28]--;
                p.TreeHeight++;
            });
        }

        /// <summary>Performs consume rake.</summary>
        /// <param name = "id">The id.</param>
        public void ConsumeRake(Guid id)
        {
            Change(id, p =>
            {
                if (p._purchases[24] > 0)
                {
                    p._purchases[24]--;
                }
            });
        }

        /// <summary>Performs garden random.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "utc">The utc.</param>
        /// <returns>The resulting garden random.</returns>
        private static OriginalRandom GardenRandom(GardenPlant plant, long utc)
        {
            return new OriginalRandom(unchecked((uint)plant.Id.GetHashCode() ^ (uint)utc ^ (uint)plant.CareCycle));
        }

        /// <summary>Performs empty garden slot.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "garden">The garden.</param>
        /// <returns>The resulting empty garden slot.</returns>
        private static int EmptyGardenSlot(PlayerAccount p, GardenKind garden)
        {
            var capacity = garden == GardenKind.Greenhouse ? 32 : 8;
            for (var slot = 0; slot < capacity; slot++)
            {
                if (!p._garden.Exists(g => g.Garden == garden && g.Slot == slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        /// <summary>Performs add garden plant.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "utc">The utc.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private static void AddGardenPlant(PlayerAccount p, PlantKind kind, long utc)
        {
            var slot = EmptyGardenSlot(p, GardenKind.Greenhouse);
            if (slot < 0)
            {
                throw new InvalidOperationException("禅境花园已满。");
            }

            var g = new GardenPlant
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Slot = slot,
                LastCoinUtc = utc
            };
            g.FeedingsPerGrow = 3 + (int)GardenRandom(g, utc).NextBounded(3);
            p._garden.Add(g);
        }

        /// <summary>Performs collect mode unlock.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "receipt">The receipt.</param>
        /// <param name = "puzzle">The puzzle.</param>
        public void CollectModeUnlock(Guid id, string receipt, bool puzzle)
        {
            Change(id, p =>
            {
                if (puzzle)
                {
                    p.PuzzleUnlocked = true;
                }
                else
                {
                    p.MinigamesUnlocked = true;
                }
            });
        }

        /// <summary>Performs collect battle loot.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "receipt">The receipt.</param>
        /// <param name = "chocolate">The chocolate.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "utc">The utc.</param>
        public void CollectBattleLoot(Guid id, string receipt, bool chocolate, PlantKind kind, long utc)
        {
            lock (_gate)
            {
                var p = Get(id);
                var key = "gift:" + receipt;
                if (p._receipts.ContainsKey(key))
                {
                    return;
                }

                if (chocolate)
                {
                    p._purchases[26] = Math.Min(10, p._purchases[26] + 1);
                }
                else
                {
                    AddGardenPlant(p, kind, utc);
                }

                p._receipts[key] = 1;
                TrimReceipts(p);
                Commit(p);
            }
        }

        /// <summary>Performs give garden plant.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "utc">The utc.</param>
        /// <param name = "receipt">The receipt.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void GiveGardenPlant(Guid id, PlantKind kind, long utc, string receipt)
        {
            if ((int)kind < 0 || (int)kind > 39 || string.IsNullOrEmpty(receipt) || receipt.Length > 80)
            {
                throw new ArgumentException();
            }

            lock (_gate)
            {
                var p = Get(id);
                var key = "gift:" + receipt;
                if (p._receipts.ContainsKey(key))
                {
                    return;
                }

                AddGardenPlant(p, kind, utc);
                p._receipts[key] = 1;
                TrimReceipts(p);
                Commit(p);
            }
        }

        /// <summary>Performs tend garden.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "plantId">The plant id.</param>
        /// <param name = "tool">The tool.</param>
        /// <param name = "utc">The utc.</param>
        /// <param name = "target">The target.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void TendGarden(Guid id, Guid plantId, GardenTool tool, long utc, GardenKind target = GardenKind.Greenhouse)
        {
            Change(id, p =>
            {
                var g = p._garden.Find(x => x.Id == plantId);
                if (g == null)
                {
                    throw new InvalidOperationException("找不到这株植物。");
                }

                if (tool == GardenTool.Collect)
                {
                    AddCoins(p, g.PendingCoins);
                    g.PendingCoins = 0;
                    return;
                }

                if (tool == GardenTool.Sell)
                {
                    AddCoins(p, SellPrice(g));
                    p._garden.Remove(g);
                    return;
                }

                if (tool == GardenTool.Move)
                {
                    if (p.PurchaseCount(17) == 0)
                    {
                        throw new InvalidOperationException("请先购买园艺手套。");
                    }

                    if (target != g.Garden && p.PurchaseCount(19) == 0)
                    {
                        throw new InvalidOperationException("移动到其他花园需要手推车。");
                    }

                    if (target == GardenKind.Mushroom && p.PurchaseCount(18) == 0 || target == GardenKind.Aquarium && p.PurchaseCount(25) == 0)
                    {
                        throw new InvalidOperationException("尚未购买这个花园。");
                    }

                    var slot = EmptyGardenSlot(p, target);
                    if (slot < 0)
                    {
                        throw new InvalidOperationException("目标花园已满。");
                    }

                    g.Garden = target;
                    g.Slot = slot;
                    return;
                }

                var need = g.Need(utc);
                if (tool == GardenTool.Chocolate)
                {
                    if (g.Growth != 3 || need != GardenNeed.Happy || utc < g.ChocolateUntilUtc)
                    {
                        throw new InvalidOperationException("只可给心满意足的成株喂巧克力。");
                    }

                    if (p._purchases[26] <= 0)
                    {
                        throw new InvalidOperationException("没有巧克力。");
                    }

                    p._purchases[26]--;
                    g.ChocolateUntilUtc = utc + 300;
                    g.LastCoinUtc = utc + 2;
                    return;
                }

                if (tool == GardenTool.Water && need == GardenNeed.Water)
                {
                    var watered = new List<GardenPlant>
                    {
                        g
                    };
                    if (p.PurchaseCount(13) > 0)
                    {
                        foreach (var other in p._garden)
                        {
                            if (other != g && other.Garden == g.Garden && Math.Abs(other.Slot % 8 - g.Slot % 8) <= 1 && Math.Abs(other.Slot / 8 - g.Slot / 8) <= 1 && other.Need(utc) == GardenNeed.Water)
                            {
                                watered.Add(other);
                            }
                        }
                    }

                    foreach (var plant in watered)
                    {
                        plant.Waterings++;
                        plant.LastWaterUtc = utc - (int)GardenRandom(plant, utc).NextBounded(10);
                        plant.NextCareUtc = plant.LastWaterUtc + 16;
                        if (plant.Growth == 3 && plant.Waterings >= plant.FeedingsPerGrow)
                        {
                            plant.MatureNeed = GardenRandom(plant, utc).NextBounded(2) == 0 ? GardenNeed.BugSpray : GardenNeed.Music;
                        }
                    }

                    return;
                }

                if (tool == GardenTool.Fertilizer && need == GardenNeed.Fertilizer)
                {
                    if (p._purchases[14] <= 0)
                    {
                        throw new InvalidOperationException("肥料已用完。");
                    }

                    p._purchases[14]--;
                    g.Growth++;
                    g.Waterings = 0;
                    g.LastFertilizedUtc = utc;
                    g.NextCareUtc = utc + 3600;
                    g.PendingCoins += g.Growth == 1 ? 5 : g.Growth == 2 ? 10 : g.Kind == PlantKind.Marigold ? 100 : 200;
                    return;
                }

                if (tool == GardenTool.BugSpray && need == GardenNeed.BugSpray || tool == GardenTool.Music && need == GardenNeed.Music)
                {
                    var item = tool == GardenTool.BugSpray ? 15 : 16;
                    if (p._purchases[item] <= 0)
                    {
                        throw new InvalidOperationException(tool == GardenTool.BugSpray ? "杀虫剂已用完。" : "请先购买留声机。");
                    }

                    if (item == 15)
                    {
                        p._purchases[item]--;
                    }

                    g.LastNeedFulfilledUtc = utc;
                    g.HappyUntilUtc = utc + 86400;
                    g.LastCoinUtc = utc + 18 + (int)GardenRandom(g, utc).NextBounded(13);
                    g.Waterings = 0;
                    g.CareCycle++;
                    g.PendingCoins += g.Kind == PlantKind.Marigold ? 5 : 15;
                    return;
                }

                throw new InvalidOperationException(need == GardenNeed.Resting ? "植物暂时不需要照料。" : "请按植物的需求选择工具。");
            });
        }

        // Called only while the garden is open. Production does not simulate coins while the game is closed.
        /// <summary>Performs update garden.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "utc">The utc.</param>
        /// <param name = "entering">The entering.</param>
        public void UpdateGarden(Guid id, long utc, bool entering = false)
        {
            lock (_gate)
            {
                var p = Get(id);
                var changed = false;
                foreach (var g in p._garden)
                {
                    if (g.Growth != 3 || !g.InPreferredGarden || g.Need(utc) != GardenNeed.Happy)
                    {
                        continue;
                    }

                    if (entering)
                    {
                        g.LastCoinUtc = utc + 18 + (int)GardenRandom(g, utc).NextBounded(13);
                        changed = true;
                        continue;
                    }

                    if (utc < g.LastCoinUtc)
                    {
                        continue;
                    }

                    var rng = GardenRandom(g, utc);
                    var happyMinutes = Math.Max(0, (utc - g.LastNeedFulfilledUtc) / 60);
                    var rarity = (int)Math.Min(80, Math.Max(0, happyMinutes - 5) * 80 / 25);
                    g.PendingCoins += rng.NextBounded(1000) + rarity < 100 ? 5 : 1;
                    var maximum = (int)(3000 + Math.Min(25, Math.Max(0, happyMinutes - 5)) * 480);
                    var seconds = 18 + (int)rng.NextBounded((uint)Math.Max(1, maximum / 100 - 17));
                    g.LastCoinUtc = utc + (g.ChocolateUntilUtc > utc ? Math.Max(1, seconds / 2) : seconds);
                    changed = true;
                }

                if (p.PurchaseCount(20) > 0 && p.SnailAwakeUntilUtc > utc)
                {
                    foreach (var g in p._garden)
                    {
                        if (g.PendingCoins > 0)
                        {
                            AddCoins(p, g.PendingCoins);
                            g.PendingCoins = 0;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    Commit(p);
                }
            }
        }

        /// <summary>Performs wake snail.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "utc">The utc.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void WakeSnail(Guid id, long utc)
        {
            Change(id, p =>
            {
                if (p.PurchaseCount(20) == 0)
                {
                    throw new InvalidOperationException("尚未购买蜗牛。");
                }

                p.SnailAwakeUntilUtc = utc + 180;
            });
        }

        /// <summary>Performs sell price.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting sell price.</returns>
        public static int SellPrice(GardenPlant plant)
        {
            if (plant.Kind == PlantKind.Marigold)
            {
                return new[]
                {
                    150,
                    200,
                    250,
                    300
                }[plant.Growth];
            }

            if (plant.Growth < 3)
            {
                return new[]
                {
                    150,
                    300,
                    500
                }[plant.Growth];
            }

            return GardenPlant.IsMushroom(plant.Kind) || GardenPlant.IsAquatic(plant.Kind) ? 1000 : 800;
        }
    }
}
