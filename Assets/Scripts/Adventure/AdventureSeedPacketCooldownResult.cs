#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed packet cooldown result.</summary>
    public readonly struct AdventureSeedPacketCooldownResult
    {
        /// <summary>Stores state.</summary>
        public readonly AdventureSeedPacketCooldown State;
        /// <summary>Stores completed.</summary>
        public readonly bool Completed;
        /// <summary>Initializes adventure seed packet cooldown result.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "completed">The completed.</param>
        public AdventureSeedPacketCooldownResult(AdventureSeedPacketCooldown state, bool completed)
        {
            State = state;
            Completed = completed;
        }
    }
}
