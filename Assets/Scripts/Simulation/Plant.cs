#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines plant.</summary>
    public sealed class Plant
    {
        /// <summary>Gets or sets cherry bomb presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventureCherryBombState CherryBombPresentation { get; internal set; }

        /// <summary>Stores kernel weapon prepared.</summary>
        internal bool _kernelWeaponPrepared;
        /// <summary>Stores presentation eaten flash ticks.</summary>
        public int PresentationEatenFlashTicks;
        /// <summary>Gets or sets potato mine presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventurePotatoMineState PotatoMinePresentation { get; internal set; }

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
        /// <summary>Stores timing.</summary>
        public global::PlantsVsZombies.Timing.PlantTimingState? Timing;
        /// <summary>Stores mode faces left.</summary>
        public bool ModeFacesLeft;
        /// <summary>Stores mode fire timer.</summary>
        internal float _modeFireTimer;
    }
}
