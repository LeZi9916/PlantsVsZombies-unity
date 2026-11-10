using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PvZReplica.Profiles
{
    internal enum ProfileSaveStage
    {
        BeforeCommit,
        AfterCommit
    }

    /// <summary>
    /// Stores replica-only profiles. This is not an original game save format.
    /// Each successful mutation is persisted before its snapshot is published.
    /// </summary>
    public sealed class ProfileCatalog
    {
        public const int MaxNameLength = 32;
        public const int MaxProfiles = 1024;

        private const int FormatVersion = 1;
        private const int ChecksumLength = 32;
        private const int MaxFileBytes = 256 * 1024;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PVZRPROF");
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private readonly object gate = new object();
        private readonly string path;
        private readonly Action<ProfileSaveStage> saveHook;
        private ReadOnlyCollection<UserProfile> profiles;
        private Guid? selectedId;
        private byte[] savedBytes;
        private bool unusable;

        public ProfileCatalog(string filePath) : this(filePath, null)
        {
        }

        // The hook permits deterministic tests of failures at the commit boundary.
        internal ProfileCatalog(string filePath, Action<ProfileSaveStage> saveHook)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A profile file path is required.", "filePath");

            path = Path.GetFullPath(filePath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                throw new ArgumentException("The profile path must name a file.", "filePath");
            this.saveHook = saveHook;

            savedBytes = ReadFileOrMissing(path);
            if (savedBytes == null)
            {
                profiles = Array.AsReadOnly(new UserProfile[0]);
                selectedId = null;
            }
            else
            {
                ReadOnlyCollection<UserProfile> loaded;
                Guid? selected;
                Deserialize(savedBytes, out loaded, out selected);
                profiles = loaded;
                selectedId = selected;
            }
        }

        /// <summary>An immutable snapshot, in creation order.</summary>
        public IReadOnlyList<UserProfile> Profiles
        {
            get { lock (gate) { EnsureUsable(); return profiles; } }
        }

        public Guid? SelectedId
        {
            get { lock (gate) { EnsureUsable(); return selectedId; } }
        }

        /// <summary>Creates and selects a profile, and persists the change.</summary>
        public UserProfile Create(string name)
        {
            lock (gate)
            {
                EnsureUsable();
                string normalized = ValidateName(name);
                EnsureUniqueName(normalized, Guid.Empty);
                if (profiles.Count >= MaxProfiles)
                    throw new InvalidOperationException("The profile limit has been reached.");

                Guid id;
                do { id = Guid.NewGuid(); } while (id == Guid.Empty || FindIndex(id) >= 0);
                UserProfile created = new UserProfile(id, normalized);
                List<UserProfile> next = new List<UserProfile>(profiles);
                next.Add(created);
                Save(next, id);
                return created;
            }
        }

        public UserProfile Rename(Guid id, string name)
        {
            lock (gate)
            {
                EnsureUsable();
                int index = RequireIndex(id);
                string normalized = ValidateName(name);
                EnsureUniqueName(normalized, id);
                if (string.Equals(profiles[index].Name, normalized, StringComparison.Ordinal))
                {
                    Save(new List<UserProfile>(profiles), selectedId);
                    return profiles[index];
                }

                UserProfile renamed = new UserProfile(id, normalized);
                List<UserProfile> next = new List<UserProfile>(profiles);
                next[index] = renamed;
                Save(next, selectedId);
                return renamed;
            }
        }

        public void Select(Guid id)
        {
            lock (gate)
            {
                EnsureUsable();
                RequireIndex(id);
                Save(new List<UserProfile>(profiles), id);
            }
        }

        /// <summary>Deletes a profile; deleting the selection selects the first remaining one.</summary>
        public void Delete(Guid id)
        {
            lock (gate)
            {
                EnsureUsable();
                int index = RequireIndex(id);
                List<UserProfile> next = new List<UserProfile>(profiles);
                next.RemoveAt(index);
                Guid? nextSelected = selectedId;
                if (selectedId == id)
                    nextSelected = next.Count == 0 ? (Guid?)null : next[0].Id;
                Save(next, nextSelected);
            }
        }

        private void Save(List<UserProfile> next, Guid? nextSelected)
        {
            // Prepare every allocation needed to publish the new state before the commit.
            ReadOnlyCollection<UserProfile> nextSnapshot = Array.AsReadOnly(next.ToArray());
            byte[] nextBytes = Serialize(nextSnapshot, nextSelected);
            FileStream fileLock;
            try
            {
                // The sidecar is never truncated or deleted, including when it already exists.
                fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                unusable = true;
                throw;
            }

            using (fileLock)
            {
                byte[] current;
                try { current = ReadFileOrMissing(path); }
                catch (Exception ex)
                {
                    unusable = true;
                    throw new IOException("Could not verify the profile file; reopen the catalog.", ex);
                }
                if (!EqualBytes(current, savedBytes))
                {
                    unusable = true;
                    throw new IOException("The profile file changed externally; reopen the catalog.");
                }
                if (EqualBytes(nextBytes, savedBytes)) return;

                string temporary = null;
                string backup = null;
                try
                {
                    temporary = WriteTemporary(nextBytes);
                    if (savedBytes != null) backup = ReserveBackup();
                    if (saveHook != null) saveHook(ProfileSaveStage.BeforeCommit);

                    if (savedBytes == null)
                        File.Move(temporary, path);
                    else
                        File.Replace(temporary, path, backup);
                    temporary = null;

                    if (saveHook != null) saveHook(ProfileSaveStage.AfterCommit);
                    Publish(nextSnapshot, nextSelected, nextBytes);
                }
                catch (Exception failure)
                {
                    SaveOutcome outcome = Reconcile(nextBytes, backup);
                    if (outcome == SaveOutcome.Committed)
                    {
                        Publish(nextSnapshot, nextSelected, nextBytes);
                        return;
                    }
                    if (outcome == SaveOutcome.Unchanged) throw;

                    unusable = true;
                    throw new IOException(
                        "The save outcome could not be verified; reopen the catalog and inspect its backups.",
                        failure);
                }
                finally
                {
                    DeleteOwnedTemporary(temporary);
                }
            }
        }

        private enum SaveOutcome { Unchanged, Committed, Uncertain }

        private SaveOutcome Reconcile(byte[] nextBytes, string backup)
        {
            try
            {
                byte[] current = ReadFileOrMissing(path);
                if (EqualBytes(current, nextBytes)) return SaveOutcome.Committed;
                if (EqualBytes(current, savedBytes)) return SaveOutcome.Unchanged;

                // Never overwrite unexpected data. Restore only a missing target, and only
                // from a backup known to contain the exact previously loaded bytes.
                if (current == null && savedBytes != null && backup != null &&
                    EqualBytes(ReadFileOrMissing(backup), savedBytes))
                {
                    string restore = null;
                    try
                    {
                        restore = WriteTemporary(savedBytes);
                        File.Move(restore, path);
                        restore = null;
                    }
                    catch (Exception) { }
                    finally { DeleteOwnedTemporary(restore); }

                    current = ReadFileOrMissing(path);
                    if (EqualBytes(current, nextBytes)) return SaveOutcome.Committed;
                    if (EqualBytes(current, savedBytes)) return SaveOutcome.Unchanged;
                }
            }
            catch (Exception) { }
            return SaveOutcome.Uncertain;
        }

        private void Publish(ReadOnlyCollection<UserProfile> next, Guid? nextSelected, byte[] bytes)
        {
            profiles = next;
            selectedId = nextSelected;
            savedBytes = bytes;
        }

        private string WriteTemporary(byte[] bytes)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            bool created = false;
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    created = true;
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                return temporary;
            }
            catch
            {
                if (created) DeleteOwnedTemporary(temporary);
                throw;
            }
        }

        private string ReserveBackup()
        {
            string backup = path + ".backup-" + Guid.NewGuid().ToString("N");
            // CreateNew reserves only our own name; an existing user file is not touched.
            using (new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            return backup;
        }

        private static void DeleteOwnedTemporary(string temporary)
        {
            if (temporary == null) return;
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void EnsureUsable()
        {
            if (unusable)
                throw new InvalidOperationException("The profile catalog is no longer usable; reopen it from disk.");
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < profiles.Count; i++)
                if (profiles[i].Id == id) return i;
            return -1;
        }

        private int RequireIndex(Guid id)
        {
            int index = FindIndex(id);
            if (index < 0) throw new KeyNotFoundException("The profile ID was not found.");
            return index;
        }

        private void EnsureUniqueName(string name, Guid except)
        {
            foreach (UserProfile profile in profiles)
                if (profile.Id != except && string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Profile names must be unique, ignoring case.", "name");
        }

        private static string ValidateName(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            string normalized = name.Trim();
            if (normalized.Length == 0 || normalized.Length > MaxNameLength)
                throw new ArgumentException("A profile name must contain 1 to 32 UTF-16 code units.", "name");
            foreach (char character in normalized)
                if (char.IsControl(character))
                    throw new ArgumentException("A profile name cannot contain control characters.", "name");
            try { StrictUtf8.GetByteCount(normalized); }
            catch (EncoderFallbackException ex)
            {
                throw new ArgumentException("A profile name must contain valid Unicode.", "name", ex);
            }
            return normalized;
        }

        private static byte[] Serialize(IReadOnlyList<UserProfile> items, Guid? selected)
        {
            byte[] payload;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(buffer, StrictUtf8))
            {
                writer.Write(items.Count);
                writer.Write((selected ?? Guid.Empty).ToByteArray());
                foreach (UserProfile profile in items)
                {
                    byte[] name = StrictUtf8.GetBytes(profile.Name);
                    writer.Write(profile.Id.ToByteArray());
                    writer.Write(name.Length);
                    writer.Write(name);
                    writer.Write(1);
                }
                writer.Flush();
                payload = buffer.ToArray();
            }

            byte[] body;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(buffer, StrictUtf8))
            {
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(payload.Length);
                writer.Write(payload);
                writer.Flush();
                body = buffer.ToArray();
            }
            byte[] checksum;
            using (SHA256 sha = SHA256.Create()) checksum = sha.ComputeHash(body);
            byte[] result = new byte[body.Length + checksum.Length];
            Buffer.BlockCopy(body, 0, result, 0, body.Length);
            Buffer.BlockCopy(checksum, 0, result, body.Length, checksum.Length);
            return result;
        }

        private static void Deserialize(byte[] bytes, out ReadOnlyCollection<UserProfile> result,
            out Guid? selected)
        {
            if (bytes.Length < Magic.Length + 8 + 20 + ChecksumLength)
                throw new InvalidDataException("The profile file is truncated.");
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, StrictUtf8))
            {
                if (!EqualBytes(reader.ReadBytes(Magic.Length), Magic))
                    throw new InvalidDataException("This is not a replica profile catalog.");
                int version = reader.ReadInt32();
                if (version != FormatVersion)
                    throw new InvalidDataException("Unsupported replica profile version: " + version + ".");
                int payloadLength = reader.ReadInt32();
                if (payloadLength < 20 || (long)payloadLength + Magic.Length + 8 + ChecksumLength != bytes.Length)
                    throw new InvalidDataException("Invalid profile payload length.");

                byte[] checksum;
                using (SHA256 sha = SHA256.Create())
                    checksum = sha.ComputeHash(bytes, 0, bytes.Length - ChecksumLength);
                for (int i = 0; i < ChecksumLength; i++)
                    if (checksum[i] != bytes[bytes.Length - ChecksumLength + i])
                        throw new InvalidDataException("The profile checksum does not match.");

                try
                {
                    int count = reader.ReadInt32();
                    if (count < 0 || count > MaxProfiles)
                        throw new InvalidDataException("Invalid profile count.");
                    Guid selectedValue = new Guid(ReadExactly(reader, 16));
                    List<UserProfile> loaded = new List<UserProfile>(count);
                    HashSet<Guid> ids = new HashSet<Guid>();
                    HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < count; i++)
                    {
                        Guid id = new Guid(ReadExactly(reader, 16));
                        int nameLength = reader.ReadInt32();
                        if (id == Guid.Empty || !ids.Add(id) || nameLength < 1 || nameLength > MaxNameLength * 3)
                            throw new InvalidDataException("Invalid profile ID or name length.");
                        string name = StrictUtf8.GetString(ReadExactly(reader, nameLength));
                        string checkedName = ValidateName(name);
                        if (!string.Equals(name, checkedName, StringComparison.Ordinal) || !names.Add(name))
                            throw new InvalidDataException("Invalid or duplicate profile name.");
                        if (reader.ReadInt32() != 1)
                            throw new InvalidDataException("Replica profiles cannot contain game progress.");
                        loaded.Add(new UserProfile(id, name));
                    }
                    if (stream.Position != bytes.Length - ChecksumLength ||
                        (count == 0 && selectedValue != Guid.Empty) ||
                        (count != 0 && (selectedValue == Guid.Empty || !ids.Contains(selectedValue))))
                        throw new InvalidDataException("Invalid profile selection or trailing data.");

                    result = Array.AsReadOnly(loaded.ToArray());
                    selected = selectedValue == Guid.Empty ? (Guid?)null : selectedValue;
                }
                catch (EndOfStreamException ex) { throw new InvalidDataException("The profile payload is truncated.", ex); }
                catch (ArgumentException ex) { throw new InvalidDataException("The profile payload contains invalid text.", ex); }
            }
        }

        private static byte[] ReadExactly(BinaryReader reader, int count)
        {
            byte[] result = reader.ReadBytes(count);
            if (result.Length != count) throw new EndOfStreamException();
            return result;
        }

        private static byte[] ReadFileOrMissing(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaxFileBytes)
                        throw new InvalidDataException("The profile file is too large.");
                    byte[] bytes = new byte[(int)stream.Length];
                    int position = 0;
                    while (position < bytes.Length)
                    {
                        int read = stream.Read(bytes, position, bytes.Length - position);
                        if (read == 0) throw new InvalidDataException("The profile file changed while reading.");
                        position += read;
                    }
                    if (stream.ReadByte() != -1)
                        throw new InvalidDataException("The profile file changed while reading.");
                    return bytes;
                }
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        private static bool EqualBytes(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }
    }
}
