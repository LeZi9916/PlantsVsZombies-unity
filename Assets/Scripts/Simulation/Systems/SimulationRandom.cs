#nullable enable

using PlantsVsZombies.Randomness;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SimulationRandom responsibilities for one deterministic match.</summary>
    internal sealed class SimulationRandom
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SimulationRandom(SimulationWorld world)
        {
            _world = world;
        }

        /// <summary>Gets or sets battle random draw count.</summary>
        internal ulong BattleRandomDrawCount
        {
            get
            {
                var original = _world._random as OriginalRandomAdapter;
                return original == null ? 0 : original.Generator.DrawCount;
            }
        }

        /// <summary>Performs capture battle random state.</summary>
        /// <returns>The resulting capture battle random state.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal OriginalRandomState CaptureBattleRandomState()
        {
            var original = _world._random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            return original.CaptureState();
        }

        /// <summary>Performs restore battle random state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void RestoreBattleRandomState(OriginalRandomState snapshot)
        {
            var original = _world._random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            original.RestoreState(snapshot);
        }
    }
}
