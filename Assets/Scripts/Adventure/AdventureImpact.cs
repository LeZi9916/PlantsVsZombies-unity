#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure impact.</summary>
    public sealed class AdventureImpact : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores native ice point.</summary>
        public bool NativeIcePoint;
        /// <summary>Stores native particle x, native particle y, native attached x, native attached y.</summary>
        public float NativeParticleX, NativeParticleY, NativeAttachedX, NativeAttachedY;
        /// <summary>Stores attached zombie.</summary>
        public Zombie? AttachedZombie;
        /// <summary>Stores id, seed.</summary>
        public int Id, Seed;
        /// <summary>Stores kind.</summary>
        public PlantKind Kind;
        /// <summary>Stores x, row, age.</summary>
        public float X, Row, Age;
        /// <summary>Stores snow, fire, butter, cob, mowed.</summary>
        public bool Snow, Fire, Butter, Cob, Mowed;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            NativeIcePoint = default;
            NativeParticleX = default;
            NativeParticleY = default;
            NativeAttachedX = default;
            NativeAttachedY = default;
            AttachedZombie = default;
            Id = default;
            Seed = default;
            Kind = default;
            X = default;
            Row = default;
            Age = default;
            Snow = default;
            Fire = default;
            Butter = default;
            Cob = default;
            Mowed = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
            pool.Mark(AttachedZombie);
        }

    }
}
