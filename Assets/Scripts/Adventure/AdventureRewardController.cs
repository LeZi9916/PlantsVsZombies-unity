#nullable enable
using PlantsVsZombies.Progression;
using System;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Drives the end-of-level reward screen: its audio, continuation into the next level and pointer input.</summary>
    internal sealed class AdventureRewardController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose adventure board and campaign the rewards advance.</param>
        internal AdventureRewardController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores award introduction audio.</summary>
        private AdventureRewardPresentation _awardIntroductionAudio;
        /// <summary>Stores award seed next.</summary>
        internal static readonly Rect s_awardSeedNext = new Rect(324, 546, 156, 42);
        /// <summary>Stores award menu.</summary>
        internal static readonly Rect s_awardMenu = new Rect(677, 553, 111, 26);

        /// <summary>Performs update adventure reward continuation.</summary>
        internal void UpdateAdventureRewardContinuation()
        {
            if (_owner._currentView != PvZGame.ScreenView.Board || _owner._game == null || !_owner._game.IsAdventure || _owner.IndependentModes.IsModeBoard)
            {
                return;
            }

            var level = _owner._game.Adventure.Level;
            var reward = _owner._game.Adventure.RewardPresentation;
            if (reward != null && reward.Stage == AdventureAwardStage.Introduction && !ReferenceEquals(_awardIntroductionAudio, reward))
            {
                _awardIntroductionAudio = reward;
                if (reward.NativeType == 15 || reward.NativeType == 18 && level.Number == 50)
                {
                    _owner.Sounds.PlayAdventureRewardSound("paper.ogg");
                }
            }

            if (_owner.Campaign._adventureTestBoard || _owner.Campaign._adventureCampaign == null)
            {
                return;
            }

            // PC LawnApp::CheckForGameEnd 0x45F6E0 skips plant awards on replay, except notes/final awards.
            if (!level.IsReplay || level.Number == 50 || reward == null || reward.NativeType != 18 || reward.Stage != AdventureAwardStage.Introduction)
            {
                return;
            }

            if (_owner.AdventureBoard._adventureFeedback != null && Time.unscaledTime < _owner.AdventureBoard._adventureFeedbackUntil)
            {
                return;
            }

            try
            {
                if (_owner.Campaign._adventureCampaign.StartNextLevel())
                {
                    _owner.Campaign.AttachAdventureGame(_owner.Campaign._adventureCampaign.Game);
                }
            }
            catch (Exception error)
            {
                _owner.AdventureBoard.AdventureFeedback("下一关无法开始：" + error.Message, 5f);
            }
        }

        /// <summary>Performs handle adventure reward click.</summary>
        /// <param name = "point">The point.</param>
        internal void HandleAdventureRewardClick(Vector2 point)
        {
            var reward = _owner._game.Adventure.RewardPresentation;
            if (reward == null)
            {
                return;
            }

            if (reward.Stage != AdventureAwardStage.Introduction)
            {
                if (!reward.Contains(point.x, point.y))
                {
                    return;
                }

                try
                {
                    var collected = _owner.Campaign._adventureTestBoard ? _owner._game.CollectAdventureReward() : _owner.Campaign._adventureCampaign.CollectReward();
                    if (!collected)
                    {
                        _owner.AdventureBoard.AdventureFeedback("奖励尚未保存：" + (_owner.Campaign._adventureCampaign == null ? "请重试" : _owner.Campaign._adventureCampaign.LastPersistenceError), 5f);
                    }
                }
                catch (Exception error)
                {
                    _owner.AdventureBoard.AdventureFeedback("奖励尚未保存，请重试：" + error.Message, 6f);
                }

                return;
            }

            // PC AwardScreen starts its counter at180; controls enable when it reaches zero.
            if (reward.IntroductionTicks < 180)
            {
                return;
            }

            if (s_awardMenu.Contains(point))
            {
                _owner.Sounds.PlayAdventureRewardSound("buttonclick.ogg");
                _owner._currentView = PvZGame.ScreenView.Selector;
                return;
            }

            var next = reward.NativeType == 15 || reward.NativeType == 18 && _owner._game.Adventure.Level.Number == 50 ? new Rect(324, 500, 156, 42) : s_awardSeedNext;
            if (!next.Contains(point))
            {
                return;
            }

            _owner.Sounds.PlayAdventureRewardSound("buttonclick.ogg");
            try
            {
                var intro = CrazyDaveStoreDialog.AdventureRewardIntroduction(_owner._game.Adventure.Level.Number, _owner._game.Adventure.Level.IsReplay);
                if (!_owner.Campaign._adventureTestBoard && intro >= 0)
                {
                    _owner.Store.OpenStoreForAdventureIntro(intro);
                    return;
                }

                if (!_owner.Campaign._adventureTestBoard && !_owner._game.Adventure.Level.IsReplay && _owner._game.Adventure.Level.Number == 44)
                {
                    _owner.Garden.OpenZenGardenForAdventureIntro();
                    return;
                }

                if (_owner.Campaign._adventureTestBoard || _owner.Campaign._adventureCampaign.Progress.Completed)
                {
                    _owner._currentView = PvZGame.ScreenView.Selector;
                }
                else if (_owner.Campaign._adventureCampaign.StartNextLevel())
                {
                    _owner.Campaign.AttachAdventureGame(_owner.Campaign._adventureCampaign.Game);
                }
            }
            catch (Exception error)
            {
                _owner.AdventureBoard.AdventureFeedback("下一关无法开始：" + error.Message, 5f);
            }
        }

        /// <summary>Performs adventure reward name.</summary>
        /// <returns>The resulting adventure reward name.</returns>
        private string AdventureRewardName()
        {
            var level = _owner._game.Adventure.Level;
            if (level.IsReplay)
            {
                return level.Number == 50 ? "获得金币奖励和向日葵奖杯" : "获得金币奖励";
            }

            if (level.RewardSeed.HasValue)
            {
                return "获得 " + _owner.AdventureBoard.AdventurePlantName(level.RewardSeed.Value);
            }

            switch (level.RewardKind)
            {
                case AdventureRewardKind.Shovel:
                    return "获得铲子";
                case AdventureRewardKind.Almanac:
                    return "获得图鉴";
                case AdventureRewardKind.CarKeys:
                    return "获得戴夫的车钥匙";
                case AdventureRewardKind.Taco:
                    return "获得玉米卷";
                case AdventureRewardKind.WateringCan:
                    return "获得浇水壶";
                case AdventureRewardKind.Note:
                    return "僵尸送来了一张纸条";
                default:
                    return "获得银向日葵奖杯";
            }
        }
    }
}
