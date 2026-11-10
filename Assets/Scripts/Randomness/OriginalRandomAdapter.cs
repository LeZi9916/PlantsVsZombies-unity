#nullable enable
using System;

namespace PlantsVsZombies.Randomness
{
    /// <summary>
    /// Routes existing System.Random consumers through the local executable's 31-bit MT stream.
    /// Integer bounds use native remainder selection. Floating draws use the local 0x5E7060 scale-1 float result.
    /// Other original subsystems' initialization and draw order remain outside this adapter.
    /// </summary>
    public sealed class OriginalRandomAdapter : System.Random
    {
        /// <summary>Gets or sets generator.</summary>
        public OriginalRandom Generator { get; private set; }
        /// <summary>Gets or sets seed.</summary>
        public uint Seed { get; private set; }

        /// <summary>Initializes original random adapter.</summary>
        /// <param name = "seed">The seed.</param>
        public OriginalRandomAdapter(uint seed) : base(0)
        {
            Seed = seed == 0 ? OriginalRandom.DefaultSeed : seed;
            Generator = new OriginalRandom(seed);
        }

        /// <summary>Performs next.</summary>
        /// <returns>The resulting next.</returns>
        public override int Next()
        {
            return (int)Generator.Next31();
        }

        /// <summary>Performs next.</summary>
        /// <param name = "maxValue">The max value.</param>
        /// <returns>The resulting next.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return maxValue == 0 ? 0 : (int)Generator.NextBounded((uint)maxValue);
        }

        /// <summary>Performs next.</summary>
        /// <param name = "minValue">The min value.</param>
        /// <param name = "maxValue">The max value.</param>
        /// <returns>The resulting next.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            var span = (ulong)((long)maxValue - minValue);
            if (span == 0)
            {
                return minValue;
            }

            var draw = span <= 0x80000000UL ? Generator.Next31() : ((ulong)Generator.Next31() << 1) | (Generator.Next31() & 1u);
            return (int)(minValue + (long)(draw % span));
        }

        /// <summary>Performs sample.</summary>
        /// <returns>The resulting sample.</returns>
        protected override double Sample()
        {
            return (double)(float)(Generator.Next31() / 2147483647.0);
        }

        /// <summary>Performs next double.</summary>
        /// <returns>The resulting next double.</returns>
        public override double NextDouble()
        {
            return Sample();
        }

        /// <summary>Performs next bytes.</summary>
        /// <param name = "buffer">The buffer.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)Generator.NextBounded(256);
            }
        }

        /// <summary>Performs capture state.</summary>
        /// <returns>The resulting capture state.</returns>
        public OriginalRandomState CaptureState()
        {
            return Generator.CaptureState();
        }

        /// <summary>Performs restore state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        public void RestoreState(OriginalRandomState snapshot)
        {
            Generator.RestoreState(snapshot);
        }
    }
}
