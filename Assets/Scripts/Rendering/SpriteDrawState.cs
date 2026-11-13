#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Shares explicit pixel transforms, tint, and painter order between scene sprites and Canvas graphics.</summary>
    public static class SpriteDrawState
    {
        /// <summary>The next order assigned to a visible scene or interface element.</summary>
        private static int s_order;

        /// <summary>Gets the complete presentation identity used to defer reuse of textures still submitted this frame.</summary>
        internal static int FrameVersion { get; private set; }

        /// <summary>Gets or sets the source-to-screen pixel transform used by the current presenter.</summary>
        public static Matrix4x4 Matrix { get; set; } = Matrix4x4.identity;

        /// <summary>Gets or sets the inherited tint and opacity used by the current presenter.</summary>
        public static Color Color { get; set; } = Color.white;

        /// <summary>Gets or sets the accepted top-left screen pixel rectangle shared by scene sprites and Canvas groups.</summary>
        public static Rect Clip { get; set; } = new Rect(0f, 0f, Screen.width, Screen.height);

        /// <summary>Resets painter order and inherited drawing state before presenting a complete frame.</summary>
        public static void BeginFrame()
        {
            SpriteRenderingProfiler.BeginFrame();
            FrameVersion++;
            SpriteAssetProvider.OnSceneFrameStarted();
            s_order = 0;
            Matrix = Matrix4x4.identity;
            Color = Color.white;
            Clip = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        /// <summary>Assigns the next shared painter order to a renderer or interface graphic.</summary>
        /// <returns>The sorting order of the next visible element.</returns>
        /// <exception cref="InvalidOperationException">The frame exceeds Unity's signed sorting-order range.</exception>
        public static int NextOrder()
        {
            if (s_order >= short.MaxValue)
            {
                throw new InvalidOperationException("The PvZ presentation exceeds Unity's sorting-order range.");
            }

            return s_order++;
        }
    }
}
