#nullable enable

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>A stored zombie-matrix row header, identified by its lookup index.</summary>
    public sealed class AdventureZombieRowHeader
    {
        /// <summary>Creates a verified raw zombie-matrix row header.</summary>
        /// <param name = "zombieIdIndex">The zero-based original zombie identifier.</param>
        /// <param name = "storedHeaderRaw">The uninterpreted original matrix row header.</param>
        internal AdventureZombieRowHeader(int zombieIdIndex, int storedHeaderRaw)
        {
            ZombieIdIndex = zombieIdIndex;
            StoredHeaderRaw = storedHeaderRaw;
        }

        /// <summary>Gets the zero-based zombie identifier used to locate this matrix row.</summary>
        public int ZombieIdIndex { get; }
        /// <summary>Gets the stored row-header integer without interpreting its gameplay meaning.</summary>
        public int StoredHeaderRaw { get; }
    }
}
