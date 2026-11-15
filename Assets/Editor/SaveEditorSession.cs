#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlantsVsZombies.Editor
{
    /// <summary>Opens replica save files and keeps detached drafts for one editor-selected profile.</summary>
    internal sealed class SaveEditorSession
    {
        /// <summary>Identifies the catalog whose membership must remain current before a save.</summary>
        private readonly string _catalogPath;

        /// <summary>Stores the catalog snapshot used to detect external profile changes.</summary>
        private byte[] _catalogBytes;

        /// <summary>Provides validated profile names and persistent identities.</summary>
        private ProfileCatalog _catalog;

        /// <summary>Preserves accounts belonging to every profile while saving the selected draft.</summary>
        private readonly PlayerAccountStore _accountStore;

        /// <summary>Preserves checkpoints belonging to every profile while saving the selected draft.</summary>
        private readonly AdventureProgressStore _adventureStore;

        /// <summary>Opens the three replica save files without creating a profile or changing the game's selection.</summary>
        /// <param name="directory">The existing directory containing profiles.dat and optional progression sidecars.</param>
        /// <exception cref="ArgumentException">The directory path is empty or invalid.</exception>
        /// <exception cref="ArgumentNullException">The directory path is null.</exception>
        /// <exception cref="InvalidDataException">The profile catalog is missing, empty or invalid, or a progression file is invalid.</exception>
        /// <exception cref="IOException">A save file cannot be read or the catalog changes while opening the session.</exception>
        /// <exception cref="UnauthorizedAccessException">The save files cannot be accessed.</exception>
        internal SaveEditorSession(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("请选择存档目录。", nameof(directory));
            }

            DirectoryPath = Path.GetFullPath(directory);
            _catalogPath = Path.Combine(DirectoryPath, "profiles.dat");
            if (!File.Exists(_catalogPath))
            {
                throw new InvalidDataException("该目录中没有 profiles.dat，请先在游戏中创建用户，或选择已有存档目录。");
            }

            _catalogBytes = File.ReadAllBytes(_catalogPath);
            _catalog = new ProfileCatalog(_catalogPath);
            if (_catalog.Profiles.Count == 0)
            {
                throw new InvalidDataException("存档目录中没有用户，请先在游戏中创建用户。");
            }

            _accountStore = new PlayerAccountStore(Path.Combine(DirectoryPath, "player.account"));
            _adventureStore = new AdventureProgressStore(Path.Combine(DirectoryPath, "campaign.adventure"));
            SelectedProfileId = _catalog.SelectedId ?? _catalog.Profiles[0].Id;
            Account = _accountStore.Get(SelectedProfileId);
            Adventure = _adventureStore.Get(SelectedProfileId);
            EnsureCatalogUnchanged();
        }

        /// <summary>Gets the absolute save directory opened by this session.</summary>
        internal string DirectoryPath { get; }

        /// <summary>Gets the immutable profiles available when the catalog was opened or last renamed.</summary>
        internal IReadOnlyList<UserProfile> Profiles
        {
            get
            {
                return _catalog.Profiles;
            }
        }

        /// <summary>Gets the profile selected only within the editor window.</summary>
        internal Guid SelectedProfileId { get; private set; }

        /// <summary>Gets the detached, editable account draft for the selected profile.</summary>
        internal PlayerAccount Account { get; private set; }

        /// <summary>Gets the detached, editable adventure draft for the selected profile.</summary>
        internal AdventureProgress Adventure { get; private set; }

        /// <summary>Gets whether recovered drafts were based on older files and must be discarded or reloaded before writing.</summary>
        internal bool HasStaleDraft { get; private set; }

        /// <summary>Gets a copy of the exact catalog baseline accepted by this session.</summary>
        internal byte[] CatalogBytes
        {
            get
            {
                return (byte[])_catalogBytes.Clone();
            }
        }

        /// <summary>Gets the exact accepted account baseline, or null when its file did not exist.</summary>
        internal byte[]? AccountBytes
        {
            get
            {
                return _accountStore.GetEditorSavedBytes();
            }
        }

        /// <summary>Gets the exact accepted adventure baseline, or null when its file did not exist.</summary>
        internal byte[]? AdventureBytes
        {
            get
            {
                return _adventureStore.GetEditorSavedBytes();
            }
        }

        /// <summary>Gets the selected profile's committed display name.</summary>
        /// <exception cref="ArgumentException">The profile is no longer present in the loaded catalog.</exception>
        internal string ProfileName
        {
            get
            {
                return RequireProfile(SelectedProfileId).Name;
            }
        }

        /// <summary>Loads fresh detached drafts for an existing profile without changing the catalog's game selection.</summary>
        /// <param name="id">The identity of a profile in the current catalog.</param>
        /// <exception cref="ArgumentException">The identity does not belong to the catalog.</exception>
        /// <exception cref="IOException">The catalog changed externally or cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog cannot be accessed.</exception>
        internal void SelectProfile(Guid id)
        {
            EnsureCatalogUnchanged();
            RequireProfile(id);
            var account = _accountStore.Get(id);
            var adventure = _adventureStore.Get(id);
            SelectedProfileId = id;
            Account = account;
            Adventure = adventure;
            HasStaleDraft = false;
        }

        /// <summary>Restores detached drafts after an editor domain reload without writing any save file.</summary>
        /// <param name="account">The recovered account draft for the selected profile.</param>
        /// <param name="adventure">The recovered adventure draft for the selected profile.</param>
        /// <param name="stale">Whether any original baseline differs from the newly opened save files.</param>
        /// <exception cref="ArgumentNullException">Either recovered draft is null.</exception>
        /// <exception cref="InvalidOperationException">A recovered draft does not belong to the selected profile.</exception>
        internal void RestoreEditorDraft(PlayerAccount account, AdventureProgress adventure, bool stale)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (adventure == null)
            {
                throw new ArgumentNullException(nameof(adventure));
            }

            if (account.ProfileId != SelectedProfileId || adventure.ProfileId != SelectedProfileId)
            {
                throw new InvalidOperationException("恢复的存档草稿与当前用户不一致。");
            }

            Account = account.Copy();
            Adventure = adventure.Copy();
            HasStaleDraft = stale;
        }

        /// <summary>Persists a new display name while keeping the profile identity and game selection.</summary>
        /// <param name="name">The new profile name, validated by the profile catalog.</param>
        /// <exception cref="ArgumentException">The name is invalid, duplicated or the profile no longer exists.</exception>
        /// <exception cref="InvalidOperationException">The catalog requires reopening after an earlier failed save.</exception>
        /// <exception cref="IOException">The catalog changed externally or the rename cannot be saved.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog cannot be accessed.</exception>
        internal void RenameProfile(string name)
        {
            EnsureDraftCurrent();
            EnsureCatalogUnchanged();
            RequireProfile(SelectedProfileId);
            _catalog.Rename(SelectedProfileId, name);
            using (var lease = new FileStream(_catalogPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                _catalogBytes = File.ReadAllBytes(_catalogPath);
                _catalog = new ProfileCatalog(_catalogPath);
                EnsureCatalogUnchanged();
                RequireProfile(SelectedProfileId);
            }
        }

        /// <summary>Validates and saves only the selected profile's complete account draft.</summary>
        /// <exception cref="InvalidDataException">The draft has an invalid identity or account data.</exception>
        /// <exception cref="ArgumentException">The selected profile no longer exists.</exception>
        /// <exception cref="InvalidOperationException">The account file cannot accept the draft.</exception>
        /// <exception cref="IOException">The catalog or account changed externally, is locked or cannot be saved.</exception>
        /// <exception cref="UnauthorizedAccessException">The save files cannot be accessed.</exception>
        internal void SaveAccount()
        {
            EnsureDraftCurrent();
            if (Account.ProfileId != SelectedProfileId)
            {
                throw new InvalidDataException("账户草稿的用户标识与当前用户不一致，请重新加载存档。");
            }

            using (var lease = AcquireCatalogLease())
            {
                _accountStore.SaveEditorSnapshot(Account);
                Account = _accountStore.Get(SelectedProfileId);
            }
        }

        /// <summary>Validates and saves only the selected profile's complete adventure draft.</summary>
        /// <exception cref="InvalidDataException">The draft has an invalid identity or inconsistent checkpoint data.</exception>
        /// <exception cref="ArgumentException">The selected profile no longer exists.</exception>
        /// <exception cref="InvalidOperationException">The adventure file cannot accept the draft.</exception>
        /// <exception cref="IOException">The catalog or adventure changed externally, is locked or cannot be saved.</exception>
        /// <exception cref="UnauthorizedAccessException">The save files cannot be accessed.</exception>
        internal void SaveAdventure()
        {
            EnsureDraftCurrent();
            if (Adventure.ProfileId != SelectedProfileId)
            {
                throw new InvalidDataException("冒险草稿的用户标识与当前用户不一致，请重新加载存档。");
            }

            using (var lease = AcquireCatalogLease())
            {
                _adventureStore.SaveEditorSnapshot(Adventure);
                Adventure = _adventureStore.Get(SelectedProfileId);
            }
        }

        /// <summary>Prevents a recovered draft from replacing newer files even after another editor domain reload.</summary>
        /// <exception cref="IOException">The recovered draft was based on older save files.</exception>
        private void EnsureDraftCurrent()
        {
            if (HasStaleDraft)
            {
                throw new IOException("恢复的草稿所依据的存档已经变化，请重新读取存档后再保存。");
            }
        }

        /// <summary>Locks profile membership until the caller finishes saving one progression sidecar.</summary>
        /// <returns>The catalog lock stream that the caller must dispose after saving.</returns>
        /// <exception cref="ArgumentException">The selected profile does not belong to the catalog.</exception>
        /// <exception cref="IOException">The catalog changed, is locked or cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog lock cannot be accessed.</exception>
        private FileStream AcquireCatalogLease()
        {
            var lease = new FileStream(_catalogPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try
            {
                EnsureCatalogUnchanged();
                RequireProfile(SelectedProfileId);
                return lease;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        /// <summary>Rejects writes based on a catalog that another game or editor session has changed.</summary>
        /// <exception cref="IOException">The catalog changed externally or cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The catalog cannot be accessed.</exception>
        private void EnsureCatalogUnchanged()
        {
            var current = File.ReadAllBytes(_catalogPath);
            if (current.Length == _catalogBytes.Length)
            {
                var equal = true;
                for (var index = 0; index < current.Length; index++)
                {
                    if (current[index] != _catalogBytes[index])
                    {
                        equal = false;
                        break;
                    }
                }

                if (equal)
                {
                    return;
                }
            }

            throw new IOException("用户目录已被游戏或其他窗口修改，请重新加载存档后重试。");
        }

        /// <summary>Finds an existing profile without accepting orphaned progression records.</summary>
        /// <param name="id">The identity that must exist in the loaded catalog.</param>
        /// <returns>The immutable profile snapshot with the requested identity.</returns>
        /// <exception cref="ArgumentException">The identity does not belong to the catalog.</exception>
        private UserProfile RequireProfile(Guid id)
        {
            foreach (var profile in _catalog.Profiles)
            {
                if (profile.Id == id)
                {
                    return profile;
                }
            }

            throw new ArgumentException("当前用户不在存档目录中，请重新加载存档。", nameof(id));
        }
    }
}
