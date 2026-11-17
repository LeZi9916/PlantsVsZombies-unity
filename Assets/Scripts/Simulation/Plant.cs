#nullable enable
using System;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines plant.</summary>
    public sealed class Plant : ISimulationPooledObject
    {
        /// <summary>Connects lazily rented capabilities without adding any checkpoint state.</summary>
        [NonSerialized]
        internal SimulationRuntimePool? RuntimePool;
        /// <summary>Retains behavior-owned state for the current kind.</summary>
        private IPlantBehavior? _behavior;
        /// <summary>Identifies the behavior's native plant kind.</summary>
        private PlantKind _behaviorKind = (PlantKind)(-1);
        /// <summary>Gets the capability owner, rebuilding it only when the kind changes.</summary>
        internal IPlantBehavior Behavior
        {
            get
            {
                if (_behavior == null || _behaviorKind != Kind)
                {
                    _behavior = PlantBehaviorFactory.Create(Kind, RuntimePool);
                    _behaviorKind = Kind;
                }
                return _behavior;
            }
        }

        /// <summary>Gets the original randomly initialized animation frame length.</summary>
        public int AnimationFrameLength { get; internal set; }
        /// <summary>Gets the original awake updates remaining before the next blink attempt.</summary>
        public int BlinkCountdown { get; internal set; }
        /// <summary>Gets the original active blink marker, or null between blinks.</summary>
        public string? BlinkAnimation { get; internal set; }
        /// <summary>Gets the native updates elapsed in the independent fifteen-FPS blink.</summary>
        public int BlinkAnimationTicks { get; internal set; }
        /// <summary>Gets the parent body marker retained when its first blink fixes the native attachment base pose.</summary>
        public string? BlinkBaseAnimation { get; internal set; }
        /// <summary>Gets the original zero-based head selected for an attached blink.</summary>
        public int BlinkHead { get; internal set; }

        /// <summary>Gets the original persistent horizontal planting displacement in board pixels.</summary>
        public int PositionOffsetX { get; internal set; }
        /// <summary>Gets the original persistent vertical planting displacement in board pixels.</summary>
        public int PositionOffsetY { get; internal set; }

        /// <summary>Invalidates the owning store after indexed state changes.</summary>
        internal Action? IndexChanged;

        /// <summary>Gets the original Blover's initial blow, continuing wind, and delayed special action.</summary>
        public AdventureBloverState? BloverPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as BloverPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is BloverPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets the original cactus's low, rising, high, lowering and body-shooting animation state.</summary>
        public AdventureCactusState? CactusPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as CactusPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is CactusPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets the original kelp grab while the plant and its target remain on the board.</summary>
        public AdventureTangleKelpState? TangleKelpPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as TangleKelpPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is TangleKelpPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets the original Magnet-shroom's attraction, held metal and cooldown presentation.</summary>
        public AdventureMagnetState? MagnetPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as MagnetShroomPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is MagnetShroomPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets the original Squash's target, moving body, and authored attack playback.</summary>
        public AdventureSquashState? SquashPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as SquashPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is SquashPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets the original Scaredy-shroom fear and recovery body state.</summary>
        public AdventureScaredyShroomState? ScaredyShroomPresentation
        {
            get => _behaviorKind == Kind && _behavior is ScaredyPlantBehavior behavior ? behavior.State : null;
            internal set
            {
                if (Behavior is ScaredyPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original flying coffee bean's special counter and crumble playback.</summary>
        public AdventureCoffeeBeanState? CoffeePresentation
        {
            get => _behaviorKind == Kind ? (_behavior as CoffeeBeanPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is CoffeeBeanPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Stores the body age at coffee wake, or minus one before wake or a later body action restarts playback.</summary>
        public float PresentationCoffeeWakeAge { get; internal set; } = -1f;
        /// <summary>Retains the original independent sleeping Z animation until this plant wakes or dies.</summary>
        public AdventureSleepingState? SleepingPresentation { get; internal set; }
        /// <summary>Gets the pre-explosion body and head state, created when a Doom-shroom first updates awake.</summary>
        public AdventureDoomShroomState? DoomShroomPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as DoomShroomPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is DoomShroomPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets the original Jalapeno fuse animation and native per-update shake.</summary>
        public AdventureJalapenoState? JalapenoPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as JalapenoPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is JalapenoPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets cherry bomb presentation.</summary>
        public AdventureCherryBombState? CherryBombPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as CherryBombPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is CherryBombPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }

        /// <summary>Gets the cherry bomb presentation, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Adventure.AdventureCherryBombState RequiredCherryBombPresentation => CherryBombPresentation ?? throw new System.InvalidOperationException("The member 'CherryBombPresentation' is not available.");

        /// <summary>Stores kernel weapon prepared.</summary>
        internal bool _kernelWeaponPrepared;
        /// <summary>Stores presentation eaten flash ticks.</summary>
        public int PresentationEatenFlashTicks { get; internal set; }
        /// <summary>Stores accumulated defensive body animation frames and the current native frame rate.</summary>
        public float PresentationBodyFrames { get; internal set; }
        /// <summary>Gets the PresentationBodyRate state maintained by simulation capabilities.</summary>
        public float PresentationBodyRate { get; internal set; }
        /// <summary>Stores the last damage stage already presented for defensive plants.</summary>
        internal int _presentationDefensiveDamageStage;
        /// <summary>Records whether the defensive body clock is slowed by recent chewing.</summary>
        internal bool _presentationDefensiveChewing;
        /// <summary>Gets or sets the native chomper bite, digestion, and swallowing presentation.</summary>
        public AdventureChomperState? ChomperPresentation
        {
            get => _behaviorKind == Kind ? (_behavior as ChomperPlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is ChomperPlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Gets or sets potato mine presentation.</summary>
        public AdventurePotatoMineState? PotatoMinePresentation
        {
            get => _behaviorKind == Kind ? (_behavior as PotatoMinePlantBehavior)?.State : null;
            internal set
            {
                if (Behavior is PotatoMinePlantBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }

        /// <summary>Gets the potato mine presentation, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Adventure.AdventurePotatoMineState RequiredPotatoMinePresentation => PotatoMinePresentation ?? throw new System.InvalidOperationException("The member 'PotatoMinePresentation' is not available.");

        /// <summary>Records PC PlantInit's stopped flower-pot idle until the last supported plant dies.</summary>
        public bool PresentationFlowerPotPaused { get; internal set; }
        /// <summary>Stores presentation rate, presentation awake age.</summary>
        public float PresentationRate { get; internal set; }
        /// <summary>Gets the PresentationAwakeAge state maintained by simulation capabilities.</summary>
        public float PresentationAwakeAge { get; internal set; }
        /// <summary>Stores presentation head age.</summary>
        internal readonly float[] PresentationHeadAge =
        {
            -1f,
            -1f,
            -1f
        };
        /// <summary>Stores the fresh twelve-FPS lobber idle clock after a completed shot, or minus one before that reset.</summary>
        public float PresentationLobberIdleAge { get; internal set; } = -1f;
        /// <summary>Stores presentation shot age.</summary>
        public float PresentationShotAge { get; internal set; } = -1f;
        /// <summary>Stores presentation butter.</summary>
        public bool PresentationButter { get; internal set; }
        /// <summary>Stores presentation prefiring.</summary>
        internal bool _presentationPrefiring;
        /// <summary>Stores presentation kind.</summary>
        internal PlantKind _presentationKind = (PlantKind)(-1);
        // Kept in this independent file so production/rendering integration need not alter shared state files.
        /// <summary>Stores producer glow.</summary>
        public global::PlantsVsZombies.Adventure.AdventureProducerGlowState ProducerGlow { get; internal set; }
        /// <summary>Stores the original Umbrella Leaf block animation age, or minus one while inactive.</summary>
        public float PresentationUmbrellaAge { get; internal set; } = -1f;
        /// <summary>Stores native PlantOnBungeeState: zero normal, one grabbed, two rising with its carrier.</summary>
        public int BungeeState { get; internal set; }
        /// <summary>Stores layer.</summary>
        public global::PlantsVsZombies.Adventure.PlantLayer Layer
        {
            get => _layer;
            internal set
            {
                if (_layer != value)
                {
                    _layer = value;
                    IndexChanged?.Invoke();
                }
            }
        }
        /// <summary>Stores the indexed layer value.</summary>
        private global::PlantsVsZombies.Adventure.PlantLayer _layer;
        /// <summary>Stores asleep.</summary>
        public bool Asleep { get; internal set; }
        /// <summary>Stores awake by coffee.</summary>
        public bool AwakeByCoffee { get; internal set; }
        /// <summary>Stores is small.</summary>
        public bool IsSmall { get; internal set; }
        /// <summary>Stores has ladder.</summary>
        public bool HasLadder { get; internal set; }
        /// <summary>Stores action count.</summary>
        public int ActionCount { get; internal set; }
        /// <summary>Stores special timer.</summary>
        public float SpecialTimer { get; internal set; }
        /// <summary>Stores wake timer.</summary>
        public float WakeTimer { get; internal set; }
        /// <summary>Stores id.</summary>
        public int Id
        {
            get => _id;
            internal set
            {
                if (_id != value)
                {
                    _id = value;
                    IndexChanged?.Invoke();
                }
            }
        }
        /// <summary>Stores the indexed id value.</summary>
        private int _id;
        /// <summary>Stores imitater target.</summary>
        public PlantKind? ImitaterTarget { get; internal set; }
        /// <summary>Stores is imitated.</summary>
        public bool IsImitated { get; internal set; }
        /// <summary>Stores imitater transform remaining.</summary>
        public float ImitaterTransformRemaining { get; internal set; }
        /// <summary>Stores upgrade burst ticks.</summary>
        public int UpgradeBurstTicks
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.BurstTicks : 0;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.BurstTicks = value;
                }
            }
        }
        /// <summary>Stores the original shared shooting counter and head states for multi-head shooters.</summary>
        public AdventureMultiHeadTimingState? MultiHeadTiming
        {
            get => _behaviorKind == Kind && _behavior is MultiHeadPlantBehavior behavior ? behavior.Timing : null;
            internal set
            {
                if (Behavior is MultiHeadPlantBehavior behavior)
                {
                    behavior.Timing = value;
                }
            }
        }

        /// <summary>Gets the multi head timing, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public AdventureMultiHeadTimingState RequiredMultiHeadTiming => MultiHeadTiming ?? throw new System.InvalidOperationException("The member 'MultiHeadTiming' is not available.");
        /// <summary>Stores the original Starfruit launch countdown; negative means it has not been initialized.</summary>
        public int StarfruitLaunchCountdown
        {
            get => _behaviorKind == Kind && _behavior is StarfruitPlantBehavior behavior ? behavior.LaunchCountdown : -1;
            internal set
            {
                if (Behavior is StarfruitPlantBehavior behavior)
                {
                    behavior.LaunchCountdown = value;
                }
            }
        }
        /// <summary>Stores the original Starfruit body shooting countdown.</summary>
        public int StarfruitShootingCountdown
        {
            get => _behaviorKind == Kind && _behavior is StarfruitPlantBehavior behavior ? behavior.ShootingCountdown : 0;
            internal set
            {
                if (Behavior is StarfruitPlantBehavior behavior)
                {
                    behavior.ShootingCountdown = value;
                }
            }
        }
        /// <summary>Stores native ticks remaining since the plant was last chewed.</summary>
        public int RecentlyEatenTicks { get; internal set; }
        /// <summary>Stores cannon phase.</summary>
        public CobCannonPhase CannonPhase
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.CannonPhase : default;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.CannonPhase = value;
                }
            }
        }
        /// <summary>Gets the completed shooting cycle whose native final pose begins the cannon's return blend.</summary>
        public int CannonResetVersion
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.CannonResetVersion : 0;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.CannonResetVersion = value;
                }
            }
        }
        /// <summary>Stores cannon reload remaining.</summary>
        public float CannonReloadRemaining
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.CannonReloadRemaining : 0f;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.CannonReloadRemaining = value;
                }
            }
        }
        /// <summary>Stores cannon target x.</summary>
        public float CannonTargetX
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.CannonTargetX : 0f;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.CannonTargetX = value;
                }
            }
        }
        /// <summary>Stores cannon target row.</summary>
        public float CannonTargetRow
        {
            get => _behaviorKind == Kind && _behavior is UpgradePlantBehavior behavior ? behavior.CannonTargetRow : 0f;
            internal set
            {
                if (Behavior is UpgradePlantBehavior behavior)
                {
                    behavior.CannonTargetRow = value;
                }
            }
        }
        /// <summary>Stores vehicle hits taken.</summary>
        public int VehicleHitsTaken { get; internal set; }
        /// <summary>Stores presentation bite native state.</summary>
        public int PresentationBiteNativeState { get; internal set; } = -1;
        /// <summary>Stores presentation bite not on ground.</summary>
        public bool PresentationBiteNotOnGround { get; internal set; }
        /// <summary>Stores column span.</summary>
        public int ColumnSpan
        {
            get => _columnSpan;
            internal set
            {
                if (_columnSpan != value)
                {
                    _columnSpan = value;
                    IndexChanged?.Invoke();
                }
            }
        }
        /// <summary>Stores the indexed columnspan value.</summary>
        private int _columnSpan = 1;
        /// <summary>Stores kind.</summary>
        public PlantKind Kind { get; internal set; }
        /// <summary>Stores row.</summary>
        public int Row
        {
            get => _row;
            internal set
            {
                if (_row != value)
                {
                    _row = value;
                    IndexChanged?.Invoke();
                }
            }
        }
        /// <summary>Stores the indexed row value.</summary>
        private int _row;
        /// <summary>Stores column.</summary>
        public int Column
        {
            get => _column;
            internal set
            {
                if (_column != value)
                {
                    _column = value;
                    IndexChanged?.Invoke();
                }
            }
        }
        /// <summary>Stores the indexed column value.</summary>
        private int _column;
        /// <summary>Stores health.</summary>
        public float Health { get; internal set; }
        /// <summary>Stores action timer.</summary>
        public float ActionTimer { get; internal set; }
        /// <summary>Stores age.</summary>
        public float Age { get; internal set; }
        /// <summary>Stores shoot age.</summary>
        public float ShootAge { get; internal set; } = -1f;
        /// <summary>Stores mine phase.</summary>
        public MinePhase MinePhase
        {
            get => _behaviorKind == Kind && _behavior is PotatoMinePlantBehavior behavior ? behavior.MinePhase : default;
            internal set
            {
                if (Behavior is PotatoMinePlantBehavior behavior)
                {
                    behavior.MinePhase = value;
                }
            }
        }
        /// <summary>Stores phase age.</summary>
        public float PhaseAge { get; internal set; }
        /// <summary>Stores whether a Grave Buster has already created its attached eating particles.</summary>
        public bool GraveBusterEatingStarted { get; internal set; }
        /// <summary>Stores timing.</summary>
        public global::PlantsVsZombies.Timing.PlantTimingState? Timing { get; internal set; }

        /// <summary>Gets the timing, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Timing.PlantTimingState RequiredTiming => Timing ?? throw new System.InvalidOperationException("The member 'Timing' is not available.");
        /// <summary>Stores mode faces left.</summary>
        public bool ModeFacesLeft { get; internal set; }
        /// <summary>Stores mode fire timer.</summary>
        internal float _modeFireTimer;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void ISimulationPooledObject.ResetForPool()
        {
            RuntimePool = null;
            for (var head = 0; head < PresentationHeadAge.Length; head++)
            {
                PresentationHeadAge[head] = -1f;
            }
            _behavior = default;
            _behaviorKind = (PlantKind)(-1);
            AnimationFrameLength = default;
            BlinkCountdown = default;
            BlinkAnimation = default;
            BlinkAnimationTicks = default;
            BlinkBaseAnimation = default;
            BlinkHead = default;
            PositionOffsetX = default;
            PositionOffsetY = default;
            IndexChanged = default;
            PresentationCoffeeWakeAge = -1f;
            SleepingPresentation = default;
            _kernelWeaponPrepared = default;
            PresentationEatenFlashTicks = default;
            PresentationBodyFrames = default;
            PresentationBodyRate = default;
            _presentationDefensiveDamageStage = default;
            _presentationDefensiveChewing = default;
            PresentationFlowerPotPaused = default;
            PresentationRate = default;
            PresentationAwakeAge = default;
            PresentationLobberIdleAge = -1f;
            PresentationShotAge = -1f;
            PresentationButter = default;
            _presentationPrefiring = default;
            _presentationKind = (PlantKind)(-1);
            ProducerGlow = default;
            PresentationUmbrellaAge = -1f;
            BungeeState = default;
            _layer = default;
            Asleep = default;
            AwakeByCoffee = default;
            IsSmall = default;
            HasLadder = default;
            ActionCount = default;
            SpecialTimer = default;
            WakeTimer = default;
            _id = default;
            ImitaterTarget = default;
            IsImitated = default;
            ImitaterTransformRemaining = default;
            RecentlyEatenTicks = default;
            VehicleHitsTaken = default;
            PresentationBiteNativeState = -1;
            PresentationBiteNotOnGround = default;
            _columnSpan = 1;
            Kind = default;
            _row = default;
            _column = default;
            Health = default;
            ActionTimer = default;
            Age = default;
            ShootAge = -1f;
            PhaseAge = default;
            GraveBusterEatingStarted = default;
            Timing = default;
            ModeFacesLeft = default;
            _modeFireTimer = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void ISimulationPooledObject.MarkPoolReferences(SimulationRuntimePool pool)
        {
            pool.Mark(_behavior);
            pool.Mark(SleepingPresentation);
        }

    }
}
