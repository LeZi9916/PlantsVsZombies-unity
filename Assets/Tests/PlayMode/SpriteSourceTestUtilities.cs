#nullable enable
using System.Collections.Generic;
using System.Reflection;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Centralizes source-image injection used by synthetic presenter and isolated artwork fixtures.</summary>
    internal static class SpriteSourceTestUtilities
    {
        /// <summary>The retained reflection lookup for the logical image cache.</summary>
        private static readonly FieldInfo s_images = typeof(TextureCache).GetField("_imageCache", BindingFlags.Instance | BindingFlags.NonPublic)!;

        /// <summary>Overrides a logical fixture image while retaining the existing synthetic texture ownership.</summary>
        /// <param name="cache">The production presenter cache receiving the fixture override.</param>
        /// <param name="name">The original image filename used by the presenter.</param>
        /// <param name="texture">The synthetic source texture, or null to suppress the image.</param>
        internal static void SetImage(TextureCache cache, string name, Texture2D? texture)
        {
            cache._textures[name] = texture!;
            SetLogicalImage(cache, name, texture == null ? null : SpriteImage.FromTexture(texture));
        }

        /// <summary>Overrides a logical source without allocating or replacing its original GPU texture.</summary>
        /// <param name="cache">The production cache receiving the logical source override.</param>
        /// <param name="name">The original filename used by the presenter.</param>
        /// <param name="image">The retained source handle, or null to suppress the image.</param>
        internal static void SetLogicalImage(TextureCache cache, string name, SpriteImage? image)
        {
            var images = (Dictionary<string, SpriteImage?>)s_images.GetValue(cache)!;
            images[name] = image;
        }

        /// <summary>Reads a source already requested by production without loading missing fixture artwork.</summary>
        /// <param name="cache">The cache populated by the production presenter.</param>
        /// <param name="name">The exact original filename expected from production.</param>
        /// <param name="image">Receives the cached source, including a cached optional-image miss.</param>
        /// <returns>True when production has already requested the source.</returns>
        internal static bool TryGetImage(TextureCache cache, string name, out SpriteImage? image)
        {
            var images = (Dictionary<string, SpriteImage?>)s_images.GetValue(cache)!;
            return images.TryGetValue(name, out image);
        }
    }
}
