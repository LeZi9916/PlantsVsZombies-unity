#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines sun.</summary>
    public sealed class Sun
    {
        /// <summary>Stores presentation native, presentation from plant, presentation special expansion.</summary>
        public bool PresentationNative, PresentationFromPlant, PresentationSpecialExpansion;
        /// <summary>Stores presentation motion.</summary>
        public global::PlantsVsZombies.Adventure.AdventureSunMotion PresentationMotion;
        /// <summary>Stores presentation pickup.</summary>
        public global::PlantsVsZombies.Sun.SunPickupState PresentationPickup;
        /// <summary>Stores presentation previous x, presentation previous y, presentation scale.</summary>
        public float PresentationPreviousX, PresentationPreviousY, PresentationScale = 1f;
        /// <summary>Stores presentation velocity x, presentation velocity y, presentation landing y.</summary>
        public float PresentationVelocityX, PresentationVelocityY, PresentationLandingY;
        /// <summary>Stores value.</summary>
        public int Value = 25;
        /// <summary>Stores x.</summary>
        public float X;
        /// <summary>Stores y.</summary>
        public float Y;
        /// <summary>Stores target y.</summary>
        public float TargetY;
        /// <summary>Stores age.</summary>
        public float Age;

        /// <summary>Restores construction defaults before a retired sun is spawned again.</summary>
        internal void Reset()
        {
            PresentationNative = false;
            PresentationFromPlant = false;
            PresentationSpecialExpansion = false;
            PresentationMotion = default;
            PresentationPickup = default;
            PresentationPreviousX = 0f;
            PresentationPreviousY = 0f;
            PresentationScale = 1f;
            PresentationVelocityX = 0f;
            PresentationVelocityY = 0f;
            PresentationLandingY = 0f;
            Value = 25;
            X = 0f;
            Y = 0f;
            TargetY = 0f;
            Age = 0f;
        }
    }
}
