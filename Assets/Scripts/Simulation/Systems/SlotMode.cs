#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SlotMode responsibilities for one deterministic match.</summary>
    internal sealed class SlotMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SlotMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        internal void Connect(CoinSystem coinSystem, IndependentModeRunner independentModeRunner, SunSystem sunSystem, VaseSystem vaseSystem)
        {
            _coinSystem = coinSystem;
            _independentModeRunner = independentModeRunner;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
        }

        /// <summary>Performs spin mode slots.</summary>
        /// <returns>The resulting spin mode slots.</returns>
        internal bool SpinModeSlots()
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Slots || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing || s.SlotRollingRemaining > 0 || _world.SunAmount < 25)
            {
                return false;
            }

            _world.SunAmount -= 25;
            s.SlotRollingRemaining = 4f;
            for (var reel = 0; reel < 3; reel++)
            {
                int[] kinds =
                {
                    1,
                    0,
                    5,
                    3,
                    56,
                    57
                };
                int[] weights =
                {
                    100,
                    200 - Math.Min(5, _world.Plants.CountMatching(p => p.Kind == PlantKind.Peashooter)) * 20,
                    100,
                    100,
                    100,
                    30
                };
                if (reel == 2)
                {
                    for (var i = 0; i < kinds.Length; i++)
                    {
                        if (kinds[i] != 57 && (kinds[i] == s.SlotSymbols[0] || kinds[i] == s.SlotSymbols[1]))
                        {
                            weights[i] += weights[i] / 2;
                        }
                    }
                }

                var total = 0;
                foreach (int w in weights)
                {
                    total += w;
                }

                var ticket = _world._random.Next(total);
                for (var i = 0; i < kinds.Length; i++)
                {
                    ticket -= weights[i];
                    if (ticket < 0)
                    {
                        s.SlotSymbols[reel] = kinds[i];
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>Performs tick mode slot.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeSlot(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            if (s.SlotRollingRemaining <= 0)
            {
                return;
            }

            s.SlotRollingRemaining -= dt;
            if (s.SlotRollingRemaining > 0)
            {
                return;
            }

            int a = s.SlotSymbols[0], b = s.SlotSymbols[1], c = s.SlotSymbols[2];
            var matches = a == b && b == c ? 3 : a == b || a == c || b == c ? 2 : 0;
            if (matches == 0)
            {
                s.Advice = "[ADVICE_SLOT_MACHINE_SPIN_AGAIN]";
                return;
            }

            var kind = a == b || a == c ? a : b;
            if (kind == 56)
            {
                var count = matches == 3 ? 20 : 4;
                for (var i = 0; i < count; i++)
                {
                    _world.Suns.Add(_sunSystem.CreateSun(3.5f + i * 0.025f, 0f, 0.6f));
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_SUN_JACKPOT]" : "[ADVICE_SLOT_MACHINE_2_SUNS]";
            }
            else if (kind == 57)
            {
                for (var i = 0; i < (matches == 3 ? 5 : 1); i++)
                {
                    _coinSystem.SpawnAdventureCoin(AdventureCoinKind.Diamond, 4f + i * 0.2f, 0);
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_DIAMOND_JACKPOT]" : "[ADVICE_SLOT_MACHINE_2_DIAMONDS]";
            }
            else
            {
                for (var i = 0; i < (matches == 3 ? 3 : 1); i++)
                {
                    _vaseSystem.AddAdventurePacket((PlantKind)kind);
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_3_OF_A_KIND]" : "[ADVICE_SLOT_MACHINE_2_OF_A_KIND]";
            }
        }
    }
}
