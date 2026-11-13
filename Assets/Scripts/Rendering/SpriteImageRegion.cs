#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Describes one visible, nonoverlapping part of a logical image after source cropping.</summary>
    public readonly struct SpriteImageRegion
    {
        /// <summary>Gets the atlas page or external texture sampled by this region.</summary>
        public Texture Texture { get; }
        /// <summary>Gets the positive bottom-left normalized rectangle in the complete original image.</summary>
        public Rect SourceUv { get; }
        /// <summary>Gets the exact original source-pixel rectangle, including virtual Clamp-extension pixels.</summary>
        public Rect SourcePixels { get; }
        /// <summary>Gets the matching positive bottom-left normalized rectangle in <see cref="Texture"/>.</summary>
        public Rect TextureUv { get; }

        /// <summary>Creates one paired source and texture rectangle.</summary>
        /// <param name="texture">The texture containing the region's texels.</param>
        /// <param name="sourceUv">The normalized rectangle in the complete original image.</param>
        /// <param name="textureUv">The normalized rectangle in the sampled texture.</param>
        /// <param name="sourcePixels">The exact original source-pixel edges before normalization.</param>
        internal SpriteImageRegion(Texture texture, Rect sourceUv, Rect textureUv, Rect sourcePixels)
        {
            Texture = texture;
            SourceUv = sourceUv;
            SourcePixels = sourcePixels;
            TextureUv = textureUv;
        }
    }
}
