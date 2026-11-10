#nullable enable

using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Separate replica-only sidecar keyed by the unchanged v1 ProfileCatalog GUID. Does not open original user files.</summary>
    public sealed class AdventureProgressStore
    {
        /// <summary>Stores magic.</summary>
        private static readonly byte[] s_magic = Encoding.ASCII.GetBytes("PVZRADV1");
        /// <summary>Stores path.</summary>
        private readonly string _path;
        /// <summary>Stores saved bytes.</summary>
        private byte[] _savedBytes;
        /// <summary>Stores records.</summary>
        private Dictionary<Guid, AdventureProgress> _records;
        /// <summary>Stores gate.</summary>
        private readonly object _gate = new object ();
        /// <summary>Initializes adventure progress store.</summary>
        /// <param name = "filePath">The file path.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "DirectoryNotFoundException">The operation cannot satisfy its preconditions.</exception>
        public AdventureProgressStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A replica progress path is required.");
            }

            _path = Path.GetFullPath(filePath);
            if (!string.Equals(Path.GetExtension(_path), ".adventure", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Use a dedicated .adventure sidecar, never an original .dat or profile file.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(_path)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(_path));
            }

            _savedBytes = ReadOrMissing(_path);
            _records = _savedBytes == null ? new Dictionary<Guid, AdventureProgress>() : Decode(_savedBytes);
        }

        /// <summary>Performs get.</summary>
        /// <param name = "profileId">The profile id.</param>
        /// <returns>The resulting get.</returns>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public AdventureProgress Get(Guid profileId)
        {
            if (profileId == Guid.Empty)
            {
                throw new ArgumentException("A profile GUID is required.");
            }

            lock (_gate)
            {
                AdventureProgress p;
                return _records.TryGetValue(profileId, out p) ? p.Copy() : new AdventureProgress
                {
                    ProfileId = profileId
                };
            }
        }

        /// <summary>Performs save attempt.</summary>
        /// <param name = "profileId">The profile id.</param>
        /// <param name = "seed">The seed.</param>
        /// <param name = "selected">The selected.</param>
        /// <param name = "access">The access.</param>
        /// <param name = "slots">The slots.</param>
        /// <param name = "imitater">The imitater.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void SaveAttempt(Guid profileId, int seed, IReadOnlyList<PlantKind> selected, AdventureSeedAccess access = null, int slots = 6, PlantKind? imitater = null)
        {
            lock (_gate)
            {
                var p = Get(profileId);
                p.AttemptSeed = seed;
                if (selected == null)
                {
                    throw new ArgumentNullException("selected");
                }

                var loadout = new List<PlantKind>();
                var level = AdventureLevels.ForRun(p.NextLevel, p.Cycle, slots);
                var available = new List<PlantKind>(level.UnlockedSeeds);
                if (access != null)
                {
                    available.AddRange(access.AdditionalSeeds);
                }

                foreach (var kind in selected)
                {
                    if (loadout.Contains(kind) || !Has(available, kind))
                    {
                        throw new ArgumentException("Invalid or unearned loadout.");
                    }

                    loadout.Add(kind);
                }

                if (loadout.Count > level.PacketSlots || level.ChoosesSeeds && loadout.Count != 0 && loadout.Count != Math.Min(level.PacketSlots, available.Count))
                {
                    throw new ArgumentException("A selected loadout must fill the available packet slots.");
                }

                if (imitater.HasValue && (!Has(available, PlantKind.Imitater) || !Has(level.UnlockedSeeds, imitater.Value)))
                {
                    throw new ArgumentException("Invalid Imitater target.");
                }

                p.SelectedSeeds = loadout.AsReadOnly();
                p.PacketSlots = slots;
                p.ImitaterTarget = imitater;
                Commit(p);
            }
        }

        /// <summary>Performs record reward.</summary>
        /// <param name = "profileId">The profile id.</param>
        /// <param name = "level">The level.</param>
        /// <returns>The resulting record reward.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal bool RecordReward(Guid profileId, int level)
        {
            lock (_gate)
            {
                var p = Get(profileId);
                if (level < 1 || level > 50)
                {
                    throw new ArgumentOutOfRangeException("level");
                }

                var bit = 1UL << (level - 1);
                if ((p.CollectedRewards & bit) != 0)
                {
                    return false;
                }

                if (level != p.NextLevel)
                {
                    throw new InvalidOperationException("Rewards must be collected in adventure order.");
                }

                p.CollectedRewards |= bit;
                p.NextLevel = Math.Min(50, level + 1);
                p.SelectedSeeds = Array.AsReadOnly(new PlantKind[0]);
                Commit(p);
                return true;
            }
        }

        /// <summary>Performs start next cycle.</summary>
        /// <param name = "profileId">The profile id.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void StartNextCycle(Guid profileId)
        {
            lock (_gate)
            {
                var p = Get(profileId);
                if (!p.Completed)
                {
                    throw new InvalidOperationException("Complete the current adventure first.");
                }

                p.Cycle++;
                p.NextLevel = 1;
                p.CollectedRewards = 0;
                p.SelectedSeeds = Array.AsReadOnly(new PlantKind[0]);
                p.ImitaterTarget = null;
                Commit(p);
            }
        }

        /// <summary>Performs import.</summary>
        /// <param name = "profileId">The profile id.</param>
        /// <param name = "level">The level.</param>
        /// <param name = "completedRuns">The completed runs.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void Import(Guid profileId, int level, int completedRuns)
        {
            if (level < 1 || level > 50 || completedRuns < 0 || completedRuns > 999999)
            {
                throw new ArgumentException("Invalid original adventure checkpoint.");
            }

            lock (_gate)
            {
                var p = Get(profileId);
                p.NextLevel = level;
                p.Cycle = completedRuns + 1;
                p.CollectedRewards = (1UL << (level - 1)) - 1;
                p.SelectedSeeds = Array.AsReadOnly(new PlantKind[0]);
                p.ImitaterTarget = null;
                Commit(p);
            }
        }

        /// <summary>Performs commit.</summary>
        /// <param name = "changed">The changed.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "IOException">The operation cannot satisfy its preconditions.</exception>
        private void Commit(AdventureProgress changed)
        {
            var next = new Dictionary<Guid, AdventureProgress>(_records);
            next[changed.ProfileId] = changed;
            if (next.Count > 1024)
            {
                throw new InvalidOperationException("The adventure profile limit has been reached.");
            }

            var bytes = Encode(next);
            string temp = null;
            using (var lease = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                if (!Same(_savedBytes, ReadOrMissing(_path)))
                {
                    throw new IOException("Adventure progress changed in another session; reopen before saving.");
                }

                try
                {
                    temp = _path + ".tmp-" + Guid.NewGuid().ToString("N");
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        file.Write(bytes, 0, bytes.Length);
                        file.Flush(true);
                    }

                    if (_savedBytes == null)
                    {
                        File.Move(temp, _path);
                    }
                    else
                    {
                        File.Replace(temp, _path, _path + ".backup-" + Guid.NewGuid().ToString("N"));
                    }

                    temp = null;
                    _records = next;
                    _savedBytes = bytes;
                }
                finally
                {
                    if (temp != null && File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                }
            }
        }

        /// <summary>Performs has.</summary>
        /// <param name = "values">The values.</param>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting has.</returns>
        private static bool Has(IReadOnlyList<PlantKind> values, PlantKind kind)
        {
            foreach (var v in values)
            {
                if (v == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs same.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting same.</returns>
        private static bool Same(byte[] a, byte[] b)
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

        /// <summary>Performs read or missing.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting read or missing.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static byte[] ReadOrMissing(string p)
        {
            try
            {
                var info = new FileInfo(p);
                if (info.Length > 1024 * 1024)
                {
                    throw new InvalidDataException("Oversized progress file.");
                }

                return File.ReadAllBytes(p);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        /// <summary>Performs encode.</summary>
        /// <param name = "items">The items.</param>
        /// <returns>The resulting encode.</returns>
        private static byte[] Encode(Dictionary<Guid, AdventureProgress> items)
        {
            byte[] body;
            using (var m = new MemoryStream())
            {
                using (var w = new BinaryWriter(m, Encoding.UTF8))
                {
                    w.Write(s_magic);
                    w.Write(2);
                    w.Write(items.Count);
                    var ids = new List<Guid>(items.Keys);
                    ids.Sort();
                    foreach (var id in ids)
                    {
                        var p = items[id];
                        w.Write(id.ToByteArray());
                        w.Write(p.NextLevel);
                        w.Write(p.CollectedRewards);
                        w.Write(p.AttemptSeed);
                        w.Write(p.SelectedSeeds.Count);
                        foreach (var seed in p.SelectedSeeds)
                        {
                            w.Write((int)seed);
                        }

                        w.Write(p.Cycle);
                        w.Write(p.PacketSlots);
                        w.Write(p.ImitaterTarget.HasValue ? (int)p.ImitaterTarget.Value : -1);
                    }

                    w.Flush();
                    body = m.ToArray();
                }
            }

            using (var sha = SHA256.Create())
            {
                using (var m = new MemoryStream())
                {
                    m.Write(body, 0, body.Length);
                    var sum = sha.ComputeHash(body);
                    m.Write(sum, 0, sum.Length);
                    return m.ToArray();
                }
            }
        }

        /// <summary>Performs decode.</summary>
        /// <param name = "bytes">The bytes.</param>
        /// <returns>The resulting decode.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static Dictionary<Guid, AdventureProgress> Decode(byte[] bytes)
        {
            if (bytes.Length < 48 || bytes.Length > 1024 * 1024)
            {
                throw new InvalidDataException("Invalid replica adventure file.");
            }

            byte[] body = new byte[bytes.Length - 32], checksum = new byte[32];
            Array.Copy(bytes, body, body.Length);
            Array.Copy(bytes, body.Length, checksum, 0, 32);
            using (var sha = SHA256.Create())
            {
                if (!Same(checksum, sha.ComputeHash(body)))
                {
                    throw new InvalidDataException("Adventure checksum mismatch; existing backups are preserved.");
                }
            }

            try
            {
                using (var m = new MemoryStream(body))
                {
                    using (var r = new BinaryReader(m, Encoding.UTF8))
                    {
                        if (!Same(s_magic, r.ReadBytes(s_magic.Length)))
                        {
                            throw new InvalidDataException("Unsupported adventure format.");
                        }

                        var version = r.ReadInt32();
                        if (version != 1 && version != 2)
                        {
                            throw new InvalidDataException("Unsupported adventure version.");
                        }

                        var count = r.ReadInt32();
                        if (count < 0 || count > 1024)
                        {
                            throw new InvalidDataException("Invalid profile count.");
                        }

                        var result = new Dictionary<Guid, AdventureProgress>();
                        for (var i = 0; i < count; i++)
                        {
                            var id = new Guid(r.ReadBytes(16));
                            var level = r.ReadInt32();
                            var mask = r.ReadUInt64();
                            int seed = r.ReadInt32(), cards = r.ReadInt32();
                            var valid = (1UL << 50) - 1;
                            if (id == Guid.Empty || result.ContainsKey(id) || level < 1 || level > 50 || cards < 0 || cards > (version == 1 ? 6 : 10) || mask > valid || mask == valid && level != 50 || mask != (mask == valid ? valid : (1UL << (level - 1)) - 1))
                            {
                                throw new InvalidDataException("Inconsistent adventure checkpoint.");
                            }

                            var seeds = new List<PlantKind>();
                            var spec = AdventureLevels.Get(level);
                            if (version == 1 && spec.ChoosesSeeds && cards != 0 && cards != Math.Min(spec.PacketSlots, spec.UnlockedSeeds.Count))
                            {
                                throw new InvalidDataException("Incomplete saved loadout.");
                            }

                            for (var j = 0; j < cards; j++)
                            {
                                var kind = (PlantKind)r.ReadInt32();
                                if (seeds.Contains(kind) || (version == 1 ? !Has(spec.UnlockedSeeds, kind) : (int)kind < 0 || (int)kind > 48))
                                {
                                    throw new InvalidDataException("Invalid saved seed.");
                                }

                                seeds.Add(kind);
                            }

                            int cycle = version == 2 ? r.ReadInt32() : 1, slots = version == 2 ? r.ReadInt32() : 6, target = version == 2 ? r.ReadInt32() : -1;
                            if (cycle < 1 || cycle > 1000000 || slots < 6 || slots > 10 || target < -1 || target > 39)
                            {
                                throw new InvalidDataException("Invalid replay checkpoint.");
                            }

                            result.Add(id, new AdventureProgress {
                                ProfileId = id,
                                NextLevel = level,
                                Cycle = cycle,
                                PacketSlots = slots,
                                ImitaterTarget = target < 0 ? (PlantKind? )null : (PlantKind)target,
                                CollectedRewards = mask,
                                AttemptSeed = seed,
                                SelectedSeeds = seeds.AsReadOnly()
                            });
                        }

                        if (m.Position != m.Length)
                        {
                            throw new InvalidDataException("Trailing adventure data.");
                        }

                        return result;
                    }
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException("Truncated adventure checkpoint.", e);
            }
        }
    }
}
