#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Builds bounded conservative image geometry and selects memory-bounded atlas layouts.</summary>
    public static class SpriteGeometryUtility
    {
        /// <summary>The maximum RGBA32 texture allocation for one default atlas page.</summary>
        public const long MaximumPageBytes = 16L * 1024 * 1024;
        /// <summary>The default largest page edge, independently of a GPU's larger hardware limit.</summary>
        public const int MaximumPageSize = 2048;
        /// <summary>The source-pixel grid used to omit transparent image areas.</summary>
        public const int GeometryCellSize = 8;
        /// <summary>The maximum number of visible geometry rectangles retained for one source image.</summary>
        public const int MaximumGeometryPieces = 32;

        /// <summary>Selects the largest baked layout fitting hardware dimensions and the per-page memory budget.</summary>
        /// <param name="layouts">The complete independently packed layouts.</param>
        /// <param name="hardwareMaximum">The graphics device's maximum texture edge.</param>
        /// <param name="pageBudgetBytes">The allowed bytes for one RGBA32 page without mipmaps.</param>
        /// <returns>The layout array index, or minus one when no supported variant exists.</returns>
        public static int SelectLayout(SpriteAtlasLayout[] layouts, int hardwareMaximum, long pageBudgetBytes = MaximumPageBytes)
        {
            var selected = -1;
            var selectedSize = 0;
            for (var index = 0; index < layouts.Length; index++)
            {
                var size = layouts[index].MaximumPageSize;
                if (size > selectedSize && size <= hardwareMaximum && size <= MaximumPageSize
                    && (long)size * size * 4 <= pageBudgetBytes)
                {
                    selected = index;
                    selectedSize = size;
                }
            }
            return selected;
        }

        /// <summary>Normalizes a rectangle without changing the represented source interval.</summary>
        /// <param name="rectangle">A rectangle whose width or height may be negative.</param>
        /// <returns>The corresponding positive rectangle.</returns>
        public static Rect Positive(Rect rectangle)
        {
            return Rect.MinMaxRect(Mathf.Min(rectangle.x, rectangle.x + rectangle.width),
                Mathf.Min(rectangle.y, rectangle.y + rectangle.height),
                Mathf.Max(rectangle.x, rectangle.x + rectangle.width), Mathf.Max(rectangle.y, rectangle.y + rectangle.height));
        }

        /// <summary>Finds the strictly positive overlap of two positive rectangles.</summary>
        /// <param name="first">The first positive rectangle.</param>
        /// <param name="second">The second positive rectangle.</param>
        /// <param name="intersection">Receives the overlap, or a zero-area rectangle.</param>
        /// <returns>True when the intersection has positive width and height.</returns>
        public static bool Intersect(Rect first, Rect second, out Rect intersection)
        {
            intersection = Rect.MinMaxRect(Mathf.Max(first.xMin, second.xMin), Mathf.Max(first.yMin, second.yMin),
                Mathf.Min(first.xMax, second.xMax), Mathf.Min(first.yMax, second.yMax));
            return intersection.width > 0f && intersection.height > 0f;
        }

        /// <summary>Builds disjoint eight-pixel cell rectangles, conservatively retaining a one-pixel filtering halo.</summary>
        /// <param name="pixels">Original bottom-left RGBA source pixels.</param>
        /// <param name="width">The original image width.</param>
        /// <param name="height">The original image height.</param>
        /// <returns>At most 32 disjoint visible pixel rectangles, or no rectangles for an empty image.</returns>
        /// <exception cref="ArgumentException">The pixel buffer dimensions are inconsistent.</exception>
        public static Rect[] BuildVisibleGeometry(Color32[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0 || pixels.Length != (long)width * height)
            {
                throw new ArgumentException("Source dimensions do not match the pixel buffer.", nameof(pixels));
            }
            var minimumX = width;
            var minimumY = height;
            var maximumX = -1;
            var maximumY = -1;
            var columns = (width + GeometryCellSize - 1) / GeometryCellSize;
            var rows = (height + GeometryCellSize - 1) / GeometryCellSize;
            var occupied = new bool[columns * rows];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a == 0)
                    {
                        continue;
                    }
                    minimumX = Math.Min(minimumX, x);
                    minimumY = Math.Min(minimumY, y);
                    maximumX = Math.Max(maximumX, x);
                    maximumY = Math.Max(maximumY, y);
                    for (var cellY = Math.Max(0, y - 1) / GeometryCellSize; cellY <= Math.Min(height - 1, y + 1) / GeometryCellSize; cellY++)
                    {
                        for (var cellX = Math.Max(0, x - 1) / GeometryCellSize; cellX <= Math.Min(width - 1, x + 1) / GeometryCellSize; cellX++)
                        {
                            occupied[cellY * columns + cellX] = true;
                        }
                    }
                }
            }
            if (maximumX < 0)
            {
                return Array.Empty<Rect>();
            }
            var bounds = Rect.MinMaxRect(Math.Max(0, minimumX - 1), Math.Max(0, minimumY - 1),
                Math.Min(width, maximumX + 2), Math.Min(height, maximumY + 2));
            var rectangles = new List<Rect>();
            for (var row = 0; row < rows; row++)
            {
                var column = 0;
                while (column < columns)
                {
                    if (!occupied[row * columns + column])
                    {
                        column++;
                        continue;
                    }
                    var start = column++;
                    while (column < columns && occupied[row * columns + column])
                    {
                        column++;
                    }
                    var rectangle = new Rect(start * GeometryCellSize, row * GeometryCellSize,
                        Math.Min(width, column * GeometryCellSize) - start * GeometryCellSize,
                        Math.Min(GeometryCellSize, height - row * GeometryCellSize));
                    Intersect(rectangle, bounds, out rectangle);
                    var merged = false;
                    for (var index = rectangles.Count - 1; index >= 0; index--)
                    {
                        var previous = rectangles[index];
                        if (previous.x == rectangle.x && previous.width == rectangle.width && previous.yMax == rectangle.yMin)
                        {
                            previous.height += rectangle.height;
                            rectangles[index] = previous;
                            merged = true;
                            break;
                        }
                    }
                    if (!merged)
                    {
                        rectangles.Add(rectangle);
                    }
                }
            }
            return rectangles.Count <= MaximumGeometryPieces ? rectangles.ToArray() : new[] { bounds };
        }
    }
}
