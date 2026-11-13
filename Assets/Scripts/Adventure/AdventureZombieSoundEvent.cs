#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie sound event.</summary>
    public sealed class AdventureZombieSoundEvent
    {
        /// <summary>Stores id, foley, tick.</summary>
        public int Id, Foley, Tick;
        /// <summary>Stores the original explicit Foley pitch adjustment in semitones.</summary>
        public float Pitch;
    }
}
