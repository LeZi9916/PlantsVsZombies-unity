#nullable enable
using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares the original pole-vault and pool phases between simulation and drawing.</summary>
    internal static class AdventureZombieMotionRules
    {
        /// <summary>Checks the original CheckForPool probes at mX + 75 and mX + 45.</summary>
        /// <param name="nativeX">The original zombie position, before body overlay offsets.</param>
        /// <returns>Whether both probes are in a pool column and integer mX is below 680.</returns>
        internal static bool IsPoolPosition(float nativeX)
        {
            // PC CheckForPool 0x54B5F0 compares integer mX against 680 after testing both probes.
            return (int)nativeX + 45 >= 40 && (int)nativeX < 680;
        }

        /// <summary>Samples the roof floor beneath the pole vault's authored horizontal displacement.</summary>
        /// <param name="nativeX">The zombie's current native position before animation displacement.</param>
        /// <param name="progress">The normalized original jump reanimation time.</param>
        /// <returns>The additional native row height caused by the original virtual foot position.</returns>
        internal static float PoleVaultRoofOffset(float nativeX, float progress)
        {
            // PC 541850 temporarily subtracts 150 * animTime before calling GetPosYBasedOnRow (54D620).
            var footX = nativeX - 150f * Math.Max(0f, Math.Min(1f, progress));
            return (Math.Max(0f, 400f - footX) - Math.Max(0f, 400f - nativeX)) * .25f;
        }

        /// <summary>Selects the native phase's animation before generic eating or walking selection.</summary>
        /// <param name="zombie">The zombie with an initialized native phase.</param>
        /// <returns>The phase animation, or null for the existing generic selector.</returns>
        internal static string? Animation(Zombie zombie)
        {
            if (AdventurePogoRules.IsBouncing(zombie))
            {
                return "anim_pogo";
            }

            if (zombie.Kind == ZombieKind.Ladder && zombie.PresentationNativePhase == 77)
            {
                return "anim_placeladder";
            }

            if (zombie.Kind == ZombieKind.Digger)
            {
                switch (zombie.PresentationNativePhase)
                {
                    case 32:
                    case 34:
                        return "anim_dig";
                    case 33:
                        return zombie.DiggerState?.PhaseTicks > 30 ? "anim_drill" : "anim_landing";
                    case 35:
                        return "anim_landing";
                    case 36:
                        return "anim_dizzy";
                }
            }

            if (zombie.Kind == ZombieKind.PoleVaulter)
            {
                return zombie.PresentationNativePhase == 12 ? "anim_jump" : !zombie.UsedAbility ? "anim_run" : null;
            }

            if (zombie.Kind == ZombieKind.Balloon)
            {
                return zombie.PresentationNativePhase == 74 ? "anim_pop" : zombie.Airborne ? "anim_idle" : null;
            }

            if (zombie.Kind == ZombieKind.DolphinRider)
            {
                switch (zombie.PresentationNativePhase)
                {
                    case 51:
                        return "anim_walkdolphin";
                    case 52:
                        return "anim_jumpinpool";
                    case 53:
                        return "anim_ride";
                    case 54:
                        return "anim_dolphinjump";
                    case 55:
                        return zombie.Eating ? "anim_eat" : "anim_swim";
                    case 56:
                        return zombie.Eating ? "anim_eat" : "anim_walk";
                    default:
                        return null;
                }
            }

            if (zombie.Kind != ZombieKind.Snorkel)
            {
                return null;
            }

            switch (zombie.PresentationNativePhase)
            {
                case 57:
                    return "anim_walk";
                case 58:
                    return "anim_jumpinpool";
                case 59:
                    return "anim_swim";
                case 60:
                case 62:
                    return "anim_uptoeat";
                case 61:
                    return "anim_eat";
                default:
                    return null;
            }
        }

        /// <summary>Identifies the original PlayOnceAndHold phases.</summary>
        /// <param name="zombie">The zombie whose animation is being drawn.</param>
        /// <returns>Whether the animation must hold its final frame instead of looping.</returns>
        internal static bool PlaysOnce(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            return zombie.Kind == ZombieKind.Ladder && zombie.PresentationNativePhase == 77
                || zombie.Kind == ZombieKind.Catapult && (phase == 67 || zombie.CatapultState?.FlatTires == true)
                || AdventurePogoRules.IsBouncing(zombie)
                || zombie.Kind == ZombieKind.Digger && (phase == 35 || phase == 33 && zombie.DiggerState?.PhaseTicks <= 30)
                || zombie.Kind == ZombieKind.JackInTheBox && phase == 16
                || zombie.Kind == ZombieKind.Zomboni && zombie.ZomboniState?.FlatTires == true
                || zombie.Kind == ZombieKind.Balloon && phase == 74
                || zombie.Kind == ZombieKind.PoleVaulter && phase == 12
                || zombie.Kind == ZombieKind.DolphinRider && (phase == 52 || phase == 54)
                || zombie.Kind == ZombieKind.Snorkel && (phase == 58 || phase == 60 || phase == 62);
        }

        /// <summary>Converts the down-from-eating clock into reverse uptoeat playback.</summary>
        /// <param name="zombie">The zombie whose forward elapsed clock is sampled.</param>
        /// <param name="seconds">The elapsed time, including any render interpolation.</param>
        /// <returns>The native playback time for the selected positive frame rate.</returns>
        internal static float PlaybackSeconds(Zombie zombie, float seconds)
        {
            return zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 62
                ? Math.Max(0f, 12f / 24f - seconds) : seconds;
        }

        /// <summary>Gets the dolphin's original phase or independent meal-transition blend duration.</summary>
        /// <param name="zombie">The dolphin pose whose native clip transition is being submitted.</param>
        /// <returns>The original blend duration in one-hundred-hertz updates.</returns>
        internal static int DolphinBlendTicks(Zombie zombie)
        {
            return zombie.DolphinState?.AnimationBlendTicks
                ?? (zombie.PresentationNativePhase == 52 || zombie.PresentationNativePhase == 1 ? 20 : 0);
        }

        /// <summary>Gets the original unscaled horizontal image displacement for the dolphin's current phase.</summary>
        /// <param name="zombie">The live or retained dolphin rider pose.</param>
        /// <returns>The native displacement, or zero for another species.</returns>
        internal static float DolphinImageOffsetX(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.DolphinRider)
            {
                return 0f;
            }

            var phase = zombie.PresentationNativePhase;
            return phase == 54 ? 70f + zombie.PresentationAltitude
                : phase == 53 || phase < 0 && !zombie.UsedAbility ? 70f : 0f;
        }

        /// <summary>Gets the original unscaled vertical image displacement for swimming and dying dolphin riders.</summary>
        /// <param name="zombie">The live or retained dolphin rider pose.</param>
        /// <returns>The native displacement, or zero for another species or phase.</returns>
        internal static float DolphinImageOffsetY(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            return zombie.Kind == ZombieKind.DolphinRider
                && (phase == 55 || phase == 1 || phase < 0 && zombie.UsedAbility) ? 50f : 0f;
        }

        /// <summary>Computes the original GetDrawPos water clipping height.</summary>
        /// <param name="zombie">The live zombie or retained death pose.</param>
        /// <param name="scale">The native zombie scale.</param>
        /// <returns>The native clipping height, or the original off-screen sentinel.</returns>
        internal static float ClipHeight(Zombie zombie, float scale)
        {
            if (zombie.Kind == ZombieKind.Digger
                && (zombie.PresentationNativePhase == 33 || zombie.PresentationNativePhase == 35)
                && zombie.DiggerState?.PhaseTicks > 20)
            {
                // PC 549C33 / 548D40 cancels the underground altitude at the fixed ground clipping edge.
                return -zombie.PresentationAltitude;
            }

            // PC GetDrawPos 54998C..549B31 changes the clip only during these authored dolphin intervals.
            if (zombie.Kind == ZombieKind.DolphinRider)
            {
                var phase = zombie.PresentationNativePhase;
                var altitude = zombie.PresentationAltitude;
                if (phase == 52)
                {
                    var progress = zombie.PresentationAnimationAge * 16f / 45f;
                    if (progress >= .56f && progress <= .65f)
                    {
                        return 0f;
                    }

                    return progress >= .75f ? -altitude - 10f : -10000f;
                }

                if (phase == 53 || phase < 0 && !zombie.UsedAbility)
                {
                    return -altitude - (zombie.PresentationNativeHeight == 3 ? 15f : 10f);
                }

                if (phase == 54)
                {
                    var progress = zombie.PresentationAnimationAge * 10f / 23f;
                    return progress <= .06f ? -altitude - 10f
                        : progress >= .5f && progress <= .76f ? -13f : -10000f;
                }

                if (phase == 1)
                {
                    return 44f - altitude;
                }

                if (phase == 55 || phase < 0 && zombie.UsedAbility)
                {
                    return zombie.PresentationNativeHeight == 3 ? 36f - altitude : -10000f;
                }

                return (phase == 51 || phase == 56) && zombie.PresentationNativeHeight == 2 ? -altitude : -10000f;
            }

            // Whitewater tracks separately ignore this rectangle until DragUnder restores their clipping.
            if (zombie.Kind == ZombieKind.Snorkel)
            {
                if (zombie.PresentationNativePhase == 58)
                {
                    return zombie.PresentationAnimationAge * 16f / 19f >= .8f ? -10f : -10000f;
                }

                return zombie.PresentationInPool == true
                    ? -zombie.PresentationAltitude - 5f + 20f - 20f * scale : -10000f;
            }

            return zombie.PresentationInPool == true
                ? -zombie.PresentationAltitude - 7f + 10f - 10f * scale + (zombie.Eating ? 7f : 0f) : -10000f;
        }

        /// <summary>Computes the native DrawReanim water clipping edge.</summary>
        /// <param name="zombie">The live zombie or retained death pose.</param>
        /// <param name="nativeY">The original mPosY before altitude and body overlay offsets.</param>
        /// <param name="scale">The native zombie scale.</param>
        /// <returns>The world-space bottom edge, or positive infinity when clipping is disabled.</returns>
        internal static float ClipBottom(Zombie zombie, float nativeY, float scale)
        {
            var height = ClipHeight(zombie, scale);
            // PC DrawReanim 0x548D40: -78 top offset plus (120 - clipHeight + 71) height.
            var imageOffsetY = DolphinImageOffsetY(zombie);
            return height > -100f ? nativeY + imageOffsetY + 113f - zombie.PresentationAltitude - height : float.PositiveInfinity;
        }
    }
}
