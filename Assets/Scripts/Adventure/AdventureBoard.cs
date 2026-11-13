#nullable enable

using PlantsVsZombies.Simulation;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure board.</summary>
    public sealed class AdventureBoard
    {
        /// <summary>Creates the board state for one level.</summary>
        /// <param name="level">The level whose rules and layout this board follows.</param>
        internal AdventureBoard(AdventureLevelSpec level)
        {
            Level = level;
            _iceView = _ice.AsReadOnly();
            _packetsView = _packets.AsReadOnly();
            _vasesView = _vases.AsReadOnly();
            _gravesView = _graves.AsReadOnly();
            _cratersView = _craters.AsReadOnly();
            _ballsView = _balls.AsReadOnly();
            _nutsView = _nuts.AsReadOnly();
            _coinsView = _coins.AsReadOnly();
            _cobShellsView = _cobShells.AsReadOnly();
        }

        /// <summary>Gets the independent PC PoolEffect counter advanced before board objects update.</summary>
        public int PoolEffectTicks { get; internal set; }
        /// <summary>Gets the native board ice-trap countdown shared by water animation and final pool ambushes.</summary>
        public int IceTrapTicks { get; internal set; }

        /// <summary>Gets or sets conveyor ticks.</summary>
        public int ConveyorTicks { get; internal set; }
        /// <summary>Gets or sets total waves spawned.</summary>
        public int TotalWavesSpawned { get; internal set; }

        /// <summary>Stores dropped first coin,  dropped mode gift.</summary>
        internal bool _droppedFirstCoin, _droppedModeGift;
        /// <summary>Gets or sets rake row.</summary>
        public int RakeRow { get; internal set; } = -1;
        /// <summary>Gets or sets rake column.</summary>
        public int RakeColumn { get; internal set; } = 7;
        /// <summary>Gets or sets rake attracting.</summary>
        public bool RakeAttracting { get; internal set; }
        /// <summary>Gets or sets rake triggered.</summary>
        public bool RakeTriggered { get; internal set; }
        /// <summary>Gets or sets rake age.</summary>
        public float RakeAge { get; internal set; }

        /// <summary>Stores rake damage done.</summary>
        internal bool _rakeDamageDone;
        /// <summary>Gets or sets reward presentation.</summary>
        public AdventureRewardPresentation? RewardPresentation { get; internal set; }

        /// <summary>Gets the reward presentation, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public AdventureRewardPresentation RequiredRewardPresentation => RewardPresentation ?? throw new System.InvalidOperationException("The member 'RewardPresentation' is not available.");

        /// <summary>Stores last reward x,  last reward y.</summary>
        internal float _lastRewardX = 400f, _lastRewardY = 300f;
        /// <summary>Stores reward lightfill played.</summary>
        internal bool _rewardLightfillPlayed;
        /// <summary>Gets or sets required seeds.</summary>
        public IReadOnlyList<PlantKind> RequiredSeeds { get; internal set; } = Array.AsReadOnly(new PlantKind[0]);

        /// <summary>Stores seed flashes.</summary>
        public readonly List<AdventureSeedFlashState> SeedFlashes = new List<AdventureSeedFlashState>();
        /// <summary>Stores seed flash id.</summary>
        internal int _seedFlashId;
        /// <summary>Gets or sets level.</summary>
        public AdventureLevelSpec Level { get; internal set; }
        /// <summary>Gets or sets phase.</summary>
        public AdventurePhase Phase { get; internal set; }
        /// <summary>Gets or sets tutorial.</summary>
        public AdventureTutorial Tutorial { get; internal set; }
        /// <summary>Gets or sets selected seeds.</summary>
        public IReadOnlyList<PlantKind> SelectedSeeds { get; internal set; } = System.Array.Empty<PlantKind>();
        /// <summary>Gets or sets waves.</summary>
        public IReadOnlyList<AdventureWave> Waves { get; internal set; } = System.Array.Empty<AdventureWave>();

        /// <summary>Stores packets.</summary>
        internal readonly List<AdventurePacket> _packets = new List<AdventurePacket>();
        /// <summary>Retains the live read-only view of packets for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventurePacket> _packetsView;
        /// <summary>Stores vases.</summary>
        internal readonly List<AdventureVase> _vases = new List<AdventureVase>();
        /// <summary>Retains the live read-only view of vases for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureVase> _vasesView;
        /// <summary>Stores graves.</summary>
        internal readonly List<AdventureCellTimer> _graves = new List<AdventureCellTimer>();
        /// <summary>Retains the live read-only view of graves for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureCellTimer> _gravesView;
        /// <summary>Gets whether the original introduction has enabled gravestone emergence.</summary>
        public bool GravesEnabled { get; internal set; } = true;
        /// <summary>Stores craters.</summary>
        internal readonly List<AdventureCellTimer> _craters = new List<AdventureCellTimer>();
        /// <summary>Retains the live read-only view of craters for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureCellTimer> _cratersView;
        /// <summary>Stores ice.</summary>
        internal readonly List<AdventureIceTrail> _ice = new List<AdventureIceTrail>();
        /// <summary>Retains one read-only ice view for every row's repeated presentation queries.</summary>
        private readonly IReadOnlyList<AdventureIceTrail> _iceView;
        /// <summary>Stores balls.</summary>
        internal readonly List<AdventureBossBall> _balls = new List<AdventureBossBall>();
        /// <summary>Retains the live read-only view of balls for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureBossBall> _ballsView;
        /// <summary>Stores nuts.</summary>
        internal readonly List<AdventureBowlingNut> _nuts = new List<AdventureBowlingNut>();
        /// <summary>Retains the live read-only view of nuts for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureBowlingNut> _nutsView;
        /// <summary>Gets or sets packets.</summary>
        public IReadOnlyList<AdventurePacket> Packets
        {
            get
            {
                return _packetsView;
            }
        }

        /// <summary>Gets or sets vases.</summary>
        public IReadOnlyList<AdventureVase> Vases
        {
            get
            {
                return _vasesView;
            }
        }

        /// <summary>Gets or sets graves.</summary>
        public IReadOnlyList<AdventureCellTimer> Graves
        {
            get
            {
                return _gravesView;
            }
        }

        /// <summary>Gets or sets craters.</summary>
        public IReadOnlyList<AdventureCellTimer> Craters
        {
            get
            {
                return _cratersView;
            }
        }

        /// <summary>Gets or sets ice trails.</summary>
        public IReadOnlyList<AdventureIceTrail> IceTrails
        {
            get
            {
                return _iceView;
            }
        }

        /// <summary>Gets or sets boss balls.</summary>
        public IReadOnlyList<AdventureBossBall> BossBalls
        {
            get
            {
                return _ballsView;
            }
        }

        /// <summary>Gets or sets bowling nuts.</summary>
        public IReadOnlyList<AdventureBowlingNut> BowlingNuts
        {
            get
            {
                return _nutsView;
            }
        }

        /// <summary>Gets or sets current wave.</summary>
        public int CurrentWave { get; internal set; }
        /// <summary>Gets or sets flags spawned.</summary>
        public int FlagsSpawned { get; internal set; }
        /// <summary>Gets or sets wave countdown.</summary>
        public float WaveCountdown { get; internal set; }
        /// <summary>Gets or sets vase stage.</summary>
        public int VaseStage { get; internal set; }
        /// <summary>Gets whether the adventure vase stage transition is in progress.</summary>
        public bool VaseInterludeActive { get; internal set; }
        /// <summary>Gets or sets whether a presentation host handles the vase dialogue.</summary>
        internal bool VaseInterludeDialogueEnabled { get; set; }
        /// <summary>Stores native ticks remaining before the vase transition dialogue.</summary>
        internal int _vaseInterludeTicks;
        /// <summary>Stores whether the next vase stage has been populated during the dialogue.</summary>
        internal bool _vaseInterludePopulated;
        /// <summary>Gets or sets fog blown remaining.</summary>
        public float FogBlownRemaining { get; internal set; }

        /// <summary>Retains the original overlapping fog tiles and their rolling displacement.</summary>
        internal AdventureFogState Fog { get; } = new AdventureFogState();
        /// <summary>Gets or sets storm flash.</summary>
        public bool StormFlash { get; internal set; }
        /// <summary>Gets or sets boss health.</summary>
        public float BossHealth { get; internal set; }

        /// <summary>Gets or sets boss max health.</summary>
        public float BossMaxHealth
        {
            get
            {
                return 40000f;
            }
        }

        /// <summary>Gets or sets boss head vulnerable.</summary>
        public bool BossHeadVulnerable { get; internal set; }

        /// <summary>Gets or sets reward available.</summary>
        public bool RewardAvailable
        {
            get
            {
                return Phase == AdventurePhase.Reward;
            }
        }

        /// <summary>Gets or sets reward collected.</summary>
        public bool RewardCollected
        {
            get
            {
                return Phase == AdventurePhase.Finished;
            }
        }

        /// <summary>Gets or sets selected seed.</summary>
        public PlantKind? SelectedSeed { get; internal set; }

        /// <summary>Stores notice raised.</summary>
        public Action<AdventureNotice>? NoticeRaised;
        /// <summary>Stores packet id.</summary>
        internal int _packetId;
        /// <summary>Stores zombie id.</summary>
        internal int _zombieId;
        /// <summary>Stores ticks.</summary>
        internal int _ticks;
        /// <summary>Stores next packet.</summary>
        internal float _nextPacket;
        /// <summary>Stores next sky sun.</summary>
        internal float _nextSkySun;
        /// <summary>Stores sky sun count.</summary>
        internal int _skySunCount;
        /// <summary>Stores warned.</summary>
        internal bool _warned;
        /// <summary>The exact enabled-type mask retained by the current native wave generator.</summary>
        internal bool[] EnabledZombieTypes { get; } = new bool[34];
        /// <summary>The original six-lane selection history and mower suppression markers.</summary>
        internal AdventureRowPicker RowPicker { get; } = new AdventureRowPicker();
        /// <summary>Stores last conveyor seed.</summary>
        internal PlantKind? _lastConveyorSeed;
        /// <summary>Stores final ambush.</summary>
        internal bool _finalAmbush;
        /// <summary>Stores previous wave health.</summary>
        internal float _previousWaveHealth;
        /// <summary>Stores wave initial countdown.</summary>
        internal float _waveInitialCountdown;
        /// <summary>Stores wave trigger health.</summary>
        internal float _waveTriggerHealth;
        /// <summary>Stores planted sunflowers.</summary>
        internal int _plantedSunflowers;
        /// <summary>Stores collected sun.</summary>
        internal bool _collectedSun;
        /// <summary>Stores boss timer.</summary>
        internal float _bossTimer;
        /// <summary>Stores boss cycle.</summary>
        internal int _bossCycle;
        /// <summary>Stores next hammer sun.</summary>
        internal float _nextHammerSun;
        /// <summary>Stores the Whack-a-Zombie seed-bank vertical position.</summary>
        internal float _whackSeedBankY = -87f;
        /// <summary>Stores whack-a-zombie spawn timer.</summary>
        internal float _whackSpawnTimer;
        /// <summary>Stores whether whack-a-zombie graves were raised for the current wave.</summary>
        internal bool _whackGravesRaised;
        /// <summary>Stores removal tutorial done.</summary>
        internal bool _removalTutorialDone;
        /// <summary>Performs notice.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        internal void Notice(AdventureNoticeKind kind, int value = 0, int row = -1, int column = -1)
        {
            if (NoticeRaised != null)
            {
                NoticeRaised(new AdventureNotice {
                    Kind = kind,
                    Value = value,
                    Row = row,
                    Column = column
                });
            }
        }

        /// <summary>Gets or sets available seeds.</summary>
        public IReadOnlyList<PlantKind> AvailableSeeds { get; internal set; } = System.Array.Empty<PlantKind>();
        /// <summary>Gets or sets imitater target.</summary>
        public PlantKind? ImitaterTarget { get; internal set; }

        /// <summary>Gets the imitater target, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public PlantKind RequiredImitaterTarget => ImitaterTarget ?? throw new System.InvalidOperationException("The member 'ImitaterTarget' is not available.");
        /// <summary>Gets or sets selected cob cannon id.</summary>
        public int? SelectedCobCannonId { get; internal set; }
        /// <summary>Gets or sets money collected units.</summary>
        public int MoneyCollectedUnits { get; internal set; }

        /// <summary>Gets or sets money display value.</summary>
        public long MoneyDisplayValue
        {
            get
            {
                return (long)MoneyCollectedUnits * 10;
            }
        }

        /// <summary>Stores plant id.</summary>
        internal int _plantId;
        /// <summary>Stores coin id.</summary>
        internal int _coinId;
        /// <summary>Stores coins.</summary>
        internal readonly List<AdventureCoin> _coins = new List<AdventureCoin>();
        /// <summary>Retains the live read-only view of coins for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureCoin> _coinsView;
        /// <summary>Stores cob shells.</summary>
        internal readonly List<AdventureCobShell> _cobShells = new List<AdventureCobShell>();
        /// <summary>Retains the live read-only view of cobShells for allocation-free repeated access.</summary>
        private readonly IReadOnlyList<AdventureCobShell> _cobShellsView;
        /// <summary>Gets or sets coins.</summary>
        public IReadOnlyList<AdventureCoin> Coins
        {
            get
            {
                return _coinsView;
            }
        }

        /// <summary>Gets or sets cob shells.</summary>
        public IReadOnlyList<AdventureCobShell> CobShells
        {
            get
            {
                return _cobShellsView;
            }
        }
    }
}
