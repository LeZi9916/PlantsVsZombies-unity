#nullable enable
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Handles pointer input, seed selection, planting feedback and tutorial text for the adventure board.</summary>
    internal sealed class AdventureBoardController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose board, selection and campaign the controller drives.</param>
        internal AdventureBoardController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Advances the shovel hammer swing by unscaled time until it completes.</summary>
        /// <param name="delta">The unscaled seconds elapsed since the previous frame.</param>
        internal void AdvanceHammerAnimation(float delta)
        {
            if (_hammerAnimationAge < 1f)
            {
                _hammerAnimationAge = Mathf.Min(1f, _hammerAnimationAge + delta);
            }
        }

        /// <summary>Retires a selected conveyor packet after the simulation removes it.</summary>
        internal void NormalizeAdventureSelection()
        {
            if (!_owner.RequiredGame.IsAdventure)
            {
                return;
            }

            var board = _owner.RequiredGame.Adventure;
            if (_selectedAdventurePacket >= 0)
            {
                var exists = false;
                for (var packetIndex = 0; packetIndex < board.Packets.Count; packetIndex++)
                {
                    var packet = board.Packets[packetIndex];
                    if (packet.Id == _selectedAdventurePacket)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _selectedAdventurePacket = -1;
                }
            }
        }
        /// <summary>Stores selected adventure packet.</summary>
        internal int _selectedAdventurePacket = -1;
        /// <summary>Stores adventure feedback.</summary>
        internal string? _adventureFeedback;
        /// <summary>Stores adventure feedback until.</summary>
        internal float _adventureFeedbackUntil;
        /// <summary>Stores choosing imitater.</summary>
        internal bool _choosingImitater;
        /// <summary>Stores adventure pointer.</summary>
        internal Vector2 _adventurePointer;
        /// <summary>Gets the original grid-selected vase beneath the normal cursor.</summary>
        internal AdventureVase? HighlightedVase => VaseAtPoint(_adventurePointer);

        /// <summary>Resolves the original PC vase hit test after higher-priority pickups and cursor tools.</summary>
        /// <param name="point">The pointer in the original 800 by 600 board coordinates.</param>
        /// <returns>The eligible vase, or null while the board or normal cursor cannot interact.</returns>
        internal AdventureVase? VaseAtPoint(Vector2 point)
        {
            var game = _owner._game;
            if (game == null || !game.IsAdventure || game.Adventure.Level.Mode != AdventureMode.Vasebreaker
                || game.State != MatchState.Playing || game.Adventure.Phase != AdventurePhase.Playing
                || game.Adventure.VaseInterludeActive || game.Presentation.VaseMalletTicks > 0
                || _owner._shovel || _owner._selected >= 0 || _selectedAdventurePacket >= 0
                || _owner._choosingSeeds || _owner.Opening.AdventureIntroActive
                || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null
                || !new Rect(0f, 0f, 800f, 600f).Contains(point)
                || point.x < BoardLayout.AdventureGridX || point.y < BoardLayout.AdventureGridY)
            {
                return null;
            }

            for (var index = 0; index < game.Adventure.Coins.Count; index++)
            {
                if (_owner.Layout.AdventureCoinRect(game.Adventure.Coins[index]).Contains(point))
                {
                    return null;
                }
            }

            for (var index = 0; index < game.Suns.Count; index++)
            {
                if (_owner.Layout.AdventureSunRect(game.Suns[index]).Contains(point))
                {
                    return null;
                }
            }

            // PC PixelToGridX/Y clamp the final column and row to the board edge.
            var column = Mathf.Min(8, (int)((point.x - BoardLayout.AdventureGridX) / BoardLayout.CellWidth));
            var row = Mathf.Min(4, (int)((point.y - BoardLayout.AdventureGridY) / 100f));
            for (var index = 0; index < game.Adventure.Vases.Count; index++)
            {
                var vase = game.Adventure.Vases[index];
                if (vase.Row == row && vase.Column == column)
                {
                    return vase;
                }
            }

            return null;
        }

        /// <summary>Stores the current native Whack-a-Zombie hammer animation age.</summary>
        internal float _hammerAnimationAge = 1f;
        /// <summary>The board that received the most recent insufficient-sun click.</summary>
        private GameSimulation? _sunWarningGame;
        /// <summary>The native update at which the sun display began its seventy-tick warning.</summary>
        private int _sunWarningStartTick;
        /// <summary>Gets the native update used by input feedback, which stops while the board is paused.</summary>
        private int SeedFeedbackTick => _owner.RequiredGame.IsAdventure ? _owner.RequiredGame.Adventure._ticks : Mathf.FloorToInt(_owner.RequiredGame.Elapsed * 100f);
        /// <summary>Gets whether the native insufficient-sun counter currently requests red text.</summary>
        internal bool SunMoneyWarningRed
        {
            get
            {
                var remaining = ReferenceEquals(_sunWarningGame, _owner._game) ? Math.Max(0, 70 - (SeedFeedbackTick - _sunWarningStartTick)) : 0;
                return remaining > 0 && remaining % 20 < 10;
            }
        }

        /// <summary>Reports an unavailable seed using the original sound and optional sun-display warning.</summary>
        /// <param name="missingSun">Whether the ready seed failed the native affordability check.</param>
        internal void RejectSeedClick(bool missingSun)
        {
            _owner.Sounds.PlaySeedBankBuzzer();
            if (missingSun)
            {
                _sunWarningGame = _owner._game;
                _sunWarningStartTick = SeedFeedbackTick;
            }
        }
        /// <summary>Stores adventure action button.</summary>
        private static readonly Rect s_adventureActionButton = new Rect(316, 356, 168, 48);
        /// <summary>Stores adventure menu button.</summary>
        internal static readonly Rect s_adventureMenuButton = new Rect(704, 6, 90, 34);

        /// <summary>Performs adventure feedback.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "duration">The duration.</param>
        internal void AdventureFeedback(string text, float duration = 2.5f)
        {
            _adventureFeedback = _owner._presenter.ResolveAdventureAdvice(text);
            _adventureFeedbackUntil = Time.unscaledTime + duration;
        }

        /// <summary>Preserves the original board message lifetime while gameplay is paused by a modal.</summary>
        /// <param name="delta">The unscaled frame duration consumed by the host clock.</param>
        internal void PreserveAdventureFeedbackDuringPause(float delta)
        {
            if (_owner._currentView != PvZGame.ScreenView.Board || _owner._game == null || !_owner._game.IsAdventure || _adventureFeedback == null
                || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            if (_owner._game.State == MatchState.Paused || _owner._optionsView != null && _owner._optionsView.IsOpen
                || _owner._confirmingQuit || _owner._unavailableFeature != null || _owner._profileDialog != null && _owner._profileDialog.IsOpen)
            {
                _adventureFeedbackUntil += delta;
            }
        }

        /// <summary>Performs adventure plant name.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure plant name.</returns>
        internal string AdventurePlantName(PlantKind kind)
        {
            return kind == PlantKind.ExplodeONut ? "爆炸坚果" : _owner.RequiredAlmanacView.PlantDisplayName((int)kind);
        }

        /// <summary>Performs contains required seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting contains required seed.</returns>
        internal bool ContainsRequiredSeed(PlantKind kind)
        {
            for (var seedIndex = 0; seedIndex < _owner.RequiredGame.Adventure.RequiredSeeds.Count; seedIndex++)
            {
                var seed = _owner.RequiredGame.Adventure.RequiredSeeds[seedIndex];
                if (seed == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs adventure tutorial text.</summary>
        /// <param name = "tutorial">The tutorial.</param>
        /// <returns>The resulting adventure tutorial text.</returns>
        internal string? AdventureTutorialText(AdventureTutorial tutorial)
        {
            var shovelPlants = 0;
            foreach (var plant in _owner.RequiredGame.Plants)
            {
                if (plant.Kind == PlantKind.Peashooter && plant.Row == 2 && plant.Column >= 3 && plant.Column <= 5)
                {
                    shovelPlants++;
                }
            }

            var key = AdventureAdviceRules.TutorialKey(tutorial, _owner._selected >= 0, _owner._shovel, shovelPlants);
            return key == null ? null : _owner._presenter.OriginalProgressionText(key);
        }

        /// <summary>Performs handle adventure click.</summary>
        /// <param name = "point">The point.</param>
        internal void HandleAdventureClick(Vector2 point)
        {
            var board = _owner.RequiredGame.Adventure;
            if (_owner.Opening.AdventureIntroActive && !_owner.Opening.AdventureIntroAwaitingSeeds)
            {
                if (_owner.Opening._bowlingShovelLessonActive)
                {
                    _owner.Opening.HandleBowlingShovelLesson(point);
                }

                return;
            }

            if (s_adventureMenuButton.Contains(point))
            {
                _owner.Menus.OpenOptions();
                return;
            }

            if (_owner._choosingSeeds)
            {
                var unlocked = board.AvailableSeeds;
                var required = Math.Min(board.Level.PacketSlots, unlocked.Count);
                _owner.RequiredPendingSeeds.LandFlyingSeeds();
                if (_choosingImitater)
                {
                    var targets = board.Level.UnlockedSeeds;
                    for (var i = 0; i < targets.Count; i++)
                    {
                        if (_owner.Opening.AdventureCampaignChooserRect(targets[i]).Contains(point) && _owner.RequiredGame.SetAdventureImitaterTarget(targets[i]))
                        {
                            var home = _owner.Opening.AdventureCampaignChooserRect(PlantKind.Imitater);
                            _owner.RequiredPendingSeeds.Choose(PlantKind.Imitater, (int)home.x, (int)home.y, _owner.Layout.SeedBankCapacity);
                            _owner.Sounds.PlayAdventureRewardSound("tap2.ogg");
                            _choosingImitater = false;
                            return;
                        }
                    }

                    if (_owner.Opening.AdventureChooserStartRect.Contains(point))
                    {
                        _choosingImitater = false;
                    }

                    return;
                }

                if (_owner.Router.ReturnSelectedChooserSeed(point, _owner.Layout.SeedBankCapacity))
                {
                    return;
                }

                for (var i = 0; i < unlocked.Count; i++)
                {
                    if (_owner.Opening.AdventureCampaignChooserRect(unlocked[i]).Contains(point))
                    {
                        if (ContainsRequiredSeed(unlocked[i]))
                        {
                            return;
                        }

                        if (!_owner.RequiredPendingSeeds.Contains(unlocked[i]) && _owner.RequiredPendingSeeds.Count < required)
                        {
                            if (unlocked[i] == PlantKind.Imitater)
                            {
                                _choosingImitater = true;
                            }
                            else
                            {
                                var home = _owner.Opening.AdventureCampaignChooserRect(unlocked[i]);
                                _owner.RequiredPendingSeeds.Choose(unlocked[i], (int)home.x, (int)home.y, _owner.Layout.SeedBankCapacity);
                                _owner.Sounds.PlayAdventureRewardSound("tap2.ogg");
                            }
                        }

                        return;
                    }
                }

                if (_owner.Opening.AdventureChooserStartRect.Contains(point) && _owner.RequiredPendingSeeds.Count == required)
                {
                    var seeds = new List<PlantKind>(_owner._pendingSeeds);

                    try
                    {
                        var chosen = _owner.Campaign._adventureTestBoard ? _owner.RequiredGame.ChooseAdventureSeeds(seeds) : _owner.Campaign.RequiredAdventureCampaign.ChooseSeeds(seeds);
                        if (chosen)
                        {
                            _owner._choosingSeeds = false;
                            _owner._deck.Clear();
                            _owner._deck.AddRange(seeds);
                            _owner.Opening.BeginAdventureOpeningReturn();
                        }
                    }
                    catch (Exception error)
                    {
                        AdventureFeedback("无法保存选卡：" + error.Message, 5f);
                    }
                }

                return;
            }

            if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished)
            {
                _owner.Rewards.HandleAdventureRewardClick(point);
                return;
            }

            if (_owner.RequiredGame.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    _owner.Campaign.RestartAdventure();
                }

                return;
            }

            if (_owner.RequiredGame.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point))
                {
                    _owner.RequiredGame.TogglePause();
                }
                else if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    _owner.Campaign.RestartAdventure();
                }

                return;
            }

            for (var i = board.Coins.Count - 1; i >= 0; i--)
            {
                if (_owner.Layout.AdventureCoinRect(board.Coins[i]).Contains(point))
                {
                    if (!_owner.RequiredGame.CollectAdventureCoin(board.Coins[i]) && _owner.Campaign._adventureCampaign != null && _owner.Campaign._adventureCampaign.LastPersistenceError != null)
                    {
                        AdventureFeedback("金币尚未保存：" + _owner.Campaign._adventureCampaign.LastPersistenceError, 5f);
                    }

                    return;
                }
            }

            for (var i = _owner.RequiredGame.Suns.Count - 1; i >= 0; i--)
            {
                if (_owner.Layout.AdventureSunRect(_owner.RequiredGame.Suns[i]).Contains(point))
                {
                    _owner.RequiredGame.CollectSun(_owner.RequiredGame.Suns[i]);
                    return;
                }
            }

            var packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (packets)
            {
                for (var i = 0; i < board.Packets.Count && i < 10; i++)
                {
                    if (_owner.Layout.AdventurePacketContains(i, point))
                    {
                        _selectedAdventurePacket = _selectedAdventurePacket == board.Packets[i].Id ? -1 : board.Packets[i].Id;
                        _owner._selected = -1;
                        _owner._shovel = false;
                        _owner.RequiredGame.CancelAdventureCobCannonAim();
                        return;
                    }
                }
            }
            else
            {
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    if (_owner.Layout.AdventureSeedRect(i).Contains(point))
                    {
                        var context = _owner.RequiredGame.AdventurePacketContext(_owner._deck[i]);
                        if (AdventureSeedBankPresentation.CanPickUp(context) && (board.Level.Mode == AdventureMode.WhackAZombie || _owner.RequiredGame.SelectAdventureSeed(_owner._deck[i])))
                        {
                            _owner._selected = _owner._selected == i ? -1 : i;
                            _owner.Sounds.PlayPresentationSound("seedlift");
                        }
                        else
                        {
                            RejectSeedClick(context.Active && !context.CanAfford);
                        }

                        _owner._shovel = false;
                        _selectedAdventurePacket = -1;
                        _owner.RequiredGame.CancelAdventureCobCannonAim();
                        return;
                    }
                }
            }

            if (board.Level.Number >= 5 && _owner.Layout.AdventureShovelButton.Contains(point))
            {
                _owner._shovel = !_owner._shovel;
                _owner.Sounds.PlayPresentationSound("shovel");
                _owner._selected = -1;
                _selectedAdventurePacket = -1;
                _owner.RequiredGame.CancelAdventureCobCannonAim();
                return;
            }

            if (board.Level.Mode == AdventureMode.Vasebreaker && !_owner._shovel
                && _selectedAdventurePacket < 0 && _owner._selected < 0)
            {
                var vase = VaseAtPoint(point);
                if (vase != null)
                {
                    _owner.RequiredGame.SmashAdventureVase(vase.Row, vase.Column);
                }

                return;
            }

            var col = Mathf.FloorToInt((point.x - BoardLayout.AdventureGridX) / BoardLayout.CellWidth);
            var row = Mathf.FloorToInt((point.y - BoardLayout.AdventureGridY - _owner.Layout.AdventureSlope(col)) / _owner.Layout.AdventurePitch);
            if (col < 0 || col > 8 || row < 0 || row >= _owner.RequiredGame.RowCount)
            {
                return;
            }

            if (board.SelectedCobCannonId.HasValue)
            {
                _owner.RequiredGame.TryFireAdventureCobCannonAtCell(row, col);
                return;
            }

            if (_owner._shovel)
            {
                if (_owner.RequiredGame.TryShovelAdventurePlant(_owner.Layout.AdventureShovelTarget(point)))
                {
                    _owner._shovel = false;
                    _owner.Sounds.PlayPresentationSound("plant");
                }

                return;
            }

            if (_selectedAdventurePacket >= 0)
            {
                if (_owner.RequiredGame.TryUseAdventurePacket(_selectedAdventurePacket, row, col))
                {
                    _selectedAdventurePacket = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return;
            }

            if (_owner._selected >= 0)
            {
                if (_owner.RequiredGame.TryPlant(_owner._deck[_owner._selected], row, col))
                {
                    _owner._selected = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return;
            }

            if (_owner.RequiredGame.TryAimAdventureCobCannon(row, col))
            {
                return;
            }

            if (board.Level.Mode == AdventureMode.WhackAZombie)
            {
                TryHammerAdventureZombie(point);
            }
        }

        /// <summary>Performs the original Whack-a-Zombie circular hammer hit test.</summary>
        /// <param name="point">The board-space click point.</param>
        internal void TryHammerAdventureZombie(Vector2 point)
        {
            _hammerAnimationAge = 0.2f;
            _owner.Sounds.PlayPresentationSound("swing");
            var hitPoint = new Vector2(point.x, point.y - 20f);
            Zombie? target = null;
            var targetRow = -1;
            for (var i = 0; i < _owner.RequiredGame.Zombies.Count; i++)
            {
                var zombie = _owner.RequiredGame.Zombies[i];
                var bodyY = 50 + zombie.Row * 100;
                if (zombie.GraveRiseTicks > 0)
                {
                    bodyY += (int)-AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks);
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, bodyY);
                var height = body.Height - 50;
                if (zombie.GraveRiseTicks > 0)
                {
                    height -= (int)(-AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks) + Math.Min(40, zombie.GraveRiseTicks));
                }

                // PC 0x428460 retains negative rise heights and checks the near edge first.
                var dx = hitPoint.x < body.X ? body.X - hitPoint.x
                    : hitPoint.x > body.X + body.Width ? hitPoint.x - body.X - body.Width : 0f;
                var dy = hitPoint.y < body.Y ? body.Y - hitPoint.y
                    : hitPoint.y > body.Y + height ? hitPoint.y - body.Y - height : 0f;
                if (dx * dx + dy * dy > 45f * 45f)
                {
                    continue;
                }

                if (target == null || zombie.Row >= targetRow)
                {
                    target = zombie;
                    targetRow = zombie.Row;
                }
            }

            if (target != null)
            {
                var hadArmor = target.ArmorHealth > 0f;
                if (_owner.RequiredGame.HammerAdventureZombie(target.Id, point.x, point.y) && !hadArmor)
                {
                    _owner.Sounds.PlayPresentationSound("bonk");
                }
            }
        }

        /// <summary>Performs adventure planting feedback.</summary>
        internal void AdventurePlantingFeedback()
        {
            if (_owner.RequiredGame.LastAdventurePlantingFailure == AdventurePlantingFailure.Tutorial)
            {
                return;
            }

            var kind = _owner._selected >= 0 && _owner._selected < _owner._deck.Count ? _owner._deck[_owner._selected] : PlantKind.Peashooter;
            for (var packetIndex = 0; packetIndex < _owner.RequiredGame.Adventure.Packets.Count; packetIndex++)
            {
                var packet = _owner.RequiredGame.Adventure.Packets[packetIndex];
                if (packet.Id == _selectedAdventurePacket)
                {
                    kind = packet.Kind;
                    break;
                }
            }
            if (kind == PlantKind.Imitater && _owner.RequiredGame.Adventure.ImitaterTarget.HasValue)
            {
                kind = _owner.RequiredGame.Adventure.RequiredImitaterTarget;
            }

            var key = AdventureAdviceRules.PlantingKey(_owner.RequiredGame.LastAdventurePlantingFailure, kind, !_owner.RequiredGame.Adventure.Level.IsReplay && _owner.RequiredGame.Adventure.Level.Number == 41);
            if (key != null)
            {
                AdventureFeedback(_owner._presenter.OriginalProgressionText(key), 5f);
            }
        }
    }
}
