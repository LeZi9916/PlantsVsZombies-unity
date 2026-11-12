#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Modes
{
    /// <summary>Starts, ticks, saves and handles pointer input for the independent puzzle, survival and mini-game modes.</summary>
    internal sealed class IndependentModeController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose board and profile the modes use.</param>
        internal IndependentModeController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores mode session.</summary>
        internal ModeSession _modeSession;
        /// <summary>Stores mode profile id.</summary>
        private Guid _modeProfileId;
        /// <summary>Stores mode first clear,  mode reward claimed,  mode choosing imitater.</summary>
        internal bool _modeFirstClear, _modeRewardClaimed, _modeChoosingImitater;
        /// <summary>Stores mode saved stage.</summary>
        private int _modeSavedStage = -1;
        /// <summary>Stores mode save error.</summary>
        internal string _modeSaveError;
        /// <summary>Stores mode account snapshot.</summary>
        internal PlayerAccount _modeAccountSnapshot;
        /// <summary>Stores mode has finished adventure.</summary>
        private bool _modeHasFinishedAdventure;
        /// <summary>Stores mode adventure next level.</summary>
        private int _modeAdventureNextLevel = 1;
        /// <summary>Stores mode account refresh,  mode next save attempt.</summary>
        private float _modeAccountRefresh, _modeNextSaveAttempt;
        /// <summary>Stores external challenge requested.</summary>
        public Action<int> ExternalChallengeRequested;
        /// <summary>Gets or sets independent mode music cue.</summary>
        internal MusicCue? IndependentModeMusicCue
        {
            get
            {
                if (!IsModeBoard)
                {
                    return null;
                }

                if (_owner._game.State == MatchState.Won || _owner._game.State == MatchState.Lost)
                {
                    return MusicCue.None;
                }

                if (_owner._choosingSeeds)
                {
                    return MusicCue.ChooseSeeds;
                }

                var m = _modeSession.Definition.Mechanic;
                if (_modeSession.Definition.Puzzle)
                {
                    return MusicCue.Puzzle;
                }

                if (m == ModeMechanic.Boss)
                {
                    return MusicCue.Boss;
                }

                if (m == ModeMechanic.Columns)
                {
                    return MusicCue.Conveyor;
                }

                if (m == ModeMechanic.Bowling || m == ModeMechanic.Whack || m == ModeMechanic.LittleZombies || m == ModeMechanic.Speed)
                {
                    return MusicCue.Minigame;
                }

                var level = _owner._game.Adventure.Level;
                return level.Terrain == AdventureTerrain.Fog ? MusicCue.Fog : level.IsNight ? MusicCue.Night : level.HasPool ? MusicCue.Pool : level.HasRoof ? MusicCue.Roof : MusicCue.Day;
            }
        }

        /// <summary>Gets or sets is mode board.</summary>
        internal bool IsModeBoard
        {
            get
            {
                return _modeSession != null && ReferenceEquals(_owner._game, _modeSession.Game) && _owner._currentView == PvZGame.ScreenView.Board;
            }
        }

        /// <summary>Stores mode back button.</summary>
        internal static readonly Rect s_modeBackButton = new Rect(620, 570, 174, 26);
        /// <summary>Stores mode next button.</summary>
        internal static readonly Rect s_modeNextButton = new Rect(306, 342, 188, 42);
        /// <summary>Stores mode restart button.</summary>
        internal static readonly Rect s_modeRestartButton = new Rect(306, 394, 188, 40);
        /// <summary>Stores mode exit button.</summary>
        internal static readonly Rect s_modeExitButton = new Rect(306, 444, 188, 40);
        /// <summary>Performs initialize modes.</summary>
        internal void InitializeModes()
        {
            _owner._modeView.LaunchRequested = StartIndependentMode;
            _owner._modeView.ProgressLookup = ReadModeRecord;
            _owner._modeView.LockReasonLookup = ReadModeLockReason;
            _owner._modeView.CanLaunch = id =>
            {
                ModeDefinition d;
                return ModeCatalog.TryGet(id, out d) ? ReadModeLockReason(id) == null : ExternalChallengeRequested != null && (id == 43 || id == 50);
            };
        }

        /// <summary>Performs read mode record.</summary>
        /// <param name = "modeId">The mode id.</param>
        /// <returns>The resulting read mode record.</returns>
        private int ReadModeRecord(int modeId)
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue || !_owner.Accounts.EnsurePlayerAccount())
            {
                return 0;
            }

            if (_modeAccountSnapshot == null || _modeAccountSnapshot.ProfileId != _owner._profiles.SelectedId.Value || Time.unscaledTime >= _modeAccountRefresh)
            {
                _modeAccountSnapshot = _owner.Accounts.GetSelectedAccount();
                if (_owner.Campaign.EnsureAdventureStore())
                {
                    var progress = _owner.Campaign._adventureStore.Get(_owner._profiles.SelectedId.Value);
                    _modeHasFinishedAdventure = progress.CompletedRuns > 0;
                    _modeAdventureNextLevel = progress.NextLevel;
                }

                _modeAccountRefresh = Time.unscaledTime + 1f;
            }

            return _modeAccountSnapshot == null ? 0 : _modeAccountSnapshot.ModeRecord(modeId - 1);
        }

        /// <summary>Performs read mode lock reason.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <returns>The resulting read mode lock reason.</returns>
        private string ReadModeLockReason(int originalModeId)
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return "请先选择用户。";
            }

            if (!_owner.Accounts.EnsurePlayerAccount() || !_owner.Campaign.EnsureAdventureStore())
            {
                return "无法读取账户或冒险进度。";
            }

            ReadModeRecord(originalModeId);
            var giftAccess = originalModeId <= 15 ? _modeAccountSnapshot.SurvivalUnlocked : originalModeId <= 35 ? _modeAccountSnapshot.MinigamesUnlocked : _modeAccountSnapshot.PuzzleUnlocked;
            if (!giftAccess && !ModeUnlockRules.CategoryUnlocked(originalModeId, _modeAdventureNextLevel, _modeHasFinishedAdventure))
            {
                return originalModeId <= 15 ? "通关冒险模式后解锁生存模式。" : originalModeId <= 35 ? "通过冒险模式3-2，领取小游戏礼物后解锁。" : "通过冒险模式4-6，领取解谜模式礼物后解锁。";
            }

            return ModeUnlockRules.LockReason(originalModeId, _modeHasFinishedAdventure, index => _modeAccountSnapshot.ModeRecord(index));
        }

        /// <summary>Performs start independent mode.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <returns>The resulting start independent mode.</returns>
        private bool StartIndependentMode(int originalModeId)
        {
            ModeDefinition definition;
            if (!ModeCatalog.TryGet(originalModeId, out definition))
            {
                if (ExternalChallengeRequested != null)
                {
                    ExternalChallengeRequested(originalModeId);
                    return true;
                }

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

            if (!_owner.Accounts.EnsurePlayerAccount())
            {
                return false;
            }

            var locked = ReadModeLockReason(originalModeId);
            if (locked != null)
            {
                _owner.Menus.SetUnavailable("挑战尚未解锁", locked);
                return false;
            }

            try
            {
                var account = _owner.Accounts.GetSelectedAccount();
                _modeProfileId = _owner._profiles.SelectedId.Value;
                var record = account.ModeRecord(definition.RecordIndex);
                var progress = _owner.Campaign._adventureStore.Get(_modeProfileId);
                var basicSeeds = progress.CompletedRuns > 0 ? 40 : Math.Min(40, AdventureLevelSpec.AwardSeedId(progress.NextLevel));
                _modeFirstClear = definition.Survival ? record < (definition.Hard ? 10 : 5) : record == 0;
                _modeSession = new ModeSession(originalModeId, account.SeedAccess(), account.PacketSlots, unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks), account.PurchaseCount(23) > 0, account.PurchaseCount(22) > 0, basicSeeds);
                var coinProfile = _modeProfileId;
                var coinReceipt = _modeSession.RunId.ToString("N");
                _modeSession.Game.Rules.AdventurePacketSlots = account.PacketSlots;
                _modeSession.Game.Rules.AdventureDisableLoot = definition.Mechanic == ModeMechanic.IZombie;
                _modeSession.Game.Rules.AdventurePottedLoot = progress.CompletedRuns > 0 && account.Plants.Count < 32;
                _modeSession.Game.Rules.AdventureChocolateLoot = account.PurchaseCount(20) > 0 && account.PurchaseCount(26) < 10;
                _modeSession.Game.Rules.AdventureLootCommit = coin =>
                {
                    try
                    {
                        _owner.Accounts._accountStore.CollectBattleLoot(coinProfile, coinReceipt + ":" + coin.Id, coin.Kind == AdventureCoinKind.Chocolate, coin.GiftPlant ?? PlantKind.Peashooter, PvZGame.UtcSeconds);
                        return true;
                    }
                    catch (IOException e)
                    {
                        _modeSaveError = e.Message;
                        return false;
                    }
                    catch (InvalidOperationException e)
                    {
                        _modeSaveError = e.Message;
                        return false;
                    }
                };
                _modeSession.Game.Rules.AdventureCoinCommit = total =>
                {
                    try
                    {
                        _owner.Accounts._accountStore.CreditBattleCoins(coinProfile, coinReceipt, total);
                        _modeAccountSnapshot = null;
                        _modeSaveError = null;
                        return true;
                    }
                    catch (IOException error)
                    {
                        _modeSaveError = "金币尚未存入，请再次拾取：" + error.Message;
                        return false;
                    }
                    catch (UnauthorizedAccessException error)
                    {
                        _modeSaveError = "金币存档无法写入：" + error.Message;
                        return false;
                    }
                };
                _modeRewardClaimed = false;
                _modeNextSaveAttempt = 0;
                _modeSavedStage = -1;
                _modeSaveError = null;
                _modeChoosingImitater = false;
                _modeSession.Game.ApplyModePurchases(account.PurchaseCount(29) > 0, account.PurchaseCount(24) > 0 ? (Func<bool>)(() =>
                {
                    try
                    {
                        if (_owner.Accounts._accountStore.Get(coinProfile).PurchaseCount(24) <= 0)
                        {
                            return false;
                        }

                        _owner.Accounts._accountStore.ConsumeRake(coinProfile);
                        _modeAccountSnapshot = null;
                        return true;
                    }
                    catch (IOException error)
                    {
                        _modeSaveError = "草耙尚未扣除，本局未放置：" + error.Message;
                        return false;
                    }
                    catch (UnauthorizedAccessException error)
                    {
                        _modeSaveError = "草耙库存无法保存，本局未放置：" + error.Message;
                        return false;
                    }
                }) : null);
                _owner.Campaign.AttachAdventureGame(_modeSession.Game);
                _owner._deck.Clear();
                _owner._deck.AddRange(_owner._game.Adventure.SelectedSeeds);
                _owner.AdventureBoard.AdventureFeedback(_modeSession.Advice, 8f);
                _owner._choosingSeeds = _modeSession.Phase == ModePhase.ChoosingSeeds;
                _owner._pendingSeeds = new AdventureSeedChooserState(_owner._game.Adventure.SelectedSeeds);
                _owner._currentView = PvZGame.ScreenView.Board;
                return true;
            }
            catch (Exception error)
            {
                _owner.Menus.SetUnavailable("无法开始独立模式", error.Message);
                return false;
            }
        }

        /// <summary>Performs restart mode if active.</summary>
        /// <returns>The resulting restart mode if active.</returns>
        internal bool RestartModeIfActive()
        {
            if (!IsModeBoard)
            {
                return false;
            }

            if (_modeSession.Phase == ModePhase.Won && !_modeRewardClaimed && !SaveModeRecord(true))
            {
                return true;
            }

            StartIndependentMode(_modeSession.Definition.Id);
            return true;
        }

        /// <summary>Performs tick modes.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting tick modes.</returns>
        internal bool TickModes(float dt)
        {
            if (!IsModeBoard)
            {
                return false;
            }

            _modeSession.Update(dt);
            if ((_modeSession.Phase == ModePhase.StageCleared || _modeSession.Phase == ModePhase.Lost) && _modeSavedStage != _modeSession.ClearedStages && Time.unscaledTime >= _modeNextSaveAttempt)
            {
                SaveModeRecord(false);
            }

            return true;
        }

        /// <summary>Performs save mode record.</summary>
        /// <param name = "claimReward">The claim reward.</param>
        /// <returns>The resulting save mode record.</returns>
        private bool SaveModeRecord(bool claimReward)
        {
            if (_modeSession == null || _owner.Accounts._accountStore == null)
            {
                return false;
            }

            try
            {
                if (claimReward && !_modeSession.Game.BankAdventureCoins())
                {
                    return false;
                }

                var s = _modeSession;
                var score = s.Definition.Survival ? s.FlagsCompleted : s.Definition.Endless ? s.ClearedStages : s.Phase == ModePhase.Won ? 1 : 0;
                if (claimReward && s.Phase == ModePhase.Won && !s.Definition.Endless)
                {
                    _owner.Accounts._accountStore.RecordModeResult(_modeProfileId, s.Definition.RecordIndex, score, s.RunId.ToString("N"), _modeFirstClear ? 100 : 25);
                    _modeRewardClaimed = true;
                }
                else
                {
                    _owner.Accounts._accountStore.RecordModeResult(_modeProfileId, s.Definition.RecordIndex, score, s.RunId.ToString("N") + ":score:" + s.ClearedStages, 0);
                }

                _modeSavedStage = s.ClearedStages;
                _modeSaveError = null;
                _modeAccountSnapshot = null;
                return true;
            }
            catch (Exception error)
            {
                _modeSaveError = "成绩/奖励保存失败：" + error.Message;
                _modeNextSaveAttempt = Time.unscaledTime + 2f;
                return false;
            }
        }

        /// <summary>Performs return to mode directory.</summary>
        private void ReturnToModeDirectory()
        {
            if (_modeSession != null && (_modeSession.Finished || _modeSession.HasNextStage))
            {
                if (!SaveModeRecord(_modeSession.Phase == ModePhase.Won && !_modeRewardClaimed))
                {
                    return;
                }
            }

            _owner._currentView = PvZGame.ScreenView.Modes;
            var id = _modeSession == null ? 16 : _modeSession.Definition.Id;
            _owner._modeView.Open(id <= 15 ? 0 : id >= 51 ? 3 : 1);
            _owner._selected = -1;
            _owner.AdventureBoard._selectedAdventurePacket = -1;
            _owner._shovel = false;
            _owner._choosingSeeds = false;
        }

        /// <summary>Performs try handle mode click.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting try handle mode click.</returns>
        internal bool TryHandleModeClick(Vector2 point)
        {
            if (!IsModeBoard)
            {
                return false;
            }

            if (_owner.Opening.AdventureIntroActive)
            {
                return true;
            }

            var s = _modeSession;
            if (s_modeBackButton.Contains(point))
            {
                ReturnToModeDirectory();
                return true;
            }

            if (s.Phase == ModePhase.ChoosingSeeds)
            {
                HandleModeChooser(point);
                return true;
            }

            if (s.HasNextStage || s.Finished)
            {
                if (s_modeNextButton.Contains(point))
                {
                    if (s.HasNextStage)
                    {
                        if (SaveModeRecord(false) && s.Continue())
                        {
                            _owner._animations.ResetNativePlayback();
                            _owner._choosingSeeds = s.Phase == ModePhase.ChoosingSeeds;
                            _owner._pendingSeeds = new AdventureSeedChooserState(_owner._game.Adventure.SelectedSeeds);
                            _owner._deck.Clear();
                            _owner._deck.AddRange(_owner._game.Adventure.SelectedSeeds);
                            _owner._selected = -1;
                            _owner.AdventureBoard._selectedAdventurePacket = -1;
                            _owner._shovel = false;
                            _owner.AdventureBoard.AdventureFeedback(s.Advice, 6f);
                        }
                    }
                    else if (s.Phase == ModePhase.Won)
                    {
                        if (!_modeRewardClaimed)
                        {
                            SaveModeRecord(true);
                        }
                        else
                        {
                            ReturnToModeDirectory();
                        }
                    }
                    else
                    {
                        RestartModeIfActive();
                    }
                }
                else if (s_modeRestartButton.Contains(point))
                {
                    RestartModeIfActive();
                }
                else if (s_modeExitButton.Contains(point))
                {
                    ReturnToModeDirectory();
                }

                return true;
            }

            if (AdventureBoardController.s_adventureMenuButton.Contains(point))
            {
                _owner.Menus.OpenOptions();
                return true;
            }

            if (_owner._game.State == MatchState.Paused)
            {
                return true;
            }

            if (s.Phase == ModePhase.Preparing && new Rect(295, 535, 210, 34).Contains(point))
            {
                s.BeginDefense();
                return true;
            }

            for (var i = _owner._game.Adventure.Coins.Count - 1; i >= 0; i--)
            {
                if (_owner.Layout.AdventureCoinRect(_owner._game.Adventure.Coins[i]).Contains(point))
                {
                    _owner._game.CollectAdventureCoin(_owner._game.Adventure.Coins[i]);
                    return true;
                }
            }

            for (var i = _owner._game.Suns.Count - 1; i >= 0; i--)
            {
                if (ModeSunRect(_owner._game.Suns[i]).Contains(point))
                {
                    _owner._game.CollectSun(_owner._game.Suns[i]);
                    return true;
                }
            }

            var m = s.Definition.Mechanic;
            if (m == ModeMechanic.Aquarium)
            {
                if (new Rect(100, 10, 155, 55).Contains(point))
                {
                    s.BuyFish();
                }
                else if (new Rect(275, 10, 175, 55).Contains(point))
                {
                    s.BuyAquariumTrophy();
                }
                else
                {
                    s.Feed(point.x, point.y);
                }

                return true;
            }

            if (m == ModeMechanic.Slots && new Rect(500, 8, 95, 64).Contains(point))
            {
                s.Spin();
                return true;
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                for (var i = 0; i < 5; i++)
                {
                    if (new Rect(80 + i * 98, 8, 92, 60).Contains(point))
                    {
                        _owner._game.BuyModeMatchAction(i);
                        return true;
                    }
                }
            }

            if (m == ModeMechanic.IZombie)
            {
                for (var i = 0; i < s.ZombieCards.Count; i++)
                {
                    if (ModeZombieCardRect(i).Contains(point))
                    {
                        _owner._selected = _owner._selected == i ? -1 : i;
                        return true;
                    }
                }
            }
            else if (s.Definition.UsesPackets)
            {
                if (m == ModeMechanic.RainingSeeds)
                {
                    for (var i = s.RainPackets.Count - 1; i >= 0; i--)
                    {
                        if (ModeRainRect(s.RainPackets[i]).Contains(point))
                        {
                            _owner.AdventureBoard._selectedAdventurePacket = _owner.AdventureBoard._selectedAdventurePacket == s.RainPackets[i].PacketId ? -1 : s.RainPackets[i].PacketId;
                            _owner._shovel = false;
                            return true;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < _owner._game.Adventure.Packets.Count && i < 10; i++)
                    {
                        if (_owner.Layout.AdventurePacketContains(i, point))
                        {
                            _owner.AdventureBoard._selectedAdventurePacket = _owner.AdventureBoard._selectedAdventurePacket == _owner._game.Adventure.Packets[i].Id ? -1 : _owner._game.Adventure.Packets[i].Id;
                            _owner._selected = -1;
                            _owner._shovel = false;
                            _owner._game.CancelAdventureCobCannonAim();
                            return true;
                        }
                    }
                }

                if (m == ModeMechanic.Vases && new Rect(550, 8, 47, 64).Contains(point))
                {
                    _owner._selected = _owner._selected == 0 ? -1 : 0;
                    _owner.AdventureBoard._selectedAdventurePacket = -1;
                    return true;
                }
            }
            else if (m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist)
            {
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    if (ModeSeedRect(i).Contains(point))
                    {
                        var context = _owner._game.AdventurePacketContext(_owner._deck[i]);
                        if (AdventureSeedBankPresentation.CanPickUp(context))
                        {
                            _owner._selected = _owner._selected == i ? -1 : i;
                            _owner.Sounds.PlayPresentationSound("seedlift");
                        }
                        else
                        {
                            _owner.AdventureBoard.RejectSeedClick(context.Active && !context.CanAfford);
                        }
                        _owner.AdventureBoard._selectedAdventurePacket = -1;
                        _owner._shovel = false;
                        _owner._game.CancelAdventureCobCannonAim();
                        return true;
                    }
                }
            }

            if (m != ModeMechanic.IZombie && m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist && AdventureBoardController.s_adventureShovelButton.Contains(point))
            {
                _owner._shovel = !_owner._shovel;
                _owner.Sounds.PlayPresentationSound("shovel");
                _owner._selected = -1;
                _owner.AdventureBoard._selectedAdventurePacket = -1;
                _owner._game.CancelAdventureCobCannonAim();
                return true;
            }

            var col = Mathf.FloorToInt((point.x - BoardLayout.AdventureGridX) / BoardLayout.CellWidth);
            var row = Mathf.FloorToInt((point.y - BoardLayout.AdventureGridY - _owner.Layout.AdventureSlope(col)) / _owner.Layout.AdventurePitch);
            if (col < 0 || col > 8 || row < 0 || row >= _owner._game.RowCount)
            {
                return true;
            }

            if (m == ModeMechanic.IZombie)
            {
                if (_owner._selected >= 0 && !s.PlaceZombie(s.ZombieCards[_owner._selected], row, col))
                {
                    var bungee = s.ZombieCards[_owner._selected] == ZombieKind.Bungee;
                    if (bungee ? col >= _owner._game.ModeIZombieColumns : col < _owner._game.ModeIZombieColumns)
                    {
                        _owner.AdventureBoard.AdventureFeedback(bungee ? "[ADVICE_I_ZOMBIE_LEFT_OF_LINE]" : "[ADVICE_I_ZOMBIE_NOT_PASSED_LINE]", 3f);
                    }
                }

                return true;
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                s.ClickMatchCell(row, col);
                return true;
            }

            if (_owner._game.Adventure.SelectedCobCannonId.HasValue)
            {
                _owner._game.TryFireAdventureCobCannonAtCell(row, col);
                return true;
            }

            if (_owner._shovel)
            {
                if (_owner._game.TryShovelAdventurePlant(_owner.Layout.AdventureShovelTarget(point)))
                {
                    _owner._shovel = false;
                    _owner.Sounds.PlayPresentationSound("plant");
                }

                return true;
            }

            if (_owner.AdventureBoard._selectedAdventurePacket >= 0)
            {
                if (s.UsePacket(_owner.AdventureBoard._selectedAdventurePacket, row, col))
                {
                    _owner.AdventureBoard._selectedAdventurePacket = -1;
                }
                else
                {
                    _owner.AdventureBoard.AdventurePlantingFeedback();
                }

                return true;
            }

            if (m == ModeMechanic.Vases && _owner._selected == 0)
            {
                if (_owner._game.PlantModeVaseCherry(row, col))
                {
                    _owner._selected = -1;
                }
                else
                {
                    _owner.AdventureBoard.AdventurePlantingFeedback();
                }

                return true;
            }

            if (_owner._selected >= 0 && _owner._selected < _owner._deck.Count)
            {
                if (s.Plant(_owner._deck[_owner._selected], row, col))
                {
                    _owner._selected = -1;
                }
                else
                {
                    _owner.AdventureBoard.AdventurePlantingFeedback();
                }

                return true;
            }

            if (_owner._game.TryAimAdventureCobCannon(row, col))
            {
                return true;
            }

            if (m == ModeMechanic.Vases)
            {
                s.SmashVase(row, col);
                return true;
            }

            if (m == ModeMechanic.Whack)
            {
                _owner.AdventureBoard.TryHammerAdventureZombie(point);
            }

            return true;
        }

        /// <summary>Performs handle mode chooser.</summary>
        /// <param name = "point">The point.</param>
        private void HandleModeChooser(Vector2 point)
        {
            var unlocked = _owner._game.Adventure.AvailableSeeds;
            var required = Math.Min(_modeSession.PacketSlots, unlocked.Count);
            _owner._pendingSeeds.LandFlyingSeeds();
            if (_modeChoosingImitater)
            {
                for (var i = 0; i < 40; i++)
                {
                    if (BoardLayout.AdventureChooserRect(i).Contains(point) && _owner._game.SetModeImitaterTarget((PlantKind)i))
                    {
                        var home = BoardLayout.AdventureChooserRect(_owner.Router.AdventureAvailableSeedIndex(PlantKind.Imitater));
                        _owner._pendingSeeds.Choose(PlantKind.Imitater, (int)home.x, (int)home.y, _owner.Layout.SeedBankCapacity);
                        _owner.Sounds.PlayAdventureRewardSound("tap2.ogg");
                        _modeChoosingImitater = false;
                        return;
                    }
                }

                if (new Rect(300, 533, 200, 34).Contains(point))
                {
                    _modeChoosingImitater = false;
                }

                return;
            }

            if (_owner.Router.ReturnSelectedChooserSeed(point, _owner.Layout.SeedBankCapacity, true))
            {
                return;
            }

            for (var i = 0; i < unlocked.Count; i++)
            {
                if (BoardLayout.AdventureChooserRect(i).Contains(point))
                {
                    if (!_owner._pendingSeeds.Contains(unlocked[i]) && _owner._pendingSeeds.Count < required)
                    {
                        if (unlocked[i] == PlantKind.Imitater)
                        {
                            _modeChoosingImitater = true;
                        }
                        else
                        {
                            var home = BoardLayout.AdventureChooserRect(i);
                            _owner._pendingSeeds.Choose(unlocked[i], (int)home.x, (int)home.y, _owner.Layout.SeedBankCapacity);
                            _owner.Sounds.PlayAdventureRewardSound("tap2.ogg");
                        }
                    }

                    return;
                }
            }

            if (new Rect(300, 533, 200, 34).Contains(point) && _owner._pendingSeeds.Count == required)
            {
                var seeds = new List<PlantKind>(_owner._pendingSeeds);

                if (_modeSession.ChooseSeeds(seeds))
                {
                    _owner._choosingSeeds = false;
                    _owner._deck.Clear();
                    _owner._deck.AddRange(seeds);
                }
            }
        }

        /// <summary>Performs mode zombie name.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting mode zombie name.</returns>
        private static string ModeZombieName(ZombieKind kind)
        {
            string[] names =
            {
                "普通僵尸",
                "旗帜僵尸",
                "路障僵尸",
                "撑杆僵尸",
                "铁桶僵尸",
                "读报僵尸",
                "铁门僵尸",
                "橄榄球僵尸",
                "舞王僵尸",
                "伴舞僵尸",
                "泳圈僵尸",
                "潜水僵尸",
                "冰车僵尸",
                "雪橇僵尸",
                "海豚僵尸",
                "小丑僵尸",
                "气球僵尸",
                "矿工僵尸",
                "蹦蹦僵尸",
                "雪人僵尸",
                "蹦极僵尸",
                "扶梯僵尸",
                "投石车僵尸",
                "巨人僵尸",
                "小鬼僵尸"
            };
            return (int)kind >= 0 && (int)kind < names.Length ? names[(int)kind] : kind.ToString();
        }

        /// <summary>Performs mode seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting mode seed rect.</returns>
        internal Rect ModeSeedRect(int index)
        {
            var origin = _owner.Layout.AdventureSeedBankOrigin;
            return new Rect(origin.x + AdventureSeedSlotLayout.PositionX(index, _owner.Layout.SeedBankCapacity), origin.y + 8f, 50f, 70f);
        }

        /// <summary>Performs mode zombie card rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting mode zombie card rect.</returns>
        internal static Rect ModeZombieCardRect(int index)
        {
            return new Rect(80 + index * 57, 8, 54, 65);
        }

        /// <summary>Performs mode rain rect.</summary>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting mode rain rect.</returns>
        internal static Rect ModeRainRect(ModeRainPacket packet)
        {
            return new Rect(packet.X - 25, packet.Y - 35, 50, 70);
        }

        /// <summary>Performs mode sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting mode sun rect.</returns>
        private Rect ModeSunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            return _owner.Layout.AdventureSunRect(sun);
        }
    }
}
