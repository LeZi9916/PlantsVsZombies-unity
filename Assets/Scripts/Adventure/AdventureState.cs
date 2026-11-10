using System;
using System.Collections.Generic;
namespace PvZReplica.Adventure
{
    public enum PlantLayer { Support, Main, Armor, Effect }
    public enum AdventureNoticeKind { Tutorial, WaveWarning, WaveSpawned, GraveRise, PoolAmbush, RoofDrop, ConveyorPacket, VaseStage, BossAttack, RewardDropped, RewardCollected, UpgradePlanted, ImitaterMorphed, CobAimStarted, CobFired, CobImpact, CobReady, CoinCreated, CoinCollected, RewardFade }
    public sealed class AdventureNotice
    {
        public AdventureNoticeKind Kind { get; internal set; }
        public int Value { get; internal set; }
        public int Row { get; internal set; }
        public int Column { get; internal set; }
    }
    public sealed partial class AdventurePacket
    {
        public int Id { get; internal set; }
        public PlantKind Kind { get; internal set; }
        public float Age { get; internal set; }
        public int? FromVaseColumn { get; internal set; }
        public int? FromVaseRow { get; internal set; }
    }
    public sealed class AdventureVase
    {
        public int Row { get; internal set; }
        public int Column { get; internal set; }
        public bool ShowsPlant { get; internal set; }
        // Contents are only exposed to the view after smashing. Tests can inspect via internal assembly access.
        internal PlantKind? Seed;
        internal ZombieKind? Zombie;
    }
    public sealed class AdventureCellTimer
    {
        public int Row { get; internal set; }
        public int Column { get; internal set; }
        public float Remaining { get; internal set; }
    }
    public sealed class AdventureBossBall
    {
        public int Row { get; internal set; }
        public float X { get; internal set; }
        public bool IsIce { get; internal set; }
    }
    public sealed partial class AdventureBowlingNut
    {
        public int Row { get; internal set; }
        public float X { get; internal set; }
        public bool Explosive { get; internal set; }
        internal int LastHitId;
        internal float BounceDelay;
    }
    public sealed class AdventureWave
    {
        public int Index { get; internal set; }
        public bool IsFlag { get; internal set; }
        public IReadOnlyList<ZombieKind> Zombies { get; internal set; }
    }
    public sealed partial class AdventureBoard
    {
        public AdventureLevelSpec Level { get; internal set; }
        public AdventurePhase Phase { get; internal set; }
        public AdventureTutorial Tutorial { get; internal set; }
        public IReadOnlyList<PlantKind> SelectedSeeds { get; internal set; }
        public IReadOnlyList<AdventureWave> Waves { get; internal set; }
        internal readonly List<AdventurePacket> packets = new List<AdventurePacket>();
        internal readonly List<AdventureVase> vases = new List<AdventureVase>();
        internal readonly List<AdventureCellTimer> graves = new List<AdventureCellTimer>();
        internal readonly List<AdventureCellTimer> craters = new List<AdventureCellTimer>();
        internal readonly List<AdventureCellTimer> ice = new List<AdventureCellTimer>();
        internal readonly List<AdventureBossBall> balls = new List<AdventureBossBall>();
        internal readonly List<AdventureBowlingNut> nuts = new List<AdventureBowlingNut>();
        public IReadOnlyList<AdventurePacket> Packets { get { return packets.AsReadOnly(); } }
        public IReadOnlyList<AdventureVase> Vases { get { return vases.AsReadOnly(); } }
        public IReadOnlyList<AdventureCellTimer> Graves { get { return graves.AsReadOnly(); } }
        public IReadOnlyList<AdventureCellTimer> Craters { get { return craters.AsReadOnly(); } }
        public IReadOnlyList<AdventureCellTimer> IceTrails { get { return ice.AsReadOnly(); } }
        public IReadOnlyList<AdventureBossBall> BossBalls { get { return balls.AsReadOnly(); } }
        public IReadOnlyList<AdventureBowlingNut> BowlingNuts { get { return nuts.AsReadOnly(); } }
        public int CurrentWave { get; internal set; }
        public int FlagsSpawned { get; internal set; }
        public float WaveCountdown { get; internal set; }
        public int VaseStage { get; internal set; }
        public float FogBlownRemaining { get; internal set; }
        public bool StormFlash { get; internal set; }
        public float BossHealth { get; internal set; }
        public float BossMaxHealth { get { return 40000f; } }
        public bool BossHeadVulnerable { get; internal set; }
        public bool RewardAvailable { get { return Phase == AdventurePhase.Reward; } }
        public bool RewardCollected { get { return Phase == AdventurePhase.Finished; } }
        public PlantKind? SelectedSeed { get; internal set; }
        public Action<AdventureNotice> NoticeRaised;
        internal int PacketId;
        internal int ZombieId;
        internal int Ticks;
        internal float NextPacket;
        internal float NextSkySun;
        internal int SkySunCount;
        internal bool Warned;
        internal int LastSpawnRow = -1;
        internal PlantKind? LastConveyorSeed;
        internal bool FinalAmbush;
        internal float PreviousWaveHealth;
        internal float WaveInitialCountdown;
        internal float WaveTriggerHealth;
        internal int PlantedSunflowers;
        internal bool CollectedSun;
        internal float BossTimer;
        internal int BossCycle;
        internal float NextHammerSun;
        internal bool RemovalTutorialDone;
        internal void Notice(AdventureNoticeKind kind, int value = 0, int row = -1, int column = -1)
        { if (NoticeRaised != null) NoticeRaised(new AdventureNotice { Kind = kind, Value = value, Row = row, Column = column }); }
    }
}
namespace PvZReplica
{
    public sealed partial class Plant
    {
        public Adventure.PlantLayer Layer;
        public bool Asleep;
        public bool AwakeByCoffee;
        public bool IsSmall;
        public bool HasLadder;
        public int ActionCount;
        public float SpecialTimer;
        public float WakeTimer;
    }
    public sealed partial class Zombie
    {
        public int Id;
        public int Wave;
        public float Speed;
        public float FrozenRemaining;
        public float AbilityTimer;
        public bool Airborne;
        public bool Submerged;
        public bool Hypnotized;
        public bool UsedAbility;
        public bool ThrownImp;
        public int AbilityPhase;
        public bool IsSmall;
        public bool Headless;
        public bool BungeeCarrier;
        public float DropRemaining;
        public int LinkedZombieId;
        public int TargetColumn = -1;
        public float StartingHealth;
    }
    public sealed partial class Pea
    {
        public float Damage = 20f;
        public float VelocityX = 4.5f;
        public float VelocityY;
        public float FlightRow;
        public float StartX;
        public float Range = 20f;
        public bool Lobbed;
        public bool PiercesShield;
        public bool PiercesZombies;
        public bool Splash;
        public bool Butter;
        public bool Fire;
        public bool HitsAir;
        public bool Backward;
        public int LastTorchColumn = -1;
        public readonly HashSet<int> HitIds = new HashSet<int>();
    }
}
