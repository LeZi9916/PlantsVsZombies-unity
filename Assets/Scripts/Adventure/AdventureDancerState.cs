#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original dancer's phase counters and four independently owned backup slots.</summary>
    public sealed class AdventureDancerState
    {
        /// <summary>Identifies the summoning leader, or zero for an independent dancer.</summary>
        public int LeaderId;

        /// <summary>Stores the original upper, lower, left, and right backup identifiers.</summary>
        public readonly int[] Followers = new int[4];

        /// <summary>Counts native updates remaining in the entrance, pointing hold, or emergence.</summary>
        public int PhaseTicks;

        /// <summary>Counts updates until a missing backup can be summoned on dance frame twelve.</summary>
        public int SummonTicks;

        /// <summary>Tracks the current phase's unpaused animation time in seconds.</summary>
        public double PhaseAge;
    }
}
