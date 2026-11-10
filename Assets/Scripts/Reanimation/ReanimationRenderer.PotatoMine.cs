using System;
using UnityEngine;
namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        public bool DrawNativePotatoMineLight(Vector2 origin, float scale, float bodySeconds,
            float bodyRate, float glowTime, int frameCount)
        {
            Reanimation asset; Clip body, glow; Track anchor; Matrix4x4 inverseBase; Rect bounds;
            if (!TryPrepareAttachedPlant("PotatoMine", "anim_armed", "anim_glow", "anim_light",
                out asset, out body, out glow, out anchor, out inverseBase, out bounds)) return false;
            // PC explicitly expands the one-frame marker's clip to 3..10 frames and
            // keeps disappearing frames. Sampling the default anim_glow clip would stay lit.
            var light = new Clip { Start = glow.Start, Count = Math.Max(3, Math.Min(10, frameCount)), Bounds = glow.Bounds };
            float bodySample = NativeSeconds(body, bodySeconds, bodyRate, asset.Fps, true);
            var time = SampleFrame(body, bodySample, asset.Fps, false);
            Matrix4x4 attachment = PartMatrix(Interpolate(GetFrame(anchor,time.Current),GetFrame(anchor,time.Next),time.Fraction)) * inverseBase;
            float phase = glowTime - (float)Math.Floor(glowTime);
            Matrix4x4 previous = SpriteMatrix; Color tint = GUI.color;
            try { return DrawClipPose(asset, light, phase * (light.Count - 1) / asset.Fps, asset.Fps, false,
                previous * NativeMatrix(origin, scale) * attachment, tint, null, null,
                track => TrackIs(track,"anim_glow")); }
            finally { SpriteMatrix = previous; GUI.color = tint; }
        }
    }
}
