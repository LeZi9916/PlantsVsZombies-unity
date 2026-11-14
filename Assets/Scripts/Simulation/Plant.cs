#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines plant.</summary>
    public sealed class Plant
    {
        /// <summary>Gets the original Blover's initial blow, continuing wind, and delayed special action.</summary>
        public AdventureBloverState? BloverPresentation { get; internal set; }
        /// <summary>Gets the original cactus's low, rising, high, lowering and body-shooting animation state.</summary>
        public AdventureCactusState? CactusPresentation { get; internal set; }
        /// <summary>Gets the original kelp grab while the plant and its target remain on the board.</summary>
        public AdventureTangleKelpState? TangleKelpPresentation { get; internal set; }
        /// <summary>Gets or sets the original Magnet-shroom's attraction, held metal and cooldown presentation.</summary>
        public AdventureMagnetState? MagnetPresentation { get; internal set; }
        /// <summary>Gets or sets the original Squash's target, moving body, and authored attack playback.</summary>
        public AdventureSquashState? SquashPresentation { get; internal set; }
        /// <summary>Gets or sets the original Scaredy-shroom fear and recovery body state.</summary>
        public AdventureScaredyShroomState? ScaredyShroomPresentation { get; internal set; }
        /// <summary>Gets the pre-explosion body and head state, created when a Doom-shroom first updates awake.</summary>
        public AdventureDoomShroomState? DoomShroomPresentation { get; internal set; }
        /// <summary>Gets or sets the original Jalapeno fuse animation and native per-update shake.</summary>
        public AdventureJalapenoState? JalapenoPresentation { get; internal set; }
        /// <summary>Gets or sets cherry bomb presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventureCherryBombState? CherryBombPresentation { get; internal set; }

        /// <summary>Gets the cherry bomb presentation, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Adventure.AdventureCherryBombState RequiredCherryBombPresentation => CherryBombPresentation ?? throw new System.InvalidOperationException("The member 'CherryBombPresentation' is not available.");

        /// <summary>Stores kernel weapon prepared.</summary>
        internal bool _kernelWeaponPrepared;
        /// <summary>Stores presentation eaten flash ticks.</summary>
        public int PresentationEatenFlashTicks;
        /// <summary>Stores accumulated defensive body animation frames and the current native frame rate.</summary>
        public float PresentationBodyFrames, PresentationBodyRate;
        /// <summary>Stores the last damage stage already presented for defensive plants.</summary>
        internal int _presentationDefensiveDamageStage;
        /// <summary>Records whether the defensive body clock is slowed by recent chewing.</summary>
        internal bool _presentationDefensiveChewing;
        /// <summary>Gets or sets the native chomper bite, digestion, and swallowing presentation.</summary>
        public AdventureChomperState? ChomperPresentation { get; internal set; }
        /// <summary>Gets or sets potato mine presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventurePotatoMineState? PotatoMinePresentation { get; internal set; }

        /// <summary>Gets the potato mine presentation, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Adventure.AdventurePotatoMineState RequiredPotatoMinePresentation => PotatoMinePresentation ?? throw new System.InvalidOperationException("The member 'PotatoMinePresentation' is not available.");

        /// <summary>Stores presentation rate, presentation awake age.</summary>
        public float PresentationRate, PresentationAwakeAge;
        /// <summary>Stores presentation head age.</summary>
        public readonly float[] PresentationHeadAge =
        {
            -1f,
            -1f,
            -1f
        };
        /// <summary>Stores presentation shot age.</summary>
        public float PresentationShotAge = -1f;
        /// <summary>Stores presentation butter.</summary>
        public bool PresentationButter;
        /// <summary>Stores presentation prefiring.</summary>
        internal bool _presentationPrefiring;
        /// <summary>Stores presentation kind.</summary>
        internal PlantKind _presentationKind = (PlantKind)(-1);
        // Kept in this independent file so production/rendering integration need not alter shared state files.
        /// <summary>Stores producer glow.</summary>
        public global::PlantsVsZombies.Adventure.AdventureProducerGlowState ProducerGlow;
        /// <summary>Stores layer.</summary>
        public global::PlantsVsZombies.Adventure.PlantLayer Layer;
        /// <summary>Stores asleep.</summary>
        public bool Asleep;
        /// <summary>Stores awake by coffee.</summary>
        public bool AwakeByCoffee;
        /// <summary>Stores is small.</summary>
        public bool IsSmall;
        /// <summary>Stores has ladder.</summary>
        public bool HasLadder;
        /// <summary>Stores action count.</summary>
        public int ActionCount;
        /// <summary>Stores special timer.</summary>
        public float SpecialTimer;
        /// <summary>Stores wake timer.</summary>
        public float WakeTimer;
        /// <summary>Stores id.</summary>
        public int Id;
        /// <summary>Stores imitater target.</summary>
        public PlantKind? ImitaterTarget;
        /// <summary>Stores is imitated.</summary>
        public bool IsImitated;
        /// <summary>Stores imitater transform remaining.</summary>
        public float ImitaterTransformRemaining;
        /// <summary>Stores upgrade burst ticks.</summary>
        public int UpgradeBurstTicks;
        /// <summary>Stores the original shared shooting counter and head states for multi-head shooters.</summary>
        public AdventureMultiHeadTimingState? MultiHeadTiming;

        /// <summary>Gets the multi head timing, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public AdventureMultiHeadTimingState RequiredMultiHeadTiming => MultiHeadTiming ?? throw new System.InvalidOperationException("The member 'MultiHeadTiming' is not available.");
        /// <summary>Stores the original Starfruit launch countdown; negative means it has not been initialized.</summary>
        public int StarfruitLaunchCountdown = -1;
        /// <summary>Stores the original Starfruit body shooting countdown.</summary>
        public int StarfruitShootingCountdown;
        /// <summary>Stores native ticks remaining since the plant was last chewed.</summary>
        public int RecentlyEatenTicks;
        /// <summary>Stores cannon phase.</summary>
        public CobCannonPhase CannonPhase;
        /// <summary>Stores cannon reload remaining.</summary>
        public float CannonReloadRemaining;
        /// <summary>Stores cannon target x.</summary>
        public float CannonTargetX;
        /// <summary>Stores cannon target row.</summary>
        public float CannonTargetRow;
        /// <summary>Stores vehicle hits taken.</summary>
        public int VehicleHitsTaken;
        /// <summary>Stores presentation bite native state.</summary>
        public int PresentationBiteNativeState = -1;
        /// <summary>Stores presentation bite not on ground.</summary>
        public bool PresentationBiteNotOnGround;
        /// <summary>Stores column span.</summary>
        public int ColumnSpan = 1;
        /// <summary>Stores kind.</summary>
        public PlantKind Kind;
        /// <summary>Stores row.</summary>
        public int Row;
        /// <summary>Stores column.</summary>
        public int Column;
        /// <summary>Stores health.</summary>
        public float Health;
        /// <summary>Stores action timer.</summary>
        public float ActionTimer;
        /// <summary>Stores age.</summary>
        public float Age;
        /// <summary>Stores shoot age.</summary>
        public float ShootAge = -1f;
        /// <summary>Stores mine phase.</summary>
        public MinePhase MinePhase;
        /// <summary>Stores phase age.</summary>
        public float PhaseAge;
        /// <summary>Stores whether a Grave Buster has already created its attached eating particles.</summary>
        public bool GraveBusterEatingStarted;
        /// <summary>Stores timing.</summary>
        public global::PlantsVsZombies.Timing.PlantTimingState? Timing;

        /// <summary>Gets the timing, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Timing.PlantTimingState RequiredTiming => Timing ?? throw new System.InvalidOperationException("The member 'Timing' is not available.");
        /// <summary>Stores mode faces left.</summary>
        public bool ModeFacesLeft;
        /// <summary>Stores mode fire timer.</summary>
        internal float _modeFireTimer;
    }
}
