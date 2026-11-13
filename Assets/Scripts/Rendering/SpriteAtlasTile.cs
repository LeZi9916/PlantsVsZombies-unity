#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Maps one original pixel rectangle to the content area of an atlas page.</summary>
    [Serializable]
    public sealed class SpriteAtlasTile
    {
        /// <summary>The index in the selected layout's page table.</summary>
        public int Page;
        /// <summary>The content rectangle in bottom-left source pixels, excluding sampling padding.</summary>
        public Rect SourcePixels;
        /// <summary>The normalized bottom-left page rectangle corresponding exactly to the source content.</summary>
        public Rect TextureUv;
        /// <summary>The expected unscaled page width.</summary>
        public int PageWidth;
        /// <summary>The expected unscaled page height.</summary>
        public int PageHeight;
    }
}
