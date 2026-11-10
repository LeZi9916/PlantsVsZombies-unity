#nullable enable
using System;

namespace PlantsVsZombies.Waves
{
    /// <summary>Defines weighted zombie.</summary>
    public struct WeightedZombie
    {
        /// <summary>Stores zombie id.</summary>
        public readonly int ZombieId;
        /// <summary>Stores weight.</summary>
        public readonly int Weight;
        /// <summary>Initializes weighted zombie.</summary>
        /// <param name = "zombieId">The zombie id.</param>
        /// <param name = "weight">The weight.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public WeightedZombie(int zombieId, int weight)
        {
            if (zombieId < 0 || zombieId >= AdventureWaveRules.ZombieTypeCount)
            {
                throw new ArgumentOutOfRangeException("zombieId");
            }

            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException("weight");
            }

            ZombieId = zombieId;
            Weight = weight;
        }
    }
}
