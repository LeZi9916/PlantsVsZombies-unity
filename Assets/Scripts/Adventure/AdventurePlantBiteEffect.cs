#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure plant bite effect.</summary>
    public sealed class AdventurePlantBiteEffect
    {
        /// <summary>Stores id, particle id, row, seed.</summary>
        public int Id, ParticleId, Row, Seed;
        /// <summary>Stores native x, native y, age, offset x, offset y.</summary>
        public float NativeX, NativeY, Age, OffsetX, OffsetY;
        /// <summary>Stores kind.</summary>
        public ZombieKind Kind;
    }
}
