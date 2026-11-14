#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC Plant::Squish's cached plant picture after logical occupancy ends.</summary>
    public sealed class AdventureSquishedPlant
    {
        /// <summary>Stores the removed plant whose original kind and cell determine the cached picture.</summary>
        public Plant Plant;
        /// <summary>Stores the native disappear countdown, removed only after it becomes negative.</summary>
        public int Remaining = 500;

        /// <summary>Retains a crushed plant without restarting any live plant animation.</summary>
        /// <param name="plant">The plant removed from active lawn occupancy.</param>
        public AdventureSquishedPlant(Plant plant)
        {
            Plant = plant;
        }
    }
}
