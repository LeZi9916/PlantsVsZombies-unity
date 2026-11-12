#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro animation.</summary>
    public readonly struct AdventureIntroAnimation
    {
        /// <summary>Stores asset.</summary>
        public readonly string Asset;
        /// <summary>Stores x, y, seconds, rate.</summary>
        public readonly float X, Y, Seconds, Rate;
        /// <summary>Stores frame count, render order.</summary>
        public readonly int FrameCount, RenderOrder;
        // Both assets use their ENTIRE original definition, not a guessed anim_ clip.
        /// <summary>Initializes adventure intro animation.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "count">The count.</param>
        /// <param name = "order">The order.</param>
        public AdventureIntroAnimation(string asset, float x, float y, float seconds, float rate, int count, int order)
        {
            Asset = asset;
            X = x;
            Y = y;
            Seconds = seconds;
            Rate = rate;
            FrameCount = count;
            RenderOrder = order;
        }
    }
}
