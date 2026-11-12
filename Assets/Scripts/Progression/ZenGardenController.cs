#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;
using System;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Progression
{
    /// <summary>Runs the Zen Garden screen: plant care, tools, the first-visit tutorial and selling plants to Crazy Dave.</summary>
    internal sealed class ZenGardenController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose profile, account and Dave the garden uses.</param>
        internal ZenGardenController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores garden kind.</summary>
        internal GardenKind _gardenKind;
        /// <summary>Stores garden tool.</summary>
        internal GardenTool _gardenTool;
        /// <summary>Stores moving garden plant.</summary>
        private Guid? _movingGardenPlant;
        /// <summary>Stores garden update utc.</summary>
        private long _gardenUpdateUtc;
        /// <summary>Whether Dave is presenting the first Zen Garden introduction.</summary>
        internal bool _gardenDaveIntroActive;
        /// <summary>Whether the Zen tutorial returns to the next adventure level.</summary>
        internal bool _gardenReturnToAdventure;
        /// <summary>Tracks the original Zen tutorial's tool and store steps.</summary>
        internal int _gardenTutorialStage;

        /// <summary>Enters the original Zen Garden tutorial from the watering-can award.</summary>
        internal void OpenZenGardenForAdventureIntro()
        {
            _gardenReturnToAdventure = true;
            OpenZenGarden();
        }

        /// <summary>Performs open zen garden.</summary>
        internal void OpenZenGarden()
        {
            if (!_owner.Accounts.EnsurePlayerAccount() || !_owner.Campaign.EnsureAdventureStore())
            {
                return;
            }

            var progress = _owner.Campaign._adventureStore.Get(_owner._profiles.SelectedId.Value);
            if (progress.CompletedRuns == 0 && progress.NextLevel < 45)
            {
                _owner.Menus.SetUnavailable("禅境花园", "通过冒险模式 5-4，取得浇水壶后即可进入。");
                return;
            }

            _owner._currentView = PvZGame.ScreenView.Garden;
            _gardenSalePlant = null;
            _gardenSaleDaveActive = false;
            _gardenKind = GardenKind.Greenhouse;
            _gardenTool = GardenTool.Water;
            _movingGardenPlant = null;
            _owner.Accounts._progressionMessage = null;
            _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.UpdateGarden(_owner._profiles.SelectedId.Value, PvZGame.UtcSeconds, true));
            var account = _owner.Accounts.GetSelectedAccount();
            if (!account.GardenGiftClaimed && account.ZenGardenTutorialStage == 0)
            {
                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.BeginZenGardenTutorial(_owner._profiles.SelectedId.Value));
                account = _owner.Accounts.GetSelectedAccount();
            }

            var savedStage = account.ZenGardenTutorialStage;
            _gardenReturnToAdventure = savedStage > 0;
            _gardenTutorialStage = savedStage == 8 ? 5 : Math.Max(0, savedStage - 1);
            _gardenDaveIntroActive = savedStage == 1;
            if (_gardenDaveIntroActive)
            {
                _owner.Dave._crazyDaveMessageIndex = -1;
                _owner.Dave._crazyDaveAnimation = null;
                _owner.Dave._crazyDaveVoice.Stop();
                _owner.Dave.ShowCrazyDaveMessage(2100);
            }
            else if (savedStage == 8)
            {
                _owner.Store.OpenStoreForGardenIntro(2600);
            }
            else if (savedStage > 1)
            {
                var key = savedStage == 2 ? "ADVICE_ZEN_GARDEN_PICK_UP_WATER" : savedStage == 3 ? "ADVICE_ZEN_GARDEN_WATER_PLANT" : savedStage == 4 ? "ADVICE_ZEN_GARDEN_KEEP_WATERING" : savedStage == 5 ? "ADVICE_ZEN_GARDEN_VISIT_STORE" : savedStage == 6 ? "ADVICE_ZEN_GARDEN_FERTILIZE" : "ADVICE_ZEN_GARDEN_CONTINUE_ADVENTURE";
                _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText(key);
            }
        }

        /// <summary>Advances Dave's original first garden conversation and its seedlings.</summary>
        internal void AdvanceZenGardenIntroduction()
        {
            var next = CrazyDaveDialogue.NextMessageIndex(_owner.Dave._crazyDaveMessageIndex, index =>
            {
                var key = "CRAZY_DAVE_" + index;
                var text = _owner._presenter.OriginalProgressionText(key);
                return !string.IsNullOrEmpty(text) && text != key;
            });
            if (next >= 0)
            {
                if (next == 2102)
                {
                    _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.GrantZenGardenIntroPlants(_owner._profiles.SelectedId.Value, PvZGame.UtcSeconds));
                    if (_owner.Accounts._progressionMessage != null)
                    {
                        return;
                    }
                }

                _owner.Dave.ShowCrazyDaveMessage(next);
                return;
            }

            _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.CompleteZenGardenIntroduction(_owner._profiles.SelectedId.Value));
            if (_owner.Accounts._progressionMessage != null)
            {
                return;
            }

            _gardenTutorialStage = 1;
            _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_PICK_UP_WATER");
            _owner.Dave.LeaveCrazyDaveDialogue();
        }

        /// <summary>Advances the garden's Dave animations, plant growth and the first-visit tutorial.</summary>
        internal void UpdateZenGarden()
        {
            if (_owner._currentView == PvZGame.ScreenView.Garden && _gardenDaveIntroActive && !_owner._optionsView.IsOpen)
            {
                _owner.Dave.UpdateCrazyDaveAnimation(Time.unscaledDeltaTime);
                if (_owner.Dave._crazyDaveMessageIndex < 0 && (_owner.Dave._crazyDaveAnimation == null || _owner.Dave._crazyDaveAnimation == "anim_leave" && _owner.Dave._crazyDaveAnimationAge >= .75f))
                {
                    _gardenDaveIntroActive = false;
                    _owner.Dave._crazyDaveAnimation = null;
                }
            }
            if (_owner._currentView == PvZGame.ScreenView.Garden && _gardenSaleDaveActive && !_owner._optionsView.IsOpen)
            {
                _owner.Dave.UpdateCrazyDaveAnimation(Time.unscaledDeltaTime);
                if (!_gardenSalePlant.HasValue && (_owner.Dave._crazyDaveAnimation == null || _owner.Dave._crazyDaveAnimation == "anim_leave" && _owner.Dave._crazyDaveAnimationAge >= .75f))
                {
                    _gardenSaleDaveActive = false;
                    _owner.Dave._crazyDaveAnimation = null;
                }
            }
            if (_owner._currentView == PvZGame.ScreenView.Garden && !_gardenSalePlant.HasValue && _owner.Accounts._accountStore != null && PvZGame.UtcSeconds != _gardenUpdateUtc)
            {
                _gardenUpdateUtc = PvZGame.UtcSeconds;
                var previousMessage = _owner.Accounts._progressionMessage;
                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.UpdateGarden(_owner._profiles.SelectedId.Value, _gardenUpdateUtc));
                if (_owner.Accounts._progressionMessage == null)
                {
                    _owner.Accounts._progressionMessage = previousMessage;
                }

                if (_gardenTutorialStage == 3)
                {
                    foreach (var plant in _owner.Accounts.GetSelectedAccount().Plants)
                    {
                        if (plant.Need(_gardenUpdateUtc) == GardenNeed.Fertilizer)
                        {
                            _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.SetZenGardenTutorialStage(_owner._profiles.SelectedId.Value, 5));
                            if (_owner.Accounts._progressionMessage == null)
                            {
                                _gardenTutorialStage = 4;
                                _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_VISIT_STORE");
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Performs garden plant rect.</summary>
        /// <param name = "g">The g.</param>
        /// <returns>The resulting garden plant rect.</returns>
        internal Rect GardenPlantRect(GardenPlant g)
        {
            var grid = OriginalGardenLayout.Grids[(int)g.Garden];
            var slot = Mathf.Clamp(g.Slot, 0, grid.Length - 1);
            return new Rect(grid[slot][0], grid[slot][1], 80, 85);
        }

        /// <summary>Handles a pointer press in the Zen Garden.</summary>
        /// <param name="point">The pointer in the original 800 by 600 coordinate space.</param>
        /// <returns>Always true, because the garden consumes every press.</returns>
        internal bool HandleGardenClick(Vector2 point)
        {
            if (_gardenReturnToAdventure && _gardenTutorialStage >= 1 && _gardenTutorialStage <= 5)
            {
                var onWater = new Rect(8, 4, 73, 57).Contains(point);
                var onFertilizer = new Rect(86, 4, 73, 57).Contains(point);
                var onStore = new Rect(654, 540, 136, 42).Contains(point);
                var onPlant = false;
                foreach (var plant in _owner.Accounts.GetSelectedAccount().Plants)
                {
                    if (plant.Garden == _gardenKind && GardenPlantRect(plant).Contains(point))
                    {
                        onPlant = true;
                        break;
                    }
                }

                if (_gardenTutorialStage == 1 && !onWater
                    || (_gardenTutorialStage == 2 || _gardenTutorialStage == 3) && !onWater && !onPlant
                    || _gardenTutorialStage == 4 && !onStore
                    || _gardenTutorialStage == 5 && !onFertilizer && !onWater && !onPlant)
                {
                    return true;
                }
            }

            if (new Rect(674, 6, 120, 36).Contains(point))
            {
                if (_gardenReturnToAdventure && _gardenTutorialStage == 6)
                {
                    try
                    {
                        if (_owner.Campaign._adventureCampaign == null || _owner.Campaign._adventureCampaign.ProfileId != _owner._profiles.SelectedId.Value)
                        {
                            _owner.Campaign._adventureCampaign = new AdventureCampaign(_owner.Campaign._adventureStore, _owner._profiles.SelectedId.Value, _owner.Accounts._accountStore);
                        }

                        var next = _owner.Campaign._adventureCampaign.Game == null
                            ? _owner.Campaign._adventureCampaign.BeginOrResume()
                            : _owner.Campaign._adventureCampaign.Game.Adventure.Phase != AdventurePhase.Finished
                                ? _owner.Campaign._adventureCampaign.Game
                                : _owner.Campaign._adventureCampaign.StartNextLevel() ? _owner.Campaign._adventureCampaign.Game : null;
                        if (next != null)
                        {
                            _owner.Accounts._accountStore.FinishZenGardenTutorial(_owner._profiles.SelectedId.Value);
                            _gardenReturnToAdventure = false;
                            _gardenTutorialStage = 0;
                            _owner.Campaign.AttachAdventureGame(next);
                            _owner._currentView = PvZGame.ScreenView.Board;
                        }
                    }
                    catch (Exception error)
                    {
                        _owner.Accounts._progressionMessage = error.Message;
                    }
                }
                else if (!_gardenReturnToAdventure)
                {
                    _owner._currentView = PvZGame.ScreenView.Selector;
                }

                return true;
            }

            if (new Rect(654, 540, 136, 42).Contains(point))
            {
                if (_gardenTutorialStage == 4)
                {
                    _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.GrantZenGardenTutorialFertilizer(_owner._profiles.SelectedId.Value));
                    if (_owner.Accounts._progressionMessage == null)
                    {
                        _gardenTutorialStage = 5;
                        _owner.Store.OpenStoreForGardenIntro(2600);
                    }
                }
                else if (_gardenTutorialStage == 0 || _gardenTutorialStage == 6)
                {
                    _owner.Store.OpenStore();
                }

                return true;
            }

            if (new Rect(175, 540, 120, 42).Contains(point))
            {
                _owner.Tree.OpenWisdomTree();
                return true;
            }

            if (new Rect(8, 540, 155, 42).Contains(point))
            {
                var p = _owner.Accounts.GetSelectedAccount();
                var next = (GardenKind)(((int)_gardenKind + 1) % 3);
                if (next == GardenKind.Mushroom && p.PurchaseCount(18) == 0)
                {
                    next = GardenKind.Aquarium;
                }

                if (next == GardenKind.Aquarium && p.PurchaseCount(25) == 0)
                {
                    next = GardenKind.Greenhouse;
                }

                if (_movingGardenPlant.HasValue)
                {
                    var id = _movingGardenPlant.Value;
                    _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.TendGarden(_owner._profiles.SelectedId.Value, id, GardenTool.Move, PvZGame.UtcSeconds, next));
                    _movingGardenPlant = null;
                }

                _gardenKind = next;
                return true;
            }

            if (new Rect(490, 490, 105, 82).Contains(point) && _owner.Accounts.GetSelectedAccount().PurchaseCount(20) > 0)
            {
                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.WakeSnail(_owner._profiles.SelectedId.Value, PvZGame.UtcSeconds));
                return true;
            }

            for (var i = 0; i < 8; i++)
            {
                if (new Rect(8 + i * 78, 4, 73, 57).Contains(point))
                {
                    if (_gardenTutorialStage == 1 && i == 0)
                    {
                        _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.SetZenGardenTutorialStage(_owner._profiles.SelectedId.Value, 3));
                        if (_owner.Accounts._progressionMessage != null)
                        {
                            return true;
                        }

                        _gardenTutorialStage = 2;
                        _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_WATER_PLANT");
                    }

                    _gardenTool = (GardenTool)i;
                    return true;
                }
            }

            foreach (var g in _owner.Accounts.GetSelectedAccount().Plants)
            {
                if (g.Garden == _gardenKind && GardenPlantRect(g).Contains(point))
                {
                    if (_gardenTool == GardenTool.Sell)
                    {
                        BeginGardenSale(g);
                    }
                    else if (_gardenTool == GardenTool.Move)
                    {
                        _movingGardenPlant = g.Id;
                        _owner.Accounts._progressionMessage = "选择下一个花园，将植物放过去。";
                    }
                    else
                    {
                        _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.TendGarden(_owner._profiles.SelectedId.Value, g.Id, _gardenTool, PvZGame.UtcSeconds));
                        if (_owner.Accounts._progressionMessage == null && _gardenTool == GardenTool.Water && (_gardenTutorialStage == 2 || _gardenTutorialStage == 3))
                        {
                            _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.SetZenGardenTutorialStage(_owner._profiles.SelectedId.Value, 4));
                            if (_owner.Accounts._progressionMessage != null)
                            {
                                return true;
                            }

                            _gardenTutorialStage = 3;
                            _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_KEEP_WATERING");
                            foreach (var plant in _owner.Accounts.GetSelectedAccount().Plants)
                            {
                                if (plant.Need(PvZGame.UtcSeconds) == GardenNeed.Fertilizer)
                                {
                                    _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.SetZenGardenTutorialStage(_owner._profiles.SelectedId.Value, 5));
                                    if (_owner.Accounts._progressionMessage != null)
                                    {
                                        return true;
                                    }

                                    _gardenTutorialStage = 4;
                                    _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_VISIT_STORE");
                                    break;
                                }
                            }
                        }
                        else if (_owner.Accounts._progressionMessage == null && _gardenTool == GardenTool.Water && _gardenTutorialStage == 5)
                        {
                            _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_FERTILIZE");
                        }
                        else if (_owner.Accounts._progressionMessage == null && _gardenTool == GardenTool.Fertilizer && _gardenTutorialStage == 5)
                        {
                            var account = _owner.Accounts.GetSelectedAccount();
                            var allFertilized = true;
                            foreach (var plant in account.Plants)
                            {
                                if (plant.Growth == 0)
                                {
                                    allFertilized = false;
                                    break;
                                }
                            }

                            if (allFertilized)
                            {
                                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.SetZenGardenTutorialStage(_owner._profiles.SelectedId.Value, 7));
                                if (_owner.Accounts._progressionMessage != null)
                                {
                                    return true;
                                }

                                _gardenTutorialStage = 6;
                                _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_CONTINUE_ADVENTURE");
                            }
                            else if (account.PurchaseCount(14) == 0)
                            {
                                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.GrantZenGardenTutorialFertilizer(_owner._profiles.SelectedId.Value, false));
                                _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_NEED_MORE_FERTILIZER");
                            }
                            else
                            {
                                _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_FERTILIZE");
                            }
                        }
                    }

                    return true;
                }
            }

            return true;
        }

        /// <summary>Identifies the garden plant awaiting a sale decision.</summary>
        internal Guid? _gardenSalePlant;
        /// <summary>Whether Dave is present for a garden sale or departure.</summary>
        internal bool _gardenSaleDaveActive;
        /// <summary>Bounds of the original garden sale confirmation's yes button.</summary>
        internal static readonly Rect s_gardenSaleYes = new Rect(365, 406, 135, 46);
        /// <summary>Bounds of the original garden sale confirmation's no button.</summary>
        internal static readonly Rect s_gardenSaleNo = new Rect(540, 406, 135, 46);

        /// <summary>Asks Dave's original sale question without changing the plant or account.</summary>
        /// <param name="plant">The plant selected by the money-sign cursor.</param>
        private void BeginGardenSale(GardenPlant plant)
        {
            var key = plant.Kind == PlantKind.Marigold && plant.Growth == 0
                ? "MARIGOLD_SPROUT" : VerifiedDataCatalog.Plants[(int)plant.Kind].InternalName;
            var name = _owner._presenter.OriginalProgressionText(key);
            var message = _owner._presenter.OriginalProgressionText("CRAZY_DAVE_1700")
                .Replace("{SELL_PRICE}", PlayerAccountStore.SellPrice(plant).ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("{PLANT_TYPE}", name);
            _gardenSalePlant = plant.Id;
            _gardenSaleDaveActive = true;
            _owner.Accounts._progressionMessage = null;
            _owner.Dave.ShowCrazyDaveResolvedMessage(1700, message);
            _owner.Dave.SetCrazyDaveAnimation("anim_blahblah", 12f, false);
        }

        /// <summary>Resolves the original garden sale confirmation.</summary>
        /// <param name="accepted">Whether the player agreed to sell the plant.</param>
        internal void ResolveGardenSale(bool accepted)
        {
            if (!_gardenSalePlant.HasValue)
            {
                return;
            }

            if (accepted)
            {
                var id = _gardenSalePlant.Value;
                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.TendGarden(_owner._profiles.SelectedId.Value, id, GardenTool.Sell, PvZGame.UtcSeconds));
                if (_owner.Accounts._progressionMessage != null)
                {
                    return;
                }

                _owner.Accounts._displayAccountSnapshot = null;
            }

            _gardenSalePlant = null;
            _owner.Dave.LeaveCrazyDaveDialogue();
        }
    }
}
