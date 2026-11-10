#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines mower.</summary>
    public sealed class Mower
    {
        /// <summary>Stores row.</summary>
        public int Row;
        /// <summary>Stores x.</summary>
        public float X;
        /// <summary>Stores available.</summary>
        public bool Available = true;
        /// <summary>Stores active.</summary>
        public bool Active;
    }
}
