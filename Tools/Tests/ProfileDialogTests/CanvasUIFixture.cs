#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Supplies deterministic Canvas text and focus state for dialog logic tests.</summary>
    internal static class CanvasUI
    {
        /// <summary>Gets or sets the inherited image tint.</summary>
        internal static Color Color { get; set; } = Color.white;

        /// <summary>Gets or sets the inherited text tint.</summary>
        internal static Color ContentColor { get; set; } = Color.white;

        /// <summary>Gets or sets whether submitted controls accept editing.</summary>
        internal static bool Enabled { get; set; } = true;

        /// <summary>Gets or sets the next text value supplied by the logical input fixture.</summary>
        internal static string? NextText { get; set; }

        /// <summary>Gets the name of the control that last requested focus.</summary>
        internal static string? Focused { get; private set; }

        /// <summary>Gets the labels emitted by the dialog for visible-state assertions.</summary>
        internal static List<string> Labels { get; } = new List<string>();

        /// <summary>Leaves image pixels to the real Unity Canvas regression tests.</summary>
        /// <param name="rect">The logical destination bounds.</param>
        /// <param name="image">The loaded dialog image.</param>
        /// <param name="mode">The requested image scaling policy.</param>
        /// <param name="alpha">Whether source transparency is retained.</param>
        internal static void DrawTexture(Rect rect, Texture2D image, ScaleMode mode, bool alpha)
        {
        }

        /// <summary>Records visible fallback text without creating Unity components.</summary>
        /// <param name="rect">The logical text bounds.</param>
        /// <param name="text">The dialog text to record.</param>
        /// <param name="style">The passive text layout supplied by the view.</param>
        internal static void Label(Rect rect, string text, GUIStyle style)
        {
            Labels.Add(text);
        }

        /// <summary>Accepts the stable control identity used by the dialog.</summary>
        /// <param name="name">The identity assigned to the next text field.</param>
        internal static void SetNextControlName(string name)
        {
        }

        /// <summary>Records the text field receiving focus.</summary>
        /// <param name="name">The identity of the control to focus.</param>
        internal static void FocusControl(string name)
        {
            Focused = name;
        }

        /// <summary>Applies one deterministic input value to the submitted text field.</summary>
        /// <param name="rect">The logical control bounds.</param>
        /// <param name="value">The dialog's current editing value.</param>
        /// <param name="style">The passive text layout supplied by the view.</param>
        /// <returns>The supplied input value, or the unchanged current value.</returns>
        internal static string TextField(Rect rect, string value, GUIStyle style)
        {
            var next = NextText;
            NextText = null;
            return next ?? value;
        }
    }
}
