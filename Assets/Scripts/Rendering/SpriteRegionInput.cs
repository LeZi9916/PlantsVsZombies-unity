#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Snapshots only numeric source mapping and destination state for one atlas region.</summary>
    internal struct SpriteRegionInput
    {
        /// <summary>The affine source-to-destination transform.</summary>
        internal Matrix4x4 Matrix;
        /// <summary>The exact source-pixel rectangle.</summary>
        internal Rect SourcePixels;
        /// <summary>The matching positive atlas UV rectangle.</summary>
        internal Rect TextureUv;
        /// <summary>The original source crop in normalized coordinates.</summary>
        internal Rect SourceUv;
        /// <summary>The source image dimensions before packing.</summary>
        internal Vector2 ImageSize;
        /// <summary>The requested local quad dimensions.</summary>
        internal Vector2 Size;
        /// <summary>The original inclusive clip rectangle.</summary>
        internal Rect Clip;
        /// <summary>The exact horizontal destination sample spacing.</summary>
        internal double PixelStepX;
        /// <summary>The exact vertical destination sample spacing.</summary>
        internal double PixelStepY;
        /// <summary>Whether the managed runtime evaluates float expressions with expanded intermediates.</summary>
        internal bool ExpandedPrecision;
        /// <summary>Whether the original destination enables clipping.</summary>
        internal bool ClipEnabled;
    }
}
