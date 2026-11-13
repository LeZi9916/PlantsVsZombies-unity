#nullable enable

using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Samples the original PC PoolEffect UV grids and fixed-point caustic texture.</summary>
    internal static class OriginalPoolEffectRules
    {
        /// <summary>The original number of horizontal pool cells.</summary>
        internal const int Columns = 15;
        /// <summary>The original number of vertical pool cells.</summary>
        internal const int Rows = 5;
        /// <summary>The number of shared vertices in each original grid.</summary>
        internal const int VertexCount = (Columns + 1) * (Rows + 1);
        /// <summary>The PC dynamic caustic image width.</summary>
        internal const int CausticWidth = 128;
        /// <summary>The PC dynamic caustic image height.</summary>
        internal const int CausticHeight = 64;
        /// <summary>The two-pi constant used by PC 0x478ED0.</summary>
        private const double Tau = 6.283185482025146;

        /// <summary>Gets a PC pool grid vertex's untransformed board position.</summary>
        /// <param name="layer">Zero for the pool base, one for shading, or two for caustics.</param>
        /// <param name="column">The grid vertex column, from zero to fifteen.</param>
        /// <param name="row">The grid vertex row, from zero to five.</param>
        /// <param name="poolSize">The original IMAGE_POOL image dimensions.</param>
        /// <returns>The original position in downward-positive board pixels.</returns>
        internal static Vector2 Position(int layer, int column, int row, Vector2 poolSize)
        {
            // PC PoolEffect::PoolEffectDraw 0x478ED0 uses a separate inset caustic grid.
            return layer == 2
                ? new Vector2(45f + column * 46.93333435058594f, 288f + row * 30f)
                : new Vector2(35f + column * (poolSize.x / Columns), 279f + row * (poolSize.y / Rows));
        }

        /// <summary>Gets the original grid's source UV, including its authored time-dependent distortion.</summary>
        /// <param name="layer">Zero for the pool base, one for shading, or two for caustics.</param>
        /// <param name="column">The grid vertex column, from zero to fifteen.</param>
        /// <param name="row">The grid vertex row, from zero to five.</param>
        /// <param name="ticks">The independent PC PoolEffect update counter.</param>
        /// <returns>The original top-left-origin normalized source coordinate.</returns>
        internal static Vector2 Uv(int layer, int column, int row, int ticks)
        {
            var u = column * .06666667014360428f;
            var v = row * .2000000029802322f;
            if (layer == 2)
            {
                // PC stores this grid's normalized UV, then adds the normalized coordinate again when assembling triangles.
                u += u;
                v += v;
            }

            if (column == 0 || column == Columns || row == 0 || row == Rows)
            {
                return new Vector2(u, v);
            }

            var time = ticks * Tau;
            var t800 = (float)(time / 800d);
            var t150 = (float)(time / 150d);
            var t900 = (float)(time / 900d);
            var t110 = (float)(time / 110d);
            var x = (float)(column * 3d * Tau / Columns);
            var y = (float)(row * 3d * Tau / Rows);
            if (layer == 0)
            {
                u += (float)(Sin(y + t800) * .005f + Sin(y + t150) * .002f);
                v += (float)(Sin(x + t800) * .005f + Sin(x + t900) * .015f + Sin(x + t110) * .01f);
            }
            else if (layer == 1)
            {
                var sx = (float)(x * .2000000029802322d);
                var sy = (float)(y * .2000000029802322d);
                u += (float)(Sin(sy + t800) * .012f + Sin(sy + t150) * .015f);
                v += (float)(Sin(sx + t800) * .02f + Sin(sx + t900) * .015f + Sin(sx + t110) * .005f);
            }
            else
            {
                u += (float)(Sin((float)(t150 * 1.5d + y)) * .005f + Sin((float)(t800 * 1.5d + y)) * .004f);
                v += (float)(Sin((float)(x * 3d + t800 * 2.5d)) * .02f
                    + Sin((float)(x * 2d + t900 * 2.5d)) * .04f
                    + Sin((float)(x * 4d + t110 * 2.5d)) * .005f);
            }

            return new Vector2(u, v);
        }

        /// <summary>Gets PC 0x478ED0's per-vertex caustic opacity.</summary>
        /// <param name="night">Whether the night pool is being drawn.</param>
        /// <param name="column">The grid vertex column, from zero to fifteen.</param>
        /// <param name="row">The grid vertex row, from zero to five.</param>
        /// <returns>The original white vertex alpha, before the dynamic texture's alpha.</returns>
        internal static byte CausticVertexAlpha(bool night, int column, int row)
        {
            return column == 0 || column == Columns || row == 0 ? (byte)32 : night ? (byte)48 : column <= 7 ? (byte)192 : (byte)128;
        }

        /// <summary>Fills the original 128-by-64 white caustic texture using PC 0x478D90.</summary>
        /// <param name="source">The original 256-by-256 grayscale image in top-to-bottom row order.</param>
        /// <param name="destination">The reusable output in Unity's bottom-to-top pixel order.</param>
        /// <param name="ticks">The independent PC PoolEffect update counter.</param>
        internal static void FillCaustics(byte[] source, Color32[] destination, int ticks)
        {
            var shiftedTime = unchecked(ticks << 16);
            var firstXTime = unchecked(((ticks & 0xFFFF) + 1) << 16) / 6;
            for (var row = 0; row < CausticHeight; row++)
            {
                var firstY = unchecked(((256 - row) << 17) + shiftedTime / 8);
                var secondY = row << 17;
                for (var column = 0; column < CausticWidth; column++)
                {
                    var x = column << 17;
                    var first = Sample(source, unchecked(x - firstXTime), firstY);
                    var second = Sample(source, unchecked(x + shiftedTime / 10), secondY);
                    var intensity = (first + second) / 2;
                    var alpha = intensity < 128 ? (byte)0
                        : intensity < 160 ? unchecked((byte)(5 * (intensity - 128)))
                        : unchecked((byte)(63 - 2 * intensity));
                    destination[(CausticHeight - row - 1) * CausticWidth + column] = new Color32(255, 255, 255, (byte)(alpha / 3));
                }
            }
        }

        /// <summary>Samples PC 0x478CB0's wrapping 16.16 bilinear interpolation with its per-term truncation.</summary>
        /// <param name="source">The original top-to-bottom 256-by-256 grayscale image.</param>
        /// <param name="x">The signed wrapping 16.16 source X coordinate.</param>
        /// <param name="y">The signed wrapping 16.16 source Y coordinate.</param>
        /// <returns>The original interpolated grayscale value.</returns>
        internal static int Sample(byte[] source, int x, int y)
        {
            var fractionX = (uint)((x & 0xFFFE) + 1);
            var fractionY = (uint)((y & 0xFFFE) + 1);
            var left = (x >> 16) & 255;
            var right = (left + 1) & 255;
            var top = (y >> 16) & 255;
            var bottom = (top + 1) & 255;
            return (int)(((((65536U - fractionX) * (65536U - fractionY)) >> 16) * source[top * 256 + left]) >> 16)
                + (int)((((fractionX * (65536U - fractionY)) >> 16) * source[top * 256 + right]) >> 16)
                + (int)(((((65536U - fractionX) * fractionY) >> 16) * source[bottom * 256 + left]) >> 16)
                + (int)((((fractionX * fractionY) >> 16) * source[bottom * 256 + right]) >> 16);
        }

        /// <summary>Rounds the PC sine result to the source's stored float precision.</summary>
        /// <param name="angle">The original float angle in radians.</param>
        /// <returns>The stored float sine, promoted for the original extended-precision arithmetic.</returns>
        private static double Sin(float angle)
        {
            return (float)Math.Sin(angle);
        }
    }
}
