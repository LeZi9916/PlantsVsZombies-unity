#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC digger's independent emergence countdown and original attached effects.</summary>
    public sealed class AdventureDiggerState
    {
        /// <summary>Stores the remaining native updates in the axe-loss pause or emergence.</summary>
        public int PhaseTicks;
        /// <summary>Stores the current original reanimation transition blend in native updates.</summary>
        public int AnimationBlendTicks;
        /// <summary>Retains the exact dig pose stopped by PC DiggerLoseAxe, before setting its rate to zero.</summary>
        public float PausedDigSeconds;
        /// <summary>Stores the original tunnel particle's cross-fade start age, or minus one while attached.</summary>
        public float TunnelStopAge = -1f;
        /// <summary>Stores elapsed seconds for the original axe-loss surprise attachment, or minus one before creation.</summary>
        public float SurpriseAge = -1f;

        /// <summary>Samples PC 0x543D90's integer-valued emergence altitude.</summary>
        /// <param name="ticks">The remaining original emergence updates.</param>
        /// <returns>The original altitude in board pixels.</returns>
        public static float RiseAltitude(int ticks)
        {
            var progress = ticks > 40 ? Math.Max(0f, Math.Min(1f, (130 - ticks) / 90f))
                : Math.Max(0f, Math.Min(1f, (30 - ticks) / 30f));
            var altitude = ticks > 40 ? -120f + 140f * (2f * progress - progress * progress)
                : 20f - 20f * progress * progress;
            // PC TodAnimateCurve 526F20 rounds signed halves away from zero before returning an integer.
            return (int)(altitude + (altitude >= 0f ? .5f : -.5f));
        }

        /// <summary>Resolves PC 0x547BC0's digger facing independently of its retained pickaxe while alive.</summary>
        /// <param name="zombie">The live digger or its retained dying pose.</param>
        /// <returns>Whether the original digger phase faces and walks to the right.</returns>
        public static bool WalksBackwards(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            return phase == 33 || phase == 36 || phase == 37
                || phase >= 1 && phase <= 3 && !zombie.UsedAbility
                || phase < 0 && (zombie.AbilityPhase == 1 || zombie.AbilityPhase == 2);
        }

        /// <summary>Checks the original magnet's digger-only object, phase, head, height, and screen-edge gates.</summary>
        /// <param name="zombie">The digger being considered by a Magnet-shroom.</param>
        /// <returns>Whether PC 0x470170 admits the retained pickaxe.</returns>
        public static bool CanLoseAxe(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            if (phase < 0)
            {
                phase = zombie.AbilityPhase == 0 ? 32 : zombie.AbilityPhase == 1 ? 36 : 37;
            }

            return !zombie.UsedAbility && !zombie.Headless && zombie.Health > 0f
                && zombie.PresentationNativeHeight == 0 && zombie.DropRemaining <= 0f
                && AdventureZombieTargetingRules.BodyRectangle(zombie).X <= 800
                && (phase == 32 || phase == 36 || phase == 37);
        }
    }
}
