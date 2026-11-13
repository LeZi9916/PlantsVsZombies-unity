#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Shares logical images, packaged original texts, and lazily loaded atlas pages for one resource root.</summary>
    /// <remarks>Use on Unity's main thread and release each <see cref="ForRoot"/> acquisition once.</remarks>
    public sealed class SpriteAssetProvider : IDisposable
    {
        /// <summary>Bounds retained image/crop combinations while allowing a busy board to reuse all common animation parts.</summary>
        private const int MaximumRegionCacheEntries = 512;

        /// <summary>Identifies immutable prepared geometry independently of transient painter state.</summary>
        private readonly struct RegionCacheKey : IEquatable<RegionCacheKey>
        {
            /// <summary>The canonical original image identity.</summary>
            private readonly string _identity;
            /// <summary>The exact normalized original source request, including either reversed axis.</summary>
            private readonly Rect _sourceUv;
            /// <summary>Whether transparent cell rectangles participate in this cached geometry.</summary>
            private readonly bool _useTightGeometry;

            /// <summary>Creates a value-type cache lookup without allocating a composite string.</summary>
            /// <param name="identity">The original image identity.</param>
            /// <param name="sourceUv">The requested original normalized rectangle.</param>
            /// <param name="useTightGeometry">Whether transparent cell pruning is enabled.</param>
            internal RegionCacheKey(string identity, Rect sourceUv, bool useTightGeometry)
            {
                _identity = identity;
                _sourceUv = sourceUv;
                _useTightGeometry = useTightGeometry;
            }

            /// <summary>Checks exact source identity and rectangle equality.</summary>
            /// <param name="other">The other image/crop key.</param>
            /// <returns>True when both keys represent the same source sampling request.</returns>
            public bool Equals(RegionCacheKey other)
            {
                return string.Equals(_identity, other._identity, StringComparison.Ordinal) && _sourceUv.Equals(other._sourceUv) && _useTightGeometry == other._useTightGeometry;
            }

            /// <summary>Checks an object for equality with this source request.</summary>
            /// <param name="obj">The other object to compare.</param>
            /// <returns>True when the object contains an equal image/crop key.</returns>
            public override bool Equals(object? obj)
            {
                return obj is RegionCacheKey other && Equals(other);
            }

            /// <summary>Hashes the source identity and exact rectangle using their nonallocating value implementations.</summary>
            /// <returns>The dictionary hash for this image/crop request.</returns>
            public override int GetHashCode()
            {
                return unchecked(((_identity.GetHashCode() * 397) ^ _sourceUv.GetHashCode()) * 397 ^ (_useTightGeometry ? 1 : 0));
            }
        }

        /// <summary>Retains one bounded prepared image/crop result across different intervening sprite submissions.</summary>
        private sealed class RegionCacheEntry
        {
            /// <summary>The reusable paired geometry and imported page references.</summary>
            internal SpriteImageRegion[] Regions = Array.Empty<SpriteImageRegion>();
            /// <summary>The valid region count in the reusable array.</summary>
            internal int Count;
            /// <summary>The complete layout generation used by the cached page mappings.</summary>
            internal int LayoutVersion = -1;
            /// <summary>The page-retirement generation used by the cached texture references.</summary>
            internal int StorageVersion = -1;
            /// <summary>The monotonic usage stamp used for bounded least-recently-used replacement.</summary>
            internal long LastUse;
        }

        /// <summary>Touches each original image's page residency at most once per complete scene frame.</summary>
        private sealed class ImageFrameState
        {
            /// <summary>The complete frame whose required pages have been prepared.</summary>
            internal int Frame = -1;
            /// <summary>The complete layout generation supplying the metadata.</summary>
            internal int LayoutVersion = -1;
            /// <summary>The page-retirement generation supplying the imported page references.</summary>
            internal int StorageVersion = -1;
            /// <summary>The metadata for the successfully prepared complete layout.</summary>
            internal SpriteAtlasImage? Image;
        }

        /// <summary>The generated catalog's extensionless Resources path.</summary>
        public const string CatalogResourcePath = "PvZRendering/catalog";
        /// <summary>The providers shared by renderers using the same original resource directory.</summary>
        private static readonly Dictionary<string, SpriteAssetProvider> s_providers = new Dictionary<string, SpriteAssetProvider>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The normalized original resource root, or an empty string in injected catalog tests.</summary>
        private readonly string _root;
        /// <summary>The complete catalog, or null for an external loose resource directory.</summary>
        private readonly SpriteAtlasCatalog? _catalog;
        /// <summary>The callback loading one imported texture page without retaining a readable CPU copy.</summary>
        private readonly Func<string, Texture2D?> _loadPage;
        /// <summary>The callback releasing a page after all submitted frames have retired.</summary>
        private readonly Action<Texture2D> _releasePage;
        /// <summary>The hardware dimension limit captured before any page is loaded.</summary>
        private readonly int _hardwareMaximum;
        /// <summary>The case-insensitive mapping from original IDs and paths to selected-layout metadata.</summary>
        private readonly Dictionary<string, SpriteAtlasImage> _metadata = new Dictionary<string, SpriteAtlasImage>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The stable logical handles retained across whole-layout fallback.</summary>
        private readonly Dictionary<string, SpriteImage> _images = new Dictionary<string, SpriteImage>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The imported pages loaded for the currently selected complete layout.</summary>
        private readonly Dictionary<int, Texture2D> _pages = new Dictionary<int, Texture2D>();
        /// <summary>The last complete scene frame referencing each current-layout page.</summary>
        private readonly Dictionary<int, int> _pageLastSeen = new Dictionary<int, int>();
        /// <summary>The reusable list of inactive page IDs awaiting safe eviction.</summary>
        private readonly List<int> _unusedPageIds = new List<int>();
        /// <summary>The bounded cross-image geometry cache, populated only by actually sampled source requests.</summary>
        private readonly Dictionary<RegionCacheKey, RegionCacheEntry> _regionCache = new Dictionary<RegionCacheKey, RegionCacheEntry>(MaximumRegionCacheEntries);
        /// <summary>The page-residency states retained for original images sampled during this provider's lifetime.</summary>
        private readonly Dictionary<string, ImageFrameState> _imageFrameStates = new Dictionary<string, ImageFrameState>(StringComparer.Ordinal);
        /// <summary>The reusable scratch buffer used only when an image/crop cache entry must be built.</summary>
        private SpriteImageRegion[] _regionBuildBuffer = Array.Empty<SpriteImageRegion>();
        /// <summary>The currently selected cached array, read directly by indexed region access.</summary>
        private SpriteImageRegion[] _preparedRegions = Array.Empty<SpriteImageRegion>();
        /// <summary>The monotonic usage stamp assigned to the most recently selected bounded cache entry.</summary>
        private long _regionCacheUse;
        /// <summary>The logical image whose cropped regions currently occupy the reusable output buffer.</summary>
        private string? _preparedIdentity;
        /// <summary>The exact requested source rectangle used to prepare the reusable regions.</summary>
        private Rect _preparedSourceUv;
        /// <summary>The geometry policy of the currently prepared source request.</summary>
        private bool _preparedTightGeometry;
        /// <summary>The number of valid entries in the reusable region buffer.</summary>
        private int _preparedCount;
        /// <summary>The atlas layout generation used by the reusable regions.</summary>
        private int _preparedLayoutVersion = -1;
        /// <summary>The page-storage generation used by the reusable regions.</summary>
        private int _preparedStorageVersion = -1;
        /// <summary>The complete scene frame that last touched the prepared image's page residency.</summary>
        private int _preparedFrame = -1;
        /// <summary>The generation incremented when imported page references retire or their complete layout changes.</summary>
        private int _pageStorageVersion;
        /// <summary>The complete scene frame whose inactive page collection has already run.</summary>
        private int _lastCollectionFrame = -1;
        /// <summary>The previous-layout pages still referenced by recently submitted frames.</summary>
        private readonly List<(Texture2D Texture, int Frame)> _retiredPages = new List<(Texture2D, int)>();
        /// <summary>The standalone decoded textures owned only by a loose-source provider.</summary>
        private readonly List<Texture2D> _looseTextures = new List<Texture2D>();
        /// <summary>The original image paths indexed once for an external resource directory.</summary>
        private readonly Dictionary<string, string> _loosePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The packaged text path lookup, independent of selected page size.</summary>
        private readonly Dictionary<string, SpriteAtlasText> _textPaths = new Dictionary<string, SpriteAtlasText>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The original texts retained after their first parse request.</summary>
        private readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The index of the active complete layout.</summary>
        private int _layoutIndex;
        /// <summary>The number of caller acquisitions still retaining this provider.</summary>
        private int _references = 1;
        /// <summary>Whether this provider has released its resources.</summary>
        private bool _disposed;
        /// <summary>Gets the layout generation, incremented when the entire layout falls back.</summary>
        public int LayoutVersion { get; private set; }
        /// <summary>Gets the selected maximum page size, or zero for an external loose source.</summary>
        public int MaxPageSize => _catalog == null ? 0 : _catalog.Layouts[_layoutIndex].MaximumPageSize;
        /// <summary>Gets whether this provider resolves imported baked resources.</summary>
        public bool IsBaked => _catalog != null;
        /// <summary>Gets the original relative paths of all available images, without loading texture pages.</summary>
        public IReadOnlyList<string> ImagePaths { get; private set; } = Array.Empty<string>();
        /// <summary>Gets the original relative paths of all packaged animation and particle texts.</summary>
        public IReadOnlyList<string> TextPaths { get; private set; } = Array.Empty<string>();
        /// <summary>Gets the number of current-layout pages currently resident.</summary>
        internal int LoadedPageCount => _pages.Count;
        /// <summary>Gets the bounded number of retained original image/crop combinations.</summary>
        internal int PreparedRegionCacheCount => _regionCache.Count;

        /// <summary>Acquires the shared provider for production assets or an external loose test directory.</summary>
        /// <param name="root">The original runtime asset root.</param>
        /// <returns>A provider acquisition that must be disposed once by the caller.</returns>
        /// <exception cref="InvalidOperationException">Production assets have no valid baked catalog or supported layout.</exception>
        /// <exception cref="ArgumentException">The supplied original resource root is empty.</exception>
        /// <exception cref="IOException">An external original resource directory or manifest cannot be read.</exception>
        /// <exception cref="XmlException">An external original resource manifest is malformed.</exception>
        public static SpriteAssetProvider ForRoot(string root)
        {
            var normalized = NormalizeProviderRoot(root);
            if (s_providers.TryGetValue(normalized, out var shared))
            {
                shared._references++;
                return shared;
            }
            var streaming = NormalizeRoot(Application.streamingAssetsPath);
            var production = string.Equals(normalized, streaming, StringComparison.OrdinalIgnoreCase);
            var provider = new SpriteAssetProvider(normalized, production);
            s_providers.Add(normalized, provider);
            return provider;
        }

        /// <summary>Canonicalizes local resource roots while preserving Android jar and other platform URLs.</summary>
        /// <param name="root">A local filesystem directory or an unchanged platform resource URL.</param>
        /// <returns>A forward-slash local absolute path or the preserved platform URL without a trailing slash.</returns>
        /// <exception cref="ArgumentException">The supplied root is empty.</exception>
        public static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("A resource root is required.", nameof(root));
            }
            var normalized = Normalize(root);
            if (normalized.StartsWith("jar:", StringComparison.OrdinalIgnoreCase) || normalized.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                return normalized.TrimEnd('/');
            }
            return Normalize(Path.GetFullPath(normalized)).TrimEnd('/');
        }

        /// <summary>Shares current and legacy production roots so one provider owns each imported atlas page.</summary>
        /// <param name="root">The original local directory or platform URL.</param>
        /// <returns>The canonical provider key, using the current StreamingAssets root for both production layouts.</returns>
        internal static string NormalizeProviderRoot(string root)
        {
            var normalized = NormalizeRoot(root);
            var streaming = NormalizeRoot(Application.streamingAssetsPath);
            return string.Equals(normalized, streaming + "/PvZ", StringComparison.OrdinalIgnoreCase) ? streaming : normalized;
        }

        /// <summary>Evicts inactive imported pages after their published scene frames have safely retired.</summary>
        internal static void OnSceneFrameStarted()
        {
            ObjectSpriteRenderer.FlushPending();
            foreach (var provider in s_providers.Values)
            {
                provider.CollectRetiredPages();
            }
        }

        /// <summary>Creates a provider and indexes metadata before loading any page.</summary>
        /// <param name="root">The normalized original resource directory.</param>
        /// <param name="production">Whether generated Resources assets are required.</param>
        private SpriteAssetProvider(string root, bool production)
        {
            _root = root;
            _loadPage = LoadResourcePage;
            _releasePage = ReleaseResourcePage;
            _hardwareMaximum = SystemInfo.maxTextureSize;
            if (production)
            {
                var asset = Resources.Load<TextAsset>(CatalogResourcePath);
                if (asset == null)
                {
                    throw new InvalidOperationException("The generated PvZ rendering catalog is missing. Generate rendering atlases with the required Unity Editor before playing or building.");
                }
                _catalog = JsonUtility.FromJson<SpriteAtlasCatalog>(asset.text);
                InitializeCatalog();
            }
            else
            {
                IndexLooseSources();
            }
        }

        /// <summary>Creates a catalog provider with injected page storage for deterministic regression tests.</summary>
        /// <param name="catalog">The complete test catalog.</param>
        /// <param name="hardwareMaximum">The simulated graphics dimension limit.</param>
        /// <param name="loadPage">The page loader, invoked only after selecting a compatible layout.</param>
        /// <param name="releasePage">The callback releasing a test page.</param>
        internal SpriteAssetProvider(SpriteAtlasCatalog catalog, int hardwareMaximum, Func<string, Texture2D?> loadPage, Action<Texture2D> releasePage)
        {
            _root = string.Empty;
            _catalog = catalog;
            _hardwareMaximum = hardwareMaximum;
            _loadPage = loadPage;
            _releasePage = releasePage;
            InitializeCatalog();
        }

        /// <summary>Validates the catalog and selects dimensions before resolving any texture references.</summary>
        /// <exception cref="InvalidOperationException">The catalog version or hardware dimensions are unsupported.</exception>
        private void InitializeCatalog()
        {
            if (_catalog == null || _catalog.Version != SpriteAtlasCatalog.CurrentVersion || _catalog.Layouts == null || _catalog.Layouts.Length == 0)
            {
                throw new InvalidOperationException("The generated rendering catalog is invalid or has an unsupported version.");
            }
            _layoutIndex = SpriteGeometryUtility.SelectLayout(_catalog.Layouts, _hardwareMaximum);
            if (_layoutIndex < 0)
            {
                throw new InvalidOperationException("No baked atlas layout fits the graphics device maximum texture size: " + _hardwareMaximum);
            }
            SelectMetadata();
            var texts = new string[_catalog.Texts.Length];
            for (var index = 0; index < texts.Length; index++)
            {
                var entry = _catalog.Texts[index];
                texts[index] = entry.SourceIdentity;
                _textPaths[entry.SourceIdentity] = entry;
            }
            TextPaths = texts;
        }

        /// <summary>Replaces the entire source mapping after selecting a different complete layout.</summary>
        private void SelectMetadata()
        {
            _metadata.Clear();
            var entries = _catalog!.Layouts[_layoutIndex].Images;
            var paths = new string[entries.Length];
            for (var index = 0; index < entries.Length; index++)
            {
                var image = entries[index];
                if (image.Width <= 0 || image.Height <= 0)
                {
                    throw new InvalidOperationException("Atlas source dimensions must be positive: " + image.SourceIdentity);
                }
                paths[index] = image.SourceIdentity;
                _metadata[image.SourceIdentity] = image;
                foreach (var alias in image.Aliases)
                {
                    _metadata[alias] = image;
                }
            }
            ImagePaths = paths;
            LayoutVersion++;
        }

        /// <summary>Loads a logical image by original resource ID or original relative filename.</summary>
        /// <param name="imageId">An IMAGE identifier, a relative image path, or a bare images filename.</param>
        /// <returns>The stable logical image, without allocating a source texture for baked images.</returns>
        /// <exception cref="FileNotFoundException">The original image is absent.</exception>
        /// <exception cref="ObjectDisposedException">The provider has already been released.</exception>
        public SpriteImage LoadImage(string imageId)
        {
            ThrowIfDisposed();
            var key = Normalize(imageId);
            if (_images.TryGetValue(key, out var image))
            {
                return image;
            }
            if (_catalog != null)
            {
                if (!_metadata.TryGetValue(key, out var metadata) && !_metadata.TryGetValue("images/" + key, out metadata))
                {
                    throw new FileNotFoundException("Image is absent from the baked rendering catalog: " + imageId);
                }
                if (!_images.TryGetValue(metadata.SourceIdentity, out image))
                {
                    image = new SpriteImage(this, metadata);
                    _images[metadata.SourceIdentity] = image;
                }
            }
            else
            {
                var relative = ResolveLooseImage(key);
                if (!_images.TryGetValue(relative, out image))
                {
                    var texture = DecodeLooseImage(Path.Combine(_root, relative));
                    image = new SpriteImage(texture, relative);
                    _images[relative] = image;
                    _looseTextures.Add(texture);
                }
            }
            _images[key] = image;
            return image;
        }

        /// <summary>Checks an original image identifier without loading atlas pages or decoding a loose image.</summary>
        /// <param name="imageId">The original resource identifier, bare filename, or relative image path.</param>
        /// <returns>True when the source metadata contains the image.</returns>
        /// <exception cref="ObjectDisposedException">The provider has already released its resources.</exception>
        public bool ContainsImage(string imageId)
        {
            ThrowIfDisposed();
            var key = Normalize(imageId);
            return _catalog != null ? _metadata.ContainsKey(key) || _metadata.ContainsKey("images/" + key)
                : _loosePaths.ContainsKey(key) || _loosePaths.ContainsKey("images/" + key);
        }

        /// <summary>Loads an optional logical image without using exceptions for ordinary missing artwork.</summary>
        /// <param name="imageId">The original resource identifier, bare filename, or relative image path.</param>
        /// <param name="image">Receives the logical image when its source exists.</param>
        /// <returns>True when the requested source exists and resolves successfully.</returns>
        /// <exception cref="ObjectDisposedException">The provider has already released its resources.</exception>
        /// <exception cref="IOException">An existing external original image cannot be read or decoded.</exception>
        public bool TryLoadImage(string imageId, [NotNullWhen(true)] out SpriteImage? image)
        {
            if (!ContainsImage(imageId))
            {
                image = null;
                return false;
            }
            image = LoadImage(imageId);
            return true;
        }

        /// <summary>Loads artwork with an explicitly selected red-channel alpha grid.</summary>
        /// <param name="colorId">The color image's relative path or bare filename.</param>
        /// <param name="maskId">The alpha grid's relative path or bare filename.</param>
        /// <returns>The retained image containing the original color and selected mask.</returns>
        /// <exception cref="FileNotFoundException">The requested pair was not baked or a loose source is missing.</exception>
        /// <exception cref="InvalidDataException">The external alpha mask is invalid or has different dimensions.</exception>
        /// <exception cref="ObjectDisposedException">The provider has already released its resources.</exception>
        public SpriteImage LoadAlphaGrid(string colorId, string maskId)
        {
            ThrowIfDisposed();
            var color = NormalizeImagePath(colorId);
            var mask = NormalizeImagePath(maskId);
            var key = color + "|" + mask;
            if (_images.TryGetValue(key, out var image))
            {
                return image;
            }
            if (_catalog != null)
            {
                return LoadImage(key);
            }
            var colorPath = Path.Combine(_root, ResolveLooseImage(color));
            var maskPath = Path.Combine(_root, ResolveLooseImage(mask));
            var texture = TextureCache.LoadMaskedImage(colorPath, maskPath);
            image = new SpriteImage(texture, key);
            _looseTextures.Add(texture);
            _images.Add(key, image);
            return image;
        }

        /// <summary>Reads a packaged original text or an external file without exposing platform URL paths.</summary>
        /// <param name="relativePath">The original relative animation, particle, or properties path.</param>
        /// <param name="text">Receives the unchanged original text.</param>
        /// <returns>True when the requested original text exists.</returns>
        /// <exception cref="InvalidOperationException">A catalog text entry refers to a missing imported resource.</exception>
        /// <exception cref="IOException">An external original text cannot be read.</exception>
        /// <exception cref="ObjectDisposedException">The provider has already released its resources.</exception>
        public bool TryReadText(string relativePath, out string text)
        {
            ThrowIfDisposed();
            var key = Normalize(relativePath);
            if (_texts.TryGetValue(key, out text))
            {
                return true;
            }
            if (_catalog != null)
            {
                if (!_textPaths.TryGetValue(key, out var entry))
                {
                    text = string.Empty;
                    return false;
                }
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (asset == null)
                {
                    throw new InvalidOperationException("A packaged original text is missing: " + relativePath);
                }
                text = asset.text;
            }
            else
            {
                var path = Path.Combine(_root, key);
                if (!File.Exists(path))
                {
                    text = string.Empty;
                    return false;
                }
                text = File.ReadAllText(path);
            }
            _texts[key] = text;
            return true;
        }

        /// <summary>Enumerates available original text paths for initialization-time animation indexing.</summary>
        /// <param name="directory">The original relative directory, such as reanim or particles.</param>
        /// <returns>The cached original text paths below the requested directory.</returns>
        public IEnumerable<string> EnumerateTextPaths(string directory)
        {
            var prefix = Normalize(directory).TrimEnd('/') + "/";
            foreach (var path in TextPaths)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    yield return path;
                }
            }
        }

        /// <summary>Prepares and borrows the complete cropped region list for synchronous consumption.</summary>
        /// <param name="identity">The original image identity.</param>
        /// <param name="sourceUv">The requested original source rectangle.</param>
        /// <param name="useTightGeometry">Whether transparent source pieces may be pruned.</param>
        /// <returns>A borrowed view invalidated by subsequent provider queries.</returns>
        internal SpriteRegionView GetRegions(string identity, Rect sourceUv, bool useTightGeometry)
        {
            PrepareRegions(identity, sourceUv, useTightGeometry);
            return new SpriteRegionView(_preparedRegions, _preparedCount);
        }

        /// <summary>Counts the cropped geometry pieces after preparing every page used by the logical image.</summary>
        /// <param name="identity">The original image identity.</param>
        /// <param name="sourceUv">The requested source rectangle.</param>
        /// <returns>The number of visible source intersections.</returns>
        /// <param name="useTightGeometry">Whether transparent cell rectangles may subdivide the source tiles.</param>
        internal int GetRegionCount(string identity, Rect sourceUv, bool useTightGeometry)
        {
            PrepareRegions(identity, sourceUv, useTightGeometry);
            return _preparedCount;
        }

        /// <summary>Resolves one source intersection to its already prepared atlas page.</summary>
        /// <param name="identity">The original image identity.</param>
        /// <param name="sourceUv">The requested source rectangle.</param>
        /// <param name="index">The visible intersection to return.</param>
        /// <param name="region">Receives paired positive original and texture UVs.</param>
        /// <returns>True when the requested visible intersection exists.</returns>
        /// <param name="useTightGeometry">Whether transparent cell rectangles may subdivide the source tiles.</param>
        internal bool TryGetRegion(string identity, Rect sourceUv, int index, bool useTightGeometry, out SpriteImageRegion region)
        {
            region = default;
            if (index < 0)
            {
                return false;
            }
            PrepareRegions(identity, sourceUv, useTightGeometry);
            if (index >= _preparedCount)
            {
                return false;
            }
            region = _preparedRegions[index];
            return true;
        }

        /// <summary>Prepares a sprite's complete cropped output once so indexed region access remains constant-time.</summary>
        /// <param name="identity">The logical original image identity.</param>
        /// <param name="sourceUv">The exact requested original source rectangle.</param>
        /// <param name="useTightGeometry">Whether transparent cell rectangles may subdivide the source tiles.</param>
        private void PrepareRegions(string identity, Rect sourceUv, bool useTightGeometry)
        {
            using var profile = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Regions);
            ThrowIfDisposed();
            var sameSource = string.Equals(_preparedIdentity, identity, StringComparison.Ordinal) && _preparedSourceUv.Equals(sourceUv) && _preparedTightGeometry == useTightGeometry;
            if (sameSource && _preparedLayoutVersion == LayoutVersion && _preparedStorageVersion == _pageStorageVersion
                && _preparedFrame == SpriteDrawState.FrameVersion)
            {
                return;
            }
            var image = PrepareImageForFrame(identity);
            var key = new RegionCacheKey(identity, sourceUv, useTightGeometry);
            if (!_regionCache.TryGetValue(key, out var entry))
            {
                entry = AcquireRegionCacheEntry();
                _regionCache.Add(key, entry);
            }
            if (entry.LayoutVersion != LayoutVersion || entry.StorageVersion != _pageStorageVersion)
            {
                BuildRegions(image, sourceUv, useTightGeometry);
                if (entry.Regions.Length < _preparedCount)
                {
                    entry.Regions = new SpriteImageRegion[_preparedCount];
                }
                Array.Copy(_regionBuildBuffer, entry.Regions, _preparedCount);
                entry.Count = _preparedCount;
                entry.LayoutVersion = LayoutVersion;
                entry.StorageVersion = _pageStorageVersion;
            }
            entry.LastUse = ++_regionCacheUse;
            _preparedRegions = entry.Regions;
            _preparedCount = entry.Count;
            _preparedIdentity = identity;
            _preparedSourceUv = sourceUv;
            _preparedTightGeometry = useTightGeometry;
            _preparedLayoutVersion = LayoutVersion;
            _preparedStorageVersion = _pageStorageVersion;
            _preparedFrame = SpriteDrawState.FrameVersion;
        }

        /// <summary>Reuses one retired cache slot when the bounded image/crop cache is full.</summary>
        /// <returns>A new or safely detached reusable cache entry with invalid generation stamps.</returns>
        private RegionCacheEntry AcquireRegionCacheEntry()
        {
            if (_regionCache.Count < MaximumRegionCacheEntries)
            {
                return new RegionCacheEntry();
            }
            var oldestUse = long.MaxValue;
            var oldestKey = default(RegionCacheKey);
            RegionCacheEntry? oldest = null;
            foreach (var candidate in _regionCache)
            {
                if (candidate.Value.LastUse < oldestUse)
                {
                    oldestUse = candidate.Value.LastUse;
                    oldestKey = candidate.Key;
                    oldest = candidate.Value;
                }
            }
            _regionCache.Remove(oldestKey);
            oldest!.LayoutVersion = -1;
            oldest.StorageVersion = -1;
            return oldest;
        }

        /// <summary>Prepares required pages once per image and complete frame, independently of changing source crops.</summary>
        /// <param name="identity">The canonical original image identity.</param>
        /// <returns>The metadata whose complete page set is ready for this frame.</returns>
        private SpriteAtlasImage PrepareImageForFrame(string identity)
        {
            if (!_imageFrameStates.TryGetValue(identity, out var state))
            {
                state = new ImageFrameState();
                _imageFrameStates.Add(identity, state);
            }
            if (state.Frame != SpriteDrawState.FrameVersion || state.LayoutVersion != LayoutVersion || state.StorageVersion != _pageStorageVersion)
            {
                state.Image = PrepareImage(identity);
                state.Frame = SpriteDrawState.FrameVersion;
                state.LayoutVersion = LayoutVersion;
                state.StorageVersion = _pageStorageVersion;
            }
            return state.Image!;
        }

        /// <summary>Writes disjoint interior, edge, and corner zones once while preserving original Clamp sampling.</summary>
        /// <param name="image">The prepared original image metadata.</param>
        /// <param name="sourceUv">The requested original source rectangle.</param>
        /// <param name="useTightGeometry">Whether transparent cell rectangles may subdivide the source tiles.</param>
        private void BuildRegions(SpriteAtlasImage image, Rect sourceUv, bool useTightGeometry)
        {
            _preparedCount = 0;
            var source = SourcePixels(image, sourceUv);
            if (source.width <= 0f || source.height <= 0f)
            {
                return;
            }
            var minimumZoneX = source.xMin < 0f ? -1 : source.xMin >= image.Width ? 1 : 0;
            var maximumZoneX = source.xMax <= 0f ? -1 : source.xMax > image.Width ? 1 : 0;
            var minimumZoneY = source.yMin < 0f ? -1 : source.yMin >= image.Height ? 1 : 0;
            var maximumZoneY = source.yMax <= 0f ? -1 : source.yMax > image.Height ? 1 : 0;
            for (var zoneY = minimumZoneY; zoneY <= maximumZoneY; zoneY++)
            {
                for (var zoneX = minimumZoneX; zoneX <= maximumZoneX; zoneX++)
                {
                    if (!TrySourceZone(source, image.Width, image.Height, zoneX, zoneY, out var virtualZone, out var sampleZone))
                    {
                        continue;
                    }
                    foreach (var tile in image.Tiles)
                    {
                        if (!SpriteGeometryUtility.Intersect(sampleZone, tile.SourcePixels, out var crop))
                        {
                            continue;
                        }
                        var geometryCount = useTightGeometry ? image.VisiblePixels.Length : 1;
                        for (var geometryIndex = 0; geometryIndex < geometryCount; geometryIndex++)
                        {
                            var geometry = useTightGeometry ? image.VisiblePixels[geometryIndex] : new Rect(0f, 0f, image.Width, image.Height);
                            if (!SpriteGeometryUtility.Intersect(crop, geometry, out var piece))
                            {
                                continue;
                            }
                            var atlas = tile.TextureUv;
                            var textureX = zoneX == 0 ? piece.x : zoneX < 0 ? .5f : image.Width - .5f;
                            var textureY = zoneY == 0 ? piece.y : zoneY < 0 ? .5f : image.Height - .5f;
                            var textureUv = new Rect(atlas.x + (textureX - tile.SourcePixels.x) / tile.SourcePixels.width * atlas.width,
                                atlas.y + (textureY - tile.SourcePixels.y) / tile.SourcePixels.height * atlas.height,
                                zoneX == 0 ? piece.width / tile.SourcePixels.width * atlas.width : 0f,
                                zoneY == 0 ? piece.height / tile.SourcePixels.height * atlas.height : 0f);
                            var virtualPiece = new Rect(zoneX == 0 ? piece.x : virtualZone.x, zoneY == 0 ? piece.y : virtualZone.y,
                                zoneX == 0 ? piece.width : virtualZone.width, zoneY == 0 ? piece.height : virtualZone.height);
                            if (_preparedCount == _regionBuildBuffer.Length)
                            {
                                Array.Resize(ref _regionBuildBuffer, Math.Max(16, _preparedCount * 2));
                            }
                            _regionBuildBuffer[_preparedCount++] = new SpriteImageRegion(_pages[tile.Page], new Rect(virtualPiece.x / image.Width, virtualPiece.y / image.Height,
                                virtualPiece.width / image.Width, virtualPiece.height / image.Height), textureUv, virtualPiece);
                        }
                    }
                }
            }
        }

        /// <summary>Separates a request into disjoint source-space zones and their edge-texel sampling support.</summary>
        /// <param name="source">The positive requested original pixel rectangle.</param>
        /// <param name="width">The complete original width.</param>
        /// <param name="height">The complete original height.</param>
        /// <param name="zoneX">Minus one for left extension, zero for interior, or one for right extension.</param>
        /// <param name="zoneY">Minus one for bottom extension, zero for interior, or one for top extension.</param>
        /// <param name="virtualZone">Receives the requested destination-supporting source zone, including virtual pixels.</param>
        /// <param name="sampleZone">Receives the original pixel area supplying samples to this zone.</param>
        /// <returns>True when the request intersects the selected zone with positive area.</returns>
        private static bool TrySourceZone(Rect source, int width, int height, int zoneX, int zoneY, out Rect virtualZone, out Rect sampleZone)
        {
            var minimumX = zoneX < 0 ? source.xMin : zoneX > 0 ? Mathf.Max(source.xMin, width) : Mathf.Max(source.xMin, 0f);
            var maximumX = zoneX < 0 ? Mathf.Min(source.xMax, 0f) : zoneX > 0 ? source.xMax : Mathf.Min(source.xMax, width);
            var minimumY = zoneY < 0 ? source.yMin : zoneY > 0 ? Mathf.Max(source.yMin, height) : Mathf.Max(source.yMin, 0f);
            var maximumY = zoneY < 0 ? Mathf.Min(source.yMax, 0f) : zoneY > 0 ? source.yMax : Mathf.Min(source.yMax, height);
            virtualZone = Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
            sampleZone = new Rect(zoneX < 0 ? 0f : zoneX > 0 ? width - 1f : minimumX,
                zoneY < 0 ? 0f : zoneY > 0 ? height - 1f : minimumY,
                zoneX == 0 ? maximumX - minimumX : 1f, zoneY == 0 ? maximumY - minimumY : 1f);
            return virtualZone.width > 0f && virtualZone.height > 0f;
        }

        /// <summary>Converts a requested normalized rectangle to positive original pixel coordinates.</summary>
        /// <param name="image">The complete original dimensions.</param>
        /// <param name="sourceUv">The requested normalized source rectangle.</param>
        /// <returns>The positive source-pixel rectangle.</returns>
        private static Rect SourcePixels(SpriteAtlasImage image, Rect sourceUv)
        {
            var positive = SpriteGeometryUtility.Positive(sourceUv);
            return new Rect(positive.x * image.Width, positive.y * image.Height, positive.width * image.Width, positive.height * image.Height);
        }

        /// <summary>Loads every page for an image and retries recoverable failures with a smaller complete layout.</summary>
        /// <param name="identity">The original source image identity.</param>
        /// <returns>Metadata from the successfully prepared complete layout.</returns>
        /// <exception cref="InvalidOperationException">All hardware-compatible layouts fail normal resource loading.</exception>
        private SpriteAtlasImage PrepareImage(string identity)
        {
            ThrowIfDisposed();
            CollectRetiredPages();
            while (true)
            {
                var image = _metadata[identity];
                try
                {
                    foreach (var tile in image.Tiles)
                    {
                        LoadPage(tile);
                    }
                    return image;
                }
                catch (Exception error) when (error is InvalidOperationException || error is UnityException || error is IOException)
                {
                    var layout = _catalog!.Layouts[_layoutIndex];
                    var next = SpriteGeometryUtility.SelectLayout(_catalog.Layouts, Math.Min(_hardwareMaximum, layout.MaximumPageSize - 1));
                    if (next < 0)
                    {
                        throw new InvalidOperationException("Every compatible baked atlas layout failed to load image: " + identity, error);
                    }
                    foreach (var page in _pages.Values)
                    {
                        _retiredPages.Add((page, SpriteDrawState.FrameVersion));
                    }
                    _pages.Clear();
                    _pageLastSeen.Clear();
                    _pageStorageVersion++;
                    _layoutIndex = next;
                    SelectMetadata();
                    Debug.LogWarning($"{nameof(SpriteAssetProvider)}: Falling back to {MaxPageSize}px atlas pages after a resource loading failure: {error.Message}");
                }
            }
        }

        /// <summary>Loads and validates one page, detecting importer resizing instead of silently accepting corrupted UVs.</summary>
        /// <param name="tile">The tile describing the required page and exact imported dimensions.</param>
        /// <exception cref="InvalidOperationException">The page is missing, oversized, or unexpectedly resized.</exception>
        private void LoadPage(SpriteAtlasTile tile)
        {
            if (_pages.ContainsKey(tile.Page))
            {
                _pageLastSeen[tile.Page] = SpriteDrawState.FrameVersion;
                return;
            }
            var layout = _catalog!.Layouts[_layoutIndex];
            if (tile.Page < 0 || tile.Page >= layout.Pages.Length || tile.PageWidth <= 0 || tile.PageHeight <= 0
                || tile.PageWidth > layout.MaximumPageSize || tile.PageHeight > layout.MaximumPageSize
                || tile.PageWidth > _hardwareMaximum || tile.PageHeight > _hardwareMaximum
                || (long)tile.PageWidth * tile.PageHeight * 4 > SpriteGeometryUtility.MaximumPageBytes)
            {
                throw new InvalidOperationException("Atlas page metadata exceeds its selected hardware or memory limits.");
            }
            var texture = _loadPage(layout.Pages[tile.Page]);
            if (texture == null)
            {
                throw new InvalidOperationException("A generated atlas page is missing: " + layout.Pages[tile.Page]);
            }
            if (texture.width != tile.PageWidth || texture.height != tile.PageHeight)
            {
                _releasePage(texture);
                throw new InvalidOperationException("An atlas page was resized during import or quality selection: " + layout.Pages[tile.Page]);
            }
            _pages.Add(tile.Page, texture);
            _pageLastSeen[tile.Page] = SpriteDrawState.FrameVersion;
        }

        /// <summary>Releases previous-layout textures only after subsequent complete scene frames have retired.</summary>
        private void CollectRetiredPages()
        {
            if (_lastCollectionFrame == SpriteDrawState.FrameVersion)
            {
                return;
            }
            _lastCollectionFrame = SpriteDrawState.FrameVersion;
            _unusedPageIds.Clear();
            foreach (var entry in _pageLastSeen)
            {
                if (SpriteDrawState.FrameVersion - entry.Value > 2)
                {
                    _unusedPageIds.Add(entry.Key);
                }
            }
            foreach (var id in _unusedPageIds)
            {
                _releasePage(_pages[id]);
                _pages.Remove(id);
                _pageLastSeen.Remove(id);
                _pageStorageVersion++;
            }
            for (var index = _retiredPages.Count - 1; index >= 0; index--)
            {
                var entry = _retiredPages[index];
                if (SpriteDrawState.FrameVersion - entry.Frame <= 1)
                {
                    continue;
                }
                _releasePage(entry.Texture);
                _retiredPages.RemoveAt(index);
            }
        }

        /// <summary>Indexes loose original paths once, retaining PNG precedence and resource-manifest aliases.</summary>
        private void IndexLooseSources()
        {
            var images = new List<string>();
            var texts = new List<string>();
            foreach (var directory in new[] { "reanim", "images", "particles", "properties" })
            {
                var folder = Path.Combine(_root, directory);
                if (!Directory.Exists(folder))
                {
                    continue;
                }
                foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    var relative = Normalize(Path.GetRelativePath(_root, path));
                    var extension = Path.GetExtension(path);
                    if (IsImage(extension))
                    {
                        images.Add(relative);
                        _loosePaths[relative] = relative;
                        var stem = relative.Substring(0, relative.Length - extension.Length);
                        if (!_loosePaths.ContainsKey(stem) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            _loosePaths[stem] = relative;
                        }
                        var alias = (directory == "reanim" ? "IMAGE_REANIM_" : "IMAGE_") + Path.GetFileNameWithoutExtension(path);
                        if (!_loosePaths.ContainsKey(alias) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            _loosePaths[alias] = relative;
                        }
                    }
                    else if (extension.Equals(".reanim", StringComparison.OrdinalIgnoreCase) || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        texts.Add(relative);
                    }
                }
            }
            ImagePaths = images.ToArray();
            TextPaths = texts.ToArray();
            IndexLooseManifest();
        }

        /// <summary>Registers original resource aliases for external test directories without decoding any artwork.</summary>
        private void IndexLooseManifest()
        {
            var path = Path.Combine(_root, "properties", "resources.xml");
            if (!File.Exists(path))
            {
                path = Path.Combine(_root, "properties", "Resources.xml");
            }
            if (!File.Exists(path))
            {
                return;
            }
            var document = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(path, settings);
            document.Load(reader);
            foreach (XmlNode group in document.SelectNodes("/ResourceManifest/Resources"))
            {
                var prefix = "IMAGE_";
                var folder = "images";
                foreach (XmlNode node in group.ChildNodes)
                {
                    if (node.Name == "SetDefaults")
                    {
                        prefix = node.Attributes?["idprefix"]?.Value ?? prefix;
                        folder = Normalize(node.Attributes?["path"]?.Value ?? folder).TrimEnd('/');
                    }
                    if (node.Name == "Image" && node.Attributes?["id"] != null && node.Attributes?["path"] != null
                        && _loosePaths.TryGetValue(folder + "/" + Normalize(node.Attributes["path"]!.Value), out var relative))
                    {
                        _loosePaths[prefix + node.Attributes["id"]!.Value] = relative;
                    }
                }
            }
        }

        /// <summary>Finds an external original image by path or conventional resource identifier.</summary>
        /// <param name="key">The original path or resource identifier.</param>
        /// <returns>The normalized original relative image path.</returns>
        /// <exception cref="FileNotFoundException">The external image is absent.</exception>
        private string ResolveLooseImage(string key)
        {
            if (_loosePaths.TryGetValue(key, out var relative) || _loosePaths.TryGetValue("images/" + key, out relative))
            {
                return relative;
            }
            throw new FileNotFoundException("Original image was not found: " + key);
        }

        /// <summary>Decodes one external source image while preserving separately stored JPEG alpha.</summary>
        /// <param name="path">The absolute original color image path.</param>
        /// <returns>The caller-owned standalone texture.</returns>
        /// <exception cref="InvalidDataException">The source cannot be decoded.</exception>
        private static Texture2D DecodeLooseImage(string path)
        {
            var mask = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "_.png");
            if (Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase) && File.Exists(mask))
            {
                return TextureCache.LoadMaskedImage(path, mask);
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileName(path),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path), true))
                {
                    throw new InvalidDataException("Could not decode original image: " + path);
                }
                return texture;
            }
            catch
            {
                DestroyLooseTexture(texture);
                throw;
            }
        }

        /// <summary>Recognizes the original game's supported standalone color image formats.</summary>
        /// <param name="extension">A filename extension including its leading dot.</param>
        /// <returns>True for PNG, JPEG, or JPG images.</returns>
        private static bool IsImage(string extension)
        {
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Normalizes relative identifiers without altering filename casing.</summary>
        /// <param name="path">The original path or identifier.</param>
        /// <returns>The path using forward slashes.</returns>
        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>Adds the conventional images directory to a bare explicit-alpha filename.</summary>
        /// <param name="path">The relative or bare image filename.</param>
        /// <returns>The normalized original relative path.</returns>
        private static string NormalizeImagePath(string path)
        {
            var normalized = Normalize(path);
            return normalized.IndexOf('/') >= 0 ? normalized : "images/" + normalized;
        }

        /// <summary>Loads a single imported page using its extensionless Resources identity.</summary>
        /// <param name="path">The generated page's Resources path.</param>
        /// <returns>The imported texture, or null when absent.</returns>
        private static Texture2D? LoadResourcePage(string path)
        {
            return Resources.Load<Texture2D>(path);
        }

        /// <summary>Releases an imported atlas page after all frame references have retired.</summary>
        /// <param name="texture">The loaded imported page.</param>
        private static void ReleaseResourcePage(Texture2D texture)
        {
            if (texture != null)
            {
                Resources.UnloadAsset(texture);
            }
        }

        /// <summary>Destroys one provider-owned external decoded texture.</summary>
        /// <param name="texture">The loose source texture.</param>
        private static void DestroyLooseTexture(Texture2D texture)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Guards operations after the final acquisition has been released.</summary>
        /// <exception cref="ObjectDisposedException">The provider has already released its resources.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteAssetProvider));
            }
        }

        /// <summary>Releases one acquisition and unloads pages when the final caller releases the provider.</summary>
        public void Dispose()
        {
            ObjectSpriteRenderer.FlushPending();
            if (_disposed || --_references > 0)
            {
                return;
            }
            foreach (var page in _pages.Values)
            {
                _releasePage(page);
            }
            foreach (var entry in _retiredPages)
            {
                _releasePage(entry.Texture);
            }
            foreach (var texture in _looseTextures)
            {
                DestroyLooseTexture(texture);
            }
            _pages.Clear();
            _pageLastSeen.Clear();
            _retiredPages.Clear();
            _looseTextures.Clear();
            _images.Clear();
            _texts.Clear();
            _regionCache.Clear();
            _imageFrameStates.Clear();
            _preparedRegions = Array.Empty<SpriteImageRegion>();
            _regionBuildBuffer = Array.Empty<SpriteImageRegion>();
            _preparedIdentity = null;
            s_providers.Remove(_root);
            _disposed = true;
        }
    }
}
