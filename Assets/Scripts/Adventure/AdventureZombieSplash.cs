#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains an original pool splash and its independently positioned planting particles.</summary>
    public sealed class AdventureZombieSplash : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Identifies the effect instance and its lawn row.</summary>
        public int Id, Row;
        /// <summary>Stores the original splash and particle origins in board pixels.</summary>
        public float X, Y, ParticleX, ParticleY;
        /// <summary>Stores the original nonuniform splash scale and elapsed time.</summary>
        public float ScaleX, ScaleY, Age;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Row = default;
            X = default;
            Y = default;
            ParticleX = default;
            ParticleY = default;
            ScaleX = default;
            ScaleY = default;
            Age = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
