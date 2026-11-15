#nullable enable
using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines zombie.</summary>
    public sealed class Zombie
    {
        /// <summary>Creates an unattached ordinary actor for simulation factories and probes.</summary>
        public Zombie() : this(ZombieKind.Normal)
        {
        }
        /// <summary>Creates an unattached actor with a stable initial species.</summary>
        /// <param name="kind">The native species used by resource probes and simulation factories.</param>
        public Zombie(ZombieKind kind)
        {
            Kind = kind;
        }

        /// <summary>Retains the capability owner for this actor.</summary>
        private IZombieBehavior? _behavior;
        /// <summary>Records the behavior's kind independently of mode normalization.</summary>
        private ZombieKind _behaviorKind = (ZombieKind)(-1);
        /// <summary>Gets the species action owner, creating it only on first use or a kind change.</summary>
        internal IZombieBehavior Behavior
        {
            get
            {
                if (_behavior == null || _behaviorKind != Kind)
                {
                    _behavior = ZombieBehaviorFactory.Create(Kind);
                    _behaviorKind = Kind;
                }
                return _behavior;
            }
        }

        /// <summary>Invalidates the owning store after indexed state changes.</summary>
        internal Action? IndexChanged;

        /// <summary>Retains the original giant's weapon and authored action event.</summary>
        public global::PlantsVsZombies.Adventure.AdventureGargantuarState? GargantuarState
        {
            get => _behaviorKind == Kind ? (_behavior as GargantuarZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is GargantuarZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original thrown imp's ballistic motion independently of body playback.</summary>
        public global::PlantsVsZombies.Adventure.AdventureThrownImpState? ThrownImpState
        {
            get => _behaviorKind == Kind ? (_behavior as ThrownImpZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is ThrownImpZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original pogo's bounce counter, held reanimation, and stick-loss transitions.</summary>
        public global::PlantsVsZombies.Adventure.AdventurePogoState? PogoState
        {
            get => _behaviorKind == Kind ? (_behavior as PogoZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is PogoZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original digger emergence, axe-loss pause, and attached dirt effects.</summary>
        public global::PlantsVsZombies.Adventure.AdventureDiggerState? DiggerState
        {
            get => _behaviorKind == Kind ? (_behavior as DiggerZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is DiggerZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original dolphin jump counter independently of animation playback.</summary>
        public global::PlantsVsZombies.Adventure.AdventureDolphinState? DolphinState
        {
            get => _behaviorKind == Kind ? (_behavior as DolphinZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is DolphinZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original linked sled team's member position and native phase countdown.</summary>
        public global::PlantsVsZombies.Adventure.AdventureBobsledState? BobsledState
        {
            get => _behaviorKind == Kind ? (_behavior as BobsledZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is BobsledZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original catapult ammunition, reload and puncture state.</summary>
        public global::PlantsVsZombies.Adventure.AdventureCatapultState? CatapultState
        {
            get => _behaviorKind == Kind ? (_behavior as CatapultZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is CatapultZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original ice vehicle's smoke and punctured-tire death state.</summary>
        public global::PlantsVsZombies.Adventure.AdventureZomboniState? ZomboniState
        {
            get => _behaviorKind == Kind ? (_behavior as ZomboniZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is ZomboniZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Retains the original kelp attachment selected by a living Tangle Kelp.</summary>
        public global::PlantsVsZombies.Adventure.AdventureTangleKelpState? TangleKelpGrab { get; internal set; }
        /// <summary>Retains the original water-body altitude, duck-tube initialization, and pool emergence effects.</summary>
        public global::PlantsVsZombies.Adventure.AdventureZombiePoolState? PoolState { get; internal set; }
        /// <summary>Stores presentation animation age, presentation plant head age, presentation flash, presentation armor flash, presentation butter, presentation previous x.</summary>
        public float PresentationAnimationAge { get; internal set; }
        /// <summary>Gets the PresentationPlantHeadAge state maintained by simulation capabilities.</summary>
        public float PresentationPlantHeadAge { get; internal set; }
        /// <summary>Gets the PresentationFlash state maintained by simulation capabilities.</summary>
        public float PresentationFlash { get; internal set; }
        /// <summary>Gets the PresentationArmorFlash state maintained by simulation capabilities.</summary>
        public float PresentationArmorFlash { get; internal set; }
        /// <summary>Gets the PresentationButter state maintained by simulation capabilities.</summary>
        public float PresentationButter { get; internal set; }
        /// <summary>Gets the PresentationPreviousX state maintained by simulation capabilities.</summary>
        public float PresentationPreviousX { get; internal set; }
        /// <summary>Stores presentation walk2.</summary>
        public bool PresentationWalk2 { get; internal set; }
        /// <summary>Overrides native body transition blending after magnetic shield detachment, or minus one for species defaults.</summary>
        internal int PresentationBodyBlendTicks = -1;
        /// <summary>Stores presentation animation.</summary>
        public string PresentationAnimation { get; internal set; } = string.Empty;
        /// <summary>Stores the original pole-vault transition blend, including the zero-tick landing handoff.</summary>
        internal int PresentationPoleVaultBlendTicks = 20;
        /// <summary>Stores presentation death.</summary>
        public global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation PresentationDeath { get; internal set; }
        /// <summary>Stores presentation initialized.</summary>
        internal bool _presentationInitialized;
        /// <summary>Stores presentation health before hit.</summary>
        public float PresentationHealthBeforeHit { get; internal set; } = -1f;
        /// <summary>Stores presentation native phase.</summary>
        public int PresentationNativePhase { get; internal set; } = -1;
        /// <summary>Stores whether the Whack-a-Zombie native speed must remain fixed through grave emergence.</summary>
        public bool PresentationWhackSpeed { get; internal set; }
        /// <summary>Stores presentation in pool, presentation has sled, presentation walking backwards.</summary>
        public bool? PresentationInPool { get; internal set; }
        /// <summary>Gets the PresentationHasSled state maintained by simulation capabilities.</summary>
        public bool? PresentationHasSled { get; internal set; }
        /// <summary>Gets the PresentationWalkingBackwards state maintained by simulation capabilities.</summary>
        public bool? PresentationWalkingBackwards { get; internal set; }
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
        /// <summary>Stores wave.</summary>
        public int Wave { get; internal set; }
        /// <summary>Stores speed.</summary>
        public float Speed { get; internal set; }
        /// <summary>Stores frozen remaining.</summary>
        public float FrozenRemaining { get; internal set; }
        /// <summary>Retains the original dancer phase counters and linked backup identifiers.</summary>
        public global::PlantsVsZombies.Adventure.AdventureDancerState? DancerState
        {
            get => _behaviorKind == Kind ? (_behavior as DancerZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is DancerZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Stores whether the original lawn row equips the zombie's pool costume before entry.</summary>
        public bool PresentationPoolRow { get; internal set; }
        /// <summary>Stores the original pool or falling altitude in pixels.</summary>
        public float PresentationAltitude { get; internal set; }
        /// <summary>Stores the original one-frame mPosY displacement when a pole vault hits Tall-nut.</summary>
        public float PresentationVaultPositionY { get; internal set; }
        /// <summary>Stores the native horizontal velocity retained throughout a pole vault.</summary>
        internal float _poleVaultVelocity;
        /// <summary>Stores the original balloon pop and walking transition blend duration in native updates.</summary>
        internal int PresentationBalloonBlendTicks;
        /// <summary>Retains the original jack-in-the-box phase countdown in native ticks.</summary>
        public int JackInTheBoxTicks { get; internal set; }
        /// <summary>Stores the original ambient vocal countdown in one-hundred-hertz playing updates.</summary>
        internal int GroanTicks;
        /// <summary>Stores ability timer.</summary>
        public float AbilityTimer { get; internal set; }
        /// <summary>The original flying attachment health, included in wave health until the balloon is destroyed.</summary>
        public int FlyingHealth { get; internal set; }
        /// <summary>The original one-in-five initialization variant selected by Board::AddZombieInRow.</summary>
        public bool NativeVariant { get; internal set; }
        /// <summary>Stores airborne.</summary>
        public bool Airborne { get; internal set; }
        /// <summary>Retains the original BlowingAway flag until the balloon passes the native right-edge exit.</summary>
        public bool BlowingAway { get; internal set; }
        /// <summary>Stores submerged.</summary>
        public bool Submerged { get; internal set; }
        /// <summary>Stores hypnotized.</summary>
        public bool Hypnotized { get; internal set; }
        /// <summary>Stores used ability.</summary>
        public bool UsedAbility { get; internal set; }
        /// <summary>Stores thrown imp.</summary>
        public bool ThrownImp { get; internal set; }
        /// <summary>Stores ability phase.</summary>
        public int AbilityPhase { get; internal set; }
        /// <summary>Stores is small.</summary>
        public bool IsSmall { get; internal set; }
        /// <summary>Stores headless.</summary>
        public bool Headless { get; internal set; }
        /// <summary>Retains the native bungee movement and stolen-plant lifecycle.</summary>
        public global::PlantsVsZombies.Adventure.AdventureBungeeState? BungeeState
        {
            get => _behaviorKind == Kind ? (_behavior as BungeeZombieBehavior)?.State : null;
            internal set
            {
                if (Behavior is BungeeZombieBehavior behavior)
                {
                    behavior.State = value;
                }
            }
        }
        /// <summary>Identifies the bungee whose body currently renders this suspended passenger.</summary>
        public int BungeeCarrierId { get; internal set; }
        /// <summary>Stores bungee carrier.</summary>
        public bool BungeeCarrier { get; internal set; }
        /// <summary>Stores drop remaining.</summary>
        public float DropRemaining { get; internal set; }
        /// <summary>Stores the original grave ambush countdown, independently of airborne bungee drops.</summary>
        public int GraveRiseTicks { get; internal set; }
        /// <summary>Stores linked zombie id.</summary>
        public int LinkedZombieId { get; internal set; }
        /// <summary>Stores PC mUseLadderCol so one placed ladder cannot repeatedly retrigger the same crossing.</summary>
        public int UseLadderColumn { get; internal set; } = -1;
        /// <summary>Stores target column.</summary>
        public int TargetColumn { get; internal set; } = -1;
        /// <summary>Stores starting health.</summary>
        public float StartingHealth { get; internal set; }
        /// <summary>Stores presentation bite body y, presentation bite clip height.</summary>
        public int PresentationBiteBodyY { get; internal set; }
        /// <summary>Gets the PresentationBiteClipHeight state maintained by simulation capabilities.</summary>
        public int PresentationBiteClipHeight { get; internal set; } = -10000;
        /// <summary>Stores presentation garlic counter.</summary>
        public int PresentationGarlicCounter { get; internal set; } = -1;
        /// <summary>Stores presentation bite clip.</summary>
        internal string? _presentationBiteClip;
        /// <summary>Stores presentation bite cycle.</summary>
        internal double _presentationBiteCycle;
        /// <summary>Stores presentation garlic plant.</summary>
        internal Plant? _presentationGarlicPlant;
        /// <summary>Stores presentation garlic held age,  presentation garlic held head age.</summary>
        internal float _presentationGarlicHeldAge, _presentationGarlicHeldHeadAge;
        /// <summary>Stores presentation max armor, presentation max shield, presentation newspaper gasp remaining.</summary>
        public float PresentationMaxArmor { get; internal set; }
        /// <summary>Gets the PresentationMaxShield state maintained by simulation capabilities.</summary>
        public float PresentationMaxShield { get; internal set; }
        /// <summary>Gets the PresentationNewspaperGaspRemaining state maintained by simulation capabilities.</summary>
        public float PresentationNewspaperGaspRemaining { get; internal set; }
        /// <summary>Tracks the original independent question-mark attachment, or a negative value when absent.</summary>
        public float PresentationNewspaperSurpriseAge { get; internal set; } = -1f;
        /// <summary>Stores presentation armor before hit,  presentation shield before hit.</summary>
        internal float _presentationArmorBeforeHit, _presentationShieldBeforeHit;
        /// <summary>Stores presentation native velocity.</summary>
        public float PresentationNativeVelocity { get; internal set; }
        /// <summary>Stores presentation speed phase,  presentation speed kind.</summary>
        internal int _presentationSpeedPhase = int.MinValue, _presentationSpeedKind = -1;
        /// <summary>Stores presentation arm lost.</summary>
        public bool PresentationArmLost { get; internal set; }
        /// <summary>Stores presentation shield recoil.</summary>
        public float PresentationShieldRecoil { get; internal set; }
        /// <summary>Stores presentation native height.</summary>
        public int PresentationNativeHeight { get; internal set; } = -1;
        /// <summary>Stores presentation movement rate.</summary>
        public float PresentationMovementRate { get; internal set; } = -1f;
        /// <summary>Stores presentation movement animation.</summary>
        internal string? _presentationMovementAnimation;
        /// <summary>Stores presentation walk phase,  presentation tick slow.</summary>
        internal float _presentationWalkPhase, _presentationTickSlow = 1f;
        /// <summary>Stores presentation walk loops.</summary>
        internal int _presentationWalkLoops;
        /// <summary>Retains the previous native animation cycle for movement-timed particle events.</summary>
        internal double _presentationPreviousWalkCycle;
        /// <summary>Stores kind.</summary>
        public ZombieKind Kind { get; internal set; }
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
        /// <summary>Stores x.</summary>
        public float X { get; internal set; }
        /// <summary>Stores health.</summary>
        public float Health { get; internal set; }
        /// <summary>Stores armor health.</summary>
        public float ArmorHealth { get; internal set; }
        /// <summary>Stores shield health.</summary>
        public float ShieldHealth { get; internal set; }
        /// <summary>Stores bite timer.</summary>
        public float BiteTimer { get; internal set; }
        /// <summary>Stores age.</summary>
        public float Age { get; internal set; }
        /// <summary>Stores chill remaining.</summary>
        public float ChillRemaining { get; internal set; }
        /// <summary>Stores eating.</summary>
        public bool Eating { get; internal set; }
        /// <summary>Stores mode head.</summary>
        public int ModeHead { get; internal set; } = -1;
        /// <summary>Stores mode head timer.</summary>
        internal float _modeHeadTimer = 1.5f;
        /// <summary>Stores mode portal cooldown.</summary>
        internal float _modePortalCooldown;
    }
}
