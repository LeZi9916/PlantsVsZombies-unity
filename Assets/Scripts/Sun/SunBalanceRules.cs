#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun balance rules.</summary>
    public static class SunBalanceRules
    {
        // Predicate arguments are the results of the corresponding EXE functions,
        // not assumptions about their names or mode semantics.
        /// <summary>Performs initial balance.</summary>
        /// <param name = "mode">The mode.</param>
        /// <param name = "playerLevel">The player level.</param>
        /// <param name = "boardLevel">The board level.</param>
        /// <param name = "predicate460C80">The predicate460 c80.</param>
        /// <param name = "predicate460BC0">The predicate460 bc0.</param>
        /// <param name = "predicate461580">The predicate461580.</param>
        /// <returns>The resulting initial balance.</returns>
        public static int InitialBalance(int mode, int playerLevel, int boardLevel, bool predicate460C80, bool predicate460BC0, bool predicate461580)
        {
            if (mode == 20 || mode == 24 || (mode >= 51 && mode <= 60) || (mode == 0 && playerLevel == 35) || predicate460C80)
            {
                return 0;
            }

            if (mode == 31)
            {
                return 5000;
            }

            if (predicate460BC0 || (predicate461580 && boardLevel == 1))
            {
                return 150;
            }

            return 50;
        }
    }
}
