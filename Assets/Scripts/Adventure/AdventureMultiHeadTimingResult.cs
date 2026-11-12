#nullable enable


namespace PlantsVsZombies.Adventure
{
    /// <summary>Reports the heads that started, stopped, or fired during one update.</summary>
    public readonly struct AdventureMultiHeadTimingResult
    {
        /// <summary>Gets the state after this update.</summary>
        public AdventureMultiHeadTimingState State { get; }

        /// <summary>Gets the heads whose shooting animation started or restarted.</summary>
        public AdventureShootingHeads StartedHeads { get; }

        /// <summary>Gets the heads forced back to idle.</summary>
        public AdventureShootingHeads StoppedHeads { get; }

        /// <summary>Gets the heads that fired from the old pending countdown.</summary>
        public AdventureShootingHeads FiredHeads { get; }

        /// <summary>Initializes one update result.</summary>
        /// <param name="state">The state after this update.</param>
        /// <param name="startedHeads">The heads starting a shooting animation.</param>
        /// <param name="stoppedHeads">The heads returning to idle.</param>
        /// <param name="firedHeads">The heads firing their scheduled projectile.</param>
        internal AdventureMultiHeadTimingResult(AdventureMultiHeadTimingState state, AdventureShootingHeads startedHeads,
            AdventureShootingHeads stoppedHeads, AdventureShootingHeads firedHeads)
        {
            State = state;
            StartedHeads = startedHeads;
            StoppedHeads = stoppedHeads;
            FiredHeads = firedHeads;
        }
    }
}
