#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PlantsVsZombies.UI
{
    /// <summary>Displays a transformed original texture through a native CanvasRenderer.</summary>
    public sealed class CanvasTextureGraphic : MaskableGraphic
    {
        /// <summary>The vertices retained while building a transformed quad.</summary>
        private readonly UIVertex[] _vertices = new UIVertex[4];
        /// <summary>The reusable polygon clipping buffers.</summary>
        private readonly CanvasMeshClipper _clipper = new CanvasMeshClipper();
        /// <summary>The texture displayed by this native graphic.</summary>
        private Texture? _texture;
        /// <summary>The untransformed destination rectangle.</summary>
        private Rect _destination;
        /// <summary>The normalized texture sampling rectangle.</summary>
        private Rect _uv;
        /// <summary>The explicit transform from destination coordinates to screen pixels.</summary>
        private Matrix4x4 _matrix;
        /// <summary>The screen pixel bounds accepted by the current clipping group.</summary>
        private Rect _clip;
        /// <summary>Gets the texture sent to this CanvasRenderer.</summary>
        public override Texture mainTexture => _texture != null ? _texture : Texture2D.whiteTexture;

        /// <summary>Updates the reused graphic for one visible frame.</summary>
        /// <param name="texture">The original texture to display.</param>
        /// <param name="destination">The destination rectangle before transformation.</param>
        /// <param name="uv">The normalized source texture coordinates.</param>
        /// <param name="matrix">The destination-to-screen transform.</param>
        /// <param name="tint">The inherited color and opacity.</param>
        /// <param name="clip">The group clipping rectangle in top-left screen pixels.</param>
        internal void Configure(Texture texture, Rect destination, Rect uv, Matrix4x4 matrix, Color tint, Rect clip)
        {
            _texture = texture;
            _destination = destination;
            _uv = uv;
            _matrix = matrix;
            _clip = clip;
            color = tint;
            raycastTarget = false;
            SetAllDirty();
        }

        /// <summary>Builds the native Canvas mesh after applying the current transform and clip.</summary>
        /// <param name="helper">The destination mesh builder.</param>
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            SetCorner(0, _destination.xMin, _destination.yMin, _uv.xMin, _uv.yMax);
            SetCorner(1, _destination.xMax, _destination.yMin, _uv.xMax, _uv.yMax);
            SetCorner(2, _destination.xMax, _destination.yMax, _uv.xMax, _uv.yMin);
            SetCorner(3, _destination.xMin, _destination.yMax, _uv.xMin, _uv.yMin);
            _clipper.AddPolygon(helper, _vertices, 4, _clip);
        }

        /// <summary>Transforms one source corner into the screen-sized native Canvas.</summary>
        /// <param name="index">The vertex slot to update.</param>
        /// <param name="x">The untransformed horizontal destination coordinate.</param>
        /// <param name="y">The untransformed downward destination coordinate.</param>
        /// <param name="u">The horizontal normalized texture coordinate.</param>
        /// <param name="v">The vertical normalized texture coordinate.</param>
        private void SetCorner(int index, float x, float y, float u, float v)
        {
            var position = _matrix.MultiplyPoint3x4(new Vector3(x, y));
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(position.x, -position.y);
            vertex.uv0 = new Vector2(u, v);
            vertex.color = color;
            _vertices[index] = vertex;
        }
    }
}
