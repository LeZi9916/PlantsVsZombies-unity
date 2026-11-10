#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PlantsVsZombies.UI
{
    /// <summary>Clips transformed Canvas geometry while preserving texture coordinates and tint.</summary>
    internal sealed class CanvasMeshClipper
    {
        /// <summary>The polygon buffer receiving each clipping pass.</summary>
        private UIVertex[] _output = new UIVertex[12];
        /// <summary>The polygon buffer consumed by each clipping pass.</summary>
        private UIVertex[] _input = new UIVertex[12];

        /// <summary>Adds a textured polygon, intersected with the current screen clipping rectangle.</summary>
        /// <param name="destination">The mesh receiving the resulting triangles.</param>
        /// <param name="vertices">The polygon in Canvas coordinates with downward logical Y negated.</param>
        /// <param name="count">The number of input polygon vertices.</param>
        /// <param name="clip">The clipping rectangle in top-left screen pixels.</param>
        internal void AddPolygon(VertexHelper destination, UIVertex[] vertices, int count, Rect clip)
        {
            for (var index = 0; index < count; index++)
            {
                _input[index] = vertices[index];
            }

            count = Clip(count, 0, clip.xMin, true);
            count = Clip(count, 0, clip.xMax, false);
            count = Clip(count, 1, -clip.yMax, true);
            count = Clip(count, 1, -clip.yMin, false);
            if (count < 3)
            {
                return;
            }

            var start = destination.currentVertCount;
            for (var index = 0; index < count; index++)
            {
                destination.AddVert(_input[index]);
            }

            for (var index = 1; index < count - 1; index++)
            {
                destination.AddTriangle(start, start + index, start + index + 1);
            }
        }

        /// <summary>Clips the buffered polygon against one axis-aligned boundary.</summary>
        /// <param name="count">The number of currently buffered vertices.</param>
        /// <param name="axis">Zero for X or one for Y.</param>
        /// <param name="boundary">The position of the boundary in Canvas coordinates.</param>
        /// <param name="keepGreater">Whether the accepted half-plane is above the boundary.</param>
        /// <returns>The number of vertices remaining after clipping.</returns>
        private int Clip(int count, int axis, float boundary, bool keepGreater)
        {
            if (count == 0)
            {
                return 0;
            }

            var used = 0;
            var previous = _input[count - 1];
            var previousValue = previous.position[axis];
            var previousInside = keepGreater ? previousValue >= boundary : previousValue <= boundary;
            for (var index = 0; index < count; index++)
            {
                var current = _input[index];
                var currentValue = current.position[axis];
                var currentInside = keepGreater ? currentValue >= boundary : currentValue <= boundary;
                if (currentInside != previousInside)
                {
                    var amount = (boundary - previousValue) / (currentValue - previousValue);
                    var crossing = previous;
                    crossing.position = Vector3.LerpUnclamped(previous.position, current.position, amount);
                    crossing.uv0 = Vector4.LerpUnclamped(previous.uv0, current.uv0, amount);
                    crossing.color = Color32.Lerp(previous.color, current.color, amount);
                    _output[used++] = crossing;
                }

                if (currentInside)
                {
                    _output[used++] = current;
                }

                previous = current;
                previousValue = currentValue;
                previousInside = currentInside;
            }

            var swap = _input;
            _input = _output;
            _output = swap;
            return used;
        }
    }
}
