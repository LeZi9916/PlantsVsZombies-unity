#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Exposes the last submitted full affine sprite pose without adding a renderer or per-object update.</summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("")]
    public sealed class SpriteHierarchySprite : MonoBehaviour
    {
        /// <summary>The registry receiving synchronous visibility changes.</summary>
        private SpriteHierarchy? _visibilityOwner;
        /// <summary>The current pool binding generation.</summary>
        private uint _visibilityGeneration;

        /// <summary>Updates the visibility binding before reusing this source node.</summary>
        /// <param name="owner">The active owner, or null while pooled.</param>
        /// <param name="generation">The owner's current match generation.</param>
        internal void BindVisibility(SpriteHierarchy? owner, uint generation)
        {
            _visibilityOwner = owner;
            _visibilityGeneration = generation;
        }

        private void OnEnable()
        {
            _visibilityOwner?.NotifyVisibilityChanged(_visibilityGeneration);
        }

        private void OnDisable()
        {
            _visibilityOwner?.NotifyVisibilityChanged(_visibilityGeneration);
        }

        /// <summary>Clears source references and sampled data before another model acquires this object.</summary>
        internal void ResetPose()
        {
            SourceIdentity = string.Empty;
            LogicalSize = default;
            SourceUv = default;
            DestinationTransform = Matrix4x4.identity;
            Tint = Color.white;
            RenderMode = 0;
            HasPose = false;
        }

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
        internal void Record(SpriteImage image, float width, float height, Rect uv, in Matrix4x4 matrix, Color color, int mode)
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
