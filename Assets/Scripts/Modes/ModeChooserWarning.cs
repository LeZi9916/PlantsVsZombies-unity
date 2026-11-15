#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Modes
{
    /// <summary>Describes one PC 49B120/49AF50 re-pick confirmation without changing its native ordering.</summary>
    internal readonly struct ModeChooserWarning
    {
        /// <summary>Gets the unchanged original warning key.</summary>
        internal string Key { get; }
        /// <summary>Gets the optional upgrade whose supporting plant is missing.</summary>
        internal PlantKind? Upgrade { get; }
        /// <summary>Gets the optional supporting plant named by the original upgrade warning.</summary>
        internal PlantKind? Base { get; }

        /// <summary>Creates an original localized warning request.</summary>
        /// <param name="key">The LawnStrings key.</param>
        /// <param name="upgrade">The optional selected upgrade.</param>
        /// <param name="basis">The optional missing supporting plant.</param>
        internal ModeChooserWarning(string key, PlantKind? upgrade = null, PlantKind? basis = null)
        {
            Key = key;
            Upgrade = upgrade;
            Base = basis;
        }
    }
}
