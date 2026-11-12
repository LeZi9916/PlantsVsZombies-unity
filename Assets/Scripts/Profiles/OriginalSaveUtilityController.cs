#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Profiles
{
    /// <summary>Reads, exports and imports the original game's user saves for the developer save utility screen.</summary>
    internal sealed class OriginalSaveUtilityController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose profiles and accounts receive imported saves.</param>
        internal OriginalSaveUtilityController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Opens the save utility when the developer shortcut is pressed outside the board and dialogs.</summary>
        internal void OpenWhenRequested()
        {
            if (Input.GetKeyDown(KeyCode.F9) && _owner._currentView != PvZGame.ScreenView.Board && !_owner._optionsView.IsOpen
                && (_owner._profileDialog == null || !_owner._profileDialog.IsOpen))
            {
                _owner._currentView = PvZGame.ScreenView.Import;
                _owner.Accounts._progressionMessage = null;
            }
        }

        /// <summary>Stores original import path.</summary>
        internal string _originalImportPath = "";
        /// <summary>Stores original export path.</summary>
        internal string _originalExportPath = "";
        /// <summary>Stores import candidates.</summary>
        internal IReadOnlyList<OriginalUserSave>? _importCandidates;
        /// <summary>Stores import selection.</summary>
        internal int _importSelection;

        /// <summary>Reads the original save candidates requested by the import screen.</summary>
        internal void ReadOriginalSaves()
        {
            _owner.Accounts.ProgressionAction(() =>
            {
                _importCandidates = Directory.Exists(_originalImportPath) ? OriginalSaveFormat.ReadDirectory(_originalImportPath) : Array.AsReadOnly(new[] { OriginalSaveFormat.ReadUser(_originalImportPath) });
                _importSelection = 0;
            });
        }

        /// <summary>Exports the selected account through the original save format.</summary>
        internal void ExportOriginalSave()
        {
            _owner.Accounts.ProgressionAction(() =>
            {
                if (!_owner.Accounts.EnsurePlayerAccount() || !_owner.Campaign.EnsureAdventureStore())
                {
                    return;
                }

                var copy = Path.Combine(Application.persistentDataPath, "original-" + _owner._profiles.SelectedId.Value.ToString("N") + ".dat");
                OriginalSaveFormat.ExportUser(copy, _originalExportPath, _owner.Accounts.GetSelectedAccount(), _owner.Campaign._adventureStore.Get(_owner._profiles.SelectedId.Value));
                _owner.Accounts._progressionMessage = "导出完成：" + _originalExportPath;
            });
        }

        /// <summary>Returns from the original save utility to the selector.</summary>
        internal void ReturnFromSaveUtility()
        {
            _owner._currentView = PvZGame.ScreenView.Selector;
        }

        /// <summary>Performs import original user.</summary>
        internal void ImportOriginalUser()
        {
            _owner.Accounts.ProgressionAction(ImportOriginalUserCore);
        }

        /// <summary>Imports the selected original user and its campaign/account state.</summary>
        private void ImportOriginalUserCore()
        {
            if (_importCandidates == null || _importSelection >= _importCandidates.Count || !_owner.Campaign.EnsureAdventureStore() || !_owner.Accounts.EnsurePlayerAccount())
            {
                return;
            }

            var source = _importCandidates[_importSelection];
            var name = source.Name;
            if (name.Length > 26)
            {
                name = name.Substring(0, 26);
            }

            var suffix = 1;
            bool used;
            do
            {
                used = false;
                foreach (var existing in _owner._profiles.Profiles)
                {
                    if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        used = true;
                        break;
                    }
                }

                if (used)
                {
                    name = source.Name.Substring(0, Math.Min(24, source.Name.Length)) + "（" + suffix++ + "）";
                }
            }
            while (used);
            var importedId = Guid.NewGuid();
            var copy = Path.Combine(Application.persistentDataPath, "original-" + importedId.ToString("N") + ".dat");
            OriginalSaveFormat.KeepImportedSource(source, copy);
            _owner.Accounts._accountStore.Import(importedId, source.Coins, source.Purchases, source.ChallengeRecords, source.ChallengeRecords[49], source.Plants, source.MinigamesUnlocked, source.PuzzleUnlocked, source.SurvivalUnlocked, source.PacketUpgradeDeclines, source.TreeFoodGiftClaimed, source.GardenGiftClaimed, source.GardenFertilizerInitialized);
            _owner.Campaign._adventureStore.Import(importedId, source.Level, source.FinishedAdventure);
            _owner._profiles.CreateImported(importedId, name);
            _owner.Accounts._progressionMessage = "已导入用户：" + name;
            _owner._currentView = PvZGame.ScreenView.Selector;
        }
    }
}
