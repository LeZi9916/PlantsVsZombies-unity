#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure potato mine state.</summary>
    public sealed class AdventurePotatoMineState
    {
        /// <summary>Stores countdown, phase ticks, glow frames, nearest distance pixels.</summary>
        public int Countdown = 1500, PhaseTicks, GlowFrames = 10, NearestDistancePixels = 1000;
        /// <summary>Stores body rate, glow time.</summary>
        public float BodyRate = 12f, GlowTime;
        /// <summary>Stores light created.</summary>
        public bool LightCreated;
    }
}
