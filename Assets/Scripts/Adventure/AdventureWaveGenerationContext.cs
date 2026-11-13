#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>
    /// Supplies native wave-generation state without inferring its lifetime from unrelated progress fields.
    /// The default represents a zero application byte at PC offset +0x9B0.
    /// </summary>
    public readonly struct AdventureWaveGenerationContext
    {
        /// <summary>Gets whether a nonzero native application byte at +0x9B0 suppresses forced yeti insertion.</summary>
        public bool SuppressForcedYeti { get; }

        /// <summary>Creates the explicit snapshot used by the original forced-newcomer branch.</summary>
        /// <param name="suppressForcedYeti">Whether the caller observed a nonzero native application byte at +0x9B0.</param>
        public AdventureWaveGenerationContext(bool suppressForcedYeti)
        {
            SuppressForcedYeti = suppressForcedYeti;
        }
    }
}
