#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MatchMode responsibilities for one deterministic match.</summary>
    internal sealed class MatchMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal MatchMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, IndependentModeRunner independentModeRunner, PlantingSystem plantingSystem, SunSystem sunSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _independentModeRunner = independentModeRunner;
            _plantingSystem = plantingSystem;
            _sunSystem = sunSystem;
        }

        /// <summary>Performs mode match kinds.</summary>
        /// <returns>The resulting mode match kinds.</returns>
        internal PlantKind[] ModeMatchKinds()
        {
            return new[]
            {
                _independentModeRunner.RequiredModeSession.MatchUpgrades[1] ? PlantKind.FumeShroom : PlantKind.PuffShroom,
                PlantKind.Starfruit,
                PlantKind.MagnetShroom,
                PlantKind.SnowPea,
                _independentModeRunner.RequiredModeSession.MatchUpgrades[2] ? PlantKind.TallNut : PlantKind.Wallnut,
                _independentModeRunner.RequiredModeSession.MatchUpgrades[0] ? PlantKind.Repeater : PlantKind.Peashooter
            };
        }

        /// <summary>Performs initialize mode match.</summary>
        internal void InitializeModeMatch()
        {
            _adventureModeRunner.Adventure._graves.Clear();
            _world.Plants.Clear();
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 8; c++)
                {
                    _plantingSystem.PlaceAdventurePlant(ModePickMatchSeed(r, c, false), r, c);
                }
            }

            _independentModeRunner.RequiredModeSession._matchDelay = 0.2f;
        }

        /// <summary>Performs mode pick match seed.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <param name = "allowMatches">The allow matches.</param>
        /// <returns>The resulting mode pick match seed.</returns>
        internal PlantKind ModePickMatchSeed(int row, int col, bool allowMatches)
        {
            var allowed = new List<PlantKind>();
            foreach (var kind in ModeMatchKinds())
            {
                if (!allowMatches && (col >= 2 && ModeMatchKindAt(row, col - 1) == kind && ModeMatchKindAt(row, col - 2) == kind || row >= 2 && ModeMatchKindAt(row - 1, col) == kind && ModeMatchKindAt(row - 2, col) == kind))
                {
                    continue;
                }

                allowed.Add(kind);
            }

            return allowed[_world._random.Next(allowed.Count)];
        }

        /// <summary>Performs mode match kind at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <returns>The resulting mode match kind at.</returns>
        internal PlantKind? ModeMatchKindAt(int row, int col)
        {
            var plant = _boardQueries.AdventurePlantAt(row, col, PlantLayer.Main);
            return plant == null ? (PlantKind? )null : plant.Kind;
        }

        /// <summary>Performs mode find matches.</summary>
        /// <param name = "groups">The groups.</param>
        /// <returns>The resulting mode find matches.</returns>
        internal HashSet<Plant> ModeFindMatches(out int groups)
        {
            groups = 0;
            var found = new HashSet<Plant>();
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 8;)
                {
                    var p = _boardQueries.AdventurePlantAt(r, c, PlantLayer.Main);
                    var end = c + 1;
                    if (p != null)
                    {
                        while (end < 8 && ModeMatchKindAt(r, end) == p.Kind)
                        {
                            end++;
                        }
                    }

                    if (p != null && end - c >= 3)
                    {
                        groups++;
                        for (var x = c; x < end; x++)
                        {
                            found.Add(_boardQueries.AdventurePlantAt(r, x, PlantLayer.Main));
                        }
                    }

                    c = end;
                }
            }

            for (var c = 0; c < 8; c++)
            {
                for (var r = 0; r < 5;)
                {
                    var p = _boardQueries.AdventurePlantAt(r, c, PlantLayer.Main);
                    var end = r + 1;
                    if (p != null)
                    {
                        while (end < 5 && ModeMatchKindAt(end, c) == p.Kind)
                        {
                            end++;
                        }
                    }

                    if (p != null && end - r >= 3)
                    {
                        groups++;
                        for (var y = r; y < end; y++)
                        {
                            found.Add(_boardQueries.AdventurePlantAt(y, c, PlantLayer.Main));
                        }
                    }

                    r = end;
                }
            }

            return found;
        }

        /// <summary>Performs click mode match cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting click mode match cell.</returns>
        internal bool ClickModeMatchCell(int row, int column)
        {
            var s = _world.ModeSession;
            if (s == null || !_independentModeRunner.IsModeMatch || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing || row < 0 || row >= 5 || column < 0 || column >= 8 || s._matchDelay > 0)
            {
                return false;
            }

            var a = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main);
            if (a == null)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.BeghouledTwist)
            {
                if (row >= 4 || column >= 7)
                {
                    return false;
                }

                var b = _boardQueries.AdventurePlantAt(row, column + 1, PlantLayer.Main);
                var c = _boardQueries.AdventurePlantAt(row + 1, column + 1, PlantLayer.Main);
                var d = _boardQueries.AdventurePlantAt(row + 1, column, PlantLayer.Main);
                if (b == null || c == null || d == null)
                {
                    return false;
                }

                a.Column++;
                b.Row++;
                c.Column--;
                d.Row--;
                s.SelectedCell = -1;
                ResolveModeMatch();
                s._matchDelay = 0.25f;
                return true;
            }

            var cell = row * 9 + column;
            if (s.SelectedCell < 0)
            {
                s.SelectedCell = cell;
                return true;
            }

            var old = s.SelectedCell;
            s.SelectedCell = -1;
            var other = _boardQueries.AdventurePlantAt(old / 9, old % 9, PlantLayer.Main);
            if (other == null || Math.Abs(other.Row - row) + Math.Abs(other.Column - column) != 1)
            {
                s.SelectedCell = cell;
                return true;
            }

            int r = other.Row, cx = other.Column;
            other.Row = row;
            other.Column = column;
            a.Row = r;
            a.Column = cx;
            int groups;
            ModeFindMatches(out groups);
            if (groups == 0)
            {
                a.Row = row;
                a.Column = column;
                other.Row = r;
                other.Column = cx;
                s.Advice = s.Score == 0 ? "[ADVICE_BEGHOULED_DRAG_TO_MATCH_3]" : "";
                return false;
            }

            ResolveModeMatch();
            s._matchDelay = 0.25f;
            return true;
        }

        /// <summary>Performs resolve mode match.</summary>
        /// <returns>The resulting resolve mode match.</returns>
        internal bool ResolveModeMatch()
        {
            int groups;
            var matched = ModeFindMatches(out groups);
            if (groups == 0)
            {
                return false;
            }

            _independentModeRunner.RequiredModeSession.Score += groups;
            _independentModeRunner.RequiredModeSession.SelectedCell = -1;
            foreach (var p in matched)
            {
                _world.Plants.Remove(p);
            }

            // PC score function 0x42C7D0 / .NET BeghouledScore: a group awards 1..5 sun objects.
            var suns = Math.Max(groups, Math.Min(groups * 5, matched.Count - groups * 2));
            for (var i = 0; i < suns; i++)
            {
                _world.Suns.Add(_sunSystem.CreateSun(2.5f + (float)_world._random.NextDouble() * 3f, 1f, 1.5f + (float)_world._random.NextDouble() * 2f));
            }

            for (var c = 0; c < 8; c++)
            {
                for (var r = 4; r >= 0; r--)
                {
                    if (_boardQueries.AdventurePlantAt(r, c, PlantLayer.Main) != null || _adventureModeRunner.Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                    {
                        continue;
                    }

                    Plant? falling = null;
                    for (var above = r - 1; above >= 0; above--)
                    {
                        if (_adventureModeRunner.Adventure._craters.Exists(x => x.Row == above && x.Column == c))
                        {
                            break;
                        }

                        falling = _boardQueries.AdventurePlantAt(above, c, PlantLayer.Main);
                        if (falling != null)
                        {
                            break;
                        }
                    }

                    if (falling != null)
                    {
                        falling.Row = r;
                    }
                    else
                    {
                        _plantingSystem.PlaceAdventurePlant(ModePickMatchSeed(r, c, true), r, c);
                    }
                }
            }

            return true;
        }

        /// <summary>Performs tick mode match.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeMatch(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            s._matchDelay = Math.Max(0, s._matchDelay - dt);
            if (s._matchDelay <= 0 && ResolveModeMatch())
            {
                s._matchDelay = 0.25f;
            }

            if (s._matchDelay <= 0)
            {
                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 8; c++)
                    {
                        if (_boardQueries.AdventurePlantAt(r, c, PlantLayer.Main) == null && !_adventureModeRunner.Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                        {
                            _adventureModeRunner.Adventure._craters.Add(new AdventureCellTimer { Row = r, Column = c, Remaining = float.MaxValue });
                        }
                    }
                }
            }
        }

        /// <param name = "action">0 repeater, 1 fume-shroom, 2 tall-nut, 3 shuffle, 4 fill a crater.</param>
        internal bool BuyModeMatchAction(int action)
        {
            var s = _world.ModeSession;
            if (s == null || !_independentModeRunner.IsModeMatch || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing || action < 0 || action > 4 || s._matchDelay > 0)
            {
                return false;
            }

            int[] costs =
            {
                1000,
                500,
                250,
                100,
                200
            };
            if (_world.SunAmount < costs[action] || action < 3 && s.MatchUpgrades[action])
            {
                return false;
            }

            if (action == 4 && _adventureModeRunner.Adventure._craters.Count == 0)
            {
                return false;
            }

            _world.SunAmount -= costs[action];
            if (action < 3)
            {
                s.MatchUpgrades[action] = true;
                PlantKind[] from =
                {
                    PlantKind.Peashooter,
                    PlantKind.PuffShroom,
                    PlantKind.Wallnut
                };
                PlantKind[] to =
                {
                    PlantKind.Repeater,
                    PlantKind.FumeShroom,
                    PlantKind.TallNut
                };
                foreach (var p in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (p.Kind == from[action])
                    {
                        _world.Plants.Remove(p);
                        _plantingSystem.PlaceAdventurePlant(to[action], p.Row, p.Column);
                    }
                }
            }
            else if (action == 3)
            {
                foreach (var p in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (p.Column < 8)
                    {
                        _world.Plants.Remove(p);
                    }
                }

                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 8; c++)
                    {
                        if (!_adventureModeRunner.Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                        {
                            _plantingSystem.PlaceAdventurePlant(ModePickMatchSeed(r, c, false), r, c);
                        }
                    }
                }
            }
            else
            {
                var crater = _adventureModeRunner.Adventure._craters[0];
                _adventureModeRunner.Adventure._craters.RemoveAt(0);
                _plantingSystem.PlaceAdventurePlant(ModePickMatchSeed(crater.Row, crater.Column, true), crater.Row, crater.Column);
            }

            s._matchDelay = 0.25f;
            return true;
        }
    }
}
