#nullable enable

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure levels.</summary>
    public static class AdventureLevels
    {
        /// <summary>Stores no belt.</summary>
        private static readonly int[] s_noBelt = new int[0];
        /// <summary>Stores levels.</summary>
        private static readonly AdventureLevelSpec[] s_levels = Create();
        /// <summary>Gets or sets all.</summary>
        public static IReadOnlyList<AdventureLevelSpec> All
        {
            get
            {
                return Array.AsReadOnly(s_levels);
            }
        }

        /// <summary>Performs get.</summary>
        /// <param name = "level">The level.</param>
        /// <returns>The resulting get.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureLevelSpec Get(int level)
        {
            if (level < 1 || level > 50)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            return s_levels[level - 1];
        }

        /// <summary>Performs for run.</summary>
        /// <param name = "level">The level.</param>
        /// <param name = "cycle">The cycle.</param>
        /// <param name = "slots">The slots.</param>
        /// <returns>The resulting for run.</returns>
        public static AdventureLevelSpec ForRun(int level, int cycle, int slots = 6)
        {
            return Get(level).ForRun(cycle, slots);
        }

        /// <summary>Performs l.</summary>
        /// <param name = "n">The n.</param>
        /// <param name = "t">The t.</param>
        /// <param name = "m">The m.</param>
        /// <param name = "seeds">The seeds.</param>
        /// <param name = "belt">The belt.</param>
        /// <returns>The resulting l.</returns>
        private static AdventureLevelSpec L(int n, AdventureTerrain t, AdventureMode m, int[] seeds, int[]? belt = null)
        {
            var reward = n == 4 ? AdventureRewardKind.Shovel : n == 14 ? AdventureRewardKind.Almanac : n == 24 ? AdventureRewardKind.CarKeys : n == 34 ? AdventureRewardKind.Taco : n == 44 ? AdventureRewardKind.WateringCan : n == 50 ? AdventureRewardKind.SilverSunflower : n % 10 == 9 ? AdventureRewardKind.Note : AdventureRewardKind.Seed;
            return new AdventureLevelSpec(n, t, m, AdventureLevelSpec.AwardSeedId(n), reward, seeds, belt ?? s_noBelt);
        }

        /// <summary>Performs create.</summary>
        /// <returns>The resulting create.</returns>
        private static AdventureLevelSpec[] Create()
        {
            // Each explicit row chooses its actual ruleset and useful first-play loadout; wave counts are read from verified EXE data.
            const AdventureTerrain d = AdventureTerrain.Day, n = AdventureTerrain.Night, p = AdventureTerrain.Pool, f = AdventureTerrain.Fog, r = AdventureTerrain.Roof, b = AdventureTerrain.BossRoof;
            const AdventureMode lw = AdventureMode.Lawn, c = AdventureMode.Conveyor;
            return new[]
            {
                L(1, d, lw, new[] { 0 }),
                L(2, d, lw, new[] { 0, 1 }),
                L(3, d, lw, new[] { 0, 1, 2 }),
                L(4, d, lw, new[] {
                        0,
                        1,
                        2,
                        3
                    }),
                L(5, d, AdventureMode.Bowling, new int[0], new[] {
                        3,
                        85,
                        49,
                        15
                    }),
                L(6, d, lw, new[] {
                        0,
                        1,
                        2,
                        3,
                        4
                    }),
                L(7, d, lw, new[] {
                        0,
                        1,
                        2,
                        3,
                        4,
                        5
                    }),
                L(8, d, lw, new[] {
                        1,
                        0,
                        5,
                        6,
                        3,
                        4
                    }),
                L(9, d, lw, new[] {
                        1,
                        7,
                        5,
                        2,
                        3,
                        4
                    }),
                L(10, d, c, new int[0], new[] {
                        0,
                        20,
                        2,
                        20,
                        3,
                        15,
                        7,
                        20,
                        5,
                        10,
                        6,
                        5,
                        4,
                        10
                    }),
                L(11, n, lw, new[] {
                        8,
                        1,
                        0,
                        7,
                        3,
                        2
                    }),
                L(12, n, lw, new[] {
                        8,
                        9,
                        7,
                        3,
                        4,
                        2
                    }),
                L(13, n, lw, new[] {
                        8,
                        9,
                        10,
                        7,
                        3,
                        2
                    }),
                L(14, n, lw, new[] {
                        8,
                        9,
                        10,
                        11,
                        3,
                        2
                    }),
                L(15, n, AdventureMode.WhackAZombie, new int[0]),
                L(16, n, lw, new[] {
                        8,
                        9,
                        10,
                        12,
                        11,
                        3
                    }),
                L(17, n, lw, new[] {
                        8,
                        9,
                        10,
                        13,
                        11,
                        3
                    }),
                L(18, n, lw, new[] {
                        8,
                        9,
                        10,
                        14,
                        11,
                        3
                    }),
                L(19, n, lw, new[] {
                        8,
                        9,
                        10,
                        15,
                        11,
                        3
                    }),
                L(20, n, c, new int[0], new[] {
                        11,
                        20,
                        14,
                        15,
                        15,
                        15,
                        12,
                        10,
                        13,
                        15,
                        10,
                        15,
                        8,
                        10
                    }),
                L(21, p, lw, new[] {
                        1,
                        16,
                        7,
                        5,
                        3,
                        2
                    }),
                L(22, p, lw, new[] {
                        1,
                        16,
                        7,
                        17,
                        3,
                        2
                    }),
                L(23, p, lw, new[] {
                        1,
                        16,
                        18,
                        5,
                        3,
                        2
                    }),
                L(24, p, lw, new[] {
                        1,
                        16,
                        18,
                        19,
                        3,
                        2
                    }),
                L(25, p, AdventureMode.LittleTrouble, new int[0], new[] {
                        16,
                        25,
                        3,
                        15,
                        0,
                        25,
                        2,
                        35
                    }),
                L(26, p, lw, new[] {
                        1,
                        16,
                        18,
                        20,
                        3,
                        19
                    }),
                L(27, p, lw, new[] {
                        1,
                        16,
                        18,
                        21,
                        20,
                        19
                    }),
                L(28, p, lw, new[] {
                        1,
                        16,
                        7,
                        22,
                        20,
                        19
                    }),
                L(29, p, lw, new[] {
                        1,
                        16,
                        18,
                        23,
                        20,
                        19
                    }),
                L(30, p, c, new int[0], new[] {
                        16,
                        25,
                        17,
                        5,
                        18,
                        25,
                        19,
                        5,
                        20,
                        10,
                        21,
                        10,
                        22,
                        10,
                        23,
                        10
                    }),
                L(31, f, lw, new[] {
                        9,
                        16,
                        24,
                        10,
                        18,
                        19
                    }),
                L(32, f, lw, new[] {
                        9,
                        16,
                        24,
                        25,
                        18,
                        19
                    }),
                L(33, f, lw, new[] {
                        9,
                        16,
                        26,
                        25,
                        10,
                        19
                    }),
                L(34, f, lw, new[] {
                        9,
                        16,
                        26,
                        27,
                        10,
                        19
                    }),
                L(35, n, AdventureMode.Vasebreaker, new int[0]),
                L(36, f, lw, new[] {
                        9,
                        16,
                        26,
                        28,
                        10,
                        19
                    }),
                L(37, f, lw, new[] {
                        9,
                        16,
                        26,
                        29,
                        10,
                        19
                    }),
                L(38, f, lw, new[] {
                        9,
                        16,
                        26,
                        29,
                        30,
                        19
                    }),
                L(39, f, lw, new[] {
                        9,
                        16,
                        26,
                        31,
                        30,
                        19
                    }),
                L(40, f, AdventureMode.Storm, new int[0], new[] {
                        16,
                        25,
                        24,
                        10,
                        31,
                        5,
                        27,
                        5,
                        26,
                        15,
                        29,
                        25,
                        28,
                        5,
                        30,
                        10
                    }),
                L(41, r, lw, new[] {
                        1,
                        32,
                        2,
                        3,
                        20,
                        4
                    }),
                L(42, r, lw, new[] {
                        1,
                        33,
                        32,
                        2,
                        3,
                        20
                    }),
                L(43, r, lw, new[] {
                        1,
                        33,
                        34,
                        32,
                        2,
                        3
                    }),
                L(44, r, lw, new[] {
                        1,
                        33,
                        34,
                        35,
                        31,
                        2
                    }),
                L(45, r, AdventureMode.Bungee, new int[0], new[] {
                        33,
                        50,
                        6,
                        25,
                        30,
                        15,
                        2,
                        10
                    }),
                L(46, r, lw, new[] {
                        1,
                        33,
                        32,
                        36,
                        2,
                        20
                    }),
                L(47, r, lw, new[] {
                        1,
                        33,
                        34,
                        37,
                        2,
                        20
                    }),
                L(48, r, lw, new[] {
                        1,
                        33,
                        32,
                        37,
                        38,
                        20
                    }),
                L(49, r, lw, new[] {
                        1,
                        33,
                        39,
                        37,
                        2,
                        20
                    }),
                L(50, b, AdventureMode.Boss, new int[0], new[] {
                        33,
                        55,
                        39,
                        10,
                        20,
                        12,
                        32,
                        10,
                        34,
                        5,
                        14,
                        8
                    })
            };
        }
    }
}
