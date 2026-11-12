#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains a native plant sound milestone after an atomic simulation action removes its actor.</summary>
    internal struct AdventurePlantSoundCue
    {
        /// <summary>Stores native ticks remaining until the sound is requested.</summary>
        internal int Ticks;
        /// <summary>Stores the original PC Foley identifier.</summary>
        internal int Foley;
    }
}
