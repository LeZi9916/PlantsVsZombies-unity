using System;

namespace PvZReplica.Randomness
{
    /// <summary>
    /// Routes existing System.Random consumers through the local executable's 31-bit MT stream.
    /// Integer bounds use native remainder selection. Floating draws use the local 0x5E7060 scale-1 float result.
    /// Other original subsystems' initialization and draw order remain outside this adapter.
    /// </summary>
    public sealed class OriginalRandomAdapter : System.Random
    {
        public OriginalRandom Generator { get; private set; }
        public uint Seed { get; private set; }
        public OriginalRandomAdapter(uint seed) : base(0)
        { Seed = seed == 0 ? OriginalRandom.DefaultSeed : seed; Generator = new OriginalRandom(seed); }
        public override int Next() { return (int)Generator.Next31(); }
        public override int Next(int maxValue)
        {
            if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
            return maxValue == 0 ? 0 : (int)Generator.NextBounded((uint)maxValue);
        }
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
            ulong span = (ulong)((long)maxValue - minValue);
            if (span == 0) return minValue;
            ulong draw = span <= 0x80000000UL ? Generator.Next31() :
                ((ulong)Generator.Next31() << 1) | (Generator.Next31() & 1u);
            return (int)(minValue + (long)(draw % span));
        }
        protected override double Sample() { return (double)(float)(Generator.Next31() / 2147483647.0); }
        public override double NextDouble() { return Sample(); }
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)Generator.NextBounded(256);
        }
        public OriginalRandomState CaptureState() { return Generator.CaptureState(); }
        public void RestoreState(OriginalRandomState snapshot) { Generator.RestoreState(snapshot); }
    }
}
