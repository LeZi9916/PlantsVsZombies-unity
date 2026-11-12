#nullable enable
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Progression
{
    /// <summary>Runs the Wisdom Tree screen: Dave's first introduction, tree feeding and the tree's speech clock.</summary>
    internal sealed class WisdomTreeController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose profile, account and Dave the tree page uses.</param>
        internal WisdomTreeController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores tree grow started.</summary>
        internal float _treeGrowStarted = -100f;
        /// <summary>Whether the first-visit tree conversation has not yet been acknowledged.</summary>
        internal bool _treeDaveIntroPending;
        /// <summary>Whether Dave is entering, talking, or departing from the tree.</summary>
        internal bool _treeDaveIntroActive;
        /// <summary>Elapsed time since the first tree introduction started.</summary>
        private float _treeDaveIntroTime;
        /// <summary>Runs the tree's original timed wisdom and idle speech.</summary>
        internal WisdomTreeSpeechClock? _treeSpeechClock;
        /// <summary>Accumulates elapsed time between native speech updates.</summary>
        private double _treeSpeechRemainder;

        /// <summary>Performs open wisdom tree.</summary>
        internal void OpenWisdomTree()
        {
            var p = _owner.Accounts.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            if (p.PurchaseCount(27) == 0)
            {
                _owner.Menus.SetUnavailable("智慧树", "请先在商店购买智慧树。");
                return;
            }

            if (_owner.Store._storeSpeechClock != null)
            {
                _owner.Store.StopCrazyDaveStoreSpeech();
                _owner.Store._storeSpeechClock = null;
            }

            _owner._currentView = PvZGame.ScreenView.Tree;
            _treeGrowStarted = -100f;
            _owner.Accounts._progressionMessage = null;
            _treeSpeechClock = new WisdomTreeSpeechClock(bound => UnityEngine.Random.Range(0, bound));
            _treeSpeechRemainder = 0;
            _treeDaveIntroTime = 0f;
            _treeDaveIntroPending = !p.TreeFoodGiftClaimed;
            _treeDaveIntroActive = _treeDaveIntroPending;
            _owner.Dave._crazyDaveMessageIndex = -1;
            _owner.Dave._crazyDaveAnimation = null;
            _owner.Dave._crazyDaveHeldItem = null;
            _owner.Dave._crazyDaveVoice.Stop();
        }

        /// <summary>Advances the first tree introduction and the tree's native speech clock.</summary>
        /// <param name="delta">Elapsed unscaled seconds.</param>
        internal void UpdateWisdomTreeDialogue(float delta)
        {
            if (_owner._currentView != PvZGame.ScreenView.Tree || _owner._optionsView.IsOpen || (_owner._profileDialog != null && _owner._profileDialog.IsOpen) || _owner._unavailableFeature != null || delta <= 0f)
            {
                return;
            }

            if (_treeDaveIntroActive)
            {
                _treeDaveIntroTime += delta;
                _owner.Dave.UpdateCrazyDaveAnimation(delta);
                if (_treeDaveIntroPending && _owner.Dave._crazyDaveAnimation == null && _treeDaveIntroTime >= 2f && _treeDaveIntroTime < 2.75f)
                {
                    _owner.Dave.SetCrazyDaveAnimation("anim_enter", 24f, false);
                }

                if (_treeDaveIntroPending && _owner.Dave._crazyDaveMessageIndex < 0 && _treeDaveIntroTime >= 2.75f)
                {
                    var first = _owner._presenter.OriginalProgressionText("CRAZY_DAVE_3200");
                    if (string.IsNullOrEmpty(first) || first == "CRAZY_DAVE_3200")
                    {
                        Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave text CRAZY_DAVE_3200 is missing.");
                        CompleteWisdomTreeIntroduction();
                    }
                    else
                    {
                        _owner.Dave.ShowCrazyDaveMessage(3200);
                    }
                }

                if (!_treeDaveIntroPending && (_owner.Dave._crazyDaveAnimation == null || _owner.Dave._crazyDaveAnimation == "anim_leave" && _owner.Dave._crazyDaveAnimationAge >= .75f))
                {
                    _owner.Dave._crazyDaveAnimation = null;
                    _treeDaveIntroActive = false;
                }
            }

            if (_treeDaveIntroPending || _treeSpeechClock == null)
            {
                return;
            }

            var account = _owner.Accounts.GetSelectedAccount();
            if (account == null)
            {
                return;
            }

            _treeSpeechRemainder += delta;
            while (_treeSpeechRemainder >= .01)
            {
                _treeSpeechRemainder -= .01;
                _treeSpeechClock.Tick(account.TreeHeight);
            }
        }

        /// <summary>Grants the original five tree-food bags when Dave's introduction is acknowledged.</summary>
        internal void CompleteWisdomTreeIntroduction()
        {
            _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.ClaimWisdomTreeFoodGift(_owner._profiles.SelectedId.Value));
            if (!_owner.Accounts.GetSelectedAccount().TreeFoodGiftClaimed)
            {
                return;
            }

            _treeDaveIntroPending = false;
            _owner.Dave.LeaveCrazyDaveDialogue();
        }

        /// <summary>Handles a pointer press on the Wisdom Tree page.</summary>
        /// <param name="point">The pointer in the original 800 by 600 coordinate space.</param>
        /// <returns>Always true, because the tree page consumes every press.</returns>
        internal bool HandleTreeClick(Vector2 point)
        {
            if (new Rect(655, 8, 138, 40).Contains(point))
            {
                _treeDaveIntroActive = false;
                _owner.Dave._crazyDaveAnimation = null;
                _owner._currentView = PvZGame.ScreenView.Garden;
            }
            else if (new Rect(655, 548, 138, 42).Contains(point))
            {
                _treeDaveIntroActive = false;
                _owner.Dave._crazyDaveAnimation = null;
                _owner.Store.OpenStore();
            }
            else if (new Rect(24, 505, 253, 80).Contains(point))
            {
                if (Time.unscaledTime - _treeGrowStarted < 1.2f)
                {
                    return true;
                }

                _owner.Accounts.ProgressionAction(() => _owner.Accounts._accountStore.FeedWisdomTree(_owner._profiles.SelectedId.Value));
                if (_owner.Accounts._progressionMessage == null)
                {
                    _treeGrowStarted = Time.unscaledTime;
                    _treeSpeechClock?.StartGrowth();
                }
            }

            return true;
        }
    }
}
