#nullable enable
namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains resolved metadata and page residency for a single provider-owned logical image.</summary>
    internal sealed class SpriteImageBinding
    {
        /// <summary>Gets the stable provider-local identity used by cropped-region caches.</summary>
        internal int Id { get; }
        /// <summary>The image metadata from the last successfully resolved complete layout.</summary>
        internal SpriteAtlasImage Image;
        /// <summary>The layout generation supplying the metadata.</summary>
        internal int LayoutVersion;
        /// <summary>The most recent cropped-region slot, which may be reassigned by the provider's bounded cache.</summary>
        internal SpriteRegionCacheEntry? RecentRegions;
        /// <summary>The complete frame whose required pages were last prepared.</summary>
        internal int Frame = -1;
        /// <summary>The page storage generation whose references were last prepared.</summary>
        internal int StorageVersion = -1;

        /// <summary>Retains metadata without requiring a path lookup on subsequent presentation frames.</summary>
        /// <param name="id">The unique identifier within the owning provider.</param>
        /// <param name="image">The currently selected complete layout's metadata.</param>
        /// <param name="layoutVersion">The generation supplying the metadata.</param>
        internal SpriteImageBinding(int id, SpriteAtlasImage image, int layoutVersion)
        {
            Id = id;
            Image = image;
            LayoutVersion = layoutVersion;
        }
    }
}
