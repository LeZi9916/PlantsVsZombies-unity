#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LootSystem responsibilities for one deterministic match.</summary>
    internal sealed class LootSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LootSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, CoinSystem coinSystem, SunSystem sunSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _coinSystem = coinSystem;
            _sunSystem = sunSystem;
        }

        // PC Board.DropLootPiece 0x4289C0: 30000 tickets; +14/+250/+2500 currency cutoffs.
        /// <summary>Performs drop adventure loot.</summary>
        /// <param name = "z">The z.</param>
        internal void DropAdventureLoot(Zombie z)
        {
            var a = _adventureModeRunner.Adventure;
            if (_world.Rules.AdventureDisableLoot || (_world.ModeSession == null && !a.Level.IsReplay && a.Level.Number < 11) || z.BungeeCarrier)
            {
                return;
            }

            if (_world.ModeSession == null && !a.Level.IsReplay && a.CurrentWave > 5 && !a._droppedModeGift && (a.Level.Number == 22 && !_world.Rules.AdventureMinigamesUnlocked || a.Level.Number == 36 && !_world.Rules.AdventurePuzzleUnlocked))
            {
                _coinSystem.SpawnAdventureCoin(a.Level.Number == 22 ? AdventureCoinKind.MinigameUnlock : AdventureCoinKind.PuzzleUnlock, Math.Max(.1f, Math.Min(8.8f, z.X)), z.Row);
                a._droppedModeGift = true;
                return;
            }

            if (a.Level.Mode == AdventureMode.LittleTrouble && _world._random.Next(4) != 0)
            {
                return;
            }

            var x = Math.Max(.1f, Math.Min(8.8f, z.X));
            if (z.Kind == ZombieKind.Yeti)
            {
                for (var i = 0; i < 4; i++)
                {
                    _coinSystem.SpawnAdventureCoin(AdventureCoinKind.Diamond, Math.Max(.1f, x - i * .125f), z.Row);
                }

                return;
            }

            var factor = Math.Max(0, VerifiedDataCatalog.Zombies[(int)z.Kind].WaveBudgetCostRaw);
            if (factor == 0)
            {
                return;
            }

            DropAdventureLootPiece(x, z.Row, factor);
        }

        /// <summary>Rolls the original Board::DropLootPiece tickets and spawns the resulting sun, coin or gift.</summary>
        /// <param name="x">The board column position of the drop, already clamped to the lawn.</param>
        /// <param name="row">The lawn row of the drop.</param>
        /// <param name="factor">The drop factor that scales every ticket cutoff.</param>
        internal void DropAdventureLootPiece(float x, int row, int factor)
        {
            var a = _adventureModeRunner.Adventure;
            if (_world.Rules.AdventureDisableLoot)
            {
                return;
            }

            var roll = _world._random.Next(30000);
            if (_world.ModeSession == null && !a.Level.IsReplay && a.Level.Number == 11 && !a._droppedFirstCoin && a.CurrentWave > 5)
            {
                roll = 1000;
            }

            if (_world.ModeSession != null && _world.ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.Columns)
            {
                roll *= 5;
            }

            if (a.Level.Mode == AdventureMode.WhackAZombie)
            {
                var cutoff = _world.SunAmount > 500 ? 2800 : _world.SunAmount > 350 ? 3100 : _world.SunAmount > 200 ? 3700 : 5000;
                if (roll >= 2500 * factor && roll < cutoff * factor)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        _world.Suns.Add(_sunSystem.CreateSun(x - i * .25f, row, row + .3f));
                    }

                    return;
                }
            }

            if (a.TotalWavesSpawned > 70)
            {
                return;
            }

            var endless = _world.ModeSession != null && _world.ModeSession.Definition.Survival && _world.ModeSession.Definition.Endless;
            var plant = _world.Rules.AdventurePottedLoot ? (endless ? 3 : a.Level.IsReplay ? 24 : 12) : 0;
            var chocolate = plant + (_world.Rules.AdventureChocolateLoot ? (endless ? 9 : a.Level.IsReplay ? 72 : 36) : 0);
            AdventureCoinKind kind;
            if (roll < plant * factor)
            {
                kind = AdventureCoinKind.PlantGift;
            }
            else if (roll < chocolate * factor)
            {
                kind = AdventureCoinKind.Chocolate;
            }
            else if (roll < (chocolate + 14) * factor)
            {
                kind = _world.Rules.AdventurePacketSlots > 6 ? AdventureCoinKind.Diamond : AdventureCoinKind.Gold;
            }
            else if (roll < (chocolate + 250) * factor)
            {
                kind = AdventureCoinKind.Gold;
            }
            else if (roll < (chocolate + 2500) * factor)
            {
                kind = AdventureCoinKind.Silver;
            }
            else
            {
                return;
            }

            // Original bowling coins come from ricochets rather than every zombie death.
            if (a.Level.Mode == AdventureMode.Bowling && kind <= AdventureCoinKind.Diamond)
            {
                return;
            }

            var coin = _coinSystem.SpawnAdventureCoin(kind, x, row);
            if (coin == null)
            {
                return;
            }

            if (kind == AdventureCoinKind.PlantGift)
            {
                coin.GiftPlant = PickAdventureGardenGift();
            }

            a._droppedFirstCoin = true;
        }

        /// <summary>Performs pick adventure garden gift.</summary>
        /// <returns>The resulting pick adventure garden gift.</returns>
        internal PlantKind PickAdventureGardenGift()
        {
            if (_world.ModeSession != null && _world.ModeSession.Definition.Endless)
            {
                var all = new List<PlantKind>();
                for (var i = 0; i < 40; i++)
                {
                    if (i != 33 && i != 38)
                    {
                        all.Add((PlantKind)i);
                    }
                }

                return all[_world._random.Next(all.Count)];
            }

            int[][] plants =
            {
                new[]
                {
                    0,
                    1,
                    2,
                    3,
                    7,
                    4,
                    5,
                    6
                },
                new[]
                {
                    8,
                    9,
                    10,
                    11,
                    12,
                    13,
                    14,
                    15
                },
                new[]
                {
                    16,
                    17,
                    18,
                    19,
                    20,
                    21,
                    22,
                    23
                },
                new[]
                {
                    24,
                    25,
                    26,
                    27,
                    28,
                    29,
                    30,
                    31
                },
                new[]
                {
                    32,
                    34,
                    35,
                    36,
                    37,
                    39
                }
            };
            var index = (int)_adventureModeRunner.Adventure.Level.Terrain;
            if (index >= 5)
            {
                index = 4;
            }

            var list = plants[index];
            return (PlantKind)list[_world._random.Next(list.Length)];
        }
    }
}
