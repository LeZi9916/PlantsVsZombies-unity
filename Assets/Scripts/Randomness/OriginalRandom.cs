#nullable enable
using System;

namespace PlantsVsZombies.Randomness
{
    /// <summary>
    /// The isolated 31-bit generator used by the examined executable. A matching
    /// seed is not sufficient to reproduce a game unless every intervening draw
    /// and the original initialization order are also reproduced.
    /// </summary>
    public sealed class OriginalRandom
    {
        /// <summary>Stores default seed.</summary>
        public const uint DefaultSeed = 4357;
        /// <summary>Stores state length.</summary>
        private const int StateLength = 624;
        /// <summary>Stores state.</summary>
        private readonly uint[] _state = new uint[StateLength];
        /// <summary>Stores index.</summary>
        private int _index;
        /// <summary>Gets or sets draw count.</summary>
        public ulong DrawCount { get; private set; }

        /// <summary>Initializes original random.</summary>
        /// <param name = "seed">The seed.</param>
        public OriginalRandom(uint seed)
        {
            _state[0] = seed == 0 ? DefaultSeed : seed;
            unchecked
            {
                for (_index = 1; _index < StateLength; _index++)
                {
                    var previous = _state[_index - 1];
                    _state[_index] = 1812433253u * (previous ^ (previous >> 30)) + (uint)_index;
                }
            }
        }

        /// <summary>Returns the next value in [0, 0x7fffffff].</summary>
        public uint Next31()
        {
            if (_index >= StateLength)
            {
                Twist();
            }

            var value = _state[_index++];
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
            if (upperExclusive == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upperExclusive));
            }

            return Next31() % upperExclusive;
        }

        /// <summary>A defensive copy of the exact MT state, suitable for replay checkpoints.</summary>
        public OriginalRandomState CaptureState()
        {
            return new OriginalRandomState((uint[])_state.Clone(), _index, DrawCount);
        }

        /// <summary>Performs restore state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public void RestoreState(OriginalRandomState snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var words = snapshot.CopyWords();
            if (words.Length != StateLength || snapshot.Index < 0 || snapshot.Index > StateLength)
            {
                throw new ArgumentException("Invalid original random state.", nameof(snapshot));
            }

            Array.Copy(words, _state, StateLength);
            _index = snapshot.Index;
            DrawCount = snapshot.DrawCount;
        }

        /// <summary>Performs twist.</summary>
        private void Twist()
        {
            for (var i = 0; i < 227; i++)
            {
                var combined = (_state[i] & 0x80000000u) | (_state[i + 1] & 0x7fffffffu);
                _state[i] = _state[i + 397] ^ (combined >> 1) ^ ((combined & 1u) == 0 ? 0u : 0x9908b0dfu);
            }

            for (var i = 227; i < 623; i++)
            {
                var combined = (_state[i] & 0x80000000u) | (_state[i + 1] & 0x7fffffffu);
                _state[i] = _state[i - 227] ^ (combined >> 1) ^ ((combined & 1u) == 0 ? 0u : 0x9908b0dfu);
            }

            var last = (_state[623] & 0x80000000u) | (_state[0] & 0x7fffffffu);
            _state[623] = _state[396] ^ (last >> 1) ^ ((last & 1u) == 0 ? 0u : 0x9908b0dfu);
            _index = 0;
        }
    }
}
