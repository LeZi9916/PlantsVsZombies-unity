#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure snow trail.</summary>
    public sealed class AdventureSnowTrail : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores id, seed, row.</summary>
        public int Id, Seed, Row;
        /// <summary>Stores age, x, y, previous x, previous y, stop age.</summary>
        public float Age, X, Y, PreviousX, PreviousY, StopAge = -1f;
        /// <summary>Stores projectile.</summary>
        public Pea? Projectile;
        /// <summary>Gets or sets stopped.</summary>
        public bool Stopped
        {
            get
            {
                return StopAge >= 0f;
            }
        }
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Seed = default;
            Row = default;
            Age = default;
            X = default;
            Y = default;
            PreviousX = default;
            PreviousY = default;
            StopAge = -1f;
            Projectile = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
