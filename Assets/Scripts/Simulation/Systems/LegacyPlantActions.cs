#nullable enable

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LegacyPlantActions responsibilities for one deterministic match.</summary>
    internal sealed class LegacyPlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides LegacyCombatSystem operations.</summary>
        private LegacyCombatSystem _legacyCombatSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LegacyPlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="legacyCombatSystem">The LegacyCombatSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(BoardQueries boardQueries, LegacyCombatSystem legacyCombatSystem, LegacySimulation legacySimulation, ProjectileSystem projectileSystem, SunSystem sunSystem)
        {
            _boardQueries = boardQueries;
            _legacyCombatSystem = legacyCombatSystem;
            _legacySimulation = legacySimulation;
            _projectileSystem = projectileSystem;
            _sunSystem = sunSystem;
        }

        /// <summary>Performs update plants.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdatePlants(float dt)
        {
            for (var i = _world.Plants.Count - 1; i >= 0; i--)
            {
                var plant = _world.Plants[i];
                plant.Age += dt;
                plant.PhaseAge += dt;
                if (plant.ShootAge >= 0f)
                {
                    plant.ShootAge += dt;
                }

                if (plant.Timing.HasValue)
                {
                    UpdateTimedPlant(plant);
                    continue;
                }

                plant.ActionTimer -= dt;
                if (plant.Kind == PlantKind.PotatoMine)
                {
                    UpdateMine(i, plant);
                    continue;
                }

                if (UpdateExtendedPlant(i, plant))
                {
                    continue;
                }

                if (plant.ActionTimer > 0.000001f)
                {
                    continue;
                }

                if (plant.Kind == PlantKind.CherryBomb)
                {
                    for (var z = _world.Zombies.Count - 1; z >= 0; z--)
                    {
                        var zombie = _world.Zombies[z];
                        if (Math.Abs(zombie.Row - plant.Row) <= 1 && Math.Abs(zombie.X - (plant.Column + 0.5f)) <= 1.5f)
                        {
                            _legacyCombatSystem.DamageZombie(z, 1800f);
                        }
                    }

                    _world.Plants.RemoveAt(i);
                    _legacySimulation.Raise(GameEvent.Explosion);
                }
            }
        }

        /// <summary>Performs has original timing.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting has original timing.</returns>
        internal static bool HasOriginalTiming(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater || kind == PlantKind.Sunflower;
        }

        /// <summary>Performs update timed plant.</summary>
        /// <param name = "plant">The plant.</param>
        internal void UpdateTimedPlant(Plant plant)
        {
            var state = plant.RequiredTiming;
            var target = false;
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Row == plant.Row && zombie.X > plant.Column + 0.4f)
                {
                    target = true;
                    break;
                }
            }
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, global::PlantsVsZombies.Timing.ShootingTrack.Head, true, 0);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            if (bound > 0)
            {
                input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, global::PlantsVsZombies.Timing.ShootingTrack.Head, true, _world._random.Next(bound));
            }

            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state, input);
            plant.Timing = result.State;
            if (result.AnimationStarted)
            {
                plant.ShootAge = 0f;
            }

            if (result.SunCreated)
            {
                _world.Suns.Add(_sunSystem.CreateSun(plant.Column + 0.5f, plant.Row + 0.45f, plant.Row + 0.7f));
            }

            for (var i = 0; i < result.ProjectilesCreated; i++)
            {
                var shot = _projectileSystem.RentPea();
                shot.SourceKind = plant.Kind;
                shot.Row = plant.Row;
                shot.X = plant.Column + 0.85f;
                shot.IsSnow = plant.Kind == PlantKind.SnowPea;
                _world.Peas.Add(shot);
                _legacySimulation.Raise(GameEvent.Shoot);
            }
        }

        /// <summary>Performs update mine.</summary>
        /// <param name = "plantIndex">The plant index.</param>
        /// <param name = "mine">The mine.</param>
        internal void UpdateMine(int plantIndex, Plant mine)
        {
            if (mine.MinePhase == MinePhase.Buried && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Rising;
                mine.PhaseAge = 0f;
                mine.ActionTimer = 19f / 18f;
            }
            else if (mine.MinePhase == MinePhase.Rising && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Armed;
                mine.PhaseAge = 0f;
            }
            else if (mine.MinePhase == MinePhase.Armed && _boardQueries.HasZombieNear(mine.Row, mine.Column + 0.5f, 0, 0.75f, false))
            {
                for (var z = _world.Zombies.Count - 1; z >= 0; z--)
                {
                    if (_world.Zombies[z].Row == mine.Row && Math.Abs(_world.Zombies[z].X - (mine.Column + 0.5f)) <= 0.75f)
                    {
                        _legacyCombatSystem.DamageZombie(z, 1800f);
                    }
                }

                _world.Plants.RemoveAt(plantIndex);
                _legacySimulation.Raise(GameEvent.MineExplosion);
            }
        }
        /// <summary>Performs update extended plant.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting update extended plant.</returns>
        internal bool UpdateExtendedPlant(int index, Plant plant)
        {
            if (plant.Kind == PlantKind.TallNut)
            {
                return true;
            }

            if (plant.Kind != PlantKind.Jalapeno)
            {
                return false;
            }

            // One hundred 0.01-second float steps can leave a tiny positive remainder.
            if (plant.ActionTimer > 0.000001f)
            {
                return true;
            }

            for (var z = _world.Zombies.Count - 1; z >= 0; z--)
            {
                var zombie = _world.Zombies[z];
                if (zombie.Row != plant.Row)
                {
                    continue;
                }

                switch (zombie.Kind)
                {
                    case ZombieKind.Normal:
                    case ZombieKind.Flag:
                    case ZombieKind.Conehead:
                    case ZombieKind.Buckethead:
                    case ZombieKind.ScreenDoor:
                        zombie.ChillRemaining = 0f;
                        _legacyCombatSystem.DamageZombie(z, SimulationWorld.JalapenoDamage);
                        break;
                }
            }

            _world.Plants.RemoveAt(index);
            _projectileSystem.EmitLaneFlame(plant.Row);
            _legacySimulation.Raise(GameEvent.JalapenoExplosion);
            return true;
        }
    }
}
