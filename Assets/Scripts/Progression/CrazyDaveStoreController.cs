#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Adventure;
using System;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Progression
{
    /// <summary>Runs Crazy Dave's store screen: introduction speech, hatch animation, taco diamond, shop pages and purchases.</summary>
    internal sealed class CrazyDaveStoreController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose profile, account and Dave the store uses.</param>
        internal CrazyDaveStoreController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Tracks Crazy Dave's timed store messages.</summary>
        internal CrazyDaveStoreSpeechClock? _storeSpeechClock;

        /// <summary>Gets the store speech clock, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal CrazyDaveStoreSpeechClock RequiredStoreSpeechClock => _storeSpeechClock ?? throw new System.InvalidOperationException("The member '_storeSpeechClock' is not available.");
        /// <summary>Stores the fractional original store update time.</summary>
        private float _storeSpeechRemainder;
        /// <summary>Starts one store speech message without per-tick delegate allocation.</summary>
        private Action<int>? _storeSpeak;
        /// <summary>Stops store speech without per-tick delegate allocation.</summary>
        private Action? _storeStopSpeech;
        /// <summary>Selects an original store speech random value.</summary>
        private static readonly Func<int, int> s_storeRandomBelow = limit => UnityEngine.Random.Range(0, limit);
        /// <summary>Stores shop page.</summary>
        internal int _shopPage;
        /// <summary>Stores selected shop item.</summary>
        internal int _selectedShopItem = -1;
        /// <summary>Stores store return view.</summary>
        internal PvZGame.ScreenView _storeReturnView = PvZGame.ScreenView.Selector;
        /// <summary>The original store introduction awaiting Dave's entrance, or -1.</summary>
        private int _storeIntroPending = -1;
        /// <summary>Whether the current store visit returns to the next adventure level.</summary>
        internal bool _storeAdventureIntro;
        /// <summary>Whether the store back button uses the original introduction caption.</summary>
        internal bool _storeIntroNextLabel;
        /// <summary>Whether the next adventure board was already prepared before opening the store.</summary>
        internal bool _storeAdventureGamePrepared;
        /// <summary>Whether the store introduction requires a click to advance.</summary>
        internal bool _storeIntroTalking;
        /// <summary>Whether the magic taco diamond is visible in the store.</summary>
        internal bool _storeTacoDiamondVisible;
        /// <summary>Elapsed time since the magic taco diamond appeared.</summary>
        internal float _storeTacoDiamondAge;
        /// <summary>Gets the original from-present diamond's vertical position.</summary>
        internal float StoreTacoDiamondY => 520f - 100f * (1f - Mathf.Pow(.95f, _storeTacoDiamondAge * 100f));
        /// <summary>Whether the store hatch is open after its animation.</summary>
        internal bool _storeHatchOpen;
        /// <summary>Remaining native ticks of the store hatch animation.</summary>
        internal int _storeHatchTicks;
        /// <summary>Stores original shop pages.</summary>
        internal static readonly int[][] s_originalShopPages =
        {
            new[]
            {
                21,
                22,
                24,
                23,
                0,
                1,
                2,
                3
            },
            new[]
            {
                6,
                5,
                4,
                7,
                8,
                29
            },
            new[]
            {
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                17
            },
            new[]
            {
                18,
                25,
                19,
                20,
                27,
                28
            }
        };

        /// <summary>Performs open store.</summary>
        internal void OpenStore()
        {
            if (!_owner.Accounts.EnsurePlayerAccount() || !_owner.Campaign.EnsureAdventureStore())
            {
                return;
            }

            var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner.RequiredProfiles.RequiredSelectedId);
            if (progress.CompletedRuns == 0 && progress.NextLevel < 25)
            {
                _owner.Menus.SetUnavailable("疯狂戴夫的商店", "通过冒险模式 3-4，取得车钥匙后即可进入。");
                return;
            }

            _storeReturnView = _owner._currentView == PvZGame.ScreenView.Garden || _owner._currentView == PvZGame.ScreenView.Tree ? _owner._currentView : PvZGame.ScreenView.Selector;
            _owner._currentView = PvZGame.ScreenView.Store;
            _storeIntroPending = -1;
            _storeAdventureIntro = false;
            _storeIntroNextLabel = false;
            _storeAdventureGamePrepared = false;
            _storeIntroTalking = false;
            _storeHatchOpen = true;
            _storeHatchTicks = 0;
            _storeTacoDiamondVisible = _owner.Accounts.RequiredGetSelectedAccount().TacoDiamondPresented;
            _storeTacoDiamondAge = 0f;
            _shopPage = 0;
            _selectedShopItem = -1;
            _owner.Accounts._progressionMessage = null;
            _storeSpeechClock = new CrazyDaveStoreSpeechClock();
            _storeSpeechRemainder = 0f;
            _storeSpeak ??= _owner.Dave.ShowCrazyDaveMessage;
            _storeStopSpeech ??= StopCrazyDaveStoreSpeech;
            _owner.Dave._crazyDaveMessageIndex = -1;
            _owner.Dave._crazyDaveText = "";
            _owner.Dave._crazyDaveHanding = false;
            _owner.Dave._crazyDaveHeldItem = null;
            _owner.Dave._crazyDaveIdleMouth = null;
            _owner.Dave._crazyDaveBlinkAge = -1f;
            _owner.Dave._crazyDaveBlinkTicks = UnityEngine.Random.Range(400, 801);
            _owner.Dave._crazyDaveVoice.Stop();
            _owner.Dave.SetCrazyDaveAnimation("anim_enter", 24f, false);
        }

        /// <summary>Opens the original store interlude before the next adventure level.</summary>
        /// <param name="message">The first original dialogue entry.</param>
        internal void OpenStoreForAdventureIntro(int message)
        {
            OpenStore();
            if (_owner._currentView == PvZGame.ScreenView.Store)
            {
                _storeAdventureIntro = true;
                _storeIntroNextLabel = true;
                _storeIntroPending = message;
                _storeHatchOpen = false;
                if (message == 601)
                {
                    _storeTacoDiamondVisible = false;
                }
            }
        }

        /// <summary>Opens a click-controlled store introduction from the Zen Garden.</summary>
        /// <param name="message">The first original dialogue entry.</param>
        internal void OpenStoreForGardenIntro(int message)
        {
            OpenStore();
            if (_owner._currentView == PvZGame.ScreenView.Store)
            {
                _storeIntroPending = message;
                _storeHatchOpen = false;
                _storeIntroNextLabel = true;
            }
        }

        /// <summary>Advances a click-controlled original store introduction.</summary>
        internal void AdvanceStoreIntro()
        {
            if (_owner.Dave._crazyDaveMessageIndex == 3100)
            {
                BeginStoreHatchOpen();
            }

            var next = CrazyDaveDialogue.NextMessageIndex(_owner.Dave._crazyDaveMessageIndex, index =>
            {
                var key = "CRAZY_DAVE_" + index;
                var value = _owner._presenter.OriginalProgressionText(key);
                return !string.IsNullOrEmpty(value) && value != key;
            });
            if (next >= 0)
            {
                if (next == 303 || next == 606 || next == 2601)
                {
                    BeginStoreHatchOpen();
                }

                if (next == 603 && _owner.Accounts.RequiredGetSelectedAccount().HasPendingTacoReward)
                {
                    _owner.Accounts.ProgressionAction(() => _owner.Accounts.RequiredAccountStore.PresentTacoReward(_owner.RequiredProfiles.RequiredSelectedId));
                    if (_owner.Accounts._progressionMessage != null)
                    {
                        return;
                    }

                    _storeTacoDiamondVisible = true;
                    _storeTacoDiamondAge = 0f;
                    _owner.Sounds.PlayPresentationSound("diamond");
                }

                RequiredStoreSpeechClock.SetMessage(next, 0);
                _owner.Dave.ShowCrazyDaveMessage(next);
                return;
            }

            _storeIntroTalking = false;
            StopCrazyDaveStoreSpeech();
        }

        /// <summary>Starts the original store hatch opening at its dialogue cue.</summary>
        private void BeginStoreHatchOpen()
        {
            _storeHatchOpen = true;
            _storeHatchTicks = 150;
            _owner.Sounds.PlayPresentationSound("hatchback_open");
        }

        /// <summary>Returns from the store to the appropriate original destination.</summary>
        internal void LeaveStore()
        {
            if (_storeIntroPending >= 0 || _storeIntroTalking || _storeHatchTicks > 0)
            {
                return;
            }

            if (_storeAdventureIntro)
            {
                try
                {
                    if (_storeAdventureGamePrepared || _owner.Campaign.RequiredAdventureCampaign.StartNextLevel())
                    {
                        _owner.Campaign.AttachAdventureGame(_owner.Campaign.RequiredAdventureCampaign.Game);
                        _storeAdventureIntro = false;
                        _storeAdventureGamePrepared = false;
                        _owner._currentView = PvZGame.ScreenView.Board;
                    }
                }
                catch (Exception error)
                {
                    _owner.Accounts._progressionMessage = error.Message;
                }

                return;
            }

            _owner._currentView = _storeReturnView;
            if (_owner._currentView == PvZGame.ScreenView.Garden && _owner.Garden._gardenTutorialStage == 5)
            {
                _owner.Accounts.ProgressionAction(() => _owner.Accounts.RequiredAccountStore.SetZenGardenTutorialStage(_owner.RequiredProfiles.RequiredSelectedId, 6));
                if (_owner.Accounts._progressionMessage == null)
                {
                    _owner.Accounts._progressionMessage = _owner._presenter.OriginalProgressionText("ADVICE_ZEN_GARDEN_FERTILIZE");
                }
            }
        }

        /// <summary>Persists the magic taco diamond before hiding its store presentation.</summary>
        private void CollectStoreTacoDiamond()
        {
            _owner.Accounts.ProgressionAction(() =>
            {
                if (_owner.Accounts.RequiredAccountStore.CollectTacoReward(_owner.RequiredProfiles.RequiredSelectedId))
                {
                    _storeTacoDiamondVisible = false;
                    _owner.Accounts._displayAccountSnapshot = null;
                    _owner.Sounds.PlayPresentationSound("points");
                }
            });
        }

        /// <summary>Advances the original store speech and releases it when the store closes.</summary>
        /// <param name="delta">Elapsed unscaled time in seconds.</param>
        internal void UpdateCrazyDaveStore(float delta)
        {
            var clock = _storeSpeechClock;
            if (clock == null)
            {
                return;
            }

            if (_owner._currentView != PvZGame.ScreenView.Store)
            {
                StopCrazyDaveStoreSpeech();
                _owner.Dave._crazyDaveAnimation = null;
                _storeSpeechClock = null;
                return;
            }

            if (_owner.RequiredOptionsView.IsOpen || (_owner._profileDialog != null && _owner._profileDialog.IsOpen) || _owner._unavailableFeature != null)
            {
                return;
            }

            _storeSpeechRemainder += Mathf.Max(0f, delta);
            var account = _owner.Accounts.GetSelectedAccount();
            if (account == null)
            {
                return;
            }

            var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner.RequiredProfiles.RequiredSelectedId);
            var completed = progress.CompletedRuns > 0;
            int? hoveredMessage = null;
            if (_selectedShopItem < 0 && GameInput.Current.InsideViewport)
            {
                var page = s_originalShopPages[_shopPage];
                for (var index = 0; index < page.Length; index++)
                {
                    var rect = ShopItemRect(index);
                    var hoverRect = new Rect(rect.x, rect.y, 50, 87);
                    var itemId = page[index];
                    if (hoverRect.Contains(_owner.AdventureBoard._adventurePointer) && CrazyDaveStoreDialog.CanHover(itemId, progress.NextLevel, completed))
                    {
                        hoveredMessage = CrazyDaveStoreDialog.GetHoverMessageIndex(itemId, account.PurchaseCount(21));
                        break;
                    }
                }
            }

            while (_storeSpeechRemainder >= .01f)
            {
                _storeSpeechRemainder -= .01f;
                _owner.Dave.UpdateCrazyDaveAnimation(.01f);
                if (_storeTacoDiamondVisible)
                {
                    _storeTacoDiamondAge += .01f;
                    if (_storeTacoDiamondAge >= .8f)
                    {
                        CollectStoreTacoDiamond();
                    }
                }
                var hatchBusy = _storeHatchTicks > 0;
                if (hatchBusy && _owner.Dave._crazyDaveAnimation != "anim_enter")
                {
                    _storeHatchTicks--;
                }
                if (_storeIntroPending >= 0 && _owner.Dave._crazyDaveAnimation != "anim_enter")
                {
                    var message = _storeIntroPending;
                    _storeIntroPending = -1;
                    var key = "CRAZY_DAVE_" + message;
                    var text = _owner._presenter.OriginalProgressionText(key);
                    if (!string.IsNullOrEmpty(text) && text != key)
                    {
                        _storeIntroTalking = true;
                        clock.SetMessage(message, 0);
                        _owner.Dave.ShowCrazyDaveMessage(message);
                    }
                    else
                    {
                        Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave text {key} is missing.");
                    }
                }

                clock.Advance(true, _owner.Dave._crazyDaveAnimation == "anim_enter", _selectedShopItem >= 0,
                    hatchBusy, _storeHatchTicks > 0, _storeIntroPending >= 0, _storeIntroTalking, _owner.Dave._crazyDaveWaitForBubbleVoice && _owner.Dave._crazyDaveVoice.isPlaying,
                    hoveredMessage, completed, s_storeRandomBelow, _storeSpeak!, _storeStopSpeech!);
            }
        }

        /// <summary>Stops a timed store message and returns Dave to his idle pose.</summary>
        internal void StopCrazyDaveStoreSpeech()
        {
            _storeSpeechClock?.ClearMessage();
            _owner.Dave._crazyDaveMessageIndex = -1;
            _owner.Dave._crazyDaveText = "";
            _owner.Dave._crazyDaveHanding = false;
            _owner.Dave._crazyDaveHeldItem = null;
            _owner.Dave._crazyDaveIdleMouth = null;
            _owner.Dave._crazyDaveVoice.Stop();
            _owner.Dave.SetCrazyDaveAnimation("anim_idle", 12f, true);
        }

        /// <summary>Performs shop item rect.</summary>
        /// <param name = "slot">The slot.</param>
        /// <returns>The resulting shop item rect.</returns>
        internal static Rect ShopItemRect(int slot)
        {
            return slot < 4 ? new Rect(422 + 74 * slot, 206, 70, 100) : new Rect(372 + 74 * (slot - 4), 310, 70, 100);
        }

        /// <summary>Handles a pointer press inside Crazy Dave's store.</summary>
        /// <param name="point">The pointer in the original 800 by 600 coordinate space.</param>
        /// <returns>Always true, because the store consumes every press.</returns>
        internal bool HandleStoreClick(Vector2 point)
        {
            if (_storeIntroPending >= 0)
            {
                return true;
            }

            if (_storeIntroTalking)
            {
                AdvanceStoreIntro();
                return true;
            }

            if (_storeTacoDiamondVisible && new Rect(77, StoreTacoDiamondY, 40, 65).Contains(point))
            {
                CollectStoreTacoDiamond();
                return true;
            }

            if (_storeHatchTicks > 0)
            {
                return true;
            }

            if (_selectedShopItem >= 0)
            {
                if (new Rect(245, 346, 135, 46).Contains(point))
                {
                    var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner.RequiredProfiles.RequiredSelectedId);
                    _owner.Accounts.ProgressionAction(() =>
                    {
                        _owner.Accounts.RequiredAccountStore.Purchase(_owner.RequiredProfiles.RequiredSelectedId, _selectedShopItem, progress.NextLevel, progress.CompletedRuns, PvZGame.UtcSeconds);
                        if (_selectedShopItem == 29)
                        {
                            _storeSpeechClock?.SetMessage(3400, 800);
                            _owner.Dave.ShowCrazyDaveMessage(3400);
                        }

                        _selectedShopItem = -1;
                        _owner.Accounts._displayAccountSnapshot = null;
                    });
                }
                else if (new Rect(420, 346, 135, 46).Contains(point))
                {
                    _selectedShopItem = -1;
                    _owner.Accounts._progressionMessage = null;
                }

                return true;
            }

            if (new Rect(366, 512, 138, 80).Contains(point))
            {
                LeaveStore();
                return true;
            }

            if (new Rect(252, 402, 96, 75).Contains(point))
            {
                StopCrazyDaveStoreSpeech();
                _shopPage = (_shopPage + 3) % 4;
                _selectedShopItem = -1;
                return true;
            }

            if (new Rect(596, 402, 136, 83).Contains(point))
            {
                StopCrazyDaveStoreSpeech();
                _shopPage = (_shopPage + 1) % 4;
                _selectedShopItem = -1;
                return true;
            }

            var page = s_originalShopPages[_shopPage];
            for (var i = 0; i < page.Length; i++)
            {
                if (ShopItemRect(i).Contains(point))
                {
                    var item = ShopCatalog.Get(page[i]);
                    var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner.RequiredProfiles.RequiredSelectedId);
                    var account = _owner.Accounts.GetSelectedAccount();
                    if (ShopCatalog.IsUnlocked(item, progress.NextLevel, progress.CompletedRuns) && !ShopCatalog.SoldOut(item, account, PvZGame.UtcSeconds))
                    {
                        StopCrazyDaveStoreSpeech();
                        _selectedShopItem = page[i];
                    }

                    _owner.Accounts._progressionMessage = null;
                    return true;
                }
            }

            return true;
        }
    }
}
