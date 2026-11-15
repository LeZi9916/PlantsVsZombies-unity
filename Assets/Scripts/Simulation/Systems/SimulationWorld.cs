#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Levels;
using PlantsVsZombies.Modes;

using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SimulationWorld responsibilities for one deterministic match.</summary>
    internal sealed class SimulationWorld
    {
        /// <summary>Creates cached read-only views of pooled object collections.</summary>
        internal SimulationWorld()
        {
            PeasView = new EntityView<Pea>(Peas);
            SunsView = new EntityView<global::PlantsVsZombies.Simulation.Sun>(Suns);
            MowersView = new EntityView<Mower>(Mowers);
            FlamesView = new EntityView<LaneFlame>(Flames);
        }
        /// <summary>Gets the cached read-only peas view.</summary>
        internal EntityView<Pea> PeasView { get; }
        /// <summary>Gets the cached read-only suns view.</summary>
        internal EntityView<global::PlantsVsZombies.Simulation.Sun> SunsView { get; }
        /// <summary>Gets the cached read-only mowers view.</summary>
        internal EntityView<Mower> MowersView { get; }
        /// <summary>Gets the cached read-only flames view.</summary>
        internal EntityView<LaneFlame> FlamesView { get; }
        /// <summary>Reuses integer candidate lists independently across nested board actions.</summary>
        internal readonly ScratchBufferPool<int> IntegerScratch = new ScratchBufferPool<int>();
        /// <summary>Reuses coin targeting candidates without allocating one list per collection attempt.</summary>
        internal readonly ScratchBufferPool<AdventureCoin> CoinScratch = new ScratchBufferPool<AdventureCoin>();
        /// <summary>Stores rows.</summary>
        internal const int Rows = 5;
        /// <summary>Stores columns.</summary>
        internal const int Columns = 9;
        /// <summary>Stores plants.</summary>
        internal readonly EntityStore<Plant> Plants = new EntityStore<Plant>(entity => entity.Id, entity => entity.Row, (entity, callback) => entity.IndexChanged = callback);
        /// <summary>Stores zombies.</summary>
        internal readonly EntityStore<Zombie> Zombies = new EntityStore<Zombie>(entity => entity.Id, entity => entity.Row, (entity, callback) => entity.IndexChanged = callback);
        /// <summary>Stores peas.</summary>
        internal readonly List<Pea> Peas = new List<Pea>(64);
        /// <summary>Stores suns.</summary>
        internal readonly List<global::PlantsVsZombies.Simulation.Sun> Suns = new List<global::PlantsVsZombies.Simulation.Sun>(16);
        /// <summary>Stores mowers.</summary>
        internal readonly List<Mower> Mowers = new List<Mower>();
        /// <summary>Stores flames.</summary>
        internal readonly List<LaneFlame> Flames = new List<LaneFlame>(5);
        /// <summary>Retains reusable projectiles and their piercing-hit buffers for this match.</summary>
        internal readonly SimulationObjectPool<Pea> _peaPool = new SimulationObjectPool<Pea>(64, 256);
        /// <summary>Retains reusable normal and native-motion suns for this match.</summary>
        internal readonly SimulationObjectPool<global::PlantsVsZombies.Simulation.Sun> _sunPool = new SimulationObjectPool<global::PlantsVsZombies.Simulation.Sun>(16, 128);
        /// <summary>Retains short-lived lane flames for this match.</summary>
        internal readonly SimulationObjectPool<LaneFlame> _flamePool = new SimulationObjectPool<LaneFlame>(5, 16);
        /// <summary>Stores rules.</summary>
        internal GameRules Rules = null!;
        /// <summary>Gets or sets state.</summary>
        internal MatchState State { get; set; }
        /// <summary>Gets or sets sun amount.</summary>
        internal int SunAmount { get; set; }
        /// <summary>Gets or sets spawned zombies.</summary>
        internal int SpawnedZombies { get; set; }
        /// <summary>Gets or sets elapsed.</summary>
        internal float Elapsed { get; set; }
        /// <summary>The mode selected at construction; changing the rules later does not switch modes.</summary>
        internal bool IsExperimentalLevelOne { get; set; }
        /// <summary>Gets or sets is adventure.</summary>
        internal bool IsAdventure { get; set; }
        /// <summary>Gets or sets row count.</summary>
        internal int RowCount { get; set; } = Rows;

        /// <summary>Stores event raised.</summary>
        internal Action<GameEvent>? EventRaised;
        /// <summary>Reuses the synchronous native reward animation callback.</summary>
        internal Action _rewardPresentationTick = null!;
        /// <summary>Accumulates decorative board ticks after independent modes finish without an adventure reward object.</summary>
        internal double _completedBoardAccumulated;
        /// <summary>Reuses the immediately consumed packet-ready feedback context.</summary>
        internal readonly AdventureSeedBankContext _packetFeedbackContext = new AdventureSeedBankContext();
        /// <summary>Counts native updates continuously across opening, dialogue and combat for original Foley admission.</summary>
        internal int _nativeFeedbackTick;
        /// <summary>Resolves original audio randomness without a renderer-owned generator.</summary>
        internal readonly AdventureFoleyRandom _foleyRandom = new AdventureFoleyRandom();
        /// <summary>Stores random.</summary>
        internal Random _random = null!;
        /// <summary>Retains PC 413930's cell looks and jitter shared by fog and gravestones.</summary>
        internal readonly AdventureFogState _boardCells = new AdventureFogState();
        /// <summary>Stores seed cooldown ticks.</summary>
        internal readonly int[] _seedCooldownTicks = new int[VerifiedDataCatalog.Plants.Count];
        /// <summary>Retains an initial packet refresh threshold independently of the plant table's post-use threshold.</summary>
        internal readonly int[] _seedInitialCooldownThresholds = new int[VerifiedDataCatalog.Plants.Count];
        /// <summary>Stores next zombie.</summary>
        internal float _nextZombie;
        /// <summary>Stores next sky sun.</summary>
        internal float _nextSkySun;
        /// <summary>Stores accumulated time.</summary>
        internal double _accumulatedTime;
        /// <summary>Stores simulation step.</summary>
        internal const double SimulationStep = 0.01;

        /// <summary>Stores the shared original dance frame clock in native board updates.</summary>
        internal int _dancerClockTicks;

        /// <summary>Stores adventure cherry fuse sounds.</summary>
        internal readonly Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound> _adventureCherryFuseSounds = new Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound>();
        /// <summary>Stores adventure cherry detonation sounds.</summary>
        internal readonly Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound> _adventureCherryDetonationSounds = new Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound>();

        /// <summary>Stores native plant hit tick remainder.</summary>
        internal float _nativePlantHitTickRemainder;

        /// <summary>Gets or sets last adventure planting failure.</summary>
        internal AdventurePlantingFailure LastAdventurePlantingFailure { get; set; }

        /// <summary>Gets or sets presentation.</summary>
        internal global::PlantsVsZombies.Adventure.AdventurePresentationState Presentation { get; set; } = new global::PlantsVsZombies.Adventure.AdventurePresentationState();

        // 88 ascending updates to -704, then 84 descending updates until Z > -40.
        /// <summary>Stores adventure presentation cob flight seconds.</summary>
        internal const float AdventurePresentationCobFlightSeconds = 1.72f;

        /// <summary>Stores the adventure board, or null when this simulation does not run an adventure level.</summary>
        internal AdventureBoard? _adventure;

        /// <summary>Accumulates the native water updates while the board waits for seed selection or preparation.</summary>
        internal double _idlePoolRemainder;

        // PC Board::CanTakeSunMoney includes suns already flying to the bank (0x4275A0/4276D0).
        // Keep the existing nonnegative pickup-rule contract by representing native negative
        // mSunMoney as visible money plus a reservation settled by the arriving pickups.
        /// <summary>Stores adventure sun reservation.</summary>
        internal int _adventureSunReservation;

        // Original PC animation and projectile milestones, interpreted at the native 100 Hz.
        /// <summary>Stores adventure cob firing seconds.</summary>
        internal const float AdventureCobFiringSeconds = AdventureCobCannonRules.FiringSeconds;
        /// <summary>Stores adventure cob loading seconds.</summary>
        internal const float AdventureCobLoadingSeconds = AdventureCobCannonRules.LoadingSeconds;
        /// <summary>Stores adventure cob flight seconds.</summary>
        internal const float AdventureCobFlightSeconds = AdventurePresentationCobFlightSeconds;

        /// <summary>Stores normalized immutable catalog asset names for every supported zombie identifier.</summary>
        internal static readonly string[] s_zombiePresentationAssets = ZombieFeedbackSystem.CreateZombiePresentationAssets();

        /// <summary>Reuses the native column-major nine-by-six target weights without per-spawn allocation.</summary>
        internal readonly int[] _bungeeTargetWeights = new int[54];

        /// <summary>Stores level one tutorial.</summary>
        internal LevelOneTutorial? _levelOneTutorial;
        /// <summary>Stores level one seed selected.</summary>
        internal bool _levelOneSeedSelected;
        /// <summary>Stores level one allow plant removal.</summary>
        internal bool _levelOneAllowPlantRemoval;
        /// <summary>Stores level one seconds per native tick.</summary>
        internal double _levelOneSecondsPerNativeTick;
        /// <summary>Stores level one wave countdown.</summary>
        internal double _levelOneWaveCountdown;
        /// <summary>Stores level one between wave delay.</summary>
        internal double _levelOneBetweenWaveDelay;
        /// <summary>Stores level one zombie spawn x.</summary>
        internal float _levelOneZombieSpawnX;
        /// <summary>Stores level one zombie spacing.</summary>
        internal float _levelOneZombieSpacing;
        /// <summary>Stores level one zombie health.</summary>
        internal float _levelOneZombieHealth;
        /// <summary>Stores level one sky sun target y.</summary>
        internal float _levelOneSkySunTargetY;
        /// <summary>Stores level one delay provider.</summary>
        internal Func<SimulationView, int, double>? _levelOneDelayProvider;
        /// <summary>Stores level one completion condition.</summary>
        internal Func<SimulationView, bool>? _levelOneCompletionCondition;
        /// <summary>Stores level one sky sun factory.</summary>
        internal Func<SimulationView, Random, global::PlantsVsZombies.Simulation.Sun>? _levelOneSkySunFactory;
        /// <summary>Stores level one use native sky sun.</summary>
        internal bool _levelOneUseNativeSkySun;
        /// <summary>Stores level one native sky sun random draw.</summary>
        internal Func<int, int>? _levelOneNativeSkySunRandomDraw;
        /// <summary>Stores level one native sky sun state.</summary>
        internal global::PlantsVsZombies.Sun.SkySunState _levelOneNativeSkySunState;
        /// <summary>Stores level one native sky sun elapsed.</summary>
        internal double _levelOneNativeSkySunElapsed;
        /// <summary>Stores level one native sky suns.</summary>
        internal readonly List<LevelOneNativeSkySun> _levelOneNativeSkySuns = new List<LevelOneNativeSkySun>();
        /// <summary>Stores level one native sky suns view.</summary>
        internal IReadOnlyList<LevelOneNativeSkySun>? _levelOneNativeSkySunsView;

        /// <summary>Gets or sets level one waves dispatched.</summary>
        internal int LevelOneWavesDispatched { get; set; }

        /// <summary>Gets or sets mode session.</summary>
        internal ModeSession? ModeSession { get; set; }

        /// <summary>Stores mode accumulated.</summary>
        internal double _modeAccumulated;

        // PC 0x4338B0: count,type,zombie,seed. Seed 52 is the left-facing repeater, not an invented plant.
        /// <summary>Stores mode vase recipes.</summary>
        internal static readonly int[][] s_modeVaseRecipes =
        {
            new[]
            {
                5,
                1,
                -1,
                0,
                5,
                1,
                -1,
                5,
                5,
                1,
                -1,
                17,
                6,
                2,
                0,
                -1,
                3,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                52,
                3,
                1,
                -1,
                5,
                3,
                1,
                -1,
                3,
                2,
                1,
                -1,
                4,
                6,
                2,
                0,
                -1,
                3,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                4,
                1,
                -1,
                5,
                2,
                1,
                -1,
                17,
                3,
                1,
                -1,
                12,
                3,
                1,
                -1,
                3,
                8,
                2,
                0,
                -1,
                2,
                2,
                4,
                -1,
                1,
                2,
                8,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                11,
                1,
                -1,
                8,
                4,
                1,
                -1,
                12,
                4,
                1,
                -1,
                52,
                8,
                2,
                15,
                -1,
                7,
                2,
                0,
                -1,
                1,
                2,
                7,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                3,
                1,
                -1,
                30,
                4,
                1,
                -1,
                17,
                2,
                1,
                -1,
                12,
                2,
                1,
                -1,
                5,
                3,
                1,
                -1,
                31,
                6,
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                3,
                2,
                7,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                52,
                2,
                1,
                -1,
                17,
                5,
                1,
                -1,
                23,
                2,
                1,
                -1,
                18,
                4,
                1,
                -1,
                22,
                7,
                2,
                0,
                -1,
                5,
                2,
                3,
                -1,
                2,
                2,
                7,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                13,
                1,
                -1,
                21,
                3,
                1,
                -1,
                3,
                3,
                1,
                -1,
                17,
                10,
                2,
                0,
                -1,
                1,
                2,
                4,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                8,
                3,
                1,
                -1,
                23,
                5,
                1,
                -1,
                17,
                4,
                1,
                -1,
                52,
                8,
                2,
                15,
                -1,
                4,
                2,
                0,
                -1,
                4,
                2,
                18,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                2,
                1,
                -1,
                5,
                2,
                1,
                -1,
                0,
                2,
                1,
                -1,
                18,
                5,
                1,
                -1,
                17,
                1,
                1,
                -1,
                4,
                1,
                1,
                -1,
                3,
                1,
                1,
                -1,
                25,
                8,
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                1,
                2,
                23,
                -1
            }
        };

        /// <summary>Stores jalapeno damage.</summary>
        internal const float JalapenoDamage = 1800f;

        /// <summary>Gets or sets wave schedule.</summary>
        internal global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus? WaveSchedule { get; set; }

        /// <summary>Stores wave schedule event raised.</summary>
        internal Action<global::PlantsVsZombies.Waves.WaveScheduleEvent>? WaveScheduleEventRaised;
        /// <summary>Stores scheduled wave remainder.</summary>
        internal double _scheduledWaveRemainder;
        /// <summary>Stores scheduled final ambush enabled.</summary>
        internal bool _scheduledFinalAmbushEnabled = true;
        /// <summary>Stores scheduled honor tutorials.</summary>
        internal bool _scheduledHonorTutorials = true;
        /// <summary>Stores first tutorial plant observed.</summary>
        internal bool _firstTutorialPlantObserved;
    }
}
