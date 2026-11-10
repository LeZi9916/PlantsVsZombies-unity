#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>PC 0x41FCD0, not a generic last-array-row announcement for continuous modes.</summary>
    public static class AdventureWaveNoticeRules
    {
        /// <summary>Stores huge duration.</summary>
        public const int HugeDuration = 750;
        /// <summary>Stores final sound delay.</summary>
        public const int FinalSoundDelay = 60;
        /// <summary>Stores baseline.</summary>
        public const int Baseline = 330; // local Chinese GOTY 0x467BB0 (reference mobile port uses290).
        /// <summary>Stores font.</summary>
        public const string Font = "HouseofTerror28"; // 0x753240 <- FONT_HOUSEOFTERROR28 at0x48B4F8.
        /// <summary>Performs announce final.</summary>
        /// <param name = "nativeMode">The native mode.</param>
        /// <param name = "finalSurvivalStage">The final survival stage.</param>
        /// <param name = "adventureBoss">The adventure boss.</param>
        /// <returns>The resulting announce final.</returns>
        public static bool AnnounceFinal(int nativeMode, bool finalSurvivalStage, bool adventureBoss)
        {
            if (nativeMode >= 1 && nativeMode <= 15 && !finalSurvivalStage)
            {
                return false;
            }

            if (nativeMode == 31 || adventureBoss)
            {
                return false;
            }

            return nativeMode != 36 && nativeMode != 40 && nativeMode != 22 && nativeMode != 18 && nativeMode != 35 && nativeMode != 20 && nativeMode != 71 && nativeMode != 72 && nativeMode != 24;
        }
    }
}
