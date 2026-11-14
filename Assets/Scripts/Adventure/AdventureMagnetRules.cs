#nullable enable

using System;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines PC 470170/46F9C0 magnetic eligibility, priority, metal movement and artwork identifiers.</summary>
    public static class AdventureMagnetRules
    {
        /// <summary>Gets the original number of updates from attraction to becoming ready again.</summary>
        public const int CooldownTicks = 1500;
        /// <summary>Gets the original complete sucking clip length at twelve FPS.</summary>
        public const int SuckingFrames = 26;

        /// <summary>Checks the original common zombie gates and its remaining magnetic object.</summary>
        /// <param name="zombie">The potential live target.</param>
        /// <param name="body">The original body rectangle, including altitude and clipping.</param>
        /// <param name="plantRow">The Magnet-shroom's logical lawn row.</param>
        /// <returns>Whether the original metal and state gates permit attraction before the circular range check.</returns>
        public static bool CanTarget(Zombie zombie, NativeRect body, int plantRow)
        {
            var phase = zombie.PresentationNativePhase;
            if (zombie.Health <= 0f || zombie.Headless || zombie.Hypnotized || zombie.PresentationNativeHeight != 0
                || zombie.DropRemaining > 0f || zombie.GraveRiseTicks > 0 || phase == 14 || phase >= 1 && phase <= 3
                || body.X > 800 || Math.Abs(zombie.Row - plantRow) > 2)
            {
                return false;
            }

            if (zombie.Kind == ZombieKind.Pogo)
            {
                return !zombie.UsedAbility;
            }

            if (zombie.Kind == ZombieKind.Digger)
            {
                return AdventureDiggerState.CanLoseAxe(zombie);
            }

            var helm = AdventureZombieEquipmentRules.HelmType(zombie);
            return zombie.ArmorHealth > 0f && (helm == 2 || helm == 3)
                || zombie.ShieldHealth > 0f && (zombie.Kind == ZombieKind.ScreenDoor || zombie.Kind == ZombieKind.Ladder)
                || zombie.Kind == ZombieKind.JackInTheBox && phase == 15;
        }

        /// <summary>Checks the original inclusive circle against the complete native body rectangle.</summary>
        /// <param name="plantX">The plant's original top-left x.</param>
        /// <param name="plantY">The plant's original top-left y before flower-pot draw offsets.</param>
        /// <param name="body">The target's complete native body rectangle.</param>
        /// <param name="eating">Whether the original three-hundred-twenty-pixel eating radius applies.</param>
        /// <returns>Whether the body touches the original 270/320-pixel circle centered at plant+(0,20).</returns>
        public static bool InRange(int plantX, int plantY, NativeRect body, bool eating)
        {
            var dx = Math.Max(body.X - plantX, Math.Max(0, plantX - body.X - body.Width));
            var dy = Math.Max(body.Y - plantY - 20, Math.Max(0, plantY + 20 - body.Y - body.Height));
            var radius = eating ? 320 : 270;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>Gets the original nearest-target priority, retaining creation order on equal values.</summary>
        /// <param name="plantX">The plant's native top-left x.</param>
        /// <param name="plantY">The plant's native top-left y.</param>
        /// <param name="body">The original target body rectangle.</param>
        /// <param name="rowDistance">The absolute logical row difference.</param>
        /// <returns>The Euclidean top-left distance plus eighty pixels for each intervening row.</returns>
        public static float TargetDistance(int plantX, int plantY, NativeRect body, int rowDistance)
        {
            var dx = (float)(body.X - plantX);
            var dy = (float)(body.Y - plantY);
            return (float)Math.Sqrt(dx * dx + dy * dy) + 80f * rowDistance;
        }

        /// <summary>Samples the original five-percent-per-update attraction and its twenty-pixel stopping threshold.</summary>
        /// <param name="item">The metal item with a resolved initial position.</param>
        /// <param name="plantX">The plant's native top-left x.</param>
        /// <param name="plantY">The plant's native top-left y.</param>
        /// <param name="x">Receives the original current unscaled item x.</param>
        /// <param name="y">Receives the original current unscaled item y.</param>
        /// <returns>Whether the remaining distance selects the global top pass instead of the plant's local foreground pass.</returns>
        public static bool SampleItem(AdventureMagnetItem item, float plantX, float plantY, out float x, out float y)
        {
            x = item.X;
            y = item.Y;
            for (var tick = 0; tick < item.AgeTicks; tick++)
            {
                var dx = plantX + item.DestinationX - x;
                var dy = plantY + item.DestinationY - y;
                if ((float)Math.Sqrt(dx * dx + dy * dy) < 20f)
                {
                    break;
                }

                x += dx * .05f;
                y += dy * .05f;
            }

            var remainingX = plantX + item.DestinationX - x;
            var remainingY = plantY + item.DestinationY - y;
            return (float)Math.Sqrt(remainingX * remainingX + remainingY * remainingY) > 20f;
        }

        /// <summary>Gets the unchanged original metal sprite resource for a PC item identifier.</summary>
        /// <param name="kind">The original MagnetItemType identifier.</param>
        /// <returns>The original resource name, or an empty string for an unused identifier.</returns>
        public static string Image(int kind)
        {
            return kind switch
            {
                1 => "IMAGE_REANIM_ZOMBIE_BUCKET1",
                2 => "IMAGE_REANIM_ZOMBIE_BUCKET2",
                3 => "IMAGE_REANIM_ZOMBIE_BUCKET3",
                4 => "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET",
                5 => "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET2",
                6 => "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET3",
                7 => "IMAGE_REANIM_ZOMBIE_SCREENDOOR1",
                8 => "IMAGE_REANIM_ZOMBIE_SCREENDOOR2",
                9 => "IMAGE_REANIM_ZOMBIE_SCREENDOOR3",
                10 => "IMAGE_ZOMBIEPOGO",
                11 => "IMAGE_ZOMBIEPOGO",
                12 => "IMAGE_ZOMBIEPOGO",
                13 => "IMAGE_REANIM_ZOMBIE_JACKBOX_BOX",
                14 => "IMAGE_REANIM_ZOMBIE_LADDER_1",
                15 => "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE1",
                16 => "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE2",
                20 => "IMAGE_REANIM_ZOMBIE_LADDER_5",
                21 => "IMAGE_REANIM_ZOMBIE_DIGGER_PICKAXE",
                _ => string.Empty
            };
        }
    }
}
