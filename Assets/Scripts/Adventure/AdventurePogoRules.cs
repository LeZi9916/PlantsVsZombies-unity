#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Shares the original PC pogo phases and bounce curve with simulation and presentation.</summary>
    public static class AdventurePogoRules
    {
        /// <summary>Gets the original bounce duration in one-hundred-hertz updates.</summary>
        public const int BounceTicks = 80;
        /// <summary>Gets the eleven-frame pogo clip's original held endpoint in seconds at forty FPS.</summary>
        public const float HeldAnimationSeconds = 11f / 40f;

        /// <summary>Checks the original IsBouncingPogo phase interval without confusing a dropped stick with a bounce.</summary>
        /// <param name="zombie">The live or retained pogo state.</param>
        /// <returns>Whether the pogo is in one of PC phases twenty through twenty-eight.</returns>
        public static bool IsBouncing(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Pogo || zombie.UsedAbility)
            {
                return false;
            }

            var phase = zombie.PresentationNativePhase;
            return phase >= 20 && phase <= 28 || phase < 0;
        }

        /// <summary>Checks PC 470170's original pogo object, head, height, death, and right-edge magnet gates.</summary>
        /// <param name="zombie">The pogo whose retained stick is being considered by a Magnet-shroom.</param>
        /// <returns>Whether the original live pogo can lose its stick to the magnet.</returns>
        public static bool CanLoseStick(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            return zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility && !zombie.Headless
                && !zombie.Hypnotized && zombie.Health > 0f && zombie.PresentationNativeHeight == 0
                && zombie.DropRemaining <= 0f && !(phase >= 1 && phase <= 3)
                && AdventureZombieTargetingRules.BodyRectangle(zombie).X <= 800;
        }

        /// <summary>Samples PC UpdateZombiePogo 540F10's original BounceSlowMiddle altitude.</summary>
        /// <param name="phase">The original ordinary, high, or forward bounce phase.</param>
        /// <param name="ticks">The remaining updates in the original eighty-update bounce.</param>
        /// <returns>The native unscaled altitude above the pogo's row baseline.</returns>
        public static float Altitude(int phase, int ticks)
        {
            var height = phase >= 21 && phase <= 26 ? 50f + 20f * (phase - 21)
                : phase == 27 ? 90f : phase == 28 ? 170f : 40f;
            var progress = Math.Max(0f, Math.Min(1f, (BounceTicks - ticks) / (float)BounceTicks));
            // PC 526C50 makes the triangular Bounce; curve eleven at 526D51 applies EaseOut to it.
            var bounce = 1f - Math.Abs(2f * progress - 1f);
            return 9f + height * (2f * bounce - bounce * bounce);
        }
    }
}
