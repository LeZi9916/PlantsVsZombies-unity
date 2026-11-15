#nullable enable
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Retains one sampled fixed-pose image without retaining atlas pages or rasterizing a second texture.</summary>
    internal struct NativeThumbnailPart
    {
        /// <summary>The logical image whose provider continues to manage page residency.</summary>
        internal SpriteImage Image;
        /// <summary>The original drawable width.</summary>
        internal float Width;
        /// <summary>The original drawable height.</summary>
        internal float Height;
        /// <summary>The original normalized source coordinates.</summary>
        internal Rect Uv;
        /// <summary>The sampled local transform at unit scale and zero origin.</summary>
        internal Matrix4x4 Matrix;
        /// <summary>The sampled local tint and opacity before caller modulation.</summary>
        internal Color Color;
        /// <summary>The original blend operation of the sampled image.</summary>
        internal int Pass;
        /// <summary>Whether the normal image and its source-colored highlight share one submission.</summary>
        internal bool CombineHit;
    }
}
