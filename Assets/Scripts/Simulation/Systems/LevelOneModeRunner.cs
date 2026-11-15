#nullable enable

using PlantsVsZombies.Levels;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LevelOneModeRunner responsibilities for one deterministic match.</summary>
    internal sealed class LevelOneModeRunner
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides observations to experimental callbacks.</summary>
        private readonly SimulationView _view;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LevelOneModeRunner(SimulationWorld world)
        {
            _world = world;
            _view = new SimulationView(world);
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(LegacySimulation legacySimulation, SunSystem sunSystem)
        {
            _legacySimulation = legacySimulation;
            _sunSystem = sunSystem;
        }

        /// <summary>Gets the level one tutorial, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal LevelOneTutorial RequiredLevelOneTutorial => _world._levelOneTutorial ?? throw new System.InvalidOperationException("The member '_levelOneTutorial' is not available.");
        /// <summary>Captured at construction; always false outside experimental 1-1.</summary>
        internal bool LevelOneUsesNativeSkySunRules
        {
            get
            {
                return _world.IsExperimentalLevelOne && _world._levelOneUseNativeSkySun;
            }
        }

        /// <summary>
        /// Read-only native pixel-space objects, distinct from the legacy grid-space Suns list.
        /// A reference remains readable after removal, but is no longer clickable.
        /// </summary>
        internal IReadOnlyList<LevelOneNativeSkySun> LevelOneNativeSkySuns
        {
            get
            {
                if (_world._levelOneNativeSkySunsView == null)
                {
                    _world._levelOneNativeSkySunsView = _world._levelOneNativeSkySuns.AsReadOnly();
                }

                return _world._levelOneNativeSkySunsView;
            }
        }

        /// <summary>Gets or sets level one native sky sun countdown ticks.</summary>
        internal int? LevelOneNativeSkySunCountdownTicks
        {
            get
            {
                return LevelOneUsesNativeSkySunRules ? (int? )_world._levelOneNativeSkySunState.Countdown : null;
            }
        }

        /// <summary>Gets or sets level one native sky sun drop count.</summary>
        internal int? LevelOneNativeSkySunDropCount
        {
            get
            {
                return LevelOneUsesNativeSkySunRules ? (int? )_world._levelOneNativeSkySunState.Drops : null;
            }
        }

        /// <summary>Tests one live object in native pixels; it does not select between overlapping objects.</summary>
        internal bool HitTestLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY)
        {
            return LevelOneUsesNativeSkySunRules && sun != null && _world._levelOneNativeSkySuns.Contains(sun) && global::PlantsVsZombies.Sun.SunPickupRules.HitTest(sun._fallState.Pickup, pixelX, pixelY, false);
        }

        /// <summary>
        /// Begins flight for the selected object. Credit and SunCollected occur
        /// only when a later effective native update reaches the target.
        /// </summary>
        internal bool TryClickLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY, int button = 0)
        {
            if (!LevelOneUsesNativeSkySunRules || _world.State != MatchState.Playing || sun == null || !_world._levelOneNativeSkySuns.Contains(sun))
            {
                return false;
            }

            global::PlantsVsZombies.Sun.SunPickupState pickup;
            if (!global::PlantsVsZombies.Sun.SunPickupRules.TryClick(sun._fallState.Pickup, pixelX, pixelY, button, true, false, false, out pickup))
            {
                return false;
            }

            sun._fallState = sun._fallState.WithPickup(pickup);
            _legacySimulation.Raise(GameEvent.SunPickupStarted);
            return true;
        }

        /// <summary>Mode detection for the unsodded background and single-row grass.</summary>
        internal bool LevelOneUsesSingleRowLawn
        {
            get
            {
                return _world.IsExperimentalLevelOne;
            }
        }

        /// <summary>Null outside this experiment; only native tutorial states 0-4 are modeled.</summary>
        internal LevelOneTutorialStage? CurrentLevelOneTutorialStage
        {
            get
            {
                return _world._levelOneTutorial == null ? (LevelOneTutorialStage? )null : _world._levelOneTutorial.Stage;
            }
        }

        /// <summary>Gets or sets level one selected seed.</summary>
        internal PlantKind? LevelOneSelectedSeed
        {
            get
            {
                return _world._levelOneSeedSelected ? (PlantKind? )PlantKind.Peashooter : null;
            }
        }

        /// <summary>Only the verified tutorial gate, not every reason a wave can be delayed.</summary>
        internal bool LevelOneTutorialBlocksWaveCountdown
        {
            get
            {
                return _world._levelOneTutorial != null && _world._levelOneTutorial.TutorialBlocksWaveCountdown;
            }
        }

        /// <summary>Gets or sets level one total waves.</summary>
        internal int LevelOneTotalWaves
        {
            get
            {
                return _world.IsExperimentalLevelOne ? LevelOneData.WaveCount : 0;
            }
        }

        /// <summary>Gets or sets level one total zombies.</summary>
        internal int LevelOneTotalZombies
        {
            get
            {
                if (!_world.IsExperimentalLevelOne)
                {
                    return 0;
                }

                var total = 0;
                for (var i = 0; i < LevelOneData.WaveCount; i++)
                {
                    total += LevelOneData.GetZombieCount(i);
                }

                return total;
            }
        }

        /// <summary>Coarse dispatched-batch fraction, not the original health-based HUD meter.</summary>
        internal float LevelOneWaveProgress
        {
            get
            {
                return _world.IsExperimentalLevelOne ? (float)_world.LevelOneWavesDispatched / LevelOneData.WaveCount : 0f;
            }
        }

        /// <summary>Approximate scheduler time; null when inactive or all batches are dispatched.</summary>
        internal double? LevelOneWaveCountdownSeconds
        {
            get
            {
                return !_world.IsExperimentalLevelOne || _world.LevelOneWavesDispatched >= LevelOneData.WaveCount ? (double? )null : Math.Max(0.0, _world._levelOneWaveCountdown);
            }
        }

        /// <summary>Uses the same zero-based row, column order as TryPlant.</summary>
        internal bool IsLevelOnePlantingCell(int row, int column)
        {
            return _world.IsExperimentalLevelOne && row >= 0 && row < SimulationWorld.Rows && column >= 0 && column < SimulationWorld.Columns && LevelOneData.IsGrassCell(column, row);
        }

        /// <summary>Checks experimental card selection without changing state.</summary>
        internal bool CanSelectLevelOneSeed(PlantKind kind)
        {
            if (!_world.IsExperimentalLevelOne || _world.State != MatchState.Playing || _world._levelOneSeedSelected || kind != PlantKind.Peashooter || _world._seedCooldownTicks[LevelOneData.PeashooterSeedId] > 0 || _world.SunAmount < LevelOneData.PeashooterCost)
            {
                return false;
            }

            return RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection || RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.ZombieOnslaughtPrompt;
        }

        /// <summary>Checks current experimental placement conditions without planting.</summary>
        internal bool CanPlantLevelOneSeedAt(int row, int column)
        {
            return _world.IsExperimentalLevelOne && _world.State == MatchState.Playing && _world._levelOneSeedSelected && IsLevelOnePlantingCell(row, column) && _world.SunAmount >= LevelOneData.PeashooterCost && _world._seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && _legacySimulation.PlantAt(row, column) == null;
        }

        /// <summary>
        /// Selects the only available seed when the card is usable. The host
        /// must call this before TryPlant in the experimental mode.
        /// </summary>
        internal bool TrySelectLevelOneSeed(PlantKind kind)
        {
            if (!CanSelectLevelOneSeed(kind))
            {
                return false;
            }

            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection)
            {
                if (!RequiredLevelOneTutorial.OnSeedPickedUp().Changed)
                {
                    return false;
                }
            }

            _world._levelOneSeedSelected = true;
            return true;
        }

        /// <summary>Cancel a held experimental seed; returns false if none is held or play is paused.</summary>
        internal bool CancelLevelOneSeedSelection()
        {
            if (!_world.IsExperimentalLevelOne || _world.State != MatchState.Playing || !_world._levelOneSeedSelected)
            {
                return false;
            }

            _world._levelOneSeedSelected = false;
            RequiredLevelOneTutorial.OnPlantingCursorCancelled();
            return true;
        }

        /// <summary>Performs initialize experimental level one.</summary>
        /// <param name = "initialized">The initialized.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal void InitializeExperimentalLevelOne(ref bool initialized)
        {
            var options = _world.Rules.LevelOneOptions;
            if (options == null)
            {
                throw new ArgumentNullException("LevelOneOptions");
            }

            if (!Finite(options.SecondsPerNativeTick) || options.SecondsPerNativeTick <= 0.0 || options.SecondsPerNativeTick * 200.0 > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick");
            }

            if (!Finite(options.InitialWaveDelaySeconds) || options.InitialWaveDelaySeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException("InitialWaveDelaySeconds");
            }

            if (!Finite(options.BetweenWaveDelaySeconds) || options.BetweenWaveDelaySeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException("BetweenWaveDelaySeconds");
            }

            if (!Finite(options.ZombieSpawnX))
            {
                throw new ArgumentOutOfRangeException("ZombieSpawnX");
            }

            if (!Finite(options.ZombieSpacingInWave) || options.ZombieSpacingInWave < 0f || Math.Abs((double)options.ZombieSpawnX) + options.ZombieSpacingInWave > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException("ZombieSpacingInWave");
            }

            if (!Finite(options.ZombieHealth) || options.ZombieHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException("ZombieHealth");
            }

            if (!Finite(options.SkySunTargetY))
            {
                throw new ArgumentOutOfRangeException("SkySunTargetY");
            }

            if (options.UseNativeSkySunRules && options.SecondsPerNativeTick < SimulationWorld.SimulationStep / 1024.0)
            {
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick", "Native sky rules allow at most 1024 updates per simulation step.");
            }

            _world._levelOneSecondsPerNativeTick = options.SecondsPerNativeTick;
            _world._levelOneWaveCountdown = options.InitialWaveDelaySeconds;
            _world._levelOneBetweenWaveDelay = options.BetweenWaveDelaySeconds;
            _world._levelOneZombieSpawnX = options.ZombieSpawnX;
            _world._levelOneZombieSpacing = options.ZombieSpacingInWave;
            _world._levelOneZombieHealth = options.ZombieHealth;
            _world._levelOneSkySunTargetY = options.SkySunTargetY;
            _world._levelOneAllowPlantRemoval = options.AllowPlantRemoval;
            _world._levelOneDelayProvider = options.BetweenWaveDelay;
            _world._levelOneCompletionCondition = options.CompletionCondition;
            _world._levelOneSkySunFactory = options.SkySunFactory;
            _world._levelOneTutorial = new LevelOneTutorial();
            _world._levelOneUseNativeSkySun = options.UseNativeSkySunRules;
            if (_world._levelOneUseNativeSkySun)
            {
                _world._levelOneNativeSkySunRandomDraw = options.NativeSkySunRandomDraw;
                _world._levelOneNativeSkySunState = global::PlantsVsZombies.Sun.SkySunRules.Initialize(0, DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.InitialDrawBound));
            }

            _world.SunAmount = LevelOneData.StartingSun;
            for (var row = 0; row < _world.Mowers.Count; row++)
            {
                if (row != LevelOneData.GrassRow)
                {
                    _world.Mowers[row].Available = false;
                }
            }

            initialized = true;
        }

        /// <summary>Performs can plant experimental level one.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "allowed">The allowed.</param>
        internal void CanPlantExperimentalLevelOne(PlantKind kind, int row, int column, ref bool allowed)
        {
            allowed = kind == PlantKind.Peashooter && CanPlantLevelOneSeedAt(row, column);
        }

        /// <summary>Performs can remove experimental level one plant.</summary>
        /// <param name = "allowed">The allowed.</param>
        internal void CanRemoveExperimentalLevelOnePlant(ref bool allowed)
        {
            allowed = _world._levelOneAllowPlantRemoval;
        }

        /// <summary>Performs on experimental level one plant placed.</summary>
        internal void OnExperimentalLevelOnePlantPlaced()
        {
            _world._levelOneSeedSelected = false;
            // This simulation removes plants immediately; Plants.Count is only
            // an approximation of the original data-array allocation count.
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingPlantPlacement)
            {
                ApplyLevelOneTutorialEffect(RequiredLevelOneTutorial.OnPlantPlaced(_world.Plants.Count));
            }
        }

        /// <summary>Performs on experimental level one sun collected.</summary>
        internal void OnExperimentalLevelOneSunCollected()
        {
            UpdateLevelOneSeedUsability();
        }

        /// <summary>Performs before experimental level one tick.</summary>
        internal void BeforeExperimentalLevelOneTick()
        {
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.NotStarted)
            {
                RequiredLevelOneTutorial.OnFirstActiveGameplayUpdate();
            }

            UpdateLevelOneSeedUsability();
        }

        /// <summary>Performs advance experimental level one native sky sun.</summary>
        /// <param name = "handled">The handled.</param>
        internal void AdvanceExperimentalLevelOneNativeSkySun(ref bool handled)
        {
            if (!_world._levelOneUseNativeSkySun)
            {
                return;
            }

            handled = true;
            _world._levelOneNativeSkySunElapsed += SimulationWorld.SimulationStep;
            var tolerance = Math.Min(_world._levelOneSecondsPerNativeTick * 1e-9, 1e-10);
            while (_world._levelOneNativeSkySunElapsed + tolerance >= _world._levelOneSecondsPerNativeTick && _world.State == MatchState.Playing)
            {
                _world._levelOneNativeSkySunElapsed -= _world._levelOneSecondsPerNativeTick;
                if (_world._levelOneNativeSkySunElapsed < 0.0)
                {
                    _world._levelOneNativeSkySunElapsed = 0.0;
                }

                TickLevelOneNativeSkySun();
                if (_world.State != MatchState.Playing)
                {
                    // The outer Update subtracts one simulation step after Advance returns.
                    _world._levelOneNativeSkySunElapsed = 0.0;
                    _world._accumulatedTime = SimulationWorld.SimulationStep;
                    return;
                }
            }
        }

        /// <summary>Performs tick level one native sky sun.</summary>
        internal void TickLevelOneNativeSkySun()
        {
            // In the examined board update, existing pickups precede the sky scheduler.
            for (var i = _world._levelOneNativeSkySuns.Count - 1; i >= 0; i--)
            {
                var sun = _world._levelOneNativeSkySuns[i];
                var boundedBalance = Math.Min(global::PlantsVsZombies.Sun.SunPickupRules.BalanceCap, Math.Max(0, _world.SunAmount));
                var tick = global::PlantsVsZombies.Sun.SkySunFall.Tick(sun._fallState, boundedBalance, true, true, false, 0);
                sun._fallState = tick.State;
                if (tick.State.Pickup.Dead)
                {
                    _world._levelOneNativeSkySuns.RemoveAt(i);
                }

                if (tick.Credited)
                {
                    _world.SunAmount = tick.Balance;
                    OnExperimentalLevelOneSunCollected();
                    _legacySimulation.Raise(GameEvent.SunCollected);
                }

                if (_world.State != MatchState.Playing)
                {
                    return;
                }
            }

            // This host assumes its unmodeled end-state and mode predicates are clear.
            var context = new global::PlantsVsZombies.Sun.SkySunContext(0, 0, true, false, 0, -1, (int)RequiredLevelOneTutorial.Stage, _world.Plants.Count, false);
            var needsDraws = global::PlantsVsZombies.Sun.SkySunRules.NeedsDraws(_world._levelOneNativeSkySunState, context);
            var xDraw = needsDraws ? DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.XDrawBound) : -1;
            var intervalDraw = needsDraws ? DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.IntervalDrawBound) : -1;
            var next = global::PlantsVsZombies.Sun.SkySunRules.Tick(_world._levelOneNativeSkySunState, context, xDraw, intervalDraw);
            if (next.Spawned)
            {
                var landingDraw = DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunFall.LandingDrawBound);
                _world._levelOneNativeSkySuns.Add(new LevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunFall.Initialize(next, landingDraw)));
            }

            _world._levelOneNativeSkySunState = next.State;
        }

        /// <summary>Performs draw level one native sky sun.</summary>
        /// <param name = "bound">The bound.</param>
        /// <returns>The resulting draw level one native sky sun.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal int DrawLevelOneNativeSkySun(int bound)
        {
            var draw = _world._levelOneNativeSkySunRandomDraw == null ? _world._random.Next(bound) : _world._levelOneNativeSkySunRandomDraw(bound);
            if (draw < 0 || draw >= bound)
            {
                throw new InvalidOperationException("Native sky-sun random draw is outside its requested bound.");
            }

            return draw;
        }

        /// <summary>Performs can drop experimental level one sky sun.</summary>
        /// <param name = "allowed">The allowed.</param>
        internal void CanDropExperimentalLevelOneSkySun(ref bool allowed)
        {
            var stage = RequiredLevelOneTutorial.Stage;
            allowed = (stage != LevelOneTutorialStage.AwaitingSeedSelection && stage != LevelOneTutorialStage.AwaitingPlantPlacement) || _world.Plants.Count > 0;
        }

        /// <summary>Performs spawn experimental level one sky sun.</summary>
        /// <param name = "handled">The handled.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void SpawnExperimentalLevelOneSkySun(ref bool handled)
        {
            var sun = _world._levelOneSkySunFactory == null ? _sunSystem.CreateSun(0.4f + (float)_world._random.NextDouble() * 8.2f, -1f, _world._levelOneSkySunTargetY)

            : _world._levelOneSkySunFactory(_view, _world._random);
            if (sun == null || !Finite(sun.X) || !Finite(sun.Y) || !Finite(sun.TargetY) || !Finite(sun.Age))
            {
                throw new InvalidOperationException("The experimental sky-sun factory must return a sun with finite fields.");
            }

            _world.Suns.Add(sun);
            handled = true;
        }

        /// <summary>Performs advance experimental level one waves.</summary>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void AdvanceExperimentalLevelOneWaves()
        {
            if (RequiredLevelOneTutorial.TutorialBlocksWaveCountdown || _world.LevelOneWavesDispatched >= LevelOneData.WaveCount)
            {
                return;
            }

            _world._levelOneWaveCountdown -= SimulationWorld.SimulationStep;
            if (_world._levelOneWaveCountdown > 0.000000001)
            {
                return;
            }

            var wave = _world.LevelOneWavesDispatched;
            var count = LevelOneData.GetZombieCount(wave);
            for (var index = 0; index < count; index++)
            {
                _world.Zombies.Add(new Zombie {
                    Kind = (ZombieKind)LevelOneData.GetZombieId(wave, index),
                    Row = LevelOneData.ZombieSpawnRow,
                    X = _world._levelOneZombieSpawnX + index * _world._levelOneZombieSpacing,
                    Health = _world._levelOneZombieHealth
                });
                _world.SpawnedZombies++;
            }

            _world.LevelOneWavesDispatched++;
            if (_world.LevelOneWavesDispatched < LevelOneData.WaveCount)
            {
                var delay = _world._levelOneDelayProvider == null ? _world._levelOneBetweenWaveDelay : _world._levelOneDelayProvider(_view, wave);
                if (!Finite(delay) || delay < 0.0)
                {
                    throw new InvalidOperationException("The experimental wave-delay callback must return a finite nonnegative value.");
                }

                _world._levelOneWaveCountdown = delay;
            }
        }

        /// <summary>Performs check experimental level one completion.</summary>
        internal void CheckExperimentalLevelOneCompletion()
        {
            if (_world.LevelOneWavesDispatched < LevelOneData.WaveCount)
            {
                return;
            }

            var complete = _world._levelOneCompletionCondition == null ? _world.Zombies.Count == 0 : _world._levelOneCompletionCondition(_view);
            if (!complete || _world.State != MatchState.Playing)
            {
                return;
            }

            _world.State = MatchState.Won;
            _legacySimulation.Raise(GameEvent.Won);
        }

        /// <summary>Performs update level one seed usability.</summary>
        internal void UpdateLevelOneSeedUsability()
        {
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingUsableSeed && _world._seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && _world.SunAmount >= LevelOneData.PeashooterCost)
            {
                RequiredLevelOneTutorial.OnSeedBecameUsable();
            }
        }

        /// <summary>Performs apply level one tutorial effect.</summary>
        /// <param name = "effect">The effect.</param>
        internal void ApplyLevelOneTutorialEffect(LevelOneTutorialEffect effect)
        {
            if (effect.SkySunCountdownNativeTicks.HasValue)
            {
                if (_world._levelOneUseNativeSkySun)
                {
                    _world._levelOneNativeSkySunState = new global::PlantsVsZombies.Sun.SkySunState(effect.SkySunCountdownNativeTicks.Value, _world._levelOneNativeSkySunState.Drops);
                }
                else
                {
                    _world._nextSkySun = (float)(effect.SkySunCountdownNativeTicks.Value * _world._levelOneSecondsPerNativeTick);
                }
            }

            if (effect.WaveCountdownAndInitialNativeTicks.HasValue)
            {
                _world._levelOneWaveCountdown = effect.WaveCountdownAndInitialNativeTicks.Value * _world._levelOneSecondsPerNativeTick;
            }
        }

        /// <summary>Performs finite.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting finite.</returns>
        internal static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
