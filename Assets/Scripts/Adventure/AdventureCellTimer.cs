#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure cell timer.</summary>
    public sealed class AdventureCellTimer
    {
        /// <summary>Gets or sets row.</summary>
        public int Row { get; internal set; }
        /// <summary>Gets or sets column.</summary>
        public int Column { get; internal set; }
        /// <summary>Gets or sets remaining.</summary>
        public float Remaining { get; internal set; }
        /// <summary>Gets the original grave appearance when this timer represents a gravestone.</summary>
        public AdventureGravePresentation? GravePresentation { get; internal set; }
    }
}
