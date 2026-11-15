#nullable enable

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LegacyCombatSystem responsibilities for one deterministic match.</summary>
    internal sealed class LegacyCombatSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LegacyCombatSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        internal void Connect(LegacySimulation legacySimulation, ProjectileSystem projectileSystem)
        {
            _legacySimulation = legacySimulation;
            _projectileSystem = projectileSystem;
        }

        /// <summary>Performs spawn zombie.</summary>
        internal void SpawnZombie()
        {
            var number = ++_world.SpawnedZombies;
            var kind = number == _world.Rules.ZombieCount ? ZombieKind.Flag : number >= 10 && number % 5 == 0 ? ZombieKind.ScreenDoor : number >= 8 && number % 4 == 0 ? ZombieKind.Buckethead : number >= 4 && number % 3 == 0 ? ZombieKind.Conehead : ZombieKind.Normal;
            var armor = kind == ZombieKind.Buckethead ? 1100f : kind == ZombieKind.Conehead ? 370f : 0f;
            var shield = kind == ZombieKind.ScreenDoor ? 1100f : 0f;
            _world.Zombies.Add(new Zombie {
                Kind = kind,
                Row = _world._random.Next(SimulationWorld.Rows),
                X = 9.5f,
                Health = 270f,
                ArmorHealth = armor,
                ShieldHealth = shield
            });
        }

        /// <summary>Performs update peas.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdatePeas(float dt)
        {
            for (var i = _world.Peas.Count - 1; i >= 0; i--)
            {
                var pea = _world.Peas[i];
                var oldX = pea.X;
                pea.X += dt * 4.5f;
                var hit = -1;
                var closest = float.MaxValue;
                for (var j = 0; j < _world.Zombies.Count; j++)
                {
                    var zombie = _world.Zombies[j];
                    if (zombie.Row == pea.Row && zombie.X >= oldX - 0.15f && zombie.X <= pea.X + 0.35f && zombie.X < closest)
                    {
                        hit = j;
                        closest = zombie.X;
                    }
                }

                if (hit >= 0)
                {
                    DamageZombie(hit, 20f, pea.IsSnow);
                    _projectileSystem.RemovePeaAt(i);
                }
                else if (pea.X > 10f)
                {
                    _projectileSystem.RemovePeaAt(i);
                }
            }
        }

        /// <summary>Performs update zombies.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdateZombies(float dt)
        {
            for (var i = _world.Zombies.Count - 1; i >= 0; i--)
            {
                var zombie = _world.Zombies[i];
                var speedMultiplier = zombie.ChillRemaining > 0f ? 0.5f : 1f;
                zombie.ChillRemaining = Math.Max(0f, zombie.ChillRemaining - dt);
                zombie.Age += dt * speedMultiplier;
                Plant? target = null;
                for (var j = 0; j < _world.Plants.Count; j++)
                {
                    var candidate = _world.Plants[j];
                    if (candidate.Row == zombie.Row && Math.Abs(zombie.X - (candidate.Column + 0.5f)) < 0.38f)
                    {
                        target = candidate;
                        break;
                    }
                }

                zombie.Eating = target != null;
                if (target != null)
                {
                    zombie.BiteTimer -= dt * speedMultiplier;
                    if (zombie.BiteTimer <= 0f && !(target.Kind == PlantKind.PotatoMine && target.MinePhase != MinePhase.Buried) && target.Kind != PlantKind.CherryBomb && target.Kind != PlantKind.Jalapeno)
                    {
                        target.Health -= 4f;
                        zombie.BiteTimer += 0.04f;
                        if (target.Health <= 0f)
                        {
                            _world.Plants.Remove(target);
                        }
                    }
                }
                else
                {
                    zombie.BiteTimer = 0f;
                    zombie.X -= _world.Rules.ZombieSpeed * dt * speedMultiplier;
                }

                if (zombie.X < 0.15f && _world.Mowers[zombie.Row].Available)
                {
                    _world.Mowers[zombie.Row].Available = false;
                    _world.Mowers[zombie.Row].Active = true;
                    _legacySimulation.Raise(GameEvent.Mower);
                }
                else if (zombie.X < -0.65f && !_world.Mowers[zombie.Row].Active)
                {
                    _world.State = MatchState.Lost;
                    _legacySimulation.Raise(GameEvent.Lost);
                    return;
                }
            }
        }

        /// <summary>Performs update mowers.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdateMowers(float dt)
        {
            foreach (Mower mower in _world.Mowers)
            {
                if (!mower.Active)
                {
                    continue;
                }

                mower.X += dt * 5f;
                for (var i = _world.Zombies.Count - 1; i >= 0; i--)
                {
                    if (_world.Zombies[i].Row == mower.Row && Math.Abs(_world.Zombies[i].X - mower.X) < 0.7f)
                    {
                        DamageZombie(i, 99999f);
                    }
                }

                if (mower.X > 10f)
                {
                    mower.Active = false;
                }
            }
        }

        /// <summary>Performs damage zombie.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "chill">The chill.</param>
        internal void DamageZombie(int index, float damage, bool chill = false)
        {
            var zombie = _world.Zombies[index];
            if (chill)
            {
                zombie.ChillRemaining = Math.Max(zombie.ChillRemaining, 10f);
            }

            var shieldDamage = Math.Min(zombie.ShieldHealth, damage);
            zombie.ShieldHealth -= shieldDamage;
            damage -= shieldDamage;
            var armorDamage = Math.Min(zombie.ArmorHealth, damage);
            zombie.ArmorHealth -= armorDamage;
            zombie.Health -= damage - armorDamage;
            _legacySimulation.Raise(GameEvent.ZombieHit);
            if (zombie.Health <= 0f)
            {
                _world.Zombies.RemoveAt(index);
                _legacySimulation.Raise(GameEvent.ZombieDied);
            }
        }
    }
}
