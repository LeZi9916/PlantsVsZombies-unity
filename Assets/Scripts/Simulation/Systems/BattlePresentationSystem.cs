#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BattlePresentationSystem responsibilities for one deterministic match.</summary>
    internal sealed class BattlePresentationSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ColdSystem operations.</summary>
        private ColdSystem _coldSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides GraveSystem operations.</summary>
        private GraveSystem _graveSystem = null!;
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
        internal BattlePresentationSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="coldSystem">The ColdSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="graveSystem">The GraveSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, ColdSystem coldSystem, ExplosionEffects explosionEffects, GraveSystem graveSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, SpecialPlantActions specialPlantActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _coldSystem = coldSystem;
            _explosionEffects = explosionEffects;
            _graveSystem = graveSystem;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _specialPlantActions = specialPlantActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Gets or sets adventure presentation remainder.</summary>
        internal float AdventurePresentationRemainder
        {
            get
            {
                return _world.State == MatchState.Playing && _world.IsAdventure && _adventureModeRunner.Adventure.Phase == global::PlantsVsZombies.Adventure.AdventurePhase.Playing ? (float)Math.Max(0d, Math.Min(SimulationWorld.SimulationStep, _world.ModeSession == null ? _world._accumulatedTime : _world._modeAccumulated)) : 0f;
            }
        }

        /// <summary>Performs adventure presentation tick.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdventurePresentationTick(float dt)
        {
            if (_world.Presentation.VaseScreenFlashTicks >= 0 && _world.Presentation.VaseScreenFlashTicks < 50)
            {
                _world.Presentation.VaseScreenFlashTicks++;
            }

            // PC Board::Update consumes one Rand(2) only while the decremented counter is nonzero.
            var shake = _world.Presentation.ScreenShake;
            shake.Tick(shake.TicksRemaining > 1 && _world._random.Next(2) == 0);
            for (var i = _world.Presentation._plantSoundCues.Count - 1; i >= 0; i--)
            {
                var cue = _world.Presentation._plantSoundCues[i];
                cue.Ticks--;
                if (cue.Ticks == 0)
                {
                    _world.Presentation._plantSoundCues.RemoveAt(i);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(cue.Foley);
                }
                else
                {
                    _world.Presentation._plantSoundCues[i] = cue;
                }
            }

            for (var i = _world.Presentation._squashLandings.Count - 1; i >= 0; i--)
            {
                var landing = _world.Presentation._squashLandings[i];
                landing.Ticks++;
                if (landing.Ticks >= (landing.Water ? 78 : 30))
                {
                    _world.Presentation._squashLandings.RemoveAt(i);
                }
                else
                {
                    _world.Presentation._squashLandings[i] = landing;
                }
            }

            for (var i = _world.Presentation.PlantingEffects.Count - 1; i >= 0; i--)
            {
                var effect = _world.Presentation.PlantingEffects[i];
                effect.Age += dt;
                if (effect.Age + .000001f >= (effect.Pool ? .4f : .3f))
                {
                    _world.Presentation.PlantingEffects.RemoveAt(i);
                }
                else
                {
                    _world.Presentation.PlantingEffects[i] = effect;
                }
            }

            for (var i = _world.Presentation.FootstepEffects.Count - 1; i >= 0; i--)
            {
                var effect = _world.Presentation.FootstepEffects[i];
                effect.Age += dt;
                if (effect.Age >= .2f)
                {
                    _world.Presentation.FootstepEffects.RemoveAt(i);
                }
                else
                {
                    _world.Presentation.FootstepEffects[i] = effect;
                }
            }

            for (var i = _world.Presentation.ZombieSplashes.Count - 1; i >= 0; i--)
            {
                var effect = _world.Presentation.ZombieSplashes[i];
                effect.Age += dt;
                if (effect.Age >= 2f)
                {
                    _world.Presentation.ZombieSplashes.RemoveAt(i);
                }
            }

            for (var i = _world.Presentation.ZombieRiseEffects.Count - 1; i >= 0; i--)
            {
                var effect = _world.Presentation.ZombieRiseEffects[i];
                effect.Age += dt;
                if (effect.Age >= (effect.Digger ? 5f : 2f))
                {
                    _world.Presentation.ZombieRiseEffects.RemoveAt(i);
                }
            }

            for (var i = _world.Presentation.ZomboniParticles.Count - 1; i >= 0; i--)
            {
                var effect = _world.Presentation.ZomboniParticles[i];
                effect.Age += dt;
                if (effect.Age >= effect.Duration)
                {
                    _world.Presentation.ZomboniParticles.RemoveAt(i);
                }
            }

            _graveSystem.AdvanceAdventureGraves();
            _explosionEffects.AdvanceAdventureExplosionPresentation();
            _coldSystem.AdvanceAdventureIcePresentation(dt);
            _zombieFeedbackSystem.AdvanceAdventureZombieFeedback(dt);
            for (var i = _world.Presentation.Corpses.Count - 1; i >= 0; i--)
            {
                var corpse = _world.Presentation.Corpses[i];
                corpse.Age += dt;
                var specialHead = corpse.RequiredZombie.PlantZombieState;
                if (specialHead != null && !specialHead.HeadRemoved && !specialHead.Detached && !_world.Zombies.Contains(corpse.RequiredZombie))
                {
                    specialHead.HeadSeconds += dt;
                    corpse.RequiredZombie.PresentationPlantHeadAge = specialHead.HeadSeconds;
                }
                if (corpse.RequiredZombie.UseLadderColumn >= 0 && corpse.RequiredZombie.PresentationNativeHeight == 7)
                {
                    LadderSystem.AdvanceAdventureLadderFall(corpse.RequiredZombie);
                }
                else if (corpse.RequiredZombie.Kind == ZombieKind.DolphinRider && corpse.RequiredZombie.PresentationNativeHeight == 7)
                {
                    // PC UpdateDeath 54F4A0 continues the original falling update after a Tall-nut collision.
                    corpse.RequiredZombie.PresentationAltitude = Math.Max(0f, corpse.RequiredZombie.PresentationAltitude - 1f);
                    if (corpse.RequiredZombie.PresentationAltitude == 0f)
                    {
                        corpse.RequiredZombie.PresentationNativeHeight = 0;
                    }
                }

                var pool = corpse.RequiredZombie.PoolState;
                if (pool != null && !_world.Zombies.Contains(corpse.RequiredZombie))
                {
                    if (pool.SeaweedAge >= 0f)
                    {
                        pool.SeaweedAge += dt;
                    }

                    if (pool.SplashAge >= 0f)
                    {
                        pool.SplashAge += dt;
                    }
                }

                _zombieFeedbackSystem.AdvanceAdventureZombieCorpseSound(corpse);
                corpse.RequiredZombie.PresentationFlash = Math.Max(0, corpse.RequiredZombie.PresentationFlash - dt);
                corpse.RequiredZombie.PresentationArmorFlash = Math.Max(0, corpse.RequiredZombie.PresentationArmorFlash - dt);
                if (corpse.Age >= corpse.Duration)
                {
                    _world.Presentation.Corpses.RemoveAt(i);
                }
            }

            for (var i = _world.Presentation.Impacts.Count - 1; i >= 0; i--)
            {
                _world.Presentation.Impacts[i].Age += dt;
                if (_world.Presentation.Impacts[i].Age > 3f)
                {
                    _world.Presentation.Impacts.RemoveAt(i);
                }
            }

            foreach (Mower mower in _world.Mowers)
            {
                float time;
                _world.Presentation.MowerTimes.TryGetValue(mower, out time);
                _world.Presentation.MowerTimes[mower] = mower.Active || mower.SquishedTicks > 0 && mower.SquishedWhileActive ? time + dt : 0f;
            }

            var nuts = _adventureModeRunner.Adventure.BowlingNuts;
            for (var nutIndex = 0; nutIndex < nuts.Count; nutIndex++)
            {
                var nut = nuts[nutIndex];
                global::PlantsVsZombies.Adventure.AdventureBowlingPresentation? pose;
                if (!_world.Presentation.Bowling.TryGetValue(nut, out pose))
                {
                    var giant = _world.ModeSession != null && _world.ModeSession._giantNuts.Contains(nut);
                    pose = new global::PlantsVsZombies.Adventure.AdventureBowlingPresentation
                    {
                        Rate = (giant ? 6f : 12f) + (float)_world._random.NextDouble() * (giant ? 4f : 6f)
                    };
                    _world.Presentation.Bowling.Add(nut, pose);
                }

                if (!nut.NativeMotionInitialized)
                {
                    pose.Age += dt;
                }
            }

            foreach (Pea shot in _world.Peas)
            {
                shot.PresentationAge += dt;
                shot.PresentationPreviousX = shot.X;
                shot.PresentationPreviousRow = shot.FlightRow;
                shot.PresentationPreviousLobHeight = shot.PresentationLobHeight;
            }

            foreach (Zombie zombie in _world.Zombies)
            {
                zombie.PresentationPreviousX = zombie.X;
                if (!zombie._presentationInitialized)
                {
                    zombie._presentationInitialized = true;
                    zombie.PresentationWalk2 = zombie.Kind == ZombieKind.Flag || _world._random.Next(2) == 0;
                }

                if (zombie.DiggerState != null)
                {
                    if (zombie.DiggerState.SurpriseAge >= 0f)
                    {
                        zombie.DiggerState.SurpriseAge += dt;
                    }
                }

                _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
                zombie.PresentationFlash = Math.Max(0, zombie.PresentationFlash - dt);
                zombie.PresentationArmorFlash = Math.Max(0, zombie.PresentationArmorFlash - dt);
                if (zombie.PlantZombieState != null)
                {
                    zombie.PlantZombieState.ImmobilizedAtTickStart = zombie.FrozenRemaining > 0 || zombie.PresentationButter > 0;
                }
                var butterTicks = Math.Max(0, (int)Math.Round(zombie.PresentationButter * 100f) - 1);
                zombie.PresentationButter = butterTicks * .01f;
                _zombieFeedbackSystem.AdvanceAdventureZombieClock(zombie, dt);
            }

            foreach (Plant plant in _world.Plants)
            {
                _plantingSystem.InitializeAdventureSleepingPlant(plant);
                if (plant.PresentationCoffeeWakeAge >= 0f && !plant.Asleep
                    && (plant.PresentationShotAge >= 0f || plant.MagnetPresentation != null && plant.MagnetPresentation.NativePhase != 1
                        || plant.ScaredyShroomPresentation != null && plant.ScaredyShroomPresentation.NativePhase != 1))
                {
                    // A later body action starts fresh playback rather than retaining the earlier sleep-to-idle phase.
                    plant.PresentationCoffeeWakeAge = -1f;
                }

                if (plant.SleepingPresentation != null)
                {
                    // PC sleeping reanimations are independent board objects, including while their plant is grabbed.
                    plant.SleepingPresentation.AnimationSeconds += dt;
                }

                if (plant.BungeeState != 0)
                {
                    plant.Age += dt * (plant.BungeeState == 2 ? .1f / Math.Max(1f, plant.PresentationRate) : 1f);
                    if (plant.CoffeePresentation?.Crumbling == true)
                    {
                        // The free body keeps animating during a grab; lifted cargo uses the original .1-FPS body rate.
                        plant.CoffeePresentation.AnimationTicks += plant.BungeeState == 2 ? .1f / AdventureCoffeeBeanState.CrumbleRate : 1f;
                    }
                }

                if (plant.PresentationUmbrellaAge >= 0f)
                {
                    var umbrellaTicks = (int)Math.Round(plant.PresentationUmbrellaAge * 100f) + 1;
                    plant.PresentationUmbrellaAge = umbrellaTicks * .01f;
                    if (umbrellaTicks >= 60)
                    {
                        plant.PresentationUmbrellaAge = -1f;
                    }
                }

                if (plant._presentationKind != plant.Kind)
                {
                    plant._presentationKind = plant.Kind;
                    var minimum = global::PlantsVsZombies.Adventure.AdventurePresentationRules.AttachedHead(plant.Kind) ? 15f : plant.Kind == PlantKind.CherryBomb || plant.Kind == PlantKind.Imitater ? 25f : 10f;
                    plant.PresentationRate = minimum + (float)_world._random.NextDouble() * 5f;
                }

                if (plant.Kind == PlantKind.FlowerPot)
                {
                    var potRate = plant.BungeeState == 2 ? .1f : plant.PresentationFlowerPotPaused ? 0f : plant.PresentationRate;
                    plant.PresentationBodyFrames += dt * potRate;
                }

                if (plant.Asleep)
                {
                    continue;
                }

                _specialPlantActions.AdvanceAdventureDefensivePlantPresentation(plant, dt);
                var previousAwakeAge = plant.PresentationAwakeAge;
                plant.PresentationAwakeAge += dt;
                if (plant.Kind == PlantKind.SunShroom && previousAwakeAge < 120f && plant.PresentationAwakeAge >= 120f)
                {
                    plant.PresentationCoffeeWakeAge = -1f;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(63);
                }
                if (plant.PresentationShotAge >= 0)
                {
                    plant.PresentationShotAge += dt;
                    var shootingFrames = AdventurePresentationRules.LobberShootingFrames(plant.Kind);
                    if (shootingFrames > 0)
                    {
                        if (plant.PresentationLobberIdleAge >= 0f)
                        {
                            if (!_zombieFeedbackSystem.AdventureZombieIsIZombie)
                            {
                                plant.PresentationLobberIdleAge += dt;
                            }
                        }
                        else if (plant.PresentationShotAge * 35f + .0001f >= shootingFrames)
                        {
                            // PC 472C70 -> 4768A0 starts idle at time zero and the source definition's twelve FPS.
                            plant.PresentationLobberIdleAge = 0f;
                            plant.PresentationRate = _zombieFeedbackSystem.AdventureZombieIsIZombie ? 0f : 12f;
                        }
                    }
                }

                for (var head = 0; head < 3; head++)
                {
                    if (plant.PresentationHeadAge[head] >= 0)
                    {
                        plant.PresentationHeadAge[head] += dt;
                    }
                }

                if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.GloomShroom || plant.Kind == PlantKind.Cattail)
                {
                    if (plant.UpgradeBurstTicks > 0)
                    {
                        var elapsed = ((plant.Kind == PlantKind.GloomShroom ? 200 : plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f;
                        plant.PresentationShotAge = elapsed;
                        plant.PresentationHeadAge[0] = elapsed;
                    }

                    continue;
                }

                if (plant.MultiHeadTiming.HasValue)
                {
                    continue;
                }

                if (plant.Kind == PlantKind.Starfruit && plant.StarfruitLaunchCountdown >= 0)
                {
                    continue;
                }

                if (plant.Timing.HasValue && (plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea || plant.Kind == PlantKind.Repeater || plant.Kind == PlantKind.Sunflower
                    || plant.Kind == PlantKind.FumeShroom || plant.Kind == PlantKind.ScaredyShroom || plant.Kind == PlantKind.Cactus
                    || plant.Kind == PlantKind.CabbagePult || plant.Kind == PlantKind.KernelPult || plant.Kind == PlantKind.MelonPult
                    || plant.Kind == PlantKind.WinterMelon || AdventurePuffShroomRules.IsShortRange(plant.Kind)))
                {
                    continue;
                }

                var lead = global::PlantsVsZombies.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
                if (plant.ActionTimer > 0 && plant.ActionTimer <= lead && !plant._presentationPrefiring && CanPresentAdventureWindup(plant))
                {
                    plant._presentationPrefiring = true;
                    plant.PresentationShotAge = Math.Max(0, lead - plant.ActionTimer);
                    for (var h = 0; h < 3; h++)
                    {
                        plant.PresentationHeadAge[h] = plant.PresentationShotAge;
                    }
                }
            }
        }

        /// <summary>Checks whether the plant's next adventure attack can launch a projectile.</summary>
        /// <param name = "plant">The plant whose attack animation may begin.</param>
        /// <returns>Whether the current firing rules admit an attack.</returns>
        internal bool CanPresentAdventureWindup(Plant plant)
        {
            if (AdventurePuffShroomRules.IsShortRange(plant.Kind))
            {
                // PC Plant::FindTargetZombie 0x475AA0 gates the body shooting animation with its attack rectangle.
                return _boardQueries.FindAdventurePuffTarget(plant) != null;
            }

            var x = plant.Column + 0.5f;
            switch (plant.Kind)
            {
                case PlantKind.Peashooter:
                case PlantKind.SnowPea:
                case PlantKind.Repeater:
                    return plant.ModeFacesLeft
                        ? _boardQueries.FindAdventureTarget(plant.Row, -1f, x, false) != null
                        : _boardQueries.FindAdventureTarget(plant.Row, plant.Column + 0.4f, 20f, false) != null || _projectileSystem.CanShootAdventureBoss(plant);
                case PlantKind.Threepeater:
                    for (var row = Math.Max(0, plant.Row - 1); row <= Math.Min(_world.RowCount - 1, plant.Row + 1); row++)
                    {
                        if (_boardQueries.FindAdventureTarget(row, x - 0.1f, 20f, false) != null)
                        {
                            return true;
                        }
                    }

                    return false;
                case PlantKind.SplitPea:
                    return _boardQueries.FindAdventureTarget(plant.Row, x, 20f, false) != null
                        || _boardQueries.FindAdventureTarget(plant.Row, -1f, x, false) != null;
                case PlantKind.Starfruit:
                    return _projectileSystem.FindAdventureStarfruitTarget(plant) != null;
                default:
                    return false;
            }
        }

        /// <summary>Performs adventure presentation shot.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "shot">The shot.</param>
        internal void AdventurePresentationShot(Plant plant, Pea shot)
        {
            shot.PresentationInitialized = true;
            shot.PresentationPlantId = plant.Id;
            shot.PresentationLaunchRow = plant.Row;
            shot.PresentationLaunchColumn = plant.Column;
            shot.PresentationPreviousX = shot.X;
            shot.PresentationPreviousRow = shot.FlightRow;
            plant._presentationPrefiring = false;
            var age = global::PlantsVsZombies.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
            if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.Cattail)
            {
                age = Math.Max(0, ((plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f);
            }

            if (plant.Kind != PlantKind.Repeater || !plant.Timing.HasValue)
            {
                // PC UpdateShooting can fire the first pea before UpdateShooter restarts the
                // Repeater head on the same tick. Its native timing already owns that new clock.
                plant.PresentationShotAge = age;
                var head = plant.Kind == PlantKind.Threepeater ? Math.Max(0, Math.Min(2, 1 - (shot.Row - plant.Row))) : plant.Kind == PlantKind.SplitPea && shot.Backward ? 1 : 0;
                plant.PresentationHeadAge[head] = age;
            }
            _coldSystem.RequestAdventureIceLaunch(plant, shot);
        }

        /// <summary>Applies PC 54E470's independent four-hundred-update butter immobilization without ice or chill.</summary>
        /// <param name="zombie">The actual projectile victim whose original butter admission is checked.</param>
        /// <returns>Whether the original butter state was applied.</returns>
        internal bool ApplyAdventureButter(Zombie zombie)
        {
            var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
            var sled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            var canChill = AdventureIceRules.CanChill((int)zombie.Kind, phase, zombie.Health <= 0f, zombie.Hypnotized, sled);
            if (zombie.Headless || !AdventureIceRules.CanFreeze((int)zombie.Kind, phase, canChill)
                || zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Boss
                || zombie.TangleKelpGrab != null || zombie.PresentationNativeHeight == 3 || sled || zombie.Airborne)
            {
                return false;
            }

            zombie.PresentationButter = 4f;
            if (zombie.Kind == ZombieKind.Pogo)
            {
                zombie.PresentationAltitude = zombie.PogoState?.OnHighGround == true ? 30f : 0f;
                zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
            }

            return true;
        }

        /// <summary>Performs adventure presentation impact.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "zombie">The zombie.</param>
        internal void AdventurePresentationImpact(Pea shot, Zombie? zombie)
        {
            _zombieFeedbackSystem.RequestAdventureProjectileImpactSound(shot, zombie);
            if (shot.Butter && zombie != null)
            {
                ApplyAdventureButter(zombie);
            }

            var effect = new global::PlantsVsZombies.Adventure.AdventureImpact
            {
                Id = ++_world.Presentation._effectId,
                Seed = _world._random.Next(),
                Kind = shot.SourceKind,
                X = zombie == null ? shot.X : zombie.X,
                Row = shot.FlightRow,
                Snow = shot.IsSnow,
                Fire = shot.Fire,
                Butter = shot.Butter
            };
            _coldSystem.RecordAdventureIceImpact(shot, zombie, effect);
            if (shot.Lobbed && !shot.IsSnow && (shot.Splash || shot.Butter || shot.SourceKind == PlantKind.CabbagePult))
            {
                // PC DoImpact 47D410 uses the preceding integration point for all native lobber splats.
                var previousX = shot.PresentationLobNativeX - shot.PresentationLobNativeVelocityX;
                var previousY = shot.PresentationLaunchY + shot.PresentationLobFloorOffset
                    + shot.PresentationLobContactHeight - shot.PresentationLobVelocity;
                effect.NativeIcePoint = true;
                effect.NativeParticleX = previousX + (shot.Splash ? 30f : shot.Butter ? -20f : -38f);
                effect.NativeParticleY = previousY + (shot.Splash ? 30f : shot.Butter ? 63f : 23f);
                if (zombie != null && !shot.Splash)
                {
                    // Cabbage/butter attach through 54DF80; melon bursts remain at their world impact point.
                    var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                    var nativeY = (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX);
                    var offsetY = effect.NativeParticleY - nativeY;
                    if (zombie.PresentationNativePhase == 59 || zombie.PresentationNativePhase == 55)
                    {
                        offsetY += 60f;
                    }

                    effect.AttachedZombie = zombie;
                    effect.NativeAttachedX = (int)(effect.NativeParticleX + 52f - nativeX);
                    effect.NativeAttachedY = (int)Math.Max(20f, Math.Min(100f, offsetY));
                    effect.NativeParticleX = nativeX + effect.NativeAttachedX;
                    effect.NativeParticleY = nativeY + effect.NativeAttachedY;
                }
            }

            if (!shot.Lobbed && zombie == null && _adventureModeRunner.Adventure.Level.HasRoof
                && shot.SourceKind != PlantKind.Starfruit)
            {
                effect.NativeIcePoint = true;
                var puff = AdventurePuffShroomRules.IsShortRange(shot.SourceKind) || shot.SourceKind == PlantKind.ScaredyShroom;
                effect.NativeParticleX = 40f + shot.X * 80f - (shot.SourceKind == PlantKind.Cactus ? 15f : puff ? 16f : 14f)
                    + (AdventureTorchwoodRules.IsPea(shot.SourceKind) ? -3f : 12f);
                effect.NativeParticleY = shot.PresentationLaunchY + shot.PresentationLaneOffset
                    - (shot.SourceKind == PlantKind.Cactus ? 8f : puff ? 13.5f : 14f) + 12f;
                if (shot.Fire)
                {
                    effect.NativeParticleX += 3f;
                    effect.NativeParticleY -= 12f;
                }
            }

            if (shot.SourceKind == PlantKind.Starfruit)
            {
                // PC DoImpact 47D410 puts StarSplat at projectile top-left +(12,12).
                effect.NativeIcePoint = true;
                effect.NativeParticleX = 40f + shot.X * 80f + 12f;
                effect.NativeParticleY = AdventureStarfruitRules.PositionY(shot, _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f) + 12f;
            }

            _world.Presentation.Impacts.Add(effect);
        }

        /// <summary>Performs create adventure burn presentation.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting create adventure burn presentation.</returns>
        internal bool CreateAdventureBurnPresentation(Zombie zombie)
        {
            var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
            var phase = zombie.PresentationNativePhase;
            if (phase < 0)
            {
                phase = 0;
                if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase < 2)
                {
                    phase = zombie.AbilityPhase == 0 ? 33 : 35;
                }
                else if (zombie.Kind == ZombieKind.PoleVaulter && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f)
                {
                    phase = 12;
                }
                else if (zombie.Kind == ZombieKind.DolphinRider && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f)
                {
                    phase = 54;
                }
            }

            var inPool = zombie.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(zombie.Row) && zombie.X >= 0 && zombie.X < 9);
            var backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie));
            var hasSled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            var scale = AdventureDancerRules.Scale(zombie);
            var pose = global::PlantsVsZombies.Adventure.AdventureBurnPresentationRules.Resolve(kind, phase, inPool, zombie.Airborne, !zombie.Headless, hasSled, backwards, !zombie.ThrownImp, scale, zombie.PresentationHealthBeforeHit);
            // PC removes ice/butter and stops hit coloration before choosing the burn visual.
            zombie.FrozenRemaining = 0;
            zombie.PresentationButter = 0;
            zombie.PresentationFlash = 0;
            zombie.PresentationArmorFlash = 0;
            if (pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.NormalDeath)
            {
                return false;
            }

            if (pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.None)
            {
                return true;
            }

            _world.Presentation.Corpses.Add(new global::PlantsVsZombies.Adventure.AdventureZombieCorpse {
                Zombie = zombie,
                Burn = pose,
                Animation = zombie.PresentationAnimation,
                FrozenSeconds = zombie.PresentationAnimationAge,
                FrozenHeadSeconds = zombie.PresentationPlantHeadAge,
                FrozenPlantHeadState = zombie.PlantZombieState?.Snapshot(),
                RateScale = pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.CharredDefinition ? .9f + (float)_world._random.NextDouble() * .2f : 1f,
                Duration = pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.FrozenOriginal ? 3f : float.PositiveInfinity
            });
            return true;
        }

        // Called after horizontal integration. Snow trail sampling follows the torch conversion in the caller.
        // Plant::Fire native lob velocity uses a 120-tick horizontal lead and acceleration .115.
        /// <summary>Performs advance adventure projectile presentation.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureProjectilePresentation(Pea shot, float dt)
        {
            if (!shot.PresentationMotionInitialized)
            {
                shot.PresentationMotionInitialized = true;
                shot.PresentationRotation = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? 0f : shot.Splash ? -1.2566371f : -.87964594f;
                var minimum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.2f : -.08f;
                var maximum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.08f : -.02f;
                shot.PresentationSpin = shot.Lobbed ? minimum + (float)_world._random.NextDouble() * (maximum - minimum) : 0;
                if (shot.Lobbed)
                {
                    var muzzleX = shot.SourceKind == PlantKind.CabbagePult ? 5f
                        : shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? 12f : 19f : 25f;
                    var nativeLaunchX = 40f + shot.PresentationLaunchColumn * 80f + muzzleX;
                    var launch = (nativeLaunchX - 40f) / 80f;
                    var target = _boardQueries.FindAdventureLobberTarget(shot.PresentationLaunchRow, shot.PresentationLaunchColumn);
                    var distance = 700f - nativeLaunchX;
                    if (target != null)
                    {
                        // PC ZombieTargetLeadX(50): eating, freezing and butter set prediction velocity to zero.
                        var stationary = AdventureStarfruitRules.TargetNotWalking(target)
                            || AdventureDancerRules.IsDancer(target) && AdventureDancerRules.GroupAffected(target, _world.Zombies, false);
                        var velocity = stationary ? 0f : target.PresentationNativeVelocity * (target.ChillRemaining > 0f ? .5f : 1f);
                        if (target.PresentationWalkingBackwards ?? (target.Hypnotized
                            || target.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(target)))
                        {
                            velocity = -velocity;
                        }

                        var body = AdventureZombieTargetingRules.BodyRectangle(target);
                        distance = body.X + body.Width / 2 - velocity * 50f - nativeLaunchX - 30f;
                        if (target.PresentationNativePhase == 53 || target.Kind == ZombieKind.Pogo && !target.UsedAbility)
                        {
                            distance -= 60f;
                        }
                        else if (target.PresentationNativePhase == 59)
                        {
                            distance -= 40f;
                        }
                    }

                    shot.PresentationLobX = launch;
                    shot.PresentationPreviousX = launch;
                    shot.PresentationLobNativeVelocityX = Math.Max(40f, distance) / 120f;
                    shot.VelocityX = shot.PresentationLobNativeVelocityX * 100f / 80f;
                    var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
                    var launchOffset = shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? -56f : -37f : shot.Splash ? -46f : -12f;
                    var launchY = 80 + shot.PresentationLaunchRow * pitch + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - shot.PresentationLaunchColumn) * 20 - 10 : 0) + launchOffset;
                    if (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, shot.PresentationLaunchRow, shot.PresentationLaunchColumn))
                    {
                        launchY -= 5;
                    }

                    shot.PresentationLaunchY = launchY;
                    shot.PresentationLobNativeX = nativeLaunchX;
                    shot.PresentationLobNativeY = launchY;
                    float heightDifference = 0;
                    if (target != null)
                    {
                        var nativeX = AdventureZombieNativeRules.NativePositionX(target.X, AdventureDancerRules.Scale(target));
                        var body = AdventureZombieTargetingRules.BodyRectangle(target);
                        var targetY = (int)_biteSystem.AdventureBiteNativeY(target, nativeX) + body.Y + target.PresentationBiteBodyY;
                        if (target.Kind == ZombieKind.Boss)
                        {
                            targetY = (int)(80f + shot.PresentationLaunchRow * pitch - (_adventureModeRunner.Adventure.Level.HasRoof ? 10f : 0f));
                        }

                        heightDifference = targetY - launchY;
                    }

                    shot.PresentationLobInitialVelocity = -7f + heightDifference / 120f;
                    shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity;
                    shot.PresentationLobImpactHeight = (shot.Butter ? -32f : shot.Splash ? -35f : -30f)
                        + (_adventureModeRunner.Adventure.Level.IsWaterRow(shot.Row) ? 40f : 0f);
                }
            }

            shot.PresentationNativeAge += dt;
            if (!shot.Lobbed)
            {
                return;
            }

            // PC 47C880 integrates float32 velocity before position, once per native update.
            // Keeping native X avoids a grid conversion rounding an integer contact edge one update early.
            var updates = (int)Math.Round(dt * 100f);
            for (var update = 0; update < updates; update++)
            {
                var previousX = shot.PresentationLobNativeX;
                shot.PresentationLobVelocity += .115f;
                shot.PresentationLobNativeX += shot.PresentationLobNativeVelocityX;
                shot.PresentationLobNativeZ += shot.PresentationLobVelocity;
                shot.PresentationLobContactHeight = shot.PresentationLobNativeZ;
                shot.PresentationLobFloorOffset = shot.PresentationLobNativeY - shot.PresentationLaunchY;
                if (_adventureModeRunner.Adventure.Level.HasRoof)
                {
                    // UpdateLobMotion checks contact before UpdateMotion moves mPosY and rebases mPosZ.
                    var rowY = 70f + shot.Row * 85f;
                    var previousFloor = rowY + Math.Max(0f, 440f - previousX) * .25f;
                    var floor = rowY + Math.Max(0f, 440f - shot.PresentationLobNativeX) * .25f;
                    var floorDelta = floor - previousFloor;
                    shot.PresentationLobNativeY += floorDelta;
                    shot.PresentationLobNativeZ -= floorDelta;
                }
            }

            shot.X = (shot.PresentationLobNativeX - 40f) / 80f;
            shot.PresentationLobHeight = shot.PresentationLobNativeY + shot.PresentationLobNativeZ - shot.PresentationLaunchY;
        }
        // Cob impacts are produced by the actual shell explosion, without a second damage source.
        /// <summary>Performs adventure presentation cob impact.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        internal void AdventurePresentationCobImpact(float x, float row)
        {
            // PC Projectile::DoImpact 0x47D410 uses the ash plants' twelve-tick shake.
            _world.Presentation.ScreenShake.Start(3, -4);
            _world.Presentation.Impacts.Add(new global::PlantsVsZombies.Adventure.AdventureImpact {
                Id = ++_world.Presentation._effectId,
                Seed = _world._random.Next(),
                Kind = PlantKind.CobCannon,
                X = x,
                Row = row,
                Cob = true
            });
        }
    }
}
