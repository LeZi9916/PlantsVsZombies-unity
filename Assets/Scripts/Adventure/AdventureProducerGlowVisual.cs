#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure producer glow visual.</summary>
    public struct AdventureProducerGlowVisual
    {
        /// <summary>Stores enabled.</summary>
        public readonly bool Enabled;
        /// <summary>Stores flash ticks.</summary>
        public readonly int FlashTicks;
        /// <summary>Stores additive rgb.</summary>
        public readonly byte AdditiveRgb;
        /// <summary>Gets or sets source additive.</summary>
        public float SourceAdditive
        {
            get
            {
                return AdditiveRgb / 255f;
            }
        }

        /// <summary>Initializes adventure producer glow visual.</summary>
        /// <param name = "enabled">The enabled.</param>
        /// <param name = "flashTicks">The flash ticks.</param>
        /// <param name = "additiveRgb">The additive rgb.</param>
        public AdventureProducerGlowVisual(bool enabled, int flashTicks, byte additiveRgb)
        {
            Enabled = enabled;
            FlashTicks = flashTicks;
            AdditiveRgb = additiveRgb;
        }
    }
}
