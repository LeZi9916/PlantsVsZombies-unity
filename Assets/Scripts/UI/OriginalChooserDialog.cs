#nullable enable
using System;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI.BitmapFonts;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Draws PC LawnDialog 464B40/464FF0/4651D0 for native chooser warnings and Imitater selection.</summary>
    internal sealed class OriginalChooserDialog : IDisposable
    {
        /// <summary>Owns the original header font and native metrics.</summary>
        private readonly CanvasBitmapFont _header = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft24");
        /// <summary>Owns the original wrapped body font.</summary>
        private readonly CanvasBitmapFont _body = CanvasBitmapFont.LoadFromResources("DwarvenTodcraft15");
        /// <summary>Loads unchanged original dialog artwork.</summary>
        private readonly Func<string, SpriteImage?> _texture;
        /// <summary>Retains the translated title for cached layout.</summary>
        private string _title = string.Empty;
        /// <summary>Retains the translated body for cached layout.</summary>
        private string _message = string.Empty;
        /// <summary>Retains the measured original title.</summary>
        private BitmapTextLayout? _titleLayout;
        /// <summary>Retains the measured original body.</summary>
        private BitmapTextLayout? _bodyLayout;
        /// <summary>Gets the native centered dialog rectangle.</summary>
        internal Rect Bounds { get; private set; }
        /// <summary>Gets PC 464FF0's affirmative button rectangle.</summary>
        internal Rect YesBounds { get; private set; }
        /// <summary>Gets PC 464FF0's re-pick button rectangle.</summary>
        internal Rect NoBounds { get; private set; }
        /// <summary>Identifies the original buttonless Imitater dialog.</summary>
        private bool _imitater;

        /// <summary>Creates a chooser dialog using the host's existing texture ownership.</summary>
        /// <param name="texture">The original sprite lookup.</param>
        /// <exception cref="System.IO.IOException">An original bitmap font resource is unavailable.</exception>
        /// <exception cref="FormatException">An original bitmap font description is invalid.</exception>
        internal OriginalChooserDialog(Func<string, SpriteImage?> texture)
        {
            _texture = texture;
        }

        /// <summary>Computes the unchanged PC tiled size only when native dialog content changes.</summary>
        /// <param name="title">The translated original heading.</param>
        /// <param name="message">The translated original warning, or empty for Imitater.</param>
        /// <param name="imitater">Whether PC 498920 expands the buttonless copy dialog toward 500 by 600.</param>
        internal void Configure(string title, string message, bool imitater)
        {
            if (_titleLayout != null && _title == title && _message == message && _imitater == imitater)
            {
                return;
            }
            _title = title;
            _message = message;
            _imitater = imitater;
            _titleLayout = _header.Layout(title);
            var size = Calculate(0, 0);
            if (imitater)
            {
                size = Calculate(500 - (int)size.x, 600 - (int)size.y);
            }
            Bounds = new Rect((800 - (int)size.x) / 2, (600 - (int)size.y) / 2, size.x, size.y);
            var contentWidth = (int)size.x - 36 - 46 + 8;
            var caps = Width("button_left.png") + Width("button_right.png");
            var middle = Width("button_middle.png");
            var buttonWidth = caps + RoundUp((contentWidth - 10) / 2 - middle - caps + 1, middle);
            YesBounds = new Rect(Bounds.x + 31, Bounds.yMax - 36 - Height("button_left.png") + 2, buttonWidth, Height("button_left.png"));
            NoBounds = new Rect(Bounds.x + 31 + contentWidth - buttonWidth, YesBounds.y, buttonWidth, YesBounds.height);
        }

        /// <summary>Samples PC 498C70's eight-column copy-card geometry.</summary>
        /// <param name="seed">The original basic plant.</param>
        /// <returns>The unscaled screen rectangle in the independent Imitater dialog.</returns>
        internal Rect CardRect(PlantKind seed)
        {
            return new Rect(Bounds.x + (int)Bounds.width / 2 - 210 + (int)seed % 8 * 51,
                Bounds.y + 114 + (int)seed / 8 * 71, 50, 70);
        }

        /// <summary>Draws the unchanged tiled frame, header and native wrapped warning text.</summary>
        internal void Draw()
        {
            var prior = CanvasUI.Color;
            CanvasUI.Color = prior * new Color(0f, 0f, 0f, .5f);
            CanvasUI.DrawTexture(new Rect(0, 0, 800, 600), Texture2D.whiteTexture);
            CanvasUI.Color = prior;
            OriginalUiWidgets.DialogFrame(_texture, new Rect(Bounds.x, Bounds.y + 45, Bounds.width, Bounds.height - 45), false, false);
            var cap = _texture("dialog_header.png");
            if (cap != null)
            {
                OriginalUiWidgets.Image(cap, new Rect(Bounds.x + ((int)Bounds.width - cap.Width) / 2 - 5, Bounds.y, cap.Width, cap.Height));
            }
            var color = new Color(224f / 255f, 187f / 255f, 98f / 255f);
            if (_titleLayout != null)
            {
                _header.Draw(_titleLayout, Bounds.x + Mathf.Floor((Bounds.width - _titleLayout.Width) / 2), Bounds.y + 80, color);
            }
            if (_bodyLayout != null && _message.Length > 0)
            {
                var top = 80 + _header.Data.LineHeight + 3;
                var bodyY = top + ((int)Bounds.height - 36 - 24 - top - 55 - (int)_bodyLayout.Height) / 2;
                _body.Draw(_bodyLayout, Bounds.x + 38 + Mathf.Floor((Bounds.width - 86 - _bodyLayout.Width) / 2), Bounds.y + bodyY, color);
            }
        }

        /// <summary>Releases the two dialog-owned original bitmap fonts.</summary>
        public void Dispose()
        {
            _header.Dispose();
            _body.Dispose();
        }

        /// <summary>Ports PC 464B40's header/content insets and tile rounding.</summary>
        /// <param name="extraX">The native extra content width.</param>
        /// <param name="extraY">The native extra content height.</param>
        /// <returns>The original complete dialog dimensions.</returns>
        private Vector2 Calculate(int extraX, int extraY)
        {
            var tile = Width("dialog_topmiddle.png");
            var minimumWidth = tile + Width("dialog_topleft.png") + Width("dialog_topright.png");
            var width = minimumWidth + RoundUp(36 + 46 + extraX + (int)(_titleLayout?.Width ?? 0) - minimumWidth, tile);
            var height = 35 + 36 + 45 + extraY + _header.Data.LineHeight + 3;
            if (_message.Length > 0)
            {
                width += tile;
                _bodyLayout = _body.Layout(_message, maxWidth: width - 86);
                height += (int)_bodyLayout.Height + 30;
            }
            else
            {
                _bodyLayout = null;
            }
            height += 24 + 8;
            var minimumHeight = Height("dialog_topleft.png") + Height("dialog_bottomleft.png") + 45;
            height = minimumHeight + RoundUp(height - minimumHeight, Height("dialog_centerleft.png"));
            return new Vector2(width, height);
        }

        /// <summary>Reads the unchanged width of one original dialog part.</summary>
        /// <param name="name">The original image name.</param>
        /// <returns>The native image width, or zero if unavailable.</returns>
        private int Width(string name)
        {
            return _texture(name)?.Width ?? 0;
        }

        /// <summary>Reads the unchanged height of one original dialog part.</summary>
        /// <param name="name">The original image name.</param>
        /// <returns>The native image height, or zero if unavailable.</returns>
        private int Height(string name)
        {
            return _texture(name)?.Height ?? 0;
        }

        /// <summary>Rounds a positive native excess up to whole background tiles.</summary>
        /// <param name="value">The excess content dimension.</param>
        /// <param name="tile">The original tile dimension.</param>
        /// <returns>The native nonnegative rounded excess.</returns>
        private static int RoundUp(int value, int tile)
        {
            return value <= 0 ? 0 : tile <= 0 ? value : (value + tile - 1) / tile * tile;
        }
    }
}
