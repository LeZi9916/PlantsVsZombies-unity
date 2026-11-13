#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves the PC spawn helpers' thirty-one-bit floating draws and single-precision stores.</summary>
    internal static class AdventureSpawnRandom
    {
        /// <summary>Consumes one original raw draw and applies PC RandFloat's scale before narrowing to float.</summary>
        /// <param name="random">The board's original random stream.</param>
        /// <param name="scale">The native single-precision upper bound.</param>
        /// <returns>The original scaled single-precision random result.</returns>
        internal static float NextFloat(Random random, float scale)
        {
            return ScaleFloat(random.Next(), scale);
        }

        /// <summary>Applies the PC 0x5E7060 division and final float store to an already consumed raw draw.</summary>
        /// <param name="raw">The original nonnegative thirty-one-bit random value.</param>
        /// <param name="scale">The native floating upper bound.</param>
        /// <returns>The scaled draw after the original single-precision store.</returns>
        internal static float ScaleFloat(int raw, float scale)
        {
            return (float)(raw / 2147483647.0 * scale);
        }

        /// <summary>Consumes PC 0x526F90's one draw with a float-scaled range and a separate float addition.</summary>
        /// <param name="random">The board's original random stream.</param>
        /// <param name="minimum">The inclusive native lower bound.</param>
        /// <param name="maximum">The native upper bound.</param>
        /// <returns>The original single-precision range result.</returns>
        internal static float NextRange(Random random, float minimum, float maximum)
        {
            var scaled = NextFloat(random, maximum - minimum);
            return minimum + scaled;
        }
    }
}
