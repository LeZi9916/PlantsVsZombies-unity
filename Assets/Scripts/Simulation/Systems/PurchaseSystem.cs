#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PurchaseSystem responsibilities for one deterministic match.</summary>
    internal sealed class PurchaseSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PurchaseSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, CombatSystem combatSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _combatSystem = combatSystem;
        }

        // Board.PlaceRake / GridItem.UpdateRake: column 7, 20 fps, .8 hit event, 200-tick lifetime.
        /// <summary>Performs initialize adventure purchases.</summary>
        internal void InitializeAdventurePurchases()
        {
            if (_world.Rules.AdventurePlaceRake == null || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                return;
            }

            using var rowsLease = _world.IntegerScratch.Rent();
            var rows = rowsLease.Items;
            for (var r = 0; r < Math.Min(5, _world.RowCount); r++)
            {
                if (_adventureModeRunner.Adventure.Level.IsLawnRow(r) && !_adventureModeRunner.Adventure.Level.IsWaterRow(r))
                {
                    rows.Add(r);
                }
            }

            if (rows.Count == 0)
            {
                return;
            }

            var row = rows[_world._random.Next(rows.Count)];
            var column = 7;
            if (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                foreach (var vase in _adventureModeRunner.Adventure._vases)
                {
                    if (vase.Column > 0 && vase.Column <= column)
                    {
                        column = vase.Column - 1;
                    }
                }
            }

            if (!_world.Rules.AdventurePlaceRake())
            {
                return;
            }

            _adventureModeRunner.Adventure.RakeRow = row;
            _adventureModeRunner.Adventure.RakeColumn = column;
            _adventureModeRunner.Adventure.RakeAttracting = true;
        }

        /// <summary>Performs advance adventure purchases.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventurePurchases(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            if (a.RakeRow < 0)
            {
                return;
            }

            Zombie? target = null;
            foreach (var z in _world.Zombies)
            {
                if (z.Row == a.RakeRow && !z.Airborne && !z.Submerged && !z.Hypnotized && z.Kind != ZombieKind.Bungee && z.Kind != ZombieKind.Bobsled && z.X - 0.3f <= a.RakeColumn + 63f / 80f && z.X + 0.3f >= a.RakeColumn)
                {
                    target = z;
                    break;
                }
            }

            if (!a.RakeTriggered)
            {
                if (target != null)
                {
                    a.RakeTriggered = true;
                    a.RakeAge = 0f;
                }

                return;
            }

            a.RakeAge += dt;
            if (!a._rakeDamageDone && a.RakeAge >= .32f)
            {
                a._rakeDamageDone = true;
                if (target != null)
                {
                    _combatSystem.HitAdventureZombie(target, 1800f);
                }
            }

            if (a.RakeAge >= 2f)
            {
                a.RakeRow = -1;
            }
        }

        /// <summary>Performs is adventure first aid.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting is adventure first aid.</returns>
        internal bool IsAdventureFirstAid(PlantKind kind, int row, int column)
        {
            if (!_world.Rules.AdventureFirstAid || kind != PlantKind.Wallnut && kind != PlantKind.TallNut && kind != PlantKind.Pumpkin)
            {
                return false;
            }

            var old = _boardQueries.AdventurePlantAt(row, column, kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main);
            var maximum = kind == PlantKind.TallNut ? 8000 : 4000;
            return old != null && old.Kind == kind && old.BungeeState == 0 && old.Health < maximum * 2 / 3;
        }
    }
}
