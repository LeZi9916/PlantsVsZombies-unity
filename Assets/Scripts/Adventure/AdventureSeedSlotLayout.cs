#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed slot layout.</summary>
    public static class AdventureSeedSlotLayout
    {
        // PC Board::GetSeedPacketPositionX 0x427E20 and SeedBank::RefreshAll 0x49F720.
        /// <summary>Performs position x.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "count">The configured bank capacity, including slots that have no selected card.</param>
        /// <returns>The resulting position x.</returns>
        public static int PositionX(int index, int count)
        {
            return count < 8 ? 85 + 59 * index : count == 8 ? 81 + 54 * index : count == 9 ? 80 + 52 * index : 79 + 51 * index;
        }

        /// <summary>Performs background extra.</summary>
        /// <param name = "count">The count.</param>
        /// <returns>The resulting background extra.</returns>
        public static int BackgroundExtra(int count)
        {
            return count <= 6 ? 0 : count == 7 ? 60 : count == 8 ? 76 : count == 9 ? 112 : 153;
        }
    }
}
