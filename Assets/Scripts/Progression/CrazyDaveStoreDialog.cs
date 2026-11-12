#nullable enable

namespace PlantsVsZombies.Progression
{
    /// <summary>Maps original store items to Crazy Dave's item descriptions.</summary>
    public static class CrazyDaveStoreDialog
    {
        /// <summary>Selects the original store introduction after an adventure reward screen.</summary>
        /// <param name="completedLevel">The one-based level whose reward was collected.</param>
        /// <param name="isReplay">Whether this is a later adventure run.</param>
        /// <returns>The first original dialogue index, or -1 when no interlude occurs.</returns>
        public static int AdventureRewardIntroduction(int completedLevel, bool isReplay)
        {
            if (isReplay)
            {
                return -1;
            }

            return completedLevel == 24 ? 301 : completedLevel == 34 ? 601 : completedLevel == 41 ? 3100 : -1;
        }

        /// <summary>Checks the original full-version availability rule for item descriptions.</summary>
        /// <param name="itemId">The original store item identifier.</param>
        /// <param name="level">The player's current adventure level.</param>
        /// <param name="adventureCompleted">Whether adventure has been completed.</param>
        /// <returns>Whether the item can receive hover descriptions.</returns>
        public static bool CanHover(int itemId, int level, bool adventureCompleted)
        {
            if (adventureCompleted)
            {
                return true;
            }

            if (itemId == 23)
            {
                return level >= 42;
            }

            if (itemId == 2 || itemId == 3)
            {
                return level >= 35;
            }

            if (itemId == 5 || itemId == 6)
            {
                return level >= 41;
            }

            return itemId != 4 && itemId != 7 && itemId != 8 && itemId != 29;
        }

        /// <summary>Gets the original message index for a hovered store item.</summary>
        /// <param name="itemId">The original store item identifier.</param>
        /// <param name="packetUpgradeCount">The number of packet upgrades already purchased.</param>
        /// <returns>The original message index, or -1 if the item has no description.</returns>
        public static int GetHoverMessageIndex(int itemId, int packetUpgradeCount)
        {
            if (itemId >= 0 && itemId <= 9)
            {
                return 2000 + itemId;
            }

            if (itemId >= 10 && itemId <= 12)
            {
                return 2010;
            }

            switch (itemId)
            {
                case 13:
                    return 2019;
                case 14:
                    return 2020;
                case 15:
                    return 2022;
                case 16:
                    return 2021;
                case 17:
                    return 2023;
                case 18:
                    return 2032;
                case 19:
                    return 2024;
                case 20:
                    return 2025;
                case 21:
                    return 2011 + packetUpgradeCount;
                case 22:
                    return 2026;
                case 23:
                    return 2027;
                case 24:
                    return 2028;
                case 25:
                    return 2029;
                case 27:
                    return 2030;
                case 28:
                    return 2031;
                case 29:
                    return 2033;
                default:
                    return -1;
            }
        }
    }
}
