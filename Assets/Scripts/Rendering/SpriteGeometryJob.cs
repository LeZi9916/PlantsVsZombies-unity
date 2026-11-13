#nullable enable
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Computes one bounded segment of sprite geometry synchronously, preserving source and clipping order.</summary>
    /// <remarks>Some Mono configurations expand each float expression before its local float store. Explicit doubles
    /// reproduce that behavior when detected by the owner; Strict alone does not reproduce a managed runtime's precision.</remarks>
    [BurstCompile(FloatMode = FloatMode.Strict)]
    internal struct SpriteGeometryJob : IJob
    {
        /// <summary>The numeric region snapshots prepared on the main thread.</summary>
        [ReadOnly]
        internal NativeArray<SpriteRegionInput> Inputs;
        /// <summary>The nonoverlapping flat vertex output, with twelve slots per region.</summary>
        internal NativeArray<SpriteClipVertex> Vertices;
        /// <summary>The polygon metadata for each input region.</summary>
        internal NativeArray<SpriteRegionResult> Results;
        /// <summary>The first separate twelve-vertex clipping scratch buffer.</summary>
        internal NativeArray<SpriteClipVertex> ScratchInput;
        /// <summary>The second separate twelve-vertex clipping scratch buffer.</summary>
        internal NativeArray<SpriteClipVertex> ScratchOutput;
        /// <summary>The populated input prefix.</summary>
        internal int Count;
        /// <summary>The exact horizontal sample spacing of the current region.</summary>
        private double _pixelStepX;
        /// <summary>The exact vertical sample spacing of the current region.</summary>
        private double _pixelStepY;

        /// <summary>Whether the source runtime retains double precision within individual float expressions.</summary>
        private bool _expandedPrecision;

        /// <summary>Processes every region once without accessing managed objects or scheduling asynchronous work.</summary>
        public void Execute()
        {
            for (var index = 0; index < Count; index++)
            {
                var input = Inputs[index];
                _expandedPrecision = input.ExpandedPrecision;
                _pixelStepX = input.PixelStepX;
                _pixelStepY = input.PixelStepY;
                var scaleX = _expandedPrecision
                    ? (float)((double)input.Size.x / ((double)input.SourceUv.width * input.ImageSize.x))
                    : input.Size.x / (input.SourceUv.width * input.ImageSize.x);
                var scaleY = _expandedPrecision
                    ? (float)((double)input.Size.y / ((double)input.SourceUv.height * input.ImageSize.y))
                    : input.Size.y / (input.SourceUv.height * input.ImageSize.y);
                var originX = _expandedPrecision ? (float)((double)input.SourceUv.x * input.ImageSize.x) : input.SourceUv.x * input.ImageSize.x;
                var sourceTop = _expandedPrecision
                    ? (float)(((double)input.SourceUv.y + input.SourceUv.height) * input.ImageSize.y)
                    : (input.SourceUv.y + input.SourceUv.height) * input.ImageSize.y;
                var left = _expandedPrecision ? (float)(((double)input.SourcePixels.xMin - originX) * scaleX) : (input.SourcePixels.xMin - originX) * scaleX;
                var right = _expandedPrecision ? (float)(((double)input.SourcePixels.xMax - originX) * scaleX) : (input.SourcePixels.xMax - originX) * scaleX;
                var top = _expandedPrecision ? (float)(((double)sourceTop - input.SourcePixels.yMax) * scaleY) : (sourceTop - input.SourcePixels.yMax) * scaleY;
                var bottom = _expandedPrecision ? (float)(((double)sourceTop - input.SourcePixels.yMin) * scaleY) : (sourceTop - input.SourcePixels.yMin) * scaleY;
                SetClipCorner(0, input.Matrix, left, top, input.TextureUv.xMin, input.TextureUv.yMax);
                SetClipCorner(1, input.Matrix, right, top, input.TextureUv.xMax, input.TextureUv.yMax);
                SetClipCorner(2, input.Matrix, right, bottom, input.TextureUv.xMax, input.TextureUv.yMin);
                SetClipCorner(3, input.Matrix, left, bottom, input.TextureUv.xMin, input.TextureUv.yMin);
                var count = input.ClipEnabled ? ClipPolygon(input.Clip) : 4;
                var area = PolygonArea(count);
                var offset = index * 12;
                Results[index] = new SpriteRegionResult { Offset = offset, Count = count, Area = area };
                for (var vertex = 0; vertex < count; vertex++)
                {
                    Vertices[offset + vertex] = ScratchInput[vertex];
                }
            }
        }

        /// <summary>Writes an original transformed corner into the reusable clipping polygon.</summary>
        /// <param name="index">The destination polygon vertex.</param>
        /// <param name="matrix">The source-to-destination transform.</param>
        /// <param name="x">The source horizontal coordinate.</param>
        /// <param name="y">The source vertical coordinate.</param>
        /// <param name="u">The atlas horizontal coordinate.</param>
        /// <param name="v">The atlas vertical coordinate.</param>
        private void SetClipCorner(int index, Matrix4x4 matrix, float x, float y, float u, float v)
        {
            var destinationX = _expandedPrecision
                ? (float)(((double)matrix.m00 * x) + ((double)matrix.m01 * y) + matrix.m03)
                : (matrix.m00 * x) + (matrix.m01 * y) + matrix.m03;
            var destinationY = _expandedPrecision
                ? (float)(((double)matrix.m10 * x) + ((double)matrix.m11 * y) + matrix.m13)
                : (matrix.m10 * x) + (matrix.m11 * y) + matrix.m13;
            ScratchInput[index] = new SpriteClipVertex(new Vector2(destinationX, destinationY), new Vector2(u, v));
        }

        /// <summary>Clips one convex quad in destination pixels while interpolating its original texture coordinates.</summary>
        /// <param name="clip">The accepted top-left destination rectangle.</param>
        /// <returns>The number of vertices retained in the reusable input polygon.</returns>
        private int ClipPolygon(Rect clip)
        {
            var first = ScratchInput[0].Position;
            var minimumX = first.x;
            var maximumX = first.x;
            var minimumY = first.y;
            var maximumY = first.y;
            for (var index = 1; index < 4; index++)
            {
                var point = ScratchInput[index].Position;
                minimumX = Math.Min(minimumX, point.x);
                maximumX = Math.Max(maximumX, point.x);
                minimumY = Math.Min(minimumY, point.y);
                maximumY = Math.Max(maximumY, point.y);
            }

            if (minimumX >= clip.xMin && maximumX <= clip.xMax && minimumY >= clip.yMin && maximumY <= clip.yMax)
            {
                return 4;
            }

            clip = RasterClip(clip);
            if (clip.width <= 0f || clip.height <= 0f || maximumX <= clip.xMin || minimumX >= clip.xMax
                || maximumY <= clip.yMin || minimumY >= clip.yMax)
            {
                return 0;
            }

            var count = 4;
            for (var boundary = 0; boundary < 4 && count > 0; boundary++)
            {
                var outputCount = 0;
                var previous = ScratchInput[count - 1];
                var previousDistance = ClipDistance(previous.Position, clip, boundary);
                for (var index = 0; index < count; index++)
                {
                    var current = ScratchInput[index];
                    var currentDistance = ClipDistance(current.Position, clip, boundary);
                    if ((previousDistance >= 0f) != (currentDistance >= 0f))
                    {
                        var fraction = _expandedPrecision
                            ? (float)((double)previousDistance / ((double)previousDistance - currentDistance))
                            : previousDistance / (previousDistance - currentDistance);
                        ScratchOutput[outputCount++] = _expandedPrecision
                            ? InterpolateExpanded(previous, current, fraction)
                            : SpriteClipVertex.Interpolate(previous, current, fraction);
                    }

                    if (currentDistance >= 0f)
                    {
                        ScratchOutput[outputCount++] = current;
                    }

                    previous = current;
                    previousDistance = currentDistance;
                }

                var temporary = ScratchInput;
                ScratchInput = ScratchOutput;
                ScratchOutput = temporary;
                count = outputCount;
            }

            return count;
        }

        /// <summary>Maps inclusive fragment bounds to half-open geometry bounds on the actual destination sample grid.</summary>
        /// <param name="clip">The original destination-coordinate clipping rectangle.</param>
        /// <returns>Geometry bounds containing exactly the pixel centers accepted by the original fragment shader.</returns>
        private Rect RasterClip(Rect clip)
        {
            return Rect.MinMaxRect(SampleBoundary(clip.xMin, _pixelStepX, false),
                SampleBoundary(clip.yMin, _pixelStepY, false), SampleBoundary(clip.xMax, _pixelStepX, true),
                SampleBoundary(clip.yMax, _pixelStepY, true));
        }

        /// <summary>Resolves one inclusive bound without losing ties when a nonintegral pixel spacing rounds to float.</summary>
        /// <param name="bound">The original inclusive fragment bound.</param>
        /// <param name="step">The exact destination-coordinate pixel spacing.</param>
        /// <param name="maximum">Whether this is a maximum rather than minimum bound.</param>
        /// <returns>The corresponding half-open raster edge in destination coordinates.</returns>
        private static float SampleBoundary(float bound, double step, bool maximum)
        {
            if (maximum)
            {
                var sample = Math.Floor((bound / step) - 0.5d);
                if ((float)((sample + 1.5d) * step) <= bound)
                {
                    sample++;
                }
                else if ((float)((sample + 0.5d) * step) > bound)
                {
                    sample--;
                }

                return (float)((sample + 1d) * step);
            }

            var first = Math.Ceiling((bound / step) - 0.5d);
            if ((float)((first - 0.5d) * step) >= bound)
            {
                first--;
            }
            else if ((float)((first + 0.5d) * step) < bound)
            {
                first++;
            }

            return (float)(first * step);
        }

        /// <summary>Rejects zero-area transformed or boundary-only polygons before reserving painter order.</summary>
        /// <param name="count">The number of vertices in the reusable input polygon.</param>
        /// <returns>The nonnegative covered destination area, or zero for empty or degenerate geometry.</returns>
        private float PolygonArea(int count)
        {
            if (count < 3)
            {
                return 0f;
            }

            var area = 0f;
            var previous = ScratchInput[count - 1].Position;
            for (var index = 0; index < count; index++)
            {
                var current = ScratchInput[index].Position;
                area = _expandedPrecision
                    ? (float)((double)area + ((double)previous.x * current.y - (double)current.x * previous.y))
                    : area + (previous.x * current.y - current.x * previous.y);
                previous = current;
            }

            return Mathf.Abs(area) * 0.5f;
        }

        /// <summary>Preserves the managed runtime's expanded expression followed by each component's float store.</summary>
        /// <param name="first">The preceding clip edge vertex.</param>
        /// <param name="second">The following clip edge vertex.</param>
        /// <param name="fraction">The original float edge fraction.</param>
        /// <returns>The interpolated position and UV after their original float stores.</returns>
        private static SpriteClipVertex InterpolateExpanded(SpriteClipVertex first, SpriteClipVertex second, float fraction)
        {
            return new SpriteClipVertex(new Vector2(
                (float)((double)first.Position.x + ((double)second.Position.x - first.Position.x) * fraction),
                (float)((double)first.Position.y + ((double)second.Position.y - first.Position.y) * fraction)),
                new Vector2(
                    (float)((double)first.Coordinates.x + ((double)second.Coordinates.x - first.Coordinates.x) * fraction),
                    (float)((double)first.Coordinates.y + ((double)second.Coordinates.y - first.Coordinates.y) * fraction)));
        }

        /// <summary>Computes the signed distance to one axis-aligned clipping boundary.</summary>
        /// <param name="point">The destination pixel position.</param>
        /// <param name="clip">The accepted destination rectangle.</param>
        /// <param name="boundary">Left, right, top, or bottom as zero through three.</param>
        /// <returns>A nonnegative value inside the selected boundary.</returns>
        private static float ClipDistance(Vector2 point, Rect clip, int boundary)
        {
            return boundary switch
            {
                0 => point.x - clip.xMin,
                1 => clip.xMax - point.x,
                2 => point.y - clip.yMin,
                _ => clip.yMax - point.y
            };
        }

    }
}
