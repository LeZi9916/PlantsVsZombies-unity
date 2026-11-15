#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie sound event.</summary>
    public sealed class AdventureZombieSoundEvent
    {
        /// <summary>Stores id, foley, tick.</summary>
        public int Id, Foley, Tick;
        /// <summary>Stores whether simulation has resolved this request's original playback choices.</summary>
        public bool Resolved;
        /// <summary>Stores the selected original variation index, or minus one when native admission rejects playback.</summary>
        public int SampleIndex = -1;
        /// <summary>Stores the original explicit and randomized pitch after the native float store.</summary>
        public float ResolvedPitch;
        /// <summary>Stores the original explicit Foley pitch adjustment in semitones.</summary>
        public float Pitch;
    }
}
