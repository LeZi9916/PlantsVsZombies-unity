#nullable enable
using PlantsVsZombies.Simulation;

using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure presentation state.</summary>
    public sealed class AdventurePresentationState
    {
        /// <summary>Stores explosions.</summary>
        public readonly List<AdventureExplosion> Explosions = new List<AdventureExplosion>();
        /// <summary>Stores jalapeno fires.</summary>
        public readonly List<AdventureJalapenoFire> JalapenoFires = new List<AdventureJalapenoFire>();
        /// <summary>Stores snow trails.</summary>
        public readonly List<AdventureSnowTrail> SnowTrails = new List<AdventureSnowTrail>();
        /// <summary>Stores ice launch sound serial, chill onset sound serial.</summary>
        public int IceLaunchSoundSerial, ChillOnsetSoundSerial;
        /// <summary>Stores ice shoot suppressions.</summary>
        internal int _iceShootSuppressions;
        /// <summary>Stores plant bite effects.</summary>
        public readonly List<AdventurePlantBiteEffect> PlantBiteEffects = new List<AdventurePlantBiteEffect>();
        /// <summary>Stores potato mine rise sound serial.</summary>
        public int PotatoMineRiseSoundSerial;
        /// <summary>Stores corpses.</summary>
        public readonly List<AdventureZombieCorpse> Corpses = new List<AdventureZombieCorpse>();
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
    }
}
