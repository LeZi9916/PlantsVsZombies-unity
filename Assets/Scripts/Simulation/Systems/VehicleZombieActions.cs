#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns VehicleZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class VehicleZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal VehicleZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, SpecialPlantActions specialPlantActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _plantTimingSystem = plantTimingSystem;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _specialPlantActions = specialPlantActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Starts PC CatapultDeath's retained bounce, tire particle and engine smoke.</summary>
        /// <param name="zombie">The original vehicle receiving spike damage bit five.</param>
        internal void BeginAdventureCatapultFlatDeath(Zombie zombie)
        {
            var state = zombie.CatapultState ??= new AdventureCatapultState();
            state.FlatTires = true;
            state.DeathTicks = 280;
            state.SmokeStartAge = state.SmokeStartAge < 0f ? zombie.Age : state.SmokeStartAge;
            SetAdventureCatapultPhase(zombie, 1, 10);
            zombie.PresentationNativeVelocity = 0f;
            zombie.Speed = 0f;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(22);
            AddAdventureZomboniParticle(zombie, "ZamboniTire", 29f, 114f);
        }

        /// <summary>Emits the original catapult explosion once before removing its vehicle body.</summary>
        /// <param name="zombie">The catapult whose normal or punctured death has completed.</param>
        internal void ExplodeAdventureCatapult(Zombie zombie)
        {
            var state = zombie.CatapultState ??= new AdventureCatapultState();
            if (!state.Exploded)
            {
                state.Exploded = true;
                AddAdventureZomboniParticle(zombie, "CatapultExplosion", 80f, 60f);
                _zombieFeedbackSystem.RequestAdventureZombieFoley(23);
            }
        }

        /// <summary>Finds PC 541320's leftmost plant at least one hundred pixels ahead of the vehicle.</summary>
        /// <param name="zombie">The original catapult requesting a new aim.</param>
        /// <returns>The highest aerial-priority plant in the first eligible column, or null.</returns>
        internal Plant? FindAdventureCatapultTarget(Zombie zombie)
        {
            Plant? best = null;
            var x = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            foreach (var plant in _world.Plants)
            {
                if (plant.Row == zombie.Row && plant.Health > 0f && plant.BungeeState != 2
                    && !plant.PresentationBiteNotOnGround && plant.Kind != PlantKind.Spikeweed && plant.Kind != PlantKind.Spikerock
                    && x >= 140 + plant.Column * 80 && (best == null || plant.Column < best.Column))
                {
                    best = _biteSystem.AdventureAirAttackPlantAt(plant.Row, plant.Column);
                }
            }

            return best;
        }

        /// <summary>Changes PC catapult playback without resampling its retained walking velocity.</summary>
        /// <param name="zombie">The catapult changing original native phase.</param>
        /// <param name="phase">The original phase: normal, launching, reloading or dying.</param>
        /// <param name="blend">The original transition duration in native updates.</param>
        internal void SetAdventureCatapultPhase(Zombie zombie, int phase, int blend)
        {
            zombie.PresentationNativePhase = phase;
            zombie._presentationSpeedPhase = phase;
            zombie._presentationSpeedKind = (int)ZombieKind.Catapult;
            zombie.PresentationBodyBlendTicks = blend;
            zombie.PresentationAnimation = string.Empty;
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
        }

        /// <summary>Advances PC 541460's firing event, twenty shots and independent three-hundred-update cycle.</summary>
        /// <param name="zombie">The non-immobilized catapult receiving its native action update.</param>
        /// <returns>Whether launching or reloading prevents ordinary horizontal movement.</returns>
        internal bool AdvanceAdventureCatapult(Zombie zombie)
        {
            var state = zombie.CatapultState ??= new AdventureCatapultState();
            if (state.PhaseTicks > 0)
            {
                state.PhaseTicks--;
            }

            var phase = zombie.PresentationNativePhase;
            if (phase == 0)
            {
                if (AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) <= 650f
                    && state.Ammunition > 0 && FindAdventureCatapultTarget(zombie) != null)
                {
                    state.Fired = false;
                    state.PhaseTicks = 300;
                    SetAdventureCatapultPhase(zombie, 67, 0);
                }
            }
            else if (phase == 67)
            {
                if (!state.Fired && zombie.PresentationAnimationAge * 24f / 32f >= .545f)
                {
                    state.Fired = true;
                    FireAdventureBasketball(zombie, FindAdventureCatapultTarget(zombie));
                }

                if (zombie.PresentationAnimationAge * 24f + .0001f >= 32f)
                {
                    state.Ammunition--;
                    if (state.Ammunition == 0)
                    {
                        state.WalkingRate = 6f;
                        SetAdventureCatapultPhase(zombie, 0, 20);
                    }
                    else
                    {
                        SetAdventureCatapultPhase(zombie, 68, 20);
                    }
                }
            }
            else if (phase == 68 && state.PhaseTicks == 0)
            {
                if (FindAdventureCatapultTarget(zombie) != null)
                {
                    state.Fired = false;
                    state.PhaseTicks = 300;
                    SetAdventureCatapultPhase(zombie, 67, 20);
                }
                else
                {
                    state.WalkingRate = 6f;
                    SetAdventureCatapultPhase(zombie, 0, 20);
                }
            }

            return zombie.PresentationNativePhase == 67 || zombie.PresentationNativePhase == 68;
        }

        /// <summary>Creates PC 5411C0's real hostile lob, retaining aim even when its target disappears.</summary>
        /// <param name="zombie">The vehicle firing the original projectile.</param>
        /// <param name="target">The current target, or null for the native three-hundred-pixel fallback.</param>
        internal void FireAdventureBasketball(Zombie zombie, Plant? target)
        {
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var originX = nativeX + 113f;
            var originY = _biteSystem.AdventureBiteNativeY(zombie, nativeX) - 44f;
            var targetX = target != null ? 40 + target.Column * 80 : (int)(nativeX - 300f);
            var targetY = target != null ? _specialPlantActions.AdventureSquashCellY(target.Row, target.Column) : 0;
            var shot = _projectileSystem.RentPea();
            shot.Basketball = true;
            shot.PresentationInitialized = true;
            shot.Lobbed = true;
            shot.Row = zombie.Row;
            shot.FlightRow = zombie.Row;
            shot.Damage = 75f;
            shot.PresentationLobNativeX = (int)originX;
            shot.PresentationLobNativeY = (int)originY;
            shot.PresentationLobNativeZ = 0f;
            shot.PresentationLobNativeVelocityX = -Math.Max(40f, originX - targetX - 20f) / 120f;
            shot.PresentationLobVelocity = -7f + (targetY - originY) / 120f;
            shot.PresentationLaunchY = (int)originY;
            shot.X = shot.StartX = shot.PresentationPreviousX = (shot.PresentationLobNativeX - 40f) / 80f;
            shot.PresentationRotation = (float)_world._random.NextDouble() * (float)(Math.PI * 2d);
            shot.PresentationSpin = .05f + (float)_world._random.NextDouble() * .05f;
            var column = Math.Max(0, Math.Min(8, ((int)originX - 40) / 80));
            shot.PresentationStarShadowY = _specialPlantActions.AdventureSquashCellY(zombie.Row, column) + 67f;
            _world.Peas.Add(shot);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(37);
        }

        /// <summary>Integrates PC 47C880's hostile lob and resolves original aerial plant priority on actual descent.</summary>
        /// <param name="shot">The basketball retaining original float32 flight coordinates.</param>
        /// <param name="allowDamage">Whether the active board allows collision damage.</param>
        /// <returns>Whether collision, a ground impact or board bounds remove the ball.</returns>
        internal bool AdvanceAdventureBasketball(Pea shot, bool allowDamage)
        {
            var oldX = shot.PresentationLobNativeX;
            shot.PresentationNativeAge += .01f;
            shot.PresentationLobVelocity += .115f;
            shot.PresentationLobNativeX += shot.PresentationLobNativeVelocityX;
            shot.PresentationLobNativeZ += shot.PresentationLobVelocity;
            var remove = false;
            var contactHeight = _adventureModeRunner.Adventure.Level.IsWaterRow(shot.Row) ? 100f : 60f;
            if (shot.PresentationLobVelocity >= 0f && shot.PresentationLobNativeZ > contactHeight)
            {
                Plant? target = null;
                var left = (int)shot.PresentationLobNativeX;
                foreach (var plant in _world.Plants)
                {
                    if (plant.Row != shot.Row || plant.Health <= 0f || plant.BungeeState == 2 || plant.PresentationBiteNotOnGround)
                    {
                        continue;
                    }

                    var box = _biteSystem.AdventurePlantEatingRectangle(plant);
                    if (Math.Min(left + 40, box.X + box.Width) - Math.Max(left, box.X) > 8)
                    {
                        target = _biteSystem.AdventureAirAttackPlantAt(plant.Row, plant.Column);
                        break;
                    }
                }

                if (target != null && allowDamage)
                {
                    Plant? umbrella = null;
                    foreach (var plant in _world.Plants)
                    {
                        if (plant.Kind == PlantKind.UmbrellaLeaf && plant.Health > 0f && !plant.Asleep && plant.BungeeState != 2
                            && Math.Abs(plant.Row - target.Row) <= 1 && Math.Abs(plant.Column - target.Column) <= 1)
                        {
                            umbrella = plant;
                            break;
                        }
                    }

                    if (umbrella == null)
                    {
                        target.Health -= shot.Damage;
                        _plantTimingSystem.RecordAdventurePlantHit(target);
                        if (target.Health <= 0f)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(target);
                        }

                        _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                        remove = true;
                    }
                    else if (umbrella.PresentationUmbrellaAge >= .05f)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                        AddAdventureBasketballReflect(shot);
                        remove = true;
                    }
                    else if (umbrella.PresentationUmbrellaAge < 0f)
                    {
                        umbrella.PresentationUmbrellaAge = 0f;
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(73);
                    }
                }
                else if (shot.PresentationLobNativeZ > 80f)
                {
                    // Original DoImpact emits Splat Foley without a basketball splat particle.
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                    remove = true;
                }
            }

            // PC UpdateMotion rebases roof floor only after UpdateLobMotion's contact check.
            var slope = _adventureModeRunner.Adventure.Level.HasRoof ? (Math.Max(0f, 440f - shot.PresentationLobNativeX)
                - Math.Max(0f, 440f - oldX)) * .25f : 0f;
            shot.PresentationLobNativeY += slope;
            shot.PresentationLobNativeZ -= slope;
            shot.PresentationStarShadowY += slope;
            shot.X = (shot.PresentationLobNativeX - 40f) / 80f;
            shot.PresentationLobHeight = shot.PresentationLobNativeY + shot.PresentationLobNativeZ - shot.PresentationLaunchY;
            return remove || shot.PresentationLobNativeX > 800f || shot.PresentationLobNativeX + 40f < 0f;
        }

        /// <summary>Creates the original UmbrellaReflect particle at the ball's native top-left plus twenty pixels.</summary>
        /// <param name="shot">The basketball being deflected by an already-open umbrella.</param>
        internal void AddAdventureBasketballReflect(Pea shot)
        {
            _world.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle
            {
                Id = ++_world.Presentation._effectId, Seed = _world._random.Next(), Row = shot.Row, Particle = "UmbrellaReflect", Foreground = true,
                X = shot.PresentationLobNativeX + 20f, Y = shot.PresentationLobNativeY + 20f
            });
        }

        /// <summary>Starts PC ZamboniDeath's authored tire puncture and 280-update wheelie.</summary>
        /// <param name="zombie">The vehicle hit by the original spike damage flag.</param>
        internal void BeginAdventureZomboniFlatDeath(Zombie zombie)
        {
            var state = zombie.ZomboniState ??= new AdventureZomboniState();
            state.FlatTires = true;
            state.DeathTicks = 280;
            state.Wheelie2 = _world._random.Next(4) == 0
                && AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) < 600f;
            zombie.PresentationNativePhase = 1;
            zombie.PresentationAnimation = state.Animation;
            zombie.PresentationAnimationAge = 0f;
            zombie.PresentationNativeVelocity = 0f;
            zombie.Speed = 0f;
            zombie.Eating = false;
            zombie.FrozenRemaining = 0f;
            zombie.PresentationButter = 0f;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(22);
            AddAdventureZomboniParticle(zombie, "ZamboniTire", 29f, 114f);
        }

        /// <summary>Creates an unchanged original vehicle particle at the PC unscaled native origin.</summary>
        /// <param name="zombie">The source vehicle whose position and row determine the particle layer.</param>
        /// <param name="particle">The original particle definition name.</param>
        /// <param name="offsetX">The unscaled horizontal offset from mPosX.</param>
        /// <param name="offsetY">The unscaled vertical offset from mPosY.</param>
        internal void AddAdventureZomboniParticle(Zombie zombie, string particle, float offsetX, float offsetY)
        {
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            _world.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle
            {
                Id = ++_world.Presentation._effectId, Seed = _world._random.Next(), Row = zombie.Row, Particle = particle,
                X = nativeX + offsetX, Y = _biteSystem.AdventureBiteNativeY(zombie, nativeX) + offsetY
            });
        }

        /// <summary>Emits PC ZamboniDeath's original explosion once, without a generic corpse animation.</summary>
        /// <param name="zombie">The ice vehicle whose body is being removed.</param>
        internal void ExplodeAdventureZomboni(Zombie zombie)
        {
            var state = zombie.ZomboniState ??= new AdventureZomboniState();
            if (state.Exploded)
            {
                return;
            }

            state.Exploded = true;
            AddAdventureZomboniParticle(zombie, state.Wheelie2 ? "ZamboniExplosion2" : "ZamboniExplosion", 80f, 60f);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(23);
        }
    }
}
