#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Matches the shader's portable, explicitly aligned 64-byte polygon instance record.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteInstanceData : IEquatable<SpriteInstanceData>
    {
        /// <summary>The first clipped vertex in the chunk's float4 buffer.</summary>
        internal uint VertexOffset;
        /// <summary>The number of live vertices in this convex polygon.</summary>
        internal uint VertexCount;
        /// <summary>The original texture's position in the ordered eight-texture bank.</summary>
        internal uint TextureSlot;
        /// <summary>The unchanged depth generated from the logical painter order.</summary>
        internal float Depth;
        /// <summary>The original unquantized color.</summary>
        internal Vector4 Color;
        /// <summary>The grayscale, producer brightness, render mode, and fused-hit switch.</summary>
        internal Vector4 Effects;
        /// <summary>The original source-colored highlight.</summary>
        internal Vector4 Highlight;

        /// <summary>Compares a record with retained uploaded data without boxing or approximate operators.</summary>
        /// <param name="other">The retained record at the same buffer slot.</param>
        /// <returns>Whether all uploaded values are equal.</returns>
        public bool Equals(SpriteInstanceData other)
        {
            return VertexOffset == other.VertexOffset && VertexCount == other.VertexCount && TextureSlot == other.TextureSlot
                && Depth.Equals(other.Depth) && Color.Equals(other.Color) && Effects.Equals(other.Effects) && Highlight.Equals(other.Highlight);
        }
    }
}
