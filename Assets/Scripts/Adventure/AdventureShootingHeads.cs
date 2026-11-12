#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Identifies the active shooting heads of a Threepeater or Split Pea.</summary>
    [Flags]
    public enum AdventureShootingHeads
    {
        /// <summary>No head is shooting.</summary>
        None = 0,
        /// <summary>The lower Threepeater head or forward Split Pea head.</summary>
        First = 1,
        /// <summary>The middle Threepeater head or rear Split Pea head.</summary>
        Second = 2,
        /// <summary>The upper Threepeater head.</summary>
        Third = 4
    }
}
