#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Records Canvas submissions without depending on a Unity frame or GUI event.</summary>
    internal static class CanvasUI
    {
        /// <summary>Gets or sets the tint inherited by subsequent image submissions.</summary>
        internal static Color Color { get; set; } = new Color(1f, 1f, 1f, 1f);

        /// <summary>Gets the rectangles submitted during the current logical test frame.</summary>
        internal static List<Rect> Drawn { get; } = new List<Rect>();

        /// <summary>Gets the number of unclosed presentation groups.</summary>
        internal static int GroupDepth { get; private set; }

        /// <summary>Records a presentation clipping group.</summary>
        /// <param name="rect">The logical group bounds.</param>
        internal static void BeginGroup(Rect rect)
        {
            GroupDepth++;
        }

        /// <summary>Completes the most recent presentation clipping group.</summary>
        internal static void EndGroup()
        {
            GroupDepth--;
        }

        /// <summary>Records an image submission for anchor and layer-order assertions.</summary>
        /// <param name="rect">The logical destination bounds.</param>
        /// <param name="texture">The loaded image represented by this submission.</param>
        /// <param name="mode">The image scaling policy supplied by the view.</param>
        /// <param name="alpha">Whether transparent source pixels are composited.</param>
        internal static void DrawTexture(Rect rect, Texture2D texture, ScaleMode mode, bool alpha)
        {
            Drawn.Add(rect);
        }
    }
}
