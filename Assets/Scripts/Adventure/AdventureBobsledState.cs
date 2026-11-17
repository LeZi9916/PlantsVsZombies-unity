#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains one member's native sled position and boarding or crash countdown.</summary>
    public sealed class AdventureBobsledState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Identifies the front zombie whose helmet health belongs to the whole sled.</summary>
        public int LeaderId;
        /// <summary>Stores the original front-to-back member index, from zero through three.</summary>
        public int Position;
        /// <summary>Stores the original five-hundred-update push or one-hundred-fifty-update crash countdown.</summary>
        public int PhaseTicks = 500;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            LeaderId = default;
            Position = default;
            PhaseTicks = 500;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
