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
        /// <summary>Retains the original five-hundred-update squished picture after a boss ball contact.</summary>
        public int SquishedTicks;
        /// <summary>Retains whether the original mower reanimation was already running when squished.</summary>
        public bool SquishedWhileActive;
        /// <summary>Stores active.</summary>
        public bool Active;
    }
}
