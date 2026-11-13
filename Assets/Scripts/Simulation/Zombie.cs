#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines zombie.</summary>
    public sealed class Zombie
    {
        /// <summary>Stores presentation animation age, presentation plant head age, presentation flash, presentation armor flash, presentation butter, presentation previous x.</summary>
        public float PresentationAnimationAge, PresentationPlantHeadAge, PresentationFlash, PresentationArmorFlash, PresentationButter, PresentationPreviousX;
        /// <summary>Stores presentation walk2.</summary>
        public bool PresentationWalk2;
        /// <summary>Stores presentation animation.</summary>
        public string PresentationAnimation = string.Empty;
        /// <summary>Stores presentation death.</summary>
        public global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation PresentationDeath;
        /// <summary>Stores presentation initialized.</summary>
        internal bool _presentationInitialized;
        /// <summary>Stores presentation health before hit.</summary>
        public float PresentationHealthBeforeHit = -1f;
        /// <summary>Stores presentation native phase.</summary>
        public int PresentationNativePhase = -1;
        /// <summary>Stores whether the Whack-a-Zombie native speed must remain fixed through grave emergence.</summary>
        public bool PresentationWhackSpeed;
        /// <summary>Stores presentation in pool, presentation has sled, presentation walking backwards.</summary>
        public bool? PresentationInPool, PresentationHasSled, PresentationWalkingBackwards;
        /// <summary>Stores id.</summary>
        public int Id;
        /// <summary>Stores wave.</summary>
        public int Wave;
        /// <summary>Stores speed.</summary>
        public float Speed;
        /// <summary>Stores frozen remaining.</summary>
        public float FrozenRemaining;
        /// <summary>Retains the original dancer phase counters and linked backup identifiers.</summary>
        public global::PlantsVsZombies.Adventure.AdventureDancerState? DancerState;
        /// <summary>Stores whether the original lawn row equips the zombie's pool costume before entry.</summary>
        public bool PresentationPoolRow;
        /// <summary>Stores the original pool or falling altitude in pixels.</summary>
        public float PresentationAltitude;
        /// <summary>Stores the original one-frame mPosY displacement when a pole vault hits Tall-nut.</summary>
        public float PresentationVaultPositionY;
        /// <summary>Stores the native horizontal velocity retained throughout a pole vault.</summary>
        internal float _poleVaultVelocity;
        /// <summary>Stores ability timer.</summary>
        public float AbilityTimer;
        /// <summary>Stores airborne.</summary>
        public bool Airborne;
        /// <summary>Stores submerged.</summary>
        public bool Submerged;
        /// <summary>Stores hypnotized.</summary>
        public bool Hypnotized;
        /// <summary>Stores used ability.</summary>
        public bool UsedAbility;
        /// <summary>Stores thrown imp.</summary>
        public bool ThrownImp;
        /// <summary>Stores ability phase.</summary>
        public int AbilityPhase;
        /// <summary>Stores is small.</summary>
        public bool IsSmall;
        /// <summary>Stores headless.</summary>
        public bool Headless;
        /// <summary>Stores bungee carrier.</summary>
        public bool BungeeCarrier;
        /// <summary>Stores drop remaining.</summary>
        public float DropRemaining;
        /// <summary>Stores the original grave ambush countdown, independently of airborne bungee drops.</summary>
        public int GraveRiseTicks;
        /// <summary>Stores linked zombie id.</summary>
        public int LinkedZombieId;
        /// <summary>Stores target column.</summary>
        public int TargetColumn = -1;
        /// <summary>Stores starting health.</summary>
        public float StartingHealth;
        /// <summary>Stores presentation bite body y, presentation bite clip height.</summary>
        public int PresentationBiteBodyY, PresentationBiteClipHeight = -10000;
        /// <summary>Stores presentation garlic counter.</summary>
        public int PresentationGarlicCounter = -1;
        /// <summary>Stores presentation bite clip.</summary>
        internal string? _presentationBiteClip;
        /// <summary>Stores presentation bite cycle.</summary>
        internal double _presentationBiteCycle;
        /// <summary>Stores presentation garlic plant.</summary>
        internal Plant? _presentationGarlicPlant;
        /// <summary>Stores presentation garlic held age,  presentation garlic held head age.</summary>
        internal float _presentationGarlicHeldAge, _presentationGarlicHeldHeadAge;
        /// <summary>Stores presentation max armor, presentation max shield, presentation newspaper gasp remaining.</summary>
        public float PresentationMaxArmor, PresentationMaxShield, PresentationNewspaperGaspRemaining;
        /// <summary>Tracks the original independent question-mark attachment, or a negative value when absent.</summary>
        public float PresentationNewspaperSurpriseAge = -1f;
        /// <summary>Stores presentation armor before hit,  presentation shield before hit.</summary>
        internal float _presentationArmorBeforeHit, _presentationShieldBeforeHit;
        /// <summary>Stores presentation native velocity.</summary>
        public float PresentationNativeVelocity;
        /// <summary>Stores presentation speed phase,  presentation speed kind.</summary>
        internal int _presentationSpeedPhase = int.MinValue, _presentationSpeedKind = -1;
        /// <summary>Stores presentation arm lost.</summary>
        public bool PresentationArmLost;
        /// <summary>Stores presentation shield recoil.</summary>
        public float PresentationShieldRecoil;
        /// <summary>Stores presentation native height.</summary>
        public int PresentationNativeHeight = -1;
        /// <summary>Stores presentation movement rate.</summary>
        public float PresentationMovementRate = -1f;
        /// <summary>Stores presentation movement animation.</summary>
        internal string? _presentationMovementAnimation;
        /// <summary>Stores presentation walk phase,  presentation tick slow.</summary>
        internal float _presentationWalkPhase, _presentationTickSlow = 1f;
        /// <summary>Stores presentation walk loops.</summary>
        internal int _presentationWalkLoops;
        /// <summary>Retains the previous native animation cycle for movement-timed particle events.</summary>
        internal double _presentationPreviousWalkCycle;
        /// <summary>Stores kind.</summary>
        public ZombieKind Kind;
        /// <summary>Stores row.</summary>
        public int Row;
        /// <summary>Stores x.</summary>
        public float X;
        /// <summary>Stores health.</summary>
        public float Health;
        /// <summary>Stores armor health.</summary>
        public float ArmorHealth;
        /// <summary>Stores shield health.</summary>
        public float ShieldHealth;
        /// <summary>Stores bite timer.</summary>
        public float BiteTimer;
        /// <summary>Stores age.</summary>
        public float Age;
        /// <summary>Stores chill remaining.</summary>
        public float ChillRemaining;
        /// <summary>Stores eating.</summary>
        public bool Eating;
        /// <summary>Stores mode head.</summary>
        public int ModeHead = -1;
        /// <summary>Stores mode head timer.</summary>
        internal float _modeHeadTimer = 1.5f;
        /// <summary>Stores mode portal cooldown.</summary>
        internal float _modePortalCooldown;
    }
}
