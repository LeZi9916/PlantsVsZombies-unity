#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines game rules.</summary>
    public sealed class GameRules
    {
        /// <summary>Stores adventure cycle.</summary>
        public int AdventureCycle = 1;
        /// <summary>Stores adventure dave seed.</summary>
        public int AdventureDaveSeed = 12345;
        /// <summary>Stores adventure packet slots.</summary>
        public int AdventurePacketSlots = 6;
        /// <summary>Stores adventure coin commit.</summary>
        public Func<int, bool>? AdventureCoinCommit;
        /// <summary>Stores adventure first aid.</summary>
        public bool AdventureFirstAid;
        /// <summary>Stores adventure potted loot, adventure chocolate loot, adventure disable loot.</summary>
        public bool AdventurePottedLoot, AdventureChocolateLoot, AdventureDisableLoot;
        /// <summary>Stores adventure minigames unlocked, adventure puzzle unlocked.</summary>
        public bool AdventureMinigamesUnlocked, AdventurePuzzleUnlocked;
        /// <summary>Stores adventure loot commit.</summary>
        public Func<AdventureCoin, bool>? AdventureLootCommit;
        /// <summary>Stores adventure place rake.</summary>
        public Func<bool>? AdventurePlaceRake;
        /// <summary>Null leaves the first-adventure unlocks and loadouts unchanged. Captured at construction.</summary>
        public AdventureSeedAccess? AdventureSeedAccess;
        /// <summary>Opt in to the incomplete, explicitly approximate 1-1 experiment.</summary>
        public bool EnableExperimentalLevelOne = false;
        /// <summary>1..50 opts into the separate Adventure module; 0 retains the legacy demo.</summary>
        public int AdventureLevel = 0;
        /// <summary>Stores adventure roof cleaners.</summary>
        public bool AdventureRoofCleaners = false;
        /// <summary>Stores adventure pool cleaners.</summary>
        public bool AdventurePoolCleaners = false;
        /// <summary>Stores starting sun.</summary>
        public int StartingSun = 150;
        /// <summary>Stores zombie count.</summary>
        public int ZombieCount = 12;
        /// <summary>Stores first zombie delay.</summary>
        public float FirstZombieDelay = 15f;
        /// <summary>Stores zombie interval.</summary>
        public float ZombieInterval = 9f;
        /// <summary>Stores sky sun interval.</summary>
        public float SkySunInterval = 9f;
        /// <summary>Stores zombie speed.</summary>
        public float ZombieSpeed = 0.18f;
        /// <summary>Stores mowers enabled.</summary>
        public bool MowersEnabled = true;
        /// <summary>Used only when EnableExperimentalLevelOne is true.</summary>
        public ExperimentalLevelOneOptions LevelOneOptions = new ExperimentalLevelOneOptions();
    }
}
