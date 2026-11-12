#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure notice.</summary>
    public sealed class AdventureNotice
    {
        /// <summary>Gets or sets kind.</summary>
        public AdventureNoticeKind Kind { get; internal set; }
        /// <summary>Gets or sets value.</summary>
        public int Value { get; internal set; }
        /// <summary>Gets or sets row.</summary>
        public int Row { get; internal set; }
        /// <summary>Gets or sets column.</summary>
        public int Column { get; internal set; }
    }
}
