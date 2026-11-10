#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure snow trail.</summary>
    public sealed class AdventureSnowTrail
    {
        /// <summary>Stores id, seed, row.</summary>
        public int Id, Seed, Row;
        /// <summary>Stores age, x, y, previous x, previous y, stop age.</summary>
        public float Age, X, Y, PreviousX, PreviousY, StopAge = -1f;
        /// <summary>Stores projectile.</summary>
        public Pea Projectile;
        /// <summary>Gets or sets stopped.</summary>
        public bool Stopped
        {
            get
            {
                return StopAge >= 0f;
            }
        }
    }
}
