#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed packet flash.</summary>
    public readonly struct AdventureSeedPacketFlash
    {
        /// <summary>Stores spawn.</summary>
        public readonly bool Spawn;
        /// <summary>Stores x, y.</summary>
        public readonly float X, Y;
        /// <summary>Stores native effect, render order, duration ticks.</summary>
        public const int NativeEffect = 52, RenderOrder = 100002, DurationTicks = 25;
        /// <summary>Stores asset.</summary>
        public const string Asset = "SeedPacketFlash";
        // X/Y are packet top-left. XML supplies emitter offset25/35 and the original alpha curve.
        /// <summary>Initializes adventure seed packet flash.</summary>
        /// <param name = "spawn">The spawn.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public AdventureSeedPacketFlash(bool spawn, float x, float y)
        {
            Spawn = spawn;
            X = x;
            Y = y;
        }
    }
}
