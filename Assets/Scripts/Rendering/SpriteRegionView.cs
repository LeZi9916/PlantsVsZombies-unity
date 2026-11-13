#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Provides synchronous indexed access to one prepared crop without repeating provider validation.</summary>
    /// <remarks>Consume before another image query. Deferred commands must copy each region, never retain this view.</remarks>
    internal readonly struct SpriteRegionView
    {
        /// <summary>The provider's temporarily borrowed region storage.</summary>
        private readonly SpriteImageRegion[]? _regions;
        /// <summary>The standalone region used without allocating an array.</summary>
        private readonly SpriteImageRegion _single;
        /// <summary>Gets the number of visible prepared regions.</summary>
        internal int Count { get; }
        /// <summary>Gets a prepared region while the view remains valid.</summary>
        /// <param name="index">The visible region index.</param>
        /// <returns>The copied region value.</returns>
        internal SpriteImageRegion this[int index] => _regions == null ? _single : _regions[index];

        /// <summary>Borrows the provider's prepared storage for synchronous consumption.</summary>
        /// <param name="regions">The cached region array.</param>
        /// <param name="count">The valid region count.</param>
        internal SpriteRegionView(SpriteImageRegion[] regions, int count)
        {
            _regions = regions;
            _single = default;
            Count = count;
        }

        /// <summary>Wraps one external region without allocating storage.</summary>
        /// <param name="single">The external region.</param>
        internal SpriteRegionView(SpriteImageRegion single)
        {
            _regions = null;
            _single = single;
            Count = 1;
        }
    }
}
