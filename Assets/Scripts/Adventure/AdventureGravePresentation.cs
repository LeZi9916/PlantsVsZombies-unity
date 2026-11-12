#nullable enable

using System;
using PlantsVsZombies.BoardUI;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves the original gravestone atlas choice and native emergence counter.</summary>
    /// <remarks>The local PC DrawGraveStone at 0x4589E0 draws the full mound cell and offsets both layers by -9 pixels.</remarks>
    public sealed class AdventureGravePresentation
    {
        /// <summary>Gets the original twenty-way cell artwork choice.</summary>
        public int Look { get; }
        /// <summary>Gets the original horizontal cell jitter in pixels.</summary>
        public int OffsetX { get; }
        /// <summary>Gets the original vertical cell jitter in pixels.</summary>
        public int OffsetY { get; }
        /// <summary>Gets the native emergence counter, initially between -49 and zero.</summary>
        public int Counter { get; private set; }
        /// <summary>Gets the elapsed particle time since gravestones were enabled.</summary>
        public float Age { get; private set; }
        /// <summary>Gets or sets whether this is the board's original special gravestone.</summary>
        public bool Special { get; internal set; }

        /// <summary>Creates the original per-cell presentation without altering gameplay randomness.</summary>
        /// <param name="look">The original random cell look in the range zero through nineteen.</param>
        /// <param name="offsetX">The original horizontal jitter, from -5 through 4.</param>
        /// <param name="offsetY">The original vertical jitter, from -5 through 4.</param>
        /// <param name="delay">The original emergence delay, from zero through forty-nine ticks.</param>
        public AdventureGravePresentation(int look, int offsetX, int offsetY, int delay)
        {
            Look = look;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Counter = -delay;
        }

        /// <summary>Advances one original hundredth-of-a-second grid-item update.</summary>
        internal void Tick()
        {
            Counter = Math.Min(100, Counter + 1);
            Age += .01f;
        }

        /// <summary>Selects the original atlas row for a normal, top-row, or special gravestone.</summary>
        /// <param name="row">The lawn row containing the grave.</param>
        /// <returns>The row in the original five-column, four-row atlas.</returns>
        public int AtlasRow(int row)
        {
            return row == 0 ? 1 : Special ? 0 : 2 + Look % 2;
        }

        /// <summary>Samples the original unscaled rising stone image and its bottom clipping.</summary>
        /// <param name="row">The lawn row containing the grave.</param>
        /// <param name="width">The original atlas cell width.</param>
        /// <param name="height">The original atlas cell height.</param>
        /// <param name="eaten">The original vertical clipping amount while a Grave Buster eats.</param>
        /// <returns>The source and relative destination rectangles for the visible stone.</returns>
        public NativeBlit Stone(int row, int width, int height, int eaten = 0)
        {
            var time = AdventureIntroPresentation.Curve(0, 100, Counter, 1000, 0, 4);
            var rise = AdventureIntroPresentation.Curve(0, 1000, time, height, 0, 4);
            var inset = AdventureIntroPresentation.Curve(0, 50, time, 0, 14, 4);
            var visible = Counter > 0 ? Math.Max(0, rise - inset - eaten) : 0;
            return new NativeBlit(new NativeRect(Look % 5 * width, AtlasRow(row) * height + eaten, width, visible),
                new NativeRect(-4 + OffsetX, height + OffsetY - 9 - rise + eaten, width, visible));
        }

        /// <summary>Samples the separately drawn original soil mound during emergence.</summary>
        /// <param name="row">The lawn row containing the grave.</param>
        /// <param name="width">The original mound atlas cell width.</param>
        /// <param name="height">The original mound atlas cell height.</param>
        /// <returns>The source and relative destination rectangles for the soil mound.</returns>
        public NativeBlit Mound(int row, int width, int height)
        {
            var time = AdventureIntroPresentation.Curve(0, 100, Counter, 1000, 0, 4);
            var rise = AdventureIntroPresentation.Curve(500, 1000, time, height, 0, 4);
            var visible = Counter > 0 ? rise : 0;
            return new NativeBlit(new NativeRect(Look % 5 * width, AtlasRow(row) * height, width, visible),
                new NativeRect(-4 + OffsetX, height + OffsetY - 9 - rise, width, visible));
        }
    }
}
