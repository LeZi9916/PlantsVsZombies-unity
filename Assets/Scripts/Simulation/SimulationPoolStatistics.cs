#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides an allocation-free snapshot of match-local pool activity.</summary>
    internal readonly struct SimulationPoolStatistics
    {
        /// <summary>Gets the total model records constructed by the runtime pools.</summary>
        internal int CreatedCount { get; }
        /// <summary>Gets records still protected by live or delayed references.</summary>
        internal int ActiveCount { get; }
        /// <summary>Gets completed record returns after reference retirement.</summary>
        internal long RecycledCount { get; }
        /// <summary>Gets runtime requests that exhausted prepared inactive records.</summary>
        internal long MissCount { get; }
        /// <summary>Creates a value snapshot without enumerating record contents.</summary>
        /// <param name="createdCount">The total constructed records.</param>
        /// <param name="activeCount">The outstanding rented records.</param>
        /// <param name="recycledCount">The successfully retired records.</param>
        /// <param name="missCount">The unprepared rent requests, excluding explicit preparation.</param>
        internal SimulationPoolStatistics(int createdCount, int activeCount, long recycledCount, long missCount)
        {
            CreatedCount = createdCount;
            ActiveCount = activeCount;
            RecycledCount = recycledCount;
            MissCount = missCount;
        }
    }
}
