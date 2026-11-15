#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ConveyorSystem responsibilities for one deterministic match.</summary>
    internal sealed class ConveyorSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ConveyorSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
        }

        /// <summary>Gets or sets uses native adventure conveyor.</summary>
        internal bool UsesNativeAdventureConveyor
        {
            get
            {
                if (!_world.IsAdventure)
                {
                    return false;
                }

                if (_world.ModeSession == null)
                {
                    return _adventureModeRunner.Adventure.Level.HasConveyor;
                }

                var definition = _world.ModeSession.Definition;
                return definition.UsesPackets && definition.Mechanic != ModeMechanic.Vases && definition.Mechanic != ModeMechanic.RainingSeeds && definition.Mechanic != ModeMechanic.Slots;
            }
        }

        /// <summary>Performs initialize adventure conveyor packet.</summary>
        /// <param name = "packet">The packet.</param>
        /// <param name = "index">The index.</param>
        internal void InitializeAdventureConveyorPacket(global::PlantsVsZombies.Adventure.AdventurePacket packet, int index)
        {
            if (!UsesNativeAdventureConveyor || packet._conveyorInitialized)
            {
                return;
            }

            // PC AddSeed 0x49F4C0 sets mOffsetX=515-51*i; retain the original 50px slot pitch.
            var offset = 515 - 51 * index;
            if (index > 0 && offset < _adventureModeRunner.Adventure._packets[index - 1].ConveyorOffsetX)
            {
                offset = _adventureModeRunner.Adventure._packets[index - 1].ConveyorOffsetX + 40;
            }

            packet.ConveyorOffsetX = offset;
            packet._conveyorInitialized = true;
        }

        /// <summary>Performs advance adventure conveyor packets.</summary>
        internal void AdvanceAdventureConveyorPackets()
        {
            if (!UsesNativeAdventureConveyor)
            {
                return;
            }

            _adventureModeRunner.Adventure.ConveyorTicks++;
            for (var i = 0; i < _adventureModeRunner.Adventure._packets.Count; i++)
            {
                var packet = _adventureModeRunner.Adventure._packets[i];
                InitializeAdventureConveyorPacket(packet, i);
                if (_adventureModeRunner.Adventure.ConveyorTicks % 4 == 0 && packet.ConveyorOffsetX > 0)
                {
                    packet.ConveyorOffsetX--;
                }
            }
        }

        /// <summary>Performs adventure conveyor card x.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting adventure conveyor card x.</returns>
        internal float AdventureConveyorCardX(int index)
        {
            return 91 + 50 * index + _adventureModeRunner.Adventure._packets[index].ConveyorOffsetX;
        }

        /// <summary>Performs remove adventure packet.</summary>
        /// <param name = "packet">The packet.</param>
        internal void RemoveAdventurePacket(global::PlantsVsZombies.Adventure.AdventurePacket packet)
        {
            var index = _adventureModeRunner.Adventure._packets.IndexOf(packet);
            if (index < 0)
            {
                return;
            }

            // PC RemoveSeed 0x49F530 moves the next type forward and adds51 to its offset.
            if (UsesNativeAdventureConveyor)
            {
                for (var i = index + 1; i < _adventureModeRunner.Adventure._packets.Count; i++)
                {
                    _adventureModeRunner.Adventure._packets[i].ConveyorOffsetX += 51;
                }
            }

            _adventureModeRunner.Adventure._packets.RemoveAt(index);
        }

        /// <summary>Advances the original Whack-a-Zombie seed-bank reveal after a sun is available.</summary>
        /// <param name="dt">The elapsed simulation seconds.</param>
        internal void AdvanceAdventureWhackSeedBank(float dt)
        {
            var board = _adventureModeRunner.Adventure;
            if (board.Level.Mode != AdventureMode.WhackAZombie || board._whackSeedBankY >= 0f)
            {
                return;
            }

            var collecting = false;
            foreach (var sun in _world.Suns)
            {
                if (sun.PresentationPickup.Collecting)
                {
                    collecting = true;
                    break;
                }
            }

            if (_world.SunAmount <= 0 && !collecting && board._whackSeedBankY <= -87f)
            {
                return;
            }

            board._whackSeedBankY = Math.Min(0f, board._whackSeedBankY + dt * 200f);
        }

        /// <summary>Performs adventure conveyor weight.</summary>
        /// <param name = "seed">The seed.</param>
        /// <returns>The resulting adventure conveyor weight.</returns>
        internal int AdventureConveyorWeight(SeedWeight seed)
        {
            var queued = _adventureModeRunner.Adventure._packets.CountMatching(p => p.Kind == seed.Kind);
            var planted = _world.Plants.CountMatching(p => p.Kind == seed.Kind);
            var weight = seed.Weight;
            if (seed.Kind == PlantKind.GraveBuster && _adventureModeRunner.Adventure._graves.Count <= queued + planted)
            {
                return 0;
            }

            if (seed.Kind == PlantKind.LilyPad || seed.Kind == PlantKind.FlowerPot)
            {
                var limit = seed.Kind == PlantKind.LilyPad ? 18 : 35;
                weight = (int)Math.Round(seed.Weight + (1f - seed.Weight) * Math.Min(limit, queued + planted) / limit);
            }

            if (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss && (seed.Kind == PlantKind.CabbagePult || seed.Kind == PlantKind.KernelPult || seed.Kind == PlantKind.MelonPult))
            {
                var empty = _world.Plants.CountMatching(p => p.Kind == PlantKind.FlowerPot && _boardQueries.AdventurePlantAt(p.Row, p.Column, PlantLayer.Main) == null);
                if (empty <= 2)
                {
                    weight /= 5;
                }
                else if (empty <= 5)
                {
                    weight /= 3;
                }
            }

            if (_adventureModeRunner.Adventure.Level.ConveyorSeeds.Count > 2)
            {
                if (queued >= 4)
                {
                    weight = 1;
                }
                else if (queued >= 3)
                {
                    weight = 5;
                }
                else if (_adventureModeRunner.Adventure._lastConveyorSeed == seed.Kind)
                {
                    weight /= 2;
                }
            }

            return Math.Max(0, weight);
        }
    }
}
