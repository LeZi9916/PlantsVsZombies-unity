#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>Defines wave schedule event.</summary>
    public sealed class WaveScheduleEvent
    {
        /// <summary>Gets or sets kind.</summary>
        public WaveScheduleEventKind Kind { get; internal set; }
        /// <summary>Gets or sets wave index.</summary>
        public int WaveIndex { get; internal set; }
        /// <summary>Gets or sets tick.</summary>
        public long Tick { get; internal set; }
    }
}
