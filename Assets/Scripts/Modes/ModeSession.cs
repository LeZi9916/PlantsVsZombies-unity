using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica.Modes
{
    public enum ModePhase { ChoosingSeeds, Preparing, Playing, StageCleared, Won, Lost }
    public sealed class ModeFish
    {
        public float X, Y, Angle, Speed, PhaseRemaining;
        public int SwimPhase;
        public float Hunger=27f, SunTimer=10f;
    }
    public sealed class ModeRainPacket { public int PacketId; public float X,Y,TargetY,Age; }
    public sealed class ModeFood { public float X,Y,Age; }
    public sealed class ModePortal { public int Column, Row, Pair; }
    public sealed class ModeHeadShot { public float X, Row; public float Damage=20f; public bool Hypnotized; }
    public sealed class ModeSession
    {
        public ModeDefinition Definition { get; private set; }
        public GameSimulation Game { get; private set; }
        public Guid RunId { get; private set; }
        public ModePhase Phase { get; internal set; }
        public int Stage { get; internal set; }
        public int ClearedStages { get; internal set; }
        public int Score { get; internal set; }
        public int PacketSlots { get; private set; }
        public int UnlockedBasicSeeds { get; private set; }
        public int SelectedCell { get; internal set; }=-1;
        public string Advice { get; internal set; }
        public float SlotRollingRemaining { get; internal set; }
        public int[] SlotSymbols { get; private set; }=new int[3];
        public bool[] Brains { get; private set; }=new bool[5];
        internal readonly float[] BrainBite=new float[5];
        public readonly List<ModeFish> Fish=new List<ModeFish>();
        public readonly List<ModeRainPacket> RainPackets=new List<ModeRainPacket>();
        public readonly bool[] MatchUpgrades=new bool[3];
        internal readonly HashSet<int> GiantPackets=new HashSet<int>();
        internal readonly HashSet<AdventureBowlingNut> GiantNuts=new HashSet<AdventureBowlingNut>();
        public readonly List<ModeFood> Food=new List<ModeFood>();
        public readonly List<ModePortal> Portals=new List<ModePortal>();
        public readonly List<ModeHeadShot> HeadShots=new List<ModeHeadShot>();
        internal readonly Dictionary<int,int> VaseSeeds=new Dictionary<int,int>();
        internal readonly Dictionary<int,int> VaseSun=new Dictionary<int,int>();
        internal readonly HashSet<int> BackwardPackets=new HashSet<int>();
        internal readonly Dictionary<int,float> SunflowerHealth=new Dictionary<int,float>();
        internal readonly Dictionary<int,int> SunflowerDrops=new Dictionary<int,int>();
        internal float NextPacket, PortalTimer, MatchDelay, FailureDelay, WhackSpawnTimer;
        internal float ContinuousWaveTimer=-1f;
        internal bool WhackGravesRaised;
        public bool Finished { get { return Phase==ModePhase.Won||Phase==ModePhase.Lost; } }
        public bool HasNextStage { get { return Phase==ModePhase.StageCleared; } }
        public int FlagsCompleted { get { return ClearedStages*Definition.FlagsPerStage; } }
        public IReadOnlyList<ZombieKind> ZombieCards { get; internal set; }
        public ModeSession(int originalModeId,AdventureSeedAccess access=null,int packetSlots=6,int randomSeed=12345,bool roofCleaners=false,bool poolCleaners=false,int unlockedBasicSeeds=40)
        {
            Definition=ModeCatalog.Get(originalModeId);RunId=Guid.NewGuid();PacketSlots=Math.Max(6,Math.Min(10,packetSlots));Stage=1;UnlockedBasicSeeds=Math.Max(1,Math.Min(40,unlockedBasicSeeds));
            Game=new GameSimulation(new GameRules{AdventureLevel=Definition.BoardLevel,AdventureRoofCleaners=roofCleaners,AdventurePoolCleaners=poolCleaners,MowersEnabled=!Definition.Puzzle},randomSeed);
            Game.ConfigureMode(this,access);
        }
        public bool ChooseSeeds(IEnumerable<PlantKind> seeds) { return Game.ChooseModeSeeds(seeds); }
        public void Update(float dt) { Game.UpdateMode(dt); }
        public bool Continue() { return Game.ContinueModeStage(); }
        public bool BeginDefense() { return Game.BeginModeDefense(); }
        public bool Plant(PlantKind kind,int row,int column) { return Game.TryPlantMode(kind,row,column); }
        public bool UsePacket(int id,int row,int column) { return Game.TryUseModePacket(id,row,column); }
        public bool SmashVase(int row,int column) { return Game.SmashModeVase(row,column); }
        public bool PlaceZombie(ZombieKind kind,int row,int column) { return Game.PlaceModeZombie(kind,row,column); }
        public bool ClickMatchCell(int row,int column) { return Game.ClickModeMatchCell(row,column); }
        public bool Spin() { return Game.SpinModeSlots(); }
        public bool Feed(float x,float y) { return Game.FeedModeFish(x,y); }
        public bool BuyFish() { return Game.BuyModeFish(); }
        public bool BuyAquariumTrophy() { return Game.BuyModeAquariumTrophy(); }
    }
}
