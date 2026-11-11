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
        public void Tick(bool paused = false, bool dialogActive = false, bool shovelTutorialActive = false, Action<AdventureIntroEvent> emit = null)
        {
            if (Complete || paused || dialogActive || shovelTutorialActive || WaitingForSeeds)
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

        /// <summary>Performs begin return.</summary>
        public void BeginReturn()
        {
            if (!WaitingForSeeds)
            {
                return;
            }

            WaitingForSeeds = false;
            NativeTime = Plan.ChooserOnEnd + 10; // PC 0x49C955..0x49C967, successful seed chooser handoff.
        }
    }
}
