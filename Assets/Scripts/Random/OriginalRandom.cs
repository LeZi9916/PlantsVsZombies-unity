using System;

namespace PvZReplica.Randomness
{
    [Serializable]
    public sealed class OriginalRandomState
    {
        private readonly uint[] words;
        public int Index { get; private set; }
        public ulong DrawCount { get; private set; }
        public OriginalRandomState(uint[] words, int index, ulong drawCount)
        {
            if (words == null || words.Length != 624 || index < 0 || index > 624)
                throw new ArgumentException("An MT checkpoint requires 624 words and an index in [0,624].");
            this.words = (uint[])words.Clone(); Index = index; DrawCount = drawCount;
        }
        public uint[] CopyWords() { return (uint[])words.Clone(); }
    }

    /// <summary>
    /// The isolated 31-bit generator used by the examined executable. A matching
    /// seed is not sufficient to reproduce a game unless every intervening draw
    /// and the original initialization order are also reproduced.
    /// </summary>
    public sealed class OriginalRandom
    {
        public const uint DefaultSeed = 4357;
        private const int StateLength = 624;
        private readonly uint[] state = new uint[StateLength];
        private int index;
        public ulong DrawCount { get; private set; }

        public OriginalRandom(uint seed)
        {
            state[0] = seed == 0 ? DefaultSeed : seed;
            unchecked
            {
                for (index = 1; index < StateLength; index++)
                {
                    uint previous = state[index - 1];
                    state[index] = 1812433253u * (previous ^ (previous >> 30)) + (uint)index;
                }
            }
        }

        /// <summary>Returns the next value in [0, 0x7fffffff].</summary>
        public uint Next31()
        {
            if (index >= StateLength) Twist();
            uint value = state[index++];
            DrawCount++;
            value ^= value >> 11;
            value ^= (value << 7) & 0x9d2c5680u;
            value ^= (value << 15) & 0xefc60000u;
            value ^= value >> 18;
            return value & 0x7fffffffu;
        }

        /// <summary>
        /// Uses the executable's unsigned remainder operation. Zero is rejected
        /// before advancing the state; the native division would instead fault.
        /// </summary>
        public uint NextBounded(uint upperExclusive)
        {
            if (upperExclusive == 0) throw new ArgumentOutOfRangeException(nameof(upperExclusive));
            return Next31() % upperExclusive;
        }

        /// <summary>A defensive copy of the exact MT state, suitable for replay checkpoints.</summary>
        public OriginalRandomState CaptureState()
        {
            return new OriginalRandomState((uint[])state.Clone(), index, DrawCount);
        }

        public void RestoreState(OriginalRandomState snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            uint[] words = snapshot.CopyWords();
            if (words.Length != StateLength || snapshot.Index < 0 || snapshot.Index > StateLength)
                throw new ArgumentException("Invalid original random state.", nameof(snapshot));
            Array.Copy(words, state, StateLength);
            index = snapshot.Index;
            DrawCount = snapshot.DrawCount;
        }

        private void Twist()
        {
            for (int i = 0; i < 227; i++)
            {
                uint combined = (state[i] & 0x80000000u) | (state[i + 1] & 0x7fffffffu);
                state[i] = state[i + 397] ^ (combined >> 1) ^ ((combined & 1u) == 0 ? 0u : 0x9908b0dfu);
            }
            for (int i = 227; i < 623; i++)
            {
                uint combined = (state[i] & 0x80000000u) | (state[i + 1] & 0x7fffffffu);
                state[i] = state[i - 227] ^ (combined >> 1) ^ ((combined & 1u) == 0 ? 0u : 0x9908b0dfu);
            }
            uint last = (state[623] & 0x80000000u) | (state[0] & 0x7fffffffu);
            state[623] = state[396] ^ (last >> 1) ^ ((last & 1u) == 0 ? 0u : 0x9908b0dfu);
            index = 0;
        }
    }
}
