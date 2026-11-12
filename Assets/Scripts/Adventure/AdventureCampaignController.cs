#nullable enable
using PlantsVsZombies.Simulation;
using System;
using System.IO;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Opens the adventure save, starts or restarts campaign levels and attaches each level's simulation to the game session.</summary>
    internal sealed class AdventureCampaignController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose profile, account and board the campaign drives.</param>
        internal AdventureCampaignController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores adventure store.</summary>
        internal AdventureProgressStore _adventureStore;
        /// <summary>Stores adventure campaign.</summary>
        internal AdventureCampaign _adventureCampaign;
        /// <summary>Stores adventure save path.</summary>
        private string _adventureSavePath;
        /// <summary>Stores adventure load error.</summary>
        private string _adventureLoadError;
        /// <summary>Stores adventure test board.</summary>
        internal bool _adventureTestBoard;
        /// <summary>Gets or sets use legacy board.</summary>
        internal bool UseLegacyBoard
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                return _owner._experimentalLevelOne || Array.IndexOf(args, "-pvz-demo") >= 0 || (_owner._smokeMode && Array.IndexOf(args, "-pvz-combat-fidelity-smoke") < 0 && Array.IndexOf(args, "-pvz-intro-audio-smoke") < 0 && Array.IndexOf(args, "-pvz-feedback-smoke") < 0 && Array.IndexOf(args, "-pvz-progression-smoke") < 0 && Array.IndexOf(args, "-pvz-adventure-smoke") < 0 && Array.IndexOf(args, "-pvz-ui-review") < 0);
            }
        }

        /// <summary>Performs ensure adventure store.</summary>
        /// <returns>The resulting ensure adventure store.</returns>
        internal bool EnsureAdventureStore()
        {
            if (_adventureStore != null)
            {
                return true;
            }

            if (_adventureLoadError != null)
            {
                return false;
            }

            try
            {
                if (_adventureSavePath == null)
                {
                    _adventureSavePath = _owner._smokeMode ? Path.Combine(Application.temporaryCachePath, "pvz-campaign-" + Guid.NewGuid().ToString("N") + ".adventure") : Path.Combine(Application.persistentDataPath, "replica-campaign.adventure");
                }

                _adventureStore = new AdventureProgressStore(_adventureSavePath);
                return true;
            }
            catch (Exception error)
            {
                _adventureLoadError = error.Message;
                return false;
            }
        }

        /// <summary>Performs start adventure campaign.</summary>
        /// <returns>The resulting start adventure campaign.</returns>
        internal bool StartAdventureCampaign()
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                if (_owner._profileDialog != null)
                {
                    _owner._profileDialog.Open();
                }

                return false;
            }

            if (!EnsureAdventureStore() || !_owner.Accounts.EnsurePlayerAccount())
            {
                _owner.Menus.SetUnavailable("冒险进度无法读取", _adventureLoadError);
                return false;
            }

            try
            {
                _adventureCampaign = new AdventureCampaign(_adventureStore, _owner._profiles.SelectedId.Value, _owner.Accounts._accountStore);
                if (_owner.Accounts.GetSelectedAccount().ZenGardenTutorialStage > 0)
                {
                    _owner.Garden.OpenZenGardenForAdventureIntro();
                    return true;
                }

                if (_adventureCampaign.Progress.Completed)
                {
                    _adventureStore.StartNextCycle(_owner._profiles.SelectedId.Value);
                }

                _adventureTestBoard = false;
                AttachAdventureGame(_adventureCampaign.BeginOrResume());
                _owner._currentView = PvZGame.ScreenView.Board;
                var account = _owner.Accounts.GetSelectedAccount();
                if (_adventureCampaign.Progress.NextLevel == 35 && account.HasPendingTacoReward && !account.TacoDiamondPresented)
                {
                    _owner.Store.OpenStoreForAdventureIntro(601);
                    _owner.Store._storeAdventureGamePrepared = true;
                }

                return true;
            }
            catch (Exception error)
            {
                _owner.Menus.SetUnavailable("冒险关卡无法开始", error.Message);
                return false;
            }
        }

        /// <summary>Performs attach adventure game.</summary>
        /// <param name = "simulation">The simulation.</param>
        /// <param name="isRestart">Whether this board follows a loss or an explicit restart.</param>
        internal void AttachAdventureGame(GameSimulation simulation, bool isRestart = false)
        {
            _owner._game = simulation;
            _owner.AdventureBoard._hammerAnimationAge = 1f;
            _owner.Opening._adventureOpeningIsRestart = isRestart;
            _owner._animations.ResetNativePlayback();
            _owner._game.EventRaised = _owner.Sounds.PlaySound;
            _owner._game.Adventure.NoticeRaised = AdventureNotice;
            _owner._game.Adventure.VaseInterludeDialogueEnabled = true;
            _owner.WaveNotices.InitializeAdventureWaveNotices();
            _owner._deck.Clear();
            foreach (PlantKind seed in _owner._game.Adventure.SelectedSeeds)
            {
                _owner._deck.Add(seed);
            }

            if (_owner.IndependentModes._modeSession == null && _owner._game.Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                _owner._deck.AddRange(new[] { PlantKind.PotatoMine, PlantKind.GraveBuster, PlantKind.CherryBomb });
            }

            _owner._choosingSeeds = _owner._game.Adventure.Phase == AdventurePhase.ChoosingSeeds;
            _owner._pendingSeeds = new AdventureSeedChooserState(_owner._game.Adventure.SelectedSeeds);
            _owner._selected = -1;
            _owner.AdventureBoard._selectedAdventurePacket = -1;
            _owner._shovel = false;
            _owner.AdventureBoard._adventureFeedback = null;
            _owner.AdventureBoard._choosingImitater = false;
            _owner.Opening.InitializeAdventureOpening();
        }

        /// <summary>Performs restart adventure.</summary>
        internal void RestartAdventure()
        {
            if (_adventureTestBoard)
            {
                StartAdventureTestLevel(_owner._game.Adventure.Level.Number);
                return;
            }

            try
            {
                AttachAdventureGame(_adventureCampaign.BeginOrResume(), isRestart: true);
            }
            catch (Exception error)
            {
                _owner.Menus.SetUnavailable("冒险关卡无法重新开始", error.Message);
            }
        }

        /// <summary>Performs start adventure test level.</summary>
        /// <param name = "number">The number.</param>
        /// <param name = "access">The access.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void StartAdventureTestLevel(int number, AdventureSeedAccess access = null)
        {
            if (!_owner._smokeMode)
            {
                throw new InvalidOperationException("Test boards require an isolated smoke session.");
            }

            _adventureTestBoard = true;
            AttachAdventureGame(new GameSimulation(new GameRules { AdventureLevel = number, AdventureSeedAccess = access }, number * 104729));
            _owner._currentView = PvZGame.ScreenView.Board;
        }

        /// <summary>Performs adventure notice.</summary>
        /// <param name = "notice">The notice.</param>
        private void AdventureNotice(AdventureNotice notice)
        {
            if (notice.Kind == AdventureNoticeKind.VaseInterludeStarted)
            {
                _owner.Opening.StartVaseInterlude(notice.Value);
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollectionStarted)
            {
                _owner.Sounds.PlayAdventureCoinPickupSound((AdventureCoinKind)notice.Value);
                _owner._presenter.ShowWalletUntil(_owner._game.Elapsed + 10f);
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollected)
            {
                _owner.IndependentModes._modeAccountSnapshot = null;
                _owner.Accounts._displayAccountSnapshot = null;
                _owner._presenter.ShowWalletUntil(_owner._game.Elapsed + 10f);
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollectionFailed)
            {
                _owner.AdventureBoard.AdventureFeedback(_owner.IndependentModes._modeSaveError ?? ("金币尚未保存：" + (_adventureCampaign?.LastPersistenceError ?? "请重试")), 5f);
            }

            _owner.Sounds.HandleAdventureRewardNoticeSound(notice);
        }
    }
}
