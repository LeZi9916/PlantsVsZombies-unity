#nullable enable
using System;

namespace PlantsVsZombies.Randomness
{
    /// <summary>Defines original random state.</summary>
    [Serializable]
    public sealed class OriginalRandomState
    {
        /// <summary>Stores words.</summary>
        private readonly uint[] _words;
        /// <summary>Gets or sets index.</summary>
        public int Index { get; private set; }
        /// <summary>Gets or sets draw count.</summary>
        public ulong DrawCount { get; private set; }

        /// <summary>Initializes original random state.</summary>
        /// <param name = "words">The words.</param>
        /// <param name = "index">The index.</param>
        /// <param name = "drawCount">The draw count.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public OriginalRandomState(uint[] words, int index, ulong drawCount)
        {
            if (words == null || words.Length != 624 || index < 0 || index > 624)
            {
                throw new ArgumentException("An MT checkpoint requires 624 words and an index in [0,624].");
            }

            this._words = (uint[])words.Clone();
            Index = index;
            DrawCount = drawCount;
        }

        /// <summary>Performs copy words.</summary>
        /// <returns>The resulting copy words.</returns>
        public uint[] CopyWords()
        {
            return (uint[])_words.Clone();
        }
    }
}
