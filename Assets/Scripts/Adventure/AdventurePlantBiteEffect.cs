#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure plant bite effect.</summary>
    public sealed class AdventurePlantBiteEffect : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores id, particle id, row, seed.</summary>
        public int Id, ParticleId, Row, Seed;
        /// <summary>Stores native x, native y, age, offset x, offset y.</summary>
        public float NativeX, NativeY, Age, OffsetX, OffsetY;
        /// <summary>Stores kind.</summary>
        public ZombieKind Kind;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            ParticleId = default;
            Row = default;
            Seed = default;
            NativeX = default;
            NativeY = default;
            Age = default;
            OffsetX = default;
            OffsetY = default;
            Kind = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
