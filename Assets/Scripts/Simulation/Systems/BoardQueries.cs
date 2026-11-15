#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BoardQueries responsibilities for one deterministic match.</summary>
    internal sealed class BoardQueries
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Indexes occupied plant footprints.</summary>
        private readonly PlantCellIndex _cells;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BoardQueries(SimulationWorld world)
        {
            _world = world;
            _cells = new PlantCellIndex(world.Plants);
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        internal void Connect(BiteSystem biteSystem, CombatSystem combatSystem)
        {
            _biteSystem = biteSystem;
            _combatSystem = combatSystem;
        }

        /// <summary>Checks for a nearby zombie without allocating a captured predicate.</summary>
        /// <param name="row">The center lane.</param>
        /// <param name="x">The center board x coordinate.</param>
        /// <param name="rowRange">The maximum lane difference.</param>
        /// <param name="distance">The horizontal distance threshold.</param>
        /// <param name="hostileOnly">Whether to exclude hypnotized zombies and use a strict distance threshold.</param>
        /// <returns>True when a matching zombie is present.</returns>
        internal bool HasZombieNear(int row, float x, int rowRange, float distance, bool hostileOnly)
        {
            foreach (var zombie in _world.Zombies)
            {
                var separation = Math.Abs(zombie.X - x);
                if ((!hostileOnly || !zombie.Hypnotized) && Math.Abs(zombie.Row - row) <= rowRange && (hostileOnly ? separation < distance : separation <= distance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Finds a living plant by its per-match identifier without creating a predicate.</summary>
        /// <param name="id">The plant identifier to locate.</param>
        /// <returns>The matching plant, or null when it is no longer active.</returns>
        internal Plant? FindPlantById(int id)
        {
            return _world.Plants.FindById(id);
        }

        /// <summary>Checks whether a plant kind occupies a specific cell origin.</summary>
        /// <param name="kind">The plant kind to locate.</param>
        /// <param name="row">The requested lane.</param>
        /// <param name="column">The requested origin column.</param>
        /// <returns>True when a matching plant is active.</returns>
        internal bool HasPlantKindAt(PlantKind kind, int row, int column)
        {
            foreach (var plant in _cells.At(row, column))
            {
                if (plant.Kind == kind && plant.Row == row && plant.Column == column)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs adventure plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "layer">The layer.</param>
        /// <returns>The resulting adventure plant at.</returns>
        internal Plant AdventurePlantAt(int row, int column, PlantLayer layer)
        {
            foreach (var plant in _cells.At(row, column))
            {
                if (plant.Row == row && plant.Column <= column && column < plant.Column + plant.ColumnSpan && plant.Layer == layer && plant.BungeeState != 2
                    && !(plant.Kind == PlantKind.Squash && plant.PresentationBiteNotOnGround))
                {
                    return plant;
                }
            }

            return null!;
        }

        /// <summary>Finds a short-range mushroom target using the original PC plant and zombie rectangles.</summary>
        /// <param name="plant">The mushroom acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when its native attack interval is empty.</returns>
        internal Zombie? FindAdventurePuffTarget(Plant plant)
        {
            Zombie? target = null;
            foreach (var zombie in _world.Zombies.InRow(plant.Row))
            {
                if (AdventurePuffShroomRules.CanTarget(plant, zombie) && (target == null || zombie.X < target.X))
                {
                    target = zombie;
                }
            }

            return target;
        }

        /// <summary>Finds a Fume-shroom target using the original PC attack rectangle.</summary>
        /// <param name="plant">The Fume-shroom acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when its attack rectangle is empty.</returns>
        internal Zombie? FindAdventureFumeTarget(Plant plant)
        {
            Zombie? target = null;
            foreach (var zombie in _world.Zombies.InRow(plant.Row))
            {
                if (AdventureFumeShroomRules.CanTarget(plant, zombie)
                    && (target == null || AdventureZombieTargetingRules.BodyRectangle(zombie).X < AdventureZombieTargetingRules.BodyRectangle(target).X))
                {
                    target = zombie;
                }
            }

            return target;
        }

        /// <summary>Finds the original leftmost lobber target using Plant::GetPlantAttackRect and range mask thirteen.</summary>
        /// <param name="row">The lobber's planted row.</param>
        /// <param name="column">The lobber's planted column, independent of its weapon muzzle.</param>
        /// <returns>The nearest admitted body overlapping the original eight-hundred-pixel attack interval.</returns>
        internal Zombie? FindAdventureLobberTarget(int row, int column)
        {
            var left = 40 + column * 80 + 60;
            Zombie? best = null;
            var bestLeft = int.MaxValue;
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Kind != ZombieKind.Boss && zombie.Row != row || !_combatSystem.CanTakeAdventureAshDamage(zombie, 13U))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (Math.Min(left + 800, body.X + body.Width) >= Math.Max(left, body.X) && body.X < bestLeft)
                {
                    best = zombie;
                    bestLeft = body.X;
                }
            }

            return best;
        }

        /// <summary>Performs find adventure target.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "min">The min.</param>
        /// <param name = "max">The max.</param>
        /// <param name = "air">The air.</param>
        /// <param name = "submerged">The submerged.</param>
        /// <param name = "sourceKind">The source kind.</param>
        /// <returns>The resulting find adventure target.</returns>
        internal Zombie? FindAdventureTarget(int row, float min, float max, bool air, bool submerged = false, PlantKind sourceKind = PlantKind.Peashooter)
        {
            Zombie? best = null;
            foreach (Zombie z in _world.Zombies.InRow(row))
            {
                if (!z.Hypnotized && (sourceKind != PlantKind.TangleKelp || z.PresentationInPool == true)
                    && AdventureZombieTargetingRules.CanBeAutoTargeted(z, sourceKind) && z.DropRemaining <= 0f && z.Row == row && z.X >= min && z.X <= max && (!z.Airborne || air) && (!z.Submerged || submerged))
                {
                    if (best == null || z.X < best.X)
                    {
                        best = z;
                    }
                }
            }

            return best;
        }

        /// <summary>Performs find adventure native plant target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "attack">The attack.</param>
        /// <returns>The resulting find adventure native plant target.</returns>
        internal Plant? FindAdventureNativePlantTarget(Zombie z, global::PlantsVsZombies.Adventure.AdventureBiteAttack attack)
        {
            if (z.TangleKelpGrab != null)
            {
                // PC FindPlantTarget and UpdateZombieDolphinRider reject a target already held by kelp.
                return null;
            }

            var box = _biteSystem.AdventureZombieAttackRectangle(z);
            foreach (var p in _world.Plants.InRow(z.Row))
            {
                if (_biteSystem.AdventureBiteCanTarget(z, p, attack) && global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Reaches(box, _biteSystem.AdventurePlantEatingRectangle(p)))
                {
                    return p;
                }
            }

            return null;
        }
    }
}
