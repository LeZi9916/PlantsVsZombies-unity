#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure bowling nut.</summary>
    public sealed class AdventureBowlingNut
    {
        /// <summary>Gets or sets native motion initialized.</summary>
        public bool NativeMotionInitialized { get; internal set; }
        /// <summary>Gets or sets native pixel x.</summary>
        public int NativePixelX { get; internal set; }
        /// <summary>Gets or sets native pixel y.</summary>
        public int NativePixelY { get; internal set; }
        /// <summary>Gets or sets native direction.</summary>
        public int NativeDirection { get; internal set; }
        /// <summary>Gets or sets native age.</summary>
        public float NativeAge { get; internal set; }

        /// <summary>Stores native tick remainder.</summary>
        internal float _nativeTickRemainder;
        /// <summary>Gets or sets row.</summary>
        public int Row { get; internal set; }
        /// <summary>Gets or sets x.</summary>
        public float X { get; internal set; }
        /// <summary>Gets or sets explosive.</summary>
        public bool Explosive { get; internal set; }

        /// <summary>Stores last hit id.</summary>
        internal int _lastHitId;
        /// <summary>Stores bounce delay.</summary>
        internal float _bounceDelay;
    }
}
