#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PlantsVsZombies.UI.BitmapFonts
{
    /// <summary>Owns the decoded atlas pages. Load once, draw in OnGUI, and dispose when finished.</summary>
    public sealed class ImGuiBitmapFont : IDisposable
    {
        /// <summary>Stores default resource root.</summary>
        public const string DefaultResourceRoot = "PvZBitmapFonts";
        /// <summary>Stores pages.</summary>
        private readonly Texture2D[] _pages;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets data.</summary>
        public BmFontData Data { get; private set; }

        /// <summary>Initializes im gui bitmap font.</summary>
        /// <param name = "data">The data.</param>
        /// <param name = "textures">The textures.</param>
        private ImGuiBitmapFont(BmFontData data, Texture2D[] textures)
        {
            Data = data;
            _pages = textures;
        }

        /// <summary>Loads a converted layer. Bare font names resolve to the __Main layer.</summary>
        public static ImGuiBitmapFont LoadFromResources(string name, string resourceRoot = DefaultResourceRoot)
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
            try
            {
                for (var i = 0; i < textures.Length; i++)
                {
                    var pagePath = resourceRoot + "/Atlases/" + Path.GetFileNameWithoutExtension(data.Pages[i]);
                    var image = Resources.Load<TextAsset>(pagePath);
                    if (image == null)
                    {
                        throw new FileNotFoundException("Bitmap font atlas not found in Resources: " + pagePath);
                    }

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    textures[i] = texture;
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

                return new ImGuiBitmapFont(data, textures);
            }
            catch
            {
                DestroyPages(textures);
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

            if (Event.current == null)
            {
                throw new InvalidOperationException("Bitmap fonts must be drawn from OnGUI.");
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var previous = GUI.color;
            try
            {
                GUI.color = previous * color;
                foreach (PlacedBitmapGlyph placed in layout.Glyphs)
                {
                    var glyph = placed.Glyph;
                    if (glyph.Width == 0 || glyph.Height == 0)
                    {
                        continue;
                    }

                    var destination = new Rect(x + placed.X, y + placed.Y, placed.Width, placed.Height);
                    var uv = new Rect((float)glyph.X / Data.AtlasWidth, 1f - (float)(glyph.Y + glyph.Height) / Data.AtlasHeight, (float)glyph.Width / Data.AtlasWidth, (float)glyph.Height / Data.AtlasHeight);
                    GUI.DrawTextureWithTexCoords(destination, _pages[glyph.Page], uv, true);
                }
            }
            finally
            {
                GUI.color = previous;
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
            DestroyPages(_pages);
        }

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("ImGuiBitmapFont");
            }
        }

        /// <summary>Performs destroy pages.</summary>
        /// <param name = "textures">The textures.</param>
        private static void DestroyPages(Texture2D[] textures)
        {
            foreach (Texture2D texture in textures)
            {
                if (texture == null)
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
        }
    }
}
