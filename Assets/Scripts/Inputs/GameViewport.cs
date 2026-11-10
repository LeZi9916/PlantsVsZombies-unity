#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Inputs
{
    /// <summary>Maps the original top-left 800 by 600 interface to a centered screen viewport.</summary>
    public static class GameViewport
    {
        /// <summary>The horizontal extent of the original interface.</summary>
        public const float Width = 800f;
        /// <summary>The vertical extent of the original interface.</summary>
        public const float Height = 600f;
        /// <summary>Gets the current uniform scale from logical coordinates to screen pixels.</summary>
        public static float Scale => Mathf.Min(Screen.width / Width, Screen.height / Height);
        /// <summary>Gets the letterboxed viewport in bottom-left screen pixel coordinates.</summary>
        public static Rect ScreenRect => CalculateRect(Screen.width, Screen.height);

        /// <summary>Calculates the centered viewport for a screen of the specified size.</summary>
        /// <param name="width">The screen width in pixels.</param>
        /// <param name="height">The screen height in pixels.</param>
        /// <returns>The viewport in bottom-left pixel coordinates.</returns>
        public static Rect CalculateRect(float width, float height)
        {
            var scale = System.Math.Max(0d, System.Math.Min(width / (double)Width, height / (double)Height));
            var fittedWidth = Width * scale;
            var fittedHeight = Height * scale;
            return new Rect((float)((width - fittedWidth) * .5d), (float)((height - fittedHeight) * .5d), (float)fittedWidth, (float)fittedHeight);
        }

        /// <summary>Converts Unity's bottom-left screen coordinates to original top-left coordinates.</summary>
        /// <param name="position">The position in screen pixels.</param>
        /// <returns>The position in logical interface coordinates, including positions outside the viewport.</returns>
        public static Vector2 ScreenToLogical(Vector2 position)
        {
            return ScreenToLogical(position, Screen.width, Screen.height);
        }

        /// <summary>Converts a screen position using an explicit screen size.</summary>
        /// <param name="position">The position in bottom-left screen pixels.</param>
        /// <param name="width">The screen width in pixels.</param>
        /// <param name="height">The screen height in pixels.</param>
        /// <returns>The equivalent top-left logical position.</returns>
        public static Vector2 ScreenToLogical(Vector2 position, float width, float height)
        {
            var viewport = CalculateRect(width, height);
            var scale = viewport.width / (double)Width;
            if (scale <= 0f)
            {
                return Vector2.negativeInfinity;
            }

            return new Vector2((float)((position.x - viewport.x) / scale), (float)((viewport.yMax - position.y) / scale));
        }

        /// <summary>Converts original top-left coordinates to Unity screen pixels.</summary>
        /// <param name="position">The original logical position.</param>
        /// <returns>The equivalent bottom-left screen pixel position.</returns>
        public static Vector2 LogicalToScreen(Vector2 position)
        {
            var viewport = ScreenRect;
            return new Vector2(viewport.x + position.x * Scale, viewport.yMax - position.y * Scale);
        }
    }
}
