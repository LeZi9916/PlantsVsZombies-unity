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
            // PC Foley table 0x74B3B8, stride 52; names resolved by resource initialization 0x4935B0.
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
                new AdventureZombieFoleyDefinition(0.0f, new[] { "dancer" })
            },
            {
                79,
                new AdventureZombieFoleyDefinition(10.0f, new[] { "gravestone_rumble" })
            },
        };
    }
}
