#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains the attributes interpolated while clipping an affine sprite polygon.</summary>
    internal readonly struct SpriteClipVertex
    {
        /// <summary>Gets the top-left destination pixel position.</summary>
        internal Vector2 Position { get; }
        /// <summary>Gets the mapped texture coordinates.</summary>
        internal Vector2 Coordinates { get; }

        /// <summary>Creates an interpolatable sprite vertex.</summary>
        /// <param name="position">The destination pixel position.</param>
        /// <param name="coordinates">The texture coordinates at that position.</param>
        internal SpriteClipVertex(Vector2 position, Vector2 coordinates)
        {
            Position = position;
            Coordinates = coordinates;
        }

        /// <summary>Interpolates all attributes along a clipped edge.</summary>
        /// <param name="first">The edge's first endpoint.</param>
        /// <param name="second">The edge's second endpoint.</param>
        /// <param name="fraction">The normalized edge position.</param>
        /// <returns>The vertex at the requested edge position.</returns>
        internal static SpriteClipVertex Interpolate(SpriteClipVertex first, SpriteClipVertex second, float fraction)
        {
            return new SpriteClipVertex(Vector2.LerpUnclamped(first.Position, second.Position, fraction),
                Vector2.LerpUnclamped(first.Coordinates, second.Coordinates, fraction));
        }
    }
}
