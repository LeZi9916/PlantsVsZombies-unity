#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains an original ice vehicle particle system after the vehicle leaves the board.</summary>
    public sealed class AdventureZomboniParticle
    {
        /// <summary>Stores the independent particle identity, random seed, and native lawn row.</summary>
        public int Id, Seed, Row;
        /// <summary>Stores the original board-pixel creation position.</summary>
        public float X, Y;
        /// <summary>Stores the elapsed original particle-system time in seconds.</summary>
        public float Age;
        /// <summary>Stores the unchanged original particle definition name.</summary>
        public string Particle = "ZamboniExplosion";
        /// <summary>Gets the authored maximum system duration from the original XML.</summary>
        public float Duration => Particle == "ZamboniTire" ? 5f : 2f;
    }
}
