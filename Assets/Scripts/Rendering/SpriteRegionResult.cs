#nullable enable
namespace PlantsVsZombies.Rendering
{
    /// <summary>Locates one ordered clipped polygon in the flat output arena.</summary>
    internal struct SpriteRegionResult
    {
        /// <summary>The first output vertex for this source region.</summary>
        internal int Offset;
        /// <summary>The number of retained polygon vertices.</summary>
        internal int Count;
        /// <summary>The nonnegative destination area before mesh assembly.</summary>
        internal float Area;
    }
}
