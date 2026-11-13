#nullable enable
using System;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Tracks PC Board fog updates (0x4260B0), clearing (0x425F10) and stage depth (0x427DD0).</summary>
    internal sealed class AdventureFogState
    {
        /// <summary>Stores the nine columns and seven overlapping fog rows from Board::DrawFog.</summary>
        private readonly int[,] _opacity = new int[9, 7];
        /// <summary>Stores the original decorative cell choices, shared with the seventh fog row.</summary>
        private readonly int[,] _look = new int[9, 6];

        /// <summary>Gets the horizontal displacement that rolls the entire fog grid off the lawn.</summary>
        internal float Offset { get; private set; }

        /// <summary>Initializes the original decorative cell distribution without consuming combat randomness.</summary>
        /// <param name="random">The independently seeded presentation random source.</param>
        internal void InitializeLooks(Random random)
        {
            for (var column = 0; column < 9; column++)
            {
                for (var row = 0; row < 6; row++)
                {
                    _look[column, row] = random.Next(20);
                    // Board initializes the two cell jitter values between adjacent look draws.
                    random.Next(10);
                    random.Next(10);
                }
            }
        }

        /// <summary>Returns the original left fog column for adventure and independent fog boards.</summary>
        /// <param name="level">The adventure level number.</param>
        /// <param name="independent">Whether this is an independent challenge or survival board.</param>
        /// <param name="airRaid">Whether this is the original Air Raid challenge.</param>
        /// <returns>The first of nine original fog columns.</returns>
        internal static int LeftColumn(int level, bool independent = false, bool airRaid = false)
        {
            return airRaid || !independent && level == 31 ? 6 : independent || level <= 36 ? 5 : 4;
        }

        /// <summary>Starts the level preview with the original fog grid displaced entirely to the right.</summary>
        /// <param name="leftColumn">The first fog column for this stage.</param>
        internal void BeginIntro(int leftColumn)
        {
            Offset = 1065f - leftColumn * 80f;
        }

        /// <summary>Advances one native fog tick, including gradual light clearing and the returning fog curve.</summary>
        /// <param name="leftColumn">The first fog column for this stage.</param>
        /// <param name="blownRemaining">The native fog countdown in seconds before this tick is decremented.</param>
        /// <param name="intro">Whether the original two-second opening curve replaces the twenty-second return.</param>
        /// <param name="plants">The current live plants that can clear fog.</param>
        internal void Advance(int leftColumn, float blownRemaining, bool intro, IReadOnlyList<Plant> plants)
        {
            var maximumOffset = 1065f - leftColumn * 80f;
            if (blownRemaining <= 0f)
            {
                Offset = 0f;
            }
            else if (intro || blownRemaining < 20f)
            {
                var fraction = Math.Min(1f, blownRemaining / (intro ? 2f : 20f));
                Offset = maximumOffset * fraction * fraction;
            }
            else if (Offset < maximumOffset)
            {
                // Original TodAnimateCurveFloat(-5, maximumOffset, int(offset * 1.1), 0, maximumOffset, Linear).
                Offset = maximumOffset * Math.Min(1f, ((int)(Offset * 1.1f) + 5f) / (maximumOffset + 5f));
            }

            var returning = blownRemaining > 0f && blownRemaining < 20f;
            var recovery = returning ? 1 : blownRemaining > 0f ? 20 : 3;
            for (var column = leftColumn; column < 9; column++)
            {
                var maximum = column == leftColumn ? 200 : 255;
                for (var row = 0; row < 7; row++)
                {
                    _opacity[column, row] = Math.Min(maximum, _opacity[column, row] + recovery);
                }
            }

            var clearing = returning ? 2 : blownRemaining > 0f ? 40 : 6;
            for (var index = 0; index < plants.Count; index++)
            {
                var plant = plants[index];
                if (plant.Health > 0f && !plant.PresentationBiteNotOnGround && (plant.Kind == PlantKind.Plantern || plant.Kind == PlantKind.Torchwood))
                {
                    Clear(plant, leftColumn, plant.Kind == PlantKind.Plantern ? 4 : 1, clearing);
                }
            }
        }

        /// <summary>Clears the original rounded Plantern footprint or Torchwood's five-cell cross.</summary>
        /// <param name="plant">The living light source.</param>
        /// <param name="leftColumn">The first fog column for this stage.</param>
        /// <param name="size">The original Plantern or Torchwood clearing size.</param>
        /// <param name="amount">The opacity removed during this native tick.</param>
        private void Clear(Plant plant, int leftColumn, int size, int amount)
        {
            for (var column = plant.Column - size; column <= plant.Column + size; column++)
            {
                var shiftedColumn = column - ((int)Offset + 50) / 100;
                for (var row = plant.Row - size; row <= plant.Row + size; row++)
                {
                    if (shiftedColumn < leftColumn || shiftedColumn >= 9 || row < 0 || row >= 7)
                    {
                        continue;
                    }

                    var dx = Math.Abs(column - plant.Column);
                    var dy = Math.Abs(row - plant.Row);
                    if (size == 4 ? dx > 3 || dy > 2 || dx + dy == 5 : dx + dy > size)
                    {
                        continue;
                    }

                    _opacity[shiftedColumn, row] = Math.Max(0, _opacity[shiftedColumn, row] - amount);
                }
            }
        }

        /// <summary>Gets the native opacity of an overlapping fog tile.</summary>
        /// <param name="column">The zero-based fog column.</param>
        /// <param name="row">The zero-based fog row, including the seventh edge row.</param>
        /// <returns>The native opacity from zero through 255.</returns>
        internal int Opacity(int column, int row)
        {
            return _opacity[column, row];
        }

        /// <summary>Gets the native cell decoration used for atlas selection and tint.</summary>
        /// <param name="column">The zero-based fog column.</param>
        /// <param name="row">The zero-based fog row.</param>
        /// <returns>The original random look from zero through nineteen.</returns>
        internal int Look(int column, int row)
        {
            return _look[column, row % 6];
        }
    }
}
