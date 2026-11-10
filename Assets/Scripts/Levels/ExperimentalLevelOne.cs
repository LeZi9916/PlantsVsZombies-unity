using System;
using System.Collections.Generic;
using PvZReplica.Levels;
using OriginalSun = PvZUnity.Sun;

namespace PvZReplica
{
    /// <summary>
    /// A live experimental sky pickup in the original 800x600 pixel space.
    /// Position is its native top-left, not a Unity or grid coordinate.
    /// </summary>
    public sealed class LevelOneNativeSkySun
    {
        public const int BaseWidthPixels = 60;
        public const int BaseHeightPixels = 60;

        internal OriginalSun.SkySunFallState FallState;

        internal LevelOneNativeSkySun(OriginalSun.SkySunFallState state) { FallState = state; }

        public float PixelX { get { return FallState.Pickup.X; } }
        public float PixelY { get { return FallState.Pickup.Y; } }
        public int LandingPixelY { get { return FallState.LandingY; } }
        public OriginalSun.SunKind Kind { get { return FallState.Pickup.Kind; } }
        public bool IsCollecting { get { return FallState.Pickup.Collecting; } }
        public bool IsDead { get { return FallState.Pickup.Dead; } }
        public int GroundAgeTicks { get { return FallState.Pickup.GroundAge; } }
        public int FadeTicksRemaining { get { return FallState.Pickup.FadeCountdown; } }
    }

    public sealed partial class GameRules
    {
        /// <summary>Used only when EnableExperimentalLevelOne is true.</summary>
        public ExperimentalLevelOneOptions LevelOneOptions = new ExperimentalLevelOneOptions();
    }

    /// <summary>
    /// Replaceable approximations for the incomplete 1-1 experiment. None of
    /// these defaults is a claim about the original game's complete behavior.
    /// Values and callbacks are captured when the simulation is constructed.
    /// </summary>
    public sealed class ExperimentalLevelOneOptions
    {
        // The conversion from the observed native counters to seconds is not verified.
        public double SecondsPerNativeTick = 0.01;
        public double InitialWaveDelaySeconds = 18.0;
        public double BetweenWaveDelaySeconds = 25.0;
        public float ZombieSpawnX = 9.5f;
        public float ZombieSpacingInWave = 0.45f;
        public float ZombieHealth = 270f;
        public float SkySunTargetY = 2.5f;
        public bool AllowPlantRemoval = false;

        /// <summary>
        /// Opt in to the separate pixel-space sky-sun model. When enabled,
        /// SkySunInterval, SkySunTargetY and SkySunFactory do not schedule sky
        /// suns; legacy and plant suns in GameSimulation.Suns remain unchanged.
        /// </summary>
        public bool UseNativeSkySunRules = false;

        /// <summary>
        /// Optional bounded draw source for native sky rules. Null uses the
        /// simulation's shared System.Random. Each result must be in [0, bound).
        /// This does not reproduce the EXE's global RNG sequence.
        /// </summary>
        public Func<int, int> NativeSkySunRandomDraw;

        /// <summary>
        /// Optional replacement for the fixed inter-wave delay. Called after a
        /// batch is dispatched with its zero-based index, except after the last
        /// batch. The result must be finite and nonnegative; zero still waits
        /// until the next simulation tick.
        /// </summary>
        public Func<GameSimulation, int, double> BetweenWaveDelay;

        /// <summary>
        /// Optional completion policy, evaluated only after all four batches
        /// have been dispatched. Null uses the provisional immediate-win rule:
        /// the simulation's zombie list is empty. No reward is created.
        /// </summary>
        public Func<GameSimulation, bool> CompletionCondition;

        /// <summary>
        /// Optional replacement for the approximate single-row sky-sun
        /// creation. It must return a non-null sun with finite coordinates.
        /// When native sky rules are disabled, Rules.SkySunInterval controls
        /// repetition and can disable sky sun.
        /// The supplied Random is the simulation's shared random source.
        /// </summary>
        public Func<GameSimulation, Random, Sun> SkySunFactory;
    }

    public sealed partial class GameSimulation
    {
        private LevelOneTutorial levelOneTutorial;
        private bool levelOneSeedSelected;
        private bool levelOneAllowPlantRemoval;
        private double levelOneSecondsPerNativeTick;
        private double levelOneWaveCountdown;
        private double levelOneBetweenWaveDelay;
        private float levelOneZombieSpawnX;
        private float levelOneZombieSpacing;
        private float levelOneZombieHealth;
        private float levelOneSkySunTargetY;
        private Func<GameSimulation, int, double> levelOneDelayProvider;
        private Func<GameSimulation, bool> levelOneCompletionCondition;
        private Func<GameSimulation, Random, Sun> levelOneSkySunFactory;
        private bool levelOneUseNativeSkySun;
        private Func<int, int> levelOneNativeSkySunRandomDraw;
        private OriginalSun.SkySunState levelOneNativeSkySunState;
        private double levelOneNativeSkySunElapsed;
        private readonly List<LevelOneNativeSkySun> levelOneNativeSkySuns = new List<LevelOneNativeSkySun>();
        private IReadOnlyList<LevelOneNativeSkySun> levelOneNativeSkySunsView;

        /// <summary>Captured at construction; always false outside experimental 1-1.</summary>
        public bool LevelOneUsesNativeSkySunRules { get { return IsExperimentalLevelOne && levelOneUseNativeSkySun; } }

        /// <summary>
        /// Read-only native pixel-space objects, distinct from the legacy grid-space Suns list.
        /// A reference remains readable after removal, but is no longer clickable.
        /// </summary>
        public IReadOnlyList<LevelOneNativeSkySun> LevelOneNativeSkySuns
        {
            get
            {
                if (levelOneNativeSkySunsView == null) levelOneNativeSkySunsView = levelOneNativeSkySuns.AsReadOnly();
                return levelOneNativeSkySunsView;
            }
        }

        public int? LevelOneNativeSkySunCountdownTicks
        {
            get { return LevelOneUsesNativeSkySunRules ? (int?)levelOneNativeSkySunState.Countdown : null; }
        }

        public int? LevelOneNativeSkySunDropCount
        {
            get { return LevelOneUsesNativeSkySunRules ? (int?)levelOneNativeSkySunState.Drops : null; }
        }

        /// <summary>Tests one live object in native pixels; it does not select between overlapping objects.</summary>
        public bool HitTestLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY)
        {
            return LevelOneUsesNativeSkySunRules && sun != null && levelOneNativeSkySuns.Contains(sun) &&
                OriginalSun.SunPickupRules.HitTest(sun.FallState.Pickup, pixelX, pixelY, false);
        }

        /// <summary>
        /// Begins flight for the selected object. Credit and SunCollected occur
        /// only when a later effective native update reaches the target.
        /// </summary>
        public bool TryClickLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY, int button = 0)
        {
            if (!LevelOneUsesNativeSkySunRules || State != MatchState.Playing || sun == null ||
                !levelOneNativeSkySuns.Contains(sun)) return false;
            OriginalSun.SunPickupState pickup;
            if (!OriginalSun.SunPickupRules.TryClick(sun.FallState.Pickup, pixelX, pixelY, button,
                true, false, false, out pickup)) return false;
            sun.FallState = sun.FallState.WithPickup(pickup);
            Raise(GameEvent.SunPickupStarted);
            return true;
        }

        /// <summary>Mode detection for the unsodded background and single-row grass.</summary>
        public bool LevelOneUsesSingleRowLawn { get { return IsExperimentalLevelOne; } }

        /// <summary>Null outside this experiment; only native tutorial states 0-4 are modeled.</summary>
        public LevelOneTutorialStage? CurrentLevelOneTutorialStage
        {
            get { return levelOneTutorial == null ? (LevelOneTutorialStage?)null : levelOneTutorial.Stage; }
        }

        public PlantKind? LevelOneSelectedSeed
        {
            get { return levelOneSeedSelected ? (PlantKind?)PlantKind.Peashooter : null; }
        }

        /// <summary>Only the verified tutorial gate, not every reason a wave can be delayed.</summary>
        public bool LevelOneTutorialBlocksWaveCountdown
        {
            get { return levelOneTutorial != null && levelOneTutorial.TutorialBlocksWaveCountdown; }
        }

        public int LevelOneWavesDispatched { get; private set; }
        public int LevelOneTotalWaves { get { return IsExperimentalLevelOne ? LevelOneData.WaveCount : 0; } }
        public int LevelOneTotalZombies
        {
            get
            {
                if (!IsExperimentalLevelOne) return 0;
                int total = 0;
                for (int i = 0; i < LevelOneData.WaveCount; i++) total += LevelOneData.GetZombieCount(i);
                return total;
            }
        }

        /// <summary>Coarse dispatched-batch fraction, not the original health-based HUD meter.</summary>
        public float LevelOneWaveProgress
        {
            get { return IsExperimentalLevelOne ? (float)LevelOneWavesDispatched / LevelOneData.WaveCount : 0f; }
        }

        /// <summary>Approximate scheduler time; null when inactive or all batches are dispatched.</summary>
        public double? LevelOneWaveCountdownSeconds
        {
            get
            {
                return !IsExperimentalLevelOne || LevelOneWavesDispatched >= LevelOneData.WaveCount
                    ? (double?)null : Math.Max(0.0, levelOneWaveCountdown);
            }
        }

        /// <summary>Uses the same zero-based row, column order as TryPlant.</summary>
        public bool IsLevelOnePlantingCell(int row, int column)
        {
            return IsExperimentalLevelOne && row >= 0 && row < Rows && column >= 0 && column < Columns &&
                LevelOneData.IsGrassCell(column, row);
        }

        /// <summary>Checks experimental card selection without changing state.</summary>
        public bool CanSelectLevelOneSeed(PlantKind kind)
        {
            if (!IsExperimentalLevelOne || State != MatchState.Playing || levelOneSeedSelected ||
                kind != PlantKind.Peashooter || seedCooldownTicks[LevelOneData.PeashooterSeedId] > 0 ||
                SunAmount < LevelOneData.PeashooterCost)
                return false;
            return levelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection ||
                levelOneTutorial.Stage == LevelOneTutorialStage.ZombieOnslaughtPrompt;
        }

        /// <summary>Checks current experimental placement conditions without planting.</summary>
        public bool CanPlantLevelOneSeedAt(int row, int column)
        {
            return IsExperimentalLevelOne && State == MatchState.Playing && levelOneSeedSelected &&
                IsLevelOnePlantingCell(row, column) && SunAmount >= LevelOneData.PeashooterCost &&
                seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && PlantAt(row, column) == null;
        }

        /// <summary>
        /// Selects the only available seed when the card is usable. The host
        /// must call this before TryPlant in the experimental mode.
        /// </summary>
        public bool TrySelectLevelOneSeed(PlantKind kind)
        {
            if (!CanSelectLevelOneSeed(kind)) return false;
            if (levelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection)
            {
                if (!levelOneTutorial.OnSeedPickedUp().Changed) return false;
            }
            levelOneSeedSelected = true;
            return true;
        }

        /// <summary>Cancel a held experimental seed; returns false if none is held or play is paused.</summary>
        public bool CancelLevelOneSeedSelection()
        {
            if (!IsExperimentalLevelOne || State != MatchState.Playing || !levelOneSeedSelected) return false;
            levelOneSeedSelected = false;
            levelOneTutorial.OnPlantingCursorCancelled();
            return true;
        }

        partial void InitializeExperimentalLevelOne(ref bool initialized)
        {
            ExperimentalLevelOneOptions options = Rules.LevelOneOptions;
            if (options == null) throw new ArgumentNullException("LevelOneOptions");
            if (!Finite(options.SecondsPerNativeTick) || options.SecondsPerNativeTick <= 0.0 ||
                options.SecondsPerNativeTick * 200.0 > float.MaxValue)
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick");
            if (!Finite(options.InitialWaveDelaySeconds) || options.InitialWaveDelaySeconds < 0.0)
                throw new ArgumentOutOfRangeException("InitialWaveDelaySeconds");
            if (!Finite(options.BetweenWaveDelaySeconds) || options.BetweenWaveDelaySeconds < 0.0)
                throw new ArgumentOutOfRangeException("BetweenWaveDelaySeconds");
            if (!Finite(options.ZombieSpawnX)) throw new ArgumentOutOfRangeException("ZombieSpawnX");
            if (!Finite(options.ZombieSpacingInWave) || options.ZombieSpacingInWave < 0f ||
                Math.Abs((double)options.ZombieSpawnX) + options.ZombieSpacingInWave > float.MaxValue)
                throw new ArgumentOutOfRangeException("ZombieSpacingInWave");
            if (!Finite(options.ZombieHealth) || options.ZombieHealth <= 0f)
                throw new ArgumentOutOfRangeException("ZombieHealth");
            if (!Finite(options.SkySunTargetY)) throw new ArgumentOutOfRangeException("SkySunTargetY");
            if (options.UseNativeSkySunRules && options.SecondsPerNativeTick < SimulationStep / 1024.0)
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick", "Native sky rules allow at most 1024 updates per simulation step.");

            levelOneSecondsPerNativeTick = options.SecondsPerNativeTick;
            levelOneWaveCountdown = options.InitialWaveDelaySeconds;
            levelOneBetweenWaveDelay = options.BetweenWaveDelaySeconds;
            levelOneZombieSpawnX = options.ZombieSpawnX;
            levelOneZombieSpacing = options.ZombieSpacingInWave;
            levelOneZombieHealth = options.ZombieHealth;
            levelOneSkySunTargetY = options.SkySunTargetY;
            levelOneAllowPlantRemoval = options.AllowPlantRemoval;
            levelOneDelayProvider = options.BetweenWaveDelay;
            levelOneCompletionCondition = options.CompletionCondition;
            levelOneSkySunFactory = options.SkySunFactory;
            levelOneTutorial = new LevelOneTutorial();
            levelOneUseNativeSkySun = options.UseNativeSkySunRules;
            if (levelOneUseNativeSkySun)
            {
                levelOneNativeSkySunRandomDraw = options.NativeSkySunRandomDraw;
                levelOneNativeSkySunState = OriginalSun.SkySunRules.Initialize(0,
                    DrawLevelOneNativeSkySun(OriginalSun.SkySunRules.InitialDrawBound));
            }
            SunAmount = LevelOneData.StartingSun;
            for (int row = 0; row < Mowers.Count; row++)
                if (row != LevelOneData.GrassRow) Mowers[row].Available = false;
            initialized = true;
        }

        partial void CanPlantExperimentalLevelOne(PlantKind kind, int row, int column, ref bool allowed)
        {
            allowed = kind == PlantKind.Peashooter && CanPlantLevelOneSeedAt(row, column);
        }

        partial void CanRemoveExperimentalLevelOnePlant(ref bool allowed)
        {
            allowed = levelOneAllowPlantRemoval;
        }

        partial void OnExperimentalLevelOnePlantPlaced()
        {
            levelOneSeedSelected = false;
            // This simulation removes plants immediately; Plants.Count is only
            // an approximation of the original data-array allocation count.
            if (levelOneTutorial.Stage == LevelOneTutorialStage.AwaitingPlantPlacement)
                ApplyLevelOneTutorialEffect(levelOneTutorial.OnPlantPlaced(Plants.Count));
        }

        partial void OnExperimentalLevelOneSunCollected()
        {
            UpdateLevelOneSeedUsability();
        }

        partial void BeforeExperimentalLevelOneTick()
        {
            if (levelOneTutorial.Stage == LevelOneTutorialStage.NotStarted)
                levelOneTutorial.OnFirstActiveGameplayUpdate();
            UpdateLevelOneSeedUsability();
        }

        partial void AdvanceExperimentalLevelOneNativeSkySun(ref bool handled)
        {
            if (!levelOneUseNativeSkySun) return;
            handled = true;
            levelOneNativeSkySunElapsed += SimulationStep;
            double tolerance = Math.Min(levelOneSecondsPerNativeTick * 1e-9, 1e-10);
            while (levelOneNativeSkySunElapsed + tolerance >= levelOneSecondsPerNativeTick && State == MatchState.Playing)
            {
                levelOneNativeSkySunElapsed -= levelOneSecondsPerNativeTick;
                if (levelOneNativeSkySunElapsed < 0.0) levelOneNativeSkySunElapsed = 0.0;
                TickLevelOneNativeSkySun();
                if (State != MatchState.Playing)
                {
                    // The outer Update subtracts one simulation step after Advance returns.
                    levelOneNativeSkySunElapsed = 0.0;
                    accumulatedTime = SimulationStep;
                    return;
                }
            }
        }

        private void TickLevelOneNativeSkySun()
        {
            // In the examined board update, existing pickups precede the sky scheduler.
            for (int i = levelOneNativeSkySuns.Count - 1; i >= 0; i--)
            {
                LevelOneNativeSkySun sun = levelOneNativeSkySuns[i];
                int boundedBalance = Math.Min(OriginalSun.SunPickupRules.BalanceCap, Math.Max(0, SunAmount));
                OriginalSun.SkySunFallTick tick = OriginalSun.SkySunFall.Tick(sun.FallState,
                    boundedBalance, true, true, false, 0);
                sun.FallState = tick.State;
                if (tick.State.Pickup.Dead) levelOneNativeSkySuns.RemoveAt(i);
                if (tick.Credited)
                {
                    SunAmount = tick.Balance;
                    OnExperimentalLevelOneSunCollected();
                    Raise(GameEvent.SunCollected);
                }
                if (State != MatchState.Playing) return;
            }

            // This host assumes its unmodeled end-state and mode predicates are clear.
            var context = new OriginalSun.SkySunContext(0, 0, true, false, 0, -1,
                (int)levelOneTutorial.Stage, Plants.Count, false);
            bool needsDraws = OriginalSun.SkySunRules.NeedsDraws(levelOneNativeSkySunState, context);
            int xDraw = needsDraws ? DrawLevelOneNativeSkySun(OriginalSun.SkySunRules.XDrawBound) : -1;
            int intervalDraw = needsDraws ? DrawLevelOneNativeSkySun(OriginalSun.SkySunRules.IntervalDrawBound) : -1;
            OriginalSun.SkySunTick next = OriginalSun.SkySunRules.Tick(levelOneNativeSkySunState, context, xDraw, intervalDraw);
            if (next.Spawned)
            {
                int landingDraw = DrawLevelOneNativeSkySun(OriginalSun.SkySunFall.LandingDrawBound);
                levelOneNativeSkySuns.Add(new LevelOneNativeSkySun(OriginalSun.SkySunFall.Initialize(next, landingDraw)));
            }
            levelOneNativeSkySunState = next.State;
        }

        private int DrawLevelOneNativeSkySun(int bound)
        {
            int draw = levelOneNativeSkySunRandomDraw == null ? random.Next(bound) : levelOneNativeSkySunRandomDraw(bound);
            if (draw < 0 || draw >= bound)
                throw new InvalidOperationException("Native sky-sun random draw is outside its requested bound.");
            return draw;
        }

        partial void CanDropExperimentalLevelOneSkySun(ref bool allowed)
        {
            LevelOneTutorialStage stage = levelOneTutorial.Stage;
            allowed = (stage != LevelOneTutorialStage.AwaitingSeedSelection &&
                       stage != LevelOneTutorialStage.AwaitingPlantPlacement) || Plants.Count > 0;
        }

        partial void SpawnExperimentalLevelOneSkySun(ref bool handled)
        {
            Sun sun = levelOneSkySunFactory == null
                ? new Sun { X = 0.4f + (float)random.NextDouble() * 8.2f, Y = -1f, TargetY = levelOneSkySunTargetY }
                : levelOneSkySunFactory(this, random);
            if (sun == null || !Finite(sun.X) || !Finite(sun.Y) || !Finite(sun.TargetY) || !Finite(sun.Age))
                throw new InvalidOperationException("The experimental sky-sun factory must return a sun with finite fields.");
            Suns.Add(sun);
            handled = true;
        }

        partial void AdvanceExperimentalLevelOneWaves()
        {
            if (levelOneTutorial.TutorialBlocksWaveCountdown || LevelOneWavesDispatched >= LevelOneData.WaveCount)
                return;
            levelOneWaveCountdown -= SimulationStep;
            if (levelOneWaveCountdown > 0.000000001) return;

            int wave = LevelOneWavesDispatched;
            int count = LevelOneData.GetZombieCount(wave);
            for (int index = 0; index < count; index++)
            {
                Zombies.Add(new Zombie {
                    Kind = (ZombieKind)LevelOneData.GetZombieId(wave, index),
                    Row = LevelOneData.ZombieSpawnRow,
                    X = levelOneZombieSpawnX + index * levelOneZombieSpacing,
                    Health = levelOneZombieHealth
                });
                SpawnedZombies++;
            }
            LevelOneWavesDispatched++;
            if (LevelOneWavesDispatched < LevelOneData.WaveCount)
            {
                double delay = levelOneDelayProvider == null ? levelOneBetweenWaveDelay : levelOneDelayProvider(this, wave);
                if (!Finite(delay) || delay < 0.0)
                    throw new InvalidOperationException("The experimental wave-delay callback must return a finite nonnegative value.");
                levelOneWaveCountdown = delay;
            }
        }

        partial void CheckExperimentalLevelOneCompletion()
        {
            if (LevelOneWavesDispatched < LevelOneData.WaveCount) return;
            bool complete = levelOneCompletionCondition == null ? Zombies.Count == 0 : levelOneCompletionCondition(this);
            if (!complete || State != MatchState.Playing) return;
            State = MatchState.Won;
            Raise(GameEvent.Won);
        }

        private void UpdateLevelOneSeedUsability()
        {
            if (levelOneTutorial.Stage == LevelOneTutorialStage.AwaitingUsableSeed &&
                seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && SunAmount >= LevelOneData.PeashooterCost)
                levelOneTutorial.OnSeedBecameUsable();
        }

        private void ApplyLevelOneTutorialEffect(LevelOneTutorialEffect effect)
        {
            if (effect.SkySunCountdownNativeTicks.HasValue)
            {
                if (levelOneUseNativeSkySun)
                    levelOneNativeSkySunState = new OriginalSun.SkySunState(effect.SkySunCountdownNativeTicks.Value,
                        levelOneNativeSkySunState.Drops);
                else
                    nextSkySun = (float)(effect.SkySunCountdownNativeTicks.Value * levelOneSecondsPerNativeTick);
            }
            if (effect.WaveCountdownAndInitialNativeTicks.HasValue)
                levelOneWaveCountdown = effect.WaveCountdownAndInitialNativeTicks.Value * levelOneSecondsPerNativeTick;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
