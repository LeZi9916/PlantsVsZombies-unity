#nullable enable
using System.Collections.Generic;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Rehearses production actor and feedback lifetimes on a detached checkpoint with an independent random stream.</summary>
    internal sealed class SimulationPreparationRehearsal
    {
        /// <summary>The independently restored production facade used to rehearse complete fixed updates.</summary>
        private readonly GameSimulation _game;
        /// <summary>The detached model whose changes are never published to the live board.</summary>
        private readonly SimulationWorld _world;
        /// <summary>The detached production spawn collaborator.</summary>
        private readonly ZombieSpawnSystem _spawns;
        /// <summary>The detached production combat collaborator.</summary>
        private readonly CombatSystem _combat;
        /// <summary>The detached production death and corpse collaborator.</summary>
        private readonly ZombieLifecycle _lifecycle;
        /// <summary>The detached production planting collaborator.</summary>
        private readonly PlantingSystem _planting;
        /// <summary>The known native kinds, including summoned backups and thrown imps.</summary>
        private readonly List<ZombieKind> _kinds = new List<ZombieKind>(32);
        /// <summary>The selected plant kinds whose production initialization is rehearsed.</summary>
        private readonly List<PlantKind> _seeds = new List<PlantKind>(16);
        /// <summary>The next plant initialization to rehearse.</summary>
        private int _seed;
        /// <summary>The next native kind to rehearse.</summary>
        private int _kind;
        /// <summary>The current birth, hit, death, release or grace-period phase.</summary>
        private int _phase;
        /// <summary>The detached actor retained through its hit and corpse lifecycle.</summary>
        private Zombie? _zombie;

        /// <summary>Connects a detached checkpoint to its own production collaborators without touching the live board.</summary>
        /// <param name="game">The independently restored production facade.</param>
        /// <param name="world">The independently restored checkpoint model.</param>
        /// <param name="spawns">Its production spawn owner.</param>
        /// <param name="combat">Its production damage owner.</param>
        /// <param name="lifecycle">Its production death owner.</param>
        /// <param name="planting">Its production planting owner.</param>
        internal SimulationPreparationRehearsal(GameSimulation game, SimulationWorld world, ZombieSpawnSystem spawns, CombatSystem combat,
            ZombieLifecycle lifecycle, PlantingSystem planting)
        {
            _game = game;
            _world = world;
            _spawns = spawns;
            _combat = combat;
            _lifecycle = lifecycle;
            _planting = planting;
            foreach (var zombie in world.Zombies)
            {
                AddKind(zombie.Kind);
            }
            if (world._adventure != null)
            {
                foreach (var wave in world._adventure.Waves)
                {
                    foreach (var kind in wave.Zombies)
                    {
                        AddKind(kind);
                    }
                }
                foreach (var seed in world._adventure.SelectedSeeds)
                {
                    if (!_seeds.Contains(seed))
                    {
                        _seeds.Add(seed);
                    }
                }
                world._adventure.Phase = AdventurePhase.Playing;
            }
            world.State = MatchState.Playing;
        }

        /// <summary>Collects known derived actor kinds without consuming either world's random stream.</summary>
        /// <param name="kind">The authored or currently alive native kind.</param>
        private void AddKind(ZombieKind kind)
        {
            if (!_kinds.Contains(kind))
            {
                _kinds.Add(kind);
            }
            if (kind == ZombieKind.Dancing && !_kinds.Contains(ZombieKind.BackupDancer))
            {
                _kinds.Add(ZombieKind.BackupDancer);
            }
            if ((kind == ZombieKind.Gargantuar || (int)kind == 32) && !_kinds.Contains(ZombieKind.Imp))
            {
                _kinds.Add(ZombieKind.Imp);
            }
        }

        /// <summary>Performs one independently bounded production lifetime operation.</summary>
        /// <returns>Whether all known kinds completed birth, hit, death, corpse release and delayed return.</returns>
        internal bool Step()
        {
            if (_seed < _seeds.Count)
            {
                var plant = _planting.PlaceAdventurePlant(_seeds[_seed++], 0, 0);
                if (plant != null)
                {
                    _planting.RemoveAdventurePlantAndStack(plant);
                }
                return false;
            }
            if (_kind >= _kinds.Count)
            {
                return true;
            }
            switch (_phase++)
            {
                case 0:
                    var row = 0;
                    for (var candidate = 0; candidate < _world.RowCount; candidate++)
                    {
                        if (_spawns.CanAdventureZombieSpawnInRow(_kinds[_kind], candidate))
                        {
                            row = candidate;
                            break;
                        }
                    }
                    _zombie = _spawns.SpawnAdventureZombie(_kinds[_kind], row, 6f, -1);
                    break;
                case 1:
                    _combat.ApplyAdventureZombieDamage(_zombie!, 1f, false, 0);
                    break;
                case 2:
                    _combat.ApplyAdventureZombieDamage(_zombie!, _zombie!.Health + _zombie.ArmorHealth + _zombie.ShieldHealth + 1f, true, 0);
                    _lifecycle.KillAdventureZombie(_zombie);
                    break;
                case 3:
                    ReleaseFeedback();
                    _zombie = null;
                    break;
                case 4:
                case 5:
                case 6:
                    _game.Update(.01f);
                    break;
                default:
                    _kind++;
                    _phase = 0;
                    break;
            }
            return false;
        }

        /// <summary>Releases detached presentation owners before the normal two-boundary lifetime grace period.</summary>
        private void ReleaseFeedback()
        {
            var p = _world.Presentation;
            p.Impacts.Clear();
            p.Corpses.Clear();
            p.SnowTrails.Clear();
            p.PlantBiteEffects.Clear();
            p.Explosions.Clear();
            p.ZombiePartDrops.Clear();
            p.ArmDrops.Clear();
            p.ZombieSounds.Clear();
            p.ZombieRiseEffects.Clear();
            p.ZombieSplashes.Clear();
            p.SquishedPlants.Clear();
            p.ZomboniParticles.Clear();
            if (_world._adventure != null)
            {
                _world._adventure._coins.Clear();
                _world._adventure._cobShells.Clear();
                _world._adventure.SeedFlashes.Clear();
            }
        }
    }
}
