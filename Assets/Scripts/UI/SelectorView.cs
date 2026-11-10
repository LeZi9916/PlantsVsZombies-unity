#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>
    /// Draws the selector in logical 800x600 coordinates and reports intents only.
    /// The host owns scaling, input, progress, navigation and availability of modes.
    /// </summary>
    public sealed class SelectorView : IDisposable
    {
        /// <summary>Stores width.</summary>
        public const float Width = 800f;
        /// <summary>Stores height.</summary>
        public const float Height = 600f;
        /// <summary>Defines area.</summary>
        private sealed class Area
        {
            /// <summary>Stores action.</summary>
            public readonly SelectorAction Action;
            /// <summary>Stores bounds.</summary>
            public readonly Rect Bounds;
            /// <summary>Stores polygon.</summary>
            public readonly Vector2[] Polygon;
            /// <summary>Initializes area.</summary>
            /// <param name = "action">The action.</param>
            /// <param name = "bounds">The bounds.</param>
            /// <param name = "polygon">The polygon.</param>
            public Area(SelectorAction action, Rect bounds, params Vector2[] polygon)
            {
                Action = action;
                Bounds = bounds;
                Polygon = polygon;
            }

            /// <summary>Performs contains.</summary>
            /// <param name = "point">The point.</param>
            /// <returns>The resulting contains.</returns>
            public bool Contains(Vector2 point)
            {
                if (!Bounds.Contains(point))
                {
                    return false;
                }

                if (Polygon.Length == 0)
                {
                    return true;
                }

                var inside = false;
                for (int i = 0, j = Polygon.Length - 1; i < Polygon.Length; j = i++)
                {
                    Vector2 a = Polygon[i], b = Polygon[j];
                    if ((a.y > point.y) != (b.y > point.y) && point.x < a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y))
                    {
                        inside = !inside;
                    }
                }

                return inside;
            }
        }

        // Settled BG_Right track (71, 41) plus the native anchor minus (800, 0).
        // 0x456B33-0x456B78 associates Almanac with (1056, 387), Store with (1134, 441).
        // Use the same rectangles for drawing and hit testing.
        /// <summary>Stores almanac bounds.</summary>
        private static readonly Rect s_almanacBounds = new Rect(327, 428, 99, 99);
        /// <summary>Stores store bounds.</summary>
        private static readonly Rect s_storeBounds = new Rect(405, 482, 130, 89);
        // Coordinates are in front-to-back order. Mode polygons are the EXE's
        // button-local vertices, translated to the settled reanim track position.
        /// <summary>Stores areas.</summary>
        private static readonly Area[] s_areas =
        {
            new Area(SelectorAction.Quit, new Rect(715, 510, 47, 60)),
            new Area(SelectorAction.Help, new Rect(647, 499, 58, 32)),
            new Area(SelectorAction.Options, new Rect(565, 475, 81, 54)),
            new Area(SelectorAction.Almanac, s_almanacBounds),
            new Area(SelectorAction.Store, s_storeBounds),
            new Area(SelectorAction.ZenGarden, new Rect(171, 401, 130, 130)),
            new Area(SelectorAction.Zombatar, new Rect(32, 180, 279, 83)),
            new Area(SelectorAction.ChangeUser, new Rect(49, 136, 250, 30)),
            new Area(SelectorAction.Adventure, new Rect(405, 65, 329, 125), new Vector2(412, 66), new Vector2(733, 95), new Vector2(719, 190), new Vector2(406, 143)),
            new Area(SelectorAction.Survival, new Rect(413, 328, 267, 124), new Vector2(420, 329), new Vector2(680, 390), new Vector2(670, 452), new Vector2(420, 385)),
            new Area(SelectorAction.Puzzle, new Rect(410, 257, 281, 121), new Vector2(412, 257), new Vector2(691, 312), new Vector2(678, 378), new Vector2(413, 317)),
            new Area(SelectorAction.MiniGames, new Rect(406, 173, 313, 130), new Vector2(410, 175), new Vector2(718, 224), new Vector2(702, 303), new Vector2(413, 250))
        };
        /// <summary>Stores root.</summary>
        private readonly string _root;
        /// <summary>Stores textures.</summary>
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        /// <summary>Stores locked.</summary>
        private readonly HashSet<SelectorAction> _locked = new HashSet<SelectorAction>();
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <param name = "resourceRoot">Directory containing the images and reanim directories.</param>
        public SelectorView(string resourceRoot)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
            {
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            }

            _root = Path.GetFullPath(resourceRoot);
        }

        /// <summary>The host sets this from HitTest after converting the pointer to logical coordinates.</summary>
        public SelectorAction HoveredAction { get; set; }

        /// <summary>
        /// Selects the original darkened mode-sign state. Unlocking is visual only and
        /// does not imply that the host implements the mode. All signs default to unlocked.
        /// </summary>
        public void SetModeLocked(SelectorAction action, bool isLocked)
        {
            ThrowIfDisposed();
            if (action != SelectorAction.MiniGames && action != SelectorAction.Puzzle && action != SelectorAction.Survival)
            {
                throw new ArgumentOutOfRangeException(nameof(action));
            }

            if (isLocked)
            {
                _locked.Add(action);
            }
            else
            {
                _locked.Remove(action);
            }
        }

        /// <summary>Returns an intent even for a locked or unimplemented entry. It never handles the click.</summary>
        public SelectorAction HitTest(Vector2 position)
        {
            ThrowIfDisposed();
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || position.x < 0f || position.y < 0f || position.x >= Width || position.y >= Height)
            {
                return SelectorAction.None;
            }

            foreach (Area area in s_areas)
            {
                if (area.Contains(position))
                {
                    return area.Action;
                }
            }

            return SelectorAction.None;
        }

        /// <summary>Submits the retained Canvas elements using the caller's viewport transform.</summary>
        public void Draw(float seconds)
        {
            ThrowIfDisposed();
            CanvasUI.BeginGroup(new Rect(0, 0, Width, Height));
            try
            {
                // The menu-open final frame, plus the independently animated sign's final frame.
                Image("reanim/SelectorScreen_BG", 0, 0, 800, 600);
                Image("reanim/SelectorScreen_BG_Center", 80, 250, 720, 350, true);
                Image("reanim/SelectorScreen_BG_Left", 0, -79.8f, 476, 680, true);
                Image("reanim/SelectorScreen_BG_Right", 71, 41, 730, 560, true);
                Image("reanim/SelectorScreen_almanac_key_shadow", 391.9f, 460.3f, 181, 110);
                Image("reanim/SelectorScreen_Shadow_Survival", 407, 177.2f, 318, 136);
                Button(SelectorAction.MiniGames, "reanim/SelectorScreen_Survival_button", "reanim/SelectorScreen_Survival_highlight", 406, 173.1f, 313, 133);
                Image("reanim/SelectorScreen_Shadow_Challenge", 411, 259.9f, 289, 127);
                Button(SelectorAction.Puzzle, "reanim/SelectorScreen_Challenges_button", "reanim/SelectorScreen_Challenges_highlight", 410, 257.5f, 286, 122);
                Image("reanim/SelectorScreen_Shadow_ZenGarden", 412, 330, 269, 127);
                Button(SelectorAction.Survival, "reanim/SelectorScreen_Vasebreaker_button", "reanim/SelectorScreen_vasebreaker_highlight", 413, 328, 266, 123);
                Image("reanim/SelectorScreen_Shadow_StartAdventure", 398, 66, 350, 150);
                Button(SelectorAction.Adventure, "reanim/SelectorScreen_StartAdventure_Button1", "reanim/SelectorScreen_StartAdventure_Highlight", 405, 65, 331, 146);
                Image("reanim/SelectorScreen_Leaf3", 255.3f, 553.5f, 45.4f, 61.2f);
                Image("reanim/SelectorScreen_Leaf2", 24, 558, 34.5f, 33.5f);
                Image("reanim/SelectorScreen_Leaf2", 99.1f, 545.6f, 34.5f, 33.5f);
                Image("reanim/SelectorScreen_Leaves", -2.4f, 537.5f, 243.6f, 62.9f);
                Image("reanim/SelectorScreen_Leaf4", 222.8f, 558.9f, 32.5f, 45.7f);
                Image("reanim/SelectorScreen_Leaf5", 250.1f, 573.1f, 21.5f, 31.7f);
                Image("reanim/SelectorScreen_Leaf1", 285.6f, 565.5f, 40.6f, 41.6f);
                Image("reanim/SelectorScreen_Flower1", 680, 414, 36, 47);
                Image("reanim/SelectorScreen_Flower2", 640, 437, 43, 43);
                Image("reanim/SelectorScreen_Flower3", 740, 458, 46, 53);
                Image("reanim/SelectorScreen_WoodSign1", 22.9f, -7.9f, 293, 150);
                Button(SelectorAction.ChangeUser, "reanim/SelectorScreen_WoodSign2", "reanim/SelectorScreen_WoodSign2_press", 25.5f, 126.8f, 291, 71);
                Button(SelectorAction.Zombatar, "reanim/SelectorScreen_WoodSign3", "reanim/SelectorScreen_WoodSign3_press", 32.4f, 180.4f, 279, 83);
                Button(SelectorAction.ZenGarden, "images/SelectorScreen_ZenGarden", "images/SelectorScreen_ZenGardenHighlight", 171, 401, 191, 163);
                Button(SelectorAction.Store, "images/SelectorScreen_Store", "images/SelectorScreen_StoreHighlight", s_storeBounds);
                Button(SelectorAction.Almanac, "images/SelectorScreen_Almanac", "images/SelectorScreen_AlmanacHighlight", s_almanacBounds);
                Button(SelectorAction.Options, "images/SelectorScreen_Options1", "images/SelectorScreen_Options2", 565, 490, 81, 31);
                Button(SelectorAction.Help, "images/SelectorScreen_Help1", "images/SelectorScreen_Help2", 652, 504, 48, 22);
                Button(SelectorAction.Quit, "images/SelectorScreen_Quit1", "images/SelectorScreen_Quit2", 715, 540, 47, 27);
            }
            finally
            {
                CanvasUI.EndGroup();
            }
        }

        /// <summary>Performs button.</summary>
        /// <param name = "action">The action.</param>
        /// <param name = "normal">The normal.</param>
        /// <param name = "highlight">The highlight.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        private void Button(SelectorAction action, string normal, string highlight, float x, float y, float width, float height)
        {
            var texture = HoveredAction == action ? GetTexture(highlight, false) : null;
            if (texture == null)
            {
                texture = GetTexture(normal, false);
            }

            if (texture == null)
            {
                return;
            }

            var previous = CanvasUI.Color;
            try
            {
                if (_locked.Contains(action))
                {
                    CanvasUI.Color = new Color(previous.r * 0.5f, previous.g * 0.5f, previous.b * 0.5f, previous.a);
                }

                CanvasUI.DrawTexture(new Rect(x, y, width, height), texture, ScaleMode.StretchToFill, true);
            }
            finally
            {
                CanvasUI.Color = previous;
            }
        }

        /// <summary>Performs button.</summary>
        /// <param name = "action">The action.</param>
        /// <param name = "normal">The normal.</param>
        /// <param name = "highlight">The highlight.</param>
        /// <param name = "bounds">The bounds.</param>
        private void Button(SelectorAction action, string normal, string highlight, Rect bounds)
        {
            Button(action, normal, highlight, bounds.x, bounds.y, bounds.width, bounds.height);
        }

        /// <summary>Performs image.</summary>
        /// <param name = "path">The path.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "mask">The mask.</param>
        private void Image(string path, float x, float y, float width, float height, bool mask = false)
        {
            var texture = GetTexture(path, mask);
            if (texture != null)
            {
                CanvasUI.DrawTexture(new Rect(x, y, width, height), texture, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Performs get texture.</summary>
        /// <param name = "relativePath">The relative path.</param>
        /// <param name = "requiresMask">The requires mask.</param>
        /// <returns>The resulting get texture.</returns>
        /// <exception cref = "FileNotFoundException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private Texture2D GetTexture(string relativePath, bool requiresMask)
        {
            Texture2D texture;
            if (_textures.TryGetValue(relativePath, out texture))
            {
                return texture;
            }

            texture = null;
            Texture2D mask = null;
            try
            {
                var basePath = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                var hasPng = File.Exists(basePath + ".png");
                var path = basePath + (hasPng ? ".png" : ".jpg");
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Image not found", path);
                }

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                {
                    throw new InvalidDataException("Could not decode " + path);
                }

                if (!hasPng && requiresMask)
                {
                    var maskPath = basePath + "_.png";
                    if (!File.Exists(maskPath))
                    {
                        throw new FileNotFoundException("Alpha mask not found", maskPath);
                    }

                    mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    mask.hideFlags = HideFlags.HideAndDontSave;
                    if (!ImageConversion.LoadImage(mask, File.ReadAllBytes(maskPath), false))
                    {
                        throw new InvalidDataException("Could not decode " + maskPath);
                    }

                    if (texture.width != mask.width || texture.height != mask.height)
                    {
                        throw new InvalidDataException("Alpha mask dimensions do not match " + path);
                    }

                    var pixels = texture.GetPixels32();
                    var alpha = mask.GetPixels32();
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].a = alpha[i].r;
                    }

                    var combined = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    combined.hideFlags = HideFlags.HideAndDontSave;
                    combined.SetPixels32(pixels);
                    DestroyTexture(texture);
                    texture = combined;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply(false, true);
            }
            catch (Exception exception)
            {
                DestroyTexture(texture);
                texture = null;
                Debug.LogWarning("SelectorView: " + relativePath + ": " + exception.Message);
            }
            finally
            {
                DestroyTexture(mask);
            }

            _textures.Add(relativePath, texture);
            return texture;
        }

        /// <summary>Performs destroy texture.</summary>
        /// <param name = "texture">The texture.</param>
        private static void DestroyTexture(Texture2D texture)
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

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SelectorView));
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
            foreach (Texture2D texture in _textures.Values)
            {
                DestroyTexture(texture);
            }

            _textures.Clear();
            _locked.Clear();
        }
    }
}
