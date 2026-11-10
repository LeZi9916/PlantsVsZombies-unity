using System;
using UnityEngine;

namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        // PC loop types 1/4 sample count*time, holding the complete last frame.
        // Normal DrawNativePlayback instead samples (count-1)*time; keep both APIs explicit.
        public bool DrawNativeFullLastFrame(string name, Vector2 origin, float horizontalScale,
            float seconds, string animation, bool loop, float rate)
        {
            Reanimation asset; Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            double frame = Math.Max(0d, seconds) * Math.Max(0d, rate);
            if (loop) frame %= clip.Count;
            float sample = (float)(Math.Min(clip.Count - 1, frame) / asset.Fps);
            Matrix4x4 previous = SpriteMatrix; Color tint = GUI.color;
            Matrix4x4 placement = NativeMatrix(origin, 1f); placement.m00 = horizontalScale;
            try { return DrawClipPose(asset, clip, sample, asset.Fps, false,
                previous * placement, tint, null, null, null); }
            finally { SpriteMatrix = previous; GUI.color = tint; }
        }
    }
}
