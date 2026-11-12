#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro particle.</summary>
    public readonly struct AdventureIntroParticle
    {
        /// <summary>Stores asset.</summary>
        public readonly string Asset;
        /// <summary>Stores x, y, seconds.</summary>
        public readonly float X, Y, Seconds;
        /// <summary>Stores render order.</summary>
        public readonly int RenderOrder;
        /// <summary>Initializes adventure intro particle.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "order">The order.</param>
        public AdventureIntroParticle(string asset, float x, float y, float seconds, int order)
        {
            Asset = asset;
            X = x;
            Y = y;
            Seconds = seconds;
            RenderOrder = order;
        }
    }
}
