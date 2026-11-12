#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed packet cooldown.</summary>
    public struct AdventureSeedPacketCooldown
    {
        /// <summary>Stores active, refreshing.</summary>
        public bool Active, Refreshing;
        /// <summary>Stores counter, duration.</summary>
        public int Counter, Duration;
    }
}
