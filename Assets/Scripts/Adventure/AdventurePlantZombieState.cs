#nullable enable
namespace PlantsVsZombies.Adventure
{
    /// <summary>Owns PC 542E50/543040/5431D0/543520's independent head animation and integer phase counter.</summary>
    public sealed class AdventurePlantZombieState
    {
        /// <summary>Gets the unchanged original plant-headed zombie identifier.</summary>
        public int NativeKind { get; }
        /// <summary>Gets or sets the original phase counter, decremented once per non-immobilized update.</summary>
        public int Counter { get; internal set; }
        /// <summary>Gets the original once-and-hold or looping child animation.</summary>
        public string HeadClip { get; private set; }
        /// <summary>Gets the original child animation rate before a butter pause.</summary>
        public float HeadRate { get; private set; } = 15f;
        /// <summary>Gets whether the current child clip loops.</summary>
        public bool HeadLoops { get; private set; } = true;
        /// <summary>Gets or sets the independently advanced child animation age.</summary>
        public float HeadSeconds { get; internal set; }
        /// <summary>Gets the original child pose transition duration.</summary>
        public int HeadBlendTicks { get; private set; }
        /// <summary>Retains the original immobilization gate before this update expires cold or butter counters.</summary>
        internal bool ImmobilizedAtTickStart;
        /// <summary>Gets whether the Squash head has detached from its body.</summary>
        public bool Detached { get; internal set; }
        /// <summary>Gets whether the detached head has been destroyed.</summary>
        public bool HeadRemoved { get; internal set; }
        /// <summary>Gets or sets the original Squash phase, 92 through 95.</summary>
        public int SquashPhase { get; internal set; } = 92;
        /// <summary>Gets or sets the detached original source x coordinate.</summary>
        public float DetachedX { get; internal set; }
        /// <summary>Gets or sets the detached original source y coordinate.</summary>
        public float DetachedY { get; internal set; }

        /// <summary>Creates the original independent child state after native body allocation.</summary>
        /// <param name="nativeKind">The original plant-headed species, 26 through 31.</param>
        /// <param name="jalapenoCounter">The original velocity-derived Jalapeno fuse.</param>
        internal AdventurePlantZombieState(int nativeKind, int jalapenoCounter = 0)
        {
            NativeKind = nativeKind;
            Counter = nativeKind == 26 || nativeKind == 29 ? 150 : nativeKind == 28 ? jalapenoCounter : 0;
            HeadClip = nativeKind == 26 || nativeKind == 29 ? "anim_head_idle" : "anim_idle";
        }

        /// <summary>Captures the original child's held animation for a frozen burned silhouette.</summary>
        /// <returns>An independent copy whose clock cannot be changed by later corpse updates.</returns>
        internal AdventurePlantZombieState Snapshot()
        {
            return new AdventurePlantZombieState(NativeKind)
            {
                Counter = Counter, HeadClip = HeadClip, HeadRate = HeadRate, HeadLoops = HeadLoops,
                HeadSeconds = HeadSeconds, HeadBlendTicks = 0, Detached = Detached, HeadRemoved = HeadRemoved,
                SquashPhase = SquashPhase, DetachedX = DetachedX, DetachedY = DetachedY
            };
        }

        /// <summary>Starts a native child clip without coupling it to the body's walking clock.</summary>
        /// <param name="clip">The original animation marker.</param>
        /// <param name="rate">The original frames per second.</param>
        /// <param name="loop">Whether the original clip loops.</param>
        /// <param name="blendTicks">The original transition blend duration.</param>
        internal void Play(string clip, float rate, bool loop, int blendTicks)
        {
            HeadClip = clip;
            HeadRate = rate;
            HeadLoops = loop;
            HeadBlendTicks = blendTicks;
            HeadSeconds = 0f;
        }
    }
}
