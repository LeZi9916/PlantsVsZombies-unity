using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace PvZReplica.Reanimation
{
    public enum ZombieVariant { Normal, Conehead, Buckethead, Flag, ScreenDoor }

    /// <summary>Draws original PopCap reanim files from an OnGUI callback.</summary>
    public sealed class ReanimationRenderer : IDisposable
    {
        public struct AnimationInfo
        {
            public float FramesPerSecond { get; internal set; }
            public int FrameCount { get; internal set; }
            public Rect SourceBounds { get; internal set; }
            public float DurationSeconds { get { return FrameCount / FramesPerSecond; } }
        }

        private struct Frame
        {
            public float X, Y, Kx, Ky, ScaleX, ScaleY, Alpha, Visibility;
            public Texture2D Texture;
        }
        private struct FrameSample
        {
            public int Current, Next;
            public float Fraction;
        }
        private sealed class Track
        {
            public string Name;
            public Frame[] Frames;
        }
        private sealed class Clip
        {
            public int Start;
            public int Count;
            public Rect Bounds;
            public readonly Dictionary<int, Rect> VariantBounds = new Dictionary<int, Rect>();
        }
        private sealed class Reanimation
        {
            public string Name;
            public float Fps;
            public int FrameCount;
            public readonly List<Track> Tracks = new List<Track>();
            public readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, Rect> AttachedBounds = new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase);
        }

        private readonly string root;
        private readonly Dictionary<string, string> animationPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> reanimImagePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> imagePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Reanimation> animations = new Dictionary<string, Reanimation>(StringComparer.OrdinalIgnoreCase);
        private bool indexed;
        private bool disposed;

        /// <summary>The reason the most recent Draw or TryGetAnimationInfo call failed.</summary>
        public string LastError { get; private set; }

        public ReanimationRenderer(string resourceRoot)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            root = Path.GetFullPath(resourceRoot);
        }

        /// <summary>Draws a looping animation inside a screen-space rectangle, preserving aspect ratio.</summary>
        public bool Draw(string name, Rect destination, float seconds, string animation = null)
        {
            return Draw(name, destination, seconds, animation, true);
        }

        /// <summary>Set loop to false to hold the last frame of a one-shot animation.</summary>
        public bool Draw(string name, Rect destination, float seconds, string animation, bool loop)
        {
            if (IsPeaShooter(name))
            {
                if (string.IsNullOrWhiteSpace(animation) || IsAnimation(animation, "anim_full_idle"))
                    return DrawAttachedPlant(name, destination, seconds, seconds, "anim_head_idle", loop);
                if (IsAnimation(animation, "anim_shooting"))
                    return DrawAttachedPlant(name, destination, seconds, seconds, "anim_shooting", loop);
            }
            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, null);
        }

        /// <summary>
        /// Draws with a custom track-name filter. The filter replaces the default Zombie rules;
        /// it must remain consistent during a call and is also applied to the bounding box.
        /// </summary>
        public bool Draw(string name, Rect destination, float seconds, string animation, bool loop,
            Func<string, bool> trackVisible)
        {
            if (trackVisible == null) return Draw(name, destination, seconds, animation, loop);
            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, trackVisible);
        }

        /// <summary>
        /// Overrides one image track for this draw only. The replacement is stretched to the
        /// original track image rectangle, so the animation and cached layout remain unchanged.
        /// Pass an image ID such as IMAGE_REANIM_WALLNUT_CRACKED1, or null for the original image.
        /// </summary>
        public bool DrawWithImageOverride(string name, Rect destination, float seconds, string trackName,
            string imageId, string animation = null, bool loop = true)
        {
            LastError = null;
            if (imageId == null) return Draw(name, destination, seconds, animation, loop);
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(imageId))
                return Fail("A track name and image resource ID are required.");
            Reanimation asset;
            Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            Track track = asset.Tracks.Find(item => TrackIs(item.Name, trackName));
            if (track == null || !Array.Exists(track.Frames, frame => frame.Texture != null))
                return Fail("Image track not found: " + trackName + " in " + name);
            Texture2D replacement;
            try { replacement = LoadTexture(imageId); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is FormatException || error is ArgumentException ||
                                          error is InvalidOperationException)
            {
                return Fail("Could not load image " + imageId + ": " + error.Message);
            }
            if (IsPeaShooter(name) && (string.IsNullOrWhiteSpace(animation) ||
                IsAnimation(animation, "anim_full_idle") || IsAnimation(animation, "anim_shooting")))
            {
                string headName = IsAnimation(animation, "anim_shooting") ? "anim_shooting" : "anim_head_idle";
                return DrawAttachedPlantCore(name, destination, seconds, seconds, headName, loop, 0f, 0f,
                    "anim_idle", "anim_stem", trackName, replacement);
            }
            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, null,
                trackName, replacement);
        }

        /// <summary>
        /// Draws a ground zombie. equipmentIntact controls the cone, bucket, door, or flag;
        /// false restores the ordinary unarmored body and arms.
        /// </summary>
        public bool DrawZombie(Rect destination, float seconds, ZombieVariant variant,
            bool equipmentIntact = true, string animation = null, bool loop = true)
        {
            if (!Enum.IsDefined(typeof(ZombieVariant), variant))
                return Fail("Unknown zombie variant: " + variant);
            return DrawInternal("Zombie", destination, seconds, animation, loop, variant, equipmentIntact, null);
        }

        /// <summary>
        /// Draws an independently animated head attached to a looping plant body. Zero frame rates
        /// use the source fps; provide separate times to keep body motion continuous while shooting.
        /// </summary>
        public bool DrawAttachedPlant(string name, Rect destination, float bodySeconds, float headSeconds,
            string headAnimation = "anim_shooting", bool headLoop = false,
            float bodyFramesPerSecond = 0f, float headFramesPerSecond = 0f,
            string bodyAnimation = "anim_idle", string attachmentTrack = "anim_stem")
        {
            return DrawAttachedPlantCore(name, destination, bodySeconds, headSeconds, headAnimation, headLoop,
                bodyFramesPerSecond, headFramesPerSecond, bodyAnimation, attachmentTrack, null, null);
        }

        private bool DrawAttachedPlantCore(string name, Rect destination, float bodySeconds, float headSeconds,
            string headAnimation, bool headLoop, float bodyFramesPerSecond, float headFramesPerSecond,
            string bodyAnimation, string attachmentTrack, string overrideTrack, Texture2D imageOverride)
        {
            LastError = null;
            if (!IsValidDestination(destination) || !IsFinite(bodySeconds) || !IsFinite(headSeconds) ||
                !IsFinite(bodyFramesPerSecond) || !IsFinite(headFramesPerSecond) ||
                bodyFramesPerSecond < 0f || headFramesPerSecond < 0f)
                return Fail("The rectangle, times, and frame rates must be finite and nonnegative.");
            if (string.IsNullOrWhiteSpace(headAnimation) || string.IsNullOrWhiteSpace(bodyAnimation) ||
                string.IsNullOrWhiteSpace(attachmentTrack))
                return Fail("Body animation, head animation and attachment track are required.");

            Reanimation asset;
            Clip body, head;
            Track anchor;
            Matrix4x4 inverseBase;
            Rect bounds;
            if (!TryPrepareAttachedPlant(name, bodyAnimation, headAnimation, attachmentTrack,
                out asset, out body, out head, out anchor, out inverseBase, out bounds)) return false;

            float bodyFps = bodyFramesPerSecond == 0f ? asset.Fps : bodyFramesPerSecond;
            float headFps = headFramesPerSecond == 0f ? asset.Fps : headFramesPerSecond;
            FrameSample bodyTime = SampleFrame(body, bodySeconds, bodyFps, true);
            Frame anchorPose = Interpolate(GetFrame(anchor, bodyTime.Current), GetFrame(anchor, bodyTime.Next), bodyTime.Fraction);
            Matrix4x4 attachment = PartMatrix(anchorPose) * inverseBase;
            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;
            Matrix4x4 layout = LayoutMatrix(destination, bounds);
            bool drawn;
            try
            {
                drawn = DrawClipPose(asset, body, bodySeconds, bodyFps, true, originalMatrix * layout, originalColor,
                    overrideTrack, imageOverride);
                drawn |= DrawClipPose(asset, head, headSeconds, headFps, headLoop,
                    originalMatrix * layout * attachment, originalColor, overrideTrack, imageOverride);
            }
            finally
            {
                GUI.matrix = originalMatrix;
                GUI.color = originalColor;
            }
            if (!drawn) return Fail("No visible body or head image tracks at this animation time.");
            return true;
        }

        private bool DrawInternal(string name, Rect destination, float seconds, string animation, bool loop,
            ZombieVariant variant, bool equipmentIntact, Func<string, bool> trackVisible,
            string overrideTrack = null, Texture2D imageOverride = null)
        {
            LastError = null;
            if (!IsFinite(seconds) || !IsFinite(destination.x) || !IsFinite(destination.y) ||
                !IsFinite(destination.width) || !IsFinite(destination.height) ||
                destination.width <= 0f || destination.height <= 0f)
                return Fail("The destination and time must be finite, with a positive rectangle size.");

            Reanimation asset;
            Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            Reanimation flag = null;
            Clip flagClip = null;
            if (trackVisible == null && IsZombie(asset) && variant == ZombieVariant.Flag && equipmentIntact &&
                !TryGetClip("Zombie_flagpole", null, out flag, out flagClip)) return false;
            Rect bounds;
            if (!TryGetBounds(asset, clip, variant, equipmentIntact, trackVisible, out bounds)) return false;
            double timeInFrames = Math.Max(0d, (double)seconds) * asset.Fps;
            if (loop) timeInFrames %= clip.Count;
            else timeInFrames = Math.Min(timeInFrames, clip.Count - 1d);
            int relativeFrame = (int)Math.Floor(timeInFrames);
            float fraction = (float)(timeInFrames - relativeFrame);
            int current = clip.Start + relativeFrame;
            int next = relativeFrame + 1 < clip.Count ? current + 1 : (loop ? clip.Start : current);

            float scale = Math.Min(destination.width / bounds.width, destination.height / bounds.height);
            Matrix4x4 layout = Matrix4x4.identity;
            layout.m00 = scale;
            layout.m11 = scale;
            layout.m03 = destination.x + (destination.width - bounds.width * scale) * 0.5f - bounds.x * scale;
            layout.m13 = destination.y + (destination.height - bounds.height * scale) * 0.5f - bounds.y * scale;

            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;
            bool drawn = false;
            try
            {
                // The exporter writes back-to-front layers; preserve that order.
                foreach (Track track in asset.Tracks)
                {
                    if (!IsTrackVisible(asset, track.Name, variant, equipmentIntact, trackVisible)) continue;
                    Frame from = GetFrame(track, current);
                    if (from.Visibility < 0f || from.Texture == null) continue;
                    Frame frame = Interpolate(from, GetFrame(track, next), fraction);
                    if (frame.Alpha <= 0f) continue;
                    if (flag != null && TrackIs(track.Name, "Zombie_flaghand"))
                        drawn |= DrawAttachment(flag, flagClip, seconds, originalMatrix * layout * PartMatrix(frame),
                            originalColor, frame.Alpha);
                    GUI.matrix = originalMatrix * layout * PartMatrix(frame);
                    GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b,
                        originalColor.a * Math.Max(0f, Math.Min(1f, frame.Alpha)));
                    Texture2D image = imageOverride != null && TrackIs(track.Name, overrideTrack) ? imageOverride : from.Texture;
                    GUI.DrawTexture(new Rect(0f, 0f, from.Texture.width, from.Texture.height), image, ScaleMode.StretchToFill, true);
                    drawn = true;
                }
            }
            finally
            {
                GUI.matrix = originalMatrix;
                GUI.color = originalColor;
            }
            if (!drawn) return Fail("No visible image tracks at this animation time.");
            return true;
        }

        /// <summary>Returns the padded union of all frames in the selected animation.</summary>
        public bool TryGetAnimationInfo(string name, string animation, out AnimationInfo info)
        {
            LastError = null;
            info = default(AnimationInfo);
            if (IsPeaShooter(name) && (string.IsNullOrWhiteSpace(animation) ||
                IsAnimation(animation, "anim_full_idle") || IsAnimation(animation, "anim_shooting")))
            {
                string headName = IsAnimation(animation, "anim_shooting") ? "anim_shooting" : "anim_head_idle";
                Reanimation attachedAsset;
                Clip body, head;
                Track anchor;
                Matrix4x4 inverseBase;
                Rect attachedBounds;
                if (!TryPrepareAttachedPlant(name, "anim_idle", headName, "anim_stem", out attachedAsset,
                    out body, out head, out anchor, out inverseBase, out attachedBounds)) return false;
                info = new AnimationInfo { FramesPerSecond = attachedAsset.Fps, FrameCount = head.Count, SourceBounds = attachedBounds };
                return true;
            }
            Reanimation asset;
            Clip clip;
            if (!TryGetClip(name, animation, out asset, out clip)) return false;
            Rect bounds;
            if (!TryGetBounds(asset, clip, ZombieVariant.Normal, false, null, out bounds)) return false;
            info = new AnimationInfo { FramesPerSecond = asset.Fps, FrameCount = clip.Count, SourceBounds = bounds };
            return true;
        }

        /// <summary>Applies the ground-zombie track rules without consulting animation frame visibility.</summary>
        public static bool IsZombieTrackVisible(string trackName, ZombieVariant variant, bool equipmentIntact = true)
        {
            if (string.IsNullOrEmpty(trackName)) return false;
            bool cone = equipmentIntact && variant == ZombieVariant.Conehead;
            bool bucket = equipmentIntact && variant == ZombieVariant.Buckethead;
            bool door = equipmentIntact && variant == ZombieVariant.ScreenDoor;
            bool flag = equipmentIntact && variant == ZombieVariant.Flag;

            if (TrackIs(trackName, "anim_cone")) return cone;
            if (TrackIs(trackName, "anim_bucket")) return bucket;
            if (TrackIs(trackName, "anim_hair")) return !cone && !bucket;
            if (TrackIs(trackName, "Zombie_flaghand")) return flag;
            if (TrackIs(trackName, "Zombie_innerarm_screendoor")) return door || flag;
            if (TrackIs(trackName, "anim_screendoor") || TrackIs(trackName, "Zombie_outerarm_screendoor") ||
                TrackIs(trackName, "Zombie_innerarm_screendoor_hand")) return door;
            if (trackName.StartsWith("anim_innerarm", StringComparison.OrdinalIgnoreCase)) return !door && !flag;
            if (TrackIs(trackName, "Zombie_outerarm_hand") || TrackIs(trackName, "Zombie_outerarm_lower") ||
                TrackIs(trackName, "Zombie_outerarm_upper")) return !door;
            if (TrackIs(trackName, "Zombie_duckytube") || TrackIs(trackName, "Zombie_whitewater") ||
                TrackIs(trackName, "Zombie_whitewater2") || TrackIs(trackName, "Zombie_mustache") ||
                TrackIs(trackName, "anim_tongue")) return false;
            return true;
        }

        private static bool IsZombie(Reanimation asset)
        {
            return string.Equals(asset.Name, "Zombie", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrackIs(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrackVisible(Reanimation asset, string track, ZombieVariant variant,
            bool equipmentIntact, Func<string, bool> custom)
        {
            if (custom != null) return custom(track);
            return !IsZombie(asset) || IsZombieTrackVisible(track, variant, equipmentIntact);
        }

        private bool TryGetBounds(Reanimation asset, Clip clip, ZombieVariant variant, bool equipmentIntact,
            Func<string, bool> custom, out Rect bounds)
        {
            bounds = default(Rect);
            if (custom == null && !IsZombie(asset)) { bounds = clip.Bounds; return true; }
            // A flag is an attachment, not part of the body-size reference rectangle.
            if (custom == null && variant == ZombieVariant.Flag)
                return TryGetBounds(asset, clip, ZombieVariant.Normal, false, null, out bounds);
            int key = (int)variant * 2 + (equipmentIntact ? 1 : 0);
            if (custom == null && clip.VariantBounds.TryGetValue(key, out bounds)) return true;
            if (custom == null && (variant == ZombieVariant.Conehead || variant == ZombieVariant.Buckethead ||
                                   variant == ZombieVariant.ScreenDoor))
            {
                Rect intact, broken;
                if (!TryCalculateBounds(asset, clip, out intact, true, variant, true) ||
                    !TryCalculateBounds(asset, clip, out broken, true, variant, false))
                    return Fail("The selected tracks have no drawable images: " + asset.Name);
                float minX = Math.Min(intact.x, broken.x), minY = Math.Min(intact.y, broken.y);
                float maxX = Math.Max(intact.x + intact.width, broken.x + broken.width);
                float maxY = Math.Max(intact.y + intact.height, broken.y + broken.height);
                bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
                clip.VariantBounds[(int)variant * 2] = bounds;
                clip.VariantBounds[(int)variant * 2 + 1] = bounds;
                return true;
            }
            if (!TryCalculateBounds(asset, clip, out bounds, true, variant, equipmentIntact, custom))
                return Fail("The selected tracks have no drawable images: " + asset.Name);
            if (custom == null) clip.VariantBounds[key] = bounds;
            return true;
        }

        private static bool DrawAttachment(Reanimation asset, Clip clip, float seconds, Matrix4x4 parent,
            Color originalColor, float parentAlpha)
        {
            double timeInFrames = Math.Max(0d, (double)seconds) * asset.Fps % clip.Count;
            int relativeFrame = (int)Math.Floor(timeInFrames);
            int current = clip.Start + relativeFrame;
            int next = clip.Start + ((relativeFrame + 1) % clip.Count);
            float fraction = (float)(timeInFrames - relativeFrame);
            bool drawn = false;
            foreach (Track track in asset.Tracks)
            {
                Frame from = GetFrame(track, current);
                if (from.Visibility < 0f || from.Texture == null) continue;
                Frame frame = Interpolate(from, GetFrame(track, next), fraction);
                if (frame.Alpha <= 0f) continue;
                GUI.matrix = parent * PartMatrix(frame);
                GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b,
                    originalColor.a * Math.Max(0f, Math.Min(1f, parentAlpha * frame.Alpha)));
                GUI.DrawTexture(new Rect(0f, 0f, from.Texture.width, from.Texture.height),
                    from.Texture, ScaleMode.StretchToFill, true);
                drawn = true;
            }
            return drawn;
        }

        private bool TryPrepareAttachedPlant(string name, string bodyAnimation, string headAnimation, string attachmentTrack,
            out Reanimation asset, out Clip body, out Clip head, out Track anchor,
            out Matrix4x4 inverseBase, out Rect bounds)
        {
            asset = null; body = null; head = null; anchor = null;
            inverseBase = Matrix4x4.identity; bounds = default(Rect);
            if (!TryGetClip(name, bodyAnimation, out asset, out body)) return false;
            Reanimation sameAsset;
            if (!TryGetClip(name, headAnimation, out sameAsset, out head)) return false;
            anchor = asset.Tracks.Find(track => TrackIs(track.Name, attachmentTrack));
            if (anchor == null && TrackIs(attachmentTrack, "anim_stem"))
                anchor = asset.Tracks.Find(track => TrackIs(track.Name, "anim_idle"));
            if (anchor == null) return Fail("Attachment track not found: " + attachmentTrack + " in " + name);
            if (!TryInverseAffine(PartMatrix(GetFrame(anchor, body.Start)), out inverseBase))
                return Fail("The attachment base pose is singular: " + anchor.Name);
            return TryGetAttachedPlantBounds(asset, body, head, anchor, inverseBase, out bounds);
        }

        private bool TryGetAttachedPlantBounds(Reanimation asset, Clip body, Clip head, Track anchor,
            Matrix4x4 inverseBase, out Rect bounds)
        {
            bounds = default(Rect);
            Clip idle = null, shooting = null;
            Reanimation ignored;
            if (asset.Tracks.Exists(track => TrackIs(track.Name, "anim_head_idle")) &&
                !TryGetClip(asset.Name, "anim_head_idle", out ignored, out idle)) return false;
            if (asset.Tracks.Exists(track => TrackIs(track.Name, "anim_shooting")) &&
                !TryGetClip(asset.Name, "anim_shooting", out ignored, out shooting)) return false;
            string group = ReferenceEquals(head, idle) || ReferenceEquals(head, shooting)
                ? "standard" : head.Start + ":" + head.Count;
            string key = body.Start + ":" + body.Count + ":" + anchor.Name + ":" + group;
            if (asset.AttachedBounds.TryGetValue(key, out bounds)) return true;

            Rect headBounds = head.Bounds;
            if (idle != null) headBounds = UnionBounds(headBounds, idle.Bounds);
            if (shooting != null) headBounds = UnionBounds(headBounds, shooting.Bounds);
            float minX = body.Bounds.x, minY = body.Bounds.y;
            float maxX = body.Bounds.x + body.Bounds.width, maxY = body.Bounds.y + body.Bounds.height;
            for (int i = 0; i < body.Count; i++)
            {
                Frame from = GetFrame(anchor, body.Start + i);
                Frame to = GetFrame(anchor, body.Start + ((i + 1) % body.Count));
                for (int sample = 0; sample < 8; sample++)
                {
                    Matrix4x4 overlay = PartMatrix(Interpolate(from, to, sample / 8f)) * inverseBase;
                    IncludeRectBounds(overlay, headBounds, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
            if (!IsFinite(minX) || !IsFinite(minY) || !IsFinite(maxX) || !IsFinite(maxY) || maxX <= minX || maxY <= minY)
                return Fail("Could not calculate attached plant bounds: " + asset.Name);
            float padding = 1f + Math.Max(maxX - minX, maxY - minY) * 0.02f;
            bounds = new Rect(minX - padding, minY - padding, maxX - minX + 2f * padding, maxY - minY + 2f * padding);
            asset.AttachedBounds[key] = bounds;
            return true;
        }

        private static Rect UnionBounds(Rect a, Rect b)
        {
            float x = Math.Min(a.x, b.x), y = Math.Min(a.y, b.y);
            float right = Math.Max(a.x + a.width, b.x + b.width);
            float bottom = Math.Max(a.y + a.height, b.y + b.height);
            return new Rect(x, y, right - x, bottom - y);
        }

        /// <summary>Releases loaded textures. Call from the Unity main thread.</summary>
        public void Dispose()
        {
            if (disposed) return;
            foreach (Texture2D texture in textures.Values)
            {
                if (texture == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                else UnityEngine.Object.DestroyImmediate(texture);
            }
            textures.Clear();
            animations.Clear();
            disposed = true;
        }

        private bool TryGetClip(string name, string animation, out Reanimation asset, out Clip clip)
        {
            asset = null;
            clip = null;
            if (disposed) return Fail("The renderer has been disposed.");
            if (string.IsNullOrWhiteSpace(name)) return Fail("An animation file name is required.");
            try
            {
                EnsureIndex();
                string key = name.EndsWith(".reanim", StringComparison.OrdinalIgnoreCase)
                    ? name.Substring(0, name.Length - ".reanim".Length) : name;
                if (!animations.TryGetValue(key, out asset))
                {
                    string path;
                    if (!animationPaths.TryGetValue(key, out path)) return Fail("Reanimation not found: " + name);
                    asset = LoadReanimation(path, key);
                    animations.Add(key, asset);
                }
                bool useDefault = string.IsNullOrWhiteSpace(animation);
                string clipName = useDefault ? DefaultAnimation(asset.Name) : animation.Trim();
                if (!string.IsNullOrEmpty(clipName) && !clipName.StartsWith("anim_", StringComparison.OrdinalIgnoreCase))
                    clipName = "anim_" + clipName;
                Track marker = null;
                if (!string.IsNullOrEmpty(clipName))
                    marker = asset.Tracks.Find(track => string.Equals(track.Name, clipName, StringComparison.OrdinalIgnoreCase));
                if (marker == null && !useDefault) return Fail("Animation not found: " + animation + " in " + name);
                if (marker == null && useDefault)
                {
                    marker = asset.Tracks.Find(track => string.Equals(track.Name, "anim_idle", StringComparison.OrdinalIgnoreCase));
                    if (marker == null)
                        marker = asset.Tracks.Find(track => track.Name.StartsWith("anim_", StringComparison.OrdinalIgnoreCase));
                }
                string cacheKey = marker == null ? "" : marker.Name;
                if (asset.Clips.TryGetValue(cacheKey, out clip)) return true;
                int start = 0;
                int end = asset.FrameCount;
                if (marker != null)
                {
                    while (start < marker.Frames.Length && marker.Frames[start].Visibility < 0f) start++;
                    end = start;
                    while (end < marker.Frames.Length && marker.Frames[end].Visibility >= 0f) end++;
                }
                if (end <= start) return Fail("The animation has no visible frames: " + cacheKey);
                clip = new Clip { Start = start, Count = end - start };
                if (!TryCalculateBounds(asset, clip, out clip.Bounds))
                    return Fail("The animation has no drawable image tracks: " + name + " / " + cacheKey);
                asset.Clips.Add(cacheKey, clip);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is XmlException || error is FormatException ||
                                          error is ArgumentException || error is InvalidOperationException)
            {
                return Fail("Could not load " + name + ": " + error.Message);
            }
        }

        private static string DefaultAnimation(string name)
        {
            if (IsPeaShooter(name)) return "anim_full_idle";
            if (string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase)) return "anim_walk";
            if (string.Equals(name, "LawnMower", StringComparison.OrdinalIgnoreCase)) return "anim_normal";
            return "anim_idle";
        }

        private static bool IsPeaShooter(string name)
        {
            if (name == null) return false;
            if (name.EndsWith(".reanim", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".reanim".Length);
            return TrackIs(name, "PeaShooter") || TrackIs(name, "PeaShooterSingle") || TrackIs(name, "SnowPea");
        }

        private static bool IsAnimation(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual)) return false;
            actual = actual.Trim();
            if (!actual.StartsWith("anim_", StringComparison.OrdinalIgnoreCase)) actual = "anim_" + actual;
            return TrackIs(actual, expected);
        }

        private static bool IsValidDestination(Rect destination)
        {
            return IsFinite(destination.x) && IsFinite(destination.y) && IsFinite(destination.width) &&
                   IsFinite(destination.height) && destination.width > 0f && destination.height > 0f;
        }

        private static Matrix4x4 LayoutMatrix(Rect destination, Rect bounds)
        {
            float scale = Math.Min(destination.width / bounds.width, destination.height / bounds.height);
            Matrix4x4 layout = Matrix4x4.identity;
            layout.m00 = scale; layout.m11 = scale;
            layout.m03 = destination.x + (destination.width - bounds.width * scale) * 0.5f - bounds.x * scale;
            layout.m13 = destination.y + (destination.height - bounds.height * scale) * 0.5f - bounds.y * scale;
            return layout;
        }

        private static FrameSample SampleFrame(Clip clip, float seconds, float fps, bool loop)
        {
            double time = Math.Max(0d, (double)seconds) * fps;
            time = loop ? time % clip.Count : Math.Min(time, clip.Count - 1d);
            int index = (int)Math.Floor(time);
            return new FrameSample
            {
                Current = clip.Start + index,
                Next = clip.Start + (index + 1 < clip.Count ? index + 1 : (loop ? 0 : index)),
                Fraction = (float)(time - index)
            };
        }

        private static bool DrawClipPose(Reanimation asset, Clip clip, float seconds, float fps, bool loop,
            Matrix4x4 parent, Color tint, string overrideTrack = null, Texture2D imageOverride = null)
        {
            FrameSample time = SampleFrame(clip, seconds, fps, loop);
            bool drawn = false;
            foreach (Track track in asset.Tracks)
            {
                Frame from = GetFrame(track, time.Current);
                if (from.Visibility < 0f || from.Texture == null) continue;
                Frame frame = Interpolate(from, GetFrame(track, time.Next), time.Fraction);
                if (frame.Alpha <= 0f) continue;
                GUI.matrix = parent * PartMatrix(frame);
                GUI.color = new Color(tint.r, tint.g, tint.b, tint.a * Math.Max(0f, Math.Min(1f, frame.Alpha)));
                Texture2D image = imageOverride != null && TrackIs(track.Name, overrideTrack) ? imageOverride : from.Texture;
                GUI.DrawTexture(new Rect(0f, 0f, from.Texture.width, from.Texture.height), image, ScaleMode.StretchToFill, true);
                drawn = true;
            }
            return drawn;
        }

        private static bool TryInverseAffine(Matrix4x4 matrix, out Matrix4x4 inverse)
        {
            inverse = Matrix4x4.identity;
            float determinant = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
            if (!IsFinite(determinant) || Math.Abs(determinant) < 0.000001f) return false;
            inverse.m00 = matrix.m11 / determinant; inverse.m01 = -matrix.m01 / determinant;
            inverse.m10 = -matrix.m10 / determinant; inverse.m11 = matrix.m00 / determinant;
            inverse.m03 = -(inverse.m00 * matrix.m03 + inverse.m01 * matrix.m13);
            inverse.m13 = -(inverse.m10 * matrix.m03 + inverse.m11 * matrix.m13);
            return true;
        }

        private void EnsureIndex()
        {
            if (indexed) return;
            string reanimDirectory = Path.Combine(root, "reanim");
            if (!Directory.Exists(reanimDirectory))
                throw new DirectoryNotFoundException("Missing reanim directory: " + reanimDirectory);
            foreach (string path in Directory.EnumerateFiles(reanimDirectory))
            {
                string extension = Path.GetExtension(path);
                string key = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(extension, ".reanim", StringComparison.OrdinalIgnoreCase))
                    animationPaths[key] = path;
                else if (IsImage(extension)) AddImagePath(reanimImagePaths, key, path, extension);
            }
            string imageDirectory = Path.Combine(root, "images");
            if (Directory.Exists(imageDirectory))
            {
                foreach (string path in Directory.EnumerateFiles(imageDirectory))
                {
                    string extension = Path.GetExtension(path);
                    if (IsImage(extension))
                        AddImagePath(imagePaths, Path.GetFileNameWithoutExtension(path), path, extension);
                }
            }
            indexed = true;
        }

        private static bool IsImage(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddImagePath(Dictionary<string, string> paths, string key, string path, string extension)
        {
            if (!paths.ContainsKey(key) || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
                paths[key] = path;
        }

        private Reanimation LoadReanimation(string path, string name)
        {
            // The original files are XML fragments with sibling fps and track nodes.
            string contents = File.ReadAllText(path);
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            XmlDocument document = new XmlDocument { XmlResolver = null };
            using (StringReader input = new StringReader("<reanim>" + contents + "</reanim>"))
            using (XmlReader reader = XmlReader.Create(input, settings)) document.Load(reader);
            XmlNode fpsNode = document.SelectSingleNode("/reanim/fps");
            if (fpsNode == null) throw new FormatException("Missing fps.");
            Reanimation asset = new Reanimation { Name = name, Fps = ParseNumber(fpsNode.InnerText) };
            if (asset.Fps <= 0f) throw new FormatException("The fps must be positive.");
            foreach (XmlNode node in document.SelectNodes("/reanim/track"))
            {
                XmlNode trackName = node.SelectSingleNode("name");
                if (trackName == null) throw new FormatException("A track is missing its name.");
                List<Frame> frames = new List<Frame>();
                Frame state = new Frame { ScaleX = 1f, ScaleY = 1f, Alpha = 1f };
                foreach (XmlNode frameNode in node.SelectNodes("t"))
                {
                    foreach (XmlNode value in frameNode.ChildNodes)
                    {
                        if (value.NodeType != XmlNodeType.Element) continue;
                        switch (value.Name)
                        {
                            case "x": state.X = ParseNumber(value.InnerText); break;
                            case "y": state.Y = ParseNumber(value.InnerText); break;
                            case "kx": state.Kx = ParseNumber(value.InnerText); break;
                            case "ky": state.Ky = ParseNumber(value.InnerText); break;
                            case "sx": state.ScaleX = ParseNumber(value.InnerText); break;
                            case "sy": state.ScaleY = ParseNumber(value.InnerText); break;
                            case "a": state.Alpha = ParseNumber(value.InnerText); break;
                            case "f": state.Visibility = ParseNumber(value.InnerText); break;
                            case "i": state.Texture = string.IsNullOrEmpty(value.InnerText) ? null : LoadTexture(value.InnerText); break;
                        }
                    }
                    frames.Add(state);
                }
                if (frames.Count == 0) continue;
                asset.Tracks.Add(new Track { Name = trackName.InnerText, Frames = frames.ToArray() });
                asset.FrameCount = Math.Max(asset.FrameCount, frames.Count);
            }
            if (asset.FrameCount == 0) throw new FormatException("No animation frames were found.");
            return asset;
        }

        private Texture2D LoadTexture(string imageId)
        {
            const string reanimPrefix = "IMAGE_REANIM_";
            const string imagePrefix = "IMAGE_";
            bool isReanim = imageId.StartsWith(reanimPrefix, StringComparison.OrdinalIgnoreCase);
            string key = isReanim ? imageId.Substring(reanimPrefix.Length) :
                (imageId.StartsWith(imagePrefix, StringComparison.OrdinalIgnoreCase) ? imageId.Substring(imagePrefix.Length) : imageId);
            string path;
            Dictionary<string, string> paths = isReanim ? reanimImagePaths : imagePaths;
            if (!paths.TryGetValue(key, out path))
                throw new FileNotFoundException("Image " + imageId + " was not found under " + (isReanim ? "reanim" : "images") + ".");
            Texture2D texture;
            if (textures.TryGetValue(path, out texture)) return texture;
            byte[] bytes = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, true))
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                else UnityEngine.Object.DestroyImmediate(texture);
                throw new FormatException("Could not decode image: " + path);
            }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            textures.Add(path, texture);
            return texture;
        }

        private static float ParseNumber(string text)
        {
            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !IsFinite(value))
                throw new FormatException("Invalid reanimation number: " + text);
            return value;
        }
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
        private static Frame GetFrame(Track track, int frame)
        {
            return track.Frames[Math.Min(frame, track.Frames.Length - 1)];
        }
        private static Frame Interpolate(Frame from, Frame to, float t)
        {
            // The exporter already unwraps angles; do not use shortest-path angle interpolation.
            return new Frame
            {
                X = Lerp(from.X, to.X, t), Y = Lerp(from.Y, to.Y, t),
                Kx = Lerp(from.Kx, to.Kx, t), Ky = Lerp(from.Ky, to.Ky, t),
                ScaleX = Lerp(from.ScaleX, to.ScaleX, t), ScaleY = Lerp(from.ScaleY, to.ScaleY, t),
                Alpha = Lerp(from.Alpha, to.Alpha, t), Visibility = from.Visibility, Texture = from.Texture
            };
        }
        private static float Lerp(float from, float to, float t) { return from + (to - from) * t; }

        private static Matrix4x4 PartMatrix(Frame frame)
        {
            double x = frame.Kx * (Math.PI / 180d);
            double y = frame.Ky * (Math.PI / 180d);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = frame.ScaleX * (float)Math.Cos(x);
            matrix.m10 = frame.ScaleX * (float)Math.Sin(x);
            matrix.m01 = -frame.ScaleY * (float)Math.Sin(y);
            matrix.m11 = frame.ScaleY * (float)Math.Cos(y);
            matrix.m03 = frame.X;
            matrix.m13 = frame.Y;
            return matrix;
        }

        private static bool TryCalculateBounds(Reanimation asset, Clip clip, out Rect bounds,
            bool filterTracks = false, ZombieVariant variant = ZombieVariant.Normal, bool equipmentIntact = false,
            Func<string, bool> custom = null)
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (Track track in asset.Tracks)
            {
                if (filterTracks && !IsTrackVisible(asset, track.Name, variant, equipmentIntact, custom)) continue;
                for (int i = 0; i < clip.Count; i++)
                {
                    Frame from = GetFrame(track, clip.Start + i);
                    if (from.Visibility < 0f || from.Texture == null) continue;
                    Frame to = GetFrame(track, clip.Start + ((i + 1) % clip.Count));
                    // Include interpolated poses, including the loop transition, for stable bounds.
                    for (int sample = 0; sample < 8; sample++)
                    {
                        Matrix4x4 m = PartMatrix(Interpolate(from, to, sample / 8f));
                        IncludeTextureBounds(m, from.Texture, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
            }
            if (!IsFinite(minX) || !IsFinite(minY) || maxX <= minX || maxY <= minY)
            {
                bounds = default(Rect);
                return false;
            }
            float padding = 1f + Math.Max(maxX - minX, maxY - minY) * 0.02f;
            bounds = new Rect(minX - padding, minY - padding, maxX - minX + 2f * padding, maxY - minY + 2f * padding);
            return true;
        }

        private static void IncludeTextureBounds(Matrix4x4 m, Texture2D texture,
            ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float width = texture.width, height = texture.height;
            IncludePoint(m.m03, m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m00 * width, m.m13 + m.m10 * width, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m01 * height, m.m13 + m.m11 * height, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m00 * width + m.m01 * height, m.m13 + m.m10 * width + m.m11 * height,
                ref minX, ref minY, ref maxX, ref maxY);
        }

        private static void IncludeRectBounds(Matrix4x4 m, Rect rect,
            ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float x = rect.x, y = rect.y, right = x + rect.width, bottom = y + rect.height;
            IncludePoint(m.m00 * x + m.m01 * y + m.m03, m.m10 * x + m.m11 * y + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * right + m.m01 * y + m.m03, m.m10 * right + m.m11 * y + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * x + m.m01 * bottom + m.m03, m.m10 * x + m.m11 * bottom + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * right + m.m01 * bottom + m.m03, m.m10 * right + m.m11 * bottom + m.m13, ref minX, ref minY, ref maxX, ref maxY);
        }
        private static void IncludePoint(float x, float y, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
        private bool Fail(string message) { LastError = message; return false; }
    }
}
