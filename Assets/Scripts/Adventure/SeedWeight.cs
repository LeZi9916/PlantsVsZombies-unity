#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines seed weight.</summary>
    public struct SeedWeight
    {
        /// <summary>Stores kind.</summary>
        public readonly PlantKind Kind;
        /// <summary>Stores weight.</summary>
        public readonly int Weight;
        /// <summary>Initializes seed weight.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "weight">The weight.</param>
        public SeedWeight(PlantKind kind, int weight)
        {
            Kind = kind;
            Weight = weight;
        }
    }
}
