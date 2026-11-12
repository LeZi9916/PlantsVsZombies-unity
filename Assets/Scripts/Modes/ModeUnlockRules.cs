#nullable enable
using System;

namespace PlantsVsZombies.Modes
{
    /// <summary>Local PC GOTY 0x4397F0: native directory trophy requirements. Full version; no trial lock fabricated.</summary>
    public static class ModeUnlockRules
    {
        /// <summary>Performs beaten.</summary>
        /// <param name = "gameModeId">The game mode id.</param>
        /// <param name = "record">The record.</param>
        /// <returns>The resulting beaten.</returns>
        private static bool Beaten(int gameModeId, Func<int, int> record)
        {
            var value = record(gameModeId - 1);
            if (gameModeId >= 1 && gameModeId <= 5)
            {
                return value >= 5;
            }

            if (gameModeId >= 6 && gameModeId <= 10)
            {
                return value >= 10;
            }

            if (gameModeId >= 11 && gameModeId <= 15 || gameModeId == 60 || gameModeId == 70)
            {
                return false;
            }

            return value > 0;
        }

        /// <summary>Performs category unlocked.</summary>
        /// <param name = "gameModeId">The game mode id.</param>
        /// <param name = "nextAdventureLevel">The next adventure level.</param>
        /// <param name = "finishedAdventure">The finished adventure.</param>
        /// <returns>The resulting category unlocked.</returns>
        public static bool CategoryUnlocked(int gameModeId, int nextAdventureLevel, bool finishedAdventure)
        {
            if (finishedAdventure)
            {
                return true;
            }

            // PC 0x4289C0 gifts in levels22/36, and selector +0x34C/+0x350/+0x364 gates.
            if (gameModeId >= 1 && gameModeId <= 15)
            {
                return false;
            }

            if (gameModeId >= 16 && gameModeId <= 35)
            {
                return nextAdventureLevel >= 23;
            }

            if (gameModeId >= 51 && gameModeId <= 70)
            {
                return nextAdventureLevel >= 37;
            }

            return true;
        }

        /// <summary>Performs more trophies needed.</summary>
        /// <param name = "gameModeId">The game mode id.</param>
        /// <param name = "finishedAdventure">The finished adventure.</param>
        /// <param name = "record">The record.</param>
        /// <returns>The resulting more trophies needed.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static int MoreTrophiesNeeded(int gameModeId, bool finishedAdventure, Func<int, int> record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            ModeCatalog.Get(gameModeId);
            if (gameModeId >= 51 && gameModeId <= 70)
            {
                int first = gameModeId <= 60 ? 51 : 61, completed = 0;
                for (var id = first; id < first + 9; id++)
                {
                    if (Beaten(id, record))
                    {
                        completed++;
                    }
                }

                if (gameModeId >= first + 3 && !finishedAdventure && completed >= 3)
                {
                    return gameModeId == first + 3 ? 1 : 2;
                }

                return Math.Max(0, Math.Min(9, gameModeId - first - completed));
            }

            var survival = gameModeId <= 15;
            var position = survival ? gameModeId - 1 : gameModeId - 16;
            if (!finishedAdventure)
            {
                return position < 3 ? 0 : position == 3 ? 1 : 2;
            }

            var trophies = 0;
            for (var id = survival ? 1 : 16; id <= (survival ? 10 : 35); id++)
            {
                if (Beaten(id, record))
                {
                    trophies++;
                }
            }

            if (survival && gameModeId >= 11)
            {
                return Math.Max(0, 10 - trophies);
            }

            return Math.Max(0, position - (trophies + 3) + 1);
        }

        /// <summary>Performs lock reason.</summary>
        /// <param name = "gameModeId">The game mode id.</param>
        /// <param name = "finishedAdventure">The finished adventure.</param>
        /// <param name = "record">The record.</param>
        /// <returns>The resulting lock reason.</returns>
        public static string? LockReason(int gameModeId, bool finishedAdventure, Func<int, int> record)
        {
            var needed = MoreTrophiesNeeded(gameModeId, finishedAdventure, record);
            if (needed <= 0)
            {
                return null;
            }

            if (!finishedAdventure)
            {
                if (gameModeId >= 51 && gameModeId <= 70)
                {
                    int first = gameModeId <= 60 ? 51 : 61, count = 0;
                    for (var id = first; id < first + 9; id++)
                    {
                        if (Beaten(id, record))
                        {
                            count++;
                        }
                    }

                    if (gameModeId >= first + 3 && count >= 3)
                    {
                        return "先完成冒险模式，才能继续后续解谜挑战。";
                    }

                    return "先完成本组前面的解谜关卡（还需 " + needed + " 个奖杯）。";
                }

                return "先完成冒险模式，才能开放更多" + (gameModeId <= 15 ? "生存挑战。" : "小游戏。");
            }

            return gameModeId >= 11 && gameModeId <= 15 ? "集齐生存普通和困难的10个奖杯（还差 " + needed + " 个）。" : "在本组再获得 " + needed + " 个奖杯即可解锁。";
        }
    }
}
