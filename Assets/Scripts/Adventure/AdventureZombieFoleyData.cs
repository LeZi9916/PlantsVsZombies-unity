#nullable enable
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares original PC Foley samples and pitch ranges used by plants and zombies.</summary>
    public static class AdventureZombieFoleyData
    {
        /// <summary>Stores definitions.</summary>
        public static readonly Dictionary<int, AdventureZombieFoleyDefinition> Definitions = new Dictionary<int, AdventureZombieFoleyDefinition>
        {
            {
                85,
                new AdventureZombieFoleyDefinition(0f, new[] { "scream" })
            },
            {
                44,
                new AdventureZombieFoleyDefinition(2f, new[] { "bungee_scream", "bungee_scream2", "bungee_scream3" })
            },
            // PC storm Foley: Thunder has ten semitones of pitch variation; Rain loops once per board.
            {
                17,
                new AdventureZombieFoleyDefinition(10f, new[] { "thunder" })
            },
            {
                49,
                new AdventureZombieFoleyDefinition(0f, new[] { "rain" }, 5)
            },
            // PC 74B3B8 + id * 52: six normal groans, three extra brains variants, and two low giant groans.
            {
                11,
                new AdventureZombieFoleyDefinition(0f, new[] { "groan", "groan2", "groan3", "groan4", "groan5", "groan6" })
            },
            {
                12,
                new AdventureZombieFoleyDefinition(0f, new[] { "groan", "groan2", "groan3", "groan4", "groan5", "groan6", "sukhbir4", "sukhbir5", "sukhbir6" })
            },
            {
                70,
                new AdventureZombieFoleyDefinition(2f, new[] { "lowgroan", "lowgroan2" })
            },
            {
                34,
                new AdventureZombieFoleyDefinition(1f, new[] { "jack_surprise", "jack_surprise", "jack_surprise2" })
            },
            {
                60,
                new AdventureZombieFoleyDefinition(10f, new[] { "ballooninflate" })
            },
            {
                37,
                new AdventureZombieFoleyDefinition(10f, new[] { "basketball" })
            },
            // PC Foley table 0x74B3B8, stride 52; names resolved by resource initialization 0x4935B0.
            {
                16,
                new AdventureZombieFoleyDefinition(5f, new[] { "zamboni" })
            },
            {
                22,
                new AdventureZombieFoleyDefinition(0f, new[] { "balloon_pop" })
            },
            {
                23,
                new AdventureZombieFoleyDefinition(0f, new[] { "explosion" })
            },
            {
                7,
                new AdventureZombieFoleyDefinition(0f, new[] { "plant", "plant2" })
            },
            {
                19,
                new AdventureZombieFoleyDefinition(10f, new[] { "plant_water", "zombie_entering_water" })
            },
            {
                88,
                new AdventureZombieFoleyDefinition(5f, new[] { "imp", "imp2" })
            },
            {
                21,
                new AdventureZombieFoleyDefinition(0f, new[] { "chomp", "chomp2" })
            },
            // PC vase opening uses Swing Foley, then direct BONK and the downward-pitched VaseBreaking Foley.
            {
                47,
                new AdventureZombieFoleyDefinition(2f, new[] { "swing" })
            },
            {
                35,
                new AdventureZombieFoleyDefinition(-5f, new[] { "vase_breaking" })
            },
            {
                48,
                new AdventureZombieFoleyDefinition(2f, new[] { "bonk" })
            },
            {
                50,
                new AdventureZombieFoleyDefinition(0f, new[] { "dolphin_before_jumping" })
            },
            {
                51,
                new AdventureZombieFoleyDefinition(0f, new[] { "dolphin_appears" })
            },
            {
                52,
                new AdventureZombieFoleyDefinition(0f, new[] { "plant_water" })
            },
            {
                74,
                new AdventureZombieFoleyDefinition(2f, new[] { "grassstep" })
            },
            {
                78,
                new AdventureZombieFoleyDefinition(5f, new[] { "polevault" })
            },
            {
                4,
                new AdventureZombieFoleyDefinition(10f, new[] { "throw" })
            },
            {
                29,
                new AdventureZombieFoleyDefinition(10f, new[] { "puff" })
            },
            {
                30,
                new AdventureZombieFoleyDefinition(10f, new[] { "fume" })
            },
            {
                31,
                new AdventureZombieFoleyDefinition(10f, new[] { "coin" })
            },
            {
                41,
                new AdventureZombieFoleyDefinition(2f, new[] { "squash_hmm", "squash_hmm", "squash_hmm2" })
            },
            {
                42,
                new AdventureZombieFoleyDefinition(2f, new[] { "magnetshroom" })
            },
            {
                53,
                new AdventureZombieFoleyDefinition(0f, new[] { "zombie_entering_water" })
            },
            {
                57,
                new AdventureZombieFoleyDefinition(0f, new[] { "reverse_explosion" })
            },
            {
                63,
                new AdventureZombieFoleyDefinition(-2f, new[] { "plantgrow" })
            },
            {
                64,
                new AdventureZombieFoleyDefinition(-5f, new[] { "shoop" })
            },
            {
                65,
                new AdventureZombieFoleyDefinition(2f, new[] { "juicy" })
            },
            {
                69,
                new AdventureZombieFoleyDefinition(0f, new[] { "coffee" })
            },
            {
                73,
                new AdventureZombieFoleyDefinition(2f, new[] { "throw2" })
            },
            {
                76,
                new AdventureZombieFoleyDefinition(10f, new[] { "coblaunch" })
            },
            // PC Foley 26 at 74B3B8 + 26 * 52: the pogo landing sample varies over four semitones.
            {
                26,
                new AdventureZombieFoleyDefinition(4f, new[] { "pogo_zombie" })
            },
            {
                39,
                new AdventureZombieFoleyDefinition(10f, new[] { "firepea" })
            },
            // PC Foley 80 at 74B3B8 + 80 * 52: rising dirt has a five-semitone range.
            {
                80,
                new AdventureZombieFoleyDefinition(5f, new[] { "dirt_rise" })
            },
            {
                83,
                new AdventureZombieFoleyDefinition(0f, new[] { "wakeup" })
            },
            {
                3,
                new AdventureZombieFoleyDefinition(10f, new[] {
                            "throw",
                            "throw",
                            "throw",
                            "throw2"
                        })
            },
            {
                18,
                new AdventureZombieFoleyDefinition(0f, new[] { "frozen" })
            },
            {
                27,
                new AdventureZombieFoleyDefinition(10f, new[] { "snow_pea_sparkles" })
            },
            {
                20,
                new AdventureZombieFoleyDefinition(-3f, new[] { "bowlingimpact" })
            },
            {
                5,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "chomp", "chomp2" })
            },
            {
                6,
                new AdventureZombieFoleyDefinition(4.0f, new[] { "chompsoft" })
            },
            {
                68,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "floop" })
            },
            {
                72,
                new AdventureZombieFoleyDefinition(1.0f, new[] { "yuck", "yuck", "yuck2" })
            },
            {
                1,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "splat", "splat2", "splat3" })
            },
            {
                25,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "limbs_pop" })
            },
            {
                28,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "zombie_falling_1", "zombie_falling_2" })
            },
            {
                32,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "kernelpult", "kernelpult2" })
            },
            {
                38,
                new AdventureZombieFoleyDefinition(5.0f, new[] {
                            "ignite",
                            "ignite",
                            "ignite",
                            "ignite2"
                        })
            },
            {
                40,
                new AdventureZombieFoleyDefinition(2.0f, new[] { "gargantuar_thump" })
            },
            {
                43,
                new AdventureZombieFoleyDefinition(2.0f, new[] { "butter" })
            },
            {
                46,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "shieldhit", "shieldhit2" })
            },
            {
                58,
                new AdventureZombieFoleyDefinition(5.0f, new[] { "plastichit", "plastichit2" })
            },
            {
                61,
                new AdventureZombieFoleyDefinition(-2.0f, new[] { "bigchomp" })
            },
            {
                62,
                new AdventureZombieFoleyDefinition(-5.0f, new[] { "melonimpact", "melonimpact2" })
            },
            {
                66,
                new AdventureZombieFoleyDefinition(-2.0f, new[] { "newspaper_rarrgh", "newspaper_rarrgh2", "newspaper_rarrgh2" })
            },
            {
                67,
                new AdventureZombieFoleyDefinition(-2.0f, new[] { "newspaper_rip" })
            },
            {
                100,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "dancer" }, 6)
            },
            {
                79,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "gravestone_rumble" })
            },
            // Remaining PC 74B3B8 rows, including the original boss hydraulic and driver samples.
            {
                0,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "points" }, 0)
            },
            {
                2,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "lawnmower" }, 0)
            },
            {
                8,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "plant2" }, 0)
            },
            {
                9,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "tap2" }, 0)
            },
            {
                10,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "bleep" }, 0)
            },
            {
                13,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "groan", "groan2", "groan3", "groan4", "groan5", "groan6", "sukhbir", "sukhbir2", "sukhbir3" }, 0)
            },
            {
                14,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "jackinthebox" }, 7)
            },
            {
                15,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "diamond" }, 0)
            },
            {
                24,
                new AdventureZombieFoleyDefinition(2.0f, new[] { "slurp" }, 0)
            },
            {
                33,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "digger_zombie" }, 7)
            },
            {
                36,
                new AdventureZombieFoleyDefinition(2.0f, new[] { "pool_cleaner" }, 0)
            },
            {
                45,
                new AdventureZombieFoleyDefinition(2.0f, new[] { "explosion" }, 0)
            },
            {
                54,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "gravebusterchomp" }, 4)
            },
            {
                55,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "cherrybomb" }, 0)
            },
            {
                56,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "jalapeno" }, 0)
            },
            {
                59,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "winmusic" }, 8)
            },
            {
                71,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "prize" }, 0)
            },
            {
                75,
                new AdventureZombieFoleyDefinition(5.0f, new[] { "shovel" }, 0)
            },
            {
                77,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "watering" }, 0)
            },
            {
                81,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "fertilizer" }, 0)
            },
            {
                82,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "portal" }, 0)
            },
            {
                84,
                new AdventureZombieFoleyDefinition(3.0f, new[] { "bugspray" }, 0)
            },
            {
                86,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "paper" }, 0)
            },
            {
                87,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "moneyfalls" }, 0)
            },
            {
                89,
                new AdventureZombieFoleyDefinition(3.0f, new[] { "hydraulic_short" }, 0)
            },
            {
                90,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "hydraulic" }, 0)
            },
            {
                91,
                new AdventureZombieFoleyDefinition(3.0f, new[] { "gargantudeath" }, 0)
            },
            {
                92,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "ceramic" }, 0)
            },
            {
                93,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "bossboulderattack" }, 0)
            },
            {
                94,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "chime" }, 0)
            },
            {
                95,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydaveshort1", "crazydaveshort2", "crazydaveshort3" }, 16)
            },
            {
                96,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydavelong1", "crazydavelong2", "crazydavelong3" }, 16)
            },
            {
                97,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydaveextralong1", "crazydaveextralong2", "crazydaveextralong3" }, 16)
            },
            {
                98,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydavecrazy" }, 0)
            },
            {
                99,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "phonograph" }, 0)
            },
            {
                101,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "finalfanfare" }, 0)
            },
            {
                102,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydavescream" }, 0)
            },
            {
                103,
                new AdventureZombieFoleyDefinition(0.0f, new[] { "crazydavescream2" }, 0)
            }
        };
    }
}
