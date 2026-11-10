#nullable enable
using PlantsVsZombies.Simulation;

using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure wave.</summary>
    public sealed class AdventureWave
    {
        /// <summary>Gets or sets index.</summary>
        public int Index { get; internal set; }
        /// <summary>Gets or sets is flag.</summary>
        public bool IsFlag { get; internal set; }
        /// <summary>Gets or sets zombies.</summary>
        public IReadOnlyList<ZombieKind> Zombies { get; internal set; }
    }
}
