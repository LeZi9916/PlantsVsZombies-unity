#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Finds a tight lossless rectangular page using deterministic offline width and ordering trials.</summary>
    internal static class SpriteAtlasRectanglePacker
    {
        /// <summary>The offline candidate width step; final output dimensions remain the exact used bounds.</summary>
        private const int CandidateStep = 64;

        /// <summary>Sorts input indices without losing the association with their original image tiles.</summary>
        private sealed class RectangleComparer : IComparer<int>
        {
            /// <summary>The immutable padded rectangle sizes.</summary>
            private readonly Vector2Int[] _sizes;
            /// <summary>The deterministic height, area, or longest-side sorting strategy.</summary>
            private readonly int _strategy;

            /// <summary>Creates an offline rectangle-ordering trial.</summary>
            /// <param name="sizes">The original padded rectangle sizes.</param>
            /// <param name="strategy">Zero for height, one for area, or two for longest side.</param>
            internal RectangleComparer(Vector2Int[] sizes, int strategy)
            {
                _sizes = sizes;
                _strategy = strategy;
            }

            /// <summary>Orders padded rectangles without rotation or changing their original tile index.</summary>
            /// <param name="first">The first original tile index.</param>
            /// <param name="second">The second original tile index.</param>
            /// <returns>The deterministic descending-size comparison with original-index ties.</returns>
            public int Compare(int first, int second)
            {
                var a = _sizes[first];
                var b = _sizes[second];
                var main = _strategy == 0 ? b.y.CompareTo(a.y) : _strategy == 1
                    ? ((long)b.x * b.y).CompareTo((long)a.x * a.y)
                    : Math.Max(b.x, b.y).CompareTo(Math.Max(a.x, a.y));
                if (main != 0)
                {
                    return main;
                }
                var area = ((long)b.x * b.y).CompareTo((long)a.x * a.y);
                if (area != 0)
                {
                    return area;
                }
                var width = b.x.CompareTo(a.x);
                return width != 0 ? width : first.CompareTo(second);
            }
        }

        /// <summary>Tries rectangular pages bounded by the selected device tier, preserving every input tile's size and orientation.</summary>
        /// <param name="sizes">The padded original tile sizes.</param>
        /// <param name="maximum">The selected device tier's largest permitted page edge.</param>
        /// <param name="positions">Receives positions indexed exactly like the input tiles.</param>
        /// <param name="extent">Receives the exact used page extent without power-of-two expansion.</param>
        /// <returns>True when every tile fits one page within both dimension limits.</returns>
        internal static bool TryPack(Vector2Int[] sizes, int maximum, out RectInt[] positions, out Vector2Int extent)
        {
            positions = Array.Empty<RectInt>();
            extent = Vector2Int.zero;
            if (sizes.Length == 0 || maximum <= 0)
            {
                return false;
            }
            var minimumWidth = 0;
            var area = 0L;
            foreach (var size in sizes)
            {
                if (size.x <= 0 || size.y <= 0 || size.x > maximum || size.y > maximum)
                {
                    return false;
                }
                minimumWidth = Math.Max(minimumWidth, size.x);
                area += (long)size.x * size.y;
            }
            if (area > (long)maximum * maximum)
            {
                return false;
            }
            var bestArea = long.MaxValue;
            var order = new int[sizes.Length];
            var scratch = new RectInt[sizes.Length];
            var free = new List<RectInt>(sizes.Length * 2 + 1);
            var firstWidth = Math.Min(maximum, ((minimumWidth + CandidateStep - 1) / CandidateStep) * CandidateStep);
            for (var strategy = 0; strategy < 3; strategy++)
            {
                for (var index = 0; index < order.Length; index++)
                {
                    order[index] = index;
                }
                Array.Sort(order, new RectangleComparer(sizes, strategy));
                for (var width = firstWidth; width <= maximum; width = width == maximum ? maximum + 1 : Math.Min(maximum, width + CandidateStep))
                {
                    if ((long)width * maximum < area)
                    {
                        continue;
                    }
                    free.Clear();
                    free.Add(new RectInt(0, 0, width, maximum));
                    var used = Vector2Int.zero;
                    var fits = true;
                    foreach (var index in order)
                    {
                        if (!TryAllocate(free, sizes[index], out var rectangle))
                        {
                            fits = false;
                            break;
                        }
                        scratch[index] = rectangle;
                        used.x = Math.Max(used.x, rectangle.xMax);
                        used.y = Math.Max(used.y, rectangle.yMax);
                    }
                    if (!fits)
                    {
                        continue;
                    }
                    var usedArea = (long)used.x * used.y;
                    if (usedArea < bestArea || usedArea == bestArea && Math.Max(used.x, used.y) < Math.Max(extent.x, extent.y))
                    {
                        if (positions.Length != sizes.Length)
                        {
                            positions = new RectInt[sizes.Length];
                        }
                        Array.Copy(scratch, positions, sizes.Length);
                        extent = used;
                        bestArea = usedArea;
                    }
                }
            }
            return positions.Length == sizes.Length;
        }

        /// <summary>Allocates a rectangle from deterministic disjoint guillotine free space.</summary>
        /// <param name="free">The current candidate page's disjoint free rectangles.</param>
        /// <param name="size">The padded content size, which is never rotated.</param>
        /// <param name="rectangle">Receives the selected bottom-left rectangle.</param>
        /// <returns>True when a compatible free rectangle exists.</returns>
        private static bool TryAllocate(List<RectInt> free, Vector2Int size, out RectInt rectangle)
        {
            rectangle = default;
            var best = -1;
            var bestWaste = long.MaxValue;
            for (var index = 0; index < free.Count; index++)
            {
                var slot = free[index];
                var waste = (long)slot.width * slot.height - (long)size.x * size.y;
                if (slot.width >= size.x && slot.height >= size.y && waste < bestWaste)
                {
                    best = index;
                    bestWaste = waste;
                }
            }
            if (best < 0)
            {
                return false;
            }
            var selected = free[best];
            free.RemoveAt(best);
            if (selected.width > size.x)
            {
                free.Add(new RectInt(selected.x + size.x, selected.y, selected.width - size.x, size.y));
            }
            if (selected.height > size.y)
            {
                free.Add(new RectInt(selected.x, selected.y + size.y, selected.width, selected.height - size.y));
            }
            rectangle = new RectInt(selected.x, selected.y, size.x, size.y);
            return true;
        }
    }
}
