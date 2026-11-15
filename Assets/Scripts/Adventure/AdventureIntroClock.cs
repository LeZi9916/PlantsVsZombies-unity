#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro clock.</summary>
    public sealed class AdventureIntroClock
    {
        /// <summary>Stores plan.</summary>
        public readonly AdventureIntroPlan Plan;
        /// <summary>Gets or sets native time.</summary>
        public int NativeTime { get; private set; }
        /// <summary>Gets or sets waiting for seeds.</summary>
        public bool WaitingForSeeds { get; private set; }
        /// <summary>The last sampled immutable-in-use frame for the current clock state.</summary>
        private AdventureIntroFrame? _frame;
        /// <summary>The native time used to create the cached frame.</summary>
        private int _frameNativeTime = -1;
        /// <summary>The chooser waiting state used to create the cached frame.</summary>
        private bool _frameWaitingForSeeds;

        /// <summary>Stores PC SeedChooserScreen's view-lawn counter, or minus one outside that state.</summary>
        private int _viewLawnTicks = -1;
        /// <summary>Retains the original chooser's three-pixel resting offset after returning from lawn view.</summary>
        private int _chooserRestY;
        /// <summary>Gets whether the survival re-selection is currently inspecting the planted lawn.</summary>
        public bool ViewingLawn => _viewLawnTicks >= 0;
        /// <summary>Gets the original 100/250/350-update lawn-view counter.</summary>
        public int ViewLawnTicks => _viewLawnTicks;
        /// <summary>Gets whether PC 499170 exposes the view-lawn button for this survival re-selection.</summary>
        public bool CanViewLawn => WaitingForSeeds && Plan.RequiredContext.SurvivalRepick && !ViewingLawn;

        /// <summary>Gets or sets complete.</summary>
        public bool Complete
        {
            get
            {
                return NativeTime >= Plan.End;
            }
        }

        /// <summary>Initializes adventure intro clock.</summary>
        /// <param name = "plan">The plan.</param>
        public AdventureIntroClock(AdventureIntroPlan plan)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (plan.NonScrolling && plan.DaveTime == 0)
            {
                NativeTime = plan.ChooseSeeds ? plan.ChooserOnEnd - 20 : plan.End - 20;
            }
        }

        /// <summary>Gets the current presentation snapshot, shared until the clock state changes.</summary>
        /// <remarks>The plan and returned frames are read-only inputs to the running presentation.</remarks>
        public AdventureIntroFrame Frame
        {
            get
            {
                if (_frame == null || _frameNativeTime != NativeTime || _frameWaitingForSeeds != WaitingForSeeds)
                {
                    _frame = AdventureIntroPresentation.Sample(Plan, NativeTime, WaitingForSeeds);
                    if (WaitingForSeeds)
                    {
                        _frame.ChooserY += _chooserRestY;
                    }
                    if (ViewingLawn)
                    {
                        var time = _viewLawnTicks;
                        var street = 1180 - Plan.RequiredContext.LogicalWidth;
                        _frame.BoardX = time <= 100 ? -AdventureIntroPresentation.Curve(0, 100, time, street, 0, 4)
                            : time <= 250 ? 0 : -AdventureIntroPresentation.Curve(250, 350, time, 0, street, 4);
                        _frame.ChooserY = time <= 100 ? AdventureIntroPresentation.Curve(0, 40, time, 3, 516, 4)
                            : time <= 250 ? 516 : AdventureIntroPresentation.Curve(310, 350, time, 516, 3, 4);
                        _frame.ChooserVisible = _frame.ChooserY < 516;
                    }
                    _frameNativeTime = NativeTime;
                    _frameWaitingForSeeds = WaitingForSeeds;
                }

                return _frame;
            }
        }

        /// <summary>Performs tick.</summary>
        /// <param name = "paused">The paused.</param>
        /// <param name = "dialogActive">The dialog active.</param>
        /// <param name = "shovelTutorialActive">The shovel tutorial active.</param>
        /// <param name = "emit">The emit.</param>
        public void Tick(bool paused = false, bool dialogActive = false, bool shovelTutorialActive = false, Action<AdventureIntroEvent>? emit = null)
        {
            if (Complete || paused || dialogActive || shovelTutorialActive)
            {
                return;
            }

            if (ViewingLawn)
            {
                // PC 49A8D0 holds at 250 until a click/key requests the return at 251.
                _viewLawnTicks++;
                if (_viewLawnTicks == 251)
                {
                    _viewLawnTicks = 250;
                }
                else if (_viewLawnTicks > 350)
                {
                    _viewLawnTicks = -1;
                    _chooserRestY = 3;
                }
                _frame = null;
                return;
            }
            if (WaitingForSeeds)
            {
                return;
            }

            var previous = NativeTime;
            NativeTime = Math.Min(Plan.End, NativeTime + AdventureIntroPresentation.NativeUpdate);
            // PC CutScene::AnimateBoard skips the street pan after Dave exits a non-scrolling level.
            if (Plan.NonScrolling && Plan.DaveTime > 0 && NativeTime == 4000)
            {
                NativeTime = Math.Min(Plan.End, Plan.PanLeftEnd);
            }

            if (Plan.ChooseSeeds && previous < Plan.ChooserOnEnd && NativeTime >= Plan.ChooserOnEnd)
            {
                NativeTime = Plan.ChooserOnEnd;
                WaitingForSeeds = true;
            }

            AdventureIntroPresentation.EventsBetween(Plan, previous, NativeTime, emit);
        }

        /// <summary>Starts PC 49B6D0 button 102's survival lawn-view state.</summary>
        /// <returns>Whether a visible re-selection entered the original lawn-view state.</returns>
        public bool BeginLawnView()
        {
            if (!CanViewLawn)
            {
                return false;
            }
            _viewLawnTicks = 0;
            _frame = null;
            return true;
        }

        /// <summary>Requests the PC 49C110/49C9B0 return only after the outbound pan finishes.</summary>
        public void CancelLawnView()
        {
            if (_viewLawnTicks > 100 && _viewLawnTicks <= 250)
            {
                _viewLawnTicks = 251;
                _frame = null;
            }
        }

        /// <summary>Restarts Dave's entrance after the level-five shovel lesson.</summary>
        public void RestartAfterShovelTutorial()
        {
            // PC Board::SetTutorialState(TUTORIAL_SHOVEL_COMPLETED) resets this time to 1500.
            NativeTime = 1500;
        }

        /// <summary>Performs begin return.</summary>
        public void BeginReturn()
        {
            if (!WaitingForSeeds || ViewingLawn)
            {
                return;
            }

            WaitingForSeeds = false;
            NativeTime = Plan.ChooserOnEnd + 10; // PC 0x49C955..0x49C967, successful seed chooser handoff.
        }
    }
}
