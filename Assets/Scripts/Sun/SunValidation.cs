#nullable enable

using System;

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun validation.</summary>
    internal static class SunValidation
    {
        /// <summary>Performs draw.</summary>
        /// <param name = "draw">The draw.</param>
        /// <param name = "bound">The bound.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal static void Draw(int draw, int bound)
        {
            if (draw < 0 || draw >= bound)
            {
                throw new ArgumentOutOfRangeException(nameof(draw));
            }
        }

        /// <summary>Performs kind.</summary>
        /// <param name = "kind">The kind.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal static void Kind(SunKind kind)
        {
            if (kind != SunKind.Normal && kind != SunKind.Small && kind != SunKind.Large)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
