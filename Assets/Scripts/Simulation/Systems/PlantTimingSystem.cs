#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PlantTimingSystem responsibilities for one deterministic match.</summary>
    internal sealed class PlantTimingSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ExplosivePlantActions operations.</summary>
        private ExplosivePlantActions _explosivePlantActions = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PlantTimingSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="explosivePlantActions">The ExplosivePlantActions collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, ExplosivePlantActions explosivePlantActions, ProjectileSystem projectileSystem, SpecialPlantActions specialPlantActions, SunSystem sunSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _explosivePlantActions = explosivePlantActions;
            _projectileSystem = projectileSystem;
            _specialPlantActions = specialPlantActions;
            _sunSystem = sunSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs initialize adventure native plant timing.</summary>
        /// <param name = "plant">The plant.</param>
        internal void InitializeAdventureNativePlantTiming(Plant plant)
        {
            plant.Behavior.Initialize(plant, _behaviorContext);
            if (!plant.Timing.HasValue && !plant.MultiHeadTiming.HasValue && plant.StarfruitLaunchCountdown < 0
                && plant.Kind != PlantKind.SunShroom && plant.Kind != PlantKind.Marigold
                && global::PlantsVsZombies.VerifiedData.VerifiedDataCatalog.TryGetPlant((int)plant.Kind, out var definition)
                && definition.ActionPeriodRaw > 0)
            {
                // PC 46C1E3 also initializes counters that a species-specific state machine may later ignore.
                var countdown = plant.Kind == PlantKind.TwinSunflower ? 300 + _world._random.Next(951)
                    : _world._random.Next(definition.ActionPeriodRaw + 1);
                plant.ActionTimer = countdown * .01f;
            }
        }
        /// <summary>Provides the plant behavior context after all collaborators are connected.</summary>
        /// <param name="context">The initialized plant action dependencies.</param>
        internal void SetBehaviorContext(PlantBehaviorContext context)
        {
            _behaviorContext = context;
        }
        /// <summary>Retains the plant behavior dependencies.</summary>
        private PlantBehaviorContext _behaviorContext = null!;
        /// <summary>Initializes the reusable timed-shooter capability.</summary>
        /// <param name="plant">The plant selected by the behavior factory.</param>
        internal void InitializeTimedPlant(Plant plant)
        {
            var kind = (global::PlantsVsZombies.Timing.PlantKind)(int)plant.Kind;
            plant.Timing = global::PlantsVsZombies.Timing.PlantTiming.Initialize(kind, _world._random.Next(global::PlantsVsZombies.Timing.PlantTiming.InitialRandomBound(kind)));
            plant.ActionTimer = plant.Timing.Value.Countdown * .01f;
                }

        /// <summary>Performs advance adventure native timed plant.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting advance adventure native timed plant.</returns>
        internal bool AdvanceAdventureNativeTimedPlant(Plant plant)
        {
            return plant.Behavior.AdvanceNative(plant, _behaviorContext);
        }
        /// <summary>Advances the shared shooter timing capability after species dispatch.</summary>
        /// <param name="plant">The active timed plant.</param>
        /// <returns>Whether native timing owns the action.</returns>
        internal bool AdvanceTimedPlant(Plant plant)
        {
            if (plant.ActionTimer >= 100000f)
            {
                return false;
            }

            if (!plant.Timing.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var sunflower = plant.Kind == PlantKind.Sunflower;
            var shortPuff = AdventurePuffShroomRules.IsShortRange(plant.Kind);
            var fume = plant.Kind == PlantKind.FumeShroom;
            var kernel = plant.Kind == PlantKind.KernelPult;
            var scaredy = plant.Kind == PlantKind.ScaredyShroom;
            var state = plant.RequiredTiming;
            var pendingProjectiles = 0;
            if (scaredy)
            {
                // UpdateShooting precedes UpdateScaredyShroom, which in turn precedes UpdateShooter.
                var shooting = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                    new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, false, false, false,
                        global::PlantsVsZombies.Timing.ShootingTrack.Body, plant.PresentationShotAge * 35f >= 15f, 0));
                pendingProjectiles = shooting.ProjectilesCreated;
                AdvanceAdventureScaredyShroom(plant, state.ShootingCountdown > 0 && shooting.State.ShootingCountdown == 0,
                    shooting.State.ShootingCountdown > 0);
                state = shooting.State;
                if (plant.ScaredyShroomPresentation!.NativePhase != 1)
                {
                    state = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(state.Kind, 150, state.ShootingCountdown);
                }
            }

            var lobber = plant.Kind == PlantKind.CabbagePult || kernel || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var targetZombie = fume || shortPuff || sunflower ? null : lobber ? _boardQueries.FindAdventureLobberTarget(plant.Row, plant.Column)
                : _boardQueries.FindAdventureTarget(plant.Row, plant.ModeFacesLeft ? -1f : plant.Column + .4f,
                    plant.ModeFacesLeft ? plant.Column + .5f : 20f, false);
            var target = fume ? _boardQueries.FindAdventureFumeTarget(plant) != null : shortPuff ? _boardQueries.FindAdventurePuffTarget(plant) != null
                : !sunflower && (targetZombie != null || !plant.ModeFacesLeft && _projectileSystem.CanShootAdventureBoss(plant))
                    && (!scaredy || plant.ScaredyShroomPresentation!.NativePhase == 1);
            // PC 0x46D240 starts the body animation and its species-specific shooting counter together.
            var track = shortPuff || fume || scaredy || plant.Kind == PlantKind.CabbagePult || kernel
                || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon
                ? global::PlantsVsZombies.Timing.ShootingTrack.Body : global::PlantsVsZombies.Timing.ShootingTrack.Head;
            var resetReady = !lobber || plant.PresentationShotAge < 0f || plant.PresentationLobberIdleAge >= 0f;
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, !scaredy, true, true, target, track, resetReady, 0);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            var draw = bound == 0 ? 0 : _world._random.Next(bound);
            var butter = kernel && state.Countdown <= 1 && target && _world._random.Next(4) == 0;
            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, !scaredy, true, true, target, track, resetReady, draw, butter));
            plant.Timing = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            if (result.AnimationStarted)
            {
                plant.PresentationCoffeeWakeAge = -1f;
                if (lobber)
                {
                    plant.PresentationLobberIdleAge = -1f;
                }

                if (kernel)
                {
                    plant.PresentationButter = butter;
                    plant._kernelWeaponPrepared = true;
                }

                plant.BlinkAnimation = null;
                plant.PresentationShotAge = 0;
                plant.PresentationHeadAge[0] = 0;
                plant._presentationPrefiring = true;
                plant.ShootAge = 0;
            }

            if (result.SunCreated)
            {
                _world.Suns.Add(_sunSystem.CreateAdventurePresentationSun(plant, 25));
                _zombieFeedbackSystem.RequestAdventureZombieFoley(4);
            }

            for (var i = 0; i < result.ProjectilesCreated + pendingProjectiles; i++)
            {
                if (fume)
                {
                    _projectileSystem.FireAdventureFume(plant);
                    continue;
                }

                if (scaredy || plant.Kind == PlantKind.CabbagePult || kernel || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon)
                {
                    _projectileSystem.FireAdventureNativeTimedProjectile(plant);
                    continue;
                }

                var shot = _projectileSystem.FireAdventurePea(plant, plant.Row, plant.ModeFacesLeft, 20f);
                shot.IsSnow = plant.Kind == PlantKind.SnowPea;
                if (shortPuff)
                {
                    shot.Range = float.MaxValue;
                }
            }

            return true;
                }

        /// <summary>Runs PC UpdateShooting, UpdateCactus and UpdateShooter in their original order.</summary>
        /// <param name="plant">The live cactus whose body and native weapon state are being advanced.</param>
        internal void AdvanceAdventureCactus(Plant plant)
        {
            if (!plant.Timing.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var pose = plant.CactusPresentation;
            if (pose == null)
            {
                pose = _world.LifetimePool.Rent<AdventureCactusState>();
                pose.InitializePooledRate(plant.PresentationRate);
                plant.CactusPresentation = pose;
            }

            pose.AdvanceAnimation();
            var state = plant.RequiredTiming;
            var shooting = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, false, false, false,
                    global::PlantsVsZombies.Timing.ShootingTrack.Body, pose.AnimationFinished, 0));
            if (shooting.ProjectilesCreated > 0)
            {
                _projectileSystem.FireAdventureNativeTimedProjectile(plant);
                plant.PresentationShotAge = pose.SampleSeconds(0f);
            }

            if (state.ShootingCountdown > 0 && shooting.State.ShootingCountdown == 0)
            {
                pose.FinishShooting();
            }

            var airTarget = false;
            var groundTarget = false;
            foreach (var zombie in _world.Zombies)
            {
                if (AdventureCactusState.CanTarget(plant, zombie, true))
                {
                    airTarget = true;
                }
                else if (AdventureCactusState.CanTarget(plant, zombie, false))
                {
                    groundTarget = true;
                }
            }

            var previousPhase = pose.NativePhase;
            pose.Update(airTarget, shooting.State.ShootingCountdown > 0);
            state = shooting.State;
            if (previousPhase != 31 && pose.NativePhase == 31)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(63);
            }
            else if (previousPhase == 31 && pose.NativePhase == 32)
            {
                // PC 0x46F625 resets the launch counter before this tick's UpdateShooter.
                state = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(state.Kind, 1, state.ShootingCountdown);
            }

            var high = pose.NativePhase == 32;
            var target = high ? airTarget : pose.NativePhase == 30 && (groundTarget || _projectileSystem.CanShootAdventureBoss(plant));
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, false, true, false, target,
                global::PlantsVsZombies.Timing.ShootingTrack.Body, true, 0, cactusHigh: high);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, false, true, false, target,
                    global::PlantsVsZombies.Timing.ShootingTrack.Body, true, bound == 0 ? 0 : _world._random.Next(bound), cactusHigh: high));
            plant.Timing = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            plant.PresentationBiteNativeState = pose.NativePhase;
            if (result.AnimationStarted)
            {
                pose.BeginShooting();
                plant.BlinkAnimation = null;
                plant.PresentationShotAge = 0f;
                plant._presentationPrefiring = true;
                plant.ShootAge = 0f;
            }
        }

        /// <summary>Updates the original Scaredy-shroom state using the native circle and actual zombie rectangles.</summary>
        /// <param name="plant">The awake mushroom whose body is being advanced.</param>
        /// <param name="finishedShooting">Whether UpdateShooting just restored the original idle animation.</param>
        /// <param name="shooting">Whether the current original shooting animation still owns the body.</param>
        internal void AdvanceAdventureScaredyShroom(Plant plant, bool finishedShooting, bool shooting)
        {
            var state = plant.ScaredyShroomPresentation;
            if (state == null)
            {
                state = _world.LifetimePool.Rent<AdventureScaredyShroomState>();
                plant.ScaredyShroomPresentation = state;
            }

            var nearby = false;
            if (!shooting)
            {
                var plantY = 80 + plant.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - plant.Column) * 20f - 10f) : 0);
                foreach (var zombie in _world.Zombies)
                {
                    var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                    var body = AdventureZombieTargetingRules.BodyRectangle(zombie, (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX));
                    var bodyY = body.Y + zombie.PresentationBiteBodyY;
                    var clip = zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0;
                    if (zombie.GraveRiseTicks > 0)
                    {
                        var poolRise = zombie.PoolState != null && zombie.PresentationInPool == true;
                        var altitude = (int)(poolRise ? AdventureZombiePoolRules.BodyAltitude(zombie) : AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks));
                        bodyY -= altitude;
                        clip = -altitude + (poolRise ? 0 : Math.Min(zombie.GraveRiseTicks, 40));
                    }
                    else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
                    {
                        var altitude = AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks);
                        bodyY -= altitude;
                        clip = -altitude;
                    }

                    if (AdventureScaredyShroomRules.CanScare(plant, zombie, body, bodyY, body.Height - clip, plantY))
                    {
                        nearby = true;
                        break;
                    }
                }
            }

            var randomUnit = state.NeedsRandom(nearby, shooting) ? (float)_world._random.NextDouble() : 0f;
            state.Tick(nearby, shooting, randomUnit, finishedShooting);
            plant.PresentationBiteNativeState = state.NativePhase;
        }

        /// <summary>Advances the original shared shooting countdown of a multi-head plant.</summary>
        /// <param name="plant">The Threepeater or Split Pea being updated.</param>
        internal void AdvanceAdventureMultiHeadPlant(Plant plant)
        {
            if (!plant.MultiHeadTiming.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var available = plant.Kind == PlantKind.SplitPea ? AdventureMultiHeadTimingRules.SplitPeaHeads : AdventureShootingHeads.Second;
            var targets = AdventureShootingHeads.None;
            if (plant.Kind == PlantKind.Threepeater)
            {
                if (plant.Row + 1 < _world.RowCount)
                {
                    available |= AdventureShootingHeads.First;
                }

                if (plant.Row > 0)
                {
                    available |= AdventureShootingHeads.Third;
                }
            }

            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Hypnotized || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0f || zombie.Kind == ZombieKind.Bungee
                    || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var plantX = 40 + plant.Column * 80;
                var forward = body.X + body.Width >= plantX + 60 && body.X <= plantX + 860;
                if (plant.Kind == PlantKind.SplitPea && zombie.Row == plant.Row)
                {
                    if (forward)
                    {
                        targets |= AdventureShootingHeads.First;
                    }

                    if (body.X + body.Width >= 0 && body.X <= plantX + 16)
                    {
                        targets |= AdventureShootingHeads.Second;
                    }
                }
                else if (plant.Kind == PlantKind.Threepeater && forward)
                {
                    if (zombie.Row == plant.Row + 1)
                    {
                        targets |= AdventureShootingHeads.First;
                    }
                    else if (zombie.Row == plant.Row)
                    {
                        targets |= AdventureShootingHeads.Second;
                    }
                    else if (zombie.Row == plant.Row - 1)
                    {
                        targets |= AdventureShootingHeads.Third;
                    }
                }
            }

            var state = plant.RequiredMultiHeadTiming;
            var input = new AdventureMultiHeadTimingInput(true, true, true, targets, available, true, 0);
            var bound = AdventureMultiHeadTimingRules.RequiredRandomBound(state, input);
            var result = AdventureMultiHeadTimingRules.Tick(state,
                new AdventureMultiHeadTimingInput(true, true, true, targets, available, true, bound == 0 ? 0 : _world._random.Next(bound)));
            plant.MultiHeadTiming = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            for (var head = 0; head < 3; head++)
            {
                var mask = (AdventureShootingHeads)(1 << head);
                if ((result.FiredHeads & mask) != 0)
                {
                    var row = plant.Kind == PlantKind.Threepeater ? plant.Row + 1 - head : plant.Row;
                    var back = plant.Kind == PlantKind.SplitPea && head == 1;
                    _projectileSystem.FireAdventurePea(plant, row, back, 20f);
                }

                if ((result.StoppedHeads & mask) != 0)
                {
                    plant.PresentationHeadAge[head] = -1f;
                }

                if ((result.StartedHeads & mask) != 0)
                {
                    plant.PresentationHeadAge[head] = 0f;
                    plant.BlinkAnimation = null;
                    plant.PresentationShotAge = 0f;
                    plant.ShootAge = 0f;
                }
            }
        }

        /// <summary>Advances the original Starfruit launch and forty-tick body shooting counters.</summary>
        /// <param name="plant">The Starfruit being updated.</param>
        internal void AdvanceAdventureStarfruit(Plant plant)
        {
            if (plant.StarfruitLaunchCountdown < 0)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            if (plant.StarfruitShootingCountdown > 0)
            {
                var countdown = plant.StarfruitShootingCountdown;
                if (AdventureStarfruitRules.AdvanceAttack(ref countdown, true))
                {
                    _projectileSystem.FireAdventureStarfruit(plant);
                }

                plant.StarfruitShootingCountdown = countdown;
            }

            plant.StarfruitLaunchCountdown--;
            plant.ActionTimer = Math.Max(0, plant.StarfruitLaunchCountdown) * .01f;
            if (plant.StarfruitLaunchCountdown > 0)
            {
                return;
            }

            plant.StarfruitLaunchCountdown = 150 - _world._random.Next(15);
            plant.ActionTimer = plant.StarfruitLaunchCountdown * .01f;
            if (plant.RecentlyEatenTicks <= 0 && _projectileSystem.FindAdventureStarfruitTarget(plant) == null)
            {
                return;
            }

            plant.StarfruitShootingCountdown = AdventureStarfruitRules.ShootingCountdown;
            plant.BlinkAnimation = null;
            plant.PresentationShotAge = 0;
            plant.PresentationHeadAge[0] = 0;
            plant._presentationPrefiring = true;
            plant.ShootAge = 0;
        }
        /// <summary>Performs record adventure plant hit.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "nativeTicks">The native ticks.</param>
        internal void RecordAdventurePlantHit(Plant plant, int nativeTicks = 25)
        {
            if (plant != null)
            {
                plant.PresentationEatenFlashTicks = Math.Max(plant.PresentationEatenFlashTicks, nativeTicks);
            }
        }

        /// <summary>Performs advance adventure plant hit counters.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventurePlantHitCounters(float dt)
        {
            _world._nativePlantHitTickRemainder += dt * 100f;
            var ticks = (int)Math.Floor(_world._nativePlantHitTickRemainder + .00001f);
            _world._nativePlantHitTickRemainder -= ticks;
            foreach (var p in _world.Plants)
            {
                p.PresentationEatenFlashTicks = Math.Max(0, p.PresentationEatenFlashTicks - ticks);
                p.RecentlyEatenTicks = Math.Max(0, p.RecentlyEatenTicks - ticks);
            }

            for (var i = _world.Presentation.PlantBiteEffects.Count - 1; i >= 0; i--)
            {
                var e = _world.Presentation.PlantBiteEffects[i];
                e.Age += dt;
                if (e.Age >= 2f)
                {
                    _world.Presentation.PlantBiteEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs record adventure chew effect.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        internal void RecordAdventureChewEffect(Zombie z, Plant? p)
        {
            if (p == null)
            {
                return;
            }

            RecordAdventurePlantHit(p);
            if (p.Kind != PlantKind.Wallnut && p.Kind != PlantKind.TallNut)
            {
                return;
            }

            float x = 37f, y = 40f;
            if (z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.DolphinRider)
            {
                x -= 7f;
                y += 70f;
            }
            else if (z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)))
            {
                x += 47f;
            }
            else if (z.Kind == ZombieKind.Balloon)
            {
                y += 47f;
            }
            else if (z.Kind == ZombieKind.Imp)
            {
                x += 24f;
                y += 40f;
            }

            var pooledRecord1 = _world.LifetimePool.Rent<AdventurePlantBiteEffect>();
            pooledRecord1.Id = ++_world.Presentation._effectId;
            pooledRecord1.ParticleId = 35;
            pooledRecord1.Row = z.Row;
            pooledRecord1.Kind = z.Kind;
            pooledRecord1.NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            pooledRecord1.NativeY = _biteSystem.AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)));
            pooledRecord1.OffsetX = x;
            pooledRecord1.OffsetY = y + z.PresentationBiteBodyY;
            pooledRecord1.Seed = z.Id * 397 ^ _world.Presentation._effectId;
            _world.Presentation.PlantBiteEffects.Add(pooledRecord1);
        }
    }
}
