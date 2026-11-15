#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns CombatSystem responsibilities for one deterministic match.</summary>
    internal sealed class CombatSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BalloonZombieActions operations.</summary>
        private BalloonZombieActions _balloonZombieActions = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides LadderSystem operations.</summary>
        private LadderSystem _ladderSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides VehicleZombieActions operations.</summary>
        private VehicleZombieActions _vehicleZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal CombatSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="balloonZombieActions">The BalloonZombieActions collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="ladderSystem">The LadderSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="vehicleZombieActions">The VehicleZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BalloonZombieActions balloonZombieActions, BiteSystem biteSystem, ExplosionEffects explosionEffects, LadderSystem ladderSystem, LegacySimulation legacySimulation, VehicleZombieActions vehicleZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _balloonZombieActions = balloonZombieActions;
            _biteSystem = biteSystem;
            _explosionEffects = explosionEffects;
            _ladderSystem = ladderSystem;
            _legacySimulation = legacySimulation;
            _vehicleZombieActions = vehicleZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Performs blast adventure.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "rows">The rows.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "burned">The burned.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        internal void BlastAdventure(float x, int row, float width, int rows, float damage, bool burned = false, uint damageFlags = 0)
        {
            foreach (Zombie z in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (!z.Hypnotized && Math.Abs(z.Row - row) <= rows && Math.Abs(z.X - x) <= width)
                {
                    _explosionEffects.HitAdventureExplosionZombie(z, damage, burned, damageFlags);
                }
            }
        }

        /// <summary>Gets the native plant or aimed blast center's vertical board coordinate.</summary>
        /// <param name="x">The horizontal grid coordinate of the blast center.</param>
        /// <param name="row">The vertical grid coordinate of the blast center.</param>
        /// <returns>The original board pixel coordinate, including roof slope.</returns>
        internal int AdventureBlastCenterY(float x, float row)
        {
            return (int)(120f + row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5.5f - x) * 20f - 10f : 0f));
        }

        /// <summary>Applies a Cherry Bomb or Doom-shroom's original radius and row restriction.</summary>
        /// <param name="plant">The plant whose native eighty-pixel bounds define the center.</param>
        /// <param name="radius">The original circular radius in board pixels.</param>
        /// <param name="rows">The maximum original row distance.</param>
        internal void BlastAdventureAshPlant(Plant plant, int radius, int rows)
        {
            // PC Plant::DoSpecial 0x474B10: Cherry 115/1, Doom 250/3; target mask 0x7F.
            var x = plant.Column + .5f;
            BlastAdventureRadius(80 + plant.Column * 80, AdventureBlastCenterY(x, plant.Row), plant.Row, radius, rows, 0x7F);
        }

        /// <summary>Checks the native EffectedByDamage gate used by plant and projectile target masks.</summary>
        /// <param name="zombie">The candidate zombie.</param>
        /// <param name="targetFlags">The original damage range mask, separate from damage behavior flags.</param>
        /// <returns>Whether the original mask admits this live zombie's current movement phase.</returns>
        internal bool CanTakeAdventureAshDamage(Zombie zombie, uint targetFlags)
        {
            var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
            if (zombie.Hypnotized != ((targetFlags & 0x80) != 0) || zombie.Health <= 0f || (targetFlags & 0x20) == 0 && phase >= 1 && phase <= 3
                || zombie.PresentationNativeHeight == 9
                || zombie.Kind == ZombieKind.Bungee && phase != 6 && phase != 7
                || zombie.Kind == ZombieKind.Boss && !_adventureModeRunner.Adventure.BossHeadVulnerable
                || zombie.BobsledState?.Position > 0)
            {
                return false;
            }

            // DropRemaining also pauses pool ambushes. Only a linked bungee passenger maps to native height nine.
            if (zombie.DropRemaining > 0f)
            {
                foreach (var carrier in _world.Zombies)
                {
                    if (carrier.Kind == ZombieKind.Bungee && carrier.BungeeCarrier && carrier.LinkedZombieId == zombie.Id)
                    {
                        return false;
                    }
                }
            }

            // PC 0x54D810 returns directly for these transient phases before the screen-edge gate.
            if (phase == 12 || phase == 71 || phase == 33 || phase == 34 || phase == 35 || phase == 52
                || phase == 54 || phase == 58 || phase == 74 || phase == 14 || phase == 19 || phase == 50)
            {
                return (targetFlags & 0x10) != 0;
            }

            if (zombie.Kind != ZombieKind.Bobsled && AdventureZombieTargetingRules.BodyRectangle(zombie).X > 800)
            {
                return false;
            }

            var submerged = zombie.Kind == ZombieKind.Snorkel && zombie.Submerged && !zombie.Eating;
            var underground = zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0;
            return (targetFlags & 4) != 0 && submerged || (targetFlags & 0x40) != 0 && underground
                || (targetFlags & 2) != 0 && zombie.Airborne
                || (targetFlags & 1) != 0 && !zombie.Airborne && !submerged && !underground;
        }

        /// <summary>Applies the original circle-to-body-rectangle intersection used by radial ash attacks.</summary>
        /// <param name="x">The blast center's native horizontal pixel coordinate.</param>
        /// <param name="y">The blast center's native vertical pixel coordinate.</param>
        /// <param name="row">The blast's original board row.</param>
        /// <param name="radius">The circular radius in board pixels.</param>
        /// <param name="rows">The maximum original row distance.</param>
        /// <param name="targetFlags">The original damage range mask.</param>
        internal void BlastAdventureRadius(int x, int y, int row, int radius, int rows, uint targetFlags)
        {
            // PC Board::KillAllZombiesInRadius 0x4294C0 and GetCircleRectOverlap 0x428460.
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (!CanTakeAdventureAshDamage(zombie, targetFlags)
                    || zombie.Kind != ZombieKind.Boss && Math.Abs(zombie.Row - row) > rows)
                {
                    continue;
                }

                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX));
                var top = body.Y + zombie.PresentationBiteBodyY;
                var clip = zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0;
                if (zombie.GraveRiseTicks > 0)
                {
                    var poolRise = zombie.PoolState != null && zombie.PresentationInPool == true;
                    var altitude = (int)(poolRise ? AdventureZombiePoolRules.BodyAltitude(zombie) : AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks));
                    top -= altitude;
                    clip = -altitude + (poolRise ? 0 : Math.Min(zombie.GraveRiseTicks, 40));
                }
                else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
                {
                    var altitude = AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks);
                    top -= altitude;
                    clip = -altitude;
                }

                var height = body.Height - clip;
                var dx = Math.Max(0, Math.Max(body.X - x, x - body.X - body.Width));
                var dy = y < top ? top - y : Math.Max(0, y - top - height);
                if (dx * dx + dy * dy <= radius * radius)
                {
                    _explosionEffects.HitAdventureExplosionZombie(zombie, 1800f, true, 0x12);
                }
            }

            _ladderSystem.RemoveAdventureLaddersInBlast(x, y, rows);
        }

        /// <summary>Burns eligible zombies throughout the original Jalapeno row without a horizontal cutoff.</summary>
        /// <param name="row">The original board row ignited by the plant.</param>
        internal void BurnAdventureRow(int row)
        {
            // PC Plant::BurnRow 0x474920 removes freeze/chill before BurnBody, preserving equipment.
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if ((zombie.Row == row || zombie.Kind == ZombieKind.Boss) && CanTakeAdventureAshDamage(zombie, 0x7F))
                {
                    zombie.FrozenRemaining = 0f;
                    zombie.ChillRemaining = 0f;
                    _explosionEffects.HitAdventureExplosionZombie(zombie, 1800f, true, 0x12);
                }
            }

            foreach (var plant in _world.Plants)
            {
                if (plant.Row == row)
                {
                    plant.HasLadder = false;
                }
            }
        }

        /// <summary>Performs hit adventure zombie.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "bypassShield">The bypass shield.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        internal void HitAdventureZombie(Zombie z, float damage, bool bypassShield = false, uint damageFlags = 0)
        {
            if (!_world.Zombies.Contains(z) || z.GraveRiseTicks > 0
                || z.PresentationNativePhase == 50 && (damageFlags & 0x10) == 0
                || z.ZomboniState?.FlatTires == true || z.CatapultState?.FlatTires == true || z.BobsledState?.Position > 0)
            {
                return;
            }

            ApplyAdventureZombieDamage(z, damage, bypassShield, damageFlags);
        }

        /// <summary>Applies damage after the caller has admitted the zombie through its native target gate.</summary>
        /// <param name="z">The admitted live zombie.</param>
        /// <param name="damage">The damage before equipment absorption.</param>
        /// <param name="bypassShield">Whether ordinary held-shield absorption is bypassed.</param>
        /// <param name="damageFlags">The original TakeDamage behavior mask.</param>
        internal void ApplyAdventureZombieDamage(Zombie z, float damage, bool bypassShield, uint damageFlags)
        {
            _zombieFeedbackSystem.AdventurePresentationZombieHit(z, damage, bypassShield, damageFlags);
            if (z.Airborne)
            {
                var flyingDamage = Math.Min(z.FlyingHealth, (int)damage);
                z.FlyingHealth -= flyingDamage;
                damage -= flyingDamage;
                if (z.FlyingHealth == 0)
                {
                    _balloonZombieActions.LandAdventureBalloon(z, damageFlags);
                    if (!_world.Zombies.Contains(z))
                    {
                        return;
                    }
                }
            }

            // PC TakeDamage bit 1 damages the held shield and also sends the full
            // original damage to the helmet/body; ordinary piercing skips the shield.
            if (!bypassShield || (damageFlags & 2U) != 0)
            {
                var shield = Math.Min(z.ShieldHealth, damage);
                z.ShieldHealth -= shield;
                if ((damageFlags & 2U) == 0)
                {
                    damage -= shield;
                }
            }

            var armor = Math.Min(z.ArmorHealth, damage);
            z.ArmorHealth -= armor;
            z.Health -= damage - armor;
            _zombieFeedbackSystem.AdventurePresentationZombieEquipmentDamaged(z, damageFlags);
            _zombieFeedbackSystem.AdventurePresentationZombieBodyDamaged(z, damageFlags);
            _legacySimulation.Raise(GameEvent.ZombieHit);
            if (z.Kind == ZombieKind.Zomboni && (damageFlags & 32U) != 0)
            {
                _vehicleZombieActions.BeginAdventureZomboniFlatDeath(z);
            }
            else if (z.Kind == ZombieKind.Catapult && (damageFlags & 32U) != 0)
            {
                _vehicleZombieActions.BeginAdventureCatapultFlatDeath(z);
            }
            else if (z.Health <= 0)
            {
                z.Health = 0f;
                _zombieLifecycle.KillAdventureZombie(z, damageFlags);
            }
        }
    }
}
