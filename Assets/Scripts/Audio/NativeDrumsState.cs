#nullable enable

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines native drums state.</summary>
    public enum NativeDrumsState
    {
        /// <summary>Identifies off.</summary>
        Off = 0,
        /// <summary>Identifies queued on.</summary>
        QueuedOn = 1,
        /// <summary>Identifies on.</summary>
        On = 2,
        /// <summary>Identifies queued off.</summary>
        QueuedOff = 3,
        /// <summary>Identifies fading.</summary>
        Fading = 4
    }
}
