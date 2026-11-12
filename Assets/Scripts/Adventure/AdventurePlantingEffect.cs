#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains an original soil or water burst created by a successful player planting action.</summary>
    public struct AdventurePlantingEffect
    {
        /// <summary>Identifies this burst in the reusable particle renderer.</summary>
        public int Id;
        /// <summary>Identifies the board row where the seed was planted.</summary>
        public int Row;
        /// <summary>Seeds cosmetic particle variation independently of gameplay random draws.</summary>
        public int Seed;
        /// <summary>Stores the native horizontal particle origin in board pixels.</summary>
        public float X;
        /// <summary>Stores the native vertical particle origin in board pixels.</summary>
        public float Y;
        /// <summary>Stores elapsed native simulation time in seconds.</summary>
        public float Age;
        /// <summary>Selects the original water emitter instead of the soil emitter.</summary>
        public bool Pool;
    }
}
