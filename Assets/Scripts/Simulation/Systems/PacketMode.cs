#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PacketMode responsibilities for one deterministic match.</summary>
    internal sealed class PacketMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PacketMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, IndependentModeRunner independentModeRunner, VaseSystem vaseSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _independentModeRunner = independentModeRunner;
            _vaseSystem = vaseSystem;
        }

        /// <summary>Performs initialize mode packets.</summary>
        internal void InitializeModePackets()
        {
            var s = _independentModeRunner.RequiredModeSession;
            s._nextPacket = s.Definition.Mechanic == ModeMechanic.RainingSeeds ? 1f : 4f;
            switch (s.Definition.Mechanic)
            {
                case ModeMechanic.Bowling:
                    _vaseSystem.AddAdventurePacket(PlantKind.Wallnut);
                    break;
                case ModeMechanic.Boss:
                    _vaseSystem.AddAdventurePacket(PlantKind.CabbagePult);
                    _vaseSystem.AddAdventurePacket(PlantKind.Jalapeno);
                    _vaseSystem.AddAdventurePacket(PlantKind.CabbagePult);
                    _vaseSystem.AddAdventurePacket(PlantKind.IceShroom);
                    s._nextPacket = 10f;
                    break;
                case ModeMechanic.Columns:
                    foreach (var k in new[]
                    {
                        PlantKind.PotatoMine,
                        PlantKind.TallNut,
                        PlantKind.MelonPult,
                        PlantKind.MagnetShroom,
                        PlantKind.CoffeeBean,
                        PlantKind.MelonPult
                    }

                    )
                    {
                        _vaseSystem.AddAdventurePacket(k);
                    }

                    s._nextPacket = 10f;
                    break;
                case ModeMechanic.Slots:
                    s.SlotSymbols[0] = 1;
                    s.SlotSymbols[1] = 0;
                    s.SlotSymbols[2] = 5;
                    break;
                case ModeMechanic.Invisible:
                    _vaseSystem.AddAdventurePacket(PlantKind.Peashooter);
                    _vaseSystem.AddAdventurePacket(PlantKind.IceShroom);
                    s._nextPacket = 10f;
                    _adventureModeRunner.Adventure.FogBlownRemaining = float.MaxValue;
                    break;
            }
        }

        /// <summary>Performs tick mode packets.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModePackets(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            var m = s.Definition.Mechanic;
            foreach (var rain in new PooledSnapshot<ModeRainPacket>(s.RainPackets))
            {
                rain.Y = Math.Min(rain.TargetY, rain.Y + 45f * dt);
                if (rain.Y >= rain.TargetY)
                {
                    rain.Age += dt;
                }

                if (!_adventureModeRunner.Adventure._packets.Exists(p => p.Id == rain.PacketId))
                {
                    s.RainPackets.Remove(rain);
                    continue;
                }

                if (rain.Age >= 7.5f)
                {
                    _adventureModeRunner.Adventure._packets.RemoveAll(p => p.Id == rain.PacketId);
                    s.RainPackets.Remove(rain);
                }
            }

            if (m == ModeMechanic.Vases)
            {
                return;
            }

            if (m == ModeMechanic.Slots)
            {
                foreach (var expiring in new PooledSnapshot<AdventurePacket>(_adventureModeRunner.Adventure._packets))
                {
                    if (expiring.Age >= 15f)
                    {
                        _adventureModeRunner.Adventure._packets.Remove(expiring);
                        s._backwardPackets.Remove(expiring.Id);
                    }
                }

                return;
            }

            s._nextPacket -= dt;
            if (s._nextPacket > 0 || _adventureModeRunner.Adventure._packets.Count >= 10)
            {
                return;
            }

            int[] weights;
            switch (m)
            {
                case ModeMechanic.Bowling:
                    weights = s.Definition.Id == 33 ? new[]
                    {
                        3,
                        85,
                        49,
                        15,
                        50,
                        15
                    }

                    : new[]
                    {
                        3,
                        85,
                        49,
                        15
                    };
                    break;
                case ModeMechanic.LittleZombies:
                    weights = new[]
                    {
                        16,
                        25,
                        3,
                        15,
                        0,
                        25,
                        2,
                        35
                    };
                    break;
                case ModeMechanic.Portals:
                    weights = new[]
                    {
                        0,
                        25,
                        7,
                        20,
                        22,
                        10,
                        26,
                        15,
                        3,
                        15,
                        2,
                        15
                    };
                    break;
                case ModeMechanic.Columns:
                    weights = new[]
                    {
                        33,
                        155,
                        39,
                        5,
                        6,
                        5,
                        30,
                        15,
                        20,
                        10,
                        17,
                        10
                    };
                    break;
                case ModeMechanic.Invisible:
                    weights = new[]
                    {
                        0,
                        25,
                        3,
                        15,
                        34,
                        5,
                        17,
                        15,
                        16,
                        30,
                        14,
                        10
                    };
                    break;
                case ModeMechanic.Boss:
                    weights = new[]
                    {
                        33,
                        55,
                        39,
                        10,
                        20,
                        12,
                        32,
                        10,
                        34,
                        5,
                        14,
                        8
                    };
                    break;
                case ModeMechanic.RainingSeeds:
                {
                        using var rainKindsLease = _world.IntegerScratch.Rent();
                        var rainKinds = rainKindsLease.Items;
                        foreach (var seed in _adventureModeRunner.Adventure.AvailableSeeds)
                        {
                            var id = (int)seed;
                            if (id >= 40 || id == 1 || id == 9 || id == 35 || id == 37 || id == 11 || id == 33)
                            {
                                continue;
                            }

                            rainKinds.Add(id);
                        }

                        var rainKind = rainKinds[_world._random.Next(rainKinds.Count)];
                        var lilies = _world.Plants.CountMatching(p => p.Kind == PlantKind.LilyPad);
                        if (_world._random.Next(100) < 30 - 29 * Math.Min(18, lilies) / 18)
                        {
                            rainKind = 16;
                        }

                        weights = new[]
                        {
                            rainKind,
                            1
                        };
                        break;
                }
                default:
                    return;
            }

            for (var i = 0; i < weights.Length; i += 2)
            {
                int seed = weights[i], queued = _adventureModeRunner.Adventure._packets.CountMatching(p => (int)p.Kind == seed), planted = _world.Plants.CountMatching(p => (int)p.Kind == seed);
                if (seed == 16 || seed == 33)
                {
                    var limit = seed == 16 ? 18 : m == ModeMechanic.Columns ? 45 : 35;
                    weights[i + 1] = (int)Math.Round(weights[i + 1] + (1 - weights[i + 1]) * Math.Min(limit, queued + planted) / (float)limit);
                }

                if (m == ModeMechanic.Boss && (seed == 32 || seed == 34 || seed == 39))
                {
                    var empty = _world.Plants.CountMatching(p => p.Kind == PlantKind.FlowerPot && _boardQueries.AdventurePlantAt(p.Row, p.Column, PlantLayer.Main) == null);
                    if (empty <= 2)
                    {
                        weights[i + 1] /= 5;
                    }
                    else if (empty <= 5)
                    {
                        weights[i + 1] /= 3;
                    }
                }

                if (weights.Length > 4)
                {
                    if (queued >= 4)
                    {
                        weights[i + 1] = 1;
                    }
                    else if (queued >= 3)
                    {
                        weights[i + 1] = 5;
                    }
                    else if (_adventureModeRunner.Adventure._lastConveyorSeed == (PlantKind)seed)
                    {
                        weights[i + 1] /= 2;
                    }
                }
            }

            var total = 0;
            for (var i = 1; i < weights.Length; i += 2)
            {
                total += weights[i];
            }

            if (total <= 0)
            {
                return;
            }

            int ticket = _world._random.Next(total), kind = weights[0];
            for (var i = 0; i < weights.Length; i += 2)
            {
                ticket -= weights[i + 1];
                if (ticket < 0)
                {
                    kind = weights[i];
                    break;
                }
            }

            var packet = _vaseSystem.AddAdventurePacket(kind == 50 ? PlantKind.Wallnut : (PlantKind)kind);
            if (kind == 50)
            {
                s._giantPackets.Add(packet.Id);
            }

            if (m == ModeMechanic.RainingSeeds)
            {
                s.RainPackets.Add(new ModeRainPacket {
                    PacketId = packet.Id,
                    X = 100f + _world._random.Next(550),
                    Y = 60f,
                    TargetY = 110f + (float)_world._random.NextDouble() * 340f
                });
            }

            var multiplier = m == ModeMechanic.Boss ? 0.875f : m == ModeMechanic.Portals ? 1.5f : m == ModeMechanic.Invisible ? 2f : m == ModeMechanic.Columns ? 3f : 1f;
            _adventureModeRunner.Adventure._lastConveyorSeed = packet.Kind;
            s._nextPacket = m == ModeMechanic.RainingSeeds ? 5f + _world._random.Next(500) * 0.01f : multiplier * (_adventureModeRunner.Adventure._packets.Count > 8 ? 10f : _adventureModeRunner.Adventure._packets.Count > 6 ? 5f : _adventureModeRunner.Adventure._packets.Count > 4 ? 4.25f : 4f);
        }
    }
}
