#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns VaseMode responsibilities for one deterministic match.</summary>
    internal sealed class VaseMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal VaseMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, IndependentModeRunner independentModeRunner, SunSystem sunSystem, VaseSystem vaseSystem, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _independentModeRunner = independentModeRunner;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
            _zombieSpawnSystem = zombieSpawnSystem;
        }
        /// <summary>Performs initialize mode vases.</summary>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void InitializeModeVases()
        {
            _world.Presentation.VaseMalletTicks = 0;
            var s = _independentModeRunner.RequiredModeSession;
            s._vaseSeeds.Clear();
            s._vaseSun.Clear();
            _adventureModeRunner.Adventure._vases.Clear();
            _adventureModeRunner.Adventure._packets.Clear();
            var id = s.Definition.Id;
            var min = id == 51 ? 4 : id == 52 || id == 53 || id == 57 ? 3 : 2;
            using var cellsLease = _world.IntegerScratch.Rent();
            var cells = cellsLease.Items;
            for (var c = min; c < 9; c++)
            {
                for (var r = 0; r < 5; r++)
                {
                    if (id != 52 || c != 8)
                    {
                        cells.Add(r * 9 + c);
                    }
                }
            }

            var recipe = id == 60 ? new[]
            {
                6,
                1,
                -1,
                52,
                2,
                1,
                -1,
                5,
                1,
                1,
                -1,
                0,
                2,
                1,
                -1,
                18,
                5,
                1,
                -1,
                17,
                1,
                1,
                -1,
                4,
                1,
                1,
                -1,
                3,
                1,
                1,
                -1,
                25,
                1,
                3,
                -1,
                -1,
                8 - Math.Min(8, (s.Stage - 1) / 10),
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                1 + Math.Min(8, (s.Stage - 1) / 10),
                2,
                23,
                -1
            }

            : SimulationWorld.s_modeVaseRecipes[id - 51];
            for (var i = 0; i < recipe.Length; i += 4)
            {
                for (var n = 0; n < recipe[i]; n++)
                {
                    if (cells.Count == 0)
                    {
                        throw new InvalidOperationException("PC vase recipe exceeded its legal columns.");
                    }

                    int pick = _world._random.Next(cells.Count), cell = cells[pick];
                    cells.RemoveAt(pick);
                    var vase = new AdventureVase
                    {
                        Row = cell / 9,
                        Column = cell % 9
                    };
                    if (recipe[i + 1] == 1)
                    {
                        var seed = recipe[i + 3];
                        vase._seed = seed == 52 ? PlantKind.Repeater : (PlantKind)seed;
                        s._vaseSeeds.Add(cell, seed);
                    }
                    else if (recipe[i + 1] == 2)
                    {
                        vase._zombie = (ZombieKind)recipe[i + 2];
                    }
                    else
                    {
                        s._vaseSun.Add(cell, (_world._random.Next(3) + 1) * 25);
                    }

                    _adventureModeRunner.Adventure._vases.Add(vase);
                }
            }

            _vaseSystem.RevealRandomVasePlants(2);

            if (id == 52)
            {
                for (var r = 0; r < 5; r++)
                {
                    var cell = r * 9 + 8;
                    _adventureModeRunner.Adventure._vases.Add(new AdventureVase {
                        Row = r,
                        Column = 8,
                        _seed = PlantKind.Repeater,
                        ShowsPlant = true
                    });
                    s._vaseSeeds[cell] = 52;
                }
            }

            _adventureModeRunner.Adventure.VaseStage = 2;
            _adventureModeRunner.Adventure.Tutorial = AdventureTutorial.Complete;
            // PC puzzles keep the cherry-bomb card; seed packets from vases remain free.
            _adventureModeRunner.Adventure.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.CherryBomb });
        }

        /// <summary>Starts the original delayed hammer swing over a standalone puzzle vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        internal bool SmashModeVase(int row, int column)
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Vases || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing)
            {
                return false;
            }

            return _vaseSystem.BeginVaseMallet(row, column);
        }

        /// <summary>Releases the original standalone recipe contents after the target vase has been removed.</summary>
        /// <param name="vase">The vase reached by the completed native hammer swing.</param>
        internal void OpenModeVase(AdventureVase vase)
        {
            var s = _independentModeRunner.RequiredModeSession;
            var cell = vase.Row * 9 + vase.Column;
            if (s._vaseSeeds.TryGetValue(cell, out var seed))
            {
                var packet = _vaseSystem.AddAdventurePacket(seed == 52 ? PlantKind.Repeater : (PlantKind)seed, vase.Row, vase.Column);
                if (seed == 52)
                {
                    s._backwardPackets.Add(packet.Id);
                }

                s._vaseSeeds.Remove(cell);
            }
            else if (s._vaseSun.TryGetValue(cell, out var value))
            {
                for (var i = 0; i < value / 25; i++)
                {
                    _world.Suns.Add(_sunSystem.CreateAdventurePresentationSunFromPoint(40f + vase.Column * 80f + i * 15f, 80f + vase.Row * 100f, 25));
                }

                s._vaseSun.Remove(cell);
            }
            else if (vase._zombie.HasValue)
            {
                _zombieSpawnSystem.SpawnAdventureBoardZombie(vase._zombie.Value, vase.Row, vase.Column + 0.5f, s.Stage - 1);
            }
        }
    }
}
