#nullable enable

using System;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Supplies inert drawing operations for the dialog's existing logical GUI fixture.</summary>
    /// <remarks>These unit tests exercise dialog state and ownership; actual drawing is verified in Unity.</remarks>
    internal static class OriginalUiWidgets
    {
        /// <summary>Gets the fallback text color used by the logical fixture.</summary>
        internal static Color StoneText => Color.white;

        /// <summary>Leaves frame rendering to the Unity test path.</summary>
        /// <param name="texture">The source texture lookup.</param>
        /// <param name="rect">The dialog bounds.</param>
        /// <param name="tallBottom">Whether the lower frame is tall.</param>
        /// <param name="header">Whether the frame has a title cap.</param>
        internal static void DialogFrame(Func<string, Texture2D> texture, Rect rect, bool tallBottom, bool header)
        {
        }

        /// <summary>Leaves edit box image rendering to the actual Unity tests.</summary>
        /// <param name="texture">The source texture lookup.</param>
        /// <param name="rect">The edit box bounds.</param>
        internal static void EditBox(Func<string, Texture2D> texture, Rect rect)
        {
        }

        /// <summary>Returns the deterministic non-hover state of this input-free fixture.</summary>
        /// <param name="rect">The button bounds.</param>
        /// <returns>False because no native pointer is sampled.</returns>
        internal static bool Hovered(Rect rect)
        {
            return false;
        }

        /// <summary>Returns the deterministic released state of this input-free fixture.</summary>
        /// <param name="rect">The button bounds.</param>
        /// <returns>False because pointer actions are invoked by the dialog tests.</returns>
        internal static bool Pressed(Rect rect)
        {
            return false;
        }

        /// <summary>Leaves image button rendering to Unity.</summary>
        /// <param name="texture">The source texture lookup.</param>
        /// <param name="rect">The button bounds.</param>
        /// <param name="hovered">The pointer hover state.</param>
        /// <param name="pressed">The pointer press state.</param>
        /// <param name="enabled">Whether the button is enabled.</param>
        internal static void StoneButton(Func<string, Texture2D> texture, Rect rect, bool hovered, bool pressed, bool enabled)
        {
        }

        /// <summary>Selects the existing fallback label path for observable fixture labels.</summary>
        /// <param name="rect">The label bounds.</param>
        /// <param name="text">The label content.</param>
        /// <param name="hovered">The hover state.</param>
        /// <param name="pressed">The press state.</param>
        /// <param name="enabled">Whether the label is enabled.</param>
        /// <returns>False so the logical GUI records the fallback label.</returns>
        internal static bool StoneLabel(Rect rect, string text, bool hovered, bool pressed, bool enabled)
        {
            return false;
        }

        /// <summary>Preserves the fixture's GUI style without native textures.</summary>
        /// <param name="style">The style under test.</param>
        /// <returns>The supplied style.</returns>
        internal static GUIStyle TransparentTextField(GUIStyle style)
        {
            return style;
        }
    }
}
