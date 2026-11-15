#nullable enable
using PlantsVsZombies.UI.BitmapFonts;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsVsZombies.UI
{
    /// <summary>Retains one ordered font-page run and rebuilds its clipped mesh only when its display state changes.</summary>
    public sealed class CanvasBitmapGraphic : MaskableGraphic
    {
        /// <summary>Reuses the four corners of the glyph currently being clipped.</summary>
        private readonly UIVertex[] _vertices = new UIVertex[4];
        /// <summary>Reuses polygon clipping storage for every glyph.</summary>
        private readonly CanvasMeshClipper _clipper = new CanvasMeshClipper();
        /// <summary>The immutable layout supplying the glyph positions.</summary>
        private BitmapTextLayout? _layout;
        /// <summary>The page sampled by this contiguous glyph run.</summary>
        private Texture? _texture;
        /// <summary>The first glyph in the retained layout.</summary>
        private int _first;
        /// <summary>The number of glyphs in this page run.</summary>
        private int _count;
        /// <summary>The current text origin in logical coordinates.</summary>
        private Vector2 _origin;
        /// <summary>The logical-to-screen transform.</summary>
        private Matrix4x4 _matrix;
        /// <summary>The accepted top-left screen pixel rectangle.</summary>
        private Rect _clip;
        /// <summary>Gets the original font page without copying or resampling its pixels.</summary>
        public override Texture mainTexture => _texture != null ? _texture : Texture2D.whiteTexture;

        /// <summary>Updates one retained page run while preserving unchanged native mesh data.</summary>
        /// <param name="layout">The immutable glyph layout.</param>
        /// <param name="first">The first glyph to draw.</param>
        /// <param name="count">The number of consecutive glyphs on this page.</param>
        /// <param name="texture">The original font atlas page.</param>
        /// <param name="origin">The logical layout origin.</param>
        /// <param name="matrix">The logical-to-screen transform.</param>
        /// <param name="tint">The inherited text tint and opacity.</param>
        /// <param name="clip">The accepted screen pixel rectangle.</param>
        internal void Configure(BitmapTextLayout layout, int first, int count, Texture texture, Vector2 origin,
            Matrix4x4 matrix, Color tint, Rect clip)
        {
            var textureChanged = _texture != texture;
            var geometryChanged = !ReferenceEquals(_layout, layout) || _first != first || _count != count
                || !_origin.Equals(origin) || !_matrix.Equals(matrix) || !_clip.Equals(clip);
            _layout = layout;
            _first = first;
            _count = count;
            _texture = texture;
            _origin = origin;
            _matrix = matrix;
            _clip = clip;
            color = tint;
            raycastTarget = false;
            if (textureChanged)
            {
                SetMaterialDirty();
            }
            if (geometryChanged)
            {
                SetVerticesDirty();
            }
        }

        /// <summary>Emits each glyph in original order using the same per-quad transform and clipping as artwork.</summary>
        /// <param name="helper">The retained native Canvas mesh builder.</param>
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var layout = _layout;
            var font = layout?.Font;
            if (layout == null || font == null)
            {
                return;
            }
            for (var index = _first; index < _first + _count; index++)
            {
                var placed = layout.Glyphs[index];
                var glyph = placed.Glyph;
                if (glyph.Width == 0 || glyph.Height == 0)
                {
                    continue;
                }
                var destination = new Rect(_origin.x + placed.X, _origin.y + placed.Y, placed.Width, placed.Height);
                var uv = new Rect((float)glyph.X / font.AtlasWidth,
                    1f - (float)(glyph.Y + glyph.Height) / font.AtlasHeight,
                    (float)glyph.Width / font.AtlasWidth, (float)glyph.Height / font.AtlasHeight);
                SetCorner(0, destination.xMin, destination.yMin, uv.xMin, uv.yMax);
                SetCorner(1, destination.xMax, destination.yMin, uv.xMax, uv.yMax);
                SetCorner(2, destination.xMax, destination.yMax, uv.xMax, uv.yMin);
                SetCorner(3, destination.xMin, destination.yMax, uv.xMin, uv.yMin);
                _clipper.AddPolygon(helper, _vertices, 4, _clip);
            }
        }

        /// <summary>Transforms one glyph corner into the Canvas screen coordinate system.</summary>
        /// <param name="index">The reusable corner slot.</param>
        /// <param name="x">The logical horizontal coordinate.</param>
        /// <param name="y">The logical downward coordinate.</param>
        /// <param name="u">The horizontal atlas coordinate.</param>
        /// <param name="v">The vertical atlas coordinate.</param>
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
