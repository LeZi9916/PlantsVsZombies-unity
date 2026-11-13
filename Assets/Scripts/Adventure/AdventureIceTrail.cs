#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC board's single continuous ice edge and melting countdown for one lawn row.</summary>
    public sealed class AdventureIceTrail
    {
        /// <summary>Identifies the original particle instance and its lawn row.</summary>
        public int Id, Row;
        /// <summary>Stores the left edge in board pixels; the untouched PC row starts at eight hundred.</summary>
        public int MinX = 800;
        /// <summary>Stores the original remaining ice updates and elapsed sparkle updates.</summary>
        public int RemainingTicks, AgeTicks;

        /// <summary>Gets the original integer alpha during the final ten updates of melting.</summary>
        public float Opacity => Math.Max(0, Math.Min(10, RemainingTicks)) * 255 / 10 / 255f;

        /// <summary>Extends and refreshes the row using PC UpdateZamboni (0x5464A0).</summary>
        /// <param name="nativeX">The vehicle's original unscaled board position.</param>
        /// <param name="roof">Whether the original roof restricts the left edge to five hundred pixels.</param>
        /// <param name="bobsledBonanza">Whether the challenge uses the original maximum countdown.</param>
        public void Extend(float nativeX, bool roof, bool bobsledBonanza)
        {
            var edge = Math.Max(roof ? 500 : 25, (int)(nativeX + 118f));
            MinX = Math.Min(MinX, edge);
            if (edge < 800)
            {
                RemainingTicks = bobsledBonanza ? int.MaxValue : 3000;
            }
        }

        /// <summary>Checks the PC IsIceAt (0x419CA0) left edge before accepting a planting cell.</summary>
        /// <param name="column">The zero-based lawn column being tested.</param>
        /// <returns>Whether the cell is covered by the surviving native ice row.</returns>
        public bool CoversColumn(int column)
        {
            var first = Math.Max(0, Math.Min(8, (MinX + 12 - 40) / 80));
            return RemainingTicks > 0 && MinX <= 750 && column >= first;
        }
    }
}
