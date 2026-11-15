#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ColdSystem responsibilities for one deterministic match.</summary>
    internal sealed class ColdSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ColdSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, CombatSystem combatSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _combatSystem = combatSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs request adventure ice launch.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "shot">The shot.</param>
        internal void RequestAdventureIceLaunch(Plant plant, Pea shot)
        {
            if (plant.Kind != PlantKind.SnowPea && plant.Kind != PlantKind.WinterMelon)
            {
                return;
            }

            // PC 4752E0: both types play native Foley3 AND Foley27, once at actual release.
            _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(27);
            _world.Presentation._nativeShootSuppressions++;
            _world.Presentation.IceLaunchSoundSerial++;
            if (plant.Kind == PlantKind.SnowPea)
            {
                BeginAdventureSnowTrail(shot);
            }
        }

        /// <summary>Performs can adventure chill.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting can adventure chill.</returns>
        internal bool CanAdventureChill(Zombie z)
        {
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            return global::PlantsVsZombies.Adventure.AdventureIceRules.CanChill((int)z.Kind, _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z), z.Health <= 0, z.Hypnotized, sled);
        }

        /// <summary>Performs apply adventure chill.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "iceTrap">The ice trap.</param>
        /// <returns>The resulting apply adventure chill.</returns>
        internal bool ApplyAdventureChill(Zombie z, bool iceTrap = false)
        {
            if (z == null || !CanAdventureChill(z))
            {
                return false;
            }

            if (z.ChillRemaining <= 0f)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(18);
                _world.Presentation.ChillOnsetSoundSerial++;
            }

            z.ChillRemaining = Math.Max(z.ChillRemaining, iceTrap ? 20f : 10f);
            return true;
        }

        /// <summary>Performs apply adventure ice trap.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting apply adventure ice trap.</returns>
        internal bool ApplyAdventureIceTrap(Zombie z)
        {
            if (z == null)
            {
                return false;
            }

            var wasCold = z.ChillRemaining > 0f || z.FrozenRemaining > 0f;
            var chilled = ApplyAdventureChill(z, true);
            if (!global::PlantsVsZombies.Adventure.AdventureIceRules.CanFreeze((int)z.Kind, _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z), chilled))
            {
                return false;
            }

            var pool = z.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            var offset = pool ? 0 : _world._random.Next(wasCold ? 101 : 201);
            z.FrozenRemaining = global::PlantsVsZombies.Adventure.AdventureIceRules.FreezeTicks(pool, wasCold, offset) * .01f;
            // PC 54E150 also applies 20 damage with bit1 (bypass held shield). Butter is a separate counter.
            _combatSystem.HitAdventureZombie(z, 20f, true, 1U);
            return true;
        }

        /// <summary>Performs apply adventure projectile cold.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "z">The z.</param>
        internal void ApplyAdventureProjectileCold(Pea shot, Zombie z)
        {
            if (shot.IsSnow && global::PlantsVsZombies.Adventure.AdventureIceRules.ProjectileReachesColdState(shot.Damage, z.ShieldHealth, shot.PiercesShield))
            {
                ApplyAdventureChill(z);
            }

            if (shot.Fire && z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult && (z.ShieldHealth <= 0f || z.Kind != ZombieKind.ScreenDoor && z.Kind != ZombieKind.Ladder))
            {
                z.ChillRemaining = 0f;
                z.FrozenRemaining = 0f;
            }
        }

        /// <summary>Performs adventure ice projectile top left.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        internal void AdventureIceProjectileTopLeft(Pea shot, out float x, out float y)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            x = 40f + shot.X * 80f;
            if (shot.Lobbed)
            {
                x = shot.PresentationLobNativeX;
                y = shot.PresentationLaunchY + shot.PresentationLobHeight;
                return;
            }

            var slope = _adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - shot.X) * 20f - 10f : 0f;
            x -= 14f;
            y = AdventureTorchwoodRules.IsPea(shot.SourceKind) && shot.PresentationLaunchY != 0f
                ? shot.PresentationLaunchY + shot.PresentationLaneOffset - 14f
                : 80f + shot.FlightRow * pitch + slope + 16f;
        }

        /// <summary>Performs begin adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        internal void BeginAdventureSnowTrail(Pea shot)
        {
            if (shot.PresentationSnowTrail != null)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            var trail = new global::PlantsVsZombies.Adventure.AdventureSnowTrail
            {
                Id = ++_world.Presentation._effectId,
                Seed = _world._random.Next(),
                Projectile = shot,
                Row = shot.Row,
                X = x + 8f,
                Y = y + 13f,
                PreviousX = x + 8f,
                PreviousY = y + 13f
            };
            shot.PresentationSnowTrail = trail;
            _world.Presentation.SnowTrails.Add(trail);
        }

        /// <summary>Performs update adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        internal void UpdateAdventureSnowTrail(Pea shot)
        {
            if (shot.IsSnow && !shot.Lobbed)
            {
                BeginAdventureSnowTrail(shot);
            }

            var trail = shot.PresentationSnowTrail;
            if (trail == null || trail.Stopped)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            trail.X = x + 8f;
            trail.Y = y + 13f;
            trail.Row = shot.Row;
            if (!shot.IsSnow || shot.Lobbed)
            {
                // PC ConvertToPea 47E230 calls AttachmentDie (40E620), NOT FadeOut.
                _world.Presentation.SnowTrails.Remove(trail);
                shot.PresentationSnowTrail = null;
            }
        }

        /// <summary>Performs stop adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        internal void StopAdventureSnowTrail(Pea shot)
        {
            var trail = shot.PresentationSnowTrail;
            if (trail != null && !trail.Stopped)
            {
                trail.StopAge = trail.Age;
                trail.Projectile = null;
            }
        }

        /// <summary>Performs advance adventure ice presentation.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureIcePresentation(float dt)
        {
            for (var i = _world.Presentation.SnowTrails.Count - 1; i >= 0; i--)
            {
                var trail = _world.Presentation.SnowTrails[i];
                trail.PreviousX = trail.X;
                trail.PreviousY = trail.Y;
                trail.Age += dt;
                if (trail.Stopped && trail.Age - trail.StopAge >= .5f)
                {
                    _world.Presentation.SnowTrails.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs record adventure ice impact.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "impact">The impact.</param>
        internal void RecordAdventureIceImpact(Pea shot, Zombie? zombie, global::PlantsVsZombies.Adventure.AdventureImpact impact)
        {
            StopAdventureSnowTrail(shot);
            if (!shot.IsSnow)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            if (shot.Lobbed)
            {
                // PC winter-melon impact is particle1 at the lobbed ground/contact coordinates +(30,30).
                // mPosX-mVelX and mPosY+mPosZ-mVelY-mVelZ are the previous native integration point.
                var px = shot.PresentationLobNativeX - shot.PresentationLobNativeVelocityX;
                var py = shot.PresentationLaunchY + shot.PresentationLobFloorOffset
                    + shot.PresentationLobContactHeight - shot.PresentationLobVelocity;
                x = px + 30f;
                y = py + 30f;
            }
            else
            {
                var zx = zombie == null ? 0 : (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var zy = zombie == null ? 0 : (int)_biteSystem.AdventureBiteNativeY(zombie, zx);
                global::PlantsVsZombies.Adventure.AdventureIceRules.SnowImpact(x, y, zombie != null, zx, zy, zombie == null ? 0 : _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie), shot.Backward, out x, out y);
                if (zombie != null)
                {
                    impact.AttachedZombie = zombie;
                    impact.NativeAttachedX = x - zx;
                    impact.NativeAttachedY = y - zy;
                }
            }

            impact.NativeIcePoint = true;
            impact.NativeParticleX = x;
            impact.NativeParticleY = y;
        }

        /// <summary>Performs adventure ice impact point.</summary>
        /// <param name = "impact">The impact.</param>
        /// <param name = "fraction">The fraction.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        internal void AdventureIceImpactPoint(global::PlantsVsZombies.Adventure.AdventureImpact impact, float fraction, out float x, out float y)
        {
            x = impact.NativeParticleX;
            y = impact.NativeParticleY;
            var z = impact.AttachedZombie;
            if (z == null)
            {
                return;
            }

            var grid = z.PresentationPreviousX + (z.X - z.PresentationPreviousX) * Math.Max(0f, Math.Min(1f, fraction));
            x = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(grid, AdventureDancerRules.Scale(z));
            y = _biteSystem.AdventureBiteNativeY(z, x) + impact.NativeAttachedY;
            x += impact.NativeAttachedX;
        }
    }
}
