#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC Plant::Squish's cached plant picture after logical occupancy ends.</summary>
    public sealed class AdventureSquishedPlant : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the removed plant whose original kind and cell determine the cached picture.</summary>
        public Plant Plant = null!;
        /// <summary>Stores the native disappear countdown, removed only after it becomes negative.</summary>
        public int Remaining = 500;

        /// <summary>Creates an unpublished record for match-local preparation or checkpoint restoration.</summary>
        public AdventureSquishedPlant()
        {
        }

        /// <summary>Retains a crushed plant without restarting any live plant animation.</summary>
        /// <param name="plant">The plant removed from active lawn occupancy.</param>
        public AdventureSquishedPlant(Plant plant)
        {
            Plant = plant;
        }
        /// <summary>Restores construction defaults after all authoritative owners release this record.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Plant = null!;
            Remaining = 500;
        }

        /// <summary>Retains linked model records until this effect leaves every authoritative owner.</summary>
        /// <param name="pool">The match-local lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
            pool.Mark(Plant);
        }
    }
}
