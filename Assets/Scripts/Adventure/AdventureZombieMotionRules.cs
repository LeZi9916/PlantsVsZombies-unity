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
            if (zombie.Kind == ZombieKind.PoleVaulter)
            {
                return zombie.PresentationNativePhase == 12 ? "anim_jump" : !zombie.UsedAbility ? "anim_run" : null;
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
            return zombie.Kind == ZombieKind.Zomboni && zombie.ZomboniState?.FlatTires == true
                || zombie.Kind == ZombieKind.PoleVaulter && phase == 12
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

        /// <summary>Computes the original GetDrawPos water clipping height.</summary>
        /// <param name="zombie">The live zombie or retained death pose.</param>
        /// <param name="scale">The native zombie scale.</param>
        /// <returns>The native clipping height, or the original off-screen sentinel.</returns>
        internal static float ClipHeight(Zombie zombie, float scale)
        {
            // PC GetDrawPos 0x549740: water riders retain species-specific clipping while kelp drags them.
            if (zombie.Kind == ZombieKind.DolphinRider && (zombie.TangleKelpGrab != null || zombie.PresentationNativeHeight == 3))
            {
                var riding = zombie.PresentationNativePhase == 53 || zombie.PresentationNativePhase < 0 && !zombie.UsedAbility;
                if (riding)
                {
                    return -zombie.PresentationAltitude - (zombie.PresentationNativeHeight == 3 ? 15f : 10f);
                }

                return zombie.PresentationNativeHeight == 3 ? 36f - zombie.PresentationAltitude : -10000f;
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
            var imageOffsetY = zombie.Kind == ZombieKind.DolphinRider && zombie.PresentationNativeHeight == 3
                && (zombie.PresentationNativePhase == 55 || zombie.PresentationNativePhase < 0 && zombie.UsedAbility) ? 50f : 0f;
            return height > -100f ? nativeY + imageOffsetY + 113f - zombie.PresentationAltitude - height : float.PositiveInfinity;
        }
    }
}
