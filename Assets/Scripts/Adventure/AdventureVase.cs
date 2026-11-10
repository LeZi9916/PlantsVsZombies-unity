#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure vase.</summary>
    public sealed class AdventureVase
    {
        /// <summary>Gets or sets row.</summary>
        public int Row { get; internal set; }
        /// <summary>Gets or sets column.</summary>
        public int Column { get; internal set; }
        /// <summary>Gets or sets shows plant.</summary>
        public bool ShowsPlant { get; internal set; }

        // Contents are only exposed to the view after smashing. Tests can inspect via internal assembly access.
        /// <summary>Stores seed.</summary>
        internal PlantKind? _seed;
        /// <summary>Stores zombie.</summary>
        internal ZombieKind? _zombie;
    }
}
