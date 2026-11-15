#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SpecialPlantActions responsibilities for one deterministic match.</summary>
    internal sealed class SpecialPlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BalloonZombieActions operations.</summary>
        private BalloonZombieActions _balloonZombieActions = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides ColdSystem operations.</summary>
        private ColdSystem _coldSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides LadderSystem operations.</summary>
        private LadderSystem _ladderSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides WaterZombieActions operations.</summary>
        private WaterZombieActions _waterZombieActions = null!;
        /// <summary>Provides WaveSystem operations.</summary>
        private WaveSystem _waveSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SpecialPlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="balloonZombieActions">The BalloonZombieActions collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="coldSystem">The ColdSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="ladderSystem">The LadderSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="waterZombieActions">The WaterZombieActions collaborator.</param>
        /// <param name="waveSystem">The WaveSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BalloonZombieActions balloonZombieActions, BiteSystem biteSystem, BoardQueries boardQueries, BossSystem bossSystem, ColdSystem coldSystem, CombatSystem combatSystem, ExplosionEffects explosionEffects, LadderSystem ladderSystem, LegacySimulation legacySimulation, PlantingSystem plantingSystem, WaterZombieActions waterZombieActions, WaveSystem waveSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _balloonZombieActions = balloonZombieActions;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _bossSystem = bossSystem;
            _coldSystem = coldSystem;
            _combatSystem = combatSystem;
            _explosionEffects = explosionEffects;
            _ladderSystem = ladderSystem;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _waterZombieActions = waterZombieActions;
            _waveSystem = waveSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Advances PC UpdateTangleKelp (0x46E430), keeping the authored grab attached until death.</summary>
        /// <param name="plant">The water plant whose native grab owns its attack timing.</param>
        internal void AdvanceAdventureTangleKelp(Plant plant)
        {
            var state = plant.TangleKelpPresentation;
            if (state == null)
            {
                var x = plant.Column + .5f;
                var target = _boardQueries.FindAdventureTarget(plant.Row, x - .5f, x + .7f, false, true, PlantKind.TangleKelp);
                if (target == null)
                {
                    return;
                }

                _zombieFeedbackSystem.RequestAdventureZombieFoley(68);
                SplashAdventureTangleKelpTarget(target);
                state = new AdventureTangleKelpState
                {
                    TargetZombieId = target.Id,
                    OffsetX = target.Kind == ZombieKind.Snorkel ? -43 : _zombieFeedbackSystem.AdventureZombieFeedbackPhase(target) == 53 ? -20 : -13,
                    OffsetY = target.Kind == ZombieKind.Snorkel ? 55 : _zombieFeedbackSystem.AdventureZombieFeedbackPhase(target) == 53 ? 37 : 15
                };
                plant.TangleKelpPresentation = state;
                target.TangleKelpGrab = state;
                plant.PresentationBiteNativeState = 34;
                return;
            }

            state.Countdown--;
            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 50)
            {
                var target = FindAdventureKelpZombie(state.TargetZombieId);
                if (target != null)
                {
                    target.PresentationNativeHeight = 3;
                    _biteSystem.StopAdventureNativeEating(target);
                    SplashAdventureTangleKelpTarget(target);
                }
            }

            if (state.Countdown == 20)
            {
                var x = 40f + plant.Column * 80f;
                var y = 80f + plant.Row * 85f;
                _world.Presentation.ZombieSplashes.Add(new AdventureZombieSplash
                {
                    Id = ++_world.Presentation._effectId, Row = plant.Row,
                    X = x - 23f, Y = y + 7f, ParticleX = x + 31f, ParticleY = y + 64f,
                    ScaleX = 1.3f, ScaleY = 1.3f
                });
                _zombieFeedbackSystem.RequestAdventureZombieFoley(53);
            }

            if (state.Countdown <= 0)
            {
                _plantingSystem.RemoveAdventurePlantAndStack(plant);
            }
        }

        /// <summary>Finds the surviving actor referenced by the original kelp grab.</summary>
        /// <param name="id">The stable zombie identifier retained by the grabbing plant.</param>
        /// <returns>The living board actor, or null after an earlier death or removal.</returns>
        internal Zombie? FindAdventureKelpZombie(int id)
        {
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Id == id)
                {
                    return zombie;
                }
            }

            return null;
        }

        /// <summary>Emits the original target's PoolSplash(false), including its swimming-snorkel offsets.</summary>
        /// <param name="target">The grabbed zombie whose native position anchors the splash.</param>
        internal void SplashAdventureTangleKelpTarget(Zombie target)
        {
            var scale = AdventureDancerRules.Scale(target);
            var swimming = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(target) == 59;
            var altitude = target.PoolState?.Altitude ?? target.PresentationAltitude;
            _waterZombieActions.AddAdventureZombieSplash(target, (swimming ? -14f : 23f) * scale,
                (swimming ? 70f : 78f) * (altitude == 0f ? scale : 1f), .8f, .8f, 52,
                target.Kind == ZombieKind.DolphinRider ? 65f : (float?)null);
        }

        /// <summary>Runs the original Blover body clock, single wind effect, and two-hundred-update special lifetime.</summary>
        /// <param name="plant">The planted Blover whose original state is advanced.</param>
        internal void AdvanceAdventureBlover(Plant plant)
        {
            var state = plant.BloverPresentation ??= new AdventureBloverState();
            if (state.Advance())
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(-2);
                _balloonZombieActions.BlowAdventureFliersAway();
                // PC BlowAwayFliers 474AAE includes the original twenty-second fog return interval.
                _adventureModeRunner.Adventure.FogBlownRemaining = 40f;
            }

            plant.ActionTimer = state.SpecialTicks * .01f;
            if (state.Expired)
            {
                _plantingSystem.RemoveAdventurePlantAndStack(plant);
            }
        }

        /// <summary>Runs the original coffee special before its fifteen-frame crumble ends its own lifetime.</summary>
        /// <param name="plant">The live flying bean, independent of the mushroom below it.</param>
        internal void AdvanceAdventureCoffeeBean(Plant plant)
        {
            var state = plant.CoffeePresentation ??= new AdventureCoffeeBeanState();
            if (!state.Crumbling)
            {
                state.Countdown = Math.Max(0, state.Countdown - 1);
                plant.ActionTimer = state.Countdown * .01f;
                if (state.Countdown > 0)
                {
                    return;
                }

                var mushroom = _boardQueries.AdventurePlantAt(plant.Row, plant.Column, PlantLayer.Main);
                if (mushroom != null && mushroom.Asleep)
                {
                    mushroom.WakeTimer = 1f;
                }

                state.Crumbling = true;
                state.AnimationTicks = 0;
                _zombieFeedbackSystem.RequestAdventureZombieFoley(69);
            }
            else if (state.AnimationTicks * .01f * AdventureCoffeeBeanState.CrumbleRate >= AdventureCoffeeBeanState.CrumbleFrames)
            {
                _plantingSystem.RemoveAdventurePlantAndStack(plant);
                return;
            }

            // PC Animate updates the newly started body in the same special-action tick.
            state.AnimationTicks++;
        }

        /// <summary>Gets the original planted cell's native vertical coordinate, including the roof slope.</summary>
        /// <param name="row">The original board row.</param>
        /// <param name="column">The clamped original board column.</param>
        /// <returns>The native GridToPixelY coordinate before plant height offsets.</returns>
        internal int AdventureSquashCellY(int row, int column)
        {
            return 80 + row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100)
                + (_adventureModeRunner.Adventure.Level.HasRoof ? (int)(Math.Max(0, 5 - column) * 20 - 10) : 0);
        }

        /// <summary>Finds the closest original contact target while preferring the initially selected live identifier.</summary>
        /// <param name="plant">The Squash whose original contact interval is tested.</param>
        /// <param name="state">The retained original target and native body origin.</param>
        /// <returns>The admitted native zombie, or null when no original target remains.</returns>
        internal Zombie? FindAdventureSquashTarget(Plant plant, AdventureSquashState state)
        {
            Zombie? best = null;
            var bestGap = int.MaxValue;
            var attack = AdventureSquashRules.AttackRectangle(state.NativeX);
            foreach (var zombie in _world.Zombies)
            {
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (!_combatSystem.CanTakeAdventureAshDamage(zombie, 13U)
                    || !AdventureSquashRules.CanTarget(plant, zombie, body, state.NativeX, _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie), _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker))
                {
                    continue;
                }

                if (zombie.Id == state.TargetZombieId)
                {
                    return zombie;
                }

                var gap = -AdventureSquashRules.Overlap(attack, body);
                if (best == null || gap < bestGap)
                {
                    best = zombie;
                    bestGap = gap;
                }
            }

            return best;
        }

        /// <summary>Predicts the PC body center at the requested native lead time before subtracting half the plant width.</summary>
        /// <param name="zombie">The original target whose current body and movement state are sampled.</param>
        /// <param name="ticks">The native prediction horizon used by ZombieTargetLeadX.</param>
        /// <returns>The integer native landing coordinate of the Squash's left edge.</returns>
        internal int AdventureSquashTargetX(Zombie zombie, int ticks)
        {
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            var velocity = zombie.PresentationNativeVelocity;
            if (zombie.ChillRemaining > 0f)
            {
                velocity *= .5f;
            }

            if (zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie)))
            {
                velocity = -velocity;
            }

            if (AdventureStarfruitRules.TargetNotWalking(zombie)
                || AdventureDancerRules.IsDancer(zombie) && !AdventureDancerRules.CanWalk(zombie, _world.Zombies))
            {
                velocity = 0f;
            }

            return (int)(body.X + body.Width / 2 - velocity * ticks) - 40;
        }

        /// <summary>Applies PC DoSquashDamage's narrow horizontal overlap and no-body-parts damage flags.</summary>
        /// <param name="plant">The falling Squash reaching its original damage update.</param>
        /// <param name="state">The moving native body position at impact.</param>
        internal void DamageAdventureSquashTargets(Plant plant, AdventureSquashState state)
        {
            var attack = AdventureSquashRules.AttackRectangle(state.NativeX);
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (zombie.Kind != ZombieKind.Boss && zombie.Row != plant.Row || !_combatSystem.CanTakeAdventureAshDamage(zombie, 13U))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (AdventureSquashRules.Overlap(attack, body) > (zombie.Kind == ZombieKind.Football ? -20 : 0))
                {
                    // PC 0x46EB68 calls TakeDamage(1800,18): shield piercing and no detached parts or ordinary corpse.
                    _combatSystem.ApplyAdventureZombieDamage(zombie, 1800f, true, 0x12U);
                }
            }
        }

        /// <summary>Advances PC UpdateSquash's exact look, prelaunch, flight, damage, landing, and flattening sequence.</summary>
        /// <param name="plant">The live awake Squash being advanced by one native update.</param>
        internal void AdvanceAdventureSquash(Plant plant)
        {
            var state = plant.SquashPresentation;
            if (state == null)
            {
                state = new AdventureSquashState
                {
                    NativeX = 40 + plant.Column * 80,
                    NativeY = AdventureSquashCellY(plant.Row, plant.Column)
                };
                plant.SquashPresentation = state;
            }

            state.AdvanceClock();
            switch (state.NativePhase)
            {
                case 0:
                    var target = FindAdventureSquashTarget(plant, state);
                    if (target != null)
                    {
                        state.TargetZombieId = target.Id;
                        state.TargetX = AdventureSquashTargetX(target, 0);
                        state.Enter(3, 80, state.TargetX < state.NativeX ? "anim_lookleft" : "anim_lookright", 24f, 10);
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(41);
                    }

                    break;
                case 3:
                    if (state.Countdown == 0)
                    {
                        // Local PC 0x46EEAE uses forty-five updates; the mobile reference uses thirty.
                        state.Enter(4, 45, "anim_jumpup", 24f, 20);
                    }

                    break;
                case 4:
                    if (state.Countdown == 0)
                    {
                        var redirected = FindAdventureSquashTarget(plant, state);
                        if (redirected != null)
                        {
                            state.TargetX = AdventureSquashTargetX(redirected, 30);
                        }

                        state.Enter(5, 50);
                    }

                    break;
                case 5:
                case 6:
                    var column = Math.Max(0, Math.Min(8, (int)Math.Floor((state.TargetX - 40f) / 80f)));
                    var destinationY = AdventureSquashCellY(plant.Row, column) + 8;
                    if (state.NativePhase == 5)
                    {
                        state.NativeX = AdventureSquashRules.RisingPosition(state.Countdown, 40 + plant.Column * 80, state.TargetX);
                        state.NativeY = AdventureSquashRules.RisingPosition(state.Countdown, AdventureSquashCellY(plant.Row, plant.Column), destinationY - 120);
                        if (state.Countdown == 0)
                        {
                            state.Enter(6, 10, "anim_jumpdown", 60f, 0);
                        }
                    }
                    else
                    {
                        state.NativeY = destinationY - 120 + (10 - state.Countdown) * 12;
                        if (state.Countdown == 5)
                        {
                            DamageAdventureSquashTargets(plant, state);
                        }

                        if (state.Countdown == 0)
                        {
                            var water = _adventureModeRunner.Adventure.Level.IsWaterRow(plant.Row);
                            _world.Presentation._squashLandings.Add(new AdventureSquashLanding
                            {
                                Id = ++_world.Presentation._effectId, Row = plant.Row, Water = water,
                                X = state.NativeX + (water ? -11f : 40f),
                                Y = state.NativeY + (water ? 20f : _adventureModeRunner.Adventure.Level.HasRoof ? 69f : 80f)
                            });
                            if (water)
                            {
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(-4);
                                _plantingSystem.RemoveAdventurePlantAndStack(plant);
                            }
                            else
                            {
                                state.Enter(7, 100);
                                _world.Presentation.ScreenShake.Start(1, 4);
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(40);
                            }
                        }
                    }

                    break;
                case 7:
                    if (state.Countdown == 0)
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(plant);
                    }

                    break;
            }

            plant.PresentationBiteNativeState = state.NativePhase;
            plant.PresentationBiteNotOnGround = state.NativePhase >= 5;
        }

        /// <summary>Runs the original Ice-shroom special for both its fuse and an awake plant being squished.</summary>
        /// <param name="plant">The consumed mushroom whose cell anchors the original ice particle system.</param>
        internal void DetonateAdventureIceShroom(Plant plant)
        {
            _zombieFeedbackSystem.RequestAdventureZombieFoley(18);
            _waveSystem.NotifyAdventureIceTrap();
            _explosionEffects.AdventurePresentationExplosion(PlantKind.IceShroom, plant.Column + .5f, plant.Row);
            _bossSystem.ApplyAdventureBossIceTrap();
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                _coldSystem.ApplyAdventureIceTrap(zombie);
            }

            _bossSystem.DestroyBossBalls(false);
            _plantingSystem.RemoveAdventurePlantAndStack(plant);
        }

        /// <summary>Starts and advances the original awake Doom-shroom special action once per native tick.</summary>
        /// <param name="plant">The awake Doom-shroom whose fuse and original exploding body are updated.</param>
        internal void AdvanceAdventureDoomShroom(Plant plant)
        {
            var state = plant.DoomShroomPresentation;
            if (state == null)
            {
                state = new AdventureDoomShroomState();
                plant.DoomShroomPresentation = state;
                plant.PresentationRate = AdventureDoomShroomState.Rate;
                _zombieFeedbackSystem.RequestAdventureZombieFoley(57);
            }

            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                _combatSystem.BlastAdventureAshPlant(plant, 250, 3);
                _explosionEffects.AdventurePresentationExplosion(PlantKind.DoomShroom, plant.Column + .5f, plant.Row);
                // PC 474AC0 kills every allocated plant in the Doom-shroom's own cell, including supports and shells.
                foreach (var occupant in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (occupant.Row == plant.Row && occupant.Column == plant.Column && _world.Plants.Contains(occupant))
                    {
                        _plantingSystem.RemoveAdventurePlantAndStack(occupant);
                    }
                }

                _world.Presentation.SquishedPlants.RemoveAll(crushed => crushed.Plant.Row == plant.Row && crushed.Plant.Column == plant.Column);
                _adventureModeRunner.Adventure._craters.Add(new AdventureCellTimer { Row = plant.Row, Column = plant.Column, Remaining = 180f });
                _legacySimulation.Raise(GameEvent.DoomExplosion);
            }

            // PC Reanimation::Update samples each configured track independently on both axes.
            for (var head = 0; head < state.HeadShakeX.Length; head++)
            {
                var amount = head == 0 ? 1f : 2f;
                state.HeadShakeX[head] = _explosionEffects.AdventureExplosionRandomFloat(amount * 2f) - amount;
                state.HeadShakeY[head] = _explosionEffects.AdventureExplosionRandomFloat(amount * 2f) - amount;
            }

            state.AnimationTicks++;
        }

        /// <summary>Performs initialize adventure potato mine.</summary>
        /// <param name = "p">The p.</param>
        internal void InitializeAdventurePotatoMine(Plant p)
        {
            if (p.PotatoMinePresentation != null)
            {
                return;
            }

            p.PotatoMinePresentation = new global::PlantsVsZombies.Adventure.AdventurePotatoMineState();
            p._presentationKind = PlantKind.PotatoMine;
            p.PresentationRate = 12f;
            p.ActionTimer = 15f;
        }

        /// <summary>Advances the original chomper state machine once per native simulation tick.</summary>
        /// <param name="plant">The live chomper being updated.</param>
        /// <returns>True because the native state machine owns this plant's attack timing.</returns>
        internal bool AdvanceAdventureChomper(Plant plant)
        {
            var state = plant.ChomperPresentation;
            if (state == null)
            {
                state = new AdventureChomperState();
                plant.ChomperPresentation = state;
            }

            state.PhaseTicks++;
            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            // PC 0x46F6E0: bite at 24 fps, commit the meal after 70 ticks,
            // chew for 4000 ticks at 15 fps, then swallow at 12 fps.
            switch (state.NativePhase)
            {
                case 1:
                    if (FindAdventureChomperTarget(plant, false) != null)
                    {
                        state.NativePhase = 10;
                        state.Animation = "anim_bite";
                        state.Rate = 24f;
                        state.PhaseTicks = 0;
                        state.Countdown = 70;
                    }

                    break;
                case 10:
                    if (state.Countdown != 0)
                    {
                        break;
                    }

                    _zombieFeedbackSystem.RequestAdventureZombieFoley(61);
                    var target = FindAdventureChomperTarget(plant, true);
                    var phase = target == null ? -1 : _zombieFeedbackSystem.AdventureZombieFeedbackPhase(target);
                    if (target != null && (target.Kind == ZombieKind.Gargantuar || target.Kind == ZombieKind.Boss))
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                        _combatSystem.HitAdventureZombie(target, 40f, true);
                        state.NativePhase = 12;
                    }
                    else if (target == null || target.FrozenRemaining <= 0 && target.PresentationButter <= 0 && (phase >= 20 && phase <= 28 || phase == 11 || phase == 12))
                    {
                        state.NativePhase = 12;
                    }
                    else
                    {
                        target.PresentationDeath = AdventureZombieDeathPresentation.Eaten;
                        _zombieLifecycle.KillAdventureZombie(target);
                        state.NativePhase = 11;
                    }

                    break;
                case 11:
                case 12:
                    if (state.PhaseTicks * 24 >= 2500)
                    {
                        if (state.NativePhase == 11)
                        {
                            state.NativePhase = 13;
                            state.Animation = "anim_chew";
                            state.Rate = 15f;
                            state.Countdown = 4000;
                        }
                        else
                        {
                            state.NativePhase = 1;
                            state.Animation = "anim_idle";
                            state.Rate = 0f;
                        }

                        state.PhaseTicks = 0;
                    }

                    break;
                case 13:
                    if (state.Countdown == 0)
                    {
                        state.NativePhase = 14;
                        state.Animation = "anim_swallow";
                        state.Rate = 12f;
                        state.PhaseTicks = 0;
                    }

                    break;
                case 14:
                    if (state.PhaseTicks * 12 >= 2800)
                    {
                        state.NativePhase = 1;
                        state.Animation = "anim_idle";
                        state.Rate = 0f;
                        state.PhaseTicks = 0;
                    }

                    break;
            }

            plant.PresentationBiteNativeState = state.NativePhase;
            plant.ActionTimer = state.Countdown * .01f;
            return true;
        }

        /// <summary>Finds the headed zombie overlapping the original chomper attack interval.</summary>
        /// <param name="plant">The chomper performing the native target check.</param>
        /// <param name="biting">Whether the active bite permits the original sixty-pixel tolerance.</param>
        /// <returns>The leftmost eligible zombie, or null when the bite has no target.</returns>
        internal Zombie? FindAdventureChomperTarget(Plant plant, bool biting)
        {
            Zombie? best = null;
            var left = 40 + plant.Column * 80 + 80;
            var right = left + 40;
            foreach (var zombie in _world.Zombies)
            {
                var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Chomper) || zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0 || zombie.Kind == ZombieKind.Bungee && zombie.TargetColumn == plant.Column || phase == 20 || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var attackLeft = left + (phase == 37 ? 20 : 0);
                var overlap = Math.Min(right, body.X + body.Width) - Math.Max(attackLeft, body.X);
                if (overlap < (biting || zombie.Eating ? -60 : 0))
                {
                    continue;
                }

                if (best == null || body.X < AdventureZombieTargetingRules.BodyRectangle(best).X)
                {
                    best = zombie;
                }
            }

            return best;
        }

        /// <summary>Updates defensive body animation without changing its phase when chewing slows it.</summary>
        /// <param name="plant">The plant whose body and damage presentation is updated.</param>
        /// <param name="delta">Elapsed native simulation time.</param>
        internal void AdvanceAdventureDefensivePlantPresentation(Plant plant, float delta)
        {
            if (AdventureDefensivePlantRules.DamageTrack(plant.Kind) == null)
            {
                return;
            }

            var stage = AdventureDefensivePlantRules.DamageStage(plant.Kind, plant.Health);
            if (plant.Kind != PlantKind.Pumpkin && stage > 0 && stage != plant._presentationDefensiveDamageStage)
            {
                _world.Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                {
                    Id = ++_world.Presentation._effectId,
                    ParticleId = 36,
                    Row = plant.Row,
                    NativeX = 40 + plant.Column * 80 + 40,
                    NativeY = 80 + plant.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100) + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f) + (plant.Kind == PlantKind.TallNut ? -22 : 10),
                    Seed = plant.Id * 397 ^ _world.Presentation._effectId
                });
            }

            plant._presentationDefensiveDamageStage = stage;
            if (plant.Kind == PlantKind.Pumpkin)
            {
                return;
            }

            var chewing = plant.PresentationEatenFlashTicks > 0;
            if (!chewing && plant._presentationDefensiveChewing)
            {
                plant.PresentationRate = 10f + (float)_world._random.NextDouble() * 5f;
            }

            plant._presentationDefensiveChewing = chewing;
            plant.PresentationBodyRate = chewing ? .1f : plant.PresentationRate;
            plant.PresentationBodyFrames += delta * plant.PresentationBodyRate;
        }

        /// <summary>Performs advance adventure potato mine.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting advance adventure potato mine.</returns>
        internal bool AdvanceAdventurePotatoMine(Plant p)
        {
            InitializeAdventurePotatoMine(p);
            var state = p.RequiredPotatoMinePresentation;
            switch (p.MinePhase)
            {
                case MinePhase.Buried:
                    if (state.Countdown > 0)
                    {
                        state.Countdown--;
                    }

                    p.ActionTimer = state.Countdown * .01f;
                    if (state.Countdown == 0)
                    {
                        p.MinePhase = MinePhase.Rising;
                        p.PhaseAge = 0;
                        state.PhaseTicks = 0;
                        state.BodyRate = 18f;
                        p.PresentationRate = 18f;
                        _explosionEffects.AdventurePresentationPotatoMineRise(p);
                        _world.Presentation.PotatoMineRiseSoundSerial++;
                    }

                    break;
                case MinePhase.Rising:
                    state.PhaseTicks++;
                    // The local reanim's anim_rise is 19 frames at native 18 fps.
                    if (state.PhaseTicks * 18 >= 1900)
                    {
                        p.MinePhase = MinePhase.Armed;
                        p.PhaseAge = 0;
                        state.PhaseTicks = 0;
                        state.BodyRate = 12f + _explosionEffects.AdventureExplosionRandomFloat(3f);
                        p.PresentationRate = state.BodyRate;
                        state.LightCreated = true;
                        state.GlowFrames = 10;
                        state.GlowTime = 0;
                        p.ActionTimer = 0;
                        p.BlinkCountdown = 400 + _world._random.Next(400);
                    }

                    break;
                case MinePhase.Armed:
                    state.PhaseTicks++;
                    bool touching;
                    var gap = AdventurePotatoMineDistance(p, out touching);
                    state.NearestDistancePixels = gap;
                    if (touching)
                    {
                        var x = p.Column + .5f;
                        DamageAdventurePotatoMine(p);
                        _explosionEffects.AdventurePresentationExplosion(PlantKind.PotatoMine, x, p.Row);
                        _plantingSystem.RemoveAdventurePlantAndStack(p);
                        _legacySimulation.Raise(GameEvent.MineExplosion);
                        return true;
                    }

                    if (state.LightCreated)
                    {
                        // PC 46E405..46E423 -> curve(200,50,gap,10,3,Linear), rounded.
                        var t = Math.Max(0f, Math.Min(1f, (gap - 200f) / -150f));
                        state.GlowFrames = (int)(10f - 7f * t + .5f);
                        state.GlowTime += .01f * (state.BodyRate - 2f) / state.GlowFrames;
                        state.GlowTime -= (float)Math.Floor(state.GlowTime);
                    }

                    break;
            }

            return true;
        }

        /// <summary>Applies the original mine's sixty-pixel blast to eligible same-row body rectangles.</summary>
        /// <param name="plant">The armed mine whose native detonation center is used.</param>
        internal void DamageAdventurePotatoMine(Plant plant)
        {
            // PC 0x474EE0 / 0x4294C0 / 0x428460: center is (mX+20,mY+40),
            // radius is 60, row range is zero, and the damage target mask is 0x4D.
            var centerX = 40 + plant.Column * 80 + 20;
            var centerY = 80 + plant.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100)
                + (_adventureModeRunner.Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - plant.Column) * 20f - 10f) : 0) + 40;
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (zombie.Row != plant.Row || zombie.Hypnotized || zombie.Airborne || zombie.DropRemaining > 0
                    || zombie.Kind == ZombieKind.Boss || zombie.PresentationNativeHeight == 9
                    || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie))
                {
                    continue;
                }

                var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
                if (phase == 12 || phase == 71 || phase == 33 || phase == 34 || phase == 35 || phase == 52
                    || phase == 54 || phase == 58 || phase == 74 || phase == 14 || phase == 19 || phase == 50)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var top = (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX) + body.Y + zombie.PresentationBiteBodyY;
                var height = Math.Max(0, body.Height - (zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0));
                var dx = Math.Max(0, Math.Max(body.X - centerX, centerX - body.X - body.Width));
                var dy = Math.Max(0, Math.Max(top - centerY, centerY - top - height));
                if (dx * dx + dy * dy <= 60 * 60)
                {
                    _explosionEffects.HitAdventureExplosionZombie(zombie, 1800f, false, 0x12);
                }
            }

            _ladderSystem.RemoveAdventureLaddersInBlast(centerX, centerY, 0);
        }

        /// <summary>Performs adventure potato mine distance.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "touching">The touching.</param>
        /// <returns>The resulting adventure potato mine distance.</returns>
        internal int AdventurePotatoMineDistance(Plant p, out bool touching)
        {
            touching = false;
            // PC 475D90 measures the light's distance. The separate 475AA0 trigger
            // admits edge contact and a 30-pixel gap for a zombie already eating.
            var left = 40 + p.Column * 80;
            var right = left + 55;
            var closest = 1000;
            foreach (var z in _world.Zombies)
            {
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(z) || z.Hypnotized || z.Airborne || z.Kind == ZombieKind.Boss || z.Row != p.Row || z.DropRemaining > 0 || z.PresentationNativeHeight == 9 || z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(z);
                var overlap = Math.Min(right, body.X + body.Width) - Math.Max(left, body.X);
                var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z);
                var triggerLeft = left + (z.Kind == ZombieKind.PoleVaulter ? 40 : 0);
                var triggerOverlap = Math.Min(right, body.X + body.Width) - Math.Max(triggerLeft, body.X);
                if (AdventureZombieTargetingRules.CanBeAutoTargeted(z, PlantKind.PotatoMine)
                    && (z.Kind != ZombieKind.Bungee || z.TargetColumn == p.Column)
                    && phase != 11 && phase != 12 && !(z.Kind == ZombieKind.Pogo && !z.UsedAbility) && triggerOverlap >= (z.Eating ? -30 : 0))
                {
                    touching = true;
                }

                var gap = Math.Max(0, -overlap);
                if (gap < closest)
                {
                    closest = gap;
                }
            }

            return closest;
        }
    }
}
