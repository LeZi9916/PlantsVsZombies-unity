#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves PC 49D260/4296E0 upgrade packet artwork and survival-endless pricing eligibility.</summary>
    internal static class AdventureUpgradeSeedRules
    {
        /// <summary>Checks the original eight purple upgrade seed types.</summary>
        /// <param name="kind">The packet type before imitation resolution.</param>
        /// <returns>Whether the seed is an original upgrade.</returns>
        internal static bool IsUpgrade(PlantKind kind)
        {
            return kind >= PlantKind.GatlingPea && kind <= PlantKind.CobCannon;
        }

        /// <summary>Selects the original nine-cell seed packet background.</summary>
        /// <param name="kind">The original packet type.</param>
        /// <returns>Zero for Imitater, one for upgrades, or two for ordinary plants.</returns>
        internal static int PacketFrame(PlantKind kind)
        {
            return kind == PlantKind.Imitater ? 0 : IsUpgrade(kind) ? 1 : 2;
        }

        /// <summary>Checks PC Board::PlantUsesAcceleratedPricing without including endless puzzle modes.</summary>
        /// <param name="kind">The original packet type.</param>
        /// <param name="nativeMode">The original native mode identifier.</param>
        /// <returns>Whether currently planted copies increase this seed's price.</returns>
        internal static bool UsesAcceleratedPricing(PlantKind kind, int nativeMode)
        {
            return IsUpgrade(kind) && nativeMode >= 11 && nativeMode <= 15;
        }
    }
}
