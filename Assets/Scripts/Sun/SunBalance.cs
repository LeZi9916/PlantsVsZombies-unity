namespace PvZUnity.Sun
{
    public static class SunBalanceRules
    {
        // Predicate arguments are the results of the corresponding EXE functions,
        // not assumptions about their names or mode semantics.
        public static int InitialBalance(int mode, int playerLevel, int boardLevel,
            bool predicate460C80, bool predicate460BC0, bool predicate461580)
        {
            if (mode == 20 || mode == 24 || (mode >= 51 && mode <= 60)
                || (mode == 0 && playerLevel == 35) || predicate460C80)
                return 0;
            if (mode == 31) return 5000;
            if (predicate460BC0 || (predicate461580 && boardLevel == 1)) return 150;
            return 50;
        }
    }
}
