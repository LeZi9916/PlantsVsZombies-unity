#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Caches conservative opacity classifications for the textures shared by sprite pools.</summary>
    internal sealed class TextureOpacityCache
    {
        /// <summary>The opacity classifications retained for this animation renderer's lifetime.</summary>
        private readonly Dictionary<Texture2D, (uint UpdateCount, bool Opaque)> _textures = new Dictionary<Texture2D, (uint UpdateCount, bool Opaque)>();

        /// <summary>Determines whether every source texel is fully opaque.</summary>
        /// <param name="texture">The source texture to classify.</param>
        /// <returns>True only for a readable texture whose alpha values are all 255.</returns>
        internal bool IsOpaque(Texture texture)
        {
            if (texture is not Texture2D image || !image.isReadable)
            {
                return false;
            }
            var updateCount = image.updateCount;
            if (_textures.TryGetValue(image, out var cached) && cached.UpdateCount == updateCount)
            {
                return cached.Opaque;
            }
            var opaque = HasOnlyOpaquePixels(image);
            _textures[image] = (updateCount, opaque);
            return opaque;
        }

        /// <summary>Checks readable source pixels without guessing about unsupported texture formats.</summary>
        /// <param name="texture">The readable image to inspect.</param>
        /// <returns>True when all pixels are available and fully opaque.</returns>
        private static bool HasOnlyOpaquePixels(Texture2D texture)
        {
            try
            {
                var pixels = texture.GetPixels32();
                foreach (var pixel in pixels)
                {
                    if (pixel.a != byte.MaxValue)
                    {
                        return false;
                    }
                }
                return pixels.Length > 0;
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
