#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure jalapeno fire.</summary>
    public sealed class AdventureJalapenoFire
    {
        /// <summary>Stores id, row, ticks.</summary>
        public int Id, Row, Ticks;
        /// <summary>Stores flames.</summary>
        public readonly List<AdventureJalapenoFlame> Flames = new List<AdventureJalapenoFlame>();
    }
}
