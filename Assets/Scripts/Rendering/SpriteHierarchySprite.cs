#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Exposes the last submitted full affine sprite pose without adding a renderer or per-object update.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class SpriteHierarchySprite : MonoBehaviour
    {
        /// <summary>Gets the original resource identity sampled by this logical sprite.</summary>
        public string SourceIdentity { get; private set; } = "";
        /// <summary>Gets the requested logical drawing size before trimming or atlas tiling.</summary>
        public Vector2 LogicalSize { get; private set; }
        /// <summary>Gets the original source rectangle before atlas remapping.</summary>
        public Rect SourceUv { get; private set; }
        /// <summary>Gets the complete sprite-to-target affine matrix, preserving independently skewed axes.</summary>
        public Matrix4x4 DestinationTransform { get; private set; } = Matrix4x4.identity;
        /// <summary>Gets the vertex tint and opacity submitted for this source sprite.</summary>
        public Color Tint { get; private set; } = Color.white;
        /// <summary>Gets the original normal, hit, particle, or premultiplied source mode.</summary>
        public int RenderMode { get; private set; }
        /// <summary>Gets whether this node has submitted any source geometry.</summary>
        public bool HasPose { get; private set; }

        /// <summary>Records immutable source metadata and the current sampled pose without decomposing its affine transform.</summary>
        /// <param name="image">The logical source image before atlas remapping.</param>
        /// <param name="width">The requested logical width.</param>
        /// <param name="height">The requested logical height.</param>
        /// <param name="uv">The original source UV rectangle.</param>
        /// <param name="matrix">The complete source-to-target matrix.</param>
        /// <param name="color">The source tint and opacity.</param>
        /// <param name="mode">The original compositing mode.</param>
        internal void Record(SpriteImage image, float width, float height, Rect uv, Matrix4x4 matrix, Color color, int mode)
        {
            SourceIdentity = image.SourceIdentity;
            LogicalSize = new Vector2(width, height);
            SourceUv = uv;
            DestinationTransform = matrix;
            Tint = color;
            RenderMode = mode;
            HasPose = true;
        }
    }
}
