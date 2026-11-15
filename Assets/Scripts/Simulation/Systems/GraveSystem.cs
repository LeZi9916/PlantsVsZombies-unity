#nullable enable
using PlantsVsZombies.Adventure;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns GraveSystem responsibilities for one deterministic match.</summary>
    internal sealed class GraveSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal GraveSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, PlantingSystem plantingSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _plantingSystem = plantingSystem;
        }

        /// <summary>Performs initialize adventure graves.</summary>
        internal void InitializeAdventureGraves()
        {
            var n = _adventureModeRunner.Adventure.Level.Number;
            if (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                WhackPlaceGraves(9);
                PickAdventureSpecialGrave();
                return;
            }

            if (n <= 13)
            {
                AddGravesInColumn(6, 1);
                AddGravesInColumn(7, 1);
                AddGravesInColumn(8, 2);
            }
            else if (n == 14 || n == 16 || n == 18)
            {
                AddGravesInColumn(5, 1);
                AddGravesInColumn(6, 1);
                AddGravesInColumn(7, 2);
                AddGravesInColumn(8, 3);
            }
            else if (n < 20)
            {
                AddGravesInColumn(4, 1);
                AddGravesInColumn(5, 2);
                AddGravesInColumn(6, 2);
                AddGravesInColumn(7, 3);
                AddGravesInColumn(8, 3);
            }
            else
            {
                AddGravesInColumn(3, 1);
                AddGravesInColumn(4, 2);
                AddGravesInColumn(5, 2);
                AddGravesInColumn(6, 2);
                AddGravesInColumn(7, 3);
                AddGravesInColumn(8, 3);
            }

            PickAdventureSpecialGrave();
        }

        /// <summary>Places the original Whack-a-Zombie graves using its weighted empty-cell picker.</summary>
        /// <param name="count">The number of graves to place.</param>
        internal void WhackPlaceGraves(int count)
        {
            var cells = new List<(int Row, int Column, int Weight)>();
            for (var column = 3; column < SimulationWorld.Columns; column++)
            {
                for (var row = 0; row < _world.RowCount; row++)
                {
                    if (_adventureModeRunner.Adventure._graves.Exists(grave => grave.Row == row && grave.Column == column))
                    {
                        continue;
                    }

                    var occupied = _world.Plants.Exists(plant => plant.Row == row && plant.Column == column);
                    cells.Add((row, column, occupied ? 1 : 100000));
                }
            }

            count = Math.Min(count, cells.Count);
            for (var i = 0; i < count; i++)
            {
                var total = 0;
                foreach (var candidate in cells)
                {
                    total += candidate.Weight;
                }

                var pick = _world._random.Next(total);
                var selected = 0;
                for (var index = 0; index < cells.Count; index++)
                {
                    pick -= cells[index].Weight;
                    if (pick < 0)
                    {
                        selected = index;
                        break;
                    }
                }

                var cell = cells[selected];
                cells.RemoveAt(selected);
                foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (plant.Row == cell.Row && plant.Column == cell.Column)
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(plant);
                    }
                }

                _adventureModeRunner.Adventure._graves.Add(CreateAdventureGrave(cell.Row, cell.Column));
            }
        }

        /// <summary>Marks one initial grave with the original special atlas row.</summary>
        internal void PickAdventureSpecialGrave()
        {
            if (_adventureModeRunner.Adventure._graves.Count > 0)
            {
                var appearance = _adventureModeRunner.Adventure._graves[_world._random.Next(_adventureModeRunner.Adventure._graves.Count)].GravePresentation;
                if (appearance != null)
                {
                    appearance.Special = true;
                }
            }
        }

        /// <summary>Performs add graves in column.</summary>
        /// <param name = "column">The column.</param>
        /// <param name = "count">The count.</param>
        internal void AddGravesInColumn(int column, int count)
        {
            for (var i = 0; i < count; i++)
            {
                using var rowsLease = _world.IntegerScratch.Rent();
                var rows = rowsLease.Items;
                for (var r = 0; r < 5; r++)
                {
                    if (!_adventureModeRunner.Adventure._graves.Exists(g => g.Column == column && g.Row == r))
                    {
                        rows.Add(r);
                    }
                }

                if (rows.Count > 0)
                {
                    _adventureModeRunner.Adventure._graves.Add(CreateAdventureGrave(rows[_world._random.Next(rows.Count)], column));
                }
            }
        }

        /// <summary>Creates a grave with original atlas selection, jitter, and staggered emergence.</summary>
        /// <param name="row">The lawn row.</param>
        /// <param name="column">The lawn column.</param>
        /// <returns>A grave using original artwork parameters.</returns>
        internal AdventureCellTimer CreateAdventureGrave(int row, int column)
        {
            return new AdventureCellTimer
            {
                Row = row,
                Column = column,
                Remaining = float.MaxValue,
                GravePresentation = _world._boardCells.CreateGraveAppearance(column, row, _world._random.Next(50))
            };
        }

        /// <summary>Advances the original grid-item emergence counter during introduction or battle.</summary>
        internal void AdvanceAdventureGraves()
        {
            if (!_world.IsAdventure || !_adventureModeRunner.Adventure.GravesEnabled)
            {
                return;
            }

            foreach (var grave in _adventureModeRunner.Adventure._graves)
            {
                grave.GravePresentation?.Tick();
            }
        }
    }
}
