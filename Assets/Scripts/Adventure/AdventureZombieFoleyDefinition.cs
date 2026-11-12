#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie foley definition.</summary>
    public sealed class AdventureZombieFoleyDefinition
    {
        /// <summary>Stores pitch range.</summary>
        public float PitchRange;
        /// <summary>Stores samples.</summary>
        public string[] Samples;
        /// <summary>Initializes adventure zombie foley definition.</summary>
        /// <param name = "pitch">The pitch.</param>
        /// <param name = "samples">The samples.</param>
        public AdventureZombieFoleyDefinition(float pitch, string[] samples)
        {
            PitchRange = pitch;
            Samples = samples;
        }
    }
}
