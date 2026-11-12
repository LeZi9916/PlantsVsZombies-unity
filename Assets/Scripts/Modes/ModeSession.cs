#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode session.</summary>
    public sealed class ModeSession
    {
        /// <summary>Gets or sets definition.</summary>
        public ModeDefinition Definition { get; private set; }
        /// <summary>Gets or sets game.</summary>
        public GameSimulation Game { get; private set; }
        /// <summary>Gets or sets run id.</summary>
        public Guid RunId { get; private set; }
        /// <summary>Gets or sets phase.</summary>
        public ModePhase Phase { get; internal set; }
        /// <summary>Gets or sets stage.</summary>
        public int Stage { get; internal set; }
        /// <summary>Gets or sets cleared stages.</summary>
        public int ClearedStages { get; internal set; }
        /// <summary>Gets or sets score.</summary>
        public int Score { get; internal set; }
        /// <summary>Gets or sets packet slots.</summary>
        public int PacketSlots { get; private set; }
        /// <summary>Gets or sets unlocked basic seeds.</summary>
        public int UnlockedBasicSeeds { get; private set; }
        /// <summary>Gets or sets selected cell.</summary>
        public int SelectedCell { get; internal set; } = -1;
        /// <summary>Gets or sets advice.</summary>
        public string Advice { get; internal set; } = string.Empty;
        /// <summary>Gets or sets slot rolling remaining.</summary>
        public float SlotRollingRemaining { get; internal set; }
        /// <summary>Gets or sets slot symbols.</summary>
        public int[] SlotSymbols { get; private set; } = new int[3];
        /// <summary>Gets or sets brains.</summary>
        public bool[] Brains { get; private set; } = new bool[5];

        /// <summary>Stores brain bite.</summary>
        internal readonly float[] _brainBite = new float[5];
        /// <summary>Stores fish.</summary>
        public readonly List<ModeFish> Fish = new List<ModeFish>();
        /// <summary>Stores rain packets.</summary>
        public readonly List<ModeRainPacket> RainPackets = new List<ModeRainPacket>();
        /// <summary>Stores match upgrades.</summary>
        public readonly bool[] MatchUpgrades = new bool[3];
        /// <summary>Stores giant packets.</summary>
        internal readonly HashSet<int> _giantPackets = new HashSet<int>();
        /// <summary>Stores giant nuts.</summary>
        internal readonly HashSet<AdventureBowlingNut> _giantNuts = new HashSet<AdventureBowlingNut>();
        /// <summary>Stores food.</summary>
        public readonly List<ModeFood> Food = new List<ModeFood>();
        /// <summary>Stores portals.</summary>
        public readonly List<ModePortal> Portals = new List<ModePortal>();
        /// <summary>Stores head shots.</summary>
        public readonly List<ModeHeadShot> HeadShots = new List<ModeHeadShot>();
        /// <summary>Stores vase seeds.</summary>
        internal readonly Dictionary<int, int> _vaseSeeds = new Dictionary<int, int>();
        /// <summary>Stores vase sun.</summary>
        internal readonly Dictionary<int, int> _vaseSun = new Dictionary<int, int>();
        /// <summary>Stores backward packets.</summary>
        internal readonly HashSet<int> _backwardPackets = new HashSet<int>();
        /// <summary>Stores sunflower health.</summary>
        internal readonly Dictionary<int, float> _sunflowerHealth = new Dictionary<int, float>();
        /// <summary>Stores sunflower drops.</summary>
        internal readonly Dictionary<int, int> _sunflowerDrops = new Dictionary<int, int>();
        /// <summary>Stores next packet,  portal timer,  match delay,  failure delay,  whack spawn timer.</summary>
        internal float _nextPacket, _portalTimer, _matchDelay, _failureDelay, _whackSpawnTimer;
        /// <summary>Stores continuous wave timer.</summary>
        internal float _continuousWaveTimer = -1f;
        /// <summary>Stores whack graves raised.</summary>
        internal bool _whackGravesRaised;
        /// <summary>Gets or sets finished.</summary>
        public bool Finished
        {
            get
            {
                return Phase == ModePhase.Won || Phase == ModePhase.Lost;
            }
        }

        /// <summary>Gets or sets has next stage.</summary>
        public bool HasNextStage
        {
            get
            {
                return Phase == ModePhase.StageCleared;
            }
        }

        /// <summary>Gets or sets flags completed.</summary>
        public int FlagsCompleted
        {
            get
            {
                return ClearedStages * Definition.FlagsPerStage;
            }
        }

        /// <summary>Gets or sets zombie cards.</summary>
        public IReadOnlyList<ZombieKind> ZombieCards { get; internal set; } = System.Array.Empty<ZombieKind>();

        /// <summary>Initializes mode session.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <param name = "access">The access.</param>
        /// <param name = "packetSlots">The packet slots.</param>
        /// <param name = "randomSeed">The random seed.</param>
        /// <param name = "roofCleaners">The roof cleaners.</param>
        /// <param name = "poolCleaners">The pool cleaners.</param>
        /// <param name = "unlockedBasicSeeds">The unlocked basic seeds.</param>
        public ModeSession(int originalModeId, AdventureSeedAccess? access = null, int packetSlots = 6, int randomSeed = 12345, bool roofCleaners = false, bool poolCleaners = false, int unlockedBasicSeeds = 40)
        {
            Definition = ModeCatalog.Get(originalModeId);
            RunId = Guid.NewGuid();
            PacketSlots = Math.Max(6, Math.Min(10, packetSlots));
            Stage = 1;
            UnlockedBasicSeeds = Math.Max(1, Math.Min(40, unlockedBasicSeeds));
            Game = new GameSimulation(new GameRules {
                AdventureLevel = Definition.BoardLevel,
                AdventureRoofCleaners = roofCleaners,
                AdventurePoolCleaners = poolCleaners,
                MowersEnabled = !Definition.Puzzle
            }, randomSeed);
            Game.ConfigureMode(this, access);
        }

        /// <summary>Performs choose seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose seeds.</returns>
        public bool ChooseSeeds(IEnumerable<PlantKind> seeds)
        {
            return Game.ChooseModeSeeds(seeds);
        }

        /// <summary>Performs update.</summary>
        /// <param name = "dt">The dt.</param>
        public void Update(float dt)
        {
            Game.UpdateMode(dt);
        }

        /// <summary>Performs continue.</summary>
        /// <returns>The resulting continue.</returns>
        public bool Continue()
        {
            return Game.ContinueModeStage();
        }

        /// <summary>Performs begin defense.</summary>
        /// <returns>The resulting begin defense.</returns>
        public bool BeginDefense()
        {
            return Game.BeginModeDefense();
        }

        /// <summary>Performs plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant.</returns>
        public bool Plant(PlantKind kind, int row, int column)
        {
            return Game.TryPlantMode(kind, row, column);
        }

        /// <summary>Performs use packet.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting use packet.</returns>
        public bool UsePacket(int id, int row, int column)
        {
            return Game.TryUseModePacket(id, row, column);
        }

        /// <summary>Performs smash vase.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting smash vase.</returns>
        public bool SmashVase(int row, int column)
        {
            return Game.SmashModeVase(row, column);
        }

        /// <summary>Performs place zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place zombie.</returns>
        public bool PlaceZombie(ZombieKind kind, int row, int column)
        {
            return Game.PlaceModeZombie(kind, row, column);
        }

        /// <summary>Performs click match cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting click match cell.</returns>
        public bool ClickMatchCell(int row, int column)
        {
            return Game.ClickModeMatchCell(row, column);
        }

        /// <summary>Performs spin.</summary>
        /// <returns>The resulting spin.</returns>
        public bool Spin()
        {
            return Game.SpinModeSlots();
        }

        /// <summary>Performs feed.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting feed.</returns>
        public bool Feed(float x, float y)
        {
            return Game.FeedModeFish(x, y);
        }

        /// <summary>Performs buy fish.</summary>
        /// <returns>The resulting buy fish.</returns>
        public bool BuyFish()
        {
            return Game.BuyModeFish();
        }

        /// <summary>Performs buy aquarium trophy.</summary>
        /// <returns>The resulting buy aquarium trophy.</returns>
        public bool BuyAquariumTrophy()
        {
            return Game.BuyModeAquariumTrophy();
        }
    }
}
