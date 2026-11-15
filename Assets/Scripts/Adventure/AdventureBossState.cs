#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC 551ED0's independent boss counters, action phase and original reanimation clock.</summary>
    public sealed class AdventureBossState
    {
        /// <summary>Gets the native boss phase, from Enter78 through HeadLeave90.</summary>
        public int Phase { get; internal set; } = 78;
        /// <summary>Gets the native damage mode, selected at eighty and fifty percent health.</summary>
        public int Mode { get; internal set; }
        /// <summary>Gets the boss age in native updates used by the original summon table.</summary>
        public int Age { get; internal set; }
        /// <summary>Gets the shared phase countdown.</summary>
        public int PhaseCountdown { get; internal set; }
        /// <summary>Gets the independent summon countdown.</summary>
        public int SummonCountdown { get; internal set; } = 500;
        /// <summary>Gets the independent bungee countdown, disabled until the original damage transition.</summary>
        public int BungeeCountdown { get; internal set; } = -1;
        /// <summary>Gets the independent stomp countdown, disabled until the original RV landing.</summary>
        public int StompCountdown { get; internal set; } = -1;
        /// <summary>Gets the independent head countdown.</summary>
        public int HeadCountdown { get; internal set; } = 5000;
        /// <summary>Gets the selected native target row.</summary>
        public int TargetRow { get; internal set; }
        /// <summary>Gets the selected native target column.</summary>
        public int TargetColumn { get; internal set; }
        /// <summary>Gets the head's randomly selected ball row.</summary>
        public int BallRow { get; internal set; }
        /// <summary>Gets whether the selected native ball is ice rather than fire.</summary>
        public bool BallIsIce { get; internal set; }
        /// <summary>Gets the original current body animation marker.</summary>
        public string Animation { get; internal set; } = "anim_idle";
        /// <summary>Gets the original animation frame rate.</summary>
        public float AnimationRate { get; internal set; } = 12f;
        /// <summary>Gets the elapsed seconds in the current original animation.</summary>
        public float AnimationSeconds { get; internal set; }
        /// <summary>Gets the original driver's independent current animation marker.</summary>
        public string DriverAnimation { get; internal set; } = "anim_idle";
        /// <summary>Gets the driver's native current animation rate.</summary>
        public float DriverRate { get; internal set; } = 18f;
        /// <summary>Gets the driver's independently accumulated source-frame clock.</summary>
        public float DriverFrames { get; internal set; }
        /// <summary>Gets the previous normalized animation time used by native contact markers.</summary>
        internal float PreviousTime;
        /// <summary>Gets the normalized animation time used by native contact markers.</summary>
        internal float Time;
        /// <summary>Counts completed native animation loops independently of action phase changes.</summary>
        internal int AnimationLoopCount;
        /// <summary>Retains the original three bungee followers, in spawn order.</summary>
        internal readonly int[] BungeeIds = new int[3];
        /// <summary>Gets the native freeze countdown applied to this boss.</summary>
        public int FrozenTicks { get; internal set; }
        /// <summary>Gets the native chill countdown applied to this boss.</summary>
        public int ChilledTicks { get; internal set; }
        /// <summary>Gets whether the current native body animation has completed.</summary>
        internal bool Finished => Time >= 1f || AnimationLoopCount > 0;
        /// <summary>Gets whether the current native head is exposed to attacks.</summary>
        public bool HeadVulnerable => Phase >= 86 && Phase <= 90;

        /// <summary>Gets frame counts from the unchanged original Zombie_boss.reanim marker tracks.</summary>
        /// <returns>The number of frames in the current native animation.</returns>
        internal int FrameCount()
        {
            if (Animation.StartsWith("anim_spawn_", StringComparison.Ordinal))
            {
                return TargetRow == 0 ? 22 : 21;
            }

            if (Animation.StartsWith("anim_stomp_", StringComparison.Ordinal))
            {
                return TargetRow < 2 ? 31 : 29;
            }

            return Animation == "anim_bungee_1_enter" ? 25 : Animation == "anim_bungee_1_leave" ? 18
                : Animation == "anim_RV_1" ? 38 : Animation == "anim_head_enter" ? 57
                : Animation.StartsWith("anim_head_attack_", StringComparison.Ordinal) ? BallRow == 4 ? 43 : 40
                : Animation == "anim_head_leave" ? 45 : Animation == "anim_head_idle" ? 17 : 15;
        }

        /// <summary>Advances the native body clock before contact events are inspected.</summary>
        internal void AdvanceAnimation()
        {
            PreviousTime = Time;
            if (FrozenTicks > 0)
            {
                DriverRate = 0f;
            }
            else if (ChilledTicks > 0)
            {
                DriverRate = 6f;
            }
            else if (DriverRate == 0f)
            {
                DriverRate = 12f;
            }

            DriverFrames += DriverRate * .01f;
            if (FrozenTicks > 0)
            {
                return;
            }

            var rate = AnimationRate * (ChilledTicks > 0 ? .5f : 1f);
            AnimationSeconds += .01f * (ChilledTicks > 0 ? .5f : 1f);
            Time += rate * .01f / FrameCount();
            if (Animation == "anim_idle" || Animation == "anim_head_idle")
            {
                var loops = (int)Math.Floor(Time);
                AnimationLoopCount += loops;
                Time -= loops;
            }
        }

        /// <summary>Checks the original normalized contact marker crossing.</summary>
        /// <param name="marker">The original marker fraction.</param>
        /// <returns>Whether the current update crosses the marker once.</returns>
        internal bool Triggered(float marker)
        {
            return PreviousTime <= marker && Time > marker;
        }

        /// <summary>Starts an original boss action with its own body clock.</summary>
        /// <param name="phase">The original boss phase identifier.</param>
        /// <param name="animation">The original animation marker.</param>
        /// <param name="rate">The original frame rate.</param>
        internal void Enter(int phase, string animation, float rate)
        {
            Phase = phase;
            Animation = animation;
            AnimationRate = rate;
            AnimationSeconds = 0f;
            PreviousTime = 0f;
            Time = 0f;
            AnimationLoopCount = 0;
        }
    }
}
