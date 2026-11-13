#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Samples original PopCap animations and submits their textured tracks to pooled scene renderers.</summary>
    public sealed class ReanimationRenderer : IDisposable
    {
        /// <summary>Limits dictionary buffers retained after actors leave the visible presentation.</summary>
        private const int MaximumRetainedPoseBlends = 256;
        /// <summary>Bounds inactive capture textures after temporary combat feedback ends.</summary>
        private const int MaximumRetainedNativeSurfaces = 16;
        /// <summary>Bounds inactive color and depth attachment storage to roughly 32 MiB.</summary>
        private const long MaximumRetainedNativePixels = 4L * 1024 * 1024;
        /// <summary>Defines frame.</summary>
        private struct Frame
        {
            /// <summary>Stores x, y, kx, ky, scale x, scale y, alpha, visibility.</summary>
            public float X, Y, Kx, Ky, ScaleX, ScaleY, Alpha, Visibility;
            /// <summary>Stores texture.</summary>
            public SpriteImage? Texture;
        }

        /// <summary>Defines frame sample.</summary>
        private struct FrameSample
        {
            /// <summary>Stores current, next.</summary>
            public int Current, Next;
            /// <summary>Stores fraction.</summary>
            public float Fraction;
        }

        /// <summary>Defines track.</summary>
        private sealed class Track
        {
            /// <summary>Stores name.</summary>
            public string Name = string.Empty;
            /// <summary>Caches immutable default visibility for every variant and equipment state.</summary>
            public int VariantVisibility;
            /// <summary>Identifies tracks whose default visibility follows the ground-zombie variant rules.</summary>
            public bool GroundZombie;
            /// <summary>Stores frames.</summary>
            public Frame[] Frames = System.Array.Empty<Frame>();
        }

        /// <summary>Defines clip.</summary>
        private sealed class Clip
        {
            /// <summary>Stores start.</summary>
            public int Start;
            /// <summary>Stores count.</summary>
            public int Count;
            /// <summary>Stores bounds.</summary>
            public Rect Bounds;
            /// <summary>Stores variant bounds.</summary>
            public readonly Dictionary<int, Rect> VariantBounds = new Dictionary<int, Rect>();
        }

        /// <summary>Defines native pose blend.</summary>
        private sealed class NativePoseBlend
        {
            /// <summary>Stores animation.</summary>
            public string? Animation = string.Empty;
            /// <summary>Stores started at, fraction.</summary>
            public float StartedAt, Fraction;
            /// <summary>Retains the caller's unwrapped playback clock to detect a replay of the same clip.</summary>
            public float LastSeconds;
            /// <summary>The outer presentation frame that most recently sampled this actor.</summary>
            public int LastSeenFrame;
            /// <summary>Stores from.</summary>
            public Dictionary<string, Frame> From = new Dictionary<string, Frame>();
            /// <summary>Stores current.</summary>
            public readonly Dictionary<string, Frame> Current = new Dictionary<string, Frame>();
        }

        /// <summary>Performs blend native pose.</summary>
        /// <param name = "blend">The blend.</param>
        /// <param name = "track">The track.</param>
        /// <param name = "pose">The pose.</param>
        /// <returns>The resulting blend native pose.</returns>
        private static Frame BlendNativePose(NativePoseBlend? blend, string track, Frame pose)
        {
            if (blend == null)
            {
                return pose;
            }

            Frame previous;
            if (blend.Fraction < 1f && blend.From.TryGetValue(track, out previous))
            {
                var interpolated = Interpolate(previous, pose, blend.Fraction);
                interpolated.Kx = BlendNativeAngle(previous.Kx, pose.Kx, blend.Fraction);
                interpolated.Ky = BlendNativeAngle(previous.Ky, pose.Ky, blend.Fraction);
                // The current track owns images/visibility; native blending interpolates transforms.
                interpolated.Texture = pose.Texture;
                interpolated.Visibility = pose.Visibility;
                pose = interpolated;
            }

            blend.Current[track] = pose;
            return pose;
        }

        /// <summary>Defines reanimation.</summary>
        private sealed class Reanimation
        {
            /// <summary>Stores name.</summary>
            public string Name = string.Empty;
            /// <summary>Stores fps.</summary>
            public float Fps;
            /// <summary>Stores frame count.</summary>
            public int FrameCount;
            /// <summary>Stores tracks.</summary>
            public readonly List<Track> Tracks = new List<Track>();
            /// <summary>Preserves original track indices within each native shield type's ordered render groups.</summary>
            public int[][][] NativeTrackGroups = Array.Empty<int[][]>();
            /// <summary>Indexes the first track with each original case-insensitive name.</summary>
            public readonly Dictionary<string, Track> TracksByName = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
            /// <summary>Stores clips.</summary>
            public readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>(StringComparer.OrdinalIgnoreCase);
            /// <summary>Caches resolved requested clips, including default animation aliases.</summary>
            public readonly Dictionary<string, Clip> RequestedClips = new Dictionary<string, Clip>(StringComparer.OrdinalIgnoreCase);
            /// <summary>Stores attached bounds.</summary>
            public readonly Dictionary<(Clip Body, Clip? Head, Track Anchor), Rect> AttachedBounds = new Dictionary<(Clip Body, Clip? Head, Track Anchor), Rect>();
        }

        /// <summary>Stores root.</summary>
        private readonly string _root;
        /// <summary>Stores animation paths.</summary>
        private readonly Dictionary<string, string> _animationPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Shares logical images and generated resource text with the session.</summary>
        private SpriteAssetProvider? _assets;
        /// <summary>Gets the lazily acquired resource provider.</summary>
        private SpriteAssetProvider Assets => _assets ??= SpriteAssetProvider.ForRoot(_root);
        /// <summary>Resolves repeated original image identifiers without allocating prefix substrings.</summary>
        private readonly Dictionary<string, SpriteImage> _texturesById = new Dictionary<string, SpriteImage>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores animations.</summary>
        private readonly Dictionary<string, Reanimation> _animations = new Dictionary<string, Reanimation>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Defines native surface.</summary>
        private sealed class NativeSurface
        {
            /// <summary>The color and depth attachments retained with this capture's mesh pool.</summary>
            public readonly RenderTexture Texture;
            /// <summary>Creates the independent attachments retained by a cropped actor image.</summary>
            /// <param name="width">The capture width in source pixels.</param>
            /// <param name="height">The capture height in source pixels.</param>
            public NativeSurface(int width, int height)
            {
                Texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                Texture.Create();
            }
            /// <summary>Stores ready.</summary>
            public bool Ready;
            /// <summary>The complete scene frame still referencing this texture from a composite quad.</summary>
            public int LastPresentedFrame;
        }

        /// <summary>Stores native surfaces.</summary>
        private readonly Dictionary<string, NativeSurface> _nativeSurfaces = new Dictionary<string, NativeSurface>();
        /// <summary>Gets the number of local capture surfaces retained by this animation renderer.</summary>
        internal int NativeSurfaceCount => _nativeSurfaces.Count;
        /// <summary>Reuses capture textures after their logical actors expire.</summary>
        private readonly List<NativeSurface> _inactiveNativeSurfaces = new List<NativeSurface>();
        /// <summary>The number of source pixels retained in inactive capture attachments.</summary>
        private long _inactiveNativePixels;
        /// <summary>Gets the bounded number of inactive capture resources awaiting another actor.</summary>
        internal int InactiveNativeSurfaceCount => _inactiveNativeSurfaces.Count;
        /// <summary>Gets the number of independent capture textures created during this renderer's lifetime.</summary>
        internal int NativeSurfaceCreationCount { get; private set; }
        /// <summary>Shares synchronous capture cameras and mesh buffers across actors, retaining one pool for each nesting depth.</summary>
        private readonly List<ObjectSpriteRenderer> _nativeCaptureRenderers = new List<ObjectSpriteRenderer>();
        /// <summary>The number of capture callbacks currently producing an image before its synchronous render completes.</summary>
        private int _nativeCaptureDepth;
        /// <summary>Gets the number of capture cameras and mesh pools retained independently of actor count.</summary>
        internal int NativeCaptureRendererCount => _nativeCaptureRenderers.Count;
        /// <summary>Caches stable blend identities for attached heads between presentation frames.</summary>
        private readonly Dictionary<(string Instance, int Head), string> _headInstanceKeys = new Dictionary<(string Instance, int Head), string>();
        /// <summary>Collects expired head identities without mutating their dictionary during traversal.</summary>
        private readonly List<(string Instance, int Head)> _expiredHeadInstanceKeys = new List<(string Instance, int Head)>();
        /// <summary>Caches child asset names resolved from the immutable native definitions.</summary>
        private readonly Dictionary<int, string> _attachmentNames = new Dictionary<int, string>();
        /// <summary>Reuses each supported expanded potato-mine glow clip.</summary>
        private readonly Dictionary<int, Clip> _potatoLightClips = new Dictionary<int, Clip>();
        /// <summary>The reusable scene objects used by the current screen batch.</summary>
        private ObjectSpriteRenderer? _objectRenderer;
        /// <summary>The source opacity classifications shared by all screen and cropped surface pools.</summary>
        private readonly TextureOpacityCache _textureOpacity = new TextureOpacityCache();
        /// <summary>The reusable scene objects used by the active cropped native surface.</summary>
        private ObjectSpriteRenderer? _nativeObjectRenderer;
        /// <summary>Stores rendering native surface.</summary>
        private bool _renderingNativeSurface;
        /// <summary>Stores native surface matrix.</summary>
        private Matrix4x4 _nativeSurfaceMatrix;
        /// <summary>Bounds the current offscreen capture in its own destination pixels.</summary>
        private Rect _nativeCaptureClip;
        /// <summary>Overrides clipping for one native body track and its particle attachments.</summary>
        private Rect? _nativeSpriteClip;
        /// <summary>Gets or sets sprite matrix.</summary>
        private Matrix4x4 SpriteMatrix
        {
            get
            {
                return _renderingNativeSurface ? _nativeSurfaceMatrix : SpriteDrawState.Matrix;
            }

            set
            {
                if (_renderingNativeSurface)
                {
                    _nativeSurfaceMatrix = value;
                }
                else
                {
                    SpriteDrawState.Matrix = value;
                }
            }
        }

        /// <summary>Stores native portrait material.</summary>
        private Material? _nativePortraitMaterial;
        /// <summary>The managed imitation filter inherited by synchronous source-track and nested capture draws.</summary>
        private float _nativeGrayscale;
        /// <summary>The managed source-colored brightness inherited by synchronous source-track and nested capture draws.</summary>
        private float _nativeProducerAdditive;

        /// <summary>Gets the native portrait material, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private Material RequiredNativePortraitMaterial => _nativePortraitMaterial ?? throw new System.InvalidOperationException("The member '_nativePortraitMaterial' is not available.");
        /// <summary>Reuses live detached-part particle identities without retaining expired presentation frames.</summary>
        private readonly PresentationKeyCache _particleKeys = new PresentationKeyCache();
        /// <summary>The shared scene frame most recently associated with detached-part identities.</summary>
        private int _particleKeyFrame = -1;
        /// <summary>Stores indexed.</summary>
        private bool _indexed;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>The nesting depth of explicit complete presentation frames.</summary>
        private int _objectFrameDepth;
        /// <summary>The identity of the outer presentation frame used to retire actor-specific playback state.</summary>
        private int _nativeBlendFrame;
        /// <summary>Gets the number of live actor and head blend identities.</summary>
        internal int NativePoseBlendCount => _nativePoseBlends.Count;
        /// <summary>Gets the number of live cached head identities.</summary>
        internal int HeadInstanceKeyCount => _headInstanceKeys.Count;
        /// <summary>Begins a screen batch for backgrounds, shadows, animated objects and projectiles.</summary>
        public void BeginObjectFrame()
        {
            if (CanSubmitObjectFrame)
            {
                GetObjectRenderer().BeginFrame();
                if (_objectFrameDepth++ == 0)
                {
                    _nativeBlendFrame++;
                    _particleKeys.BeginFrame();
                    _particleKeyFrame = SpriteDrawState.FrameVersion;
                }
            }
        }

        /// <summary>Publishes the current sprite batch for the shared direct-display scene camera.</summary>
        public void EndObjectFrame()
        {
            if (CanSubmitObjectFrame && _objectRenderer != null)
            {
                _objectRenderer.EndFrame();
                if (_objectFrameDepth > 0 && --_objectFrameDepth == 0)
                {
                    RetireUnusedNativePlayback();
                }
            }
        }

        /// <summary>Gets whether this renderer can submit explicit presentation commands.</summary>
        private bool CanSubmitObjectFrame => !_disposed;

        /// <summary>Gets or creates the screen renderer owned by this animation renderer.</summary>
        /// <returns>The reusable pool of scene sprite objects.</returns>
        private ObjectSpriteRenderer GetObjectRenderer()
        {
            if (_objectRenderer == null)
            {
                _objectRenderer = new ObjectSpriteRenderer(_textureOpacity);
            }

            return _objectRenderer;
        }

        /// <summary>Begins a nested draw scope when an animation is drawn outside a board batch.</summary>
        /// <returns>A scope that publishes only after its outermost animation draw completes.</returns>
        private ObjectRenderScope BeginObjectScope()
        {
            if (!CanSubmitObjectFrame || _renderingNativeSurface)
            {
                return default;
            }

            return GetObjectRenderer().BeginFrame();
        }

        /// <summary>Gets the stable inspection nodes shared by direct draws and local captures.</summary>
        internal SpriteHierarchy Hierarchy { get; } = new SpriteHierarchy();
        /// <summary>The visibility revision represented by currently frozen captures.</summary>
        private int _hierarchyVisibilityRevision;

        /// <summary>Gets the unchanged source root used to identify shared animation and particle providers.</summary>
        internal string ResourceRoot => _root;

        /// <summary>Reads original resource text from the selected baked or loose source.</summary>
        /// <param name="path">The original relative resource path.</param>
        /// <param name="text">Receives the original text when present.</param>
        /// <returns>Whether the original text resource exists.</returns>
        internal bool TryReadResource(string path, out string text)
        {
            return Assets.TryReadText(path, out text);
        }

        /// <summary>The reason the most recent Draw or TryGetAnimationInfo call failed.</summary>
        public string? LastError { get; private set; }

        /// <summary>Initializes reanimation renderer.</summary>
        /// <param name = "resourceRoot">The resource root.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public ReanimationRenderer(string resourceRoot)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
            {
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            }

            _root = SpriteAssetProvider.NormalizeRoot(resourceRoot);
        }

        /// <summary>Draws a looping animation inside a screen-space rectangle, preserving aspect ratio.</summary>
        public bool Draw(string name, Rect destination, float seconds, string? animation = null)
        {
            using var objectScope = BeginObjectScope();
            return Draw(name, destination, seconds, animation, true);
        }

        /// <summary>Set loop to false to hold the last frame of a one-shot animation.</summary>
        public bool Draw(string name, Rect destination, float seconds, string? animation, bool loop)
        {
            using var objectScope = BeginObjectScope();
            if (IsPeaShooter(name))
            {
                if (string.IsNullOrWhiteSpace(animation) || IsAnimation(animation, "anim_full_idle"))
                {
                    return DrawAttachedPlant(name, destination, seconds, seconds, "anim_head_idle", loop);
                }

                if (IsAnimation(animation, "anim_shooting"))
                {
                    return DrawAttachedPlant(name, destination, seconds, seconds, "anim_shooting", loop);
                }
            }

            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, null);
        }

        /// <summary>
        /// Draws with a custom track-name filter. The filter replaces the default Zombie rules;
        /// it must remain consistent during a call and is also applied to the bounding box.
        /// </summary>
        public bool Draw(string name, Rect destination, float seconds, string animation, bool loop, Func<string, bool> trackVisible)
        {
            using var objectScope = BeginObjectScope();
            if (trackVisible == null)
            {
                return Draw(name, destination, seconds, animation, loop);
            }

            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, trackVisible);
        }

        /// <summary>
        /// Overrides one image track for this draw only. The replacement is stretched to the
        /// original track image rectangle, so the animation and cached layout remain unchanged.
        /// Pass an image ID such as IMAGE_REANIM_WALLNUT_CRACKED1, or null for the original image.
        /// </summary>
        public bool DrawWithImageOverride(string name, Rect destination, float seconds, string trackName, string? imageId, string? animation = null, bool loop = true)
        {
            using var objectScope = BeginObjectScope();
            LastError = null;
            if (imageId == null)
            {
                return Draw(name, destination, seconds, animation, loop);
            }

            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(imageId))
            {
                return Fail("A track name and image resource ID are required.");
            }

            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            var track = FindTrack(asset, trackName);
            if (track == null || !Array.Exists(track.Frames, frame => frame.Texture != null))
            {
                return Fail("Image track not found: " + trackName + " in " + name);
            }

            SpriteImage replacement;
            try
            {
                replacement = LoadTexture(imageId);
            }
            catch (Exception error)when (error is IOException || error is UnauthorizedAccessException || error is FormatException || error is ArgumentException || error is InvalidOperationException)
            {
                return Fail("Could not load image " + imageId + ": " + error.Message);
            }

            if (IsPeaShooter(name) && (string.IsNullOrWhiteSpace(animation) || IsAnimation(animation, "anim_full_idle") || IsAnimation(animation, "anim_shooting")))
            {
                var headName = IsAnimation(animation, "anim_shooting") ? "anim_shooting" : "anim_head_idle";
                return DrawAttachedPlantCore(name, destination, seconds, seconds, headName, loop, 0f, 0f, "anim_idle", "anim_stem", trackName, replacement);
            }

            return DrawInternal(name, destination, seconds, animation, loop, ZombieVariant.Normal, false, null, trackName, replacement);
        }

        /// <summary>
        /// Draws a ground zombie. equipmentIntact controls the cone, bucket, door, or flag;
        /// false restores the ordinary unarmored body and arms.
        /// </summary>
        public bool DrawZombie(Rect destination, float seconds, ZombieVariant variant, bool equipmentIntact = true, string? animation = null, bool loop = true)
        {
            using var objectScope = BeginObjectScope();
            if (variant < ZombieVariant.Normal || variant > ZombieVariant.ScreenDoor)
            {
                return Fail("Unknown zombie variant: " + variant);
            }

            return DrawInternal("Zombie", destination, seconds, animation, loop, variant, equipmentIntact, null);
        }

        /// <summary>
        /// Draws an independently animated head attached to a looping plant body. Zero frame rates
        /// use the source fps; provide separate times to keep body motion continuous while shooting.
        /// </summary>
        public bool DrawAttachedPlant(string name, Rect destination, float bodySeconds, float headSeconds, string headAnimation = "anim_shooting", bool headLoop = false, float bodyFramesPerSecond = 0f, float headFramesPerSecond = 0f, string bodyAnimation = "anim_idle", string attachmentTrack = "anim_stem")
        {
            using var objectScope = BeginObjectScope();
            return DrawAttachedPlantCore(name, destination, bodySeconds, headSeconds, headAnimation, headLoop, bodyFramesPerSecond, headFramesPerSecond, bodyAnimation, attachmentTrack, null, null);
        }

        /// <summary>Performs draw attached plant core.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "destination">The destination.</param>
        /// <param name = "bodySeconds">The body seconds.</param>
        /// <param name = "headSeconds">The head seconds.</param>
        /// <param name = "headAnimation">The head animation.</param>
        /// <param name = "headLoop">The head loop.</param>
        /// <param name = "bodyFramesPerSecond">The body frames per second.</param>
        /// <param name = "headFramesPerSecond">The head frames per second.</param>
        /// <param name = "bodyAnimation">The body animation.</param>
        /// <param name = "attachmentTrack">The attachment track.</param>
        /// <param name = "overrideTrack">The override track.</param>
        /// <param name = "imageOverride">The image override.</param>
        /// <param name = "nativePlacement">The native placement.</param>
        /// <returns>The resulting draw attached plant core.</returns>
        private bool DrawAttachedPlantCore(string? name, Rect destination, float bodySeconds, float headSeconds, string headAnimation, bool headLoop, float bodyFramesPerSecond, float headFramesPerSecond, string bodyAnimation, string attachmentTrack, string? overrideTrack, SpriteImage? imageOverride, Matrix4x4? nativePlacement = null)
        {
            LastError = null;
            if (!IsValidDestination(destination) || !IsFinite(bodySeconds) || !IsFinite(headSeconds) || !IsFinite(bodyFramesPerSecond) || !IsFinite(headFramesPerSecond) || bodyFramesPerSecond < 0f || headFramesPerSecond < 0f)
            {
                return Fail("The rectangle, times, and frame rates must be finite and nonnegative.");
            }

            if (string.IsNullOrWhiteSpace(headAnimation) || string.IsNullOrWhiteSpace(bodyAnimation) || string.IsNullOrWhiteSpace(attachmentTrack))
            {
                return Fail("Body animation, head animation and attachment track are required.");
            }

            Reanimation? asset;
            Clip? body, head;
            Track? anchor;
            Matrix4x4 inverseBase;
            Rect bounds;
            if (!TryPrepareAttachedPlant(name, bodyAnimation, headAnimation, attachmentTrack, out asset, out body, out head, out anchor, out inverseBase, out bounds))
            {
                return false;
            }

            var bodyFps = bodyFramesPerSecond == 0f ? asset.Fps : bodyFramesPerSecond;
            var headFps = headFramesPerSecond == 0f ? asset.Fps : headFramesPerSecond;
            var bodyTime = SampleFrame(body, bodySeconds, bodyFps, true);
            var anchorPose = Interpolate(GetFrame(anchor, bodyTime.Current), GetFrame(anchor, bodyTime.Next), bodyTime.Fraction);
            var attachment = PartMatrix(anchorPose) * inverseBase;
            var originalMatrix = SpriteMatrix;
            var originalColor = SpriteDrawState.Color;
            var layout = nativePlacement ?? LayoutMatrix(destination, bounds);
            bool drawn;
            try
            {
                drawn = DrawClipPose(asset, body, bodySeconds, bodyFps, true, originalMatrix * layout, originalColor, overrideTrack, imageOverride);
                using var hierarchyAttachment = Hierarchy.BeginAttachment(1);
                drawn |= DrawClipPose(asset, head, headSeconds, headFps, headLoop, originalMatrix * layout * attachment, originalColor, overrideTrack, imageOverride);
            }
            finally
            {
                SpriteMatrix = originalMatrix;
                SpriteDrawState.Color = originalColor;
            }

            if (!drawn)
            {
                return Fail("No visible body or head image tracks at this animation time.");
            }

            return true;
        }

        /// <summary>Performs draw internal.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "destination">The destination.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "variant">The variant.</param>
        /// <param name = "equipmentIntact">The equipment intact.</param>
        /// <param name = "trackVisible">The track visible.</param>
        /// <param name = "overrideTrack">The override track.</param>
        /// <param name = "imageOverride">The image override.</param>
        /// <param name = "nativePlacement">The native placement.</param>
        /// <param name = "cachedFlag">The cached flag.</param>
        /// <param name = "blend">The blend.</param>
        /// <param name="crazyDaveHeldItem">The item attached to Dave's hand, if any.</param>
        /// <param name="crazyDaveHeldItemAge">The elapsed time of the held item's animation.</param>
        /// <param name="trackOffset">Optional native pixel offsets applied to individual animation tracks.</param>
        /// <param name="trackAttachment">Optional original attachments receiving the track, native pose, outer transform, and opacity.</param>
        /// <returns>The resulting draw internal.</returns>
        private bool DrawInternal(string? name, Rect destination, float seconds, string? animation, bool loop, ZombieVariant variant, bool equipmentIntact, Func<string, bool>? trackVisible, string? overrideTrack = null, SpriteImage? imageOverride = null, Matrix4x4? nativePlacement = null, bool cachedFlag = false, NativePoseBlend? blend = null, string? crazyDaveHeldItem = null, float crazyDaveHeldItemAge = 0f, Func<string, Vector2>? trackOffset = null, Action<string, Matrix4x4, Matrix4x4, float>? trackAttachment = null)
        {
            using var profile = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Animation);
            LastError = null;
            if (!IsFinite(seconds) || !IsFinite(destination.x) || !IsFinite(destination.y) || !IsFinite(destination.width) || !IsFinite(destination.height) || destination.width <= 0f || destination.height <= 0f)
            {
                return Fail("The destination and time must be finite, with a positive rectangle size.");
            }

            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            Reanimation? flag = null;
            Clip? flagClip = null;
            var combatFlag = IsZombie(asset) && _nativeCombatZombie != null
                && _nativeCombatZombie.Kind == ZombieKind.Flag && !_nativeCombatZombie.Headless;
            if (combatFlag)
            {
                if (!TryGetNativeDefinitionClip("Zombie_FlagPole", out flag, out flagClip))
                {
                    return false;
                }
            }
            else if (trackVisible == null && IsZombie(asset) && variant == ZombieVariant.Flag && equipmentIntact
                && !TryGetClip("Zombie_flagpole", null, out flag, out flagClip))
            {
                return false;
            }

            var flagBaseInverse = Matrix4x4.identity;
            if (flag != null)
            {
                var hand = FindTrack(asset, "Zombie_flaghand");
                if (hand == null || !TryInverseAffine(PartMatrix(GetFrame(hand, 0)), out flagBaseInverse))
                {
                    return Fail("Flag attachment base pose is missing or singular.");
                }
            }

            Reanimation? heldItem = null;
            Clip? heldItemClip = null;
            var heldItemBaseInverse = Matrix4x4.identity;
            var heldHammer = crazyDaveHeldItem == "HAMMER";
            var heldWallnut = crazyDaveHeldItem == "WALLNUT";
            var heldTreeFood = crazyDaveHeldItem == "TREE_FOOD";
            var heldFertilizer = crazyDaveHeldItem == "FERTILIZER";
            var heldMoneyBag = crazyDaveHeldItem == "MONEYBAG";
            if (heldHammer || heldWallnut || heldTreeFood || heldFertilizer || heldMoneyBag)
            {
                var itemName = heldHammer ? "Hammer" : heldTreeFood ? "TreeFood" : heldFertilizer || heldMoneyBag ? "ZenGarden_fertilizer" : "Wallnut";
                // Fertilizer and tree food have a drawable "bag" track, not an animation marker.
                var itemAnimation = heldHammer ? "anim_whack_zombie" : heldTreeFood || heldFertilizer || heldMoneyBag ? null : "anim_idle";
                if (!TryGetClip(itemName, itemAnimation, out heldItem, out heldItemClip)
                    || !TryGetClip("CrazyDave", "anim_idle_handing", out _, out var daveBase))
                {
                    heldItem = null;
                }
                else
                {
                    var hand = FindTrack(asset, "Dave_handinghand");
                    if (hand == null || !TryInverseAffine(PartMatrix(GetFrame(hand, daveBase.Start)), out heldItemBaseInverse))
                    {
                        heldItem = null;
                    }
                }
            }

            var layout = nativePlacement.GetValueOrDefault();
            if (!nativePlacement.HasValue)
            {
                if (!TryGetBounds(asset, clip, variant, equipmentIntact, trackVisible, out var bounds))
                {
                    return false;
                }

                layout = LayoutMatrix(destination, bounds);
            }

            var timeInFrames = Math.Max(0d, (double)seconds) * asset.Fps;
            if (loop)
            {
                timeInFrames %= clip.Count;
            }
            else
            {
                timeInFrames = Math.Min(timeInFrames, clip.Count - 1d);
            }

            var relativeFrame = (int)Math.Floor(timeInFrames);
            var fraction = (float)(timeInFrames - relativeFrame);
            var current = clip.Start + relativeFrame;
            var next = relativeFrame + 1 < clip.Count ? current + 1 : (loop ? clip.Start : current);
            var originalMatrix = SpriteMatrix;
            var parentMatrix = originalMatrix * layout;
            var originalColor = SpriteDrawState.Color;
            var previousSpriteClip = _nativeSpriteClip;
            var drawn = false;
            try
            {
                // Cached almanac flags are drawn independently at the body origin (0x47ECE0).
                if (flag != null && cachedFlag)
                {
                    if (flagClip == null)
                    {
                        throw new System.InvalidOperationException("The value 'flagClip' is not available.");
                    }

                    using var hierarchyAttachment = Hierarchy.BeginAttachment(10);
                    drawn |= DrawAttachment(flag, flagClip, 0f, parentMatrix, originalColor, 1f);
                }

                // PC shield groups and part overrides must also run on the common DrawInternal path.
                // DrawNativePlayback/DrawZombiePlayback both enter here, including corpse playback.
                var trackGroups = NativeZombieTrackGroups(asset);
                for (var group = 0; group < trackGroups.Length; group++)
                {
                    var indices = trackGroups[group];
                    for (var position = 0; position < indices.Length; position++)
                    {
                        var trackIndex = indices[position];
                        var track = asset.Tracks[trackIndex];
                        if (!IsTrackVisible(track, variant, equipmentIntact, trackVisible) || !NativeZombieTrackVisible(asset, track.Name))
                        {
                            continue;
                        }

                        _nativeSpriteClip = NativePoolTrackClip(asset, track.Name, originalMatrix, previousSpriteClip);
                        var from = GetFrame(track, current);
                        var flagHand = flag != null && !cachedFlag && TrackIs(track.Name, "Zombie_flaghand");
                        if (from.Visibility < 0f || from.Texture == null && !flagHand)
                        {
                            continue;
                        }

                        var frame = BlendNativePose(blend, track.Name, Interpolate(from, GetFrame(track, next), fraction));
                        if (frame.Alpha <= 0f)
                        {
                            continue;
                        }

                        using var hierarchySprite = Hierarchy.BeginSprite(asset, trackIndex, track.Name);

                        var part = PartMatrix(frame);
                        if (trackOffset != null)
                        {
                            var offset = trackOffset(track.Name);
                            part.m03 += offset.x;
                            part.m13 += offset.y;
                        }

                        if (from.Texture != null)
                        {
                            var sprite = NativeZombieTrackMatrix(parentMatrix * part, group);
                            if (!HasDrawableArea(sprite))
                            {
                                continue;
                            }

                            SpriteMatrix = sprite;
                            var alpha = originalColor.a * Math.Max(0f, Math.Min(1f, frame.Alpha));
                            SpriteDrawState.Color = _nativeCombatZombie == null ? new Color(originalColor.r, originalColor.g, originalColor.b, alpha) : NativeZombieTrackColor(asset, track.Name, group, alpha);
                            var image = imageOverride != null && TrackIs(track.Name, overrideTrack) ? imageOverride : from.Texture;
                            var nativeImage = NativeZombieImage(asset, track.Name, image);
                            DrawTexturePart(nativeImage, nativeImage == image ? from.Texture.Width : nativeImage.Width, nativeImage == image ? from.Texture.Height : nativeImage.Height);
                            drawn = true;
                        }

                        trackAttachment?.Invoke(track.Name, layout * part, originalMatrix, frame.Alpha);

                        if (flagHand)
                        {
                            if (flag == null || flagClip == null)
                            {
                                throw new System.InvalidOperationException("The value 'flagClip' is not available.");
                            }

                            // PC Zombie::Initialize (0x53DD90) attaches the flag to Zombie_flaghand.
                            // Reanimation::DrawRenderGroup draws that attachment immediately after its hand track.
                            var attachment = parentMatrix * part * flagBaseInverse;
                            using var hierarchyAttachment = Hierarchy.BeginAttachment(10);
                            if (combatFlag)
                            {
                                var tint = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * frame.Alpha);
                                drawn |= DrawClipPose(flag, flagClip, NativeSeconds(flagClip, RequiredNativeCombatZombie.Age, 15f, flag.Fps, true),
                                    flag.Fps, false, attachment, tint);
                            }
                            else
                            {
                                drawn |= DrawAttachment(flag, flagClip, seconds, attachment, originalColor, frame.Alpha);
                            }
                        }

                        if (heldItem != null && TrackIs(track.Name, "Dave_handinghand"))
                        {
                            // LawnApp::CrazyDaveTalkMessage attaches these items after the hand track.
                            var offset = heldHammer
                                ? Matrix4x4.Translate(new Vector3(62f, 445f, 0f)) * Matrix4x4.Scale(new Vector3(1.5f, 1.5f, 1f))
                                : heldMoneyBag ? Matrix4x4.Translate(new Vector3(90f, 405f, 0f))
                                : heldTreeFood || heldFertilizer ? Matrix4x4.Translate(new Vector3(102f, 412f, 0f))
                                : Matrix4x4.Translate(new Vector3(100f, 393f, 0f)) * Matrix4x4.Scale(new Vector3(1.2f, 1.2f, 1f));
                            var placement = parentMatrix * part * heldItemBaseInverse * offset;
                            using var hierarchyAttachment = Hierarchy.BeginAttachment(11);
                            if (heldItemClip == null)
                            {
                                throw new System.InvalidOperationException("The value 'heldItemClip' is not available.");
                            }

                            var itemSeconds = heldHammer
                                ? (heldItemClip.Count - 1f) / heldItem.Fps
                                : heldTreeFood || heldFertilizer || heldMoneyBag ? 0f : NativeSeconds(heldItemClip, crazyDaveHeldItemAge, 12f, heldItem.Fps, true);
                            if (!heldMoneyBag || Assets.ContainsImage("IMAGE_MONEYBAG"))
                            {
                                var replacement = heldMoneyBag ? LoadTexture("IMAGE_MONEYBAG") : null;
                                drawn |= DrawClipPose(heldItem, heldItemClip, itemSeconds, heldItem.Fps, false, placement, originalColor, heldMoneyBag ? "bag" : null, replacement);
                            }
                        }
                    }
                }
            }
            finally
            {
                SpriteMatrix = originalMatrix;
                SpriteDrawState.Color = originalColor;
                _nativeSpriteClip = previousSpriteClip;
            }

            if (!drawn)
            {
                return Fail("No visible image tracks at this animation time.");
            }

            return true;
        }

        /// <summary>
        /// Draws source reanimation pixels at an explicit origin and scale. Unlike Draw,
        /// this preserves the original canvas, anchor and size; it never fits a bounding box.
        /// </summary>
        public bool DrawNative(string? name, Vector2 origin, float scale, float seconds, string? animation = null, Func<string, bool>? trackVisible = null)
        {
            using var objectScope = BeginObjectScope();
            if (!IsFinite(origin.x) || !IsFinite(origin.y) || !IsFinite(scale) || scale <= 0f || !IsFinite(seconds))
            {
                return Fail("Native origin, scale and time must be finite, with a positive scale.");
            }

            var placement = NativeMatrix(origin, scale);
            var idle = string.IsNullOrWhiteSpace(animation) || IsAnimation(animation, "anim_full_idle");
            if (idle && IsPeaShooter(name))
            {
                return DrawAttachedPlantCore(name, new Rect(0, 0, 1, 1), seconds, seconds, "anim_head_idle", true, 0f, 0f, "anim_idle", "anim_stem", null, null, placement);
            }

            if (idle && (TrackIs(name, "ThreePeater") || TrackIs(name, "SplitPea")))
            {
                return DrawNativeMultiHead(name, placement, seconds);
            }

            return DrawInternal(name, new Rect(0, 0, 1, 1), seconds, animation, true, ZombieVariant.Normal, false, trackVisible, null, null, placement);
        }

        /// <summary>Performs draw zombie native.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "variant">The variant.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "cachedThumbnail">The cached thumbnail.</param>
        /// <returns>The resulting draw zombie native.</returns>
        public bool DrawZombieNative(Vector2 origin, float scale, float seconds, ZombieVariant variant, string animation = "anim_idle", bool cachedThumbnail = false)
        {
            using var objectScope = BeginObjectScope();
            if (!IsFinite(origin.x) || !IsFinite(origin.y) || !IsFinite(scale) || scale <= 0f || !IsFinite(seconds))
            {
                return Fail("Native origin, scale and time must be finite, with a positive scale.");
            }

            return DrawInternal("Zombie", new Rect(0, 0, 1, 1), seconds, animation, true, variant, true, null, null, null, NativeMatrix(origin, scale), cachedThumbnail);
        }

        /// <summary>Performs draw native multi head.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "placement">The placement.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <returns>The resulting draw native multi head.</returns>
        private bool DrawNativeMultiHead(string? name, Matrix4x4 placement, float seconds)
        {
            Reanimation? asset;
            Clip? body;
            if (!TryGetClip(name, "anim_idle", out asset, out body))
            {
                return false;
            }

            var threePeater = TrackIs(name, "ThreePeater");
            var headCount = threePeater ? 3 : 2;
            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            var drawn = false;
            try
            {
                drawn = DrawClipPose(asset, body, seconds, asset.Fps, true, previous * placement, tint);
                for (var index = 0; index < headCount; index++)
                {
                    var headName = threePeater
                        ? index switch
                        {
                            0 => "anim_head_idle1",
                            1 => "anim_head_idle2",
                            _ => "anim_head_idle3"
                        }
                        : index == 0 ? "anim_head_idle" : "anim_splitpea_idle";
                    var anchorName = threePeater
                        ? index switch
                        {
                            0 => "anim_head1",
                            1 => "anim_head2",
                            _ => "anim_head3"
                        }
                        : "anim_idle";
                    Reanimation? ignored;
                    Clip? head;
                    Track? anchor;
                    Matrix4x4 inverseBase;
                    Rect bounds;
                    if (!TryPrepareAttachedPlant(name, "anim_idle", headName, anchorName, out ignored, out body, out head, out anchor, out inverseBase, out bounds))
                    {
                        return false;
                    }

                    var sample = SampleFrame(body, seconds, asset.Fps, true);
                    var pose = Interpolate(GetFrame(anchor, sample.Current), GetFrame(anchor, sample.Next), sample.Fraction);
                    drawn |= DrawClipPose(asset, head, seconds, asset.Fps, true, previous * placement * PartMatrix(pose) * inverseBase, tint);
                }
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }

            return drawn || Fail("No visible multi-head plant tracks: " + name);
        }

        /// <summary>
        /// Draws source pixels into a bounded offscreen surface for native animation cropping.
        /// The cropped texture is submitted as a scene sprite at the presenter's viewport scale.
        /// </summary>
        /// <param name="key">The stable identity of the retained capture surface.</param>
        /// <param name="viewport">The original pixel bounds captured and composited.</param>
        /// <param name="draw">The synchronous callback submitting the actor's source tracks.</param>
        /// <param name="freeze">Whether an already rendered capture can be reused unchanged.</param>
        /// <param name="grayscale">Whether source tracks use imitation-plant grayscale.</param>
        /// <param name="additive">The white hit brightness applied to source tracks.</param>
        /// <param name="topCrop">The visible capture height, or a negative value for the complete surface.</param>
        /// <param name="producerAdditive">The source-colored brightness applied only while drawing the captured tracks.</param>
        /// <returns>Whether the capture callback submitted visible artwork.</returns>
        public bool DrawNativeClipped(string key, Rect viewport, Func<bool> draw, bool freeze = false, bool grayscale = false, float additive = 0f, float topCrop = -1f, float producerAdditive = 0f)
        {
            using var objectScope = BeginObjectScope();
            if (_disposed)
            {
                return Fail("The renderer has been disposed.");
            }

            if (!IsValidDestination(viewport) || draw == null || string.IsNullOrEmpty(key))
            {
                return Fail("A valid native viewport, cache key and draw callback are required.");
            }

            if (_nativePortraitMaterial == null)
            {
                var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
                if (shader == null)
                {
                    return Fail("Native portrait shader resource is missing.");
                }

                _nativePortraitMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            NativeSurface surface;
            int width = Mathf.CeilToInt(viewport.width), height = Mathf.CeilToInt(viewport.height);
            if (!_nativeSurfaces.TryGetValue(key, out surface))
            {
                surface = RentNativeSurface(width, height);
                _nativeSurfaces.Add(key, surface);
            }

            if (surface.Texture.width != width || surface.Texture.height != height)
            {
                return Fail("A native surface cache key was reused with a different viewport size.");
            }

            if (freeze)
            {
                Hierarchy.RefreshVisibility();
            }

            if (_hierarchyVisibilityRevision != Hierarchy.VisibilityRevision)
            {
                foreach (var cachedSurface in _nativeSurfaces.Values)
                {
                    cachedSurface.Ready = false;
                }

                _hierarchyVisibilityRevision = Hierarchy.VisibilityRevision;
            }

            if (!freeze || !surface.Ready)
            {
                var matrix = SpriteDrawState.Matrix;
                var color = SpriteDrawState.Color;
                var previousCapture = _renderingNativeSurface;
                var previousSurfaceMatrix = _nativeSurfaceMatrix;
                var previousCaptureClip = _nativeCaptureClip;
                var previousAdditive = _nativeSurfaceAdditive;
                var previousGrayscale = _nativeGrayscale;
                var previousProducer = _nativeProducerAdditive;
                var previousRenderer = _nativeObjectRenderer;
                var captureRenderer = GetNativeCaptureRenderer();
                captureRenderer.BeginBatch();
                _nativeCaptureDepth++;
                try
                {
                    SpriteDrawState.Matrix = Matrix4x4.identity;
                    _nativeSurfaceMatrix = NativeMatrix(new Vector2(-viewport.x, -viewport.y), 1f);
                    _nativeCaptureClip = new Rect(0f, 0f, width, height);
                    _renderingNativeSurface = true;
                    _nativeObjectRenderer = captureRenderer;
                    _nativeGrayscale = grayscale ? 1f : 0f;
                    _nativeProducerAdditive = producerAdditive;
                    _nativePortraitMaterial.SetFloat("_Grayscale", _nativeGrayscale);
                    _nativePortraitMaterial.SetFloat("_ProducerAdditive", _nativeProducerAdditive);
                    _nativeSurfaceAdditive = Math.Max(0f, additive);
                    surface.Ready = draw();
                    captureRenderer.RenderTo(surface.Texture);
                }
                finally
                {
                    _nativeCaptureDepth--;
                    _renderingNativeSurface = previousCapture;
                    _nativeSurfaceMatrix = previousSurfaceMatrix;
                    _nativeCaptureClip = previousCaptureClip;
                    _nativeSurfaceAdditive = previousAdditive;
                    _nativeObjectRenderer = previousRenderer;
                    _nativeGrayscale = previousGrayscale;
                    _nativeProducerAdditive = previousProducer;
                    _nativePortraitMaterial.SetFloat("_Grayscale", _nativeGrayscale);
                    _nativePortraitMaterial.SetFloat("_ProducerAdditive", _nativeProducerAdditive);
                    SpriteDrawState.Matrix = matrix;
                    SpriteDrawState.Color = color;
                }
            }

            surface.LastPresentedFrame = SpriteDrawState.FrameVersion;
            var surfaceColor = SpriteDrawState.Color;
            SpriteDrawState.Color = Color.white;
            if (topCrop >= 0f)
            {
                var cropHeight = Math.Min(viewport.height, topCrop);
                DrawNativeSurfaceComposite(surface.Texture, new Rect(viewport.x, viewport.y, viewport.width, cropHeight), new Rect(0, 1f - cropHeight / viewport.height, 1, cropHeight / viewport.height));
            }
            else
            {
                DrawNativeSurfaceComposite(surface.Texture, viewport);
            }

            SpriteDrawState.Color = surfaceColor;
            return surface.Ready;
        }

        /// <summary>Performs draw native image.</summary>
        /// <param name = "image">The image.</param>
        /// <param name = "destination">The destination.</param>
        /// <param name = "source">The source.</param>
        /// <returns>The resulting draw native image.</returns>
        /// <summary>Draws an externally owned texture through the shared image submission path.</summary>
        /// <param name="image">The external texture or captured surface.</param>
        /// <param name="destination">The logical destination rectangle.</param>
        /// <param name="source">The normalized source texture rectangle.</param>
        /// <returns>Whether the source was submitted.</returns>
        public bool DrawNativeImage(Texture image, Rect destination, Rect source)
        {
            return image != null && DrawNativeImage(SpriteImage.FromTexture(image), destination, source);
        }

        public bool DrawNativeImage(SpriteImage image, Rect destination, Rect source)
        {
            using var objectScope = BeginObjectScope();
            if (_disposed)
            {
                return Fail("The renderer has been disposed.");
            }

            if (image == null || !IsValidDestination(destination))
            {
                return Fail("A native image and valid destination are required.");
            }

            var previous = SpriteMatrix;
            try
            {
                SpriteMatrix = previous * NativeMatrix(new Vector2(destination.x, destination.y), 1f);
                DrawTexturePart(image, destination.width, destination.height, source);
                return true;
            }
            finally
            {
                SpriteMatrix = previous;
            }
        }

        /// <summary>Performs draw texture part.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "source">The source.</param>
        private void DrawTexturePart(SpriteImage texture, float width, float height, Rect? source = null)
        {
            if (!CanSubmitObjectFrame || !EnsureNativeSpriteMaterial())
            {
                return;
            }

            var uv = source ?? new Rect(0, 0, 1, 1);
            var matrix = SpriteMatrix;
            if (_nativeSpriteAdditive)
            {
                DrawNativeSpritePass(texture, width, height, uv, 2, matrix);
                return;
            }

            DrawNativeSpritePass(texture, width, height, uv, 0, matrix, true);
        }

        /// <summary>Original almanac boss head, with the separate driver and foreground layering.</summary>
        public bool DrawAlmanacBoss(Vector2 origin, float scale, float seconds, bool cachedThumbnail)
        {
            using var objectScope = BeginObjectScope();
            return DrawBossComposite(origin, scale, seconds, cachedThumbnail, "anim_head_idle", true);
        }

        /// <summary>Performs draw boss native.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "headLowered">The head lowered.</param>
        /// <returns>The resulting draw boss native.</returns>
        public bool DrawBossNative(Vector2 origin, float scale, float seconds, bool headLowered)
        {
            using var objectScope = BeginObjectScope();
            return DrawBossComposite(origin, scale, seconds, false, headLowered ? "anim_head_idle" : "anim_idle", false);
        }

        /// <summary>Performs draw boss composite.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "cachedThumbnail">The cached thumbnail.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "almanacOnly">The almanac only.</param>
        /// <returns>The resulting draw boss composite.</returns>
        private bool DrawBossComposite(Vector2 origin, float scale, float seconds, bool cachedThumbnail, string animation, bool almanacOnly)
        {
            if (!IsFinite(origin.x) || !IsFinite(origin.y) || !IsFinite(scale) || scale <= 0f || !IsFinite(seconds))
            {
                return Fail("Boss origin, scale and time must be finite.");
            }

            Reanimation? boss, driver;
            Clip? head, driverIdle;
            if (!TryGetClip("Zombie_boss", animation, out boss, out head) || !TryGetClip("Zombie_Boss_driver", "anim_idle", out driver, out driverIdle))
            {
                return false;
            }

            var anchor = FindTrack(boss, "Boss_head2");
            if (anchor == null)
            {
                return Fail("Boss driver attachment track is missing.");
            }

            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            try
            {
                Matrix4x4 bodyMatrix, driverMatrix;
                if (cachedThumbnail)
                {
                    // 0x47ECE0: cached head at (-524,-88), driver at (46,22).
                    bodyMatrix = previous * NativeMatrix(new Vector2(origin.x - 524f * scale, origin.y - 88f * scale), scale);
                    driverMatrix = previous * NativeMatrix(new Vector2(origin.x + 46f * scale, origin.y + 22f * scale), scale);
                }
                else
                {
                    bodyMatrix = previous * NativeMatrix(origin, scale);
                    var sample = SampleFrame(head, seconds, boss.Fps, true);
                    var pose = Interpolate(GetFrame(anchor, sample.Current), GetFrame(anchor, sample.Next), sample.Fraction);
                    Matrix4x4 inverseBase;
                    if (!TryInverseAffine(PartMatrix(GetFrame(anchor, 0)), out inverseBase))
                    {
                        return Fail("Boss attachment base pose is singular.");
                    }

                    // 0x552610 attaches reanim 93 to Boss_head2 at (28,-84), scale 1.2.
                    driverMatrix = bodyMatrix * PartMatrix(pose) * inverseBase * NativeMatrix(new Vector2(28, -84), 1.2f);
                }

                var visible = almanacOnly ? (Func<string, bool>)BossAlmanacTrack : track => !TrackIs(track, "zombie_butter");
                var drawn = DrawClipPose(boss, head, seconds, boss.Fps, true, bodyMatrix, tint, null, null, visible);
                drawn |= DrawClipPose(driver, driverIdle, seconds, cachedThumbnail ? driver.Fps : 18f, true, driverMatrix, tint);
                drawn |= DrawClipPose(boss, head, seconds, boss.Fps, true, bodyMatrix, tint, null, null, track => visible(track) && !TrackIs(track, "boss_body1") && !TrackIs(track, "Boss_neck") && !TrackIs(track, "Boss_head2"));
                return drawn || Fail("Boss portrait has no visible head or driver tracks.");
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }
        }

        /// <summary>Performs boss almanac track.</summary>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting boss almanac track.</returns>
        private static bool BossAlmanacTrack(string track)
        {
            return !track.StartsWith("Boss_innerleg", StringComparison.OrdinalIgnoreCase) && !track.StartsWith("Boss_outerleg", StringComparison.OrdinalIgnoreCase) && !track.StartsWith("Boss_body2", StringComparison.OrdinalIgnoreCase) && !track.StartsWith("Boss_innerarm", StringComparison.OrdinalIgnoreCase) && !track.StartsWith("Boss_RV", StringComparison.OrdinalIgnoreCase) && !TrackIs(track, "zombie_butter");
        }

        /// <summary>Performs native matrix.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting native matrix.</returns>
        private static Matrix4x4 NativeMatrix(Vector2 origin, float scale)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = matrix.m11 = scale;
            matrix.m03 = origin.x;
            matrix.m13 = origin.y;
            return matrix;
        }

        /// <summary>Returns the padded union of all frames in the selected animation.</summary>
        public bool TryGetAnimationInfo(string name, string? animation, out AnimationInfo info)
        {
            LastError = null;
            info = default(AnimationInfo);
            if (IsPeaShooter(name) && (string.IsNullOrWhiteSpace(animation) || IsAnimation(animation, "anim_full_idle") || IsAnimation(animation, "anim_shooting")))
            {
                var headName = IsAnimation(animation, "anim_shooting") ? "anim_shooting" : "anim_head_idle";
                Reanimation? attachedAsset;
                Clip? body, head;
                Track? anchor;
                Matrix4x4 inverseBase;
                Rect attachedBounds;
                if (!TryPrepareAttachedPlant(name, "anim_idle", headName, "anim_stem", out attachedAsset, out body, out head, out anchor, out inverseBase, out attachedBounds))
                {
                    return false;
                }

                info = new AnimationInfo
                {
                    FramesPerSecond = attachedAsset.Fps,
                    FrameCount = head.Count,
                    SourceBounds = attachedBounds
                };
                return true;
            }

            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            Rect bounds;
            if (!TryGetBounds(asset, clip, ZombieVariant.Normal, false, null, out bounds))
            {
                return false;
            }

            info = new AnimationInfo
            {
                FramesPerSecond = asset.Fps,
                FrameCount = clip.Count,
                SourceBounds = bounds
            };
            return true;
        }

        /// <summary>Applies the ground-zombie track rules without consulting animation frame visibility.</summary>
        public static bool IsZombieTrackVisible(string trackName, ZombieVariant variant, bool equipmentIntact = true)
        {
            if (string.IsNullOrEmpty(trackName))
            {
                return false;
            }

            var cone = equipmentIntact && variant == ZombieVariant.Conehead;
            var bucket = equipmentIntact && variant == ZombieVariant.Buckethead;
            var door = equipmentIntact && variant == ZombieVariant.ScreenDoor;
            var flag = equipmentIntact && variant == ZombieVariant.Flag;
            if (TrackIs(trackName, "anim_cone"))
            {
                return cone;
            }

            if (TrackIs(trackName, "anim_bucket"))
            {
                return bucket;
            }

            if (TrackIs(trackName, "anim_hair"))
            {
                return !cone && !bucket;
            }

            if (TrackIs(trackName, "Zombie_flaghand"))
            {
                return flag;
            }

            if (TrackIs(trackName, "Zombie_innerarm_screendoor"))
            {
                return door || flag;
            }

            if (TrackIs(trackName, "anim_screendoor") || TrackIs(trackName, "Zombie_outerarm_screendoor") || TrackIs(trackName, "Zombie_innerarm_screendoor_hand"))
            {
                return door;
            }

            if (trackName.StartsWith("anim_innerarm", StringComparison.OrdinalIgnoreCase))
            {
                return !door && !flag;
            }

            if (TrackIs(trackName, "Zombie_outerarm_hand") || TrackIs(trackName, "Zombie_outerarm_lower") || TrackIs(trackName, "Zombie_outerarm_upper"))
            {
                return !door;
            }

            if (TrackIs(trackName, "Zombie_duckytube") || TrackIs(trackName, "Zombie_whitewater") || TrackIs(trackName, "Zombie_whitewater2") || TrackIs(trackName, "Zombie_mustache") || TrackIs(trackName, "anim_tongue"))
            {
                return false;
            }

            return true;
        }

        /// <summary>Performs is zombie.</summary>
        /// <param name = "asset">The asset.</param>
        /// <returns>The resulting is zombie.</returns>
        private static bool IsZombie(Reanimation asset)
        {
            return string.Equals(asset.Name, "Zombie", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Performs track is.</summary>
        /// <param name = "actual">The actual.</param>
        /// <param name = "expected">The expected.</param>
        /// <returns>The resulting track is.</returns>
        private static bool TrackIs(string? actual, string? expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Performs is track visible.</summary>
        /// <param name = "track">The immutable parsed track and its cached default visibility.</param>
        /// <param name = "variant">The current ground-zombie variant.</param>
        /// <param name = "equipmentIntact">Whether the variant's equipment is currently intact.</param>
        /// <param name = "custom">An optional live visibility callback overriding the defaults.</param>
        /// <returns>Whether the track is visible under the current caller's rules.</returns>
        private static bool IsTrackVisible(Track track, ZombieVariant variant, bool equipmentIntact, Func<string, bool>? custom)
        {
            if (custom != null)
            {
                return custom(track.Name);
            }

            if (variant < ZombieVariant.Normal || variant > ZombieVariant.ScreenDoor)
            {
                return !track.GroundZombie || IsZombieTrackVisible(track.Name, variant, equipmentIntact);
            }

            var bit = (int)variant * 2 + (equipmentIntact ? 1 : 0);
            return (track.VariantVisibility & (1 << bit)) != 0;
        }

        /// <summary>Performs try get bounds.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "variant">The variant.</param>
        /// <param name = "equipmentIntact">The equipment intact.</param>
        /// <param name = "custom">The custom.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <returns>The resulting try get bounds.</returns>
        private bool TryGetBounds(Reanimation asset, Clip clip, ZombieVariant variant, bool equipmentIntact, Func<string, bool>? custom, out Rect bounds)
        {
            bounds = default(Rect);
            if (custom == null && !IsZombie(asset))
            {
                bounds = clip.Bounds;
                return true;
            }

            // A flag is an attachment, not part of the body-size reference rectangle.
            if (custom == null && variant == ZombieVariant.Flag)
            {
                return TryGetBounds(asset, clip, ZombieVariant.Normal, false, null, out bounds);
            }

            var key = (int)variant * 2 + (equipmentIntact ? 1 : 0);
            if (custom == null && clip.VariantBounds.TryGetValue(key, out bounds))
            {
                return true;
            }

            if (custom == null && (variant == ZombieVariant.Conehead || variant == ZombieVariant.Buckethead || variant == ZombieVariant.ScreenDoor))
            {
                Rect intact, broken;
                if (!TryCalculateBounds(asset, clip, out intact, true, variant, true) || !TryCalculateBounds(asset, clip, out broken, true, variant, false))
                {
                    return Fail("The selected tracks have no drawable images: " + asset.Name);
                }

                float minX = Math.Min(intact.x, broken.x), minY = Math.Min(intact.y, broken.y);
                var maxX = Math.Max(intact.x + intact.width, broken.x + broken.width);
                var maxY = Math.Max(intact.y + intact.height, broken.y + broken.height);
                bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
                clip.VariantBounds[(int)variant * 2] = bounds;
                clip.VariantBounds[(int)variant * 2 + 1] = bounds;
                return true;
            }

            if (!TryCalculateBounds(asset, clip, out bounds, true, variant, equipmentIntact, custom))
            {
                return Fail("The selected tracks have no drawable images: " + asset.Name);
            }

            if (custom == null)
            {
                clip.VariantBounds[key] = bounds;
            }

            return true;
        }

        /// <summary>Performs draw attachment.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "parent">The parent.</param>
        /// <param name = "originalColor">The original color.</param>
        /// <param name = "parentAlpha">The parent alpha.</param>
        /// <returns>The resulting draw attachment.</returns>
        private bool DrawAttachment(Reanimation asset, Clip clip, float seconds, Matrix4x4 parent, Color originalColor, float parentAlpha)
        {
            var timeInFrames = Math.Max(0d, (double)seconds) * asset.Fps % clip.Count;
            var relativeFrame = (int)Math.Floor(timeInFrames);
            var current = clip.Start + relativeFrame;
            var next = clip.Start + ((relativeFrame + 1) % clip.Count);
            var fraction = (float)(timeInFrames - relativeFrame);
            var drawn = false;
            for (var trackIndex = 0; trackIndex < asset.Tracks.Count; trackIndex++)
            {
                var track = asset.Tracks[trackIndex];
                var from = GetFrame(track, current);
                if (from.Visibility < 0f || from.Texture == null)
                {
                    continue;
                }

                var frame = Interpolate(from, GetFrame(track, next), fraction);
                if (frame.Alpha <= 0f)
                {
                    continue;
                }

                using var hierarchySprite = Hierarchy.BeginSprite(asset, trackIndex, track.Name);

                var sprite = parent * PartMatrix(frame);
                if (!HasDrawableArea(sprite))
                {
                    continue;
                }

                SpriteMatrix = sprite;
                SpriteDrawState.Color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * Math.Max(0f, Math.Min(1f, parentAlpha * frame.Alpha)));
                DrawTexturePart(from.Texture, from.Texture.Width, from.Texture.Height);
                drawn = true;
            }

            return drawn;
        }

        /// <summary>Performs try prepare attached plant.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "bodyAnimation">The body animation.</param>
        /// <param name = "headAnimation">The head animation.</param>
        /// <param name = "attachmentTrack">The attachment track.</param>
        /// <param name = "asset">The asset.</param>
        /// <param name = "body">The body.</param>
        /// <param name = "head">The head.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "inverseBase">The inverse base.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <returns>The resulting try prepare attached plant.</returns>
        private bool TryPrepareAttachedPlant(string? name, string bodyAnimation, string headAnimation, string attachmentTrack, [NotNullWhen(true)] out Reanimation? asset, [NotNullWhen(true)] out Clip? body, [NotNullWhen(true)] out Clip? head, [NotNullWhen(true)] out Track? anchor, out Matrix4x4 inverseBase, out Rect bounds)
        {
            asset = null;
            body = null;
            head = null;
            anchor = null;
            inverseBase = Matrix4x4.identity;
            bounds = default(Rect);
            if (!TryGetClip(name, bodyAnimation, out asset, out body))
            {
                return false;
            }

            Reanimation? sameAsset;
            if (!TryGetClip(name, headAnimation, out sameAsset, out head))
            {
                return false;
            }

            anchor = FindTrack(asset, attachmentTrack);
            if (anchor == null && TrackIs(attachmentTrack, "anim_stem"))
            {
                anchor = FindTrack(asset, "anim_idle");
            }

            if (anchor == null)
            {
                return Fail("Attachment track not found: " + attachmentTrack + " in " + name);
            }

            if (!TryInverseAffine(PartMatrix(GetFrame(anchor, body.Start)), out inverseBase))
            {
                return Fail("The attachment base pose is singular: " + anchor.Name);
            }

            return TryGetAttachedPlantBounds(asset, body, head, anchor, inverseBase, out bounds);
        }

        /// <summary>Performs try get attached plant bounds.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "body">The body.</param>
        /// <param name = "head">The head.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "inverseBase">The inverse base.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <returns>The resulting try get attached plant bounds.</returns>
        private bool TryGetAttachedPlantBounds(Reanimation asset, Clip body, Clip head, Track anchor, Matrix4x4 inverseBase, out Rect bounds)
        {
            bounds = default(Rect);
            Clip? idle = null, shooting = null;
            Reanimation? ignored;
            if (FindTrack(asset, "anim_head_idle") != null && !TryGetClip(asset.Name, "anim_head_idle", out ignored, out idle))
            {
                return false;
            }

            if (FindTrack(asset, "anim_shooting") != null && !TryGetClip(asset.Name, "anim_shooting", out ignored, out shooting))
            {
                return false;
            }

            var groupedHead = ReferenceEquals(head, idle) || ReferenceEquals(head, shooting) ? idle ?? shooting : head;
            var key = (body, groupedHead, anchor);
            if (asset.AttachedBounds.TryGetValue(key, out bounds))
            {
                return true;
            }

            var headBounds = head.Bounds;
            if (idle != null)
            {
                headBounds = UnionBounds(headBounds, idle.Bounds);
            }

            if (shooting != null)
            {
                headBounds = UnionBounds(headBounds, shooting.Bounds);
            }

            float minX = body.Bounds.x, minY = body.Bounds.y;
            float maxX = body.Bounds.x + body.Bounds.width, maxY = body.Bounds.y + body.Bounds.height;
            for (var i = 0; i < body.Count; i++)
            {
                var from = GetFrame(anchor, body.Start + i);
                var to = GetFrame(anchor, body.Start + ((i + 1) % body.Count));
                for (var sample = 0; sample < 8; sample++)
                {
                    var overlay = PartMatrix(Interpolate(from, to, sample / 8f)) * inverseBase;
                    IncludeRectBounds(overlay, headBounds, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            if (!IsFinite(minX) || !IsFinite(minY) || !IsFinite(maxX) || !IsFinite(maxY) || maxX <= minX || maxY <= minY)
            {
                return Fail("Could not calculate attached plant bounds: " + asset.Name);
            }

            var padding = 1f + Math.Max(maxX - minX, maxY - minY) * 0.02f;
            bounds = new Rect(minX - padding, minY - padding, maxX - minX + 2f * padding, maxY - minY + 2f * padding);
            asset.AttachedBounds[key] = bounds;
            return true;
        }

        /// <summary>Performs union bounds.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting union bounds.</returns>
        private static Rect UnionBounds(Rect a, Rect b)
        {
            float x = Math.Min(a.x, b.x), y = Math.Min(a.y, b.y);
            var right = Math.Max(a.x + a.width, b.x + b.width);
            var bottom = Math.Max(a.y + a.height, b.y + b.height);
            return new Rect(x, y, right - x, bottom - y);
        }

        /// <summary>Releases loaded textures. Call from the Unity main thread.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _assets?.Dispose();
            _assets = null;

            foreach (NativeSurface surface in _nativeSurfaces.Values)
            {
                DestroyNativeSurface(surface);
            }

            foreach (var surface in _inactiveNativeSurfaces)
            {
                DestroyNativeSurface(surface);
            }

            _nativeSurfaces.Clear();
            _inactiveNativeSurfaces.Clear();
            _inactiveNativePixels = 0;
            foreach (var renderer in _nativeCaptureRenderers)
            {
                renderer.Dispose();
            }

            _nativeCaptureRenderers.Clear();
            if (_nativePortraitMaterial != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_nativePortraitMaterial);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_nativePortraitMaterial);
                }
            }

            _objectRenderer?.Dispose();
            Hierarchy.Dispose();
            _texturesById.Clear();
            _animations.Clear();
            _nativePoseBlends.Clear();
            _inactiveNativePoseBlends.Clear();
            _expiredNativePoseBlends.Clear();
            _headInstanceKeys.Clear();
            _expiredHeadInstanceKeys.Clear();
            _particleKeys.Clear();
            _disposed = true;
        }

        /// <summary>Finds a named track without allocating a predicate or scanning the animation.</summary>
        /// <param name="asset">The animation owning the indexed tracks.</param>
        /// <param name="name">The original case-insensitive track name.</param>
        /// <returns>The first matching track, or null when no track has that name.</returns>
        private static Track? FindTrack(Reanimation asset, string? name)
        {
            if (name != null && asset.TracksByName.TryGetValue(name, out var track))
            {
                return track;
            }

            return null;
        }

        /// <summary>Performs try get clip.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting try get clip.</returns>
        private bool TryGetClip(string? name, string? animation, [NotNullWhen(true)] out Reanimation? asset, [NotNullWhen(true)] out Clip? clip)
        {
            asset = null;
            clip = null;
            if (_disposed)
            {
                return Fail("The renderer has been disposed.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail("An animation file name is required.");
            }

            try
            {
                EnsureIndex();
                var key = name.EndsWith(".reanim", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - ".reanim".Length) : name;
                if (!_animations.TryGetValue(key, out asset))
                {
                    string path;
                    if (!_animationPaths.TryGetValue(key, out path))
                    {
                        return Fail("Reanimation not found: " + name);
                    }

                    asset = LoadReanimation(path, key);
                    _animations.Add(key, asset);
                }

                var useDefault = string.IsNullOrWhiteSpace(animation);
                var requestKey = animation == null || useDefault ? string.Empty : animation;
                if (asset.RequestedClips.TryGetValue(requestKey, out clip))
                {
                    return true;
                }

                var clipName = animation == null || useDefault ? DefaultAnimation(asset.Name) : animation.Trim();
                if (!string.IsNullOrEmpty(clipName) && !clipName.StartsWith("anim_", StringComparison.OrdinalIgnoreCase) && !clipName.StartsWith("_", StringComparison.Ordinal))
                {
                    clipName = "anim_" + clipName;
                }

                Track? marker = null;
                if (!string.IsNullOrEmpty(clipName))
                {
                    marker = FindTrack(asset, clipName);
                }

                if (marker == null && !useDefault)
                {
                    return Fail("Animation not found: " + animation + " in " + name);
                }

                if (marker == null && useDefault)
                {
                    marker = FindTrack(asset, "anim_idle");
                    if (marker == null)
                    {
                        marker = asset.Tracks.Find(track => track.Name.StartsWith("anim_", StringComparison.OrdinalIgnoreCase));
                    }
                }

                var cacheKey = marker == null ? "" : marker.Name;
                if (asset.Clips.TryGetValue(cacheKey, out clip))
                {
                    asset.RequestedClips[requestKey] = clip;
                    return true;
                }

                var start = 0;
                var end = asset.FrameCount;
                if (marker != null)
                {
                    while (start < marker.Frames.Length && marker.Frames[start].Visibility < 0f)
                    {
                        start++;
                    }

                    end = start;
                    while (end < marker.Frames.Length && marker.Frames[end].Visibility >= 0f)
                    {
                        end++;
                    }
                }

                if (end <= start)
                {
                    return Fail("The animation has no visible frames: " + cacheKey);
                }

                clip = new Clip
                {
                    Start = start,
                    Count = end - start
                };
                if (!TryCalculateBounds(asset, clip, out clip.Bounds))
                {
                    return Fail("The animation has no drawable image tracks: " + name + " / " + cacheKey);
                }

                asset.Clips.Add(cacheKey, clip);
                asset.RequestedClips[requestKey] = clip;
                return true;
            }
            catch (Exception error)when (error is IOException || error is UnauthorizedAccessException || error is XmlException || error is FormatException || error is ArgumentException || error is InvalidOperationException)
            {
                return Fail("Could not load " + name + ": " + error.Message);
            }
        }

        /// <summary>Performs default animation.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting default animation.</returns>
        private static string DefaultAnimation(string name)
        {
            if (IsPeaShooter(name))
            {
                return "anim_full_idle";
            }

            if (string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase))
            {
                return "anim_walk";
            }

            if (string.Equals(name, "LawnMower", StringComparison.OrdinalIgnoreCase))
            {
                return "anim_normal";
            }

            return "anim_idle";
        }

        /// <summary>Performs is pea shooter.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting is pea shooter.</returns>
        private static bool IsPeaShooter(string? name)
        {
            if (name == null)
            {
                return false;
            }

            if (name.EndsWith(".reanim", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - ".reanim".Length);
            }

            return TrackIs(name, "PeaShooter") || TrackIs(name, "PeaShooterSingle") || TrackIs(name, "SnowPea") || TrackIs(name, "GatlingPea");
        }

        /// <summary>Performs is animation.</summary>
        /// <param name = "actual">The actual.</param>
        /// <param name = "expected">The expected.</param>
        /// <returns>The resulting is animation.</returns>
        private static bool IsAnimation(string? actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual))
            {
                return false;
            }

            actual = actual.Trim();
            if (!actual.StartsWith("anim_", StringComparison.OrdinalIgnoreCase))
            {
                actual = "anim_" + actual;
            }

            return TrackIs(actual, expected);
        }

        /// <summary>Performs is valid destination.</summary>
        /// <param name = "destination">The destination.</param>
        /// <returns>The resulting is valid destination.</returns>
        private static bool IsValidDestination(Rect destination)
        {
            return IsFinite(destination.x) && IsFinite(destination.y) && IsFinite(destination.width) && IsFinite(destination.height) && destination.width > 0f && destination.height > 0f;
        }

        /// <summary>Performs layout matrix.</summary>
        /// <param name = "destination">The destination.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <returns>The resulting layout matrix.</returns>
        private static Matrix4x4 LayoutMatrix(Rect destination, Rect bounds)
        {
            var scale = Math.Min(destination.width / bounds.width, destination.height / bounds.height);
            var layout = Matrix4x4.identity;
            layout.m00 = scale;
            layout.m11 = scale;
            layout.m03 = destination.x + (destination.width - bounds.width * scale) * 0.5f - bounds.x * scale;
            layout.m13 = destination.y + (destination.height - bounds.height * scale) * 0.5f - bounds.y * scale;
            return layout;
        }

        /// <summary>Performs sample frame.</summary>
        /// <param name = "clip">The clip.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "fps">The fps.</param>
        /// <param name = "loop">The loop.</param>
        /// <returns>The resulting sample frame.</returns>
        private static FrameSample SampleFrame(Clip clip, float seconds, float fps, bool loop)
        {
            var time = Math.Max(0d, (double)seconds) * fps;
            time = loop ? time % clip.Count : Math.Min(time, clip.Count - 1d);
            var index = (int)Math.Floor(time);
            return new FrameSample
            {
                Current = clip.Start + index,
                Next = clip.Start + (index + 1 < clip.Count ? index + 1 : (loop ? 0 : index)),
                Fraction = (float)(time - index)
            };
        }

        /// <summary>Performs draw clip pose.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "fps">The fps.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "parent">The parent.</param>
        /// <param name = "tint">The tint.</param>
        /// <param name = "overrideTrack">The override track.</param>
        /// <param name = "imageOverride">The image override.</param>
        /// <param name = "trackVisible">The track visible.</param>
        /// <param name = "blend">The blend.</param>
        /// <returns>The resulting draw clip pose.</returns>
        private bool DrawClipPose(Reanimation asset, Clip? clip, float seconds, float fps, bool loop, Matrix4x4 parent, Color tint, string? overrideTrack = null, SpriteImage? imageOverride = null, Func<string, bool>? trackVisible = null, NativePoseBlend? blend = null)
        {
            using var profile = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Animation);
            if (clip == null)
            {
                throw new System.ArgumentNullException(nameof(clip));
            }

            var time = SampleFrame(clip, seconds, fps, loop);
            var drawn = false;
            var trackGroups = NativeZombieTrackGroups(asset);
            for (var group = 0; group < trackGroups.Length; group++)
            {
                var indices = trackGroups[group];
                for (var position = 0; position < indices.Length; position++)
                {
                    var trackIndex = indices[position];
                    var track = asset.Tracks[trackIndex];
                    if (trackVisible != null && !trackVisible(track.Name) || !NativeZombieTrackVisible(asset, track.Name))
                    {
                        continue;
                    }

                    var from = GetFrame(track, time.Current);
                    if (from.Visibility < 0f || from.Texture == null)
                    {
                        continue;
                    }

                    var frame = BlendNativePose(blend, track.Name, Interpolate(from, GetFrame(track, time.Next), time.Fraction));
                    if (frame.Alpha <= 0f)
                    {
                        continue;
                    }

                    using var hierarchySprite = Hierarchy.BeginSprite(asset, trackIndex, track.Name);

                    var sprite = NativeZombieTrackMatrix(parent * PartMatrix(frame), group);
                    if (!HasDrawableArea(sprite))
                    {
                        continue;
                    }

                    SpriteMatrix = sprite;
                    var alpha = tint.a * Math.Max(0f, Math.Min(1f, frame.Alpha));
                    SpriteDrawState.Color = _nativeCombatZombie == null ? new Color(tint.r, tint.g, tint.b, alpha) : NativeZombieTrackColor(asset, track.Name, group, alpha);
                    var image = imageOverride != null && TrackIs(track.Name, overrideTrack) ? imageOverride : from.Texture;
                    var nativeImage = NativeZombieImage(asset, track.Name, image);
                    var substituted = imageOverride != null && TrackIs(track.Name, overrideTrack);
                    DrawTexturePart(nativeImage, nativeImage == image && !substituted ? from.Texture.Width : nativeImage.Width, nativeImage == image && !substituted ? from.Texture.Height : nativeImage.Height);
                    drawn = true;
                }
            }

            return drawn;
        }

        /// <summary>Performs has drawable area.</summary>
        /// <param name = "matrix">The matrix.</param>
        /// <returns>The resulting has drawable area.</returns>
        private static bool HasDrawableArea(Matrix4x4 matrix)
        {
            var area = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
            return IsFinite(area) && area != 0f;
        }

        /// <summary>Performs try inverse affine.</summary>
        /// <param name = "matrix">The matrix.</param>
        /// <param name = "inverse">The inverse.</param>
        /// <returns>The resulting try inverse affine.</returns>
        private static bool TryInverseAffine(Matrix4x4 matrix, out Matrix4x4 inverse)
        {
            inverse = Matrix4x4.identity;
            var determinant = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
            if (!IsFinite(determinant) || Math.Abs(determinant) < 0.000001f)
            {
                return false;
            }

            inverse.m00 = matrix.m11 / determinant;
            inverse.m01 = -matrix.m01 / determinant;
            inverse.m10 = -matrix.m10 / determinant;
            inverse.m11 = matrix.m00 / determinant;
            inverse.m03 = -(inverse.m00 * matrix.m03 + inverse.m01 * matrix.m13);
            inverse.m13 = -(inverse.m10 * matrix.m03 + inverse.m11 * matrix.m13);
            return true;
        }

        /// <summary>Performs ensure index.</summary>
        /// <exception cref = "DirectoryNotFoundException">The operation cannot satisfy its preconditions.</exception>
        private void EnsureIndex()
        {
            if (_indexed)
            {
                return;
            }

            foreach (var path in Assets.EnumerateTextPaths("reanim"))
            {
                if (string.Equals(Path.GetExtension(path), ".reanim", StringComparison.OrdinalIgnoreCase))
                {
                    _animationPaths[Path.GetFileNameWithoutExtension(path)] = path;
                }
            }

            _indexed = true;
        }

        /// <summary>Performs load reanimation.</summary>
        /// <param name = "path">The path.</param>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting load reanimation.</returns>
        /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
        private Reanimation LoadReanimation(string path, string name)
        {
            // The original files are XML fragments with sibling fps and track nodes.
            if (!Assets.TryReadText(path, out var contents))
            {
                throw new FileNotFoundException("Missing reanimation: " + path);
            }
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            var document = new XmlDocument
            {
                XmlResolver = null
            };
            using (var input = new StringReader("<reanim>" + contents + "</reanim>"))
            {
                using (var reader = XmlReader.Create(input, settings))
                {
                    document.Load(reader);
                }
            }

            var fpsNode = document.SelectSingleNode("/reanim/fps");
            if (fpsNode == null)
            {
                throw new FormatException("Missing fps.");
            }

            var asset = new Reanimation
            {
                Name = name,
                Fps = ParseNumber(fpsNode.InnerText)
            };
            if (asset.Fps <= 0f)
            {
                throw new FormatException("The fps must be positive.");
            }

            foreach (XmlNode node in document.SelectNodes("/reanim/track"))
            {
                var trackName = node.SelectSingleNode("name");
                if (trackName == null)
                {
                    throw new FormatException("A track is missing its name.");
                }

                var frames = new List<Frame>();
                var state = new Frame
                {
                    ScaleX = 1f,
                    ScaleY = 1f,
                    Alpha = 1f
                };
                foreach (XmlNode frameNode in node.SelectNodes("t"))
                {
                    foreach (XmlNode value in frameNode.ChildNodes)
                    {
                        if (value.NodeType != XmlNodeType.Element)
                        {
                            continue;
                        }

                        switch (value.Name)
                        {
                            case "x":
                                state.X = ParseNumber(value.InnerText);
                                break;
                            case "y":
                                state.Y = ParseNumber(value.InnerText);
                                break;
                            case "kx":
                                state.Kx = ParseNumber(value.InnerText);
                                break;
                            case "ky":
                                state.Ky = ParseNumber(value.InnerText);
                                break;
                            case "sx":
                                state.ScaleX = ParseNumber(value.InnerText);
                                break;
                            case "sy":
                                state.ScaleY = ParseNumber(value.InnerText);
                                break;
                            case "a":
                                state.Alpha = ParseNumber(value.InnerText);
                                break;
                            case "f":
                                state.Visibility = ParseNumber(value.InnerText);
                                break;
                            case "i":
                                state.Texture = string.IsNullOrEmpty(value.InnerText) ? null : LoadTexture(value.InnerText);
                                break;
                        }
                    }

                    frames.Add(state);
                }

                if (frames.Count == 0)
                {
                    continue;
                }

                var track = new Track { Name = trackName.InnerText, Frames = frames.ToArray() };
                asset.Tracks.Add(track);
                if (!asset.TracksByName.ContainsKey(track.Name))
                {
                    asset.TracksByName.Add(track.Name, track);
                }
                asset.FrameCount = Math.Max(asset.FrameCount, frames.Count);
            }

            if (asset.FrameCount == 0)
            {
                throw new FormatException("No animation frames were found.");
            }

            CacheTrackRules(asset);
            return asset;
        }

        /// <summary>Caches the immutable track classification while retaining every original source index.</summary>
        /// <param name="asset">The completely parsed animation whose track definitions will not change.</param>
        private static void CacheTrackRules(Reanimation asset)
        {
            var zombie = IsZombie(asset);
            for (var trackIndex = 0; trackIndex < asset.Tracks.Count; trackIndex++)
            {
                var track = asset.Tracks[trackIndex];
                track.GroundZombie = zombie;
                for (var variant = ZombieVariant.Normal; variant <= ZombieVariant.ScreenDoor; variant++)
                {
                    for (var intact = 0; intact < 2; intact++)
                    {
                        if (!zombie || IsZombieTrackVisible(track.Name, variant, intact != 0))
                        {
                            track.VariantVisibility |= 1 << ((int)variant * 2 + intact);
                        }
                    }
                }
            }

            asset.NativeTrackGroups = new int[4][][];
            for (var shield = 0; shield < asset.NativeTrackGroups.Length; shield++)
            {
                var groupCount = shield == 0 ? 1 : 3;
                var groups = new int[groupCount][];
                var counts = new int[groupCount];
                for (var trackIndex = 0; trackIndex < asset.Tracks.Count; trackIndex++)
                {
                    counts[AdventureZombieFeedbackRules.RenderGroup(shield, asset.Tracks[trackIndex].Name)]++;
                }

                for (var group = 0; group < groups.Length; group++)
                {
                    groups[group] = new int[counts[group]];
                    counts[group] = 0;
                }

                for (var trackIndex = 0; trackIndex < asset.Tracks.Count; trackIndex++)
                {
                    var group = AdventureZombieFeedbackRules.RenderGroup(shield, asset.Tracks[trackIndex].Name);
                    groups[group][counts[group]++] = trackIndex;
                }

                asset.NativeTrackGroups[shield] = groups;
            }
        }

        /// <summary>Performs load texture.</summary>
        /// <param name = "imageId">The image id.</param>
        /// <returns>The resulting load texture.</returns>
        /// <exception cref = "FileNotFoundException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
        private SpriteImage LoadTexture(string imageId)
        {
            if (!_texturesById.TryGetValue(imageId, out var image))
            {
                image = Assets.LoadImage(imageId);
                _texturesById.Add(imageId, image);
            }

            return image;
        }

        /// <summary>Performs parse number.</summary>
        /// <param name = "text">The text.</param>
        /// <returns>The resulting parse number.</returns>
        /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
        private static float ParseNumber(string text)
        {
            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !IsFinite(value))
            {
                throw new FormatException("Invalid reanimation number: " + text);
            }

            return value;
        }

        /// <summary>Performs is finite.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting is finite.</returns>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>Performs get frame.</summary>
        /// <param name = "track">The track.</param>
        /// <param name = "frame">The frame.</param>
        /// <returns>The resulting get frame.</returns>
        private static Frame GetFrame(Track track, int frame)
        {
            return track.Frames[Math.Min(frame, track.Frames.Length - 1)];
        }

        /// <summary>Performs interpolate.</summary>
        /// <param name = "from">The from.</param>
        /// <param name = "to">The to.</param>
        /// <param name = "t">The t.</param>
        /// <returns>The resulting interpolate.</returns>
        private static Frame Interpolate(Frame from, Frame to, float t)
        {
            // The exporter already unwraps angles; do not use shortest-path angle interpolation.
            return new Frame
            {
                X = Lerp(from.X, to.X, t),
                Y = Lerp(from.Y, to.Y, t),
                Kx = Lerp(from.Kx, to.Kx, t),
                Ky = Lerp(from.Ky, to.Ky, t),
                ScaleX = Lerp(from.ScaleX, to.ScaleX, t),
                ScaleY = Lerp(from.ScaleY, to.ScaleY, t),
                Alpha = Lerp(from.Alpha, to.Alpha, t),
                Visibility = from.Visibility,
                Texture = from.Texture
            };
        }

        /// <summary>Blends one pose angle using the local PC executable's cross-animation rule.</summary>
        /// <param name="previous">The angle retained when the previous body animation was interrupted.</param>
        /// <param name="current">The angle sampled from the new body animation.</param>
        /// <param name="fraction">The completed fraction of the original pose blend.</param>
        /// <returns>The new angle directly when the difference exceeds 180 degrees, otherwise the linear blend.</returns>
        internal static float BlendNativeAngle(float previous, float current, float fraction)
        {
            // PC BlendTransform 0x481180 replaces the old angle with the new angle outside +/-180.
            // Authored keyframes remain unwrapped and use Interpolate's ordinary linear sampling.
            return Math.Abs(previous - current) > 180f ? current : Lerp(previous, current, fraction);
        }

        /// <summary>Performs lerp.</summary>
        /// <param name = "from">The from.</param>
        /// <param name = "to">The to.</param>
        /// <param name = "t">The t.</param>
        /// <returns>The resulting lerp.</returns>
        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        /// <summary>Performs part matrix.</summary>
        /// <param name = "frame">The frame.</param>
        /// <returns>The resulting part matrix.</returns>
        private static Matrix4x4 PartMatrix(Frame frame)
        {
            var x = frame.Kx * (Math.PI / 180d);
            var y = frame.Ky * (Math.PI / 180d);
            var matrix = Matrix4x4.identity;
            matrix.m00 = frame.ScaleX * (float)Math.Cos(x);
            matrix.m10 = frame.ScaleX * (float)Math.Sin(x);
            matrix.m01 = -frame.ScaleY * (float)Math.Sin(y);
            matrix.m11 = frame.ScaleY * (float)Math.Cos(y);
            matrix.m03 = frame.X;
            matrix.m13 = frame.Y;
            return matrix;
        }

        /// <summary>Performs try calculate bounds.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "bounds">The bounds.</param>
        /// <param name = "filterTracks">The filter tracks.</param>
        /// <param name = "variant">The variant.</param>
        /// <param name = "equipmentIntact">The equipment intact.</param>
        /// <param name = "custom">The custom.</param>
        /// <returns>The resulting try calculate bounds.</returns>
        private static bool TryCalculateBounds(Reanimation asset, Clip clip, out Rect bounds, bool filterTracks = false, ZombieVariant variant = ZombieVariant.Normal, bool equipmentIntact = false, Func<string, bool>? custom = null)
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (Track track in asset.Tracks)
            {
                if (filterTracks && !IsTrackVisible(track, variant, equipmentIntact, custom))
                {
                    continue;
                }

                for (var i = 0; i < clip.Count; i++)
                {
                    var from = GetFrame(track, clip.Start + i);
                    if (from.Visibility < 0f || from.Texture == null)
                    {
                        continue;
                    }

                    var to = GetFrame(track, clip.Start + ((i + 1) % clip.Count));
                    // Include interpolated poses, including the loop transition, for stable bounds.
                    for (var sample = 0; sample < 8; sample++)
                    {
                        var m = PartMatrix(Interpolate(from, to, sample / 8f));
                        IncludeTextureBounds(m, from.Texture, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
            }

            if (!IsFinite(minX) || !IsFinite(minY) || maxX <= minX || maxY <= minY)
            {
                bounds = default(Rect);
                return false;
            }

            var padding = 1f + Math.Max(maxX - minX, maxY - minY) * 0.02f;
            bounds = new Rect(minX - padding, minY - padding, maxX - minX + 2f * padding, maxY - minY + 2f * padding);
            return true;
        }

        /// <summary>Performs include texture bounds.</summary>
        /// <param name = "m">The m.</param>
        /// <param name = "texture">The texture.</param>
        /// <param name = "minX">The min x.</param>
        /// <param name = "minY">The min y.</param>
        /// <param name = "maxX">The max x.</param>
        /// <param name = "maxY">The max y.</param>
        private static void IncludeTextureBounds(Matrix4x4 m, SpriteImage texture, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float width = texture.Width, height = texture.Height;
            IncludePoint(m.m03, m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m00 * width, m.m13 + m.m10 * width, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m01 * height, m.m13 + m.m11 * height, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m03 + m.m00 * width + m.m01 * height, m.m13 + m.m10 * width + m.m11 * height, ref minX, ref minY, ref maxX, ref maxY);
        }

        /// <summary>Performs include rect bounds.</summary>
        /// <param name = "m">The m.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "minX">The min x.</param>
        /// <param name = "minY">The min y.</param>
        /// <param name = "maxX">The max x.</param>
        /// <param name = "maxY">The max y.</param>
        private static void IncludeRectBounds(Matrix4x4 m, Rect rect, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float x = rect.x, y = rect.y, right = x + rect.width, bottom = y + rect.height;
            IncludePoint(m.m00 * x + m.m01 * y + m.m03, m.m10 * x + m.m11 * y + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * right + m.m01 * y + m.m03, m.m10 * right + m.m11 * y + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * x + m.m01 * bottom + m.m03, m.m10 * x + m.m11 * bottom + m.m13, ref minX, ref minY, ref maxX, ref maxY);
            IncludePoint(m.m00 * right + m.m01 * bottom + m.m03, m.m10 * right + m.m11 * bottom + m.m13, ref minX, ref minY, ref maxX, ref maxY);
        }

        /// <summary>Performs include point.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "minX">The min x.</param>
        /// <param name = "minY">The min y.</param>
        /// <param name = "maxX">The max x.</param>
        /// <param name = "maxY">The max y.</param>
        private static void IncludePoint(float x, float y, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        /// <summary>Performs fail.</summary>
        /// <param name = "message">The message.</param>
        /// <returns>The resulting fail.</returns>
        private bool Fail(string message)
        {
            LastError = message;
            return false;
        }

        /// <summary>Stores native pose blends.</summary>
        private readonly Dictionary<string, NativePoseBlend> _nativePoseBlends = new Dictionary<string, NativePoseBlend>();
        /// <summary>Collects unseen blend identities before retiring them from the live dictionary.</summary>
        private readonly List<string> _expiredNativePoseBlends = new List<string>();
        /// <summary>Reuses a bounded number of cleared per-track blend dictionaries for newly visible actors.</summary>
        private readonly Stack<NativePoseBlend> _inactiveNativePoseBlends = new Stack<NativePoseBlend>();

        /// <summary>Retires actor-specific state omitted from the completed outer presentation frame.</summary>
        private void RetireUnusedNativePlayback()
        {
            _expiredNativePoseBlends.Clear();
            foreach (var pair in _nativePoseBlends)
            {
                if (pair.Value.LastSeenFrame != _nativeBlendFrame)
                {
                    _expiredNativePoseBlends.Add(pair.Key);
                }
            }

            foreach (var key in _expiredNativePoseBlends)
            {
                ReturnNativePoseBlend(_nativePoseBlends[key]);
                _nativePoseBlends.Remove(key);
            }

            _expiredNativePoseBlends.Clear();
            _expiredHeadInstanceKeys.Clear();
            foreach (var pair in _headInstanceKeys)
            {
                if (!_nativePoseBlends.ContainsKey(pair.Value))
                {
                    _expiredHeadInstanceKeys.Add(pair.Key);
                }
            }

            foreach (var key in _expiredHeadInstanceKeys)
            {
                _headInstanceKeys.Remove(key);
            }

            _expiredHeadInstanceKeys.Clear();
        }

        /// <summary>Clears retired track poses before retaining their bounded dictionary capacity.</summary>
        /// <param name="blend">The actor blend no longer referenced by the current presentation.</param>
        private void ReturnNativePoseBlend(NativePoseBlend blend)
        {
            blend.From.Clear();
            blend.Current.Clear();
            if (_inactiveNativePoseBlends.Count < MaximumRetainedPoseBlends)
            {
                _inactiveNativePoseBlends.Push(blend);
            }
        }

        /// <summary>Gets the stable blend identity for a retained attached head.</summary>
        /// <param name="instance">The owning actor's stable presentation identity.</param>
        /// <param name="head">The head's index in the actor's attachment list.</param>
        /// <returns>The cached head identity, or null when blending has no actor identity.</returns>
        private string? GetHeadInstanceKey(string? instance, int head)
        {
            if (instance == null)
            {
                return null;
            }

            var identity = (instance, head);
            if (!_headInstanceKeys.TryGetValue(identity, out var key))
            {
                key = instance + "-head-" + head;
                _headInstanceKeys.Add(identity, key);
            }

            return key;
        }

        /// <summary>Performs prepare native blend.</summary>
        /// <param name = "key">The key.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "ticks">The ticks.</param>
        /// <returns>The resulting prepare native blend.</returns>
        private NativePoseBlend? PrepareNativeBlend(string? key, string? animation, float seconds, int ticks)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            NativePoseBlend blend;
            if (!_nativePoseBlends.TryGetValue(key, out blend))
            {
                blend = _inactiveNativePoseBlends.Count > 0 ? _inactiveNativePoseBlends.Pop() : new NativePoseBlend();
                blend.Animation = animation;
                blend.StartedAt = seconds;
                blend.Fraction = 1f;
                _nativePoseBlends.Add(key, blend);
            }
            else if (!string.Equals(blend.Animation, animation, StringComparison.OrdinalIgnoreCase) || seconds < blend.LastSeconds)
            {
                // PC PlayReanim 0x4830B0 starts a fresh blend even when the named clip is unchanged.
                // Normal loops wrap only in NativeSeconds, after this unwrapped playback clock is inspected.
                blend.From.Clear();
                foreach (var pose in blend.Current)
                {
                    blend.From.Add(pose.Key, pose.Value);
                }
                blend.Animation = animation;
                blend.StartedAt = seconds;
            }

            blend.Fraction = ticks <= 0 || blend.From.Count == 0 ? 1f : Mathf.Clamp01((seconds - blend.StartedAt) * 100f / ticks);
            blend.LastSeconds = seconds;
            blend.LastSeenFrame = _nativeBlendFrame;
            return blend;
        }

        // Additive overload preserves the existing Func<string,bool> signature used by loader/UI.
        /// <summary>Performs draw native.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "loop">The loop.</param>
        /// <returns>The resulting draw native.</returns>
        public bool DrawNative(string name, Vector2 origin, float scale, float seconds, string animation, bool loop)
        {
            using var objectScope = BeginObjectScope();
            return DrawNativePlayback(name, origin, scale, seconds, animation, loop);
        }

        /// <summary>Performs combat placement.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "facingLeft">The facing left.</param>
        /// <param name = "mirrorWidth">The mirror width.</param>
        /// <returns>The resulting combat placement.</returns>
        private static Matrix4x4 CombatPlacement(Vector2 origin, float scale, bool facingLeft, float mirrorWidth = 80f)
        {
            var placement = NativeMatrix(origin, scale);
            if (facingLeft)
            {
                placement.m00 = -scale;
                placement.m03 += mirrorWidth * scale;
            }

            return placement;
        }

        /// <summary>Performs try get native animation info.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "info">The info.</param>
        /// <returns>The resulting try get native animation info.</returns>
        public bool TryGetNativeAnimationInfo(string name, string? animation, out NativeAnimationInfo info)
        {
            info = default(NativeAnimationInfo);
            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            var ground = FindTrack(asset, "_ground");
            info = new NativeAnimationInfo
            {
                Start = clip.Start,
                Count = clip.Count,
                SourceRate = asset.Fps,
                GroundDistance = ground == null ? 0f : GetFrame(ground, clip.Start + clip.Count - 1).X - GetFrame(ground, clip.Start).X,
                Bounds = clip.Bounds
            };
            return true;
        }

        // PC reanimation normal Loop advances normalized time by rate / count, and samples
        // start + (count - 1) * time. It does not interpolate the last frame back into the first.
        // Keep portrait APIs unchanged and encode that exact native sample into DrawInternal.
        /// <summary>Performs native seconds.</summary>
        /// <param name = "clip">The clip.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "sourceRate">The source rate.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name="fullLastFrame">Whether sampling includes the full final source frame.</param>
        /// <returns>The resulting native seconds.</returns>
        private static float NativeSeconds(Clip clip, float seconds, float rate, float sourceRate, bool loop, bool fullLastFrame = false)
        {
            var phase = Math.Max(0d, seconds) * Math.Max(0d, rate) / clip.Count;
            phase = loop ? phase - Math.Floor(phase) : Math.Min(1d, phase);
            return (float)(Math.Min(clip.Count - 1, phase * (fullLastFrame ? clip.Count : clip.Count - 1)) / sourceRate);
        }

        /// <summary>Performs draw native playback.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "framesPerSecond">The frames per second.</param>
        /// <param name = "visible">The visible.</param>
        /// <param name = "overrideTrack">The override track.</param>
        /// <param name = "overrideImage">The override image.</param>
        /// <param name = "facingLeft">The facing left.</param>
        /// <param name = "instanceKey">The instance key.</param>
        /// <param name = "blendTicks">The blend ticks.</param>
        /// <param name = "verticalScale">The vertical scale.</param>
        /// <param name = "mirrorWidth">The mirror width.</param>
        /// <param name="crazyDaveHeldItem">The item attached to Dave's hand, if any.</param>
        /// <param name="crazyDaveHeldItemAge">The elapsed time of the held item's animation.</param>
        /// <param name="trackOffset">Optional native pixel offsets applied before each track's placement and scaling.</param>
        /// <param name="trackAttachment">Optional original particles attached after each sampled and blended body track.</param>
        /// <param name="fullLastFrame">Whether the native loop displays the full final source frame.</param>
        /// <returns>The resulting draw native playback.</returns>
        public bool DrawNativePlayback(string? name, Vector2 origin, float scale, float seconds, string? animation, bool loop, float framesPerSecond = -1f, Func<string, bool>? visible = null, string? overrideTrack = null, string? overrideImage = null, bool facingLeft = false, string? instanceKey = null, int blendTicks = 20, float verticalScale = -1f, float mirrorWidth = 80f, string? crazyDaveHeldItem = null, float crazyDaveHeldItemAge = 0f, Func<string, Vector2>? trackOffset = null, Action<string, Matrix4x4, Matrix4x4, float>? trackAttachment = null, bool fullLastFrame = false)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            var rate = framesPerSecond < 0f ? asset.Fps : framesPerSecond;
            var image = overrideImage == null ? null : LoadTexture(overrideImage);
            var placement = CombatPlacement(origin, scale, facingLeft, mirrorWidth);
            if (verticalScale >= 0f)
            {
                placement.m11 = verticalScale;
            }

            return DrawInternal(name, new Rect(0, 0, 1, 1), NativeSeconds(clip, seconds, rate, asset.Fps, loop, fullLastFrame), animation, false, ZombieVariant.Normal, false, visible, overrideTrack, image, placement, false, PrepareNativeBlend(instanceKey, animation, seconds, blendTicks), crazyDaveHeldItem, crazyDaveHeldItemAge, trackOffset, trackAttachment);
        }

        /// <summary>Draws Dave's original blink attached to the current head pose.</summary>
        /// <param name="origin">Dave's native drawing origin.</param>
        /// <param name="scale">Dave's drawing scale.</param>
        /// <param name="parentAnimation">Dave's current animation.</param>
        /// <param name="parentSeconds">Elapsed time of the current animation.</param>
        /// <param name="parentRate">The current animation rate in frames per second.</param>
        /// <param name="parentLoop">Whether the current animation loops.</param>
        /// <param name="blinkSeconds">Elapsed time of the blink animation.</param>
        /// <returns>Whether the blink was drawn.</returns>
        public bool DrawNativeCrazyDaveBlink(Vector2 origin, float scale, string parentAnimation, float parentSeconds, float parentRate, bool parentLoop, float blinkSeconds)
        {
            using var objectScope = BeginObjectScope();
            if (!TryGetClip("CrazyDave", parentAnimation, out var asset, out var parent)
                || !TryGetClip("CrazyDave", "anim_idle_handing", out _, out var basePose)
                || !TryGetClip("CrazyDave", "anim_blink", out _, out var blink))
            {
                return false;
            }

            var head = FindTrack(asset, "Dave_head");
            if (head == null || !TryInverseAffine(PartMatrix(GetFrame(head, basePose.Start)), out var inverse))
            {
                return false;
            }

            var time = SampleFrame(parent, NativeSeconds(parent, parentSeconds, parentRate, asset.Fps, parentLoop), asset.Fps, false);
            var pose = Interpolate(GetFrame(head, time.Current), GetFrame(head, time.Next), time.Fraction);
            var previous = SpriteMatrix;
            var color = SpriteDrawState.Color;
            try
            {
                var placement = previous * CombatPlacement(origin, scale, false, 80f) * PartMatrix(pose) * inverse;
                // The original blink is a PlayOnceFullLastFrameAndHold copy of the whole CrazyDave definition limited
                // to the anim_blink frames: it samples start + count * time (one source frame per 1/15 s) and shows
                // both the eyelid (Dave_blink) and the squashing Dave_eye, because the parent hides its own eye.
                return DrawClipPose(asset, blink, Math.Max(0f, blinkSeconds) * 15f / asset.Fps, asset.Fps, false,
                    placement, color, trackVisible: CrazyDaveBlinkLayerVisible);
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = color;
            }
        }

        /// <summary>Tests whether a CrazyDave track is visible inside the attached blink reanimation.</summary>
        /// <param name="track">The CrazyDave track name.</param>
        /// <returns>Whether the track is one of the two tracks that are visible in the anim_blink frames.</returns>
        private static bool CrazyDaveBlinkLayerVisible(string track)
        {
            return TrackIs(track, "Dave_blink") || TrackIs(track, "Dave_eye");
        }

        /// <summary>Performs draw zombie playback.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "variant">The variant.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "equipmentIntact">The equipment intact.</param>
        /// <param name = "visible">The visible.</param>
        /// <param name = "instanceKey">The instance key.</param>
        /// <param name = "blendTicks">The blend ticks.</param>
        /// <returns>The resulting draw zombie playback.</returns>
        public bool DrawZombiePlayback(Vector2 origin, float scale, float seconds, ZombieVariant variant, string? animation, bool loop, float rate, bool equipmentIntact = true, Func<string, bool>? visible = null, string? instanceKey = null, int blendTicks = 20)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip("Zombie", animation, out asset, out clip))
            {
                return false;
            }

            return DrawInternal("Zombie", new Rect(0, 0, 1, 1), NativeSeconds(clip, seconds, rate, asset.Fps, loop), animation, false, variant, equipmentIntact, visible, null, null, NativeMatrix(origin, scale), false, PrepareNativeBlend(instanceKey, animation, seconds, blendTicks));
        }

        /// <summary>Performs draw native attached heads.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "bodySeconds">The body seconds.</param>
        /// <param name = "bodyRate">The body rate.</param>
        /// <param name = "heads">The heads.</param>
        /// <param name = "visible">The visible.</param>
        /// <param name = "facingLeft">The facing left.</param>
        /// <param name = "instanceKey">The instance key.</param>
        /// <param name = "blendTicks">The blend ticks.</param>
        /// <returns>The resulting draw native attached heads.</returns>
        public bool DrawNativeAttachedHeads(string? name, Vector2 origin, float scale, float bodySeconds, float bodyRate, HeadPlayback[] heads, Func<string, bool>? visible = null, bool facingLeft = false, string? instanceKey = null, int blendTicks = 20)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? body;
            if (!TryGetClip(name, "anim_idle", out asset, out body))
            {
                return false;
            }

            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            var layout = previous * CombatPlacement(origin, scale, facingLeft);
            var drawn = false;
            try
            {
                var bodySample = NativeSeconds(body, bodySeconds, bodyRate, asset.Fps, true);
                drawn = DrawClipPose(asset, body, bodySample, asset.Fps, false, layout, tint, null, null, visible);
                for (var headIndex = 0; headIndex < heads.Length; headIndex++)
                {
                    var playback = heads[headIndex];
                    using var hierarchyAttachment = Hierarchy.BeginAttachment(headIndex + 1);
                    Clip? head;
                    Reanimation? ignored;
                    Track? anchor;
                    Matrix4x4 inverseBase;
                    Rect bounds;
                    if (!TryPrepareAttachedPlant(name, "anim_idle", playback.Animation, playback.Anchor, out ignored, out body, out head, out anchor, out inverseBase, out bounds))
                    {
                        continue;
                    }

                    var time = SampleFrame(body, bodySample, asset.Fps, false);
                    var attach = PartMatrix(Interpolate(GetFrame(anchor, time.Current), GetFrame(anchor, time.Next), time.Fraction)) * inverseBase;
                    var sample = NativeSeconds(head, playback.Seconds, playback.Rate, asset.Fps, playback.Loop);
                    drawn |= DrawClipPose(asset, head, sample, asset.Fps, false, layout * attach, tint, null, null, visible, PrepareNativeBlend(GetHeadInstanceKey(instanceKey, headIndex), playback.Animation, playback.Seconds, blendTicks));
                }
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }

            return drawn;
        }

        // AddReanimation defaults to the entire definition, not the first anim_ marker.
        // In particular, Zombie_charred starts with an anim_blink marker: choosing that
        // marker silently truncates the original burn to a blink instead of its full one-shot.
        /// <summary>Performs try get native definition clip.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "asset">The asset.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting try get native definition clip.</returns>
        private bool TryGetNativeDefinitionClip(string name, [NotNullWhen(true)] out Reanimation? asset, [NotNullWhen(true)] out Clip? clip)
        {
            Clip? ignored;
            TryGetClip(name, null, out asset, out ignored);
            clip = null;
            if (asset == null)
            {
                return false;
            }

            if (asset.Clips.TryGetValue("$definition", out clip))
            {
                LastError = null;
                return true;
            }

            clip = new Clip
            {
                Start = 0,
                Count = asset.FrameCount
            };
            if (!TryCalculateBounds(asset, clip, out clip.Bounds))
            {
                return Fail("No drawable original definition: " + name);
            }

            asset.Clips["$definition"] = clip;
            LastError = null;
            return true;
        }

        /// <summary>Performs try get native definition info.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "info">The info.</param>
        /// <returns>The resulting try get native definition info.</returns>
        public bool TryGetNativeDefinitionInfo(string name, out NativeAnimationInfo info)
        {
            info = default(NativeAnimationInfo);
            Reanimation? asset;
            Clip? clip;
            if (!TryGetNativeDefinitionClip(name, out asset, out clip))
            {
                return false;
            }

            info = new NativeAnimationInfo
            {
                Start = 0,
                Count = clip.Count,
                SourceRate = asset.Fps,
                Bounds = clip.Bounds
            };
            return true;
        }

        /// <summary>Performs draw native definition.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "visible">The visible.</param>
        /// <param name = "facingLeft">The facing left.</param>
        /// <param name = "mirrorWidth">The mirror width.</param>
        /// <returns>The resulting draw native definition.</returns>
        public bool DrawNativeDefinition(string name, Vector2 origin, float scale, float seconds, bool loop, float rate = -1f, Func<string, bool>? visible = null, bool facingLeft = false, float mirrorWidth = 80f)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? clip;
            if (!TryGetNativeDefinitionClip(name, out asset, out clip))
            {
                return false;
            }

            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            try
            {
                var sample = NativeSeconds(clip, seconds, rate < 0 ? asset.Fps : rate, asset.Fps, loop);
                return DrawClipPose(asset, clip, sample, asset.Fps, false, previous * CombatPlacement(origin, scale, facingLeft, mirrorWidth), tint, null, null, visible);
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }
        }

        /// <summary>Performs draw native plant head attachment.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "parentClip">The parent clip.</param>
        /// <param name = "parentSeconds">The parent seconds.</param>
        /// <param name = "parentRate">The parent rate.</param>
        /// <param name = "nativeKind">The native kind.</param>
        /// <param name = "headSeconds">The head seconds.</param>
        /// <param name = "facingLeft">The facing left.</param>
        /// <returns>The resulting draw native plant head attachment.</returns>
        public bool DrawNativePlantHeadAttachment(Vector2 origin, float scale, string? parentClip, float parentSeconds, float parentRate, int nativeKind, float headSeconds, bool facingLeft)
        {
            using var objectScope = BeginObjectScope();
            int childId;
            string anchor, childClip;
            float x, y, sx, sy;
            switch (nativeKind)
            {
                // PC 0x53F110/0x53F436/0x53F55D/0x53F692/0x53F7D9/0x53F857.
                case 26:
                    childId = 4;
                    anchor = "anim_head1";
                    childClip = "anim_head_idle";
                    x = 65;
                    y = -5;
                    sx = -1;
                    sy = 1;
                    break;
                case 27:
                    childId = 5;
                    anchor = "Zombie_body";
                    childClip = "anim_idle";
                    x = 50;
                    y = 0;
                    sx = -.8f;
                    sy = .8f;
                    break;
                case 28:
                    childId = 33;
                    anchor = "Zombie_body";
                    childClip = "anim_idle";
                    x = 55;
                    y = -5;
                    sx = -1;
                    sy = 1;
                    break;
                case 29:
                    childId = 78;
                    anchor = "anim_head1";
                    childClip = "anim_head_idle";
                    x = 65;
                    y = -5;
                    sx = -1;
                    sy = 1;
                    break;
                case 30:
                    childId = 11;
                    anchor = "anim_head1";
                    childClip = "anim_idle";
                    x = 55;
                    y = -15;
                    sx = -.75f;
                    sy = .75f;
                    break;
                case 31:
                    childId = 16;
                    anchor = "Zombie_body";
                    childClip = "anim_idle";
                    x = 37;
                    y = 0;
                    sx = -.8f;
                    sy = .8f;
                    break;
                default:
                    return false;
            }

            global::PlantsVsZombies.VerifiedData.ReanimationDefinition? definition;
            if (!global::PlantsVsZombies.VerifiedData.VerifiedDataCatalog.TryGetReanimation(childId, out definition))
            {
                return false;
            }

            if (!_attachmentNames.TryGetValue(childId, out var childName))
            {
                childName = Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'));
                _attachmentNames.Add(childId, childName);
            }
            Reanimation? parent, child;
            Clip? body, head;
            if (!TryGetClip("Zombie", parentClip, out parent, out body) || !TryGetClip(childName, childClip, out child, out head))
            {
                return false;
            }

            var track = FindTrack(parent, anchor);
            Matrix4x4 inverse;
            if (track == null || !TryInverseAffine(PartMatrix(GetFrame(track, 0)), out inverse))
            {
                return false;
            }

            var sample = SampleFrame(body, NativeSeconds(body, parentSeconds, parentRate, parent.Fps, true), parent.Fps, false);
            var pose = Interpolate(GetFrame(track, sample.Current), GetFrame(track, sample.Next), sample.Fraction);
            const double angle = .2;
            var offset = Matrix4x4.identity;
            offset.m00 = (float)Math.Cos(angle) * sx;
            offset.m10 = (float)Math.Sin(angle) * sx;
            offset.m01 = -(float)Math.Sin(angle) * sy;
            offset.m11 = (float)Math.Cos(angle) * sy;
            offset.m03 = x;
            offset.m13 = y;
            var previous = SpriteMatrix;
            var color = SpriteDrawState.Color;
            var previousClip = _nativeSpriteClip;
            try
            {
                // This attachment is submitted after the body traversal restored its track clip.
                // Reapply its original parent track's waterline in the current scene or capture coordinates.
                _nativeSpriteClip = NativePoolTrackClip(parent, anchor, previous, previousClip);
                var placement = previous * CombatPlacement(origin, scale, facingLeft, 60) * PartMatrix(pose) * inverse * offset;
                using var hierarchyAttachment = Hierarchy.BeginAttachment(12);
                return DrawClipPose(child, head, NativeSeconds(head, headSeconds, 15f, child.Fps, true), child.Fps, false, placement, color);
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = color;
                _nativeSpriteClip = previousClip;
            }
        }

        /// <summary>Performs try get native track position.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "trackName">The track name.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "position">The position.</param>
        /// <returns>The resulting try get native track position.</returns>
        public bool TryGetNativeTrackPosition(string? name, string? animation, string? trackName, float seconds, float rate, bool loop, out Vector2 position)
        {
            position = default(Vector2);
            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            var track = FindTrack(asset, trackName);
            if (track == null)
            {
                return false;
            }

            var time = SampleFrame(clip, NativeSeconds(clip, seconds, rate, asset.Fps, loop), asset.Fps, false);
            var pose = Interpolate(GetFrame(track, time.Current), GetFrame(track, time.Next), time.Fraction);
            position = new Vector2(pose.X, pose.Y);
            return true;
        }

        /// <summary>Performs draw resource sprite.</summary>
        /// <param name = "resource">The resource.</param>
        /// <param name = "center">The center.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "degrees">The degrees.</param>
        /// <param name = "frame">The frame.</param>
        /// <param name = "columns">The columns.</param>
        /// <param name = "rows">The rows.</param>
        /// <param name = "stretch">The stretch.</param>
        /// <param name = "additive">The additive.</param>
        /// <param name="clipTop">The fraction hidden from the sprite's top edge by its original particle definition.</param>
        /// <param name="clipBottom">The fraction hidden from the sprite's bottom edge by its original particle definition.</param>
        /// <param name="imageRow">The original atlas row selected by the particle emitter.</param>
        /// <returns>The resulting draw resource sprite.</returns>
        public bool DrawResourceSprite(string? resource, Vector2 center, float scale, float degrees = 0f, int frame = 0, int columns = 0, int rows = 0, float stretch = 1f, bool additive = false,
            float clipTop = 0f, float clipBottom = 0f, int imageRow = 0)
        {
            using var objectScope = BeginObjectScope();
            // Original zero-area sprites have no visible pixels. IMGUI rejects their singular matrix.
            if (scale == 0f || stretch == 0f)
            {
                return true;
            }

            clipTop = Mathf.Clamp01(clipTop);
            clipBottom = Mathf.Clamp01(clipBottom);
            var visibleHeight = 1f - clipTop - clipBottom;
            if (visibleHeight <= 0f)
            {
                return true;
            }

            if (_nativeResourceImageOverride != null)
            {
                resource = _nativeResourceImageOverride;
            }

            EnsureIndex();
            EnsureNativeAtlas();
            SpriteImage texture;
            Vector2Int atlas;
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (!_nativeAtlas.TryGetValue(resource, out atlas))
            {
                atlas = new Vector2Int(1, 1);
            }

            if (columns <= 0)
            {
                columns = atlas.x;
            }

            if (rows <= 0)
            {
                rows = atlas.y;
            }

            try
            {
                texture = LoadTexture(resource);
            }
            catch (FileNotFoundException)
            {
                return false;
            }

            if (texture == null || columns <= 0 || rows <= 0)
            {
                return false;
            }

            float width = (float)texture.Width / columns, height = (float)texture.Height / rows;
            var previous = SpriteMatrix;
            var previousAdditive = _nativeSpriteAdditive;
            _nativeSpriteAdditive = additive;
            var angle = degrees * Math.PI / 180d;
            var transform = Matrix4x4.identity;
            transform.m00 = (float)Math.Cos(angle) * scale;
            transform.m01 = -(float)Math.Sin(angle) * scale * stretch;
            transform.m10 = (float)Math.Sin(angle) * scale;
            transform.m11 = (float)Math.Cos(angle) * scale * stretch;
            transform.m03 = center.x - transform.m00 * width / 2 - transform.m01 * height / 2;
            transform.m13 = center.y - transform.m10 * width / 2 - transform.m11 * height / 2;
            try
            {
                var sprite = previous * transform;
                if (!HasDrawableArea(sprite))
                {
                    return true;
                }

                frame += Math.Max(0, Math.Min(rows - 1, imageRow)) * columns;
                SpriteMatrix = sprite * Matrix4x4.Translate(new Vector3(0f, height * clipTop, 0f));
                DrawTexturePart(texture, width, height * visibleHeight, new Rect((frame % columns) / (float)columns,
                    1f - (frame / columns + 1f - clipBottom) / rows, 1f / columns, visibleHeight / rows));
            }
            finally
            {
                SpriteMatrix = previous;
                _nativeSpriteAdditive = previousAdditive;
            }

            return true;
        }

        /// <summary>Stores native atlas.</summary>
        private readonly Dictionary<string, Vector2Int> _nativeAtlas = new Dictionary<string, Vector2Int>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores native atlas loaded.</summary>
        private bool _nativeAtlasLoaded;
        /// <summary>Performs ensure native atlas.</summary>
        private void EnsureNativeAtlas()
        {
            if (_nativeAtlasLoaded)
            {
                return;
            }

            _nativeAtlasLoaded = true;
            if (!Assets.TryReadText("properties/resources.xml", out var resources))
            {
                return;
            }

            var document = new XmlDocument
            {
                XmlResolver = null
            };
            document.LoadXml(resources);
            foreach (XmlNode group in document.SelectNodes("/ResourceManifest/Resources"))
            {
                var prefix = "IMAGE_";
                foreach (XmlNode node in group.ChildNodes)
                {
                    if (node.Name == "SetDefaults")
                    {
                        if (node.Attributes["idprefix"] != null)
                        {
                            prefix = node.Attributes["idprefix"].Value;
                        }

                        continue;
                    }

                    if (node.Name != "Image" || node.Attributes["id"] == null)
                    {
                        continue;
                    }

                    int columns, rows;
                    if (!int.TryParse(node.Attributes["cols"] == null ? "1" : node.Attributes["cols"].Value, out columns))
                    {
                        columns = 1;
                    }

                    if (!int.TryParse(node.Attributes["rows"] == null ? "1" : node.Attributes["rows"].Value, out rows))
                    {
                        rows = 1;
                    }

                    var id = prefix + node.Attributes["id"].Value;
                    _nativeAtlas[id] = new Vector2Int(columns, rows);

                }
            }
        }

        /// <summary>Performs reset native playback.</summary>
        public void ResetNativePlayback()
        {
            foreach (var blend in _nativePoseBlends.Values)
            {
                ReturnNativePoseBlend(blend);
            }

            _nativePoseBlends.Clear();
            _headInstanceKeys.Clear();
            _particleKeys.Clear();
            _expiredNativePoseBlends.Clear();
            _expiredHeadInstanceKeys.Clear();
            foreach (var surface in _nativeSurfaces.Values)
            {
                ReturnNativeSurface(surface);
            }

            _nativeSurfaces.Clear();
        }

        /// <summary>Retires a logical capture identity and returns its resources to the bounded inactive pool.</summary>
        /// <param name="key">The capture identity no longer used by the presentation.</param>
        public void ReleaseNativeSurface(string key)
        {
            NativeSurface surface;
            if (!_nativeSurfaces.TryGetValue(key, out surface))
            {
                return;
            }

            _nativeSurfaces.Remove(key);
            ReturnNativeSurface(surface);
        }

        /// <summary>Finds a reusable capture with matching attachments without rewriting a texture still visible this frame.</summary>
        /// <param name="width">The requested source width.</param>
        /// <param name="height">The requested source height.</param>
        /// <returns>A cleared capture pool with matching color and depth attachments.</returns>
        private NativeSurface RentNativeSurface(int width, int height)
        {
            for (var index = _inactiveNativeSurfaces.Count - 1; index >= 0; index--)
            {
                var surface = _inactiveNativeSurfaces[index];
                if (surface.Texture.width != width || surface.Texture.height != height
                    || surface.LastPresentedFrame == SpriteDrawState.FrameVersion)
                {
                    continue;
                }

                _inactiveNativeSurfaces.RemoveAt(index);
                _inactiveNativePixels -= (long)width * height;
                return surface;
            }

            NativeSurfaceCreationCount++;
            return new NativeSurface(width, height);
        }

        /// <summary>Gets the synchronous capture pool reserved for the current nested callback.</summary>
        /// <returns>The camera and mesh pool reused by all sequential captures at this nesting depth.</returns>
        private ObjectSpriteRenderer GetNativeCaptureRenderer()
        {
            if (_nativeCaptureDepth == _nativeCaptureRenderers.Count)
            {
                _nativeCaptureRenderers.Add(new ObjectSpriteRenderer(_textureOpacity, false));
            }

            return _nativeCaptureRenderers[_nativeCaptureDepth];
        }

        /// <summary>Clears actor-specific capture state while retaining a bounded set of reusable GPU attachments.</summary>
        /// <param name="surface">The capture returned by its former logical owner.</param>
        private void ReturnNativeSurface(NativeSurface surface)
        {
            surface.Ready = false;
            var pixels = (long)surface.Texture.width * surface.Texture.height;
            if (_inactiveNativeSurfaces.Count < MaximumRetainedNativeSurfaces
                && pixels <= MaximumRetainedNativePixels - _inactiveNativePixels)
            {
                _inactiveNativeSurfaces.Add(surface);
                _inactiveNativePixels += pixels;
                return;
            }

            DestroyNativeSurface(surface);
        }

        /// <summary>Releases the independent color and depth attachments of one actor capture.</summary>
        /// <param name="surface">The capture whose resources are no longer retained.</param>
        private static void DestroyNativeSurface(NativeSurface surface)
        {
            surface.Texture.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(surface.Texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(surface.Texture);
            }
        }

        // PC loop types 1/4 sample count*time, holding the complete last frame.
        // Normal DrawNativePlayback instead samples (count-1)*time; keep both APIs explicit.
        /// <summary>Performs draw native full last frame.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "horizontalScale">The horizontal scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "animation">The animation.</param>
        /// <param name = "loop">The loop.</param>
        /// <param name = "rate">The rate.</param>
        /// <returns>The resulting draw native full last frame.</returns>
        public bool DrawNativeFullLastFrame(string name, Vector2 origin, float horizontalScale, float seconds, string animation, bool loop, float rate)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? clip;
            if (!TryGetClip(name, animation, out asset, out clip))
            {
                return false;
            }

            var frame = Math.Max(0d, seconds) * Math.Max(0d, rate);
            if (loop)
            {
                frame %= clip.Count;
            }

            var sample = (float)(Math.Min(clip.Count - 1, frame) / asset.Fps);
            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            var placement = NativeMatrix(origin, 1f);
            placement.m00 = horizontalScale;
            try
            {
                return DrawClipPose(asset, clip, sample, asset.Fps, false, previous * placement, tint, null, null, null);
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }
        }

        // PC 0x547810 raises the SAME +0xB8 counter that production uses.
        // Reuse the existing source-texture ADD path, preserving original attachments and Imitater filtering.
        /// <summary>Performs draw native plant hit feedback.</summary>
        /// <param name = "key">The key.</param>
        /// <param name = "viewport">The viewport.</param>
        /// <param name = "draw">The draw.</param>
        /// <param name = "plant">The plant.</param>
        /// <param name="surfaceKey">The cached full surface identity, or null to derive it from the playback key.</param>
        /// <param name="highlighted">Whether the original shovel target highlight is active.</param>
        /// <returns>The resulting draw native plant hit feedback.</returns>
        public bool DrawNativePlantHitFeedback(string key, Rect viewport, Func<bool> draw, Plant plant, string? surfaceKey = null, bool highlighted = false)
        {
            using var objectScope = BeginObjectScope();
            var visual = AdventurePlantHitRules.Combined(plant);
            if (highlighted)
            {
                // Original Plant::UpdateReanimColor uses additive alpha 196, or 92 for Imitater.
                var brightness = Math.Max(visual.AdditiveRgb, plant.IsImitated ? 92 : 196);
                visual = new AdventureProducerGlowVisual(true, visual.FlashTicks, (byte)brightness);
            }

            return DrawProducerGlow(key, viewport, draw, visual, plant.IsImitated, surfaceKey);
        }

        /// <summary>Performs draw native potato mine light.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "bodySeconds">The body seconds.</param>
        /// <param name = "bodyRate">The body rate.</param>
        /// <param name = "glowTime">The glow time.</param>
        /// <param name = "frameCount">The frame count.</param>
        /// <returns>The resulting draw native potato mine light.</returns>
        public bool DrawNativePotatoMineLight(Vector2 origin, float scale, float bodySeconds, float bodyRate, float glowTime, int frameCount)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? asset;
            Clip? body, glow;
            Track? anchor;
            Matrix4x4 inverseBase;
            Rect bounds;
            if (!TryPrepareAttachedPlant("PotatoMine", "anim_armed", "anim_glow", "anim_light", out asset, out body, out glow, out anchor, out inverseBase, out bounds))
            {
                return false;
            }

            // PC explicitly expands the one-frame marker's clip to 3..10 frames and
            // keeps disappearing frames. Sampling the default anim_glow clip would stay lit.
            var lightCount = Math.Max(3, Math.Min(10, frameCount));
            if (!_potatoLightClips.TryGetValue(lightCount, out var light))
            {
                light = new Clip
                {
                    Start = glow.Start,
                    Count = lightCount,
                    Bounds = glow.Bounds
                };
                _potatoLightClips.Add(lightCount, light);
            }
            var bodySample = NativeSeconds(body, bodySeconds, bodyRate, asset.Fps, true);
            var time = SampleFrame(body, bodySample, asset.Fps, false);
            var attachment = PartMatrix(Interpolate(GetFrame(anchor, time.Current), GetFrame(anchor, time.Next), time.Fraction)) * inverseBase;
            var phase = glowTime - (float)Math.Floor(glowTime);
            var previous = SpriteMatrix;
            var tint = SpriteDrawState.Color;
            try
            {
                return DrawClipPose(asset, light, phase * (light.Count - 1) / asset.Fps, asset.Fps, false, previous * NativeMatrix(origin, scale) * attachment, tint, null, null, track => TrackIs(track, "anim_glow"));
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = tint;
            }
        }

        /// <summary>Stores producer glow shader warning reported.</summary>
        private bool _producerGlowShaderWarningReported;
        /// <summary>
        /// Draw the existing body/attached-track callback with the original source-colored additive brightness.
        /// The shared CombatSprite shader must expose _ProducerAdditive and apply
        /// saturate(pixel.rgb * (1 + _ProducerAdditive)) after its Imitater filter, before white-hit treatment.
        /// Production brightness is a per-track material operation; no glow sprite, blur or particle is added.
        /// The caller draws its original plant shadow outside this callback, as in the native renderer.
        /// </summary>
        /// <param name="instanceKey">The stable playback identity used to derive an uncached surface key.</param>
        /// <param name="viewport">The original pixel bounds receiving local composition.</param>
        /// <param name="draw">The synchronous source-track drawing callback.</param>
        /// <param name="visual">The original producer brightness sampled by the simulation.</param>
        /// <param name="imitated">Whether the source tracks use imitation grayscale.</param>
        /// <param name="surfaceKey">The cached full surface identity, or null to derive it from the playback key.</param>
        /// <returns>Whether the source callback submitted visible artwork.</returns>
        public bool DrawProducerGlow(string instanceKey, Rect viewport, Func<bool> draw, AdventureProducerGlowVisual visual, bool imitated = false, string? surfaceKey = null)
        {
            using var objectScope = BeginObjectScope();
            if (draw == null)
            {
                return Fail("An existing producer reanimation draw callback is required.");
            }

            if (!visual.Enabled && !imitated)
            {
                return draw();
            }

            if (!EnsureNativeSpriteMaterial())
            {
                return false;
            }

            if (!RequiredNativePortraitMaterial.HasProperty("_ProducerAdditive"))
            {
                if (!_producerGlowShaderWarningReported)
                {
                    _producerGlowShaderWarningReported = true;
                    Debug.LogWarning("Original producer hint awaits the CombatSprite _ProducerAdditive source-color hook.");
                }

                return draw();
            }

            if (CanDrawZombieDirect(SpriteDrawState.Color, viewport))
            {
                var previousGrayscale = _nativeGrayscale;
                var previousProducer = _nativeProducerAdditive;
                try
                {
                    _nativeGrayscale = imitated ? 1f : 0f;
                    _nativeProducerAdditive = visual.Enabled ? visual.SourceAdditive : 0f;
                    RequiredNativePortraitMaterial.SetFloat("_Grayscale", _nativeGrayscale);
                    RequiredNativePortraitMaterial.SetFloat("_ProducerAdditive", _nativeProducerAdditive);
                    return DrawDirectClipped(viewport, draw);
                }
                finally
                {
                    _nativeGrayscale = previousGrayscale;
                    _nativeProducerAdditive = previousProducer;
                    RequiredNativePortraitMaterial.SetFloat("_Grayscale", _nativeGrayscale);
                    RequiredNativePortraitMaterial.SetFloat("_ProducerAdditive", _nativeProducerAdditive);
                }
            }

            return DrawNativeClipped(surfaceKey ?? "producer-" + instanceKey, viewport, draw, grayscale: imitated, producerAdditive: visual.Enabled ? visual.SourceAdditive : 0f);
        }

        /// <summary>Stores native combat zombie.</summary>
        private Zombie? _nativeCombatZombie;

        /// <summary>Gets the native combat zombie, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private Zombie RequiredNativeCombatZombie => _nativeCombatZombie ?? throw new System.InvalidOperationException("The member '_nativeCombatZombie' is not available.");
        /// <summary>Stores native combat body.</summary>
        private string _nativeCombatBody = string.Empty;
        /// <summary>The shield render grouping computed once for the current combat callback.</summary>
        private int _nativeCombatShieldType;
        /// <summary>Stores native combat tint.</summary>
        private Color _nativeCombatTint = Color.white;
        /// <summary>Stores native track additive.</summary>
        private Color _nativeTrackAdditive = Color.clear;
        /// <summary>Stores native surface additive.</summary>
        private float _nativeSurfaceAdditive;
        /// <summary>Stores native sprite additive.</summary>
        private bool _nativeSpriteAdditive;
        /// <summary>Stores the original pool body's board-space waterline, independently of ignored-clip tracks.</summary>
        private float? _nativeCombatBodyClipBottom;
        /// <summary>The original species-specific body rectangle, separate from capture allocation and ignored water tracks.</summary>
        private Rect? _nativeCombatBodyClipBounds;
        /// <summary>Stores native resource image override.</summary>
        private string? _nativeResourceImageOverride;
        // PC 0x548D40..0x54942E. A single original sprite pass plus its textured ADD pass.
        // The callback includes body and attached heads. Do not redraw the armor afterward.
        /// <summary>Performs draw native zombie combat.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "viewport">The viewport.</param>
        /// <param name = "draw">The draw.</param>
        /// <param name="surfaceKey">The cached full surface identity used when combat feedback requires capture.</param>
        /// <param name="bodyClipBottom">Optional native pool waterline; the original ignored-clip tracks retain their full bounds.</param>
        /// <param name="bodyClipBounds">Optional original body rectangle whose horizontal and top edges also apply to ordinary tracks.</param>
        /// <returns>The resulting draw native zombie combat.</returns>
        public bool DrawNativeZombieCombat(Zombie zombie, Rect viewport, Func<bool> draw, string? surfaceKey = null, float? bodyClipBottom = null, Rect? bodyClipBounds = null)
        {
            using var objectScope = BeginObjectScope();
            if (zombie == null || draw == null)
            {
                return false;
            }

            var previous = _nativeCombatZombie;
            var previousBody = _nativeCombatBody;
            var previousShield = _nativeCombatShieldType;
            var previousBodyClip = _nativeCombatBodyClipBottom;
            var previousBodyBounds = _nativeCombatBodyClipBounds;
            Color previousTint = _nativeCombatTint, previousAdd = _nativeTrackAdditive, color = SpriteDrawState.Color;
            try
            {
                _nativeCombatZombie = zombie;
                _nativeCombatBody = GameSimulation.AdventureZombiePresentationAsset(zombie);
                _nativeCombatShieldType = AdventureZombieFeedbackRules.ShieldType(zombie);
                _nativeCombatBodyClipBottom = bodyClipBottom;
                _nativeCombatBodyClipBounds = bodyClipBounds;
                _nativeCombatTint = color;
                SpriteDrawState.Color = Color.white;
                if (CanDrawZombieDirect(color, viewport))
                {
                    return DrawDirectClipped(viewport, draw);
                }

                return DrawNativeClipped(surfaceKey ?? "native-zombie-combat-" + zombie.Id, viewport, draw);
            }
            finally
            {
                _nativeCombatZombie = previous;
                _nativeCombatBody = previousBody;
                _nativeCombatShieldType = previousShield;
                _nativeCombatBodyClipBottom = previousBodyClip;
                _nativeCombatBodyClipBounds = previousBodyBounds;
                _nativeCombatTint = previousTint;
                _nativeTrackAdditive = previousAdd;
                SpriteDrawState.Color = color;
            }
        }

        /// <summary>Clips original pool body tracks in either scene or local-capture destination pixels.</summary>
        /// <param name="asset">The animation supplying this track.</param>
        /// <param name="track">The original sampled track name.</param>
        /// <param name="boardTransform">The transform from logical board pixels into the current render target.</param>
        /// <param name="inherited">The parent attachment's clip, if any.</param>
        /// <returns>The track's water clip, or its inherited clip when PC ReanimIgnoreClipRect permits drawing outside it.</returns>
        private Rect? NativePoolTrackClip(Reanimation asset, string track, Matrix4x4 boardTransform, Rect? inherited)
        {
            if (_nativeCombatZombie == null || !_nativeCombatBodyClipBottom.HasValue
                || !AdventureZombieFeedbackRules.Same(asset.Name, _nativeCombatBody))
            {
                return inherited;
            }

            // PC 53FE40 exempts plain-body hands; swimmer initialization exempts its authored water tracks.
            // RiseFromGrave keeps whitewater2 exempt. ReanimReenableClipping restores every track for a kelp pull.
            if (_nativeCombatZombie.PresentationNativeHeight != 3
                && (TrackIs(track, "Zombie_snorkle_whitewater") || TrackIs(track, "Zombie_snorkle_whitewater2")
                    || TrackIs(track, "Zombie_dolphinrider_whitewater") || TrackIs(track, "Zombie_dolphinrider_dolphininwater")
                    || IsZombie(asset) && (TrackIs(track, "Zombie_whitewater2") || _nativeCombatZombie.PresentationNativePhase != 14
                        && (TrackIs(track, "Zombie_duckytube") && _nativeCombatZombie.PresentationNativePhase != 1
                            || TrackIs(track, "Zombie_whitewater") || TrackIs(track, "Zombie_outerarm_hand") || TrackIs(track, "Zombie_innerarm3")))))
            {
                return inherited;
            }

            var clip = _renderingNativeSurface ? _nativeCaptureClip : SpriteDrawState.Clip;
            if (_nativeCombatBodyClipBounds.HasValue)
            {
                var bounds = _nativeCombatBodyClipBounds.Value;
                var left = Math.Max(clip.xMin, boardTransform.m00 * bounds.xMin + boardTransform.m03);
                var top = Math.Max(clip.yMin, boardTransform.m11 * bounds.yMin + boardTransform.m13);
                var right = Math.Max(left, Math.Min(clip.xMax, boardTransform.m00 * bounds.xMax + boardTransform.m03));
                var edge = Math.Max(top, Math.Min(clip.yMax, boardTransform.m11 * bounds.yMax + boardTransform.m13));
                return Rect.MinMaxRect(left, top, right, edge);
            }

            var bottom = boardTransform.m11 * _nativeCombatBodyClipBottom.Value + boardTransform.m13;
            var height = Math.Max(0f, Math.Min(clip.yMax, bottom) - clip.yMin);
            return new Rect(clip.x, clip.y, clip.width, height);
        }

        /// <summary>Draws an original particle attachment at its transformed position without inheriting track rotation or scale.</summary>
        /// <param name="particles">The retained original particle systems.</param>
        /// <param name="key">The cached identity of this attachment.</param>
        /// <param name="age">The original particle system age in seconds.</param>
        /// <param name="origin">The transformed attachment position in native board pixels.</param>
        /// <param name="seed">The original deterministic particle seed.</param>
        /// <param name="scale">The zombie's native particle scale override.</param>
        /// <param name="boardTransform">The outer board-to-target transform before sampling this track.</param>
        /// <param name="alpha">The sampled track's attachment opacity.</param>
        /// <param name="particleName">The unchanged original particle definition, defaulting to pool-emergence seaweed.</param>
        /// <returns>Whether the original attached particle system submitted artwork.</returns>
        internal bool DrawNativePoolParticleAttachment(OriginalParticleRenderer particles, string key, float age, Vector2 origin,
            int seed, float scale, Matrix4x4 boardTransform, float alpha, string particleName = "Zombie_seaweed")
        {
            var previousMatrix = SpriteMatrix;
            var previousColor = SpriteDrawState.Color;
            try
            {
                // PC Attachment::SetMatrix uses only the transformed translation for particle systems.
                SpriteMatrix = boardTransform;
                SpriteDrawState.Color = _nativeCombatZombie == null
                    ? new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * alpha)
                    : NativeZombieTrackColor(0, alpha);
                return particles.Draw(key, particleName, age, origin, seed, scale);
            }
            finally
            {
                SpriteMatrix = previousMatrix;
                SpriteDrawState.Color = previousColor;
            }
        }

        /// <summary>Draws source tracks using the same target-pixel bounds as a local composition surface.</summary>
        /// <param name="viewport">The native actor bounds before the outer board transform.</param>
        /// <param name="draw">The synchronous source-track callback.</param>
        /// <returns>Whether the callback submitted any original artwork.</returns>
        private bool DrawDirectClipped(Rect viewport, Func<bool> draw)
        {
            var previousClip = SpriteDrawState.Clip;
            var matrix = SpriteDrawState.Matrix;
            var left = Math.Max(previousClip.xMin, viewport.xMin * matrix.m00 + matrix.m03);
            var top = Math.Max(previousClip.yMin, viewport.yMin * matrix.m11 + matrix.m13);
            var right = Math.Min(previousClip.xMax, viewport.xMax * matrix.m00 + matrix.m03);
            var bottom = Math.Min(previousClip.yMax, viewport.yMax * matrix.m11 + matrix.m13);
            SpriteDrawState.Clip = new Rect(left, top, Math.Max(0f, right - left), Math.Max(0f, bottom - top));
            try
            {
                return draw();
            }
            finally
            {
                SpriteDrawState.Clip = previousClip;
            }
        }

        /// <summary>Identifies source-track effects that can retain alpha directly without a local capture.</summary>
        /// <param name="tint">The caller's final actor tint and opacity.</param>
        /// <param name="viewport">The local clipping bounds normally supplied by the capture surface.</param>
        /// <returns>True when source-track shader operations and a screen clip preserve the original pose.</returns>
        private bool CanDrawZombieDirect(Color tint, Rect viewport)
        {
            var matrix = SpriteDrawState.Matrix;
            return !_renderingNativeSurface && IsValidDestination(viewport) && tint.Equals(Color.white)
                && matrix.m00 > 0f && matrix.m11 > 0f && matrix.m01 == 0f && matrix.m10 == 0f;
        }

        /// <summary>Selects cached render groups using the live shield state only for its owning body asset.</summary>
        /// <param name="asset">The source body or attachment being sampled.</param>
        /// <returns>Original source indices in native group order, or the unchanged full track order for attachments.</returns>
        private int[][] NativeZombieTrackGroups(Reanimation asset)
        {
            var shield = _nativeCombatZombie != null && AdventureZombieFeedbackRules.Same(asset.Name, _nativeCombatBody)
                ? _nativeCombatShieldType : 0;
            return asset.NativeTrackGroups[shield];
        }

        /// <summary>Performs native zombie track visible.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting native zombie track visible.</returns>
        private bool NativeZombieTrackVisible(Reanimation asset, string track)
        {
            if (_nativeCombatZombie == null || !AdventureZombieFeedbackRules.Same(asset.Name, _nativeCombatBody))
            {
                return true;
            }

            var kind = _nativeCombatZombie.ModeHead >= 26 ? _nativeCombatZombie.ModeHead : (int)_nativeCombatZombie.Kind;
            return AdventureZombieEquipmentRules.TrackVisible(_nativeCombatZombie, track) && (!_nativeCombatZombie.PresentationArmLost || AdventureZombieFeedbackRules.ArmTrackVisible(kind, track)) && (!_nativeCombatZombie.Headless || AdventureZombiePartsRules.HeadTrackVisible(kind, track));
        }

        /// <summary>Performs native zombie image.</summary>
        /// <param name = "asset">The asset.</param>
        /// <param name = "track">The track.</param>
        /// <param name = "original">The original.</param>
        /// <returns>The resulting native zombie image.</returns>
        private SpriteImage NativeZombieImage(Reanimation asset, string track, SpriteImage original)
        {
            if (_nativeCombatZombie == null)
            {
                return original;
            }

            var equipment = AdventureZombieEquipmentRules.Image(_nativeCombatZombie, asset.Name, _nativeCombatBody, track);
            if (equipment != null)
            {
                EnsureNativeAtlas();
                return LoadTexture(equipment);
            }

            if (!_nativeCombatZombie.PresentationArmLost)
            {
                return original;
            }

            var kind = _nativeCombatZombie.ModeHead >= 26 ? _nativeCombatZombie.ModeHead : (int)_nativeCombatZombie.Kind;
            var replacement = AdventureZombieFeedbackRules.Same(asset.Name, _nativeCombatBody) ? AdventureZombieFeedbackRules.ArmReplacement(kind, track) : null;
            if ((kind == 1 || kind == 33) && AdventureZombieFeedbackRules.Same(track, "Zombie_flag"))
            {
                replacement = "IMAGE_REANIM_ZOMBIE_FLAG3";
            }

            if (replacement == null)
            {
                return original;
            }

            EnsureNativeAtlas();
            return LoadTexture(replacement);
        }

        /// <summary>Preserves the dolphin's original water-track color overrides independently of its body feedback.</summary>
        /// <param name="asset">The source definition supplying this track.</param>
        /// <param name="track">The original source track name.</param>
        /// <param name="group">The native body or shield render group.</param>
        /// <param name="alpha">The source track's current alpha.</param>
        /// <returns>The original track tint, including the caller's inherited color and opacity.</returns>
        private Color NativeZombieTrackColor(Reanimation asset, string track, int group, float alpha)
        {
            // PC SetupWaterTrack 552CC0 sets IgnoreColorOverride and IgnoreExtraAdditiveColor as well as IgnoreClipRect.
            // DragUnder restores only clipping; these two color exclusions remain active for both original water tracks.
            if (_nativeCombatZombie?.Kind == ZombieKind.DolphinRider
                && AdventureZombieFeedbackRules.Same(asset.Name, _nativeCombatBody)
                && (TrackIs(track, "Zombie_dolphinrider_whitewater") || TrackIs(track, "Zombie_dolphinrider_dolphininwater")))
            {
                _nativeTrackAdditive = Color.clear;
                return new Color(_nativeCombatTint.r, _nativeCombatTint.g, _nativeCombatTint.b, alpha * _nativeCombatTint.a);
            }

            return NativeZombieTrackColor(group, alpha);
        }

        /// <summary>Performs native zombie track color.</summary>
        /// <param name = "group">The group.</param>
        /// <param name = "alpha">The alpha.</param>
        /// <returns>The resulting native zombie track color.</returns>
        private Color NativeZombieTrackColor(int group, float alpha)
        {
            var z = RequiredNativeCombatZombie;
            Color tint = Color.white, additive = Color.clear;
            if (group == 1)
            {
                var flash = AdventureZombieFeedbackRules.FlashChannel(z.PresentationArmorFlash) / 255f;
                additive = new Color(flash, flash, flash, 1f);
            }
            else
            {
                if (z.Hypnotized)
                {
                    tint = new Color(128f / 255f, 64f / 255f, 192f / 255f, 1f);
                }
                else if (z.ChillRemaining > 0 || z.FrozenRemaining > 0 && z.PresentationButter <= 0)
                {
                    tint = new Color(75f / 255f, 75f / 255f, 1f, 1f);
                }
                else if (z.PresentationNativeHeight == 10 && z.Health < 100f)
                {
                    tint = new Color(100f / 255f, 150f / 255f, 25f / 255f, 1f);
                }

                if (z.Hypnotized || z.ChillRemaining > 0 || z.FrozenRemaining > 0 && z.PresentationButter <= 0 || z.PresentationNativeHeight == 10 && z.Health < 100f)
                {
                    additive = tint;
                }

                var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
                additive.r = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.r * 255f), z.PresentationFlash, sled) / 255f;
                additive.g = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.g * 255f), z.PresentationFlash, sled) / 255f;
                additive.b = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.b * 255f), z.PresentationFlash, sled) / 255f;
            }

            // A frozen-black burn corpse is supplied in the caller's native tint.
            if (_nativeCombatTint.r == 0f && _nativeCombatTint.g == 0f && _nativeCombatTint.b == 0f)
            {
                additive = Color.clear;
            }

            _nativeTrackAdditive = additive;
            return new Color(tint.r * _nativeCombatTint.r, tint.g * _nativeCombatTint.g, tint.b * _nativeCombatTint.b, alpha * _nativeCombatTint.a);
        }

        /// <summary>Performs native zombie track matrix.</summary>
        /// <param name = "matrix">The matrix.</param>
        /// <param name = "group">The group.</param>
        /// <returns>The resulting native zombie track matrix.</returns>
        private Matrix4x4 NativeZombieTrackMatrix(Matrix4x4 matrix, int group)
        {
            if (_nativeCombatZombie != null && group == 1)
            {
                matrix.m03 += (int)(3f * Math.Min(.12f, _nativeCombatZombie.PresentationShieldRecoil) / .12f);
            }

            return matrix;
        }

        // zombieOrigin is the SAME source/pivot origin used when the body was rendered at drop.X/Row.
        // All launch, acceleration, spin and bounce values remain in original particle XML.
        /// <summary>Performs draw native zombie arm drop.</summary>
        /// <param name = "particles">The particles.</param>
        /// <param name = "drop">The drop.</param>
        /// <param name = "zombieOrigin">The zombie origin.</param>
        /// <param name = "renderRemainder">The render remainder.</param>
        /// <returns>The resulting draw native zombie arm drop.</returns>
        public bool DrawNativeZombieArmDrop(OriginalParticleRenderer? particles, AdventureZombieArmDrop drop, Vector2 zombieOrigin, float renderRemainder = 0f)
        {
            using var objectScope = BeginObjectScope();
            if (particles == null || drop == null)
            {
                return false;
            }

            Vector2 point;
            if (!TryGetNativeTrackPosition(drop.Asset, drop.Animation, drop.Anchor, drop.AnimationAge, drop.Rate, true, out point))
            {
                point = new Vector2(45f + (drop.Backwards ? 36f : 0f), 78f);
            }
            else if (drop.Backwards)
            {
                point.x = 90f - point.x;
            }

            var origin = zombieOrigin + point * drop.Scale;
            var previousImage = _nativeResourceImageOverride;
            var previousColor = SpriteDrawState.Color;
            try
            {
                _nativeResourceImageOverride = drop.Image;
                if (drop.Hypnotized)
                {
                    SpriteDrawState.Color = new Color(128f / 255f, 64f / 255f, 192f / 255f, previousColor.a);
                }
                else if (drop.Chilled)
                {
                    SpriteDrawState.Color = new Color(75f / 255f, 75f / 255f, 1f, previousColor.a);
                }

                return particles.Draw(ParticleKey("native-arm-", drop.Id), drop.Mowed ? "MoweredZombieArm" : "ZombieArm", drop.Age + renderRemainder, origin, drop.Seed, drop.Scale);
            }
            finally
            {
                _nativeResourceImageOverride = previousImage;
                SpriteDrawState.Color = previousColor;
            }
        }

        /// <summary>Reuses detached-part identities while also supporting callers without an explicit object frame.</summary>
        /// <param name="prefix">The existing native particle identity prefix.</param>
        /// <param name="id">The live detached part's simulation identity.</param>
        /// <returns>A stable identity retained only for the current and preceding presentation frames.</returns>
        private string ParticleKey(string prefix, int id)
        {
            if (_particleKeyFrame != SpriteDrawState.FrameVersion)
            {
                _particleKeys.BeginFrame();
                _particleKeyFrame = SpriteDrawState.FrameVersion;
            }

            return _particleKeys.Get(prefix, id);
        }

        /// <summary>Performs draw native zombie part drop.</summary>
        /// <param name = "particles">The particles.</param>
        /// <param name = "drop">The drop.</param>
        /// <param name = "zombieOrigin">The zombie origin.</param>
        /// <param name = "renderRemainder">The render remainder.</param>
        /// <returns>The resulting draw native zombie part drop.</returns>
        public bool DrawNativeZombiePartDrop(OriginalParticleRenderer? particles, AdventureZombiePartDrop drop, Vector2 zombieOrigin, float renderRemainder = 0f)
        {
            using var objectScope = BeginObjectScope();
            if (particles == null || drop == null)
            {
                return false;
            }

            Vector2 point, origin;
            if (drop.ParticleId == 54)
            {
                // PC DropShield ladder: (mPosX+31,mPosY+80), independent of the ladder track.
                origin = zombieOrigin + new Vector2(16f - 30f * (1f - drop.Scale), 80f - 120f * (1f - drop.Scale));
            }
            else
            {
                if (!TryGetNativeTrackPosition(drop.Asset, drop.Animation, drop.Anchor, drop.AnimationAge, drop.Rate, true, out point))
                {
                    return false;
                }

                if (drop.Backwards)
                {
                    point.x = 90f - point.x;
                }

                origin = zombieOrigin + point * drop.Scale;
            }

            if (drop.ParticleId == 11)
            {
                origin += new Vector2(6f, -45f); // PC 0x5453B0, unscaled world offset.
            }

            var previousImage = _nativeResourceImageOverride;
            var previousColor = SpriteDrawState.Color;
            try
            {
                _nativeResourceImageOverride = drop.Image;
                if (drop.Hypnotized)
                {
                    SpriteDrawState.Color = new Color(128f / 255f, 64f / 255f, 192f / 255f, previousColor.a);
                }
                else if (drop.Chilled)
                {
                    SpriteDrawState.Color = new Color(75f / 255f, 75f / 255f, 1f, previousColor.a);
                }

                // Backup dancer's 1.05 is an absolute particle scale in PC, after ApplyZombieScale.
                var scale = drop.NativeKind == 9 && drop.ParticleId != 11 ? 1.05f : drop.Scale;
                return particles.Draw(ParticleKey("native-zombie-part-", drop.Id), AdventureZombiePartsRules.ParticleName(drop.ParticleId), drop.Age + renderRemainder, origin, drop.Seed, scale);
            }
            finally
            {
                _nativeResourceImageOverride = previousImage;
                SpriteDrawState.Color = previousColor;
            }
        }

        /// <summary>Performs draw native zombie flag attachment.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "bodyClip">The body clip.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "bodyRate">The body rate.</param>
        /// <param name = "flagSeconds">The flag seconds.</param>
        /// <param name = "backwards">The backwards.</param>
        /// <returns>The resulting draw native zombie flag attachment.</returns>
        public bool DrawNativeZombieFlagAttachment(Vector2 origin, float scale, string? bodyClip, float seconds, float bodyRate, float flagSeconds, bool backwards)
        {
            using var objectScope = BeginObjectScope();
            Reanimation? parent, child;
            Clip? body, flag;
            if (!TryGetClip("Zombie", bodyClip, out parent, out body) || !TryGetNativeDefinitionClip("Zombie_FlagPole", out child, out flag))
            {
                return false;
            }

            var anchor = FindTrack(parent, "Zombie_flaghand");
            Matrix4x4 inverse;
            if (anchor == null || !TryInverseAffine(PartMatrix(GetFrame(anchor, 0)), out inverse))
            {
                return false;
            }

            var sample = SampleFrame(body, NativeSeconds(body, seconds, bodyRate, parent.Fps, true), parent.Fps, false);
            var pose = Interpolate(GetFrame(anchor, sample.Current), GetFrame(anchor, sample.Next), sample.Fraction);
            var previous = SpriteMatrix;
            var color = SpriteDrawState.Color;
            try
            {
                using var hierarchyAttachment = Hierarchy.BeginAttachment(10);
                return DrawClipPose(child, flag, NativeSeconds(flag, flagSeconds, 15f, child.Fps, true), child.Fps, false,
                    previous * CombatPlacement(origin, scale, backwards, 90f) * PartMatrix(pose) * inverse, color);
            }
            finally
            {
                SpriteMatrix = previous;
                SpriteDrawState.Color = color;
            }
        }

        /// <summary>Performs draw native sprite pass.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "uv">The uv.</param>
        /// <param name = "pass">The pass.</param>
        /// <param name = "matrix">The matrix.</param>
        /// <param name="combineHit">Whether normal alpha and source-textured hit addition share one draw.</param>
        private void DrawNativeSpritePass(SpriteImage texture, float width, float height, Rect uv, int pass, Matrix4x4 matrix, bool combineHit = false)
        {
            var renderer = _renderingNativeSurface ? _nativeObjectRenderer : GetObjectRenderer();
            if (renderer == null)
            {
                throw new System.InvalidOperationException("The value 'renderer' is not available.");
            }

            renderer.EnqueueQuad(texture, width, height, uv, matrix, SpriteDrawState.Color, pass, _nativeGrayscale, _nativeProducerAdditive, _nativeCombatZombie == null ? _nativeSurfaceAdditive : 0f, _nativeCombatZombie == null ? Color.clear : _nativeTrackAdditive, _nativeSpriteClip, combineHit);
        }

        /// <summary>Performs draw native surface composite.</summary>
        /// <param name = "texture">The texture.</param>
        /// <param name = "viewport">The viewport.</param>
        /// <param name = "source">The source.</param>
        private void DrawNativeSurfaceComposite(Texture texture, Rect viewport, Rect? source = null)
        {
            if (!CanSubmitObjectFrame || !EnsureNativeSpriteMaterial())
            {
                return;
            }

            DrawNativeSpritePass(SpriteImage.FromTexture(texture), viewport.width, viewport.height, source ?? new Rect(0, 0, 1, 1), 3, SpriteMatrix * NativeMatrix(new Vector2(viewport.x, viewport.y), 1f));
        }

        /// <summary>Performs ensure native sprite material.</summary>
        /// <returns>The resulting ensure native sprite material.</returns>
        private bool EnsureNativeSpriteMaterial()
        {
            if (_nativePortraitMaterial != null)
            {
                return true;
            }

            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            if (shader == null)
            {
                return Fail("Native combat sprite shader resource is missing.");
            }

            _nativePortraitMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }
    }
}
