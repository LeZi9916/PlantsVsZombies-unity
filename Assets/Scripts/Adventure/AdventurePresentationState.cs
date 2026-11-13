#nullable enable
using PlantsVsZombies.Simulation;

using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure presentation state.</summary>
    public sealed class AdventurePresentationState
    {
        /// <summary>Defines the eight-frame, forty-FPS pot-opening hammer duration in native updates.</summary>
        internal const int VaseMalletDurationTicks = 20;
        /// <summary>Gets the column beneath the single active pot-opening hammer.</summary>
        public int VaseMalletColumn { get; internal set; }
        /// <summary>Gets the row beneath the single active pot-opening hammer.</summary>
        public int VaseMalletRow { get; internal set; }
        /// <summary>Gets the native updates remaining before the hammer releases its vase contents.</summary>
        public int VaseMalletTicks { get; internal set; }
        /// <summary>Gets the elapsed seconds of the current forty-FPS pot-opening animation.</summary>
        public float VaseMalletAge => (VaseMalletDurationTicks - VaseMalletTicks) * .01f;
        /// <summary>Counts accepted pot-opening swings that request the original Swing Foley.</summary>
        public int VaseSwingSoundSerial { get; internal set; }
        /// <summary>Counts opened pots that request the original Bonk and VaseBreaking sounds.</summary>
        public int VaseBreakSoundSerial { get; internal set; }
        /// <summary>Identifies each original screen flash emitted when a puzzle stage is cleared.</summary>
        public int VaseScreenFlashSerial { get; internal set; }
        /// <summary>Gets or sets elapsed native flash updates, or minus one before the first flash.</summary>
        internal int VaseScreenFlashTicks { get; set; } = -1;
        /// <summary>Gets the screen flash's native age in seconds, or minus one before the first flash.</summary>
        public float VaseScreenFlashAge => VaseScreenFlashTicks < 0 ? -1f : VaseScreenFlashTicks * .01f;
        /// <summary>Retains the original PC flag meter's native animation state.</summary>
        public readonly AdventureProgressMeterState ProgressMeter = new AdventureProgressMeterState();
        /// <summary>Retains the native board widget displacement shared by board artwork and its interface.</summary>
        public readonly AdventureScreenShakeState ScreenShake = new AdventureScreenShakeState();
        /// <summary>Retains original dust and splash effects after a Squash's actual ground impact.</summary>
        internal readonly List<AdventureSquashLanding> _squashLandings = new List<AdventureSquashLanding>();
        /// <summary>Retains original delayed sound milestones for plant actions resolved atomically by the simulation.</summary>
        internal readonly List<AdventurePlantSoundCue> _plantSoundCues = new List<AdventurePlantSoundCue>();
        /// <summary>Stores explosions.</summary>
        public readonly List<AdventureExplosion> Explosions = new List<AdventureExplosion>();
        /// <summary>Stores jalapeno fires.</summary>
        public readonly List<AdventureJalapenoFire> JalapenoFires = new List<AdventureJalapenoFire>();
        /// <summary>Stores snow trails.</summary>
        public readonly List<AdventureSnowTrail> SnowTrails = new List<AdventureSnowTrail>();
        /// <summary>Stores ice launch sound serial, chill onset sound serial.</summary>
        public int IceLaunchSoundSerial, ChillOnsetSoundSerial;
        /// <summary>Suppresses generic launch samples when the original plant Foley has already been requested.</summary>
        internal int _nativeShootSuppressions;
        /// <summary>Suppresses generic Cherry Bomb samples for impacts that use their own native Foley.</summary>
        internal int _nativeExplosionSuppressions;
        /// <summary>Marks the synchronous player planting event whose sound is supplied by native planting Foley.</summary>
        internal bool _nativePlantingEffects;
        /// <summary>Retains original planting bursts above the board actors for their authored lifetime.</summary>
        public readonly List<AdventurePlantingEffect> PlantingEffects = new List<AdventurePlantingEffect>();
        /// <summary>Stores plant bite effects.</summary>
        public readonly List<AdventurePlantBiteEffect> PlantBiteEffects = new List<AdventurePlantBiteEffect>();
        /// <summary>Stores potato mine rise sound serial.</summary>
        public int PotatoMineRiseSoundSerial;
        /// <summary>Stores the number of Grave Buster landings that requested the original chomp sound.</summary>
        public int GraveBusterChompSoundSerial;
        /// <summary>Stores corpses.</summary>
        public readonly List<AdventureZombieCorpse> Corpses = new List<AdventureZombieCorpse>();
        /// <summary>Retains original ice vehicle explosion and tire systems after their source actor is removed.</summary>
        public readonly List<AdventureZomboniParticle> ZomboniParticles = new List<AdventureZomboniParticle>();
        /// <summary>Stores impacts.</summary>
        public readonly List<AdventureImpact> Impacts = new List<AdventureImpact>();
        /// <summary>Stores mower times.</summary>
        public readonly Dictionary<Mower, float> MowerTimes = new Dictionary<Mower, float>();
        /// <summary>Stores bowling.</summary>
        public readonly Dictionary<AdventureBowlingNut, AdventureBowlingPresentation> Bowling = new Dictionary<AdventureBowlingNut, AdventureBowlingPresentation>();
        /// <summary>Stores effect id.</summary>
        internal int _effectId;
        /// <summary>Stores arm drops.</summary>
        public readonly List<AdventureZombieArmDrop> ArmDrops = new List<AdventureZombieArmDrop>();
        /// <summary>Stores zombie part drops.</summary>
        public readonly List<AdventureZombiePartDrop> ZombiePartDrops = new List<AdventureZombiePartDrop>();
        // PC DropArm/DropHead both request Foley 25 (limbs_pop), once per successful drop.
        /// <summary>Stores zombie part sound serial.</summary>
        public int ZombiePartSoundSerial;
        /// <summary>Stores zombie sounds.</summary>
        public readonly List<AdventureZombieSoundEvent> ZombieSounds = new List<AdventureZombieSoundEvent>();
        /// <summary>Retains original grave-rise particles for their independent two-second lifetime.</summary>
        public readonly List<AdventureZombieRiseEffect> ZombieRiseEffects = new List<AdventureZombieRiseEffect>();
        /// <summary>Retains short-lived native foot-dust bursts below the zombies in their lawn row.</summary>
        public readonly List<AdventureZombieFootstepEffect> FootstepEffects = new List<AdventureZombieFootstepEffect>(32);
        /// <summary>Retains original pool splashes independently of the zombie's later phases.</summary>
        public readonly List<AdventureZombieSplash> ZombieSplashes = new List<AdventureZombieSplash>();
        /// <summary>Counts Whack-a-Zombie dirt-rise sound requests.</summary>
        public int ZombieRiseSoundSerial;
    }
}
