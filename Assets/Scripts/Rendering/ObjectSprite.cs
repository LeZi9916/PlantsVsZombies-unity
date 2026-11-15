#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains the logical source quad independently of its combined GPU mesh.</summary>
    internal sealed class ObjectSprite
    {
        /// <summary>The reused diagnostic corner array, valid until this source slot is recorded again.</summary>
        private readonly Vector3[] _vertices = new Vector3[4];
        /// <summary>The reused diagnostic source-coordinate array.</summary>
        private readonly Vector2[] _textureCoordinates = new Vector2[4];
        /// <summary>The reused diagnostic tint array.</summary>
        private readonly Color[] _colors = new Color[4];
        /// <summary>The horizontal affine row (m00, m01, m03) needed by the original 2D corner formula.</summary>
        private Vector3 _rowX;
        /// <summary>The vertical affine row (m10, m11, m13) needed by the original 2D corner formula.</summary>
        private Vector3 _rowY;
        /// <summary>The original logical quad width and height before trimming or clipping.</summary>
        private Vector2 _size;
        /// <summary>The original source rectangle before atlas mapping.</summary>
        private Rect _sourceUv;
        /// <summary>The complete uniform source tint before geometry clipping.</summary>
        private Color _color;
        /// <summary>The painter-order depth shared by all original source corners.</summary>
        private float _depth;
        /// <summary>Whether the stored source pose is newer than the borrowed diagnostic arrays.</summary>
        private bool _geometryDirty;

        /// <summary>Gets or sets the optional stable hierarchy node that owns this source sprite.</summary>
        internal GameObject? GameObject { get; set; }
        /// <summary>Gets or sets the source image before atlas remapping and transparent trimming.</summary>
        internal SpriteImage? SourceImage { get; set; }
        /// <summary>Gets or sets the source blending operation before GPU batch grouping.</summary>
        internal int Mode { get; set; }
        /// <summary>Gets or sets the original grayscale effect amount.</summary>
        internal float Grayscale { get; set; }
        /// <summary>Gets or sets the original source-colored producer brightness.</summary>
        internal float ProducerAdditive { get; set; }
        /// <summary>Gets or sets the original white textured-hit multiplier.</summary>
        internal float Additive { get; set; }
        /// <summary>Gets or sets the original colored textured-hit multiplier.</summary>
        internal Color AddColor { get; set; }
        /// <summary>Gets or sets the accepted top-left destination rectangle.</summary>
        internal Rect Clip { get; set; }
        /// <summary>Gets or sets whether destination clipping applies to the source command.</summary>
        internal bool ClipEnabled { get; set; }
        /// <summary>Gets the latest original corners before destination clipping without rebuilding GPU geometry.</summary>
        /// <remarks>The reused array is a borrowed view until the next record; copy it for a cross-frame snapshot.</remarks>
        internal Vector3[] Vertices
        {
            get
            {
                PrepareGeometry();
                return _vertices;
            }
        }
        /// <summary>Gets the latest original source UV corners before atlas remapping.</summary>
        /// <remarks>The reused array is a borrowed view until the next record.</remarks>
        internal Vector2[] TextureCoordinates
        {
            get
            {
                PrepareGeometry();
                return _textureCoordinates;
            }
        }
        /// <summary>Gets the latest source tint retained at each original corner.</summary>
        /// <remarks>The reused array is a borrowed view until the next record.</remarks>
        internal Color[] Colors
        {
            get
            {
                PrepareGeometry();
                return _colors;
            }
        }

        /// <summary>Snapshots the small source pose while deferring diagnostic-only corner expansion until it is requested.</summary>
        /// <param name="width">The original logical quad width.</param>
        /// <param name="height">The original logical quad height.</param>
        /// <param name="uv">The original normalized source rectangle, including reversed axes.</param>
        /// <param name="matrix">The full input transform whose original 2D rows are copied immediately.</param>
        /// <param name="color">The full-precision source tint and opacity.</param>
        /// <param name="depth">The painter depth of every original corner.</param>
        internal void RecordGeometry(float width, float height, Rect uv, in Matrix4x4 matrix, Color color, float depth)
        {
            _rowX = new Vector3(matrix.m00, matrix.m01, matrix.m03);
            _rowY = new Vector3(matrix.m10, matrix.m11, matrix.m13);
            _size = new Vector2(width, height);
            _sourceUv = uv;
            _color = color;
            _depth = depth;
            _geometryDirty = true;
        }

        /// <summary>Expands the latest original source pose once for diagnostic readers, retaining all array identities.</summary>
        private void PrepareGeometry()
        {
            if (!_geometryDirty)
            {
                return;
            }
            SetCorner(0, 0f, 0f);
            SetCorner(1, _size.x, 0f);
            SetCorner(2, _size.x, _size.y);
            SetCorner(3, 0f, _size.y);
            _textureCoordinates[0] = new Vector2(_sourceUv.xMin, _sourceUv.yMax);
            _textureCoordinates[1] = new Vector2(_sourceUv.xMax, _sourceUv.yMax);
            _textureCoordinates[2] = new Vector2(_sourceUv.xMax, _sourceUv.yMin);
            _textureCoordinates[3] = new Vector2(_sourceUv.xMin, _sourceUv.yMin);
            _geometryDirty = false;
        }

        /// <summary>Applies the original ordered float expression without decomposing or rounding the affine pose.</summary>
        /// <param name="index">The original corner slot.</param>
        /// <param name="x">The original horizontal corner coordinate.</param>
        /// <param name="y">The original downward-positive corner coordinate.</param>
        private void SetCorner(int index, float x, float y)
        {
            var destinationX = (_rowX.x * x) + (_rowX.y * y) + _rowX.z;
            var destinationY = (_rowY.x * x) + (_rowY.y * y) + _rowY.z;
            _vertices[index] = new Vector3(destinationX, -destinationY, _depth);
            _colors[index] = _color;
        }
    }
}
