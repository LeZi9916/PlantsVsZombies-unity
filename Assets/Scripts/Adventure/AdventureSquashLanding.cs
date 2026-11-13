#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains original dust or splash artwork after a Squash reaches its actual landing.</summary>
    internal struct AdventureSquashLanding
    {
        /// <summary>Stores the stable presentation identity, board row, and elapsed native effect updates.</summary>
        internal int Id, Row, Ticks;
        /// <summary>Stores the native particle or splash reanimation origin.</summary>
        internal float X, Y;
        /// <summary>Stores whether the landing splashes instead of shaking the screen.</summary>
        internal bool Water;
    }
}
