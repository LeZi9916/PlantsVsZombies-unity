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

        /// <summary>Gets or sets frame.</summary>
        public AdventureIntroFrame Frame
        {
            get
            {
                return AdventureIntroPresentation.Sample(Plan, NativeTime, WaitingForSeeds);
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
