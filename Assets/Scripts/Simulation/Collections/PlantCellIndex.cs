#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Indexes plant footprints while preserving the ordered candidate sequence.</summary>
    internal sealed class PlantCellIndex
    {
        /// <summary>Owns the indexed plants.</summary>
        private readonly EntityStore<Plant> _plants;
        /// <summary>Retains candidate lists for all supported board cells.</summary>
        private readonly List<Plant>[] _cells = new List<Plant>[6 * 9];
        /// <summary>Represents queries outside the board.</summary>
        private readonly List<Plant> _empty = new List<Plant>();
        /// <summary>Records the last synchronized store version.</summary>
        private int _version = -1;
        /// <summary>Creates empty cell buffers once per match.</summary>
        /// <param name="plants">The authoritative ordered plant owner.</param>
        internal PlantCellIndex(EntityStore<Plant> plants)
        {
            _plants = plants;
            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new List<Plant>(4);
            }
        }
        /// <summary>Returns footprint candidates, including every layer and both cannon cells.</summary>
        /// <param name="row">The zero-based row.</param>
        /// <param name="column">The zero-based column.</param>
        /// <returns>The ordered candidates; callers apply dynamic layer and airborne predicates.</returns>
        internal List<Plant> At(int row, int column)
        {
            if (row < 0 || row >= 6 || column < 0 || column >= 9)
            {
                return _empty;
            }
            if (_version != _plants.Version)
            {
                foreach (var cell in _cells)
                {
                    cell.Clear();
                }
                foreach (var plant in _plants)
                {
                    if (plant.Row < 0 || plant.Row >= 6)
                    {
                        continue;
                    }
                    var end = Math.Min(9, plant.Column + plant.ColumnSpan);
                    for (var x = Math.Max(0, plant.Column); x < end; x++)
                    {
                        _cells[plant.Row * 9 + x].Add(plant);
                    }
                }
                _version = _plants.Version;
            }
            return _cells[row * 9 + column];
        }
    }
}
