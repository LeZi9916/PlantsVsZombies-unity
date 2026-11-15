#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns EconomySystem responsibilities for one deterministic match.</summary>
    internal sealed class EconomySystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal EconomySystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, ConveyorSystem conveyorSystem, SunSystem sunSystem, VaseSystem vaseSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _conveyorSystem = conveyorSystem;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
        }

        /// <summary>Performs advance adventure economy.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureEconomy(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            if (!a.Level.IsNight && !a.Level.HasConveyor && a.Level.Mode != AdventureMode.Vasebreaker)
            {
                a._nextSkySun -= dt;
                if (a._nextSkySun <= 0f)
                {
                    _world.Suns.Add(_sunSystem.CreateAdventurePresentationSun(null, 25));
                    a._nextSkySun = 4.25f + Math.Min(9.5f, a._skySunCount++ * 0.1f) + (float)_world._random.NextDouble() * 2.75f;
                }
            }

            if (a.Level.HasConveyor)
            {
                a._nextPacket -= dt;
                if (a._nextPacket <= 0f)
                {
                    if (a._packets.Count < 10)
                    {
                        var total = 0;
                        foreach (SeedWeight s in a.Level.ConveyorSeeds)
                        {
                            total += _conveyorSystem.AdventureConveyorWeight(s);
                        }

                        if (total > 0)
                        {
                            var draw = _world._random.Next(total);
                            foreach (SeedWeight s in a.Level.ConveyorSeeds)
                            {
                                draw -= _conveyorSystem.AdventureConveyorWeight(s);
                                if (draw < 0)
                                {
                                    _vaseSystem.AddAdventurePacket(s.Kind);
                                    a._lastConveyorSeed = s.Kind;
                                    break;
                                }
                            }
                        }
                    }

                    a._nextPacket += (a._packets.Count > 8 ? 10f : a._packets.Count > 6 ? 5f : a._packets.Count > 4 ? 4.25f : 4f) * (a.Level.Mode == AdventureMode.Boss ? 0.875f : 1f);
                }
            }

            for (var i = a._packets.Count - 1; i >= 0; i--)
            {
                if (!a._packets[i].IsVaseDrop)
                {
                    a._packets[i].Age += dt;
                }
            }
        }
    }
}
