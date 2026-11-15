#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains one reusable slot in the provider's bounded image/crop cache.</summary>
    /// <remarks>Image handles can retain this slot after eviction; validate its current key before every direct hit.</remarks>
    internal sealed class SpriteRegionCacheEntry
    {
        /// <summary>The provider-local identity currently owning this slot.</summary>
        internal int ImageId;
        /// <summary>The exact normalized source rectangle currently occupying this slot.</summary>
        internal Rect SourceUv;
        /// <summary>Whether this slot contains tight transparent geometry or complete source tiles.</summary>
        internal bool UseTightGeometry;
        /// <summary>The reusable paired geometry and imported page references.</summary>
        internal SpriteImageRegion[] Regions = Array.Empty<SpriteImageRegion>();
        /// <summary>The valid region count in the reusable array.</summary>
        internal int Count;
        /// <summary>The complete layout generation used by the cached page mappings.</summary>
        internal int LayoutVersion = -1;
        /// <summary>The page-retirement generation used by the cached texture references.</summary>
        internal int StorageVersion = -1;
        /// <summary>The monotonic usage stamp used for bounded least-recently-used replacement.</summary>
        internal long LastUse;

        /// <summary>Validates that a retained direct reference still belongs to the requested image and crop.</summary>
        /// <param name="imageId">The provider-local stable image identity.</param>
        /// <param name="sourceUv">The exact normalized original source request.</param>
        /// <param name="useTightGeometry">Whether transparent source geometry should be pruned.</param>
        /// <returns>True when the slot has not been reassigned to another image, crop, or geometry policy.</returns>
        internal bool Matches(int imageId, Rect sourceUv, bool useTightGeometry)
        {
            return ImageId == imageId && SourceUv.Equals(sourceUv) && UseTightGeometry == useTightGeometry;
        }
    }
}
