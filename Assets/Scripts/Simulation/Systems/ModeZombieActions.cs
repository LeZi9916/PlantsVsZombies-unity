#nullable enable

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ModeZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class ModeZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ModeZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(CombatSystem combatSystem, IndependentModeRunner independentModeRunner, LegacySimulation legacySimulation, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, ZombieLifecycle zombieLifecycle)
        {
            _combatSystem = combatSystem;
            _independentModeRunner = independentModeRunner;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Performs is mode zombie visible.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting is mode zombie visible.</returns>
        internal bool IsModeZombieVisible(Zombie zombie)
        {
            return _world.ModeSession == null || _world.ModeSession.Definition.Mechanic != ModeMechanic.Invisible;
        }

        // PC types: 26 pea, 27 wall-nut, 28 jalapeno, 29 gatling, 30 squash, 31 tall-nut; 0x53F110..0x53F857.
        /// <summary>Performs tick mode special zombies.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeSpecialZombies(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            foreach (var z in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                var raw = (int)z.Kind;
                if (raw >= 26 && z.ModeHead < 0)
                {
                    z.ModeHead = raw;
                    z.Kind = raw == 32 ? ZombieKind.Gargantuar : ZombieKind.Normal;
                    if (raw == 27)
                    {
                        z.ArmorHealth = 1100f;
                    }
                    else if (raw == 31)
                    {
                        z.ArmorHealth = 2200f;
                    }
                    else if (raw == 28)
                    {
                        z.Health = 500f;
                        z.StartingHealth = 500f;
                        z._modeHeadTimer = 2f * (275 + _world._random.Next(175)) / (z.Speed * 80f);
                    }
                    else if (raw == 32)
                    {
                        z.Health = 6000f;
                        z.StartingHealth = 6000f;
                        z.Speed = 0.12f;
                    }
                }

                if (s.Definition.Mechanic == ModeMechanic.Speed && z.ModeHead != -2)
                {
                    z.Speed *= 2f;
                    z.ModeHead = -2;
                }

                if (z.ModeHead < 26 || z.ModeHead > 31 || z.Headless || z.FrozenRemaining > 0)
                {
                    continue;
                }

                var headBefore = z._modeHeadTimer;
                z._modeHeadTimer -= dt * (z.ChillRemaining > 0 ? 0.5f : 1f);
                if (z.ModeHead == 26 && z._modeHeadTimer <= 0)
                {
                    s.HeadShots.Add(new ModeHeadShot { X = z.X, Row = z.Row, Hypnotized = z.Hypnotized });
                    z._modeHeadTimer = 1.5f;
                }
                else if (z.ModeHead == 29)
                {
                    foreach (float threshold in new[]
                    {
                        0.69f,
                        0.51f,
                        0.36f,
                        0.18f
                    }

                    )
                    {
                        if (headBefore > threshold && z._modeHeadTimer <= threshold)
                        {
                            s.HeadShots.Add(new ModeHeadShot { X = z.X, Row = z.Row, Hypnotized = z.Hypnotized });
                        }
                    }

                    if (z._modeHeadTimer <= 0)
                    {
                        z._modeHeadTimer = 1.5f;
                    }
                }
                else if (z.ModeHead == 28 && z._modeHeadTimer <= 0)
                {
                    foreach (var p in new PooledSnapshot<Plant>(_world.Plants))
                    {
                        if (p.Row == z.Row)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(p);
                        }
                    }

                    _zombieLifecycle.KillAdventureZombie(z);
                    _projectileSystem.EmitLaneFlame(z.Row);
                    _legacySimulation.Raise(GameEvent.JalapenoExplosion);
                }
                else if (z.ModeHead == 30 && _world.Plants.Exists(p => p.Row == z.Row && Math.Abs(p.Column + 0.5f - z.X) < 0.8f))
                {
                    foreach (var p in new PooledSnapshot<Plant>(_world.Plants))
                    {
                        if (p.Row == z.Row && Math.Abs(p.Column + 0.5f - z.X) < 1f)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(p);
                        }
                    }

                    _zombieLifecycle.KillAdventureZombie(z);
                    _legacySimulation.Raise(GameEvent.Explosion);
                }
            }

            if (s == null)
            {
                throw new System.InvalidOperationException("The value 's' is not available.");
            }

            foreach (var shot in new PooledSnapshot<ModeHeadShot>(s.HeadShots))
            {
                if (shot.Hypnotized)
                {
                    shot.X += 4.5f * dt;
                    var enemy = _world.Zombies.Find(z => !z.Hypnotized && z.Row == (int)shot.Row && Math.Abs(z.X - shot.X) < 0.35f);
                    if (enemy != null)
                    {
                        _combatSystem.HitAdventureZombie(enemy, shot.Damage);
                        s.HeadShots.Remove(shot);
                    }
                    else if (shot.X > 12)
                    {
                        s.HeadShots.Remove(shot);
                    }

                    continue;
                }

                shot.X -= 4.5f * dt;
                var plant = _world.Plants.FindLast(p => p.Row == (int)shot.Row && Math.Abs(p.Column + 0.5f - shot.X) < 0.35f);
                if (plant != null)
                {
                    plant.Health -= shot.Damage;
                    if (plant.Health <= 0)
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(plant);
                    }

                    s.HeadShots.Remove(shot);
                }
                else if (shot.X < -1)
                {
                    s.HeadShots.Remove(shot);
                }
            }
        }
    }
}
