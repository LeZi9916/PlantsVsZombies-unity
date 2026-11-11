#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie parts rules.</summary>
    public static class AdventureZombiePartsRules
    {
        /// <summary>Performs should lose head.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "flying">The flying.</param>
        /// <param name = "sled">The sled.</param>
        /// <param name = "hasHead">The has head.</param>
        /// <param name = "health">The health.</param>
        /// <param name = "maximum">The maximum.</param>
        /// <returns>The resulting should lose head.</returns>
        public static bool ShouldLoseHead(int kind, int height, bool flying, bool sled, bool hasHead, float health, int maximum)
        {
            return hasHead && health < maximum / 3 && AdventureZombieFeedbackRules.CanLoseParts(kind, height, flying, sled);
        }

        /// <summary>Performs head particle.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "mowed">The mowed.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <returns>The resulting head particle.</returns>
        public static int HeadParticle(int kind, bool mowed, bool inPool)
        {
            // IDs come from THIS PC EXE's 0x74C8D8 table, not the mobile ParticleEffect enum.
            if (kind == 5)
            {
                return 17;
            }

            if (kind == 18)
            {
                return 61;
            }

            if (kind == 16)
            {
                return 18;
            }

            return mowed ? 72 : inPool ? 74 : 6;
        }

        /// <summary>Performs particle name.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting particle name.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static string ParticleName(int id)
        {
            switch (id)
            {
                case 6:
                    return "ZombieHead";
                case 8:
                    return "ZombieTrafficCone";
                case 9:
                    return "ZombiePail";
                case 10:
                    return "ZombieHelmet";
                case 12:
                    return "ZombieDoor";
                case 13:
                    return "ZombieNewspaper";
                case 14:
                    return "ZombieHeadLight";
                case 54:
                    return "ZombieLadder";
                case 11:
                    return "ZombieFlag";
                case 17:
                    return "ZombieNewspaperHead";
                case 18:
                    return "ZombieBalloonHead";
                case 61:
                    return "ZombiePogoHead";
                case 72:
                    return "MoweredZombieHead";
                case 74:
                    return "ZombieHeadPool";
                default:
                    throw new ArgumentOutOfRangeException("id");
            }
        }

        /// <summary>Performs head image.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting head image.</returns>
        public static string HeadImage(int kind)
        {
            // PC 0x54581A..0x5458D9, image globals resolved against the PC resource loader.
            switch (kind)
            {
                case 8:
                    return "IMAGE_ZOMBIEDANCERHEAD";
                case 9:
                    return "IMAGE_ZOMBIEBACKUPDANCERHEAD";
                case 13:
                    return "IMAGE_ZOMBIEBOBSLEDHEAD";
                case 21:
                    return "IMAGE_ZOMBIELADDERHEAD";
                case 24:
                    return "IMAGE_ZOMBIEIMPHEAD";
                case 7:
                    return "IMAGE_ZOMBIEFOOTBALLHEAD";
                case 3:
                    return "IMAGE_ZOMBIEPOLEVAULTERHEAD";
                case 11:
                    return "IMAGE_REANIM_ZOMBIE_SNORKLE_HEAD";
                case 17:
                    return "IMAGE_ZOMBIEDIGGERHEAD";
                case 14:
                    return "IMAGE_ZOMBIEDOLPHINRIDERHEAD";
                case 19:
                    return "IMAGE_ZOMBIEYETIHEAD";
                default:
                    return null; // Preserve each XML's head / hat / propeller / glasses images.
            }
        }

        /// <summary>Performs head track visible.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting head track visible.</returns>
        public static bool HeadTrackVisible(int kind, string track)
        {
            if (track.StartsWith("anim_head", StringComparison.OrdinalIgnoreCase) || track.StartsWith("anim_hair", StringComparison.OrdinalIgnoreCase) || track.StartsWith("anim_tongue", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (kind == 8 && (Same(track, "Zombie_disco_chops") || Same(track, "Zombie_disco_glasses")))
            {
                return false;
            }

            if (kind == 9 && (Same(track, "Zombie_disco_chops") || Same(track, "Zombie_backup_stash")))
            {
                return false;
            }

            if (kind == 16 && (track.StartsWith("anim_hat", StringComparison.OrdinalIgnoreCase) || track.StartsWith("hat", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Original Zombie::DropPole hides prefixes, including both arm segments and pole2.
            if (kind == 3 && (track.StartsWith("Zombie_polevaulter_innerarm", StringComparison.OrdinalIgnoreCase)
                || track.StartsWith("Zombie_polevaulter_innerhand", StringComparison.OrdinalIgnoreCase)
                || track.StartsWith("Zombie_polevaulter_pole", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return (kind != 1 && kind != 33) || !Same(track, "Zombie_flaghand") && !Same(track, "Zombie_innerarm_screendoor");
        }

        /// <summary>Performs same.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting same.</returns>
        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
