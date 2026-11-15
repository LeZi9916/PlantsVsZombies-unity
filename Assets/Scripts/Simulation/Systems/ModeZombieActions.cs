#nullable enable

using PlantsVsZombies.Modes;
using PlantsVsZombies.Adventure;

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
        /// <summary>Provides original plant hit and row positions.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides original layered plant queries.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides the original Fwoosh row presentation.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides the original independent child clocks and Foley requests.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
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
        /// <param name="biteSystem">The original plant collision and crush owner.</param>
        /// <param name="boardQueries">The original layered plant lookup owner.</param>
        /// <param name="explosionEffects">The original row fire presentation owner.</param>
        /// <param name="zombieFeedbackSystem">The native animation and Foley owner.</param>
        internal void Connect(CombatSystem combatSystem, IndependentModeRunner independentModeRunner, LegacySimulation legacySimulation, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, ZombieLifecycle zombieLifecycle, BiteSystem biteSystem, BoardQueries boardQueries, ExplosionEffects explosionEffects, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _combatSystem = combatSystem;
            _independentModeRunner = independentModeRunner;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _zombieLifecycle = zombieLifecycle;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _explosionEffects = explosionEffects;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs is mode zombie visible.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting is mode zombie visible.</returns>
        internal bool IsModeZombieVisible(Zombie zombie)
        {
            return _world.ModeSession == null || _world.ModeSession.Definition.Mechanic != ModeMechanic.Invisible;
        }

        /// <summary>Advances original hostile peas before newly allocated zombie projectiles and then updates native head actions.</summary>
        /// <param name="dt">The fixed native update duration.</param>
        internal void TickModeSpecialZombies(float dt)
        {
            AdvanceHeadProjectiles();
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                var state = zombie.PlantZombieState;
                if (state == null)
                {
                    if (_independentModeRunner.RequiredModeSession.Definition.Mechanic == ModeMechanic.Speed && zombie.ModeHead != -2)
                    {
                        zombie.Speed *= 2f;
                        zombie.ModeHead = -2;
                    }
                    continue;
                }
                if (state.ImmobilizedAtTickStart || zombie.FrozenRemaining > 0 || zombie.PresentationButter > 0)
                {
                    continue;
                }
                if (state.Counter > 0)
                {
                    state.Counter--;
                }
                if (state.NativeKind == 30)
                {
                    AdvanceSquashHead(zombie, state);
                    continue;
                }
                if (zombie.Headless)
                {
                    continue;
                }
                if (state.NativeKind == 26)
                {
                    if (state.Counter == 35)
                    {
                        state.Play("anim_shooting", 35, false, 20);
                    }
                    else if (state.Counter == 0)
                    {
                        state.Play("anim_head_idle", 15, false, 20);
                        FireHeadPea(zombie, true);
                        state.Counter = 150;
                    }
                }
                else if (state.NativeKind == 29)
                {
                    if (state.Counter == 100)
                    {
                        state.Play("anim_shooting", 38, false, 20);
                    }
                    else if (state.Counter == 18 || state.Counter == 35 || state.Counter == 51 || state.Counter == 68)
                    {
                        FireHeadPea(zombie, false);
                    }
                    else if (state.Counter == 0)
                    {
                        state.Play("anim_head_idle", 15, false, 20);
                        state.Counter = 150;
                    }
                }
                else if (state.NativeKind == 28 && state.Counter == 0)
                {
                    // PC 543040 burns plants even after mind control; the mobile port's extra BurnRow branch is absent.
                    _explosionEffects.AdventurePresentationExplosion(PlantKind.Jalapeno, zombie.X, zombie.Row);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(65);
                    foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
                    {
                        if (plant.Row == zombie.Row && !plant.PresentationBiteNotOnGround && plant.BungeeState != 2
                            && !(plant.Kind == PlantKind.Squash && plant.SquashPresentation?.NativePhase >= 5))
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(plant);
                        }
                    }
                    zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    _zombieLifecycle.KillAdventureZombie(zombie);
                    _legacySimulation.Raise(GameEvent.JalapenoExplosion);
                }
            }
        }

        /// <summary>Creates PC 542E50/5431D0's original hostile pea at the authored head bone.</summary>
        /// <param name="zombie">The shooting original zombie.</param>
        /// <param name="subtractAltitude">Whether the original pea-head branch subtracts its current altitude.</param>
        private void FireHeadPea(Zombie zombie, bool subtractAltitude)
        {
            var clip = _zombieFeedbackSystem.AdventurePresentationZombieAnimation(zombie);
            AdventurePlantZombieHeadTrack.Sample(clip, zombie.PresentationAnimationAge,
                _zombieFeedbackSystem.SynchronizeAdventureZombieRate(zombie), out var headX, out var headY);
            var x = (int)(AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) + headX - 9f);
            var y = (int)(_biteSystem.AdventureBiteNativeY(zombie, x) + headY + 6f - (subtractAltitude ? zombie.PresentationAltitude : 0f));
            var level = _independentModeRunner.RequiredModeSession.Game.Adventure.Level;
            _independentModeRunner.RequiredModeSession.HeadShots.Add(new ModeHeadShot
            {
                NativeX = x, PreviousNativeX = x, NativeY = y, X = (x - 40f) / 80f, Row = zombie.Row,
                NativeShadowY = 80 + zombie.Row * (level.HasPool || level.HasRoof ? 85 : 100) + 67
            });
            _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
        }

        /// <summary>Runs PC 543520's detached Squash phases and original whole-cell crush.</summary>
        /// <param name="zombie">The original squash-headed zombie body.</param>
        /// <param name="state">The original child and phase state.</param>
        private void AdvanceSquashHead(Zombie zombie, AdventurePlantZombieState state)
        {
            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var y = _biteSystem.AdventureBiteNativeY(zombie, x);
            var column = Math.Max(0, Math.Min(8, (int)((x - 40) / 80)));
            var cellX = 40 + column * 80;
            if (!zombie.Headless && zombie.Eating && state.SquashPhase == 92)
            {
                zombie.Eating = false;
                zombie.Headless = true;
                state.Detached = true;
                state.SquashPhase = zombie.PresentationNativePhase = 93;
                state.Counter = 95;
                state.Play("anim_jumpup", 24, false, 20);
                zombie.PresentationAnimation = "anim_idle";
                zombie.PresentationAnimationAge = 0;
                zombie._presentationWalkPhase = 0;
                zombie._presentationWalkLoops = 0;
                zombie.PresentationBodyBlendTicks = 20;
            }
            if (state.SquashPhase == 93)
            {
                state.DetachedX = x + 6 + AdventureIntroPresentation.Curve(20, 50, 70 - state.Counter, 0, (int)(cellX - x), 4);
                state.DetachedY = y - 21 + AdventureIntroPresentation.Curve(20, 50, 70 - state.Counter, 0, -20, 4);
                if (state.Counter == 0)
                {
                    state.SquashPhase = zombie.PresentationNativePhase = 94;
                    state.Counter = 10;
                    state.Play("anim_jumpdown", 60, false, 0);
                }
            }
            if (state.SquashPhase == 94)
            {
                state.DetachedX = cellX + 6;
                state.DetachedY = y - 21 + AdventureIntroPresentation.Curve(0, 10, 10 - state.Counter, -20, 74, 1);
                if (state.Counter == 2)
                {
                    _biteSystem.CrushAdventurePlantCell(zombie.Row, column, false);
                }
                if (state.Counter == 0)
                {
                    state.SquashPhase = zombie.PresentationNativePhase = 95;
                    state.Counter = 100;
                    _world.Presentation.ScreenShake.Start(1, 4);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(40);
                }
            }
            if (state.SquashPhase == 95 && state.Counter == 0)
            {
                state.HeadRemoved = true;
                _combatSystem.HitAdventureZombie(zombie, 1800, damageFlags: 9);
            }
        }

        /// <summary>Moves original backwards peas and applies their short-plant immunity and native collision rectangle.</summary>
        private void AdvanceHeadProjectiles()
        {
            var shots = _independentModeRunner.RequiredModeSession.HeadShots;
            for (var index = shots.Count - 1; index >= 0; index--)
            {
                var shot = shots[index];
                shot.PreviousNativeX = shot.NativeX;
                shot.NativeX -= 3.33f;
                shot.X = (shot.NativeX - 40) / 80;
                Plant? target = null;
                foreach (var plant in _world.Plants)
                {
                    if (plant.Row != (int)shot.Row || plant.Kind == PlantKind.PuffShroom || plant.Kind == PlantKind.SunShroom
                        || plant.Kind == PlantKind.PotatoMine || plant.Kind == PlantKind.Spikeweed || plant.Kind == PlantKind.Spikerock
                        || plant.Kind == PlantKind.LilyPad)
                    {
                        continue;
                    }
                    var rectangle = _biteSystem.AdventurePlantEatingRectangle(plant);
                    var overlap = Math.Min(shot.NativeX + 40, rectangle.X + rectangle.Width) - Math.Max(shot.NativeX - 15, rectangle.X);
                    if (overlap > 8)
                    {
                        target = _boardQueries.AdventurePlantAt(plant.Row, plant.Column, PlantLayer.Armor)
                            ?? _boardQueries.AdventurePlantAt(plant.Row, plant.Column, PlantLayer.Main)
                            ?? _boardQueries.AdventurePlantAt(plant.Row, plant.Column, PlantLayer.Support);
                        break;
                    }
                }
                if (target != null)
                {
                    target.Health -= shot.Damage;
                    target.PresentationEatenFlashTicks = Math.Max(target.PresentationEatenFlashTicks, 25);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                    _world.Presentation.Impacts.Add(new AdventureImpact
                    {
                        Id = ++_world.Presentation._effectId, Seed = _world._random.Next(), Kind = PlantKind.Peashooter,
                        X = shot.X, Row = shot.Row, NativeIcePoint = true,
                        NativeParticleX = shot.NativeX - 3, NativeParticleY = shot.NativeY + 17
                    });
                    if (target.Health <= 0)
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(target);
                    }
                    shots.RemoveAt(index);
                }
                else if (shot.NativeX < -40)
                {
                    shots.RemoveAt(index);
                }
            }
        }
    }
}
