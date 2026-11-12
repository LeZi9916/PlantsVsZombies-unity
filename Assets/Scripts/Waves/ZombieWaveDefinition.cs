#nullable enable
using System;

namespace PlantsVsZombies.Waves
{
    /// <summary>Raw EXE selection fields for one numeric zombie ID.</summary>
    public struct ZombieWaveDefinition
    {
        /// <summary>Stores id.</summary>
        public readonly int Id;
        /// <summary>Stores budget cost.</summary>
        public readonly int BudgetCost;
        /// <summary>Stores level gate.</summary>
        public readonly int LevelGate;
        /// <summary>Stores wave gate.</summary>
        public readonly int WaveGate;
        /// <summary>Stores selection weight.</summary>
        public readonly int SelectionWeight;
        /// <summary>Initializes zombie wave definition.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "budgetCost">The budget cost.</param>
        /// <param name = "levelGate">The level gate.</param>
        /// <param name = "waveGate">The wave gate.</param>
        /// <param name = "selectionWeight">The selection weight.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public ZombieWaveDefinition(int id, int budgetCost, int levelGate, int waveGate, int selectionWeight)
        {
            if (id < 0 || id >= AdventureWaveRules.ZombieTypeCount)
            {
                throw new ArgumentOutOfRangeException("id");
            }

            if (budgetCost < 0)
            {
                throw new ArgumentOutOfRangeException("budgetCost");
            }

            if (levelGate < 0)
            {
                throw new ArgumentOutOfRangeException("levelGate");
            }

            if (waveGate < 0)
            {
                throw new ArgumentOutOfRangeException("waveGate");
            }

            if (selectionWeight < 0)
            {
                throw new ArgumentOutOfRangeException("selectionWeight");
            }

            Id = id;
            BudgetCost = budgetCost;
            LevelGate = levelGate;
            WaveGate = waveGate;
            SelectionWeight = selectionWeight;
        }
    }
}
