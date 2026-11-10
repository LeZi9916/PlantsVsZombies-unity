#nullable enable
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Original PC materials. Texture lookups use the host cache; no generated artwork or GUI skins.</summary>
    public static class OriginalUiWidgets
    {
        /// <summary>Stores fonts.</summary>
        private static readonly Dictionary<string, CanvasBitmapFont> s_fonts = new Dictionary<string, CanvasBitmapFont>();
        /// <summary>Stores failed fonts.</summary>
        private static readonly HashSet<string> s_failedFonts = new HashSet<string>();
        /// <summary>Bounds retained text, font, and wrap-width combinations across changing UI captions.</summary>
        private const int LayoutCacheCapacity = 512;
        /// <summary>Reuses immutable glyph placements without mixing fonts or wrapping widths.</summary>
        private static readonly Dictionary<(CanvasBitmapFont Font, string Text, float Width), BitmapTextLayout> s_layouts = new Dictionary<(CanvasBitmapFont, string, float), BitmapTextLayout>();
        /// <summary>Stores stone text.</summary>
        public static readonly Color StoneText = new Color(21f / 255f, 175f / 255f, 0f, 1f);
        /// <summary>Performs hovered.</summary>
        /// <param name = "rect">The rect.</param>
        /// <returns>The resulting hovered.</returns>
        public static bool Hovered(Rect rect)
        {
            return rect.Contains(CanvasUI.PointerPosition);
        }

        /// <summary>Performs pressed.</summary>
        /// <param name = "rect">The rect.</param>
        /// <returns>The resulting pressed.</returns>
        public static bool Pressed(Rect rect)
        {
            return Hovered(rect) && Input.GetMouseButton(0);
        }

        /// <summary>Performs dialog frame.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "tallBottom">The tall bottom.</param>
        /// <param name = "header">The header.</param>
        public static void DialogFrame(Func<string, Texture2D> texture, Rect rect, bool tallBottom = true, bool header = true)
        {
            Texture2D tl = texture("dialog_topleft.png"), tr = texture("dialog_topright.png"),
                bl = texture(tallBottom ? "dialog_bigbottomleft.png" : "dialog_bottomleft.png"),
                br = texture(tallBottom ? "dialog_bigbottomright.png" : "dialog_bottomright.png");
            if (tl == null || tr == null || bl == null || br == null)
            {
                return;
            }

            var centerWidth = Math.Max(0, rect.width - tl.width - tr.width);
            var middleHeight = Math.Max(0, rect.height - tr.height - bl.height);
            Image(tl, new Rect(rect.x, rect.y, tl.width, tl.height));
            Tile(texture("dialog_topmiddle.png"), new Rect(rect.x + tl.width, rect.y, centerWidth, tl.height));
            Image(tr, new Rect(rect.xMax - tr.width, rect.y, tr.width, tr.height));
            Tile(texture("dialog_centerleft.png"), new Rect(rect.x, rect.y + tr.height, tl.width, middleHeight));
            Tile(texture("dialog_centermiddle.png"), new Rect(rect.x + tl.width, rect.y + tr.height, centerWidth, middleHeight));
            Tile(texture("dialog_centerright.png"), new Rect(rect.xMax - tr.width, rect.y + tr.height, tr.width, middleHeight));
            Image(bl, new Rect(rect.x, rect.yMax - bl.height, bl.width, bl.height));
            Tile(texture(tallBottom ? "dialog_bigbottommiddle.png" : "dialog_bottommiddle.png"), new Rect(rect.x + bl.width, rect.yMax - bl.height, Math.Max(0, rect.width - bl.width - br.width), bl.height));
            Image(br, new Rect(rect.xMax - br.width, rect.yMax - br.height, br.width, br.height));
            var cap = header ? texture("dialog_header.png") : null;
            if (cap != null)
            {
                Image(cap, new Rect(rect.center.x - cap.width / 2f, rect.y - 8, cap.width, cap.height));
            }
        }

        // PC 0x452720/0x4532B0: preserve end caps, tile the original middle with a final source crop.
        // Pressed graphics shift X by1; caption shifts X/Y by1. Return value is caption displacement.
        /// <summary>Performs stone button.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "hovered">The hovered.</param>
        /// <param name = "pressed">The pressed.</param>
        /// <param name = "enabled">The enabled.</param>
        /// <returns>The resulting stone button.</returns>
        public static Vector2 StoneButton(Func<string, Texture2D> texture, Rect rect, bool hovered = false, bool pressed = false, bool enabled = true)
        {
            pressed &= enabled;
            Texture2D left = texture(pressed ? "button_down_left.png" : "button_left.png"),
                middle = texture(pressed ? "button_down_middle.png" : "button_middle.png"),
                right = texture(pressed ? "button_down_right.png" : "button_right.png");
            if (left == null || middle == null || right == null)
            {
                return Vector2.zero;
            }

            var old = CanvasUI.Color;
            try
            {
                if (!enabled)
                {
                    CanvasUI.Color = new Color(old.r * .5f, old.g * .5f, old.b * .5f, old.a);
                }

                var x = rect.x + (pressed ? 1 : 0);
                Image(left, new Rect(x, rect.y, left.width, left.height));
                Tile(middle, new Rect(x + left.width, rect.y, Math.Max(0, rect.width - left.width - right.width), middle.height));
                Image(right, new Rect(x + rect.width - right.width, rect.y, right.width, right.height));
            }
            finally
            {
                CanvasUI.Color = old;
            }

            return pressed ? Vector2.one : Vector2.zero;
        }

        /// <summary>Performs image button.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "normal">The normal.</param>
        /// <param name = "hover">The hover.</param>
        /// <param name = "down">The down.</param>
        /// <param name = "hovered">The hovered.</param>
        /// <param name = "pressed">The pressed.</param>
        /// <param name = "enabled">The enabled.</param>
        public static void ImageButton(Func<string, Texture2D> texture, Rect rect, string normal, string hover = null, string down = null, bool hovered = false, bool pressed = false, bool enabled = true)
        {
            var name = enabled && pressed && down != null ? down : enabled && hovered && hover != null ? hover : normal;
            var image = texture(name);
            if (image == null && name != normal)
            {
                image = texture(normal);
            }

            var old = CanvasUI.Color;
            try
            {
                if (!enabled)
                {
                    CanvasUI.Color = new Color(old.r * .5f, old.g * .5f, old.b * .5f, old.a);
                }

                Image(image, rect);
            }
            finally
            {
                CanvasUI.Color = old;
            }
        }

        // PC LawnCommon DrawEditBox / Graphics.DrawImageBox: thirds of original editbox, outside edit bounds by5/6.
        /// <summary>Performs edit box.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "field">The field.</param>
        public static void EditBox(Func<string, Texture2D> texture, Rect field)
        {
            var image = texture("editbox.png");
            if (image == null)
            {
                return;
            }

            NineSlice(image, new Rect(field.x - 5, field.y - 6, field.width + 10, field.height + 12), image.width / 3, image.height / 3);
        }

        /// <summary>Performs transparent text field.</summary>
        /// <param name = "source">The source.</param>
        /// <returns>The resulting transparent text field.</returns>
        public static GUIStyle TransparentTextField(GUIStyle source)
        {
            var style = new GUIStyle(source);
            foreach (GUIStyleState state in new[]
            {
                style.normal,
                style.hover,
                style.active,
                style.focused,
                style.onNormal,
                style.onHover,
                style.onActive,
                style.onFocused
            }

            )
            {
                state.background = null;
                state.textColor = new Color(240f / 255f, 240f / 255f, 1f);
            }

            style.border = new RectOffset(0, 0, 0, 0);
            return style;
        }

        // TodDrawString uses a baseline; preserving it matters on the original award plaque.
        /// <summary>Performs text at baseline.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "centerX">The center x.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "maxWidth">The max width.</param>
        /// <param name = "originalFont">The original font.</param>
        /// <param name = "color">The color.</param>
        /// <returns>The resulting text at baseline.</returns>
        public static bool TextAtBaseline(string text, float centerX, float baseline, float maxWidth, string originalFont, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            CanvasBitmapFont bitmap;
            if (!s_fonts.TryGetValue(originalFont, out bitmap))
            {
                if (s_failedFonts.Contains(originalFont))
                {
                    return false;
                }

                try
                {
                    bitmap = CanvasBitmapFont.LoadFromResources(originalFont);
                    s_fonts.Add(originalFont, bitmap);
                }
                catch (Exception error)when (error is IOException || error is FormatException)
                {
                    s_failedFonts.Add(originalFont);
                    return false;
                }
            }

            var layout = GetLayout(bitmap, text);
            if (layout.MissingGlyphs.Count != 0 || layout.LineCount != 1 || layout.Width > maxWidth)
            {
                return false;
            }

            bitmap.Draw(layout, centerX - Mathf.Floor(layout.Width * .5f), baseline - bitmap.Data.Base, color);
            return true;
        }

        /// <summary>Performs text.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "originalFont">The original font.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "centered">The centered.</param>
        /// <param name = "wrap">The wrap.</param>
        /// <returns>The resulting text.</returns>
        public static bool Text(Rect rect, string text, string originalFont, Color color, bool centered = true, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            CanvasBitmapFont font;
            if (!s_fonts.TryGetValue(originalFont, out font))
            {
                if (s_failedFonts.Contains(originalFont))
                {
                    return false;
                }

                try
                {
                    font = CanvasBitmapFont.LoadFromResources(originalFont);
                    s_fonts.Add(originalFont, font);
                }
                catch (Exception e)when (e is IOException || e is FormatException)
                {
                    s_failedFonts.Add(originalFont);
                    return false;
                }
            }

            var layout = GetLayout(font, text, wrap ? rect.width : float.PositiveInfinity);
            if (layout.MissingGlyphs.Count != 0)
            {
                return false;
            }

            var x = centered ? rect.center.x - layout.Width / 2f : rect.x;
            var y = rect.center.y - layout.Height / 2f;
            font.Draw(layout, x, y, color);
            return true;
        }

        /// <summary>Performs stone label.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "hovered">The hovered.</param>
        /// <param name = "pressed">The pressed.</param>
        /// <param name = "enabled">The enabled.</param>
        /// <returns>The resulting stone label.</returns>
        public static bool StoneLabel(Rect rect, string text, bool hovered = false, bool pressed = false, bool enabled = true)
        {
            var name = hovered && enabled ? "DwarvenTodcraft18BrightGreenInset" : "DwarvenTodcraft18GreenInset";
            CanvasBitmapFont bitmap;
            if (!s_fonts.TryGetValue(name, out bitmap))
            {
                if (s_failedFonts.Contains(name))
                {
                    return false;
                }

                try
                {
                    bitmap = CanvasBitmapFont.LoadFromResources(name);
                    s_fonts.Add(name, bitmap);
                }
                catch (Exception e)when (e is IOException || e is FormatException)
                {
                    s_failedFonts.Add(name);
                    return false;
                }
            }

            var layout = GetLayout(bitmap, text ?? "");
            if (layout.MissingGlyphs.Count != 0)
            {
                return false;
            }

            var height = bitmap.Data.LineHeight;
            var baseline = rect.y + (pressed ? 1 : 0) + (int)((rect.height - height / 6 - 1f + height) / 2f) - 4f;
            var x = rect.x + (int)((rect.width - layout.Width) / 2f) + 1 + (pressed ? 1 : 0);
            bitmap.Draw(layout, x, baseline - bitmap.Data.Base, enabled ? Color.white : new Color(.5f, .5f, .5f));
            return true;
        }

        /// <summary>Reuses immutable text placements while bounding retained captions.</summary>
        /// <param name="font">The owning font and atlas identity.</param>
        /// <param name="text">The exact caption to lay out.</param>
        /// <param name="maxWidth">The character-wrapping width, or infinity for an unwrapped caption.</param>
        /// <returns>The existing layout or a newly cached layout with the same rendering semantics.</returns>
        /// <exception cref="ArgumentException">The caption contains an unpaired UTF-16 surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The wrapping width is NaN or not positive.</exception>
        private static BitmapTextLayout GetLayout(CanvasBitmapFont font, string text, float maxWidth = float.PositiveInfinity)
        {
            var key = (font, text, maxWidth);
            if (!s_layouts.TryGetValue(key, out var layout))
            {
                if (s_layouts.Count >= LayoutCacheCapacity)
                {
                    s_layouts.Clear();
                }

                layout = font.Layout(text, 1f, maxWidth);
                s_layouts.Add(key, layout);
            }

            return layout;
        }

        /// <summary>Performs image.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "destination">The destination.</param>
        public static void Image(Texture2D texture, Rect destination)
        {
            if (texture != null && destination.width > 0 && destination.height > 0)
            {
                CanvasUI.DrawTexture(destination, texture, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Performs tile.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "destination">The destination.</param>
        public static void Tile(Texture2D texture, Rect destination)
        {
            if (texture == null || destination.width <= 0 || destination.height <= 0)
            {
                return;
            }

            for (var y = destination.y; y < destination.yMax; y += texture.height)
            {
                for (var x = destination.x; x < destination.xMax; x += texture.width)
                {
                    float w = Math.Min(texture.width, destination.xMax - x), h = Math.Min(texture.height, destination.yMax - y);
                    CanvasUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), texture, new Rect(0, 1 - h / texture.height, w / texture.width, h / texture.height), true);
                }
            }
        }

        /// <summary>Performs nine slice.</summary>
        /// <param name = "image">The image.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "borderX">The border x.</param>
        /// <param name = "borderY">The border y.</param>
        public static void NineSlice(Texture2D image, Rect rect, int borderX, int borderY)
        {
            float imageWidth = image.width, imageHeight = image.height;
            float bx = Math.Min(borderX, rect.width / 2), by = Math.Min(borderY, rect.height / 2);
            for (var row = 0; row < 3; row++)
            {
                var sourceTop = SliceEdge(row, 0f, imageHeight, borderY);
                var sourceBottom = SliceEdge(row + 1, 0f, imageHeight, borderY);
                var top = SliceEdge(row, rect.y, rect.height, by);
                var bottom = SliceEdge(row + 1, rect.y, rect.height, by);
                for (var col = 0; col < 3; col++)
                {
                    var sourceLeft = SliceEdge(col, 0f, imageWidth, borderX);
                    var sourceRight = SliceEdge(col + 1, 0f, imageWidth, borderX);
                    var left = SliceEdge(col, rect.x, rect.width, bx);
                    var right = SliceEdge(col + 1, rect.x, rect.width, bx);
                    CanvasUI.DrawTextureWithTexCoords(new Rect(left, top, right - left, bottom - top), image,
                        new Rect(sourceLeft / imageWidth, 1f - sourceBottom / imageHeight, (sourceRight - sourceLeft) / imageWidth, (sourceBottom - sourceTop) / imageHeight), true);
                }
            }
        }

        /// <summary>Computes one of the four edges of a three-part image axis without a temporary array.</summary>
        /// <param name="index">The zero-based edge, from zero through three.</param>
        /// <param name="origin">The beginning of the axis.</param>
        /// <param name="length">The complete source or destination length.</param>
        /// <param name="border">The border retained at each end of the axis.</param>
        /// <returns>The requested source or destination edge coordinate.</returns>
        private static float SliceEdge(int index, float origin, float length, float border)
        {
            return index switch
            {
                0 => origin,
                1 => origin + border,
                2 => origin + length - border,
                _ => origin + length
            };
        }

        /// <summary>Performs release fonts.</summary>
        public static void ReleaseFonts()
        {
            s_layouts.Clear();
            foreach (CanvasBitmapFont font in s_fonts.Values)
            {
                font.Dispose();
            }

            s_fonts.Clear();
            s_failedFonts.Clear();
        }
    }
}
