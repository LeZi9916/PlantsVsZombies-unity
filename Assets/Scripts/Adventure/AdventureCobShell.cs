#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure cob shell.</summary>
    public sealed class AdventureCobShell
    {
        /// <summary>Gets or sets source plant id.</summary>
        public int SourcePlantId { get; internal set; }
        /// <summary>Gets or sets target x.</summary>
        public float TargetX { get; internal set; }
        /// <summary>Gets or sets target row.</summary>
        public float TargetRow { get; internal set; }
        /// <summary>Gets or sets flight remaining.</summary>
        public float FlightRemaining { get; internal set; }
        /// <summary>Retains the original descending projectile's top-left floor y, including its roof column.</summary>
        public float TargetNativeY { get; internal set; }
        /// <summary>Retains the original rising image center y before the projectile's altitude is applied.</summary>
        public float LaunchNativeY { get; internal set; }
        /// <summary>Gets or sets launch x.</summary>
        public float LaunchX { get; internal set; }
        /// <summary>Gets or sets launch row.</summary>
        public float LaunchRow { get; internal set; }
    }
}
