#nullable enable
using System;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>Coordinates the original save-on-leave and continue/restart/cancel lifecycle.</summary>
    internal sealed class MatchContinuationController
    {
        /// <summary>The host whose board and modal state are coordinated.</summary>
        private readonly PvZGame _owner;
        /// <summary>The lazily opened profile-local checkpoint store.</summary>
        private MatchCheckpointStore? _store;
        /// <summary>Tracks a terminal board whose stale continuation was already erased.</summary>
        private GameSimulation? _retiredBoard;
        /// <summary>Throttles retries if a terminal checkpoint cannot be erased.</summary>
        private float _nextDeleteAttempt;
        /// <summary>Gets whether the original continue dialog currently owns input.</summary>
        internal bool DialogOpen { get; private set; }
        /// <summary>Gets whether the second, original restart confirmation is open.</summary>
        internal bool ConfirmingRestart { get; private set; }
        /// <summary>Gets the checkpoint backend belonging to the active profile catalog.</summary>
        internal MatchCheckpointStore Store => _store ??= new MatchCheckpointStore(_owner.RequiredProfiles.StorageDirectory);
        /// <summary>Gets whether the loaded board is an adventure level.</summary>
        internal bool IsAdventure => _owner.RequiredGame.ModeSession == null;

        /// <summary>Creates the board continuation coordinator.</summary>
        /// <param name="owner">The game host.</param>
        internal MatchContinuationController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Saves an original Playing scene, or finishes its pending end-level persistence.</summary>
        /// <returns>Whether leaving the board can proceed without losing progress.</returns>
        internal bool TrySaveActive()
        {
            if (DialogOpen || _owner._game == null || !_owner._game.IsAdventure || _owner.Campaign._adventureTestBoard
                || _owner._profiles?.SelectedId == null || !_owner.Menus.HasBoardForSaving)
            {
                return true;
            }
            var game = _owner.RequiredGame;
            var mode = game.ModeSession;
            try
            {
                if (game.State == MatchState.Lost)
                {
                    DeleteCurrent();
                    return true;
                }
                if (mode?.Finished == true)
                {
                    if (!game.FinishAdventureForSaving())
                    {
                        throw new InvalidOperationException("关卡金币尚未保存，请重试。");
                    }
                    if (!_owner.IndependentModes.SaveModeRecord(mode.Phase == Modes.ModePhase.Won && !_owner.IndependentModes._modeRewardClaimed))
                    {
                        throw new InvalidOperationException(_owner.IndependentModes._modeSaveError);
                    }
                    DeleteCurrent();
                    return true;
                }
                if (mode == null && game.State == MatchState.Won)
                {
                    if (!game.FinishAdventureForSaving())
                    {
                        throw new InvalidOperationException("关卡金币尚未保存，请重试。");
                    }
                    if (game.Adventure.Phase == AdventurePhase.Reward && !_owner.Campaign.RequiredAdventureCampaign.CollectReward())
                    {
                        throw new InvalidOperationException(_owner.Campaign.RequiredAdventureCampaign.LastPersistenceError);
                    }
                    DeleteCurrent();
                    return true;
                }
                // PC 414A00 only writes in scene Playing. An initial chooser must not
                // replace the previous survival checkpoint taken at the stage boundary.
                if (_owner.Opening.AdventureIntroActive || _owner._choosingSeeds)
                {
                    return true;
                }
                var nativeMode = mode?.Definition.Id ?? 0;
                if (!MatchCheckpointStore.SupportsMode(nativeMode))
                {
                    return true;
                }
                _owner._selected = -1;
                _owner._shovel = false;
                _owner.AdventureBoard.ClearAdventurePacketSelection();
                game.CancelAdventureCobCannonAim();
                game.Adventure.SelectedSeed = null;
                _owner.Opening.CaptureCheckpointPresentation();
                Store.Save(_owner.RequiredProfiles.RequiredSelectedId, nativeMode,
                    mode?.RunId.ToString("N") ?? _owner.Campaign.RequiredAdventureCampaign.AttemptId, game);
                if (mode?.Definition.Survival == true && !_owner.IndependentModes.SaveModeRecord(false))
                {
                    throw new InvalidOperationException(_owner.IndependentModes._modeSaveError);
                }
                Debug.Log($"{nameof(MatchContinuationController)}: Saved mode {nativeMode} board.");
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"{nameof(MatchContinuationController)}: Board save failed: {error.Message}");
                _owner.Menus.SetUnavailable("对局进度保存失败", error.Message);
                return false;
            }
        }

        /// <summary>Deletes the current profile's board when the original would abandon it.</summary>
        internal void DeleteCurrent()
        {
            if (_owner._profiles?.SelectedId != null && _owner._game?.IsAdventure == true && !_owner.Campaign._adventureTestBoard)
            {
                Store.Delete(_owner.RequiredProfiles.RequiredSelectedId, _owner.RequiredGame.ModeSession?.Definition.Id ?? 0);
            }
        }

        /// <summary>Erases stale survival backups on terminal loss or victory, including paths returning directly to a directory.</summary>
        internal void DiscardFinishedCheckpoint()
        {
            var game = _owner._game;
            if (game == null || DialogOpen || !_owner.Menus.HasBoardForSaving || ReferenceEquals(game, _retiredBoard)
                || Time.unscaledTime < _nextDeleteAttempt || game.State != MatchState.Lost
                && (game.State != MatchState.Won || game.ModeSession?.HasNextStage == true))
            {
                return;
            }
            try
            {
                DeleteCurrent();
                _retiredBoard = game;
            }
            catch (Exception error)
            {
                _nextDeleteAttempt = Time.unscaledTime + 2f;
                Debug.LogWarning($"{nameof(MatchContinuationController)}: Could not retire checkpoint: {error.Message}");
            }
        }

        /// <summary>Opens the original modal after a verified saved board has been attached.</summary>
        internal void OpenContinueDialog()
        {
            if (_owner.RequiredGame.State == MatchState.Paused)
            {
                _owner.RequiredGame.TogglePause();
            }
            DialogOpen = true;
            ConfirmingRestart = false;
            _owner._presenter.PrepareContinueDialog();
        }

        /// <summary>Routes native continue/restart/cancel controls ahead of board interaction.</summary>
        internal void HandleInput()
        {
            var input = GameInput.Current;
            if (input.EscapePressed)
            {
                Cancel();
                return;
            }
            if (input.EnterPressed || input.SpacePressed)
            {
                Accept();
                return;
            }
            if (!input.Pressed || !input.InsideViewport)
            {
                return;
            }
            var dialog = _owner._presenter.RequiredContinueDialog;
            if (dialog.YesBounds.Contains(input.Position))
            {
                _owner.Sounds.PlayAdventureRewardSound("gravebutton.ogg");
                Accept();
            }
            else if (dialog.NoBounds.Contains(input.Position))
            {
                _owner.Sounds.PlayAdventureRewardSound("gravebutton.ogg");
                if (ConfirmingRestart)
                {
                    Cancel();
                }
                else
                {
                    ConfirmingRestart = true;
                    _owner._presenter.PrepareContinueDialog();
                }
            }
            else if (!ConfirmingRestart && dialog.CancelBounds.Contains(input.Position))
            {
                _owner.Sounds.PlayAdventureRewardSound("gravebutton.ogg");
                Cancel();
            }
        }

        /// <summary>Continues the loaded board or confirms discarding it and starting again.</summary>
        internal void Accept()
        {
            try
            {
                if (ConfirmingRestart)
                {
                    DeleteCurrent();
                    DialogOpen = false;
                    ConfirmingRestart = false;
                    _owner.ResetGame();
                    return;
                }
                // PC ContinueDialog::ButtonDepress 43EDF0 preserves the survival
                // backup at countdown one until the next stage replaces it.
                if (_owner.RequiredGame.ModeSession?.ReSelectionTicks != 1)
                {
                    DeleteCurrent();
                }
                DialogOpen = false;
            }
            catch (Exception error)
            {
                _owner.Menus.SetUnavailable("对局进度无法更新", error.Message);
            }
        }

        /// <summary>Cancels restart confirmation, or returns to the original mode directory without erasing the save.</summary>
        internal void Cancel()
        {
            if (ConfirmingRestart)
            {
                ConfirmingRestart = false;
                _owner._presenter.PrepareContinueDialog();
                return;
            }
            DialogOpen = false;
            var mode = _owner.RequiredGame.ModeSession?.Definition.Id ?? 0;
            if (mode == 0)
            {
                _owner._currentView = PvZGame.ScreenView.Selector;
            }
            else
            {
                _owner.Menus.OpenModes(mode <= 15 ? 0 : mode >= 51 ? 3 : 1);
            }
        }
    }
}
