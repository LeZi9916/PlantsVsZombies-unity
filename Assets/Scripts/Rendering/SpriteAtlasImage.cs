#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Preserves one original image's identity, dimensions, visible geometry, and lossless tiles.</summary>
    [Serializable]
    public sealed class SpriteAtlasImage
    {
        /// <summary>The original relative path with its actual filename casing and extension.</summary>
        public string SourceIdentity = string.Empty;
        /// <summary>The original resource identifiers that resolve to this image.</summary>
        public string[] Aliases = Array.Empty<string>();
        /// <summary>The original image width before trimming or tiling.</summary>
        public int Width;
        /// <summary>The original image height before trimming or tiling.</summary>
        public int Height;
        /// <summary>Nonoverlapping visible pixel rectangles using the original bottom-left coordinate system.</summary>
        public Rect[] VisiblePixels = Array.Empty<Rect>();
        /// <summary>The losslessly packed tiles using the original bottom-left coordinate system.</summary>
        public SpriteAtlasTile[] Tiles = Array.Empty<SpriteAtlasTile>();
    }
}
