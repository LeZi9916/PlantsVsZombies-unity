#nullable enable
using System;

namespace PlantsVsZombies.BoardUI
{
    /// <summary>A top-left-origin integer rectangle, in native logical pixels.</summary>
    public readonly struct NativeRect
    {
        /// <summary>Stores x.</summary>
        public readonly int X;
        /// <summary>Stores y.</summary>
        public readonly int Y;
        /// <summary>Stores width.</summary>
        public readonly int Width;
        /// <summary>Stores height.</summary>
        public readonly int Height;
        /// <summary>Initializes native rect.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public NativeRect(int x, int y, int width, int height)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
