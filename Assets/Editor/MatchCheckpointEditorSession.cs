#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Editor
{
    /// <summary>Owns an offline board draft and the exact disk baselines required to commit it.</summary>
    internal sealed class MatchCheckpointEditorSession
    {
        /// <summary>The profile catalog opened without changing its selected user.</summary>
        private readonly ProfileCatalog _catalog;
        /// <summary>The catalog file whose membership must remain current.</summary>
        private readonly string _catalogPath;
        /// <summary>The per-profile, per-mode checkpoint backend.</summary>
        private readonly MatchCheckpointStore _store;
        /// <summary>The exact accepted profile catalog snapshot.</summary>
        private byte[] _catalogBytes;
        /// <summary>The exact checkpoint snapshot accepted before editing.</summary>
        private byte[] _checkpointBytes = Array.Empty<byte>();
        /// <summary>The immutable level identity accepted when loading the draft.</summary>
        private int _level;
        /// <summary>The immutable adventure cycle accepted when loading the draft.</summary>
        private int _cycle;
        /// <summary>The immutable challenge receipt identity accepted when loading the draft.</summary>
        private Guid? _runId;
        /// <summary>Gets the absolute directory shared by the catalog and board files.</summary>
        internal string DirectoryPath { get; }
        /// <summary>Gets the profiles available to this editor without altering game selection.</summary>
        internal IReadOnlyList<UserProfile> Profiles => _catalog.Profiles;
        /// <summary>Gets the profile selected only in this editor.</summary>
        internal Guid SelectedProfileId { get; private set; }
        /// <summary>Gets the native mode IDs which currently have checkpoint files.</summary>
        internal IReadOnlyList<int> AvailableModes { get; private set; } = Array.Empty<int>();
        /// <summary>Gets the loaded mode ID, or minus one before a board is loaded.</summary>
        internal int SelectedModeId { get; private set; } = -1;
        /// <summary>Gets the detached editable board, or null before a saved mode is selected.</summary>
        internal GameSimulation? Game { get; private set; }
        /// <summary>Gets the unchanged account coin/loot receipt identity.</summary>
        internal string Receipt { get; private set; } = string.Empty;
        /// <summary>Gets whether recovered edits are based on older disk files.</summary>
        internal bool HasStaleDraft { get; private set; }
        /// <summary>Gets an independent copy of the original catalog baseline.</summary>
        internal byte[] CatalogBytes => (byte[])_catalogBytes.Clone();
        /// <summary>Gets an independent copy of the original checkpoint baseline.</summary>
        internal byte[] CheckpointBytes => (byte[])_checkpointBytes.Clone();
        /// <summary>Gets the loaded board's absolute file path.</summary>
        internal string CheckpointPath => _store.SavePath(SelectedProfileId, SelectedModeId);
        /// <summary>Gets the original immutable level identity for recovery snapshots.</summary>
        internal int OriginalLevel => _level;
        /// <summary>Gets the original immutable cycle identity for recovery snapshots.</summary>
        internal int OriginalCycle => _cycle;
        /// <summary>Gets the original immutable run identity for recovery snapshots.</summary>
        internal Guid? OriginalRunId => _runId;

        /// <summary>Reads catalog membership and discovers saved modes without creating files.</summary>
        /// <param name="directory">An existing game save directory.</param>
        /// <exception cref="IOException">The catalog is missing, invalid or changes while opening.</exception>
        /// <exception cref="InvalidDataException">The catalog has no game profiles.</exception>
        /// <exception cref="ArgumentException">The directory path is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog cannot be read.</exception>
        internal MatchCheckpointEditorSession(string directory)
        {
            DirectoryPath = Path.GetFullPath(directory);
            _catalogPath = Path.Combine(DirectoryPath, "profiles.dat");
            _catalogBytes = File.ReadAllBytes(_catalogPath);
            _catalog = new ProfileCatalog(_catalogPath);
            _store = new MatchCheckpointStore(DirectoryPath);
            if (_catalog.Profiles.Count == 0)
            {
                throw new InvalidDataException("该目录没有游戏用户，请先在游戏中创建用户。");
            }
            EnsureCatalogCurrent();
            SelectProfile(_catalog.SelectedId ?? _catalog.Profiles[0].Id);
        }

        /// <summary>Discovers this profile's existing board files while leaving game selection unchanged.</summary>
        /// <param name="profile">A profile belonging to the loaded catalog.</param>
        /// <exception cref="ArgumentException">The profile does not belong to the catalog.</exception>
        /// <exception cref="IOException">The accepted catalog is stale.</exception>
        internal void SelectProfile(Guid profile)
        {
            EnsureCatalogCurrent();
            var exists = false;
            foreach (var entry in Profiles)
            {
                exists |= entry.Id == profile;
            }
            if (!exists)
            {
                throw new ArgumentException("用户不在已读取的用户目录中。", nameof(profile));
            }
            var modes = new List<int>();
            for (var mode = 0; mode <= 72; mode++)
            {
                if (_store.Exists(profile, mode))
                {
                    modes.Add(mode);
                }
            }
            SelectedProfileId = profile;
            AvailableModes = modes.AsReadOnly();
            SelectedModeId = -1;
            Game = null;
            Receipt = string.Empty;
            _checkpointBytes = Array.Empty<byte>();
            HasStaleDraft = false;
        }

        /// <summary>Loads one validated detached board with a stable disk baseline.</summary>
        /// <param name="mode">An existing native mode checkpoint.</param>
        /// <exception cref="IOException">The save is missing, damaged or changes while reading.</exception>
        /// <exception cref="ArgumentException">The mode cannot have an original continuation.</exception>
        /// <exception cref="InvalidDataException">The file fails format or checksum validation.</exception>
        /// <exception cref="UnauthorizedAccessException">The checkpoint cannot be read.</exception>
        internal void SelectMode(int mode)
        {
            EnsureCatalogCurrent();
            if (!MatchCheckpointStore.SupportsMode(mode))
            {
                throw new ArgumentException("该模式没有原版对局存档。", nameof(mode));
            }
            var path = _store.SavePath(SelectedProfileId, mode);
            if (new FileInfo(path).Length > MatchCheckpointStore.MaxBytes)
            {
                throw new InvalidDataException("对局文件超过格式大小限制。");
            }
            var baseline = File.ReadAllBytes(path);
            var loaded = _store.Load(SelectedProfileId, mode, out var receipt);
            if (!MatchCheckpointStore.MatchesFile(path, baseline))
            {
                throw new IOException("对局文件在读取时发生变化，请重载。");
            }
            EnsureCatalogCurrent();
            SelectedModeId = mode;
            Receipt = receipt;
            Game = loaded;
            _checkpointBytes = baseline;
            HasStaleDraft = false;
            CaptureIdentity();
        }

        /// <summary>Reinstates a serialized draft while retaining its original baselines through repeated reloads.</summary>
        /// <param name="draft">The detached window recovery snapshot.</param>
        /// <exception cref="InvalidDataException">The draft has inconsistent board or profile identities.</exception>
        /// <exception cref="IOException">The current profile catalog cannot be read.</exception>
        /// <exception cref="FormatException">A serialized identity is invalid.</exception>
        internal void RestoreDraft(MatchCheckpointEditorDraft draft)
        {
            SelectProfile(draft.ProfileId);
            var restored = GameSimulation.RestoreCheckpoint(draft.Payload);
            if ((restored.ModeSession?.Definition.Id ?? 0) != draft.Mode)
            {
                throw new InvalidDataException("对局草稿与所选模式不一致。");
            }
            SelectedModeId = draft.Mode;
            Receipt = draft.Receipt;
            Game = restored;
            _catalogBytes = (byte[])draft.CatalogBytes.Clone();
            _checkpointBytes = (byte[])draft.CheckpointBytes.Clone();
            HasStaleDraft = draft.Stale || !MatchCheckpointStore.MatchesFile(_catalogPath, _catalogBytes)
                || !MatchCheckpointStore.MatchesFile(CheckpointPath, _checkpointBytes);
            _level = draft.Level;
            _cycle = draft.Cycle;
            _runId = draft.RunId;
        }

        /// <summary>Validates and atomically saves only the selected board, preserving all unrelated files.</summary>
        /// <exception cref="IOException">The accepted files are stale or cannot be saved.</exception>
        /// <exception cref="InvalidDataException">The draft changes identity or contains invalid basic board values.</exception>
        /// <exception cref="InvalidOperationException">No checkpoint is loaded.</exception>
        /// <exception cref="UnauthorizedAccessException">The checkpoint or backup cannot be written.</exception>
        internal void Save()
        {
            if (Game == null || SelectedModeId < 0)
            {
                throw new InvalidOperationException("请先读取一个已保存的对局。");
            }
            if (HasStaleDraft)
            {
                throw new IOException("恢复的草稿所依据的文件已经变化，请重载后再保存。");
            }
            // Rebuild views, indexes and entity callbacks before publishing the edited draft.
            var candidate = GameSimulation.RestoreCheckpoint(Game.CaptureCheckpoint());
            Validate(candidate);
            using (var lease = new FileStream(_catalogPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                EnsureCatalogCurrent();
                _store.SaveEditorSnapshot(SelectedProfileId, SelectedModeId, Receipt, candidate, _checkpointBytes);
                SelectMode(SelectedModeId);
            }
        }

        /// <summary>Checks fields needed to resume this board and immutable receipt identity.</summary>
        /// <param name="game">The reconstituted candidate draft.</param>
        /// <exception cref="InvalidDataException">Basic counters or persistent identities are inconsistent.</exception>
        private void Validate(GameSimulation game)
        {
            if (game.Adventure.Level.Number != _level || game.Rules.AdventureCycle != _cycle
                || game.ModeSession?.RunId != _runId || (game.ModeSession?.Definition.Id ?? 0) != SelectedModeId)
            {
                throw new InvalidDataException("对局类型、关卡、周目或回执标识不能更改。");
            }
            if (game.SunAmount < 0 || float.IsNaN(game.Elapsed) || float.IsInfinity(game.Elapsed) || game.Elapsed < 0f
                || game.Adventure.CurrentWave < 0 || game.Adventure.CurrentWave > game.Adventure.Waves.Count)
            {
                throw new InvalidDataException("阳光、时间或波次超出当前对局的有效范围。");
            }
            foreach (var ticks in game.CheckpointState._seedCooldownTicks)
            {
                if (ticks < 0)
                {
                    throw new InvalidDataException("种子冷却不能为负数。");
                }
            }
            foreach (var plant in game.Plants)
            {
                if (plant.Row < 0 || plant.Row >= game.RowCount || plant.Column < 0 || plant.Column >= GameSimulation.Columns
                    || float.IsNaN(plant.Health) || float.IsInfinity(plant.Health))
                {
                    throw new InvalidDataException("植物的行列或生命值无效。");
                }
            }
            foreach (var zombie in game.Zombies)
            {
                if (zombie.Row < 0 || zombie.Row >= game.RowCount || float.IsNaN(zombie.X) || float.IsInfinity(zombie.X)
                    || float.IsNaN(zombie.Health) || float.IsInfinity(zombie.Health))
                {
                    throw new InvalidDataException("僵尸的位置或生命值无效。");
                }
            }
        }

        /// <summary>Records the immutable identity used to validate the next save.</summary>
        private void CaptureIdentity()
        {
            _level = Game!.Adventure.Level.Number;
            _cycle = Game.Rules.AdventureCycle;
            _runId = Game.ModeSession?.RunId;
        }

        /// <summary>Rejects profile membership changes without accepting a new baseline.</summary>
        /// <exception cref="IOException">The profile catalog changed externally.</exception>
        private void EnsureCatalogCurrent()
        {
            if (!MatchCheckpointStore.MatchesFile(_catalogPath, _catalogBytes))
            {
                throw new IOException("用户目录已变化，请重新读取存档目录。");
            }
        }
    }
}
