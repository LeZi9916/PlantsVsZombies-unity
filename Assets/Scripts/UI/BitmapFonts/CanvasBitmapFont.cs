#nullable enable
using System;
using System.IO;
using System.Text;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Owns the decoded atlas pages. Load once, submit glyphs to Canvas, and dispose when finished.</summary>
    public sealed class CanvasBitmapFont : IDisposable
    {
        /// <summary>Stores default resource root.</summary>
        public const string DefaultResourceRoot = "PvZBitmapFonts";
        /// <summary>Stores pages.</summary>
        private readonly Texture2D[] _pages;
        /// <summary>Records which pages were decoded and are owned by this font.</summary>
        private readonly bool[] _ownedPages;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets data.</summary>
        public BmFontData Data { get; private set; }

        /// <summary>Initializes ownership of decoded bitmap atlas pages.</summary>
        /// <param name = "data">The data.</param>
        /// <param name = "textures">The textures.</param>
        /// <param name="ownedPages">Whether each page is privately decoded or a shared Resources asset.</param>
        private CanvasBitmapFont(BmFontData data, Texture2D[] textures, bool[] ownedPages)
        {
            Data = data;
            _pages = textures;
            _ownedPages = ownedPages;
        }

        /// <summary>Loads a converted layer. Bare font names resolve to the __Main layer.</summary>
        public static CanvasBitmapFont LoadFromResources(string name, string resourceRoot = DefaultResourceRoot)
        {
            if (string.IsNullOrEmpty(name) || name == "." || name == ".." || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                throw new ArgumentException("Expected a font name without a path.", "name");
            }

            if (string.IsNullOrEmpty(resourceRoot) || resourceRoot.StartsWith("/", StringComparison.Ordinal) || resourceRoot.EndsWith("/", StringComparison.Ordinal) || resourceRoot.Contains("..") || resourceRoot.IndexOfAny(new[] { '\\', ':' }) >= 0)
            {
                throw new ArgumentException("Expected a Resources-relative root.", "resourceRoot");
            }

            var dataPath = resourceRoot + "/Descriptions/" + name;
            var description = Resources.Load<TextAsset>(dataPath);
            if (description == null && name.IndexOf("__", StringComparison.Ordinal) < 0)
            {
                dataPath += "__Main";
                description = Resources.Load<TextAsset>(dataPath);
            }

            if (description == null)
            {
                throw new FileNotFoundException("Bitmap font description not found in Resources: " + dataPath + ". Run Tools/PrepareBitmapFonts.ps1 before building.");
            }

            var text = new UTF8Encoding(false, true).GetString(description.bytes);
            var data = BmFontData.Parse(text);
            var textures = new Texture2D[data.Pages.Count];
            var ownedPages = new bool[textures.Length];
            try
            {
                for (var i = 0; i < textures.Length; i++)
                {
                    var pagePath = resourceRoot + "/Atlases/" + Path.GetFileNameWithoutExtension(data.Pages[i]);
                    var replacement = HighQualityArtwork.LoadTexture(pagePath, data.AtlasWidth, data.AtlasHeight);
                    if (replacement != null)
                    {
                        textures[i] = replacement;
                        continue;
                    }

                    var image = Resources.Load<TextAsset>(pagePath);
                    if (image == null)
                    {
                        throw new FileNotFoundException("Bitmap font atlas not found in Resources: " + pagePath);
                    }

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    textures[i] = texture;
                    ownedPages[i] = true;
                    texture.name = data.Pages[i];
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    if (!ImageConversion.LoadImage(texture, image.bytes, false))
                    {
                        throw new FormatException("Could not decode bitmap font atlas: " + pagePath);
                    }

                    // Original ImageFont descriptors name the bare image; a sole _Name PNG is
                    // the grayscale alpha image, not opaque black RGB artwork.
                    if (Path.GetFileName(data.Pages[i]).StartsWith("_", StringComparison.Ordinal))
                    {
                        var pixels = texture.GetPixels32();
                        var alphaOnly = true;
                        foreach (Color32 pixel in pixels)
                        {
                            if (pixel.a != 255 || pixel.r != pixel.g || pixel.r != pixel.b)
                            {
                                alphaOnly = false;
                                break;
                            }
                        }

                        if (alphaOnly)
                        {
                            for (var j = 0; j < pixels.Length; j++)
                            {
                                pixels[j] = new Color32(255, 255, 255, pixels[j].r);
                            }

                            texture.SetPixels32(pixels);
                        }
                    }

                    texture.Apply(false, true);
                    if (texture.width != data.AtlasWidth || texture.height != data.AtlasHeight)
                    {
                        throw new FormatException("Bitmap font atlas dimensions do not match: " + pagePath);
                    }
                }

                return new CanvasBitmapFont(data, textures, ownedPages);
            }
            catch
            {
                DestroyPages(textures, ownedPages);
                throw;
            }
        }

        /// <summary>Performs layout.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "maxWidth">The max width.</param>
        /// <returns>The resulting layout.</returns>
        public BitmapTextLayout Layout(string text, float scale = 1f, float maxWidth = float.PositiveInfinity)
        {
            ThrowIfDisposed();
            return BitmapTextLayout.Create(Data, text, scale, maxWidth);
        }

        /// <summary>Performs draw.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "maxWidth">The max width.</param>
        /// <param name = "allowMissing">The allow missing.</param>
        /// <returns>The resulting draw.</returns>
        public BitmapTextLayout Draw(string text, float x, float y, Color color, float scale = 1f, float maxWidth = float.PositiveInfinity, bool allowMissing = false)
        {
            var layout = Layout(text, scale, maxWidth);
            Draw(layout, x, y, color, allowMissing);
            return layout;
        }

        /// <summary>Reuses a layout; coordinates are the top-left line origin, not the ink bounds.</summary>
        public void Draw(BitmapTextLayout layout, float x, float y, Color color, bool allowMissing = false)
        {
            ThrowIfDisposed();
            if (layout == null)
            {
                throw new ArgumentNullException("layout");
            }

            if (!ReferenceEquals(layout.Font, Data))
            {
                throw new ArgumentException("Layout belongs to another font.", "layout");
            }

            if (!allowMissing)
            {
                layout.ThrowIfMissing();
            }

            var previous = CanvasUI.Color;
            try
            {
                CanvasUI.Color = previous * color;
                using var profile = PlantsVsZombies.Rendering.SpriteRenderingProfiler.Measure(PlantsVsZombies.Rendering.SpriteRenderingProfiler.BitmapText);
                var glyphs = layout.Glyphs;
                var first = 0;
                while (first < glyphs.Count)
                {
                    var page = glyphs[first].Glyph.Page;
                    var end = first + 1;
                    while (end < glyphs.Count && end - first < 1024 && glyphs[end].Glyph.Page == page)
                    {
                        end++;
                    }
                    CanvasUI.DrawBitmapRun(layout, first, end - first, _pages[page], new Vector2(x, y));
                    first = end;
                }
            }
            finally
            {
                CanvasUI.Color = previous;
            }
        }

        /// <summary>Performs dispose.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DestroyPages(_pages, _ownedPages);
        }

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("CanvasBitmapFont");
            }
        }

        /// <summary>Performs destroy pages.</summary>
        /// <param name = "textures">The textures.</param>
        /// <param name="ownedPages">Which textures were decoded by this font and need destruction.</param>
        private static void DestroyPages(Texture2D[] textures, bool[] ownedPages)
        {
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (!ownedPages[i] || texture == null)
                {
                    continue;
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
            Array.Clear(textures, 0, textures.Length);
        }
    }
}
