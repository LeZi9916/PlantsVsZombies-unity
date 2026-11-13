#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Loads and retains the original image files used by the board and menus.</summary>
    internal sealed class TextureCache
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Shares generated atlas pages with animation and interface image consumers.</summary>
        private SpriteAssetProvider? _images;

        /// <summary>Retains logical image lookups, including missing optional artwork.</summary>
        private readonly Dictionary<string, SpriteImage?> _imageCache = new Dictionary<string, SpriteImage?>(StringComparer.Ordinal);

        /// <summary>Retains explicit alpha-grid combinations without per-frame key concatenation.</summary>
        private readonly Dictionary<(string Color, string Mask), SpriteImage?> _alphaImages = new Dictionary<(string, string), SpriteImage?>();

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session that supplies the original resource root.</param>
        internal TextureCache(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Destroys every retained texture.</summary>
        internal void ReleaseAll()
        {
            _images?.Dispose();
            _images = null;
            _imageCache.Clear();
            _alphaImages.Clear();
            foreach (var texture in _textures.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _textures.Clear();
        }

        /// <summary>Reads original interface text independently of animation renderer initialization.</summary>
        /// <param name="relativePath">The original resource path relative to the selected source root.</param>
        /// <param name="text">Receives the unchanged text when present.</param>
        /// <returns>Whether the source root contains the requested text resource.</returns>
        internal bool TryReadText(string relativePath, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrWhiteSpace(_owner._root))
            {
                return false;
            }
            _images ??= SpriteAssetProvider.ForRoot(_owner._root);
            return _images.TryReadText(relativePath, out text);
        }

        /// <summary>Loads a logical image while preserving its original dimensions.</summary>
        /// <param name="name">The image filename relative to the images directory.</param>
        /// <returns>The shared image, or null when the original resource is missing.</returns>
        internal SpriteImage? LoadImage(string name)
        {
            if (_imageCache.TryGetValue(name, out var image))
            {
                return image;
            }
            try
            {
                _images ??= SpriteAssetProvider.ForRoot(_owner._root);
                image = _images.LoadImage(name.IndexOf('/') >= 0 ? name : "images/" + name);
            }
            catch (FileNotFoundException)
            {
                image = null;
            }
            _imageCache.Add(name, image);
            return image;
        }

        /// <summary>Loads required logical artwork with a descriptive missing-resource error.</summary>
        /// <param name="name">The image filename or original relative path.</param>
        /// <returns>The image retaining its original dimensions.</returns>
        /// <exception cref="FileNotFoundException">The required original artwork is absent.</exception>
        internal SpriteImage RequiredLoadImage(string name)
        {
            return LoadImage(name) ?? throw new FileNotFoundException("Required original artwork is missing: " + name);
        }

        /// <summary>Loads an explicitly masked logical image without retaining its source GPU textures.</summary>
        /// <param name="colorName">The image filename supplying RGB channels.</param>
        /// <param name="maskName">The image filename supplying alpha in its red channel.</param>
        /// <returns>The shared composed image, or null when a source file is missing.</returns>
        internal SpriteImage? LoadAlphaGridImage(string colorName, string maskName)
        {
            var key = (colorName, maskName);
            if (_alphaImages.TryGetValue(key, out var image))
            {
                return image;
            }
            try
            {
                _images ??= SpriteAssetProvider.ForRoot(_owner._root);
                image = _images.LoadAlphaGrid(colorName, maskName);
            }
            catch (FileNotFoundException)
            {
                image = null;
            }
            _alphaImages.Add(key, image);
            return image;
        }

        /// <summary>Stores textures.</summary>
        internal readonly Dictionary<string, Texture2D?> _textures = new Dictionary<string, Texture2D?>();

        /// <summary>Loads and retains an original image by file name.</summary>
        /// <param name = "name">The image path relative to the original images directory.</param>
        /// <returns>The cached texture, or null when the file is missing or invalid.</returns>
        internal Texture2D? Load(string name)
        {
            Texture2D? texture;
            if (_textures.TryGetValue(name, out texture))
            {
                return texture;
            }

            var path = Path.Combine(_owner._root, "images", name);
            if (!File.Exists(path))
            {
                _textures[name] = null;
                return null;
            }

            var maskPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_.png");
            if (string.Equals(Path.GetExtension(path), ".jpg", StringComparison.OrdinalIgnoreCase) && File.Exists(maskPath))
            {
                texture = LoadMaskedImage(path, maskPath);
            }
            else
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                }
                else
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
            }

            _textures[name] = texture;
            return texture;
        }

        /// <summary>Calls <see cref="Load"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="name">The <c>name</c> argument forwarded to <see cref="Load"/>.</param>
        /// <returns>The non-null load result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        internal Texture2D RequiredLoad(string name) => Load(name) ?? throw new System.InvalidOperationException("'Load' returned no result.");

        /// <summary>Loads and retains an original resources.xml image with an explicit alpha grid.</summary>
        /// <param name="colorName">The original color image filename.</param>
        /// <param name="maskName">The original alpha grid filename.</param>
        /// <returns>The cached RGBA texture, or null when either source is missing.</returns>
        internal Texture2D? LoadAlphaGrid(string colorName, string maskName)
        {
            var key = colorName + "|" + maskName;
            if (_textures.TryGetValue(key, out var texture))
            {
                return texture;
            }

            var colorPath = Path.Combine(_owner._root, "images", colorName);
            var maskPath = Path.Combine(_owner._root, "images", maskName);
            if (!File.Exists(colorPath) || !File.Exists(maskPath))
            {
                _textures[key] = null;
                return null;
            }

            texture = LoadMaskedImage(colorPath, maskPath);
            _textures[key] = texture;
            return texture;
        }

        /// <summary>Calls <see cref="LoadAlphaGrid"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="colorName">The <c>colorName</c> argument forwarded to <see cref="LoadAlphaGrid"/>.</param>
        /// <param name="maskName">The <c>maskName</c> argument forwarded to <see cref="LoadAlphaGrid"/>.</param>
        /// <returns>The non-null load alpha grid result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        internal Texture2D RequiredLoadAlphaGrid(string colorName, string maskName) => LoadAlphaGrid(colorName, maskName) ?? throw new System.InvalidOperationException("'LoadAlphaGrid' returned no result.");

        /// <summary>Combines original color artwork with the red channel of its separate alpha image.</summary>
        /// <param name="colorPath">The original RGB or RGBA image path.</param>
        /// <param name="maskPath">The matching original grayscale alpha image path.</param>
        /// <returns>A new RGBA texture owned by the caller.</returns>
        /// <exception cref="IOException">A source image cannot be read.</exception>
        /// <exception cref="InvalidDataException">An image is invalid or image dimensions differ.</exception>
        internal static Texture2D LoadMaskedImage(string colorPath, string maskPath)
        {
            var color = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D? merged = null;
            try
            {
                if (!color.LoadImage(File.ReadAllBytes(colorPath)) || !mask.LoadImage(File.ReadAllBytes(maskPath))
                    || color.width != mask.width || color.height != mask.height)
                {
                    throw new InvalidDataException("Original image alpha dimensions or image data are invalid: " + maskPath);
                }

                var colors = color.GetPixels32();
                var alpha = mask.GetPixels32();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i].a = alpha[i].r;
                }

                // Loading JPEG changes the source to RGB24; a separate RGBA texture preserves the alpha.
                merged = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileName(colorPath) + "_masked",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                merged.SetPixels32(colors);
                merged.Apply(false, false);
                return merged;
            }
            catch
            {
                ReleaseImageTexture(merged);
                throw;
            }
            finally
            {
                ReleaseImageTexture(color);
                ReleaseImageTexture(mask);
            }
        }

        /// <summary>Releases a temporary decoded image on the current Unity lifecycle.</summary>
        /// <param name="texture">The temporary texture to release, if any.</param>
        private static void ReleaseImageTexture(Texture2D? texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
