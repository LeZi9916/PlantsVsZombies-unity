#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using UnityEngine;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI.BitmapFonts;
using System.Collections;
using System.Collections.Generic;

namespace PlantsVsZombies.Game
{
    /// <summary>Routes the frame's polled input to the active screen, seed chooser and board.</summary>
    internal sealed class GameInputRouter
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose screens receive the routed input.</param>
        internal GameInputRouter(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores smoke pointer.</summary>
        private Vector2? _smokePointer = null; // Explicit Player scenarios only; normal input uses the frame snapshot.

        /// <summary>Routes one polled input snapshot before advancing the game.</summary>
        internal void HandleInput()
        {
            var input = GameInput.Current;
            var mouse = _owner._smokeMode && _smokePointer.HasValue ? _smokePointer.Value : input.Position;
            _owner._presenter.SetPointerPosition(mouse);
            _owner.AdventureBoard._adventurePointer = mouse;
            _owner._selectorView.HoveredAction = _owner._selectorView.HitTest(mouse);
            if (_owner.Loading._originalLoadingActive)
            {
                if (input.Released && _owner.Loading.OriginalLoadingReady && !(_owner._smokeMode && _owner.Loading._originalShowLoadingForCapture))
                {
                    var ticks = Math.Max(0, _owner.Loading._originalTitleScreenTicks);
                    var y = ticks > 10 ? OriginalLoadingController.OriginalTitleCurve(60, 10, ticks, 650, 534, false) : OriginalLoadingController.OriginalTitleCurve(10, 0, ticks, 534, 529, true);
                    if (_owner.Loading._originalTitleTicks > 400 && new Rect(240, y, 314, 50).Contains(mouse))
                    {
                        _owner.Loading.CompleteOriginalLoading(true);
                    }
                }

                return;
            }

            if (_owner._profileDialog != null && _owner._profileDialog.IsOpen)
            {
                if (input.Pressed)
                {
                    _owner._profileDialog.HandleClick(mouse);
                }

                return;
            }

            if (_owner._optionsView.IsOpen)
            {
                for (var index = 0; index < input.PhaseCount && _owner._optionsView.IsOpen; index++)
                {
                    _owner._optionsView.HandlePointer(mouse, input.GetPhase(index));
                }

                if (!_owner._optionsView.IsOpen)
                {
                    _owner.Menus.ApplyOptions();
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Store && _owner.Store._storeIntroTalking && !_owner._confirmingQuit && _owner._unavailableFeature == null
                && (input.EnterPressed || Input.GetKeyDown(KeyCode.Space)))
            {
                _owner.Store.AdvanceStoreIntro();
                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Board && _owner.Opening._packetUpgradeOfferActive && !_owner._confirmingQuit && _owner._unavailableFeature == null)
            {
                if (input.Pressed && input.InsideViewport)
                {
                    if (AdventureOpeningController.s_packetOfferYes.Contains(mouse))
                    {
                        _owner.Opening.ResolvePacketUpgradeOffer(true);
                    }
                    else if (AdventureOpeningController.s_packetOfferNo.Contains(mouse))
                    {
                        _owner.Opening.ResolvePacketUpgradeOffer(false);
                    }
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Board && _owner.Dave._crazyDaveMessageIndex >= 0 && !_owner._confirmingQuit && _owner._unavailableFeature == null)
            {
                if ((input.Pressed && input.InsideViewport) || input.EnterPressed || Input.GetKeyDown(KeyCode.Space))
                {
                    _owner.Opening.AdvanceCrazyDaveDialogue();
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Garden && _owner.Garden._gardenSalePlant.HasValue && !_owner._confirmingQuit && _owner._unavailableFeature == null)
            {
                if (input.Pressed && input.InsideViewport)
                {
                    if (ZenGardenController.s_gardenSaleYes.Contains(mouse))
                    {
                        _owner.Garden.ResolveGardenSale(true);
                    }
                    else if (ZenGardenController.s_gardenSaleNo.Contains(mouse))
                    {
                        _owner.Garden.ResolveGardenSale(false);
                    }
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Garden && _owner.Garden._gardenDaveIntroActive && _owner.Dave._crazyDaveMessageIndex >= 0)
            {
                if ((input.Pressed && input.InsideViewport) || input.EnterPressed || Input.GetKeyDown(KeyCode.Space))
                {
                    _owner.Garden.AdvanceZenGardenIntroduction();
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Tree && _owner.Tree._treeDaveIntroPending)
            {
                if (_owner.Dave._crazyDaveMessageIndex == 3200 && ((input.Pressed && input.InsideViewport) || input.EnterPressed || Input.GetKeyDown(KeyCode.Space)))
                {
                    _owner.Tree.CompleteWisdomTreeIntroduction();
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Board && _owner.Opening._vaseDialogueActive)
            {
                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Almanac)
            {
                if (input.ScrollDelta != 0f)
                {
                    _owner._almanacView.HandleScroll(mouse, input.ScrollDelta);
                }

                for (var index = 0; index < input.PhaseCount && _owner._currentView == PvZGame.ScreenView.Almanac; index++)
                {
                    _owner.Menus.HandleAlmanacPointer(mouse, input.GetPhase(index));
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Help)
            {
                for (var index = 0; index < input.PhaseCount && _owner._currentView == PvZGame.ScreenView.Help; index++)
                {
                    HandleHelpPointer(mouse, input.GetPhase(index));
                }

                return;
            }

            if (_owner._currentView != PvZGame.ScreenView.Import && input.Pressed && input.InsideViewport)
            {
                HandleClick(mouse);
            }
            else if (input.RightPressed && _owner._currentView == PvZGame.ScreenView.Board && !_owner._choosingSeeds && _owner._unavailableFeature == null && !_owner._confirmingQuit)
            {
                CancelBoardSelection();
            }
        }

        /// <summary>Performs handle click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleClick(Vector2 point)
        {
            if (_owner._confirmingQuit)
            {
                if (new Rect(245, 346, 135, 46).Contains(point))
                {
                    _owner.RequestQuit(0);
                }
                else if (new Rect(420, 346, 135, 46).Contains(point))
                {
                    _owner._confirmingQuit = false;
                }

                return;
            }

            if (_owner._unavailableFeature != null)
            {
                if (new Rect(320, 330, 160, 42).Contains(point))
                {
                    _owner._unavailableFeature = null;
                }

                return;
            }

            if (HandleProgressionClick(point))
            {
                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Selector)
            {
                HandleSelector(_owner._selectorView.HitTest(point));
                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Modes)
            {
                _owner._modeView.HandleClick(point);
                if (_owner._modeView.Closed)
                {
                    _owner._currentView = PvZGame.ScreenView.Selector;
                }

                return;
            }

            if (_owner._currentView == PvZGame.ScreenView.Almanac)
            {
                _owner._almanacView.HandleClick(point);
                _owner.Menus.ReturnFromAlmanacIfClosed();
                return;
            }

            if (_owner.IndependentModes.TryHandleModeClick(point))
            {
                return;
            }

            if (_owner._game.IsAdventure && _owner._currentView == PvZGame.ScreenView.Board)
            {
                _owner.AdventureBoard.HandleAdventureClick(point);
                return;
            }

            if (_owner._choosingSeeds)
            {
                HandleSeedChooserClick(point);
                return;
            }

            if (_owner._game.State == MatchState.Won || _owner._game.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    _owner.ResetGame();
                }

                return;
            }

            if (new Rect(704, 6, 90, 34).Contains(point))
            {
                _owner.Menus.OpenOptions();
                return;
            }

            if (_owner._game.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point))
                {
                    _owner._game.TogglePause();
                }
                else if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    _owner.ResetGame();
                }

                return;
            }

            if (!_owner._game.IsExperimentalLevelOne && new Rect(596, 6, 100, 34).Contains(point))
            {
                _owner._choosingSeeds = true;
                _owner._pendingSeeds = new AdventureSeedChooserState(_owner._deck);
                return;
            }

            if (_owner._game.LevelOneUsesNativeSkySunRules)
            {
                int x = Mathf.FloorToInt(point.x), y = Mathf.FloorToInt(point.y);
                for (var i = _owner._game.LevelOneNativeSkySuns.Count - 1; i >= 0; i--)
                {
                    var sun = _owner._game.LevelOneNativeSkySuns[i];
                    if (_owner._game.TryClickLevelOneNativeSkySun(sun, x, y))
                    {
                        return;
                    }
                }
            }

            for (var i = _owner._game.Suns.Count - 1; i >= 0; i--)
            {
                if (BoardLayout.SunRect(_owner._game.Suns[i]).Contains(point))
                {
                    _owner._game.CollectSun(_owner._game.Suns[i]);
                    return;
                }
            }

            for (var i = 0; i < _owner._deck.Count; i++)
            {
                if (!_owner.Layout.SeedRect(i).Contains(point))
                {
                    continue;
                }

                if (_owner._game.IsExperimentalLevelOne)
                {
                    if (_owner._game.LevelOneSelectedSeed.HasValue)
                    {
                        if (_owner._game.CancelLevelOneSeedSelection())
                        {
                            _owner._selected = -1;
                        }
                    }
                    else if (_owner._game.TrySelectLevelOneSeed(_owner._deck[i]))
                    {
                        _owner._selected = i;
                    }
                    else
                    {
                        _owner.AdventureBoard.RejectSeedClick(_owner._game.CooldownRemaining(_owner._deck[i]) <= 0f && _owner._game.SunAmount < GameSimulation.Cost(_owner._deck[i]));
                    }

                    _owner._shovel = false;
                }
                else if (_owner._game.CooldownRemaining(_owner._deck[i]) <= 0f && _owner._game.SunAmount >= GameSimulation.Cost(_owner._deck[i]))
                {
                    _owner._selected = _owner._selected == i ? -1 : i;
                    _owner._shovel = false;
                }
                else
                {
                    _owner.AdventureBoard.RejectSeedClick(_owner._game.CooldownRemaining(_owner._deck[i]) <= 0f && _owner._game.SunAmount < GameSimulation.Cost(_owner._deck[i]));
                }

                return;
            }

            if (!_owner._game.IsExperimentalLevelOne && new Rect(522, 0, 70, 72).Contains(point))
            {
                _owner._shovel = !_owner._shovel;
                _owner._selected = -1;
                return;
            }

            var column = Mathf.FloorToInt((point.x - BoardLayout.GridX) / BoardLayout.CellWidth);
            var row = Mathf.FloorToInt((point.y - BoardLayout.GridY) / BoardLayout.CellHeight);
            if (row < 0 || row >= GameSimulation.Rows || column < 0 || column >= GameSimulation.Columns)
            {
                return;
            }

            if (_owner._shovel)
            {
                if (_owner._game.RemovePlant(row, column))
                {
                    _owner._shovel = false;
                }
            }
            else if (_owner._selected >= 0 && _owner._game.TryPlant(_owner._deck[_owner._selected], row, column))
            {
                _owner._selected = -1;
            }
        }

        /// <summary>Performs cancel board selection.</summary>
        internal void CancelBoardSelection()
        {
            _owner.AdventureBoard._selectedAdventurePacket = -1;
            if (_owner._game.IsAdventure)
            {
                _owner._game.CancelAdventureCobCannonAim();
            }

            if (_owner._game.State != MatchState.Playing)
            {
                return;
            }

            if (_owner._game.IsExperimentalLevelOne && _owner._game.LevelOneSelectedSeed.HasValue && !_owner._game.CancelLevelOneSeedSelection())
            {
                return;
            }

            _owner._selected = -1;
            _owner._shovel = false;
        }

        /// <summary>Performs handle selector.</summary>
        /// <param name = "action">The action.</param>
        private void HandleSelector(SelectorAction action)
        {
            switch (action)
            {
                case SelectorAction.Adventure:
                    if (!_owner.Campaign.UseLegacyBoard)
                    {
                        _owner.Campaign.StartAdventureCampaign();
                        break;
                    }

                    _owner.ResetGame();
                    _owner._currentView = PvZGame.ScreenView.Board;
                    break;
                case SelectorAction.MiniGames:
                    _owner.Menus.OpenModes(1);
                    break;
                case SelectorAction.Puzzle:
                    _owner.Menus.OpenModes(3);
                    break;
                case SelectorAction.Survival:
                    _owner.Menus.OpenModes(0);
                    break;
                case SelectorAction.Almanac:
                    _owner.Menus.OpenAlmanac(PvZGame.ScreenView.Selector);
                    break;
                case SelectorAction.Options:
                    _owner.Menus.OpenOptions();
                    break;
                case SelectorAction.Quit:
                    _owner._confirmingQuit = true;
                    break;
                case SelectorAction.ZenGarden:
                    _owner.Garden.OpenZenGarden();
                    break;
                case SelectorAction.Store:
                    _owner.Store.OpenStore();
                    break;
                case SelectorAction.Help:
                    _owner._helpView.Open();
                    _owner._currentView = PvZGame.ScreenView.Help;
                    break;
                case SelectorAction.ChangeUser:
                    if (_owner._profileDialog != null)
                    {
                        _owner._profileDialog.Open();
                    }
                    else
                    {
                        _owner.Menus.SetUnavailable("用户目录无法读取", "原文件未更改");
                    }

                    break;
                case SelectorAction.Zombatar:
                    _owner.Menus.SetUnavailable("僵尸大头贴", "尚未实现");
                    break;
            }
        }

        /// <summary>Performs handle help pointer.</summary>
        /// <param name = "point">The point.</param>
        /// <param name = "eventType">The event type.</param>
        /// <returns>The resulting handle help pointer.</returns>
        private bool HandleHelpPointer(Vector2 point, PointerPhase eventType)
        {
            var consumed = _owner._helpView.HandlePointer(point, eventType);
            if (!_owner._helpView.IsOpen)
            {
                _owner._currentView = PvZGame.ScreenView.Selector;
            }

            return consumed;
        }

        /// <summary>Performs handle seed chooser click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleSeedChooserClick(Vector2 point)
        {
            _owner._pendingSeeds.LandFlyingSeeds();
            if (ReturnSelectedChooserSeed(point, 7, false, true))
            {
                return;
            }

            for (var i = 0; i < _owner._availablePlants.Length; i++)
            {
                var home = BoardLayout.ChooserSeedRect(i);
                if (home.Contains(point))
                {
                    _owner._pendingSeeds.Choose(_owner._availablePlants[i], (int)home.x, (int)home.y, 7);
                    return;
                }
            }

            if (new Rect(322, 440, 156, 42).Contains(point) && _owner._pendingSeeds.Count > 0)
            {
                _owner._deck.Clear();
                _owner._deck.AddRange(_owner._pendingSeeds);
                _owner._choosingSeeds = false;
                _owner.ResetGame();
            }
            else if (new Rect(341, 493, 118, 30).Contains(point))
            {
                _owner._choosingSeeds = false;
            }
        }

        /// <summary>Returns a clicked bank packet and compacts the later packets in native order.</summary>
        /// <param name="point">The logical pointer position.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <param name="mode">Whether the independent mode chooser supplies packet homes.</param>
        /// <param name="simple">Whether the development chooser supplies packet homes.</param>
        /// <returns>Whether a selected bank packet consumed the click.</returns>
        internal bool ReturnSelectedChooserSeed(Vector2 point, int slots, bool mode = false, bool simple = false)
        {
            for (var i = 0; i < _owner._pendingSeeds.Count; i++)
            {
                var kind = _owner._pendingSeeds[i];
                var origin = simple ? Vector2.zero : _owner.Layout.AdventureSeedBankOrigin;
                var bank = new Rect(origin.x + AdventureSeedSlotLayout.PositionX(i, slots), origin.y + 8f, 50, 70);
                if (!bank.Contains(point))
                {
                    continue;
                }

                if (!mode && !simple && _owner.AdventureBoard.ContainsRequiredSeed(kind))
                {
                    _owner.Sounds.PlaySeedBankBuzzer();
                    return true;
                }

                var index = simple ? Array.IndexOf(_owner._availablePlants, kind) : mode ? AdventureAvailableSeedIndex(kind) : -1;
                var home = simple ? BoardLayout.ChooserSeedRect(index) : mode ? BoardLayout.AdventureChooserRect(index) : _owner.Opening.AdventureCampaignChooserRect(kind);
                if (_owner._pendingSeeds.Return(kind, (int)home.x, (int)home.y, slots))
                {
                    _owner.Sounds.PlayAdventureRewardSound("tap2.ogg");
                }

                return true;
            }

            return false;
        }

        /// <summary>Finds a seed's current chooser grid index without creating an intermediate list.</summary>
        /// <param name="kind">The available seed to locate.</param>
        /// <returns>The available seed index, or minus one if it is unavailable.</returns>
        internal int AdventureAvailableSeedIndex(PlantKind kind)
        {
            var seeds = _owner._game.Adventure.AvailableSeeds;
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Routes the Escape and Enter or Space keys to dismiss dialogs, leave screens or pause the board.</summary>
        internal void HandleEscape()
        {
            if (GameInput.Current.EscapePressed)
            {
                if (_owner._optionsView.IsOpen)
                {
                    _owner.Menus.CloseOptions();
                }
                else if (_owner._profileDialog != null && _owner._profileDialog.IsOpen)
                {
                    _owner._profileDialog.HandleBack();
                }
                else if (_owner._confirmingQuit)
                {
                    _owner._confirmingQuit = false;
                }
                else if (_owner._unavailableFeature != null)
                {
                    _owner._unavailableFeature = null;
                }
                else if (_owner._currentView == PvZGame.ScreenView.Almanac)
                {
                    _owner._almanacView.HandleBack();
                    _owner.Menus.ReturnFromAlmanacIfClosed();
                }
                else if (_owner._currentView == PvZGame.ScreenView.Modes || _owner._currentView == PvZGame.ScreenView.Garden || _owner._currentView == PvZGame.ScreenView.Import)
                {
                    _owner._currentView = PvZGame.ScreenView.Selector;
                }
                else if (_owner._currentView == PvZGame.ScreenView.Store)
                {
                    _owner.Store.LeaveStore();
                }
                else if (_owner._currentView == PvZGame.ScreenView.Tree)
                {
                    _owner._currentView = PvZGame.ScreenView.Garden;
                }
                else if (_owner._currentView == PvZGame.ScreenView.Help)
                {
                    _owner._helpView.HandleKey(KeyCode.Escape);
                    _owner._currentView = PvZGame.ScreenView.Selector;
                }
                else if (_owner._currentView == PvZGame.ScreenView.Board)
                {
                    if (_owner._choosingSeeds)
                    {
                        if (_owner._game.IsAdventure)
                        {
                            _owner._currentView = PvZGame.ScreenView.Selector;
                        }
                        else
                        {
                            _owner._choosingSeeds = false;
                        }
                    }
                    else if (_owner._game.State == MatchState.Playing)
                    {
                        _owner.Menus.OpenOptions();
                    }
                    else if (_owner._game.State == MatchState.Paused)
                    {
                        _owner._game.TogglePause();
                    }
                }
            }
            else if (_owner._currentView == PvZGame.ScreenView.Help && !_owner._optionsView.IsOpen && (GameInput.Current.EnterPressed || Input.GetKeyDown(KeyCode.Space)))
            {
                _owner._helpView.HandleKey(KeyCode.Return);
                _owner._currentView = PvZGame.ScreenView.Selector;
            }
        }

        /// <summary>Performs handle progression click.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting handle progression click.</returns>
        internal bool HandleProgressionClick(Vector2 point)
        {
            switch (_owner._currentView)
            {
                case PvZGame.ScreenView.Store:
                    return _owner.Store.HandleStoreClick(point);
                case PvZGame.ScreenView.Tree:
                    return _owner.Tree.HandleTreeClick(point);
                case PvZGame.ScreenView.Import:
                    return true;
                case PvZGame.ScreenView.Garden:
                    return _owner.Garden.HandleGardenClick(point);
                default:
                    return false;
            }
        }
    }
}
