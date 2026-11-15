#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure cherry bomb sound.</summary>
    public struct AdventureCherryBombSound
    {
        /// <summary>Stores detonation.</summary>
        public bool Detonation;
        /// <summary>Stores whether native Foley admission suppressed the fuse or primary explosion.</summary>
        public bool SuppressPrimary;
        /// <summary>Stores whether native Foley admission suppressed the juicy layer.</summary>
        public bool SuppressJuicy;
        /// <summary>Stores juicy pitch steps.</summary>
        public float JuicyPitchSteps;
    }
}
