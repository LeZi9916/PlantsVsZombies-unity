#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains managed source identity and immutable draw state alongside the numeric arena.</summary>
    internal struct DeferredSpriteCommand
    {
        /// <summary>The original logical source image.</summary>
        internal SpriteImage Image;
        /// <summary>The hierarchy object recorded at submission.</summary>
        internal GameObject? Node;
        /// <summary>The original quad width.</summary>
        internal float Width;
        /// <summary>The original quad height.</summary>
        internal float Height;
        /// <summary>The original normalized source crop.</summary>
        internal Rect Uv;
        /// <summary>The original affine transform.</summary>
        internal Matrix4x4 Matrix;
        /// <summary>The submitted source tint.</summary>
        internal Color Color;
        /// <summary>The submitted blend mode.</summary>
        internal int Mode;
        /// <summary>The submitted grayscale weight.</summary>
        internal float Grayscale;
        /// <summary>The submitted producer brightness.</summary>
        internal float ProducerAdditive;
        /// <summary>The submitted white highlight.</summary>
        internal float Additive;
        /// <summary>The submitted colored highlight.</summary>
        internal Color AddColor;
        /// <summary>The original destination clip.</summary>
        internal Rect Clip;
        /// <summary>Whether the destination enables clipping.</summary>
        internal bool ClipEnabled;
        /// <summary>Whether global scene painter order applies at submission.</summary>
        internal bool Scene;
        /// <summary>Whether source-color hit is fused with normal alpha.</summary>
        internal bool CombineHit;
        /// <summary>The first region belonging to this logical command.</summary>
        internal int FirstRegion;
        /// <summary>The number of regions belonging to this logical command.</summary>
        internal int RegionCount;
    }
}
