#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode head shot.</summary>
    public sealed class ModeHeadShot
    {
        /// <summary>Stores x, row.</summary>
        public float X, Row;
        /// <summary>Stores the original projectile's pixel x/y before sprite placement.</summary>
        public float NativeX, NativeY;
        /// <summary>Stores the previous original projectile pixel x for rendering interpolation.</summary>
        public float PreviousNativeX;
        /// <summary>Stores damage.</summary>
        public float Damage = 20f;
        /// <summary>Stores hypnotized.</summary>
        public bool Hypnotized;
    }
}
