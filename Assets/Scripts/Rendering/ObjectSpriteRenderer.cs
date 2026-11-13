#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Combines ordered sprite regions into reusable meshes drawn by the project's render pipeline cameras.</summary>
    /// <remarks>Call on Unity's main thread. Layer 31 is reserved for sprite cameras and interface canvases.</remarks>
    public sealed class ObjectSpriteRenderer : IDisposable
    {
        /// <summary>The depth separating local captures from scene sprites without reducing destination-pixel XY precision.</summary>
        private const float CaptureDepth = -100f;
        /// <summary>The screen pools whose published objects must be retired before the next presentation.</summary>
        private static readonly List<ObjectSpriteRenderer> s_scenePools = new List<ObjectSpriteRenderer>();
        /// <summary>The shared camera that renders scene sprites and Canvas graphics directly to the display.</summary>
        private static Camera? s_sceneCamera;
        /// <summary>The logical source commands retained independently of their combined GPU geometry.</summary>
        private readonly List<ObjectSprite> _sprites = new List<ObjectSprite>();
        /// <summary>The explicitly combined geometry retained independently of source-level diagnostics.</summary>
        private readonly List<SpriteMeshBatch> _batches = new List<SpriteMeshBatch>();
        /// <summary>The shared blend materials whose native texture bindings are refreshed per submission batch.</summary>
        private readonly Dictionary<Texture, SpriteMaterialBinding> _materials = new Dictionary<Texture, SpriteMaterialBinding>();
        /// <summary>The monotonically increasing batch identity used to refresh native sampler bindings once per active texture.</summary>
        private ulong _batchVersion;
        /// <summary>The reused list of destroyed texture bindings awaiting safe batch-boundary retirement.</summary>
        private readonly List<Texture> _retiredTextures = new List<Texture>();
        /// <summary>The number of clipped vertices appended in the current batch.</summary>
        private int _vertexCount;
        /// <summary>The number of triangles appended in the current batch.</summary>
        private int _triangleCount;
        /// <summary>The number of logical commands rejected before reserving GPU geometry.</summary>
        private int _culledCount;
        /// <summary>The sum of clipped polygon areas, with repeated layers counted independently.</summary>
        private double _coveredPixelArea;
        /// <summary>The splits involving a texture or hardware blend change.</summary>
        private int _materialSplits;
        /// <summary>The splits required to preserve an intervening renderer.</summary>
        private int _painterSplits;
        /// <summary>The splits required to stay within portable 16-bit indices.</summary>
        private int _capacitySplits;
        /// <summary>The shader accepting per-vertex effects and premultiplied source-color hits.</summary>
        private readonly Shader _shader;
        /// <summary>The reusable input polygon for destination clipping.</summary>
        private SpriteClipVertex[] _clipInput = new SpriteClipVertex[12];
        /// <summary>The reusable output polygon for destination clipping.</summary>
        private SpriteClipVertex[] _clipOutput = new SpriteClipVertex[12];
        /// <summary>The destination-coordinate spacing between scene target pixel centers on the horizontal axis.</summary>
        private double _pixelStepX = 1d;
        /// <summary>The destination-coordinate spacing between scene target pixel centers on the vertical axis.</summary>
        private double _pixelStepY = 1d;
        /// <summary>The original clip whose raster bounds were most recently resolved.</summary>
        private Rect _cachedClip;
        /// <summary>The horizontal sample spacing used by the cached raster bounds.</summary>
        private double _cachedPixelStepX;
        /// <summary>The vertical sample spacing used by the cached raster bounds.</summary>
        private double _cachedPixelStepY;
        /// <summary>The half-open geometry bounds preserving inclusive fragment sample-center comparisons.</summary>
        private Rect _cachedRasterClip;
        /// <summary>Whether a clip and pixel-grid combination has been cached.</summary>
        private bool _hasCachedRasterClip;
        /// <summary>The number of combined GPU batches used by the current frame.</summary>
        private int _batchUsed;
        /// <summary>Whether source geometry uses disjoint transparent-cell pieces rather than stable native-composition tile quads.</summary>
        private readonly bool _useTightGeometry;
        /// <summary>The source opacity classifications shared with related animation surface pools.</summary>
        private readonly TextureOpacityCache _textureOpacity;
        /// <summary>The scene root owning this pool.</summary>
        private readonly GameObject _root;
        /// <summary>The camera that renders this pool into its destination surface.</summary>
        private readonly Camera _camera;
        /// <summary>The reused request submitted to the active render pipeline.</summary>
        private readonly RenderPipeline.StandardRequest _request = new RenderPipeline.StandardRequest();
        /// <summary>The isolated world position used only while rendering cropped native surfaces.</summary>
        private readonly Vector3 _capturePosition;
        /// <summary>Whether this pool has participated in direct scene presentation.</summary>
        private bool _scenePool;
        /// <summary>The number of active nested screen drawing scopes.</summary>
        private int _scopeDepth;
        /// <summary>The number of pool slots used by the current batch.</summary>
        private int _used;
        /// <summary>Whether this pool has released its Unity resources.</summary>
        private bool _disposed;
        /// <summary>Gets the number of retained logical source commands, without per-source GPU renderers.</summary>
        public int PooledObjectCount => _sprites.Count;
        /// <summary>Gets the number of actual combined mesh draws submitted by this pool.</summary>
        public int BatchCount => _batchUsed;
        /// <summary>Gets the logical workload and actual batch-fragmentation counters for the latest submission.</summary>
        internal SpriteBatchStatistics LastFrameStatistics => new SpriteBatchStatistics(_used, _batchUsed,
            _vertexCount, _triangleCount, _culledCount, _coveredPixelArea, _materialSplits, _painterSplits, _capacitySplits);
        /// <summary>Gets the number of live texture and blend-state material bindings owned by this pool.</summary>
        internal int CachedMaterialCount => _materials.Count * 3;
        /// <summary>Gets the retained logical source commands for source-level diagnostics.</summary>
        internal IReadOnlyList<ObjectSprite> SubmittedSprites => _sprites;
        /// <summary>Gets the number of quads submitted in the current batch.</summary>
        public int SubmittedObjectCount => _used;
        /// <summary>Gets whether a screen drawing scope is active.</summary>
        public bool IsFrameActive => _scopeDepth > 0;

        /// <summary>Gets or sets an optional diagnostic callback observing submitted native captures before their renderers are hidden.</summary>
        /// <remarks>The callback receives the capture camera and destination. It must not change rendering state.</remarks>
        internal static Action<Camera, RenderTexture>? CaptureObserved { get; set; }

        /// <summary>Gets the shared full-screen camera used by direct scene sprites and retained interface graphics.</summary>
        public static Camera SceneCamera
        {
            get
            {
                if (s_sceneCamera == null)
                {
                    var cameraObject = new GameObject("PvZ Scene Camera")
                    {
                        hideFlags = HideFlags.DontSave,
                        layer = 31
                    };
                    s_sceneCamera = CreateCamera(cameraObject);
                    s_sceneCamera.backgroundColor = Color.black;
                    s_sceneCamera.depth = 100f;
                    s_sceneCamera.enabled = true;
                }

                ConfigureSceneCamera(s_sceneCamera);
                return s_sceneCamera;
            }
        }

        /// <summary>Retires the previous presentation and resets painter order before submitting the next complete frame.</summary>
        public static void BeginSceneFrame()
        {
            SpriteDrawState.BeginFrame();
            foreach (var pool in s_scenePools)
            {
                pool.SetRenderersEnabled(false);
            }

            if (s_sceneCamera != null)
            {
                ConfigureSceneCamera(s_sceneCamera);
            }
        }

        /// <summary>Creates the camera and shared materials used by this sprite pool.</summary>
        /// <exception cref = "InvalidOperationException">The required rendering shaders are missing.</exception>
        public ObjectSpriteRenderer()
            : this(new TextureOpacityCache())
        {
        }

        /// <summary>Creates a sprite pool sharing the source texture classifications of its owner.</summary>
        /// <param name="textureOpacity">The cache shared by related screen and native surface pools.</param>
        /// <param name="useTightGeometry">Whether to use tight source pieces; native composition can retain tile quads to avoid rasterized T-junction seams.</param>
        /// <exception cref="InvalidOperationException">The required rendering shaders are missing.</exception>
        internal ObjectSpriteRenderer(TextureOpacityCache textureOpacity, bool useTightGeometry = true)
        {
            _textureOpacity = textureOpacity;
            _useTightGeometry = useTightGeometry;
            _shader = Resources.Load<Shader>("Shaders/PvZSpriteBatch");
            if (_shader == null)
            {
                throw new InvalidOperationException("The PvZ sprite batch shader is missing.");
            }

            _root = new GameObject("PvZ Object Sprite Pool")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            _capturePosition = new Vector3(0f, 0f, CaptureDepth);
            _root.transform.position = _capturePosition;
            var cameraObject = new GameObject("PvZ Sprite Camera")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = CreateCamera(cameraObject);
        }

        /// <summary>Begins or nests a batch of objects displayed directly by the scene camera.</summary>
        /// <remarks>Assign a scene camera target texture before submission so clipping uses its actual pixel grid.</remarks>
        /// <returns>A scope that completes this batch on disposal.</returns>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        public ObjectRenderScope BeginFrame()
        {
            ThrowIfDisposed();
            if (_scopeDepth++ == 0)
            {
                if (!_scenePool)
                {
                    _scenePool = true;
                    s_scenePools.Add(this);
                    _root.name = "PvZ Scene Sprite Pool";
                }

                _root.transform.position = Vector3.zero;
                BeginBatch();
                var target = s_sceneCamera == null ? null : s_sceneCamera.targetTexture;
                if (target != null)
                {
                    _pixelStepX = Math.Max(1, Screen.width) / (double)target.width;
                    _pixelStepY = Math.Max(1, Screen.height) / (double)target.height;
                }
            }

            return new ObjectRenderScope(this);
        }

        /// <summary>Publishes the completed objects for the scene camera after the outermost scope ends.</summary>
        public void EndFrame()
        {
            if (_scopeDepth == 0 || --_scopeDepth > 0)
            {
                return;
            }

            PublishBatches();
            SetRenderersEnabled(true);
            _ = SceneCamera;
        }

        /// <summary>Clears the submitted command count while retaining reusable scene objects.</summary>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        public void BeginBatch()
        {
            ThrowIfDisposed();
            SetRenderersEnabled(false);
            _used = 0;
            _batchUsed = 0;
            _pixelStepX = 1d;
            _pixelStepY = 1d;
            _vertexCount = 0;
            _triangleCount = 0;
            _culledCount = 0;
            _coveredPixelArea = 0d;
            _materialSplits = 0;
            _painterSplits = 0;
            _capacitySplits = 0;
            _batchVersion++;
            RetireDestroyedTextureBindings();
        }

        /// <summary>Appends a standalone texture quad to a compatible combined mesh.</summary>
        /// <param name = "texture">The source texture or previously rendered surface.</param>
        /// <param name = "width">The local width of the quad in pixels.</param>
        /// <param name = "height">The local height of the quad in pixels.</param>
        /// <param name = "uv">The normalized source texture rectangle.</param>
        /// <param name = "matrix">The source-to-destination pixel transform.</param>
        /// <param name = "color">The source color and opacity.</param>
        /// <param name = "mode">Zero for normal alpha, one for hit addition, two for particle addition, or three for premultiplied surfaces.</param>
        /// <param name = "grayscale">The amount of imitation-plant grayscale.</param>
        /// <param name = "producerAdditive">The source-colored producer brightness.</param>
        /// <param name = "additive">The amount of white hit addition.</param>
        /// <param name = "addColor">The additional hit color.</param>
        /// <param name="clipOverride">Optional destination-pixel clipping, also enabled for a local capture surface.</param>
        /// <param name="combineHit">Whether normal alpha also includes the original adjacent source-color hit.</param>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        /// <exception cref = "ArgumentOutOfRangeException">The blending mode is unsupported or the frame exceeds Unity's sorting-order range.</exception>
        public void DrawQuad(Texture texture, float width, float height, Rect uv, Matrix4x4 matrix, Color color, int mode, float grayscale = 0f, float producerAdditive = 0f, float additive = 0f, Color addColor = default, Rect? clipOverride = null, bool combineHit = false)
        {
            ThrowIfDisposed();
            DrawQuad(SpriteImage.FromTexture(texture), width, height, uv, matrix, color, mode, grayscale,
                producerAdditive, additive, addColor, clipOverride, combineHit);
        }

        /// <summary>Appends atlas regions to ordered combined meshes while retaining the original source command.</summary>
        /// <param name="image">The logical source image and its packed or standalone regions.</param>
        /// <param name="width">The original quad width in local pixels.</param>
        /// <param name="height">The original quad height in local pixels.</param>
        /// <param name="uv">The original bottom-left source image rectangle.</param>
        /// <param name="matrix">The source-to-destination pixel transform.</param>
        /// <param name="color">The original source tint and opacity.</param>
        /// <param name="mode">Zero for alpha, one for a standalone hit, two for addition, or three for a premultiplied surface.</param>
        /// <param name="grayscale">The amount of source grayscale.</param>
        /// <param name="producerAdditive">The source-colored producer brightness.</param>
        /// <param name="additive">The white source-color hit multiplier.</param>
        /// <param name="addColor">The additional source-color hit multiplier.</param>
        /// <param name="clipOverride">Optional destination pixel clipping, including local capture surfaces.</param>
        /// <param name="combineHit">Whether normal alpha includes its original immediately adjacent textured hit pass.</param>
        /// <exception cref="ObjectDisposedException">This pool has already been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The blending mode or painter order is unsupported.</exception>
        public void DrawQuad(SpriteImage image, float width, float height, Rect uv, Matrix4x4 matrix, Color color, int mode, float grayscale = 0f, float producerAdditive = 0f, float additive = 0f, Color addColor = default, Rect? clipOverride = null, bool combineHit = false)
        {
            using var submission = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Submit);
            ThrowIfDisposed();
            if (mode < 0 || mode > 3 || _used >= short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "The sprite mode or frame sorting range is invalid.");
            }

            SpriteHierarchy.RecordCurrentSprite(image, width, height, uv, matrix, color, mode);
            var node = SpriteHierarchy.CurrentSpriteObject;
            if (width == 0f || height == 0f || uv.width == 0f || uv.height == 0f || mode != 3 && color.a <= 0f
                || node != null && !node.activeInHierarchy)
            {
                _culledCount++;
                return;
            }

            var clipEnabled = clipOverride.HasValue || _scenePool && IsFrameActive;
            var clip = clipOverride ?? SpriteDrawState.Clip;
            if (clipEnabled && (clip.width <= 0f || clip.height <= 0f))
            {
                _culledCount++;
                return;
            }

            var order = -1;
            var scaleX = width / (uv.width * image.Width);
            var scaleY = height / (uv.height * image.Height);
            var originX = uv.x * image.Width;
            var sourceTop = (uv.y + uv.height) * image.Height;
            var regionCount = image.GetRegionCount(uv, _useTightGeometry);
            for (var regionIndex = 0; regionIndex < regionCount; regionIndex++)
            {
                if (!image.TryGetRegion(uv, regionIndex, _useTightGeometry, out var region))
                {
                    continue;
                }

                // Keep shared source-pixel edges identical between tight pieces. Reconstructing
                // geometry from separately rounded normalized rectangles can create one-pixel seams.
                var sourcePixels = region.SourcePixels;
                var left = (sourcePixels.xMin - originX) * scaleX;
                var right = (sourcePixels.xMax - originX) * scaleX;
                var top = (sourceTop - sourcePixels.yMax) * scaleY;
                var bottom = (sourceTop - sourcePixels.yMin) * scaleY;
                SetClipCorner(0, matrix, left, top, region.TextureUv.xMin, region.TextureUv.yMax);
                SetClipCorner(1, matrix, right, top, region.TextureUv.xMax, region.TextureUv.yMax);
                SetClipCorner(2, matrix, right, bottom, region.TextureUv.xMax, region.TextureUv.yMin);
                SetClipCorner(3, matrix, left, bottom, region.TextureUv.xMin, region.TextureUv.yMin);
                var vertexCount = clipEnabled ? ClipPolygon(clip) : 4;
                var area = PolygonArea(vertexCount);
                if (vertexCount < 3 || area == 0f)
                {
                    continue;
                }

                if (order < 0)
                {
                    order = _scenePool && IsFrameActive ? SpriteDrawState.NextOrder() : _used;
                    RecordSource(image, width, height, uv, matrix, color, mode, grayscale, producerAdditive,
                        additive, addColor, clip, clipEnabled, order, node);
                }

                var opaque = !_scenePool && mode == 0 && color.a >= 1f && _textureOpacity.IsOpaque(region.Texture);
                var material = GetMaterial(region.Texture, opaque ? 2 : mode == 1 || mode == 2 ? 1 : 0);
                var batch = GetBatch(material, order, vertexCount, opaque);
                var firstVertex = batch.VertexCount;
                var effects = new Vector4(grayscale, producerAdditive, mode, combineHit ? 1f : 0f);
                var highlight = new Vector4(addColor.r + additive, addColor.g + additive, addColor.b + additive, 0f);
                var depth = -order / 32768f;
                for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    var vertex = _clipInput[vertexIndex];
                    batch.AddVertex(new Vector3(vertex.Position.x, -vertex.Position.y, depth), vertex.Coordinates,
                        color, effects, highlight);
                }

                batch.CompletePolygon(firstVertex, vertexCount, order);
                _vertexCount += vertexCount;
                _triangleCount += vertexCount - 2;
                _coveredPixelArea += area;
            }

            if (order < 0)
            {
                _culledCount++;
            }
        }

        /// <summary>Renders the submitted objects into the supplied surface through a render pipeline request.</summary>
        /// <param name = "destination">The render texture receiving this batch.</param>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        /// <exception cref = "InvalidOperationException">The active pipeline does not support standard render requests.</exception>
        public void RenderTo(RenderTexture destination)
        {
            ThrowIfDisposed();
            var previousPosition = _root.transform.position;
            var previousVisibility = _batchUsed > 0 && _batches[0].Renderer.enabled;
            _camera.transform.localPosition = new Vector3(destination.width * 0.5f, -destination.height * 0.5f, -10f);
            _camera.orthographicSize = destination.height * 0.5f;
            _camera.aspect = destination.width / (float)destination.height;
            _request.destination = destination;
            if (!RenderPipeline.SupportsRenderRequest(_camera, _request))
            {
                throw new InvalidOperationException("PvZ sprite rendering requires the configured PvZ render pipeline.");
            }

            var previousActive = RenderTexture.active;
            try
            {
                _root.transform.position = _capturePosition;
                PublishBatches();
                SetRenderersEnabled(true);
                RenderPipeline.SubmitRenderRequest(_camera, _request);
                CaptureObserved?.Invoke(_camera, destination);
            }
            finally
            {
                RenderTexture.active = previousActive;
                SetRenderersEnabled(previousVisibility);
                _root.transform.position = previousPosition;
            }
        }

        /// <summary>Releases all meshes, materials, render textures, cameras, and pooled objects.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            SetRenderersEnabled(false);
            foreach (var batch in _batches)
            {
                DestroyResource(batch.Mesh);
            }

            foreach (var binding in _materials.Values)
            {
                foreach (var material in binding.Materials)
                {
                    DestroyResource(material);
                }
            }

            _materials.Clear();
            _batches.Clear();

            if (_scenePool)
            {
                s_scenePools.Remove(this);
                if (s_scenePools.Count == 0 && s_sceneCamera != null)
                {
                    s_sceneCamera.enabled = false;
                    DestroyResource(s_sceneCamera.gameObject);
                    s_sceneCamera = null;
                }
            }

            DestroyResource(_root);
            _sprites.Clear();
            _disposed = true;
        }

        /// <summary>Retains one original quad for source-level inspection without owning a GPU renderer.</summary>
        /// <param name="image">The original logical source image.</param>
        /// <param name="width">The original source quad width.</param>
        /// <param name="height">The original source quad height.</param>
        /// <param name="uv">The source UV rectangle before remapping.</param>
        /// <param name="matrix">The original destination transform.</param>
        /// <param name="color">The source tint.</param>
        /// <param name="mode">The source blending operation.</param>
        /// <param name="grayscale">The original grayscale amount.</param>
        /// <param name="producerAdditive">The original producer brightness.</param>
        /// <param name="additive">The original white hit amount.</param>
        /// <param name="addColor">The original colored hit amount.</param>
        /// <param name="clip">The accepted destination bounds.</param>
        /// <param name="clipEnabled">Whether destination clipping applies.</param>
        /// <param name="order">The shared source painter order.</param>
        /// <param name="node">The cached logical source object associated with this command.</param>
        private void RecordSource(SpriteImage image, float width, float height, Rect uv, Matrix4x4 matrix, Color color,
            int mode, float grayscale, float producerAdditive, float additive, Color addColor, Rect clip, bool clipEnabled, int order, GameObject? node)
        {
            if (_used == _sprites.Count)
            {
                _sprites.Add(new ObjectSprite());
            }

            var sprite = _sprites[_used++];
            sprite.GameObject = node;
            sprite.SourceImage = image;
            var depth = -order / 32768f;
            SetCorner(sprite, 0, matrix, 0f, 0f, depth, color);
            SetCorner(sprite, 1, matrix, width, 0f, depth, color);
            SetCorner(sprite, 2, matrix, width, height, depth, color);
            SetCorner(sprite, 3, matrix, 0f, height, depth, color);
            sprite.TextureCoordinates[0] = new Vector2(uv.xMin, uv.yMax);
            sprite.TextureCoordinates[1] = new Vector2(uv.xMax, uv.yMax);
            sprite.TextureCoordinates[2] = new Vector2(uv.xMax, uv.yMin);
            sprite.TextureCoordinates[3] = new Vector2(uv.xMin, uv.yMin);
            sprite.Mode = mode;
            sprite.Grayscale = grayscale;
            sprite.ProducerAdditive = producerAdditive;
            sprite.Additive = additive;
            sprite.AddColor = addColor;
            sprite.Clip = clip;
            sprite.ClipEnabled = clipEnabled;
        }

        /// <summary>Releases bindings whose source texture has been destroyed after the preceding batch is disabled.</summary>
        private void RetireDestroyedTextureBindings()
        {
            _retiredTextures.Clear();
            foreach (var entry in _materials)
            {
                if (entry.Key == null)
                {
                    _retiredTextures.Add(entry.Key);
                }
            }

            foreach (var texture in _retiredTextures)
            {
                foreach (var material in _materials[texture].Materials)
                {
                    DestroyResource(material);
                }

                _materials.Remove(texture);
            }

            _retiredTextures.Clear();
        }

        /// <summary>Gets an immutable binding for one texture and compatible hardware blend state.</summary>
        /// <param name="texture">The packed page or standalone surface sampled by the batch.</param>
        /// <param name="blend">Zero for premultiplied alpha, one for addition, or two for opaque replacement.</param>
        /// <returns>The shared material for this texture and blend state.</returns>
        private Material GetMaterial(Texture texture, int blend)
        {
            if (!_materials.TryGetValue(texture, out var binding))
            {
                var materials = new Material[3];
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = new Material(_shader)
                    {
                        name = $"PvZ batch {texture.name} {index}",
                        hideFlags = HideFlags.DontSave,
                        mainTexture = texture
                    };
                    material.SetInt("_SrcBlend", (int)(index == 1 ? BlendMode.SrcAlpha : BlendMode.One));
                    material.SetInt("_DstBlend", (int)(index == 1 ? BlendMode.One : index == 2 ? BlendMode.Zero : BlendMode.OneMinusSrcAlpha));
                    material.SetInt("_SrcBlendAlpha", (int)(index == 1 ? BlendMode.Zero : BlendMode.One));
                    material.SetInt("_DstBlendAlpha", (int)(index == 1 ? BlendMode.One : index == 2 ? BlendMode.Zero : BlendMode.OneMinusSrcAlpha));
                    material.SetInt("_ZWrite", index == 2 ? 1 : 0);
                    if (index == 2)
                    {
                        material.renderQueue = (int)RenderQueue.Geometry;
                        material.SetOverrideTag("RenderType", "Opaque");
                    }

                    materials[index] = material;
                }

                binding = new SpriteMaterialBinding(materials, _batchVersion);
                _materials.Add(texture, binding);
            }
            else if (binding.LastBatchVersion != _batchVersion)
            {
                // Resources.UnloadAsset can clear a native material texture while the logical
                // cache key survives. Always use the current sampled asset, including reloaded wrappers.
                foreach (var material in binding.Materials)
                {
                    material.mainTexture = texture;
                }

                binding.LastBatchVersion = _batchVersion;
            }

            return binding.Materials[blend];
        }

        /// <summary>Gets a compatible contiguous batch, splitting on material, sorting barriers, or index limits.</summary>
        /// <param name="material">The texture and hardware blend binding.</param>
        /// <param name="order">The original source painter order.</param>
        /// <param name="vertexCount">The number of clipped vertices to append.</param>
        /// <param name="opaque">Whether the batch uses depth-writing opaque replacement.</param>
        /// <returns>The reusable combined mesh receiving the next polygon.</returns>
        private SpriteMeshBatch GetBatch(Material material, int order, int vertexCount, bool opaque)
        {
            if (_batchUsed > 0 && _batches[_batchUsed - 1].CanAppend(material, order, vertexCount))
            {
                return _batches[_batchUsed - 1];
            }

            if (_batchUsed > 0)
            {
                var previous = _batches[_batchUsed - 1];
                if (!ReferenceEquals(previous.Material, material))
                {
                    _materialSplits++;
                }

                if (order != previous.LastOrder && order != previous.LastOrder + 1)
                {
                    _painterSplits++;
                }

                if (previous.VertexCount + vertexCount > SpriteMeshBatch.MaximumVertexCount)
                {
                    _capacitySplits++;
                }
            }

            if (_batchUsed == _batches.Count)
            {
                _batches.Add(new SpriteMeshBatch(_root.transform, _batchUsed));
            }

            var batch = _batches[_batchUsed++];
            batch.Reset(material, order, opaque);
            return batch;
        }

        /// <summary>Uploads each completed mesh once before a display or local render request.</summary>
        private void PublishBatches()
        {
            for (var index = 0; index < _batchUsed; index++)
            {
                _batches[index].Publish();
            }
        }

        /// <summary>Writes an original transformed corner into the reusable clipping polygon.</summary>
        /// <param name="index">The destination polygon vertex.</param>
        /// <param name="matrix">The source-to-destination transform.</param>
        /// <param name="x">The source horizontal coordinate.</param>
        /// <param name="y">The source vertical coordinate.</param>
        /// <param name="u">The atlas horizontal coordinate.</param>
        /// <param name="v">The atlas vertical coordinate.</param>
        private void SetClipCorner(int index, Matrix4x4 matrix, float x, float y, float u, float v)
        {
            var destinationX = (matrix.m00 * x) + (matrix.m01 * y) + matrix.m03;
            var destinationY = (matrix.m10 * x) + (matrix.m11 * y) + matrix.m13;
            _clipInput[index] = new SpriteClipVertex(new Vector2(destinationX, destinationY), new Vector2(u, v));
        }

        /// <summary>Clips one convex quad in destination pixels while interpolating its original texture coordinates.</summary>
        /// <param name="clip">The accepted top-left destination rectangle.</param>
        /// <returns>The number of vertices retained in the reusable input polygon.</returns>
        private int ClipPolygon(Rect clip)
        {
            var first = _clipInput[0].Position;
            var minimumX = first.x;
            var maximumX = first.x;
            var minimumY = first.y;
            var maximumY = first.y;
            for (var index = 1; index < 4; index++)
            {
                var point = _clipInput[index].Position;
                minimumX = Math.Min(minimumX, point.x);
                maximumX = Math.Max(maximumX, point.x);
                minimumY = Math.Min(minimumY, point.y);
                maximumY = Math.Max(maximumY, point.y);
            }

            if (minimumX >= clip.xMin && maximumX <= clip.xMax && minimumY >= clip.yMin && maximumY <= clip.yMax)
            {
                return 4;
            }

            clip = RasterClip(clip);
            if (clip.width <= 0f || clip.height <= 0f || maximumX <= clip.xMin || minimumX >= clip.xMax
                || maximumY <= clip.yMin || minimumY >= clip.yMax)
            {
                return 0;
            }

            var count = 4;
            for (var boundary = 0; boundary < 4 && count > 0; boundary++)
            {
                var outputCount = 0;
                var previous = _clipInput[count - 1];
                var previousDistance = ClipDistance(previous.Position, clip, boundary);
                for (var index = 0; index < count; index++)
                {
                    var current = _clipInput[index];
                    var currentDistance = ClipDistance(current.Position, clip, boundary);
                    if ((previousDistance >= 0f) != (currentDistance >= 0f))
                    {
                        var fraction = previousDistance / (previousDistance - currentDistance);
                        _clipOutput[outputCount++] = SpriteClipVertex.Interpolate(previous, current, fraction);
                    }

                    if (currentDistance >= 0f)
                    {
                        _clipOutput[outputCount++] = current;
                    }

                    previous = current;
                    previousDistance = currentDistance;
                }

                var temporary = _clipInput;
                _clipInput = _clipOutput;
                _clipOutput = temporary;
                count = outputCount;
            }

            return count;
        }

        /// <summary>Maps inclusive fragment bounds to half-open geometry bounds on the actual destination sample grid.</summary>
        /// <param name="clip">The original destination-coordinate clipping rectangle.</param>
        /// <returns>Geometry bounds containing exactly the pixel centers accepted by the original fragment shader.</returns>
        private Rect RasterClip(Rect clip)
        {
            if (!_hasCachedRasterClip || !_cachedClip.Equals(clip) || _cachedPixelStepX != _pixelStepX || _cachedPixelStepY != _pixelStepY)
            {
                var minimumX = SampleBoundary(clip.xMin, _pixelStepX, false);
                var maximumX = SampleBoundary(clip.xMax, _pixelStepX, true);
                var minimumY = SampleBoundary(clip.yMin, _pixelStepY, false);
                var maximumY = SampleBoundary(clip.yMax, _pixelStepY, true);
                _cachedRasterClip = Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
                _cachedClip = clip;
                _cachedPixelStepX = _pixelStepX;
                _cachedPixelStepY = _pixelStepY;
                _hasCachedRasterClip = true;
            }

            return _cachedRasterClip;
        }

        /// <summary>Resolves one inclusive bound without losing ties when a nonintegral pixel spacing rounds to float.</summary>
        /// <param name="bound">The original inclusive fragment bound.</param>
        /// <param name="step">The exact destination-coordinate pixel spacing.</param>
        /// <param name="maximum">Whether this is a maximum rather than minimum bound.</param>
        /// <returns>The corresponding half-open raster edge in destination coordinates.</returns>
        private static float SampleBoundary(float bound, double step, bool maximum)
        {
            if (maximum)
            {
                var sample = Math.Floor((bound / step) - 0.5d);
                if ((float)((sample + 1.5d) * step) <= bound)
                {
                    sample++;
                }
                else if ((float)((sample + 0.5d) * step) > bound)
                {
                    sample--;
                }

                return (float)((sample + 1d) * step);
            }

            var first = Math.Ceiling((bound / step) - 0.5d);
            if ((float)((first - 0.5d) * step) >= bound)
            {
                first--;
            }
            else if ((float)((first + 0.5d) * step) < bound)
            {
                first++;
            }

            return (float)(first * step);
        }

        /// <summary>Rejects zero-area transformed or boundary-only polygons before reserving painter order.</summary>
        /// <param name="count">The number of vertices in the reusable input polygon.</param>
        /// <returns>The nonnegative covered destination area, or zero for empty or degenerate geometry.</returns>
        private float PolygonArea(int count)
        {
            if (count < 3)
            {
                return 0f;
            }

            var area = 0f;
            var previous = _clipInput[count - 1].Position;
            for (var index = 0; index < count; index++)
            {
                var current = _clipInput[index].Position;
                area += previous.x * current.y - current.x * previous.y;
                previous = current;
            }

            return Mathf.Abs(area) * 0.5f;
        }

        /// <summary>Computes the signed distance to one axis-aligned clipping boundary.</summary>
        /// <param name="point">The destination pixel position.</param>
        /// <param name="clip">The accepted destination rectangle.</param>
        /// <param name="boundary">Left, right, top, or bottom as zero through three.</param>
        /// <returns>A nonnegative value inside the selected boundary.</returns>
        private static float ClipDistance(Vector2 point, Rect clip, int boundary)
        {
            return boundary switch
            {
                0 => point.x - clip.xMin,
                1 => clip.xMax - point.x,
                2 => point.y - clip.yMin,
                _ => clip.yMax - point.y
            };
        }

        /// <summary>Writes one transformed pixel-space corner using Unity's upward-positive coordinates.</summary>
        /// <param name = "sprite">The reusable quad receiving the corner.</param>
        /// <param name = "index">The corner to replace.</param>
        /// <param name = "matrix">The source-to-destination pixel transform.</param>
        /// <param name = "x">The source horizontal coordinate.</param>
        /// <param name = "y">The source vertical coordinate.</param>
        /// <param name = "color">The vertex tint.</param>
        /// <param name="depth">The depth preserving the original painter order across render queues.</param>
        private static void SetCorner(ObjectSprite sprite, int index, Matrix4x4 matrix, float x, float y, float depth, Color color)
        {
            var destinationX = (matrix.m00 * x) + (matrix.m01 * y) + matrix.m03;
            var destinationY = (matrix.m10 * x) + (matrix.m11 * y) + matrix.m13;
            sprite.Vertices[index] = new Vector3(destinationX, -destinationY, depth);
            sprite.Colors[index] = color;
        }

        /// <summary>Changes visibility of the active commands while leaving unused pool slots disabled.</summary>
        /// <param name="enabled">Whether the current batch should be visible to a camera.</param>
        private void SetRenderersEnabled(bool enabled)
        {
            for (var index = 0; index < _batchUsed; index++)
            {
                _batches[index].Renderer.enabled = enabled;
            }
        }

        /// <summary>Creates a camera with the unlit settings shared by scene and cropped-surface rendering.</summary>
        /// <param name="cameraObject">The object that owns the new camera.</param>
        /// <returns>The disabled orthographic camera.</returns>
        private static Camera CreateCamera(GameObject cameraObject)
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            return camera;
        }

        /// <summary>Matches the display camera to the presenter's top-left screen pixel coordinate system.</summary>
        /// <param name="camera">The direct-display camera to update after a window resize.</param>
        private static void ConfigureSceneCamera(Camera camera)
        {
            var width = Math.Max(1, Screen.width);
            var height = Math.Max(1, Screen.height);
            camera.transform.position = new Vector3(width * 0.5f, -height * 0.5f, -10f);
            camera.orthographicSize = height * 0.5f;
            camera.aspect = width / (float)height;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        /// <summary>Rejects commands issued after the pool has released its Unity resources.</summary>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectSpriteRenderer));
            }
        }

        /// <summary>Destroys a resource using the correct editor or player lifetime operation.</summary>
        /// <param name = "resource">The Unity resource owned by this pool.</param>
        private static void DestroyResource(UnityEngine.Object resource)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }
    }
}
