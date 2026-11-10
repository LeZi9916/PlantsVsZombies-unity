using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        public struct NativeAnimationInfo
        {
            public int Start, Count;
            public float SourceRate, GroundDistance;
            public Rect Bounds;
        }
        public struct HeadPlayback
        {
            public string Animation, Anchor;
            public float Seconds, Rate;
            public bool Loop;
        }

        private readonly Dictionary<string, NativePoseBlend> nativePoseBlends = new Dictionary<string, NativePoseBlend>();
        private NativePoseBlend PrepareNativeBlend(string key, string animation, float seconds, int ticks)
        {
            if (string.IsNullOrEmpty(key)) return null;
            NativePoseBlend blend;
            if (!nativePoseBlends.TryGetValue(key, out blend))
            {
                blend = new NativePoseBlend { Animation = animation, StartedAt = seconds, Fraction = 1f };
                nativePoseBlends.Add(key, blend);
            }
            else if (!string.Equals(blend.Animation, animation, StringComparison.OrdinalIgnoreCase))
            {
                blend.From = new Dictionary<string, Frame>(blend.Current);
                blend.Animation = animation; blend.StartedAt = seconds;
            }
            blend.Fraction = ticks <= 0 || blend.From.Count == 0 ? 1f : Mathf.Clamp01((seconds - blend.StartedAt) * 100f / ticks);
            return blend;
        }
        // Additive overload preserves the existing Func<string,bool> signature used by loader/UI.
        public bool DrawNative(string name, Vector2 origin, float scale, float seconds, string animation, bool loop)
        { return DrawNativePlayback(name, origin, scale, seconds, animation, loop); }

        private static Matrix4x4 CombatPlacement(Vector2 origin, float scale, bool facingLeft, float mirrorWidth = 80f)
        {
            Matrix4x4 placement = NativeMatrix(origin, scale);
            if (facingLeft) { placement.m00 = -scale; placement.m03 += mirrorWidth * scale; }
            return placement;
        }

        public bool TryGetNativeAnimationInfo(string name, string animation, out NativeAnimationInfo info)
        {
            info = default(NativeAnimationInfo);
            Reanimation asset; Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            Track ground = asset.Tracks.Find(t => TrackIs(t.Name, "_ground"));
            info = new NativeAnimationInfo { Start = clip.Start, Count = clip.Count, SourceRate = asset.Fps,
                GroundDistance = ground == null ? 0f : GetFrame(ground, clip.Start + clip.Count - 1).X - GetFrame(ground, clip.Start).X,
                Bounds = clip.Bounds };
            return true;
        }

        // PC reanimation normal Loop advances normalized time by rate / count, and samples
        // start + (count - 1) * time. It does not interpolate the last frame back into the first.
        // Keep portrait APIs unchanged and encode that exact native sample into DrawInternal.
        private static float NativeSeconds(Clip clip, float seconds, float rate, float sourceRate, bool loop)
        {
            double phase = Math.Max(0d, seconds) * Math.Max(0d, rate) / clip.Count;
            phase = loop ? phase - Math.Floor(phase) : Math.Min(1d, phase);
            return (float)(phase * (clip.Count - 1) / sourceRate);
        }

        public bool DrawNativePlayback(string name, Vector2 origin, float scale, float seconds,
            string animation, bool loop, float framesPerSecond = -1f, Func<string, bool> visible = null,
            string overrideTrack = null, string overrideImage = null, bool facingLeft = false, string instanceKey = null, int blendTicks = 20, float verticalScale = -1f, float mirrorWidth = 80f)
        {
            Reanimation asset; Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            float rate = framesPerSecond < 0f ? asset.Fps : framesPerSecond;
            Texture2D image = overrideImage == null ? null : LoadTexture(overrideImage);
            Matrix4x4 placement = CombatPlacement(origin, scale, facingLeft, mirrorWidth);
            if (verticalScale >= 0f) placement.m11 = verticalScale;
            return DrawInternal(name, new Rect(0, 0, 1, 1), NativeSeconds(clip, seconds, rate, asset.Fps, loop),
                animation, false, ZombieVariant.Normal, false, visible, overrideTrack, image, placement, false, PrepareNativeBlend(instanceKey, animation, seconds, blendTicks));
        }

        public bool DrawZombiePlayback(Vector2 origin, float scale, float seconds, ZombieVariant variant,
            string animation, bool loop, float rate, bool equipmentIntact = true, Func<string, bool> visible = null, string instanceKey = null, int blendTicks = 20)
        {
            Reanimation asset; Clip clip;
            if (!TryGetClip("Zombie", animation, out asset, out clip)) return false;
            return DrawInternal("Zombie", new Rect(0, 0, 1, 1), NativeSeconds(clip, seconds, rate, asset.Fps, loop),
                animation, false, variant, equipmentIntact, visible, null, null, NativeMatrix(origin, scale), false, PrepareNativeBlend(instanceKey, animation, seconds, blendTicks));
        }

        public bool DrawNativeAttachedHeads(string name, Vector2 origin, float scale, float bodySeconds,
            float bodyRate, HeadPlayback[] heads, Func<string, bool> visible = null, bool facingLeft = false, string instanceKey = null, int blendTicks = 20)
        {
            Reanimation asset; Clip body;
            if (!TryGetClip(name, "anim_idle", out asset, out body)) return false;
            Matrix4x4 previous = SpriteMatrix;
            Color tint = GUI.color;
            Matrix4x4 layout = previous * CombatPlacement(origin, scale, facingLeft);
            bool drawn = false;
            try
            {
                float bodySample = NativeSeconds(body, bodySeconds, bodyRate, asset.Fps, true);
                drawn = DrawClipPose(asset, body, bodySample, asset.Fps, false, layout, tint, null, null, visible);
                int headIndex = 0;
                foreach (HeadPlayback playback in heads)
                {
                    Clip head; Reanimation ignored; Track anchor; Matrix4x4 inverseBase; Rect bounds;
                    if (!TryPrepareAttachedPlant(name, "anim_idle", playback.Animation, playback.Anchor,
                        out ignored, out body, out head, out anchor, out inverseBase, out bounds)) continue;
                    FrameSample time = SampleFrame(body, bodySample, asset.Fps, false);
                    Matrix4x4 attach = PartMatrix(Interpolate(GetFrame(anchor, time.Current), GetFrame(anchor, time.Next), time.Fraction)) * inverseBase;
                    float sample = NativeSeconds(head, playback.Seconds, playback.Rate, asset.Fps, playback.Loop);
                    drawn |= DrawClipPose(asset, head, sample, asset.Fps, false, layout * attach, tint, null, null, visible,
                        PrepareNativeBlend(instanceKey == null ? null : instanceKey + "-head-" + headIndex++, playback.Animation, playback.Seconds, blendTicks));
                }
            }
            finally { SpriteMatrix = previous; GUI.color = tint; }
            return drawn;
        }

        // AddReanimation defaults to the entire definition, not the first anim_ marker.
        // In particular, Zombie_charred starts with an anim_blink marker: choosing that
        // marker silently truncates the original burn to a blink instead of its full one-shot.
        private bool TryGetNativeDefinitionClip(string name, out Reanimation asset, out Clip clip)
        {
            Clip ignored;
            TryGetClip(name, null, out asset, out ignored);
            clip = null;
            if (asset == null) return false;
            if (asset.Clips.TryGetValue("$definition", out clip)) { LastError = null; return true; }
            clip = new Clip { Start = 0, Count = asset.FrameCount };
            if (!TryCalculateBounds(asset, clip, out clip.Bounds)) return Fail("No drawable original definition: " + name);
            asset.Clips["$definition"] = clip; LastError = null; return true;
        }
        public bool TryGetNativeDefinitionInfo(string name, out NativeAnimationInfo info)
        {
            info = default(NativeAnimationInfo); Reanimation asset; Clip clip;
            if (!TryGetNativeDefinitionClip(name, out asset, out clip)) return false;
            info = new NativeAnimationInfo { Start = 0, Count = clip.Count, SourceRate = asset.Fps, Bounds = clip.Bounds };
            return true;
        }
        public bool DrawNativeDefinition(string name, Vector2 origin, float scale, float seconds, bool loop,
            float rate = -1f, Func<string, bool> visible = null, bool facingLeft = false, float mirrorWidth = 80f)
        {
            Reanimation asset; Clip clip;
            if (!TryGetNativeDefinitionClip(name, out asset, out clip)) return false;
            Matrix4x4 previous = SpriteMatrix; Color tint = GUI.color;
            try
            {
                float sample = NativeSeconds(clip, seconds, rate < 0 ? asset.Fps : rate, asset.Fps, loop);
                return DrawClipPose(asset, clip, sample, asset.Fps, false,
                    previous * CombatPlacement(origin, scale, facingLeft, mirrorWidth), tint, null, null, visible);
            }
            finally { SpriteMatrix = previous; GUI.color = tint; }
        }

        public bool DrawNativePlantHeadAttachment(Vector2 origin, float scale, string parentClip, float parentSeconds,
            float parentRate, int nativeKind, float headSeconds, bool facingLeft)
        {
            int childId; string anchor, childClip; float x, y, sx, sy;
            switch (nativeKind)
            {
                // PC 0x53F110/0x53F436/0x53F55D/0x53F692/0x53F7D9/0x53F857.
                case 26: childId = 4; anchor = "anim_head1"; childClip = "anim_head_idle"; x = 65; y = -5; sx = -1; sy = 1; break;
                case 27: childId = 5; anchor = "Zombie_body"; childClip = "anim_idle"; x = 50; y = 0; sx = -.8f; sy = .8f; break;
                case 28: childId = 33; anchor = "Zombie_body"; childClip = "anim_idle"; x = 55; y = -5; sx = -1; sy = 1; break;
                case 29: childId = 78; anchor = "anim_head1"; childClip = "anim_head_idle"; x = 65; y = -5; sx = -1; sy = 1; break;
                case 30: childId = 11; anchor = "anim_head1"; childClip = "anim_idle"; x = 55; y = -15; sx = -.75f; sy = .75f; break;
                case 31: childId = 16; anchor = "Zombie_body"; childClip = "anim_idle"; x = 37; y = 0; sx = -.8f; sy = .8f; break;
                default: return false;
            }
            PvZReplica.VerifiedData.ReanimationDefinition definition;
            if (!PvZReplica.VerifiedData.VerifiedDataCatalog.TryGetReanimation(childId, out definition)) return false;
            string childName = Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'));
            Reanimation parent, child; Clip body, head;
            if (!TryGetClip("Zombie", parentClip, out parent, out body) || !TryGetClip(childName, childClip, out child, out head)) return false;
            Track track = parent.Tracks.Find(t => TrackIs(t.Name, anchor));
            Matrix4x4 inverse;
            if (track == null || !TryInverseAffine(PartMatrix(GetFrame(track, 0)), out inverse)) return false;
            FrameSample sample = SampleFrame(body, NativeSeconds(body, parentSeconds, parentRate, parent.Fps, true), parent.Fps, false);
            Frame pose = Interpolate(GetFrame(track, sample.Current), GetFrame(track, sample.Next), sample.Fraction);
            const double angle = .2;
            Matrix4x4 offset = Matrix4x4.identity;
            offset.m00 = (float)Math.Cos(angle) * sx; offset.m10 = (float)Math.Sin(angle) * sx;
            offset.m01 = -(float)Math.Sin(angle) * sy; offset.m11 = (float)Math.Cos(angle) * sy;
            offset.m03 = x; offset.m13 = y;
            Matrix4x4 previous = SpriteMatrix; Color color = GUI.color;
            try
            {
                Matrix4x4 placement = previous * CombatPlacement(origin, scale, facingLeft, 60) * PartMatrix(pose) * inverse * offset;
                return DrawClipPose(child, head, NativeSeconds(head, headSeconds, 15f, child.Fps, true), child.Fps, false, placement, color);
            }
            finally { SpriteMatrix = previous; GUI.color = color; }
        }

        public bool TryGetNativeTrackPosition(string name, string animation, string trackName, float seconds,
            float rate, bool loop, out Vector2 position)
        {
            position = default(Vector2); Reanimation asset; Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            Track track = asset.Tracks.Find(t => TrackIs(t.Name, trackName));
            if (track == null) return false;
            FrameSample time = SampleFrame(clip, NativeSeconds(clip, seconds, rate, asset.Fps, loop), asset.Fps, false);
            Frame pose = Interpolate(GetFrame(track, time.Current), GetFrame(track, time.Next), time.Fraction);
            position = new Vector2(pose.X, pose.Y); return true;
        }

        public bool DrawResourceSprite(string resource, Vector2 center, float scale, float degrees = 0f,
            int frame = 0, int columns = 0, int rows = 0, float stretch = 1f, bool additive = false)
        {
            // Original zero-area sprites have no visible pixels. IMGUI rejects their singular matrix.
            if (scale == 0f || stretch == 0f) return true;
            if (nativeResourceImageOverride != null) resource = nativeResourceImageOverride;
            EnsureIndex(); EnsureNativeAtlas(); Texture2D texture;
            Vector2Int atlas; if (!nativeAtlas.TryGetValue(resource, out atlas)) atlas = new Vector2Int(1, 1);
            if (columns <= 0) columns = atlas.x; if (rows <= 0) rows = atlas.y;
            try { texture = LoadTexture(resource); }
            catch (FileNotFoundException) { return false; }
            if (texture == null || columns <= 0 || rows <= 0) return false;
            float width = (float)texture.width / columns, height = (float)texture.height / rows;
            Matrix4x4 previous = SpriteMatrix;
            bool previousAdditive = nativeSpriteAdditive;
            nativeSpriteAdditive = additive;
            double angle = degrees * Math.PI / 180d;
            Matrix4x4 transform = Matrix4x4.identity;
            transform.m00 = (float)Math.Cos(angle) * scale; transform.m01 = -(float)Math.Sin(angle) * scale * stretch;
            transform.m10 = (float)Math.Sin(angle) * scale; transform.m11 = (float)Math.Cos(angle) * scale * stretch;
            transform.m03 = center.x - transform.m00 * width / 2 - transform.m01 * height / 2;
            transform.m13 = center.y - transform.m10 * width / 2 - transform.m11 * height / 2;
            try
            {
                Matrix4x4 sprite = previous * transform;
                if (!HasDrawableArea(sprite)) return true;
                SpriteMatrix = sprite;
                DrawTexturePart(texture, width, height, new Rect((frame % columns) / (float)columns,
                    1f - (frame / columns + 1f) / rows, 1f / columns, 1f / rows));
            }
            finally { SpriteMatrix = previous; nativeSpriteAdditive = previousAdditive; }
            return true;
        }

        private readonly Dictionary<string, Vector2Int> nativeAtlas = new Dictionary<string, Vector2Int>(StringComparer.OrdinalIgnoreCase);
        private bool nativeAtlasLoaded;
        private void EnsureNativeAtlas()
        {
            if (nativeAtlasLoaded) return;
            nativeAtlasLoaded = true;
            string path = Path.Combine(root, "properties", "Resources.xml");
            if (!File.Exists(path)) return;
            XmlDocument document = new XmlDocument { XmlResolver = null };
            document.Load(path);
            foreach (XmlNode group in document.SelectNodes("/ResourceManifest/Resources"))
            {
                string prefix = "IMAGE_";
                foreach (XmlNode node in group.ChildNodes)
                {
                    if (node.Name == "SetDefaults") { if (node.Attributes["idprefix"] != null) prefix = node.Attributes["idprefix"].Value; continue; }
                    if (node.Name != "Image" || node.Attributes["id"] == null) continue;
                    int columns, rows;
                    if (!int.TryParse(node.Attributes["cols"] == null ? "1" : node.Attributes["cols"].Value, out columns)) columns = 1;
                    if (!int.TryParse(node.Attributes["rows"] == null ? "1" : node.Attributes["rows"].Value, out rows)) rows = 1;
                    string id = prefix + node.Attributes["id"].Value;
                    nativeAtlas[id] = new Vector2Int(columns, rows);
                    if (node.Attributes["path"] == null) continue;
                    string fileKey = Path.GetFileName(node.Attributes["path"].Value.Replace('\\', '/'));
                    string imagePath;
                    var paths = id.StartsWith("IMAGE_REANIM_", StringComparison.OrdinalIgnoreCase) ? reanimImagePaths : imagePaths;
                    if (paths.TryGetValue(fileKey, out imagePath))
                        paths[id.Substring(id.StartsWith("IMAGE_REANIM_", StringComparison.OrdinalIgnoreCase) ? 13 : 6)] = imagePath;
                }
            }
        }

        public void ResetNativePlayback()
        {
            nativePoseBlends.Clear();foreach(string key in new List<string>(nativeSurfaces.Keys))ReleaseNativeSurface(key);
        }

        public void ReleaseNativeSurface(string key)
        {
            NativeSurface surface;
            if (!nativeSurfaces.TryGetValue(key, out surface)) return;
            surface.Texture.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(surface.Texture);
            else UnityEngine.Object.DestroyImmediate(surface.Texture);
            nativeSurfaces.Remove(key);
        }
    }
}
