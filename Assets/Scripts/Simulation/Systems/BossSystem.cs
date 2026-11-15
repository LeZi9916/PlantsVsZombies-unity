#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BossSystem responsibilities for one deterministic match.</summary>
    internal sealed class BossSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BossSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LegacySimulation legacySimulation, PlantingSystem plantingSystem, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Performs advance adventure boss.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureBoss(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            if (a.BossHealth <= 0)
            {
                a.BossHeadVulnerable = false;
                return;
            }

            a._bossTimer -= dt;
            if (a.BossHeadVulnerable)
            {
                if (a._bossTimer <= 0)
                {
                    a.BossHeadVulnerable = false;
                    a._bossTimer = 8f;
                }
            }
            else if (a._bossTimer <= 0)
            {
                a._bossCycle++;
                if (a._bossCycle % 3 == 0)
                {
                    a.BossHeadVulnerable = true;
                    a._bossTimer = 14f;
                    a._balls.Add(new AdventureBossBall { Row = _world._random.Next(5), X = 9f, IsIce = a._bossCycle % 2 == 0 });
                    a.Notice(AdventureNoticeKind.BossAttack, a._bossCycle % 2 == 0 ? 1 : 0);
                }
                else if (a._bossCycle % 5 == 0)
                {
                    int row = _world._random.Next(4), col = _world._random.Next(2, 7);
                    foreach (Plant p in new PooledSnapshot<Plant>(_world.Plants))
                    {
                        if (Math.Abs(p.Row - row) <= 1 && Math.Abs(p.Column - col) <= 1)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(p);
                        }
                    }

                    a.Notice(AdventureNoticeKind.BossAttack, 2, row, col);
                    a._bossTimer = 10f;
                }
                else
                {
                    var count = 2 + a._bossCycle / 8;
                    for (var i = 0; i < Math.Min(6, count); i++)
                    {
                        var kind = a.BossHealth < 15000f && i == 0 ? ZombieKind.Gargantuar : a.BossHealth < 30000f && i == 0 ? ZombieKind.Catapult : i % 3 == 0 ? ZombieKind.Buckethead : i % 3 == 1 ? ZombieKind.Conehead : ZombieKind.Normal;
                        _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, _world._random.Next(5), 9.5f + i * 0.2f, a._bossCycle);
                    }

                    a.Notice(AdventureNoticeKind.BossAttack, 3);
                    a._bossTimer = 10f;
                }
            }

            for (var i = a._balls.Count - 1; i >= 0; i--)
            {
                var ball = a._balls[i];
                ball.X -= 1.25f * dt;
                foreach (Plant p in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (p.Row == ball.Row && Math.Abs(p.Column + 0.5f - ball.X) < 0.4f)
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(p);
                    }
                }

                if (ball.X < 0)
                {
                    a._balls.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs damage adventure boss.</summary>
        /// <param name = "damage">The damage.</param>
        internal void DamageAdventureBoss(float damage)
        {
            if (_adventureModeRunner.Adventure.Level.Mode != AdventureMode.Boss || _adventureModeRunner.Adventure.BossHealth <= 0 || !_adventureModeRunner.Adventure.BossHeadVulnerable)
            {
                return;
            }

            _adventureModeRunner.Adventure.BossHealth = Math.Max(0, _adventureModeRunner.Adventure.BossHealth - damage);
            _legacySimulation.Raise(GameEvent.ZombieHit);
            if (_adventureModeRunner.Adventure.BossHealth <= 0)
            {
                _adventureModeRunner.Adventure._balls.Clear();
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.BossAttack, 4);
            }
        }
    }
}
