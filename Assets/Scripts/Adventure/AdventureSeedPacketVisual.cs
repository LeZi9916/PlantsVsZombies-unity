#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed packet visual.</summary>
    public readonly struct AdventureSeedPacketVisual
    {
        // Native color multiplication, NOT saturation/grayscale conversion or a black alpha overlay.
        /// <summary>Stores base color, mask color, mask x, mask y, mask width, mask height.</summary>
        public readonly int BaseColor, MaskColor, MaskX, MaskY, MaskWidth, MaskHeight;
        /// <summary>Stores remaining fraction.</summary>
        public readonly float RemainingFraction;
        /// <summary>Stores draw mask, draw cost, draw cooldown number.</summary>
        public readonly bool DrawMask, DrawCost, DrawCooldownNumber;
        /// <summary>Initializes adventure seed packet visual.</summary>
        /// <param name = "shade">The shade.</param>
        /// <param name = "remaining">The remaining.</param>
        /// <param name = "drawCost">The draw cost.</param>
        public AdventureSeedPacketVisual(int shade, float remaining, bool drawCost)
        {
            BaseColor = shade;
            MaskColor = 64;
            MaskX = 0;
            MaskY = 0;
            MaskWidth = 50;
            RemainingFraction = remaining;
            DrawMask = remaining > 0f;
            MaskHeight = DrawMask ? (int)(remaining * 68f + .5f) + 2 : 0;
            DrawCost = drawCost;
            DrawCooldownNumber = false;
        }
    }
}
