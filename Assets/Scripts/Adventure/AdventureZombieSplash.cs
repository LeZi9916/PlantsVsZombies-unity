#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains an original pool splash and its independently positioned planting particles.</summary>
    public sealed class AdventureZombieSplash
    {
        /// <summary>Identifies the effect instance and its lawn row.</summary>
        public int Id, Row;
        /// <summary>Stores the original splash and particle origins in board pixels.</summary>
        public float X, Y, ParticleX, ParticleY;
        /// <summary>Stores the original nonuniform splash scale and elapsed time.</summary>
        public float ScaleX, ScaleY, Age;
    }
}
