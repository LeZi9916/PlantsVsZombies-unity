#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns IZombieMode responsibilities for one deterministic match.</summary>
    internal sealed class IZombieMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal IZombieMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, IndependentModeRunner independentModeRunner, LegacySimulation legacySimulation, PlantingSystem plantingSystem, SeedBankSystem seedBankSystem, SunSystem sunSystem, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _independentModeRunner = independentModeRunner;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _seedBankSystem = seedBankSystem;
            _sunSystem = sunSystem;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        // PC 0x435B40 fixed and randomly placed plants. Coordinates differ from the mobile .NET port.
        /// <summary>Performs initialize mode izombie.</summary>
        internal void InitializeModeIZombie()
        {
            var s = _independentModeRunner.RequiredModeSession;
            Array.Clear(s.Brains, 0, s.Brains.Length);
            Array.Clear(s._brainBite, 0, s._brainBite.Length);
            s._sunflowerHealth.Clear();
            s._sunflowerDrops.Clear();
            switch (s.Definition.Id)
            {
                case 61:
                    ModeFixedPlant(1, 3, 2);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(1, 7);
                    ModeRandomPlants(17, 3);
                    ModeRandomPlants(0, 6);
                    ModeRandomPlants(5, 2);
                    break;
                case 62:
                    ModeFixedPlant(21, 3, 0);
                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(21, 1, 0);
                    ModeRandomPlants(0, 1, 0);
                    ModeRandomPlants(5, 2, 3);
                    ModeRandomPlants(1, 1, 3);
                    ModeRandomPlants(1, 4);
                    ModeRandomPlants(21, 2);
                    ModeRandomPlants(5, 2);
                    ModeRandomPlants(0, 4);
                    break;
                case 63:
                    ModeFixedPlant(4, 3, 0);
                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(4, 2, 2);
                    ModeFixedPlant(1, 2, 4);
                    ModeFixedPlant(22, 3, 3);
                    ModeRandomPlants(22, 2);
                    ModeRandomPlants(1, 5);
                    ModeRandomPlants(0, 7);
                    ModeRandomPlants(28, 1);
                    break;
                case 64:
                    for (var r = 0; r < 5; r++)
                    {
                        ModeFixedPlant(3, 3, r);
                    }

                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(1, 2, 2);
                    ModeFixedPlant(1, 2, 4);
                    ModeRandomPlants(0, 1, 0);
                    ModeRandomPlants(5, 1, 1);
                    ModeRandomPlants(10, 2, 2);
                    ModeRandomPlants(5, 1, 3);
                    ModeRandomPlants(0, 1, 4);
                    ModeRandomPlants(0, 2);
                    ModeRandomPlants(1, 4);
                    break;
                case 65:
                    ModeFixedPlant(1, 3, 2);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(26, 1, 1);
                    ModeRandomPlants(26, 1, 4);
                    ModeRandomPlants(31, 1);
                    ModeRandomPlants(1, 5);
                    ModeRandomPlants(0, 8);
                    ModeRandomPlants(5, 2);
                    break;
                case 66:
                    ModeFixedPlant(36, 4, 1);
                    ModeFixedPlant(36, 4, 3);
                    ModeRandomPlants(1, 3, 1);
                    ModeRandomPlants(1, 3, 3);
                    ModeRandomPlants(22, 2);
                    ModeRandomPlants(1, 2);
                    ModeRandomPlants(21, 3);
                    ModeRandomPlants(5, 1);
                    ModeRandomPlants(0, 5);
                    ModeRandomPlants(17, 2);
                    ModeRandomPlants(34, 2);
                    break;
                case 67:
                    ModeFixedPlant(1, 4, 2);
                    ModeFixedPlant(1, 4, 4);
                    ModeRandomPlants(1, 6);
                    ModeRandomPlants(4, 9);
                    ModeRandomPlants(6, 8);
                    break;
                case 68:
                    ModeRandomPlants(3, 3);
                    ModeRandomPlants(31, 2);
                    ModeRandomPlants(0, 8);
                    ModeRandomPlants(17, 2);
                    ModeRandomPlants(4, 2);
                    ModeRandomPlants(1, 8);
                    break;
                case 69:
                    ModeFixedPlant(23, 5, 1);
                    ModeFixedPlant(22, 5, 3);
                    ModeRandomPlants(4, 4, 0);
                    ModeRandomPlants(1, 2, 0);
                    ModeRandomPlants(1, 2, 1);
                    ModeRandomPlants(18, 1, 1);
                    ModeRandomPlants(5, 1, 1);
                    ModeRandomPlants(28, 1, 1);
                    ModeRandomPlants(6, 3, 2);
                    ModeRandomPlants(1, 2, 2);
                    ModeRandomPlants(17, 1, 2);
                    ModeRandomPlants(0, 3, 3);
                    ModeRandomPlants(1, 2, 3);
                    ModeRandomPlants(1, 1, 4);
                    ModeRandomPlants(10, 1, 4);
                    ModeRandomPlants(13, 1, 4);
                    ModeRandomPlants(29, 1, 4);
                    ModeRandomPlants(28, 1, 4);
                    ModeRandomPlants(31, 1, 4);
                    break;
                case 70:
                    InitializeModeIZombieEndless();
                    break;
            }

            foreach (var p in _world.Plants)
            {
                p.Asleep = false;
                p._modeFireTimer = 0;
                if (p.Kind == PlantKind.PotatoMine)
                {
                    p.ActionTimer = 0;
                    p.MinePhase = MinePhase.Armed;
                    p.PhaseAge = 20;
                }

                if (p.Kind == PlantKind.Sunflower)
                {
                    p.ActionTimer = 100000;
                    s._sunflowerHealth[p.Id] = p.Health;
                    s._sunflowerDrops[p.Id] = 0;
                }
            }

            int[][] cards =
            {
                new[]
                {
                    0,
                    4,
                    7
                },
                new[]
                {
                    0,
                    6,
                    4
                },
                new[]
                {
                    0,
                    4,
                    17
                },
                new[]
                {
                    0,
                    3,
                    20
                },
                new[]
                {
                    0,
                    8,
                    20,
                    16
                },
                new[]
                {
                    0,
                    3,
                    4,
                    23
                },
                new[]
                {
                    0,
                    3,
                    4,
                    8
                },
                new[]
                {
                    24,
                    2,
                    4,
                    20,
                    17,
                    21
                },
                new[]
                {
                    24,
                    2,
                    3,
                    4,
                    20,
                    17,
                    21,
                    7
                },
                new[]
                {
                    24,
                    2,
                    3,
                    4,
                    20,
                    17,
                    21,
                    7,
                    8
                }
            };
            var result = new List<ZombieKind>();
            foreach (int id in cards[s.Definition.Id - 61])
            {
                result.Add((ZombieKind)id);
            }

            s.ZombieCards = result.AsReadOnly();
        }

        /// <summary>Gets or sets mode izombie columns.</summary>
        internal int ModeIZombieColumns
        {
            get
            {
                var id = _independentModeRunner.RequiredModeSession.Definition.Id;
                return id <= 65 ? 4 : id == 69 ? 6 : 5;
            }
        }

        /// <summary>Performs mode fixed plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "row">The row.</param>
        internal void ModeFixedPlant(int kind, int column, int row)
        {
            if (_boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) == null)
            {
                _plantingSystem.PlaceAdventurePlant((PlantKind)kind, row, column);
            }
        }

        /// <summary>Performs mode random plants.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        /// <param name = "row">The row.</param>
        internal void ModeRandomPlants(int kind, int count, int row = -1)
        {
            using var cellsLease = _world.IntegerScratch.Rent();
            var cells = cellsLease.Items;
            for (var r = 0; r < 5; r++)
            {
                if (row < 0 || r == row)
                {
                    for (var c = 0; c < ModeIZombieColumns; c++)
                    {
                        if (_boardQueries.AdventurePlantAt(r, c, PlantLayer.Main) == null && ((kind != 3 && kind != 22) || ModeIZombieColumns - c <= 3))
                        {
                            cells.Add(r * 9 + c);
                        }
                    }
                }
            }

            for (var n = 0; n < count && cells.Count > 0; n++)
            {
                int p = _world._random.Next(cells.Count), cell = cells[p];
                cells.RemoveAt(p);
                _plantingSystem.PlaceAdventurePlant((PlantKind)kind, cell / 9, cell % 9);
            }
        }

        /// <summary>Performs initialize mode izombie endless.</summary>
        internal void InitializeModeIZombieEndless()
        {
            var stage = _independentModeRunner.RequiredModeSession.Stage - 1;
            var style = _world._random.Next(5);
            int lo = Math.Min(4, Math.Max(2, 2 + stage / 3)), hi = Math.Min(6, Math.Max(2, 3 + stage / 2));
            if (stage >= 10)
            {
                lo = Math.Min(5, Math.Max(2, 2 + stage / 3));
                hi = Math.Min(7, Math.Max(2, 3 + stage / 2));
            }

            var replace = stage == 0 ? 0 : stage == 1 ? 1 : _world._random.Next(lo, hi + 1);
            ModeRandomPlants(1, 8 - replace);
            ModeRandomPlants(8, replace);
            if (style == 0 && stage >= 1)
            {
                var branch = _world._random.Next(5);
                int[][] kinds =
                {
                    new[]
                    {
                        5,
                        9,
                        28,
                        4,
                        7,
                        4
                    },
                    new[]
                    {
                        4,
                        9,
                        6,
                        8
                    },
                    new[]
                    {
                        21,
                        9,
                        29,
                        8
                    },
                    new[]
                    {
                        10,
                        9,
                        31,
                        8
                    },
                    new[]
                    {
                        13,
                        12,
                        1,
                        5
                    }
                };
                var pattern = kinds[branch];
                for (var i = 0; i < pattern.Length; i += 2)
                {
                    ModeRandomPlants(pattern[i], pattern[i + 1]);
                }
            }
            else
            {
                var branch = _world._random.Next(6);
                var pattern = branch <= 2 ? new[]
                {
                    3,
                    1,
                    22,
                    1,
                    4,
                    1,
                    6,
                    2,
                    0,
                    1,
                    28,
                    1,
                    34,
                    1,
                    18,
                    1,
                    5,
                    1,
                    17,
                    1,
                    10,
                    1,
                    37,
                    1,
                    29,
                    1,
                    31,
                    1,
                    21,
                    2
                }

                : branch <= 4 ? new[]
                {
                    22,
                    1,
                    28,
                    3,
                    7,
                    1,
                    34,
                    3,
                    18,
                    1,
                    5,
                    3,
                    37,
                    1,
                    31,
                    1,
                    21,
                    3
                }

                : new[]
                {
                    4,
                    4,
                    6,
                    3,
                    17,
                    3,
                    10,
                    4,
                    21,
                    3
                };
                for (var i = 0; i < pattern.Length; i += 2)
                {
                    ModeRandomPlants(pattern[i], pattern[i + 1]);
                }
            }
        }

        /// <summary>Performs mode zombie cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting mode zombie cost.</returns>
        internal static int ModeZombieCost(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Normal:
                case ZombieKind.Imp:
                    return 50;
                case ZombieKind.Conehead:
                case ZombieKind.PoleVaulter:
                    return 75;
                case ZombieKind.ScreenDoor:
                    return 100;
                case ZombieKind.Buckethead:
                case ZombieKind.Digger:
                case ZombieKind.Bungee:
                    return 125;
                case ZombieKind.Ladder:
                case ZombieKind.Balloon:
                    return 150;
                case ZombieKind.Football:
                case ZombieKind.Zomboni:
                    return 175;
                case ZombieKind.Pogo:
                    return 200;
                case ZombieKind.Gargantuar:
                    return 300;
                case ZombieKind.Dancing:
                    return 350;
                default:
                    return int.MaxValue;
            }
        }

        /// <summary>Performs place mode zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place mode zombie.</returns>
        internal bool PlaceModeZombie(ZombieKind kind, int row, int column)
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.IZombie || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing || row < 0 || row >= 5 || column < 0 || column >= 9)
            {
                return false;
            }

            var offered = false;
            foreach (var k in s.ZombieCards)
            {
                if (k == kind)
                {
                    offered = true;
                }
            }

            if (!offered)
            {
                return false;
            }

            if (kind != ZombieKind.Bungee && column < ModeIZombieColumns)
            {
                return false;
            }

            if (kind == ZombieKind.Bungee && (column >= ModeIZombieColumns || _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) == null))
            {
                return false;
            }

            var cost = ModeZombieCost(kind);
            if (_seedBankSystem.AdventureSunAvailable < cost)
            {
                return false;
            }

            if (!_seedBankSystem.SpendAdventureSun(cost))
            {
                return false;
            }

            var z = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, row, column + 0.5f, s.Stage - 1);
            if (kind == ZombieKind.Bungee)
            {
                z.Row = row;
                z.X = AdventureZombieNativeRules.GridX(40f + column * 80f, AdventureDancerRules.Scale(z));
                z.PresentationPreviousX = z.X;
                z.TargetColumn = column;
            }

            if (kind == ZombieKind.Gargantuar)
            {
                z.ThrownImp = true;
            }

            _legacySimulation.Raise(GameEvent.Planted);
            return true;
        }

        /// <summary>Performs before mode izombie.</summary>
        internal void BeforeModeIZombie()
        {
            var s = _world.ModeSession;
            foreach (var p in _world.Plants)
            {
                if (p.Kind == PlantKind.Sunflower)
                {
                    p.ActionTimer = 100000;
                }
            }

            foreach (var z in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (!z.Hypnotized && !z.Headless && z.Kind != ZombieKind.Bungee && !(z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)) && z.X < 0.2f)
                {
                    if (s == null)
                    {
                        throw new System.InvalidOperationException("The value 's' is not available.");
                    }

                    if (s.Brains[z.Row])
                    {
                        _world.Zombies.Remove(z);
                        continue;
                    }

                    z.X = 0.16f;
                    z.Eating = true;
                    s._brainBite[z.Row] += 0.01f;
                    if (s._brainBite[z.Row] >= 0.7f)
                    {
                        s.Brains[z.Row] = true;
                        _world.Zombies.Remove(z);
                    }
                }
            }
        }

        /// <summary>Performs after mode izombie.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AfterModeIZombie(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            using var keysLease = _world.IntegerScratch.Rent();
            keysLease.Items.AddRange(s._sunflowerHealth.Keys);
            foreach (var id in keysLease.Items)
            {
                var p = _world.Plants.FindById(id);
                var lost = s._sunflowerHealth[id] - (p == null || p.BungeeState == 2 ? 0 : p.Health);
                var drops = p == null || p.BungeeState == 2 ? 8 : Math.Min(8, (int)(lost / 40f));
                var old = s._sunflowerDrops[id];
                for (var i = old; i < drops; i++)
                {
                    _world.Suns.Add(_sunSystem.CreateSun(p == null ? 1f : p.Column + 0.5f, p == null ? 2f : p.Row + 0.5f, p == null ? 2f : p.Row + 0.7f));
                }

                s._sunflowerDrops[id] = drops;
            }

            var available = _world.SunAmount - _world._adventureSunReservation;
            foreach (global::PlantsVsZombies.Simulation.Sun sun in _world.Suns)
            {
                available += sun.Value;
            }

            if (_world.Zombies.Count == 0 && available < 50)
            {
                s._failureDelay += dt;
                if (s._failureDelay >= 0.5f)
                {
                    _world.State = MatchState.Lost;
                    _adventureModeRunner.Adventure.Phase = AdventurePhase.Lost;
                    s.Phase = ModePhase.Lost;
                    _legacySimulation.Raise(GameEvent.Lost);
                }
            }
            else
            {
                s._failureDelay = 0;
            }
        }
    }
}
