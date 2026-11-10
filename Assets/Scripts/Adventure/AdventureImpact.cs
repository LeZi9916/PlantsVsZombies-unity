#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure impact.</summary>
    public sealed class AdventureImpact
    {
        /// <summary>Stores native ice point.</summary>
        public bool NativeIcePoint;
        /// <summary>Stores native particle x, native particle y, native attached x, native attached y.</summary>
        public float NativeParticleX, NativeParticleY, NativeAttachedX, NativeAttachedY;
        /// <summary>Stores attached zombie.</summary>
        public Zombie AttachedZombie;
        /// <summary>Stores id, seed.</summary>
        public int Id, Seed;
        /// <summary>Stores kind.</summary>
        public PlantKind Kind;
        /// <summary>Stores x, row, age.</summary>
        public float X, Row, Age;
        /// <summary>Stores snow, fire, butter, cob, mowed.</summary>
        public bool Snow, Fire, Butter, Cob, Mowed;
    }
}
