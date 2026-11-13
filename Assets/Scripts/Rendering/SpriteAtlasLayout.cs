#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Describes a complete, independently usable atlas layout for one maximum page size.</summary>
    [Serializable]
    public sealed class SpriteAtlasLayout
    {
        /// <summary>The largest permitted page edge in this layout.</summary>
        public int MaximumPageSize;
        /// <summary>The Resources paths of the texture pages, loaded individually on demand.</summary>
        public string[] Pages = Array.Empty<string>();
        /// <summary>The source images and their page mappings.</summary>
        public SpriteAtlasImage[] Images = Array.Empty<SpriteAtlasImage>();
    }
}
