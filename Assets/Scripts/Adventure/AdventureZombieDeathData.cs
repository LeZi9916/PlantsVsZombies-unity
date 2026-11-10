#nullable enable

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie death data.</summary>
    public static class AdventureZombieDeathData
    {
        /// <summary>Stores clips.</summary>
        private static readonly Dictionary<string, int> s_clips = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Zombie/anim_death",
                39
            },
            {
                "Zombie/anim_death2",
                33
            },
            {
                "Zombie/anim_superlongdeath",
                137
            },
            {
                "Zombie/anim_waterdeath",
                25
            },
            {
                "Zombie_Boss_driver/anim_death",
                86
            },
            {
                "Zombie_Jackson/anim_death",
                35
            },
            {
                "Zombie_backup/anim_death",
                44
            },
            {
                "Zombie_balloon/anim_death",
                28
            },
            {
                "Zombie_bobsled/anim_death",
                32
            },
            {
                "Zombie_boss/anim_death",
                107
            },
            {
                "Zombie_dancer/anim_death",
                35
            },
            {
                "Zombie_digger/anim_death",
                40
            },
            {
                "Zombie_disco/anim_death",
                44
            },
            {
                "Zombie_dolphinrider/anim_death",
                39
            },
            {
                "Zombie_football/anim_death",
                18
            },
            {
                "Zombie_gargantuar/anim_death",
                59
            },
            {
                "Zombie_imp/anim_death",
                22
            },
            {
                "Zombie_jackbox/anim_death",
                23
            },
            {
                "Zombie_ladder/anim_death",
                36
            },
            {
                "Zombie_paper/anim_death",
                36
            },
            {
                "Zombie_pogo/anim_death",
                39
            },
            {
                "Zombie_polevaulter/anim_death",
                27
            },
            {
                "Zombie_snorkle/anim_aquarium_death",
                37
            },
            {
                "Zombie_snorkle/anim_death",
                35
            },
            {
                "Zombie_yeti/anim_death",
                27
            },
        };
        /// <summary>Performs try get.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "count">The count.</param>
        /// <returns>The resulting try get.</returns>
        public static bool TryGet(string asset, string clip, out int count)
        {
            return s_clips.TryGetValue(asset + "/" + clip, out count);
        }
    }
}
