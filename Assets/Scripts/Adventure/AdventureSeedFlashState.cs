#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed flash state.</summary>
    public sealed class AdventureSeedFlashState : ISimulationPooledObject
    {
        /// <summary>Stores id.</summary>
        public int Id;
        /// <summary>Stores kind.</summary>
        public PlantKind Kind;
        /// <summary>Stores age.</summary>
        public float Age;
        /// <summary>Restores construction defaults after every board and presentation owner releases this flash.</summary>
        void ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Kind = default;
            Age = default;
        }

        /// <summary>Declares that this flash owns no additional model records.</summary>
        /// <param name="pool">The match-local lifetime tracker.</param>
        void ISimulationPooledObject.MarkPoolReferences(SimulationRuntimePool pool)
        {
        }
    }
}
