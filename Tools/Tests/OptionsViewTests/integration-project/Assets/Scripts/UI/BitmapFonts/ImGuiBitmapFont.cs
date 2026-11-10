using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PvZReplica.UI.BitmapFonts
{
    /// <summary>Owns the decoded atlas pages. Load once, draw in OnGUI, and dispose when finished.</summary>
    public sealed class ImGuiBitmapFont : IDisposable
    {
        public const string DefaultResourceRoot = "PvZBitmapFonts";
        private readonly Texture2D[] pages;
        private bool disposed;
        public BmFontData Data { get; private set; }

        private ImGuiBitmapFont(BmFontData data, Texture2D[] textures)
        {
            Data = data;
            pages = textures;
        }

        /// <summary>Loads a converted layer. Bare font names resolve to the __Main layer.</summary>
        public static ImGuiBitmapFont LoadFromResources(string name, string resourceRoot = DefaultResourceRoot)
        {
            if (string.IsNullOrEmpty(name) || name == "." || name == ".." ||
                name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                throw new ArgumentException("Expected a font name without a path.", "name");
            if (string.IsNullOrEmpty(resourceRoot) || resourceRoot.StartsWith("/", StringComparison.Ordinal) ||
                resourceRoot.EndsWith("/", StringComparison.Ordinal) || resourceRoot.Contains("..") ||
                resourceRoot.IndexOfAny(new[] { '\\', ':' }) >= 0)
                throw new ArgumentException("Expected a Resources-relative root.", "resourceRoot");

            string dataPath = resourceRoot + "/Descriptions/" + name;
            TextAsset description = Resources.Load<TextAsset>(dataPath);
            if (description == null && name.IndexOf("__", StringComparison.Ordinal) < 0)
            {
                dataPath += "__Main";
                description = Resources.Load<TextAsset>(dataPath);
            }
            if (description == null)
                throw new FileNotFoundException("Bitmap font description not found in Resources: " + dataPath +
                    ". Run Tools/PrepareBitmapFonts.ps1 before building.");

            string text = new UTF8Encoding(false, true).GetString(description.bytes);
            BmFontData data = BmFontData.Parse(text);
            var textures = new Texture2D[data.Pages.Count];
            try
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    string pagePath = resourceRoot + "/Atlases/" + Path.GetFileNameWithoutExtension(data.Pages[i]);
                    TextAsset image = Resources.Load<TextAsset>(pagePath);
                    if (image == null) throw new FileNotFoundException("Bitmap font atlas not found in Resources: " + pagePath);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    textures[i] = texture;
                    texture.name = data.Pages[i];
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    if (!ImageConversion.LoadImage(texture, image.bytes, true))
                        throw new FormatException("Could not decode bitmap font atlas: " + pagePath);
                    if (texture.width != data.AtlasWidth || texture.height != data.AtlasHeight)
                        throw new FormatException("Bitmap font atlas dimensions do not match: " + pagePath);
                }
                return new ImGuiBitmapFont(data, textures);
            }
            catch
            {
                DestroyPages(textures);
                throw;
            }
        }

        public BitmapTextLayout Layout(string text, float scale = 1f, float maxWidth = float.PositiveInfinity)
        {
            ThrowIfDisposed();
            return BitmapTextLayout.Create(Data, text, scale, maxWidth);
        }

        public BitmapTextLayout Draw(string text, float x, float y, Color color, float scale = 1f,
            float maxWidth = float.PositiveInfinity, bool allowMissing = false)
        {
            BitmapTextLayout layout = Layout(text, scale, maxWidth);
            Draw(layout, x, y, color, allowMissing);
            return layout;
        }

        /// <summary>Reuses a layout; coordinates are the top-left line origin, not the ink bounds.</summary>
        public void Draw(BitmapTextLayout layout, float x, float y, Color color, bool allowMissing = false)
        {
            ThrowIfDisposed();
            if (layout == null) throw new ArgumentNullException("layout");
            if (!ReferenceEquals(layout.Font, Data)) throw new ArgumentException("Layout belongs to another font.", "layout");
            if (!allowMissing) layout.ThrowIfMissing();
            if (Event.current == null) throw new InvalidOperationException("Bitmap fonts must be drawn from OnGUI.");
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            try
            {
                GUI.color = previous * color;
                foreach (PlacedBitmapGlyph placed in layout.Glyphs)
                {
                    BmFontGlyph glyph = placed.Glyph;
                    if (glyph.Width == 0 || glyph.Height == 0) continue;
                    var destination = new Rect(x + placed.X, y + placed.Y, placed.Width, placed.Height);
                    var uv = new Rect((float)glyph.X / Data.AtlasWidth,
                        1f - (float)(glyph.Y + glyph.Height) / Data.AtlasHeight,
                        (float)glyph.Width / Data.AtlasWidth, (float)glyph.Height / Data.AtlasHeight);
                    GUI.DrawTextureWithTexCoords(destination, pages[glyph.Page], uv, true);
                }
            }
            finally { GUI.color = previous; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            DestroyPages(pages);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException("ImGuiBitmapFont");
        }

        private static void DestroyPages(Texture2D[] textures)
        {
            foreach (Texture2D texture in textures)
            {
                if (texture == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                else UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
