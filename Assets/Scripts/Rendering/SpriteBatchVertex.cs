#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Stores the unchanged Float32 sprite attributes in one portable 68-byte GPU stream.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteBatchVertex : IEquatable<SpriteBatchVertex>
    {
        /// <summary>The transformed world position.</summary>
        internal Vector3 Position;
        /// <summary>The unquantized tint and opacity.</summary>
        internal Color Color;
        /// <summary>The normalized texture coordinates.</summary>
        internal Vector2 Coordinates;
        /// <summary>The grayscale, producer, mode, and fused-hit values.</summary>
        internal Vector4 Effects;
        /// <summary>The source-colored highlight.</summary>
        internal Vector4 Highlight;

        /// <summary>Compares every uploaded attribute without approximate vector operators or boxing.</summary>
        /// <param name="other">The previously retained vertex.</param>
        /// <returns>Whether the two vertices produce identical uploaded Float32 values.</returns>
        public bool Equals(SpriteBatchVertex other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color)
                && Coordinates.Equals(other.Coordinates) && Effects.Equals(other.Effects) && Highlight.Equals(other.Highlight);
        }
    }
}
