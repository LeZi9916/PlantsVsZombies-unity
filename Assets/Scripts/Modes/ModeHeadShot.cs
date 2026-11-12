#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode head shot.</summary>
    public sealed class ModeHeadShot
    {
        /// <summary>Stores x, row.</summary>
        public float X, Row;
        /// <summary>Stores damage.</summary>
        public float Damage = 20f;
        /// <summary>Stores hypnotized.</summary>
        public bool Hypnotized;
    }
}
