#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlantsVsZombies.Profiles
{
    /// <summary>
    /// Stores replica-only profiles. This is not an original game save format.
    /// Each successful mutation is persisted before its snapshot is published.
    /// </summary>
    public sealed class ProfileCatalog
    {
        /// <summary>The maximum profile-name length in UTF-16 code units after trimming.</summary>
        public const int MaxNameLength = 32;
        /// <summary>The maximum number of profiles accepted by the catalog file format.</summary>
        public const int MaxProfiles = 1024;
        /// <summary>The supported replica catalog format version.</summary>
        private const int FormatVersion = 1;
        /// <summary>The SHA-256 checksum length in bytes.</summary>
        private const int ChecksumLength = 32;
        /// <summary>The maximum catalog file size accepted before allocating a read buffer.</summary>
        private const int MaxFileBytes = 256 * 1024;
        /// <summary>The fixed ASCII signature identifying replica profile catalogs.</summary>
        private static readonly byte[] s_magic = Encoding.ASCII.GetBytes("PVZRPROF");
        /// <summary>The UTF-8 codec that rejects invalid Unicode instead of substituting characters.</summary>
        private static readonly UTF8Encoding s_strictUtf8 = new UTF8Encoding(false, true);
        /// <summary>Synchronizes snapshot reads and catalog mutations.</summary>
        private readonly object _gate = new object();
        /// <summary>The absolute catalog path used for reads, lock files and atomic replacements.</summary>
        private readonly string _path;
        /// <summary>Observes commit stages for deterministic failure tests, or null during normal use.</summary>
        private readonly Action<ProfileSaveStage>? _saveHook;
        /// <summary>The most recently committed immutable profile snapshot.</summary>
        private ReadOnlyCollection<UserProfile> _profiles;
        /// <summary>The identity selected in the current snapshot, or null for an empty catalog.</summary>
        private Guid? _selectedId;
        /// <summary>The last verified disk snapshot, or null when the catalog file does not yet exist.</summary>
        private byte[]? _savedBytes;
        /// <summary>Records that an external conflict or uncertain commit requires reopening the catalog.</summary>
        private bool _unusable;
        /// <summary>Opens an existing catalog or starts an empty catalog at a new file path.</summary>
        /// <param name="filePath">The catalog file to load and atomically replace on future mutations.</param>
        /// <exception cref="ArgumentNullException">The file path is null.</exception>
        /// <exception cref="ArgumentException">The path is empty or does not name a file.</exception>
        /// <exception cref="InvalidDataException">An existing catalog fails format or checksum validation.</exception>
        /// <exception cref="IOException">An existing catalog cannot be read.</exception>
        public ProfileCatalog(string filePath) : this(filePath, null)
        {
        }

        // The hook permits deterministic tests of failures at the commit boundary.
        /// <summary>Opens a catalog with an optional observer of the save commit boundary.</summary>
        /// <param name="filePath">The catalog file to load and persist.</param>
        /// <param name="saveHook">An optional test callback invoked immediately before and after committing a save.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        internal ProfileCatalog(string filePath, Action<ProfileSaveStage>? saveHook)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A profile file path is required.", "filePath");
            }

            _path = Path.GetFullPath(filePath);
            if (string.IsNullOrEmpty(Path.GetFileName(_path)))
            {
                throw new ArgumentException("The profile path must name a file.", "filePath");
            }

            _saveHook = saveHook;
            _savedBytes = ReadFileOrMissing(_path);
            if (_savedBytes == null)
            {
                _profiles = Array.AsReadOnly(new UserProfile[0]);
                _selectedId = null;
            }
            else
            {
                ReadOnlyCollection<UserProfile> loaded;
                Guid? selected;
                Deserialize(_savedBytes, out loaded, out selected);
                _profiles = loaded;
                _selectedId = selected;
            }
        }

        /// <summary>An immutable snapshot, in creation order.</summary>
        public IReadOnlyList<UserProfile> Profiles
        {
            get
            {
                lock (_gate)
                {
                    EnsureUsable();
                    return _profiles;
                }
            }
        }

        /// <summary>Gets the selected profile identifier, or null when the catalog is empty.</summary>
        public Guid? SelectedId
        {
            get
            {
                lock (_gate)
                {
                    EnsureUsable();
                    return _selectedId;
                }
            }
        }

        /// <summary>Gets the selected id, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public Guid RequiredSelectedId => SelectedId ?? throw new System.InvalidOperationException("The member 'SelectedId' is not available.");

        /// <summary>Creates and selects a profile, and persists the change.</summary>
        /// <param name="name">The display name to trim and validate before checking uniqueness.</param>
        /// <returns>The immutable snapshot of the newly selected profile.</returns>
        /// <exception cref="ArgumentException">The name is invalid or already belongs to another profile.</exception>
        /// <exception cref="InvalidOperationException">The catalog is unusable or its profile limit has been reached.</exception>
        /// <exception cref="IOException">The save fails or the disk file changed outside this catalog.</exception>
        public UserProfile Create(string name)
        {
            lock (_gate)
            {
                EnsureUsable();
                var normalized = ValidateName(name);
                EnsureUniqueName(normalized, Guid.Empty);
                if (_profiles.Count >= MaxProfiles)
                {
                    throw new InvalidOperationException("The profile limit has been reached.");
                }

                Guid id;
                do
                {
                    id = Guid.NewGuid();
                }
                while (id == Guid.Empty || FindIndex(id) >= 0);
                var created = new UserProfile(id, normalized);
                var next = new List<UserProfile>(_profiles);
                next.Add(created);
                Save(next, id);
                return created;
            }
        }

        /// <summary>Adds and selects an imported profile while preserving its supplied identity.</summary>
        /// <param name="id">A nonempty identifier that is not already present in the catalog.</param>
        /// <param name="name">The display name to trim, validate and check for uniqueness.</param>
        /// <returns>The persisted immutable snapshot of the imported profile.</returns>
        /// <exception cref="ArgumentException">The identity or name is invalid or already in use.</exception>
        /// <exception cref="InvalidOperationException">The catalog is unusable or its profile limit has been reached.</exception>
        /// <exception cref="IOException">The save cannot be verified or committed.</exception>
        public UserProfile CreateImported(Guid id, string name)
        {
            lock (_gate)
            {
                EnsureUsable();
                var normalized = ValidateName(name);
                EnsureUniqueName(normalized, Guid.Empty);
                if (id == Guid.Empty || FindIndex(id) >= 0)
                {
                    throw new ArgumentException("Invalid imported identity.");
                }

                if (_profiles.Count >= MaxProfiles)
                {
                    throw new InvalidOperationException("The profile limit has been reached.");
                }

                var created = new UserProfile(id, normalized);
                var next = new List<UserProfile>(_profiles);
                next.Add(created);
                Save(next, id);
                return created;
            }
        }

        /// <summary>Persists a new display name without changing the profile identity or selection.</summary>
        /// <param name="id">The identifier of an existing profile.</param>
        /// <param name="name">The new display name to trim, validate and check for uniqueness.</param>
        /// <returns>The immutable snapshot containing the persisted name.</returns>
        /// <exception cref="KeyNotFoundException">No profile has the supplied identifier.</exception>
        /// <exception cref="ArgumentException">The new name is invalid or already in use.</exception>
        /// <exception cref="IOException">The save cannot be verified or committed.</exception>
        public UserProfile Rename(Guid id, string name)
        {
            lock (_gate)
            {
                EnsureUsable();
                var index = RequireIndex(id);
                var normalized = ValidateName(name);
                EnsureUniqueName(normalized, id);
                if (string.Equals(_profiles[index].Name, normalized, StringComparison.Ordinal))
                {
                    Save(new List<UserProfile>(_profiles), _selectedId);
                    return _profiles[index];
                }

                var renamed = new UserProfile(id, normalized);
                var next = new List<UserProfile>(_profiles);
                next[index] = renamed;
                Save(next, _selectedId);
                return renamed;
            }
        }

        /// <summary>Persists the supplied existing profile as the active selection.</summary>
        /// <param name="id">The identifier of the profile to select.</param>
        /// <exception cref="KeyNotFoundException">No profile has the supplied identifier.</exception>
        /// <exception cref="IOException">The save cannot be verified or committed.</exception>
        public void Select(Guid id)
        {
            lock (_gate)
            {
                EnsureUsable();
                RequireIndex(id);
                Save(new List<UserProfile>(_profiles), id);
            }
        }

        /// <summary>Deletes a profile; deleting the selection selects the first remaining one.</summary>
        /// <param name="id">The identifier of the profile to remove.</param>
        /// <exception cref="KeyNotFoundException">No profile has the supplied identifier.</exception>
        /// <exception cref="IOException">The deletion cannot be verified or committed to disk.</exception>
        public void Delete(Guid id)
        {
            lock (_gate)
            {
                EnsureUsable();
                var index = RequireIndex(id);
                var next = new List<UserProfile>(_profiles);
                next.RemoveAt(index);
                var nextSelected = _selectedId;
                if (_selectedId == id)
                {
                    nextSelected = next.Count == 0 ? (Guid? )null : next[0].Id;
                }

                Save(next, nextSelected);
            }
        }

        /// <summary>Commits a replacement catalog and publishes its snapshot only after the disk outcome is verified.</summary>
        /// <param name="next">The complete proposed profile list.</param>
        /// <param name="nextSelected">The selected identity in the proposed list, or null for an empty list.</param>
        /// <exception cref="IOException">The disk snapshot changed externally or the save outcome could not be verified.</exception>
        private void Save(List<UserProfile> next, Guid? nextSelected)
        {
            // Prepare every allocation needed to publish the new state before the commit.
            var nextSnapshot = Array.AsReadOnly(next.ToArray());
            var nextBytes = Serialize(nextSnapshot, nextSelected);
            FileStream fileLock;
            try
            {
                // The sidecar is never truncated or deleted, including when it already exists.
                fileLock = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                _unusable = true;
                throw;
            }

            using (fileLock)
            {
                byte[]? current;
                try
                {
                    current = ReadFileOrMissing(_path);
                }
                catch (Exception ex)
                {
                    _unusable = true;
                    throw new IOException("Could not verify the profile file; reopen the catalog.", ex);
                }

                if (!EqualBytes(current, _savedBytes))
                {
                    _unusable = true;
                    throw new IOException("The profile file changed externally; reopen the catalog.");
                }

                if (EqualBytes(nextBytes, _savedBytes))
                {
                    return;
                }

                string? temporary = null;
                string? backup = null;
                try
                {
                    temporary = WriteTemporary(nextBytes);
                    if (_savedBytes != null)
                    {
                        backup = ReserveBackup();
                    }

                    if (_saveHook != null)
                    {
                        _saveHook(ProfileSaveStage.BeforeCommit);
                    }

                    if (_savedBytes == null)
                    {
                        File.Move(temporary, _path);
                    }
                    else
                    {
                        File.Replace(temporary, _path, backup);
                    }

                    temporary = null;
                    if (_saveHook != null)
                    {
                        _saveHook(ProfileSaveStage.AfterCommit);
                    }

                    Publish(nextSnapshot, nextSelected, nextBytes);
                }
                catch (Exception failure)
                {
                    var outcome = Reconcile(nextBytes, backup);
                    if (outcome == SaveOutcome.Committed)
                    {
                        Publish(nextSnapshot, nextSelected, nextBytes);
                        return;
                    }

                    if (outcome == SaveOutcome.Unchanged)
                    {
                        throw;
                    }

                    _unusable = true;
                    throw new IOException("The save outcome could not be verified; reopen the catalog and inspect its backups.", failure);
                }
                finally
                {
                    DeleteOwnedTemporary(temporary);
                }
            }
        }

        /// <summary>Describes the disk state verified after an interrupted or failed save.</summary>
        private enum SaveOutcome
        {
            /// <summary>The disk still contains the exact previously loaded snapshot.</summary>
            Unchanged,
            /// <summary>The disk contains the exact proposed snapshot.</summary>
            Committed,
            /// <summary>The disk state cannot be verified as either the prior or proposed snapshot.</summary>
            Uncertain
        }

        /// <summary>Checks whether a failed save committed, remained unchanged, or left an uncertain disk state.</summary>
        /// <param name="nextBytes">The complete proposed catalog bytes.</param>
        /// <param name="backup">The owned backup path, or null when no prior file existed.</param>
        /// <returns>The verified outcome of the attempted save.</returns>
        private SaveOutcome Reconcile(byte[] nextBytes, string? backup)
        {
            try
            {
                var current = ReadFileOrMissing(_path);
                if (EqualBytes(current, nextBytes))
                {
                    return SaveOutcome.Committed;
                }

                if (EqualBytes(current, _savedBytes))
                {
                    return SaveOutcome.Unchanged;
                }

                // Never overwrite unexpected data. Restore only a missing target, and only
                // from a backup known to contain the exact previously loaded bytes.
                if (current == null && _savedBytes != null && backup != null && EqualBytes(ReadFileOrMissing(backup), _savedBytes))
                {
                    string? restore = null;
                    try
                    {
                        restore = WriteTemporary(_savedBytes);
                        File.Move(restore, _path);
                        restore = null;
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        DeleteOwnedTemporary(restore);
                    }

                    current = ReadFileOrMissing(_path);
                    if (EqualBytes(current, nextBytes))
                    {
                        return SaveOutcome.Committed;
                    }

                    if (EqualBytes(current, _savedBytes))
                    {
                        return SaveOutcome.Unchanged;
                    }
                }
            }
            catch (Exception)
            {
            }

            return SaveOutcome.Uncertain;
        }

        /// <summary>Publishes the immutable snapshot corresponding to a verified disk commit.</summary>
        /// <param name="next">The committed immutable profile snapshot.</param>
        /// <param name="nextSelected">The committed selection, or null when no profiles remain.</param>
        /// <param name="bytes">The exact committed catalog bytes.</param>
        private void Publish(ReadOnlyCollection<UserProfile> next, Guid? nextSelected, byte[] bytes)
        {
            _profiles = next;
            _selectedId = nextSelected;
            _savedBytes = bytes;
        }

        /// <summary>Creates and flushes a uniquely named temporary file owned by this save operation.</summary>
        /// <param name="bytes">The complete catalog bytes to write.</param>
        /// <returns>The path of the newly created and flushed file.</returns>
        private string WriteTemporary(byte[] bytes)
        {
            var temporary = _path + ".tmp-" + Guid.NewGuid().ToString("N");
            var created = false;
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true;
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                return temporary;
            }
            catch
            {
                if (created)
                {
                    DeleteOwnedTemporary(temporary);
                }

                throw;
            }
        }

        /// <summary>Reserves a unique backup filename without replacing any existing file.</summary>
        /// <returns>The path of the empty backup file reserved for an atomic replacement.</returns>
        private string ReserveBackup()
        {
            var backup = _path + ".backup-" + Guid.NewGuid().ToString("N");
            // CreateNew reserves only our own name; an existing user file is not touched.
            using (new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }

            return backup;
        }

        /// <summary>Best-effort deletes a temporary file created by this save operation.</summary>
        /// <param name="temporary">The owned temporary path, or null when there is nothing to delete.</param>
        private static void DeleteOwnedTemporary(string? temporary)
        {
            if (temporary == null)
            {
                return;
            }

            try
            {
                File.Delete(temporary);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Rejects operations after an external conflict or uncertain save invalidated the catalog.</summary>
        /// <exception cref="InvalidOperationException">The catalog must be reopened from disk before it can be used again.</exception>
        private void EnsureUsable()
        {
            if (_unusable)
            {
                throw new InvalidOperationException("The profile catalog is no longer usable; reopen it from disk.");
            }
        }

        /// <summary>Finds a profile within the current immutable snapshot.</summary>
        /// <param name="id">The persistent identity to search for.</param>
        /// <returns>The profile index, or minus one when the identity is absent.</returns>
        private int FindIndex(Guid id)
        {
            for (var i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Resolves an existing profile identity to its current snapshot index.</summary>
        /// <param name="id">The persistent identity to resolve.</param>
        /// <returns>The index of the matching profile.</returns>
        /// <exception cref="KeyNotFoundException">The current snapshot contains no profile with the supplied identity.</exception>
        private int RequireIndex(Guid id)
        {
            var index = FindIndex(id);
            if (index < 0)
            {
                throw new KeyNotFoundException("The profile ID was not found.");
            }

            return index;
        }

        /// <summary>Rejects a name already used by another profile using ordinal case-insensitive comparison.</summary>
        /// <param name="name">The normalized name to check.</param>
        /// <param name="except">An identity excluded during rename, or Guid.Empty when creating a profile.</param>
        /// <exception cref="ArgumentException">Another profile already uses the proposed name.</exception>
        private void EnsureUniqueName(string name, Guid except)
        {
            foreach (UserProfile profile in _profiles)
            {
                if (profile.Id != except && string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Profile names must be unique, ignoring case.", "name");
                }
            }
        }

        /// <summary>Trims a display name and validates its length, control characters and Unicode encoding.</summary>
        /// <param name="name">The proposed display name.</param>
        /// <returns>The trimmed valid display name.</returns>
        /// <exception cref="ArgumentNullException">The proposed name is null.</exception>
        /// <exception cref="ArgumentException">The name is empty, too long, contains control characters or has invalid Unicode.</exception>
        private static string ValidateName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var normalized = name.Trim();
            if (normalized.Length == 0 || normalized.Length > MaxNameLength)
            {
                throw new ArgumentException("A profile name must contain 1 to 32 UTF-16 code units.", "name");
            }

            foreach (char character in normalized)
            {
                if (char.IsControl(character))
                {
                    throw new ArgumentException("A profile name cannot contain control characters.", "name");
                }
            }

            try
            {
                s_strictUtf8.GetByteCount(normalized);
            }
            catch (EncoderFallbackException ex)
            {
                throw new ArgumentException("A profile name must contain valid Unicode.", "name", ex);
            }

            return normalized;
        }

        /// <summary>Serializes a catalog snapshot with its format header and SHA-256 integrity checksum.</summary>
        /// <param name="items">The complete ordered profile snapshot.</param>
        /// <param name="selected">The selected profile identity, or null when the list is empty.</param>
        /// <returns>The complete catalog file bytes including its checksum.</returns>
        private static byte[] Serialize(IReadOnlyList<UserProfile> items, Guid? selected)
        {
            byte[] payload;
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, s_strictUtf8))
                {
                    writer.Write(items.Count);
                    writer.Write((selected ?? Guid.Empty).ToByteArray());
                    foreach (UserProfile profile in items)
                    {
                        var name = s_strictUtf8.GetBytes(profile.Name);
                        writer.Write(profile.Id.ToByteArray());
                        writer.Write(name.Length);
                        writer.Write(name);
                        writer.Write(1);
                    }

                    writer.Flush();
                    payload = buffer.ToArray();
                }
            }

            byte[] body;
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, s_strictUtf8))
                {
                    writer.Write(s_magic);
                    writer.Write(FormatVersion);
                    writer.Write(payload.Length);
                    writer.Write(payload);
                    writer.Flush();
                    body = buffer.ToArray();
                }
            }

            byte[] checksum;
            using (var sha = SHA256.Create())
            {
                checksum = sha.ComputeHash(body);
            }

            var result = new byte[body.Length + checksum.Length];
            Buffer.BlockCopy(body, 0, result, 0, body.Length);
            Buffer.BlockCopy(checksum, 0, result, body.Length, checksum.Length);
            return result;
        }

        /// <summary>Validates and decodes a complete catalog file before publishing any state.</summary>
        /// <param name="bytes">The complete file bytes including its header and checksum.</param>
        /// <param name="result">The validated immutable profile snapshot.</param>
        /// <param name="selected">The validated selected identity, or null for an empty catalog.</param>
        /// <exception cref="InvalidDataException">The format, checksum, profile records, selection or trailing-data layout is invalid.</exception>
        private static void Deserialize(byte[] bytes, out ReadOnlyCollection<UserProfile> result, out Guid? selected)
        {
            if (bytes.Length < s_magic.Length + 8 + 20 + ChecksumLength)
            {
                throw new InvalidDataException("The profile file is truncated.");
            }

            using (var stream = new MemoryStream(bytes, false))
            {
                using (var reader = new BinaryReader(stream, s_strictUtf8))
                {
                    if (!EqualBytes(reader.ReadBytes(s_magic.Length), s_magic))
                    {
                        throw new InvalidDataException("This is not a replica profile catalog.");
                    }

                    var version = reader.ReadInt32();
                    if (version != FormatVersion)
                    {
                        throw new InvalidDataException("Unsupported replica profile version: " + version + ".");
                    }

                    var payloadLength = reader.ReadInt32();
                    if (payloadLength < 20 || (long)payloadLength + s_magic.Length + 8 + ChecksumLength != bytes.Length)
                    {
                        throw new InvalidDataException("Invalid profile payload length.");
                    }

                    byte[] checksum;
                    using (var sha = SHA256.Create())
                    {
                        checksum = sha.ComputeHash(bytes, 0, bytes.Length - ChecksumLength);
                    }

                    for (var i = 0; i < ChecksumLength; i++)
                    {
                        if (checksum[i] != bytes[bytes.Length - ChecksumLength + i])
                        {
                            throw new InvalidDataException("The profile checksum does not match.");
                        }
                    }

                    try
                    {
                        var count = reader.ReadInt32();
                        if (count < 0 || count > MaxProfiles)
                        {
                            throw new InvalidDataException("Invalid profile count.");
                        }

                        var selectedValue = new Guid(ReadExactly(reader, 16));
                        var loaded = new List<UserProfile>(count);
                        var ids = new HashSet<Guid>();
                        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (var i = 0; i < count; i++)
                        {
                            var id = new Guid(ReadExactly(reader, 16));
                            var nameLength = reader.ReadInt32();
                            if (id == Guid.Empty || !ids.Add(id) || nameLength < 1 || nameLength > MaxNameLength * 3)
                            {
                                throw new InvalidDataException("Invalid profile ID or name length.");
                            }

                            var name = s_strictUtf8.GetString(ReadExactly(reader, nameLength));
                            var checkedName = ValidateName(name);
                            if (!string.Equals(name, checkedName, StringComparison.Ordinal) || !names.Add(name))
                            {
                                throw new InvalidDataException("Invalid or duplicate profile name.");
                            }

                            if (reader.ReadInt32() != 1)
                            {
                                throw new InvalidDataException("Replica profiles cannot contain game progress.");
                            }

                            loaded.Add(new UserProfile(id, name));
                        }

                        if (stream.Position != bytes.Length - ChecksumLength || (count == 0 && selectedValue != Guid.Empty) || (count != 0 && (selectedValue == Guid.Empty || !ids.Contains(selectedValue))))
                        {
                            throw new InvalidDataException("Invalid profile selection or trailing data.");
                        }

                        result = Array.AsReadOnly(loaded.ToArray());
                        selected = selectedValue == Guid.Empty ? (Guid? )null : selectedValue;
                    }
                    catch (EndOfStreamException ex)
                    {
                        throw new InvalidDataException("The profile payload is truncated.", ex);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidDataException("The profile payload contains invalid text.", ex);
                    }
                }
            }
        }

        /// <summary>Reads a fixed number of bytes from the catalog payload.</summary>
        /// <param name="reader">The reader positioned at the next payload field.</param>
        /// <param name="count">The exact number of bytes required for that field.</param>
        /// <returns>An array containing exactly the requested number of bytes.</returns>
        /// <exception cref="EndOfStreamException">The payload ends before the requested byte count is available.</exception>
        private static byte[] ReadExactly(BinaryReader reader, int count)
        {
            var result = reader.ReadBytes(count);
            if (result.Length != count)
            {
                throw new EndOfStreamException();
            }

            return result;
        }

        /// <summary>Reads one bounded catalog snapshot and distinguishes an absent file from read failure.</summary>
        /// <param name="filePath">The catalog or owned-backup file to read.</param>
        /// <returns>The complete file bytes, or null when the file or its parent directory does not exist.</returns>
        /// <exception cref="InvalidDataException">The file exceeds the size limit or changes length while being read.</exception>
        private static byte[]? ReadFileOrMissing(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaxFileBytes)
                    {
                        throw new InvalidDataException("The profile file is too large.");
                    }

                    var bytes = new byte[(int)stream.Length];
                    var position = 0;
                    while (position < bytes.Length)
                    {
                        var read = stream.Read(bytes, position, bytes.Length - position);
                        if (read == 0)
                        {
                            throw new InvalidDataException("The profile file changed while reading.");
                        }

                        position += read;
                    }

                    if (stream.ReadByte() != -1)
                    {
                        throw new InvalidDataException("The profile file changed while reading.");
                    }

                    return bytes;
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        /// <summary>Compares catalog snapshots while treating two absent files as equal.</summary>
        /// <param name="left">The first snapshot, or null for an absent file.</param>
        /// <param name="right">The second snapshot, or null for an absent file.</param>
        /// <returns>True when both snapshots are absent or contain identical bytes.</returns>
        private static bool EqualBytes(byte[]? left, byte[]? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
