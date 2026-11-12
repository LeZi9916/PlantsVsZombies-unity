#nullable enable
using System;

namespace PlantsVsZombies.Levels
{
    /// <summary>
    /// The statically verified first-play, adventure-mode level 1 facts for the
    /// executable identified in analysis/ida/level_1_1/REPORT.md.
    /// This is not a complete gameplay simulation.
    /// </summary>
    public static class LevelOneData
    {
        /// <summary>Stores columns.</summary>
        public const int Columns = 9;
        /// <summary>Stores native row slots.</summary>
        public const int NativeRowSlots = 6;
        /// <summary>Stores grass row.</summary>
        public const int GrassRow = 2;
        /// <summary>Stores starting sun.</summary>
        public const int StartingSun = 150;
        /// <summary>Stores seed packet count.</summary>
        public const int SeedPacketCount = 1;
        /// <summary>Stores peashooter seed id.</summary>
        public const int PeashooterSeedId = 0;
        /// <summary>Stores peashooter cost.</summary>
        public const int PeashooterCost = 100;
        /// <summary>Stores peashooter recharge native ticks.</summary>
        public const int PeashooterRechargeNativeTicks = 750;
        /// <summary>Stores normal zombie id.</summary>
        public const int NormalZombieId = 0;
        /// <summary>Stores zombie spawn row.</summary>
        public const int ZombieSpawnRow = GrassRow;
        /// <summary>Stores wave count.</summary>
        public const int WaveCount = 4;
        /// <summary>Stores native reward item type.</summary>
        public const int NativeRewardItemType = 7;
        /// <summary>Rows and columns are zero-based native grid indices.</summary>
        public static bool IsGrassCell(int column, int row)
        {
            if (column < 0 || column >= Columns)
            {
                throw new ArgumentOutOfRangeException("column");
            }

            if (row < 0 || row >= NativeRowSlots)
            {
                throw new ArgumentOutOfRangeException("row");
            }

            return row == GrassRow;
        }

        /// <summary>A zero-based entry in the precomputed native wave list.</summary>
        public static int GetZombieCount(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= WaveCount)
            {
                throw new ArgumentOutOfRangeException("waveIndex");
            }

            return waveIndex == 3 ? 2 : 1;
        }

        /// <summary>Performs get zombie id.</summary>
        /// <param name = "waveIndex">The wave index.</param>
        /// <param name = "indexInWave">The index in wave.</param>
        /// <returns>The resulting get zombie id.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static int GetZombieId(int waveIndex, int indexInWave)
        {
            var count = GetZombieCount(waveIndex);
            if (indexInWave < 0 || indexInWave >= count)
            {
                throw new ArgumentOutOfRangeException("indexInWave");
            }

            return NormalZombieId;
        }

        /// <summary>Performs is flag wave.</summary>
        /// <param name = "waveIndex">The wave index.</param>
        /// <returns>The resulting is flag wave.</returns>
        public static bool IsFlagWave(int waveIndex)
        {
            GetZombieCount(waveIndex);
            return false;
        }
    }
}
