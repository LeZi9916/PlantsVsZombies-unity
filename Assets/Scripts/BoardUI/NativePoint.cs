#nullable enable

namespace PlantsVsZombies.BoardUI
{
    /// <summary>A top-left-origin point, in native logical pixels.</summary>
    public readonly struct NativePoint
    {
        /// <summary>Stores x.</summary>
        public readonly int X;
        /// <summary>Stores y.</summary>
        public readonly int Y;
        /// <summary>Initializes native point.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
