#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains the logical source quad independently of its combined GPU mesh.</summary>
    internal sealed class ObjectSprite
    {
        /// <summary>Gets or sets the optional stable hierarchy node that owns this source sprite.</summary>
        internal GameObject? GameObject { get; set; }
        /// <summary>Gets or sets the source image before atlas remapping and transparent trimming.</summary>
        internal SpriteImage? SourceImage { get; set; }
        /// <summary>Gets or sets the source blending operation before GPU batch grouping.</summary>
        internal int Mode { get; set; }
        /// <summary>Gets or sets the original grayscale effect amount.</summary>
        internal float Grayscale { get; set; }
        /// <summary>Gets or sets the original source-colored producer brightness.</summary>
        internal float ProducerAdditive { get; set; }
        /// <summary>Gets or sets the original white textured-hit multiplier.</summary>
        internal float Additive { get; set; }
        /// <summary>Gets or sets the original colored textured-hit multiplier.</summary>
        internal Color AddColor { get; set; }
        /// <summary>Gets or sets the accepted top-left destination rectangle.</summary>
        internal Rect Clip { get; set; }
        /// <summary>Gets or sets whether destination clipping applies to the source command.</summary>
        internal bool ClipEnabled { get; set; }
        /// <summary>Gets the original four transformed corners before destination clipping.</summary>
        internal Vector3[] Vertices { get; } = new Vector3[4];
        /// <summary>Gets the original source UV rectangle before atlas remapping.</summary>
        internal Vector2[] TextureCoordinates { get; } = new Vector2[4];
        /// <summary>Gets the source tint retained at each original corner.</summary>
        internal Color[] Colors { get; } = new Color[4];
    }
}
