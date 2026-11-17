#nullable enable
namespace PlantsVsZombies.Adventure
{
    /// <summary>Owns PC 542E50/543040/5431D0/543520's independent head animation and integer phase counter.</summary>
    public sealed class AdventurePlantZombieState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Gets the unchanged original plant-headed zombie identifier.</summary>
        public int NativeKind { get; private set; }
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

        /// <summary>Creates an empty native child record for budgeted runtime capacity preparation.</summary>
        public AdventurePlantZombieState() : this(0)
        {
        }

        /// <summary>Applies the original species parameters after resetting a rented child state.</summary>
        /// <param name="nativeKind">The unchanged plant-headed species identifier.</param>
        /// <param name="jalapenoCounter">The original velocity-derived Jalapeno fuse.</param>
        internal void InitializePooled(int nativeKind, int jalapenoCounter)
        {
            NativeKind = nativeKind;
            Counter = nativeKind == 26 || nativeKind == 29 ? 150 : nativeKind == 28 ? jalapenoCounter : 0;
            HeadClip = nativeKind == 26 || nativeKind == 29 ? "anim_head_idle" : "anim_idle";
        }

        /// <summary>Restores all native child clocks before another actor rents this record.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            NativeKind = 0;
            Counter = 0;
            HeadClip = "anim_idle";
            HeadRate = 15f;
            HeadLoops = true;
            HeadSeconds = 0f;
            HeadBlendTicks = 0;
            ImmobilizedAtTickStart = false;
            Detached = false;
            HeadRemoved = false;
            SquashPhase = 92;
            DetachedX = 0f;
            DetachedY = 0f;
        }

        /// <summary>Declares that this value snapshot holds no actor or event references.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
        }

        /// <summary>Captures the original child's held animation for a frozen burned silhouette.</summary>
        /// <param name="pool">The optional match-local owner retaining the independent child snapshot.</param>
        /// <returns>An independent copy whose clock cannot be changed by later corpse updates.</returns>
        internal AdventurePlantZombieState Snapshot(PlantsVsZombies.Simulation.SimulationRuntimePool? pool = null)
        {
            var snapshot = pool?.Rent<AdventurePlantZombieState>() ?? new AdventurePlantZombieState();
            snapshot.NativeKind = NativeKind;
            snapshot.Counter = Counter;
            snapshot.HeadClip = HeadClip;
            snapshot.HeadRate = HeadRate;
            snapshot.HeadLoops = HeadLoops;
            snapshot.HeadSeconds = HeadSeconds;
            snapshot.HeadBlendTicks = 0;
            snapshot.Detached = Detached;
            snapshot.HeadRemoved = HeadRemoved;
            snapshot.SquashPhase = SquashPhase;
            snapshot.DetachedX = DetachedX;
            snapshot.DetachedY = DetachedY;
            return snapshot;
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
