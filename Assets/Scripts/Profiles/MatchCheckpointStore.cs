#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Profiles
{
    /// <summary>Atomically stores one resumable board per profile and native game mode.</summary>
    internal sealed class MatchCheckpointStore
    {
        /// <summary>Bounds reads before allocating a damaged file's payload.</summary>
        internal const int MaxBytes = 32 * 1024 * 1024;
        /// <summary>Identifies the replica checkpoint envelope and its version.</summary>
        private const string Magic = "PVZRMATCH1";
        /// <summary>The directory containing per-user, per-mode checkpoints.</summary>
        private readonly string _directory;
        /// <summary>Observes the atomic commit boundary for isolated failure tests.</summary>
        private readonly Action<ProfileSaveStage>? _saveHook;

        /// <summary>Opens a directory without creating or changing any save.</summary>
        /// <param name="directory">The profile storage directory.</param>
        /// <param name="saveHook">An optional test observer at the atomic commit boundary.</param>
        internal MatchCheckpointStore(string directory, Action<ProfileSaveStage>? saveHook = null)
        {
            _directory = Path.GetFullPath(directory);
            _saveHook = saveHook;
        }

        /// <summary>Matches PC Board::TryToSaveGame (414A00), including its five excluded modes.</summary>
        /// <param name="mode">The original PC game mode identifier.</param>
        /// <returns>Whether that mode can have an in-progress game file.</returns>
        internal static bool SupportsMode(int mode)
        {
            return mode >= 0 && mode <= 72 && mode != 42 && mode != 43 && mode != 50 && mode != 71 && mode != 72;
        }

        /// <summary>Checks for the selected profile's mode-specific continuation.</summary>
        /// <param name="profile">The profile identity.</param>
        /// <param name="mode">The native game mode.</param>
        /// <returns>Whether a saved board exists.</returns>
        internal bool Exists(Guid profile, int mode)
        {
            return SupportsMode(mode) && File.Exists(SavePath(profile, mode));
        }

        /// <summary>Commits a full board only after its bytes and checksum have been written.</summary>
        /// <param name="profile">The owning profile.</param>
        /// <param name="mode">The original PC game mode identifier.</param>
        /// <param name="receipt">The existing attempt identity used by coin and loot receipts.</param>
        /// <param name="game">The board to preserve.</param>
        /// <exception cref="IOException">The checkpoint cannot be committed.</exception>
        /// <exception cref="InvalidOperationException">The board cannot have a native continuation.</exception>
        /// <exception cref="ArgumentException">The supplied mode does not identify this board.</exception>
        /// <exception cref="InvalidDataException">The model cannot be serialized or exceeds the size limit.</exception>
        /// <exception cref="UnauthorizedAccessException">The storage directory cannot be written.</exception>
        internal void Save(Guid profile, int mode, string receipt, GameSimulation game)
        {
            SaveCore(profile, mode, receipt, game, null);
        }

        /// <summary>Saves an editor draft only when its original file still matches, retaining the previous file as a backup.</summary>
        /// <param name="profile">The owning profile.</param>
        /// <param name="mode">The original PC mode identifier.</param>
        /// <param name="receipt">The unchanged coin and loot receipt identity.</param>
        /// <param name="game">The detached edited board.</param>
        /// <param name="expectedBytes">The exact file accepted when the editor loaded the board.</param>
        /// <exception cref="IOException">The file changed externally, is locked or cannot be committed.</exception>
        /// <exception cref="InvalidDataException">The edited model cannot be serialized.</exception>
        /// <exception cref="ArgumentNullException">The accepted file snapshot is null.</exception>
        /// <exception cref="ArgumentException">The mode does not match the board.</exception>
        /// <exception cref="InvalidOperationException">The board cannot have a native continuation.</exception>
        /// <exception cref="UnauthorizedAccessException">The save or backup location is not writable.</exception>
        internal void SaveEditorSnapshot(Guid profile, int mode, string receipt, GameSimulation game, byte[] expectedBytes)
        {
            SaveCore(profile, mode, receipt, game, expectedBytes ?? throw new ArgumentNullException(nameof(expectedBytes)));
        }

        /// <summary>Serializes a board and commits it under the same lease used by editor writes and runtime deletion.</summary>
        /// <param name="profile">The owning profile.</param>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="receipt">The stable receipt identity.</param>
        /// <param name="game">The board model.</param>
        /// <param name="expectedBytes">The editor baseline, or null for a runtime save without a backup.</param>
        /// <exception cref="IOException">A file operation fails or an editor baseline is stale.</exception>
        private void SaveCore(Guid profile, int mode, string receipt, GameSimulation game, byte[]? expectedBytes)
        {
            if (!SupportsMode(mode) || !game.IsAdventure || game.IsExperimentalLevelOne)
            {
                throw new InvalidOperationException("This board does not support native continuation.");
            }
            if ((game.ModeSession?.Definition.Id ?? 0) != mode)
            {
                throw new ArgumentException("The checkpoint mode must match the board.", nameof(mode));
            }
            byte[] body;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Magic);
                    writer.Write(profile.ToByteArray());
                    writer.Write(mode);
                    writer.Write(receipt);
                    var payload = game.CaptureCheckpoint();
                    writer.Write(payload.Length);
                    writer.Write(payload);
                    writer.Flush();
                    body = stream.ToArray();
                }
            }
            if (body.Length > MaxBytes - 32)
            {
                throw new InvalidDataException("Match checkpoint is too large.");
            }
            Directory.CreateDirectory(_directory);
            var path = SavePath(profile, mode);
            using var lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (expectedBytes != null && !MatchesFile(path, expectedBytes))
            {
                throw new IOException("对局存档已被游戏或其他窗口修改，请重新读取后再保存。");
            }
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var hash = SHA256.Create())
                    {
                        stream.Write(body, 0, body.Length);
                        var checksum = hash.ComputeHash(body);
                        stream.Write(checksum, 0, checksum.Length);
                        stream.Flush(true);
                    }
                }
                _saveHook?.Invoke(ProfileSaveStage.BeforeCommit);
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, expectedBytes == null ? null : path + ".backup");
                }
                else
                {
                    File.Move(temporary, path);
                }
                _saveHook?.Invoke(ProfileSaveStage.AfterCommit);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        /// <summary>Validates a file completely before returning a detached restored match.</summary>
        /// <param name="profile">The requested profile.</param>
        /// <param name="mode">The requested native mode.</param>
        /// <param name="receipt">The restored coin and loot attempt identity.</param>
        /// <returns>The restored match.</returns>
        /// <exception cref="InvalidDataException">The file is damaged, incompatible, or belongs to another board.</exception>
        /// <exception cref="IOException">The save cannot be read or is truncated.</exception>
        /// <exception cref="UnauthorizedAccessException">The save cannot be opened for reading.</exception>
        internal GameSimulation Load(Guid profile, int mode, out string receipt)
        {
            var path = SavePath(profile, mode);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 64 || stream.Length > MaxBytes)
            {
                throw new InvalidDataException("Invalid match checkpoint size.");
            }
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var body = reader.ReadBytes((int)stream.Length - 32);
            var expected = reader.ReadBytes(32);
            using var hash = SHA256.Create();
            var actual = hash.ComputeHash(body);
            var difference = 0;
            for (var index = 0; index < actual.Length; index++)
            {
                difference |= actual[index] ^ expected[index];
            }
            if (difference != 0)
            {
                throw new InvalidDataException("Match checkpoint checksum failed.");
            }
            using var payloadStream = new MemoryStream(body, false);
            using var payloadReader = new BinaryReader(payloadStream);
            if (payloadReader.ReadString() != Magic || new Guid(payloadReader.ReadBytes(16)) != profile || payloadReader.ReadInt32() != mode)
            {
                throw new InvalidDataException("Match checkpoint identity mismatch.");
            }
            receipt = payloadReader.ReadString();
            var length = payloadReader.ReadInt32();
            if (length < 0 || length != payloadStream.Length - payloadStream.Position)
            {
                throw new InvalidDataException("Invalid match checkpoint payload.");
            }
            var game = GameSimulation.RestoreCheckpoint(payloadReader.ReadBytes(length));
            if ((game.ModeSession?.Definition.Id ?? 0) != mode)
            {
                throw new InvalidDataException("Match checkpoint mode mismatch.");
            }
            return game;
        }

        /// <summary>Erases only this profile's selected mode after restart, loss or completion.</summary>
        /// <param name="profile">The owning profile.</param>
        /// <param name="mode">The native mode.</param>
        /// <exception cref="IOException">The file cannot be deleted.</exception>
        /// <exception cref="UnauthorizedAccessException">The storage location is not writable.</exception>
        internal void Delete(Guid profile, int mode)
        {
            if (!Directory.Exists(_directory))
            {
                return;
            }
            var path = SavePath(profile, mode);
            using var lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            File.Delete(path);
        }

        /// <summary>Compares a bounded accepted snapshot with one consistently opened file.</summary>
        /// <param name="path">The file to compare.</param>
        /// <param name="expected">The accepted bytes.</param>
        /// <returns>Whether the file still contains exactly the accepted snapshot.</returns>
        /// <exception cref="IOException">The file cannot be read.</exception>
        internal static bool MatchesFile(string path, byte[] expected)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length != expected.Length)
            {
                return false;
            }
            var buffer = new byte[8192];
            var position = 0;
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (position > expected.Length - count)
                {
                    return false;
                }
                for (var index = 0; index < count; index++)
                {
                    if (buffer[index] != expected[position + index])
                    {
                        return false;
                    }
                }
                position += count;
            }
            return position == expected.Length;
        }

        /// <summary>Derives a filename without accepting path components from a save file.</summary>
        /// <param name="profile">The profile identity.</param>
        /// <param name="mode">The native mode.</param>
        /// <returns>The absolute checkpoint path.</returns>
        internal string SavePath(Guid profile, int mode)
        {
            return Path.Combine(_directory, "game-" + profile.ToString("N") + "-" + mode + ".dat");
        }
    }
}
