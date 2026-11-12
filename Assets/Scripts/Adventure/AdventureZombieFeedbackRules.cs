#nullable enable
using PlantsVsZombies.Simulation;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie feedback rules.</summary>
    public static class AdventureZombieFeedbackRules
    {
        /// <summary>Performs can lose parts.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "flying">The flying.</param>
        /// <param name = "hasSled">The has sled.</param>
        /// <returns>The resulting can lose parts.</returns>
        public static bool CanLoseParts(int kind, int height, bool flying, bool hasSled)
        {
            return kind != 12 && kind != 20 && kind != 22 && kind != 23 && kind != 32 && kind != 25 && height != 10 && !flying && !hasSled;
        }

        /// <summary>Performs should lose arm.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "flying">The flying.</param>
        /// <param name = "hasSled">The has sled.</param>
        /// <param name = "hasArm">The has arm.</param>
        /// <param name = "shieldType">The shield type.</param>
        /// <param name = "bodyHealth">The body health.</param>
        /// <param name = "bodyMaxHealth">The body max health.</param>
        /// <returns>The resulting should lose arm.</returns>
        public static bool ShouldLoseArm(int kind, int phase, int height, bool flying, bool hasSled, bool hasArm, int shieldType, float bodyHealth, int bodyMaxHealth)
        {
            // PC 0x546160 and 0x545B20; integer threshold precedes the separate head test.
            if (!hasArm || bodyHealth <= 0f || bodyHealth >= (2 * bodyMaxHealth) / 3 || !CanLoseParts(kind, height, flying, hasSled) || shieldType == 1 || shieldType == 2)
            {
                return false;
            }

            return phase != 58 && phase != 51 && phase != 52 && phase != 53 && phase != 54 && phase != 29;
        }

        /// <summary>Performs arm track visible.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting arm track visible.</returns>
        public static bool ArmTrackVisible(int kind, string track)
        {
            if (kind == 7)
            {
                return !Prefix(track, "Zombie_football_leftarm_lower") && !Prefix(track, "Zombie_football_leftarm_hand");
            }

            if (kind == 5)
            {
                return !Same(track, "Zombie_paper_hands") && !Same(track, "Zombie_paper_leftarm_lower");
            }

            if (kind == 3)
            {
                return !Same(track, "Zombie_polevaulter_outerarm_lower") && !Same(track, "Zombie_outerarm_hand");
            }

            if (kind == 8 || kind == 9)
            {
                return !Same(track, "Zombie_disco_outerarm_lower") && !Same(track, "Zombie_disco_outerhand") && (kind != 8 || !Same(track, "Zombie_disco_outerhand_point"));
            }

            return !Prefix(track, "Zombie_outerarm_lower") && !Prefix(track, "Zombie_outerarm_hand");
        }

        /// <summary>Performs arm anchor.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting arm anchor.</returns>
        public static string ArmAnchor(int kind)
        {
            switch (kind)
            {
                case 7:
                    return "zombie_football_leftarm_hand";
                case 5:
                    return "Zombie_paper_leftarm_lower";
                case 3:
                    return "Zombie_polevaulter_outerarm_lower";
                case 15:
                    return "Zombie_jackbox_outerarm_lower";
                case 8:
                case 9:
                    return "Zombie_disco_outerarm_lower";
                case 19:
                case 21:
                    return "Zombie_outerarm_hand";
                default:
                    return "Zombie_outerarm_lower";
            }
        }

        /// <summary>Performs arm image.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting arm image.</returns>
        public static string ArmImage(int kind)
        {
            switch (kind)
            {
                case 7:
                    return "IMAGE_REANIM_ZOMBIE_FOOTBALL_LEFTARM_HAND";
                case 5:
                    return "IMAGE_REANIM_ZOMBIE_PAPER_LEFTARM_LOWER";
                case 8:
                    return "IMAGE_REANIM_ZOMBIE_DISCO_OUTERARM_HAND";
                case 9:
                    return "IMAGE_REANIM_ZOMBIE_BACKUP_INNERARM_HAND";
                case 13:
                    return "IMAGE_REANIM_ZOMBIE_BOBSLED_OUTERARM_HAND";
                case 24:
                    return "IMAGE_REANIM_ZOMBIE_IMP_ARM2";
                case 19:
                    return "IMAGE_REANIM_ZOMBIE_YETI_OUTERARM_HAND";
                case 15:
                    return "IMAGE_ZOMBIEJACKBOXARM";
                case 17:
                    return "IMAGE_ZOMBIEDIGGERARM";
                case 3:
                case 16:
                case 14:
                case 18:
                case 21:
                    return "IMAGE_REANIM_ZOMBIE_OUTERARM_HAND";
                default:
                    return "IMAGE_ZOMBIEARM";
            }
        }

        /// <summary>Performs arm replacement.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting arm replacement.</returns>
        public static string? ArmReplacement(int kind, string track)
        {
            switch (kind)
            {
                case 7:
                    return Same(track, "zombie_football_leftarm_upper") ? "IMAGE_REANIM_ZOMBIE_FOOTBALL_LEFTARM_UPPER2" : null;
                case 5:
                    return Same(track, "zombie_paper_leftarm_upper") ? "IMAGE_REANIM_ZOMBIE_PAPER_LEFTARM_UPPER2" : null;
                case 3:
                    return Same(track, "zombie_polevaulter_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_POLEVAULTER_OUTERARM_UPPER2" : null;
                case 16:
                    return Same(track, "zombie_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_BALLOON_OUTERARM_UPPER2" : null;
                case 24:
                    return Same(track, "zombie_imp_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_IMP_ARM1_BONE" : null;
                case 17:
                    return Same(track, "zombie_digger_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_DIGGER_OUTERARM_UPPER2" : null;
                case 13:
                    return Same(track, "zombie_dolphinrider_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_BOBSLED_OUTERARM_UPPER2" : null;
                case 15:
                    return Same(track, "Zombie_jackbox_outerarm_lower") ? "IMAGE_REANIM_ZOMBIE_JACKBOX_OUTERARM_LOWER2" : null;
                case 11:
                    return Same(track, "zombie_snorkle_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_SNORKLE_OUTERARM_UPPER2" : null;
                case 14:
                    return Same(track, "zombie_dolphinrider_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_DOLPHINRIDER_OUTERARM_UPPER2" : null;
                case 18:
                    if (Same(track, "Zombie_outerarm_upper"))
                    {
                        return "IMAGE_REANIM_ZOMBIE_POGO_OUTERARM_UPPER2";
                    }

                    if (Same(track, "Zombie_pogo_stickhands"))
                    {
                        return "IMAGE_REANIM_ZOMBIE_POGO_STICKHANDS2";
                    }

                    if (Same(track, "Zombie_pogo_stick"))
                    {
                        return "IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE2";
                    }

                    return Same(track, "Zombie_pogo_stick2") ? "IMAGE_REANIM_ZOMBIE_POGO_STICK2DAMAGE2" : null;
                case 1:
                case 33:
                    if (Same(track, "Zombie_flag"))
                    {
                        return "IMAGE_REANIM_ZOMBIE_FLAG3";
                    }

                    return Same(track, "Zombie_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_OUTERARM_UPPER2" : null;
                case 8:
                    return Same(track, "Zombie_disco_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_DISCO_OUTERARM_UPPER2" : null;
                case 9:
                    return Same(track, "Zombie_disco_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_BACKUP_OUTERARM_UPPER2" : null;
                case 21:
                    return Same(track, "zombie_ladder_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_LADDER_OUTERARM_UPPER2" : null;
                case 19:
                    return Same(track, "zombie_yeti_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_YETI_OUTERARM_UPPER2" : null;
                default:
                    return Same(track, "Zombie_outerarm_upper") ? "IMAGE_REANIM_ZOMBIE_OUTERARM_UPPER2" : null;
            }
        }

        /// <summary>Performs flash channel.</summary>
        /// <param name = "remaining">The remaining.</param>
        /// <returns>The resulting flash channel.</returns>
        public static int FlashChannel(float remaining)
        {
            return Math.Min(255, Math.Max(0, (int)Math.Round(remaining * 100f)) * 10);
        }

        /// <summary>Performs additive channel.</summary>
        /// <param name = "originalColor">The original color.</param>
        /// <param name = "remaining">The remaining.</param>
        /// <param name = "suppressFlash">The suppress flash.</param>
        /// <returns>The resulting additive channel.</returns>
        public static int AdditiveChannel(int originalColor, float remaining, bool suppressFlash = false)
        {
            return Math.Min(255, Math.Max(0, originalColor) + (suppressFlash ? 0 : FlashChannel(remaining)));
        }

        /// <summary>Performs shield type.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting shield type.</returns>
        public static int ShieldType(Zombie zombie)
        {
            if (zombie.ShieldHealth <= 0f)
            {
                return 0;
            }

            return zombie.Kind == ZombieKind.ScreenDoor ? 1 : zombie.Kind == ZombieKind.Newspaper ? 2 : zombie.Kind == ZombieKind.Ladder ? 3 : 0;
        }

        /// <summary>Performs render group.</summary>
        /// <param name = "shieldType">The shield type.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting render group.</returns>
        public static int RenderGroup(int shieldType, string track)
        {
            if (shieldType == 1)
            {
                if (Same(track, "anim_screendoor"))
                {
                    return 1;
                }

                if (Prefix(track, "Zombie_outerarm_screendoor"))
                {
                    return 2;
                }
            }

            if (shieldType == 2)
            {
                if (Same(track, "zombie_paper_paper"))
                {
                    return 1;
                }

                if (Prefix(track, "Zombie_paper_hands"))
                {
                    return 2;
                }
            }

            if (shieldType == 3)
            {
                if (Same(track, "zombie_ladder_1"))
                {
                    return 1;
                }

                if (Prefix(track, "Zombie_outerarm"))
                {
                    return 2;
                }
            }

            return 0;
        }

        /// <summary>Performs uses ground.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "hasSled">The has sled.</param>
        /// <returns>The resulting uses ground.</returns>
        public static bool UsesGround(int kind, int phase, bool hasSled)
        {
            // Both velocity branches are literal PC 0x546783..0x54680A cases.
            return !(phase >= 20 && phase <= 28) && phase != 73 && phase != 53 && phase != 59 && kind != 22 && kind != 12 && phase != 32 && phase != 54 && !hasSled && phase != 12 && phase != 58;
        }

        /// <summary>Performs same.</summary>
        /// <param name = "actual">The actual.</param>
        /// <param name = "expected">The expected.</param>
        /// <returns>The resulting same.</returns>
        public static bool Same(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Performs prefix.</summary>
        /// <param name = "actual">The actual.</param>
        /// <param name = "expected">The expected.</param>
        /// <returns>The resulting prefix.</returns>
        private static bool Prefix(string actual, string expected)
        {
            return actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
