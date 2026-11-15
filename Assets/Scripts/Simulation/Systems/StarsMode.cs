#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns StarsMode responsibilities for one deterministic match.</summary>
    internal sealed class StarsMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal StarsMode(SimulationWorld world)
        {
            _world = world;
        }

        /// <summary>Performs is mode star cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <returns>The resulting is mode star cell.</returns>
        internal static bool IsModeStarCell(int row, int col)
        {
            return row == 0 && col == 3 || row == 1 && (col == 3 || col == 4) || row == 2 && col >= 1 && col <= 6 || row == 3 && col >= 3 && col <= 5 || row == 4 && (col == 3 || col == 6);
        }

        /// <summary>Performs mode star count.</summary>
        /// <returns>The resulting mode star count.</returns>
        internal int ModeStarCount()
        {
            var n = 0;
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    if (IsModeStarCell(r, c) && _world.Plants.Exists(p => p.Row == r && p.Column == c && p.Kind == PlantKind.Starfruit))
                    {
                        n++;
                    }
                }
            }

            return n;
        }
    }
}
