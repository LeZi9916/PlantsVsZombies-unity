#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the local PC Squash's look, launch, flight, impact, and flattened body playback.</summary>
    public sealed class AdventureSquashState
    {
        /// <summary>Stores the PC plant state: idle 0, look 3, prelaunch 4, rising 5, falling 6, and landed 7.</summary>
        public int NativePhase;
        /// <summary>Stores native updates remaining before the next original state transition.</summary>
        public int Countdown;
        /// <summary>Stores updates elapsed since the current authored body clip began.</summary>
        public int AnimationTicks;
        /// <summary>Stores the original stable identifier of the initially selected zombie.</summary>
        public int TargetZombieId;
        /// <summary>Stores the integer native horizontal landing coordinate of the plant's left edge.</summary>
        public int TargetX;
        /// <summary>Stores the current integer native body origin, independently of its original planting cell.</summary>
        public int NativeX, NativeY;
        /// <summary>Stores the selected original body animation.</summary>
        public string Animation = "anim_idle";
        /// <summary>Stores the authored animation rate, or zero while the ordinary idle rate owns playback.</summary>
        public float Rate;
        /// <summary>Stores the original clip transition's blend duration in native updates.</summary>
        public int BlendTicks;

        /// <summary>Advances the body playback and state countdown once before UpdateSquash runs.</summary>
        public void AdvanceClock()
        {
            AnimationTicks++;
            Countdown = Math.Max(0, Countdown - 1);
        }

        /// <summary>Starts an original state and optionally restarts its authored body clip.</summary>
        /// <param name="phase">The original plant state identifier.</param>
        /// <param name="countdown">The native updates until this state completes.</param>
        /// <param name="animation">The new clip, or null to retain the current playback.</param>
        /// <param name="rate">The source frame rate selected by PlayBodyReanim.</param>
        /// <param name="blendTicks">The original transition blend duration.</param>
        public void Enter(int phase, int countdown, string? animation = null, float rate = 0f, int blendTicks = 0)
        {
            NativePhase = phase;
            Countdown = countdown;
            if (animation != null)
            {
                Animation = animation;
                Rate = rate;
                BlendTicks = blendTicks;
                AnimationTicks = 0;
            }
        }

        /// <summary>Samples the selected clip without changing its fixed simulation counter.</summary>
        /// <param name="remainder">The nonnegative render remainder after the latest native update.</param>
        /// <returns>The elapsed body animation time in seconds.</returns>
        public float SampleSeconds(float remainder)
        {
            return AnimationTicks * .01f + Math.Max(0f, remainder);
        }
    }
}
