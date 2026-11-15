#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie foley definition.</summary>
    public sealed class AdventureZombieFoleyDefinition
    {
        /// <summary>Stores pitch range.</summary>
        public float PitchRange;
        /// <summary>Stores the original looping, admission and no-repeat flags from PC 74B3B8.</summary>
        public int Flags;
        /// <summary>Stores samples.</summary>
        public string[] Samples;
        /// <summary>Initializes adventure zombie foley definition.</summary>
        /// <param name = "pitch">The pitch.</param>
        /// <param name = "samples">The samples.</param>
        /// <param name="flags">The original PC Foley admission and selection flags.</param>
        public AdventureZombieFoleyDefinition(float pitch, string[] samples, int flags = 0)
        {
            PitchRange = pitch;
            Samples = samples;
            Flags = flags;
        }
    }
}
