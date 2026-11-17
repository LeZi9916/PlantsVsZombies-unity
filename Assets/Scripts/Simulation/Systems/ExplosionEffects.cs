#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ExplosionEffects responsibilities for one deterministic match.</summary>
    internal sealed class ExplosionEffects
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ExplosionEffects(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        internal void Connect(CombatSystem combatSystem)
        {
            _combatSystem = combatSystem;
        }

        /// <summary>Performs adventure presentation explosion.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        internal void AdventurePresentationExplosion(PlantKind kind, float x, int row)
        {
            global::PlantsVsZombies.Adventure.AdventureExplosionEffect effect;
            switch (kind)
            {
                case PlantKind.CherryBomb:
                case PlantKind.ExplodeONut:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.Powie;
                    break;
                case PlantKind.DoomShroom:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.Doom;
                    break;
                case PlantKind.IceShroom:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.IceTrap;
                    break;
                case PlantKind.PotatoMine:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.PotatoMine;
                    break;
                case PlantKind.Jalapeno:
                    _world.Presentation.ScreenShake.Start(3, -4);
                    AdventurePresentationJalapenoFire(row);
                    return;
                default:
                    return; // Cob already creates BlastMark + PopcornSplash, not Powie.
            }

            if (kind != PlantKind.IceShroom)
            {
                // PC Plant::DoSpecial 0x474D16 and 0x474DE6 restart this same board shake.
                _world.Presentation.ScreenShake.Start(3, -4);
            }

            var pooledRecord1 = _world.LifetimePool.Rent<AdventureExplosion>();
            pooledRecord1.Id = ++_world.Presentation._effectId;
            pooledRecord1.Seed = _world._random.Next();
            pooledRecord1.Row = row;
            pooledRecord1.X = x;
            pooledRecord1.Effect = effect;
            _world.Presentation.Explosions.Add(pooledRecord1);
        }

        /// <summary>Performs adventure presentation potato mine rise.</summary>
        /// <param name = "plant">The plant.</param>
        internal void AdventurePresentationPotatoMineRise(Plant plant)
        {
            var pooledRecord2 = _world.LifetimePool.Rent<AdventureExplosion>();
            pooledRecord2.Id = ++_world.Presentation._effectId;
            pooledRecord2.Seed = _world._random.Next();
            pooledRecord2.Row = plant.Row;
            pooledRecord2.X = plant.Column + .5f;
            pooledRecord2.Effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.PotatoMineRise;
            _world.Presentation.Explosions.Add(pooledRecord2);
        }

        /// <summary>Creates one of the original Grave Buster particle systems above the plant's cell.</summary>
        /// <param name="plant">The Grave Buster that owns the particles.</param>
        /// <param name="effect">Either the eating (GraveBuster) or the finishing (GraveBusterDie) effect.</param>
        internal void AdventurePresentationGraveBuster(Plant plant, global::PlantsVsZombies.Adventure.AdventureExplosionEffect effect)
        {
            var pooledRecord3 = _world.LifetimePool.Rent<AdventureExplosion>();
            pooledRecord3.Id = ++_world.Presentation._effectId;
            pooledRecord3.Seed = _world._random.Next();
            pooledRecord3.Row = plant.Row;
            pooledRecord3.X = plant.Column + .5f;
            pooledRecord3.Effect = effect;
            _world.Presentation.Explosions.Add(pooledRecord3);
        }

        /// <summary>Starts the attached rock particles when the Grave Buster finishes landing (Plant::UpdateGraveBuster).</summary>
        /// <param name="plant">The Grave Buster whose timer was just advanced.</param>
        internal void StartAdventureGraveBusterEating(Plant plant)
        {
            if (plant.GraveBusterEatingStarted || !global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.IsEating(plant.ActionTimer))
            {
                return;
            }

            plant.GraveBusterEatingStarted = true;
            AdventurePresentationGraveBuster(plant, global::PlantsVsZombies.Adventure.AdventureExplosionEffect.GraveBuster);
        }

        /// <summary>Performs adventure explosion random float.</summary>
        /// <param name = "range">The range.</param>
        /// <returns>The resulting adventure explosion random float.</returns>
        internal float AdventureExplosionRandomFloat(float range)
        {
            return (float)(_world._random.Next() / 2147483647.0 * range);
        }

        /// <summary>Performs adventure presentation jalapeno fire.</summary>
        /// <param name = "row">The row.</param>
        internal void AdventurePresentationJalapenoFire(int row)
        {
            // PC Board::DoFwoosh 429060: replace a previous row's active flame tracks,
            // twelve placements from x=10 to 760, original fire (reanim type 34).
            _world.Presentation.JalapenoFires.RemoveAll(f => f.Row == row);
            var fire = new global::PlantsVsZombies.Adventure.AdventureJalapenoFire
            {
                Id = ++_world.Presentation._effectId,
                Row = row
            };
            for (var i = 0; i < 12; i++)
            {
                var speed = (float)(AdventureExplosionRandomFloat(.59999996f) + .699999988079071);
                var scale = (float)(AdventureExplosionRandomFloat(.20000005f) + .8999999761581421);
                if (_world._random.Next(2) == 0)
                {
                    scale = -scale;
                }

                fire.Flames.Add(new global::PlantsVsZombies.Adventure.AdventureJalapenoFlame { X = (float)(750.0 * i / 11.0 + 10.0), Rate = 12f * speed, ScaleX = scale });
            }

            _world.Presentation.JalapenoFires.Add(fire);
        }

        /// <summary>Performs advance adventure explosion presentation.</summary>
        internal void AdvanceAdventureExplosionPresentation()
        {
            for (var i = _world.Presentation.Explosions.Count - 1; i >= 0; i--)
            {
                if (++_world.Presentation.Explosions[i].Ticks >= _world.Presentation.Explosions[i].DurationTicks)
                {
                    _world.Presentation.Explosions.RemoveAt(i);
                }
            }

            foreach (var fire in _world.Presentation.JalapenoFires)
            {
                fire.Ticks++;
                var remaining = Math.Max(0, 100 - fire.Ticks);
                // PC 429250 -> TodAnimateCurve(50,0,remaining,12,0,Linear).
                // 526F20 rounds positive results by +.5 then truncation.
                var stillFlaming = (int)(12.0 * Math.Min(50, remaining) / 50.0 + .5);
                for (var j = 0; j < 12 - stillFlaming; j++)
                {
                    if (fire.Flames[j].DoneTick < 0)
                    {
                        fire.Flames[j].DoneTick = fire.Ticks;
                    }
                }
            }

            // anim_done has ten frames; PlayOnceFullLastFrame at the PC's fixed 15 fps.
            for (var i = _world.Presentation.JalapenoFires.Count - 1; i >= 0; i--)
            {
                var fire = _world.Presentation.JalapenoFires[i];
                var finished = true;
                foreach (var flame in fire.Flames)
                {
                    if (flame.DoneTick < 0 || (fire.Ticks - flame.DoneTick) * .01f < 10f / 15f)
                    {
                        finished = false;
                        break;
                    }
                }

                if (finished)
                {
                    _world.Presentation.JalapenoFires.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs hit adventure explosion zombie.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "burned">The burned.</param>
        /// <param name = "flags">The flags.</param>
        internal void HitAdventureExplosionZombie(Zombie zombie, float damage, bool burned, uint flags)
        {
            if (!_world.Zombies.Contains(zombie))
            {
                return;
            }

            if (!burned)
            {
                _combatSystem.HitAdventureZombie(zombie, damage, true, flags);
                return;
            }

            // PC 42954D uses BurnBody (54E920); Potato takes the separate damage(1800,0x12)
            // branch. BurnBody compares BODY health, so thick armor cannot absorb an ash kill.
            if (damage == 1800f && zombie.Kind != ZombieKind.Boss && zombie.Health < 1800f)
            {
                zombie.PresentationDeath = global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned;
                zombie.ArmorHealth = 0;
                zombie.ShieldHealth = 0;
                _combatSystem.ApplyAdventureZombieDamage(zombie, damage, true, 0x12);
            }
            else
            {
                _combatSystem.ApplyAdventureZombieDamage(zombie, damage, true, 0x12);
            }
        }

        // Hook for the modes owner: PC explosive bowling nut also calls Powie (not PotatoMine).
        /// <summary>Performs adventure presentation explode onut.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        internal void AdventurePresentationExplodeONut(float x, int row)
        {
            AdventurePresentationExplosion(PlantKind.ExplodeONut, x, row);
        }
    }
}
