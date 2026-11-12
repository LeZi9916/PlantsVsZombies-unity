#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original five-tick delay from Squash damage to ground or water impact.</summary>
    internal struct AdventureSquashLanding
    {
        /// <summary>Stores native ticks remaining until landing.</summary>
        internal int Ticks;
        /// <summary>Stores whether the landing splashes instead of shaking the screen.</summary>
        internal bool Water;
    }
}
