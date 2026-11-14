#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC 542D60's thrown imp velocity and previous native roof floor.</summary>
    public sealed class AdventureThrownImpState
    {
        /// <summary>Stores the original vertical velocity, reduced by .05 each update.</summary>
        public float VelocityZ;
        /// <summary>Stores the previous unscaled native row position for roof rebasing.</summary>
        public float NativeY;
    }
}
