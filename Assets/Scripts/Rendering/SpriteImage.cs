#nullable enable
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Represents a logical source image independently of its atlas layout and GPU storage.</summary>
    public sealed class SpriteImage
    {
        /// <summary>Retains one allocation-free compatibility wrapper for each external texture.</summary>
        private static readonly ConditionalWeakTable<Texture, SpriteImage> s_externalImages = new ConditionalWeakTable<Texture, SpriteImage>();
        /// <summary>The cached wrapper factory used without allocating delegates on warmed draw paths.</summary>
        private static readonly ConditionalWeakTable<Texture, SpriteImage>.CreateValueCallback s_createExternalImage = CreateExternalImage;
        /// <summary>The provider resolving the selected layout, or null for an external texture.</summary>
        private readonly SpriteAssetProvider? _provider;
        /// <summary>Gets the original image width before atlas trimming or tiling.</summary>
        public int Width { get; }
        /// <summary>Gets the original image height before atlas trimming or tiling.</summary>
        public int Height { get; }
        /// <summary>Gets the original relative path or external texture identity.</summary>
        public string SourceIdentity { get; }
        /// <summary>Gets an external or loose source texture; baked images never allocate a source texture.</summary>
        public Texture? SourceTexture { get; }

        /// <summary>Creates a logical baked image without allocating a source texture.</summary>
        /// <param name="provider">The shared owner of the baked page cache.</param>
        /// <param name="image">The selected layout's immutable source metadata.</param>
        internal SpriteImage(SpriteAssetProvider provider, SpriteAtlasImage image)
        {
            _provider = provider;
            Width = image.Width;
            Height = image.Height;
            SourceIdentity = image.SourceIdentity;
        }

        /// <summary>Wraps a standalone texture while preserving its dimensions.</summary>
        /// <param name="texture">The externally owned or provider-owned source texture.</param>
        /// <param name="identity">The source identity retained by diagnostics.</param>
        internal SpriteImage(Texture texture, string identity)
        {
            SourceTexture = texture;
            Width = texture.width;
            Height = texture.height;
            SourceIdentity = identity;
        }

        /// <summary>Returns the cached compatibility wrapper for an externally owned texture.</summary>
        /// <param name="texture">The texture or render texture to sample without reading its pixels.</param>
        /// <returns>The retained image wrapper, which does not take ownership of the texture.</returns>
        /// <exception cref="ArgumentNullException">The supplied texture is null or destroyed.</exception>
        public static SpriteImage FromTexture(Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            return s_externalImages.GetValue(texture, s_createExternalImage);
        }

        /// <summary>Creates the single cached wrapper used by an external texture.</summary>
        /// <param name="texture">The externally owned texture.</param>
        /// <returns>A wrapper containing the texture's dimensions and name.</returns>
        private static SpriteImage CreateExternalImage(Texture texture)
        {
            return new SpriteImage(texture, texture.name);
        }

        /// <summary>Counts visible regions after cropping, preparing all image pages before returning.</summary>
        /// <param name="sourceUv">The requested normalized original source rectangle; either axis may be reversed.</param>
        /// <returns>The number of nonoverlapping visible pieces intersecting the source rectangle.</returns>
        /// <exception cref="InvalidOperationException">No compatible atlas layout can load the image's pages.</exception>
        /// <exception cref="ObjectDisposedException">The provider owning this baked image has been released.</exception>
        public int GetRegionCount(Rect sourceUv)
        {
            return GetRegionCount(sourceUv, true);
        }

        /// <summary>Counts source regions with optional transparent geometry pruning for exact native composition.</summary>
        /// <param name="sourceUv">The requested original normalized source rectangle.</param>
        /// <param name="useTightGeometry">Whether conservative transparent cell rectangles may split the source tile.</param>
        /// <returns>The number of source pieces required by this request.</returns>
        /// <exception cref="InvalidOperationException">No compatible atlas layout can load the image's pages.</exception>
        /// <exception cref="ObjectDisposedException">The provider owning this baked image has been released.</exception>
        internal int GetRegionCount(Rect sourceUv, bool useTightGeometry)
        {
            if (_provider == null)
            {
                return SourceTexture != null && sourceUv.width != 0f && sourceUv.height != 0f ? 1 : 0;
            }
            return _provider.GetRegionCount(SourceIdentity, sourceUv, useTightGeometry);
        }

        /// <summary>Reads a cropped visible region without allocating an iterator or temporary collection.</summary>
        /// <param name="sourceUv">The requested normalized original source rectangle.</param>
        /// <param name="index">The zero-based visible-region index.</param>
        /// <param name="region">Receives paired positive source and atlas rectangles.</param>
        /// <returns>True when the requested region exists.</returns>
        /// <exception cref="InvalidOperationException">No compatible atlas layout can load the image's pages.</exception>
        /// <exception cref="ObjectDisposedException">The provider owning this baked image has been released.</exception>
        public bool TryGetRegion(Rect sourceUv, int index, out SpriteImageRegion region)
        {
            return TryGetRegion(sourceUv, index, true, out region);
        }

        /// <summary>Reads a source region using the geometry policy selected by its rendering destination.</summary>
        /// <param name="sourceUv">The requested original normalized rectangle.</param>
        /// <param name="index">The requested source-piece index.</param>
        /// <param name="useTightGeometry">Whether transparent cell pruning may subdivide the source tile.</param>
        /// <param name="region">Receives exact source geometry and paired texture coordinates.</param>
        /// <returns>Whether the requested piece exists.</returns>
        /// <exception cref="InvalidOperationException">No compatible atlas layout can load the image's pages.</exception>
        /// <exception cref="ObjectDisposedException">The provider owning this baked image has been released.</exception>
        internal bool TryGetRegion(Rect sourceUv, int index, bool useTightGeometry, out SpriteImageRegion region)
        {
            region = default;
            if (_provider != null)
            {
                return _provider.TryGetRegion(SourceIdentity, sourceUv, index, useTightGeometry, out region);
            }
            if (index != 0 || SourceTexture == null || sourceUv.width == 0f || sourceUv.height == 0f)
            {
                return false;
            }
            var positive = SpriteGeometryUtility.Positive(sourceUv);
            region = new SpriteImageRegion(SourceTexture, positive, positive,
                new Rect(positive.x * Width, positive.y * Height, positive.width * Width, positive.height * Height));
            return true;
        }
    }
}
