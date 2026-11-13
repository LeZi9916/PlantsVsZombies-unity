#nullable enable
using PlantsVsZombies.Simulation;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie equipment rules.</summary>
    public static class AdventureZombieEquipmentRules
    {
        // PC 0x54CD70 / 0x54C950: integer thirds; boundaries themselves stay in the healthier stage.
        /// <summary>Performs damage index.</summary>
        /// <param name = "current">The current.</param>
        /// <param name = "maximum">The maximum.</param>
        /// <returns>The resulting damage index.</returns>
        public static int DamageIndex(float current, int maximum)
        {
            return maximum <= 0 ? 0 : current < maximum / 3 ? 2 : current < (2 * maximum) / 3 ? 1 : 0;
        }

        /// <summary>Returns the helm health assigned by the original Zombie::ZombieInitialize.</summary>
        /// <param name="kind">The zombie kind being initialized.</param>
        /// <returns>The starting helm health, or 0 when the kind has no helm.</returns>
        public static float StartingArmor(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Conehead:
                    return 370f;
                case ZombieKind.Buckethead:
                    return 1100f;
                case ZombieKind.Football:
                    return 1400f;
                case ZombieKind.Digger:
                    return 100f;
                case ZombieKind.Bobsled:
                    return 300f;
                default:
                    return 0f;
            }
        }

        /// <summary>Returns the shield health assigned by the original Zombie::ZombieInitialize.</summary>
        /// <param name="kind">The zombie kind being initialized.</param>
        /// <returns>The starting shield health, or 0 when the kind has no shield.</returns>
        public static float StartingShield(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.ScreenDoor:
                    return 1100f;
                case ZombieKind.Newspaper:
                    return 150f;
                case ZombieKind.Ladder:
                    return 500f;
                default:
                    return 0f;
            }
        }

        /// <summary>Performs helm type.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting helm type.</returns>
        public static int HelmType(Zombie z)
        {
            if (z.ModeHead == 27)
            {
                return 8;
            }

            if (z.ModeHead == 31)
            {
                return 9;
            }

            switch (z.Kind)
            {
                case ZombieKind.Conehead:
                    return 1;
                case ZombieKind.Buckethead:
                    return 2;
                case ZombieKind.Football:
                    return 3;
                case ZombieKind.Digger:
                    return 4;
                case ZombieKind.Bobsled:
                    return 7;
                default:
                    return 0;
            }
        }

        /// <summary>Performs helm track.</summary>
        /// <param name = "type">The type.</param>
        /// <returns>The resulting helm track.</returns>
        public static string? HelmTrack(int type)
        {
            switch (type)
            {
                case 1:
                    return "anim_cone";
                case 2:
                    return "anim_bucket";
                case 3:
                    return "zombie_football_helmet";
                case 4:
                    return "Zombie_digger_hardhat";
                case 8:
                    return "anim_face";
                case 9:
                    return "anim_idle";
                default:
                    return null;
            }
        }

        /// <summary>Performs helm image.</summary>
        /// <param name = "type">The type.</param>
        /// <param name = "stage">The stage.</param>
        /// <returns>The resulting helm image.</returns>
        public static string? HelmImage(int type, int stage)
        {
            if (stage == 0)
            {
                return null;
            }

            switch (type)
            {
                case 1:
                    return stage == 1 ? "IMAGE_REANIM_ZOMBIE_CONE2" : "IMAGE_REANIM_ZOMBIE_CONE3";
                case 2:
                    return stage == 1 ? "IMAGE_REANIM_ZOMBIE_BUCKET2" : "IMAGE_REANIM_ZOMBIE_BUCKET3";
                case 3:
                    return stage == 1 ? "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET2" : "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET3";
                case 4:
                    return stage == 1 ? "IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT2" : "IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT3";
                case 8:
                    return stage == 1 ? "IMAGE_REANIM_WALLNUT_CRACKED1" : "IMAGE_REANIM_WALLNUT_CRACKED2";
                case 9:
                    return stage == 1 ? "IMAGE_REANIM_TALLNUT_CRACKED1" : "IMAGE_REANIM_TALLNUT_CRACKED2";
                default:
                    return null;
            }
        }

        /// <summary>Gets the original particle emitted when a held shield is detached.</summary>
        /// <param name="zombie">The zombie supplying its original shield type.</param>
        /// <returns>The PC particle identifier, or minus one when the zombie has no supported shield.</returns>
        public static int ShieldParticle(Zombie zombie)
        {
            // PC DropShield 0x54C7A0 and the local particle table 0x74C8D8.
            return zombie.Kind == ZombieKind.ScreenDoor ? 12 : zombie.Kind == ZombieKind.Newspaper ? 13 : zombie.Kind == ZombieKind.Ladder ? 54 : -1;
        }

        /// <summary>Performs shield track.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting shield track.</returns>
        public static string? ShieldTrack(Zombie z)
        {
            return z.Kind == ZombieKind.ScreenDoor ? "anim_screendoor" : z.Kind == ZombieKind.Newspaper ? "Zombie_paper_paper" : z.Kind == ZombieKind.Ladder ? "Zombie_ladder_1" : null;
        }

        /// <summary>Performs shield image.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "stage">The stage.</param>
        /// <returns>The resulting shield image.</returns>
        public static string? ShieldImage(Zombie z, int stage)
        {
            if (stage == 0)
            {
                return null;
            }

            if (z.Kind == ZombieKind.ScreenDoor)
            {
                return stage == 1 ? "IMAGE_REANIM_ZOMBIE_SCREENDOOR2" : "IMAGE_REANIM_ZOMBIE_SCREENDOOR3";
            }

            if (z.Kind == ZombieKind.Newspaper)
            {
                return stage == 1 ? "IMAGE_REANIM_ZOMBIE_PAPER_PAPER2" : "IMAGE_REANIM_ZOMBIE_PAPER_PAPER3";
            }

            if (z.Kind == ZombieKind.Ladder)
            {
                return stage == 1 ? "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE1" : "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE2";
            }

            return null;
        }

        /// <summary>Performs helm particle.</summary>
        /// <param name = "type">The type.</param>
        /// <returns>The resulting helm particle.</returns>
        public static int HelmParticle(int type)
        {
            return type == 1 ? 8 : type == 2 ? 9 : type == 3 ? 10 : type == 4 ? 14 : -1;
        }

        /// <summary>Performs track visible.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting track visible.</returns>
        public static bool TrackVisible(Zombie z, string track)
        {
            var helm = HelmType(z);
            string? h = HelmTrack(helm), shield = ShieldTrack(z);
            if (helm > 0 && helm < 8 && h != null && Same(track, h) && z.ArmorHealth <= 0)
            {
                return false;
            }

            if (shield != null && Same(track, shield) && z.ShieldHealth <= 0)
            {
                return false;
            }

            if ((helm == 1 || helm == 2 || helm == 3) && z.ArmorHealth > 0 && track.StartsWith("anim_hair", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>Performs image.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "asset">The asset.</param>
        /// <param name = "body">The body.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting image.</returns>
        public static string? Image(Zombie z, string asset, string body, string track)
        {
            var helm = HelmType(z);
            var bodyTrack = Same(asset, body);
            if (bodyTrack && z.Kind == ZombieKind.Zomboni)
            {
                return AdventureZomboniRules.DamageImage(z, track);
            }

            // PC UpdateZombieNewspaper (0x541829) replaces only anim_head1 after the gasp completes.
            if (bodyTrack && z.Kind == ZombieKind.Newspaper && z.PresentationNativePhase == 31 && Same(track, "anim_head1"))
            {
                return "IMAGE_REANIM_ZOMBIE_PAPER_MADHEAD";
            }

            var child = (helm == 8 && Same(asset, "Wallnut")) || (helm == 9 && Same(asset, "Tallnut"));
            if ((bodyTrack && helm < 8 || child) && z.ArmorHealth > 0 && Same(track, HelmTrack(helm)))
            {
                return HelmImage(helm, DamageIndex(z.ArmorHealth, (int)z.PresentationMaxArmor));
            }

            return bodyTrack && z.ShieldHealth > 0 && Same(track, ShieldTrack(z)) ? ShieldImage(z, DamageIndex(z.ShieldHealth, (int)z.PresentationMaxShield)) : null;
        }

        /// <summary>Performs same.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting same.</returns>
        private static bool Same(string a, string? b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
