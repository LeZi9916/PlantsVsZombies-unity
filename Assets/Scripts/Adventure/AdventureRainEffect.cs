#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains one original RainSplash or RainCircle reanimation at native layer 200000.</summary>
    internal struct AdventureRainEffect
    {
        /// <summary>Gets whether this effect uses RainCircle instead of RainSplash.</summary>
        internal bool Circle { get; }
        /// <summary>Gets the original reanimation origin in board pixels.</summary>
        internal float X { get; }
        /// <summary>Gets the original reanimation origin in board pixels.</summary>
        internal float Y { get; }
        /// <summary>Gets the shared horizontal and vertical scale.</summary>
        internal float Scale { get; }
        /// <summary>Gets the original opacity from zero through 255.</summary>
        internal int Alpha { get; }
        /// <summary>Gets the elapsed native updates of this one-shot animation.</summary>
        internal int Ticks { get; set; }

        /// <summary>Creates an original rain reanimation without changing its twelve-frame-per-second playback.</summary>
        /// <param name="circle">Whether to use RainCircle.</param>
        /// <param name="x">The board-space horizontal origin.</param>
        /// <param name="y">The board-space vertical origin.</param>
        /// <param name="scale">The uniform native scale.</param>
        /// <param name="alpha">The native opacity.</param>
        internal AdventureRainEffect(bool circle, float x, float y, float scale, int alpha)
        {
            Circle = circle;
            X = x;
            Y = y;
            Scale = scale;
            Alpha = alpha;
            Ticks = 0;
        }
    }
}
