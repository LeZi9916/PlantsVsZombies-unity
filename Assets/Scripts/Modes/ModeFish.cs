#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode fish.</summary>
    public sealed class ModeFish
    {
        /// <summary>Stores x, y, angle, speed, phase remaining.</summary>
        public float X, Y, Angle, Speed, PhaseRemaining;
        /// <summary>Stores swim phase.</summary>
        public int SwimPhase;
        /// <summary>Stores hunger, sun timer.</summary>
        public float Hunger = 27f, SunTimer = 10f;
    }
}
