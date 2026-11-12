#nullable enable
using System;
using System.IO;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Progression
{
    /// <summary>Opens the selected profile's coin and garden account and reports account failures to the interface.</summary>
    internal sealed class PlayerAccountSession
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose selected profile owns the account.</param>
        internal PlayerAccountSession(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores account store.</summary>
        internal PlayerAccountStore _accountStore;
        /// <summary>Stores display account snapshot.</summary>
        internal PlayerAccount _displayAccountSnapshot;
        /// <summary>Stores account error.</summary>
        private string _accountError;
        /// <summary>Stores progression message.</summary>
        internal string _progressionMessage;

        /// <summary>Performs ensure player account.</summary>
        /// <returns>The resulting ensure player account.</returns>
        internal bool EnsurePlayerAccount()
        {
            if (_accountStore != null)
            {
                return _owner._profiles != null && _owner._profiles.SelectedId.HasValue;
            }

            if (_accountError != null)
            {
                return false;
            }

            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                if (_owner._profileDialog != null)
                {
                    _owner._profileDialog.Open();
                }

                return false;
            }

            try
            {
                _accountStore = new PlayerAccountStore(Path.Combine(_owner._smokeMode ? Application.temporaryCachePath : Application.persistentDataPath, _owner._smokeMode ? "pvz-account-" + Guid.NewGuid().ToString("N") + ".account" : "replica-player.account"));
                return true;
            }
            catch (Exception e)
            {
                _accountError = e.Message;
                _owner.Menus.SetUnavailable("金币档案无法读取", e.Message);
                return false;
            }
        }

        /// <summary>Performs get selected account.</summary>
        /// <returns>The resulting get selected account.</returns>
        internal PlayerAccount GetSelectedAccount()
        {
            return EnsurePlayerAccount() ? _accountStore.Get(_owner._profiles.SelectedId.Value) : null;
        }

        /// <summary>Performs progression action.</summary>
        /// <param name = "action">The action.</param>
        internal void ProgressionAction(Action action)
        {
            try
            {
                _progressionMessage = null;
                action();
            }
            catch (Exception e)
            {
                _progressionMessage = e.Message;
            }
        }
    }
}
