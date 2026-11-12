#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sky sun tick.</summary>
    public readonly struct SkySunTick
    {
        /// <summary>Gets or sets state.</summary>
        public SkySunState State { get; }
        /// <summary>Gets or sets spawned.</summary>
        public bool Spawned { get; }
        /// <summary>Gets or sets x.</summary>
        public int X { get; }
        /// <summary>Gets or sets y.</summary>
        public int Y { get; }
        /// <summary>Gets or sets kind.</summary>
        public SunKind Kind { get; }
        /// <summary>Gets or sets motion type.</summary>
        public int MotionType { get; }

        /// <summary>Initializes sky sun tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "spawned">The spawned.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "kind">The kind.</param>
        internal SkySunTick(SkySunState state, bool spawned, int x, SunKind kind)
        {
            State = state;
            Spawned = spawned;
            X = x;
            Y = spawned ? 60 : 0;
            Kind = kind;
            MotionType = 0;
        }
    }
}
