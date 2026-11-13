#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using PlantsVsZombies.Infrastructure;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Generates lossless, device-selectable atlas layouts and packaged original rendering texts.</summary>
    public static class SpriteAtlasBaker
    {
        /// <summary>The ignored generated asset directory; source assets and existing source GUIDs remain untouched.</summary>
        public const string OutputRoot = "Assets/Generated/PvZRendering/Resources/PvZRendering";
        /// <summary>The bilinear sampling halo around every lossless content tile.</summary>
        private const int Padding = 2;
        /// <summary>The deterministic independently packed layout sizes, including a compatibility floor.</summary>
        private static readonly int[] s_pageSizes = { 256, 512, 1024, 2048 };

        /// <summary>Retains one original decoded source only during offline atlas generation.</summary>
        private sealed class Source
        {
            /// <summary>The original relative path or explicit color/mask pair.</summary>
            internal string Identity = string.Empty;
            /// <summary>The original resource identifiers referencing this source.</summary>
            internal readonly List<string> Aliases = new List<string>();
            /// <summary>The unchanged original width.</summary>
            internal int Width;
            /// <summary>The unchanged original height.</summary>
            internal int Height;
            /// <summary>The original bottom-left RGBA pixels, released after generation.</summary>
            internal Color32[] Pixels = Array.Empty<Color32>();
            /// <summary>The conservative disjoint visible rectangles, limited to 32 pieces.</summary>
            internal Rect[] Geometry = Array.Empty<Rect>();
            /// <summary>The native actor family packed as one unit whenever it fits a single page.</summary>
            internal string PackingFamily = string.Empty;
            /// <summary>The load-frequency category placing shared opening-battle artwork before large menu backgrounds.</summary>
            internal int PackingPriority;
            /// <summary>The padded tile area of the complete family, used to place large hot families before fragmented gaps form.</summary>
            internal long PackingFamilyArea;
            /// <summary>Whether this large board background owns pages independently of unrelated images.</summary>
            internal bool DedicatedPage;
        }

        /// <summary>Describes one tile placement before the atlas page dimensions are finalized.</summary>
        private sealed class Placement
        {
            /// <summary>The source whose original pixels are copied.</summary>
            internal Source Source = null!;
            /// <summary>The unpadded original pixel content rectangle.</summary>
            internal RectInt SourcePixels;
            /// <summary>The lower-left padded page allocation.</summary>
            internal RectInt Destination;
            /// <summary>The runtime tile whose normalized page mapping is finalized after packing.</summary>
            internal SpriteAtlasTile Tile = null!;
        }

        /// <summary>Owns a deterministic guillotine-packed page and its content placements.</summary>
        private sealed class Page
        {
            /// <summary>The disjoint free rectangles available for subsequent tile placement.</summary>
            internal readonly List<RectInt> Free = new List<RectInt>();
            /// <summary>The original pixel placements written to this page.</summary>
            internal readonly List<Placement> Placements = new List<Placement>();
            /// <summary>The used page width, including sampling halos.</summary>
            internal int Width;
            /// <summary>The used page height, including sampling halos.</summary>
            internal int Height;
            /// <summary>Prevents unrelated families from occupying a dedicated board-background page.</summary>
            internal bool Dedicated;

            /// <summary>Creates an empty square packing bin.</summary>
            /// <param name="size">The maximum page edge for this layout.</param>
            internal Page(int size)
            {
                Free.Add(new RectInt(0, 0, size, size));
            }
        }

        /// <summary>Regenerates the atlas assets when original source content or generator settings change.</summary>
        /// <exception cref="InvalidOperationException">The Editor version or original source root is invalid.</exception>
        [MenuItem("PlantsVsZombies/Generate Rendering Atlases")]
        public static void Generate()
        {
            EnsureCurrent(true);
        }

        /// <summary>Builds the production atlas assets if their complete content fingerprint has changed.</summary>
        /// <param name="force">Whether to regenerate even when the existing output fingerprint matches.</param>
        /// <exception cref="InvalidOperationException">The required Editor version or original resources are missing.</exception>
        public static void EnsureCurrent(bool force = false)
        {
            if (Application.unityVersion != "6000.3.17f1")
            {
                throw new InvalidOperationException("Use Unity 6000.3.17f1 to generate rendering atlases.");
            }
            var root = RuntimeAssetPaths.ResolveRoot(Application.streamingAssetsPath);
            if (!Directory.Exists(Path.Combine(root, "reanim")) || !Directory.Exists(Path.Combine(root, "images")))
            {
                throw new InvalidOperationException("Original reanim and images resources are required before generating rendering atlases.");
            }
            Bake(root, OutputRoot, force);
        }

        /// <summary>Generates a complete catalog from a source directory, including small synthetic test fixtures.</summary>
        /// <param name="root">The directory containing original images, reanim, particles, and properties.</param>
        /// <param name="outputRoot">The Unity asset output directory containing generated Resources content.</param>
        /// <param name="force">Whether to bypass the complete-source fingerprint check.</param>
        /// <returns>The generated or validated complete catalog.</returns>
        /// <exception cref="InvalidDataException">A source image, alpha grid, or source manifest is malformed.</exception>
        public static SpriteAtlasCatalog Bake(string root, string outputRoot, bool force = false)
        {
            var paths = CollectSources(root);
            var hash = Fingerprint(root, paths);
            var catalogPath = outputRoot + "/catalog.json";
            if (!force && File.Exists(catalogPath))
            {
                var cached = JsonUtility.FromJson<SpriteAtlasCatalog>(File.ReadAllText(catalogPath));
                if (cached != null && cached.Version == SpriteAtlasCatalog.CurrentVersion && cached.SourceHash == hash
                    && CompleteOutputExists(outputRoot, cached))
                {
                    return cached;
                }
            }
            Directory.CreateDirectory(outputRoot);
            // A failed regeneration must not leave a usable stale catalog pointing at partially replaced pages.
            File.WriteAllText(catalogPath, "{}", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(catalogPath, ImportAssetOptions.ForceSynchronousImport);
            var sources = DecodeSources(root, paths);
            AddManifestAliasesAndMasks(root, sources);
            foreach (var source in sources)
            {
                source.DedicatedPage = source.Identity.StartsWith("images/background", StringComparison.OrdinalIgnoreCase) && source.Width >= 1024;
                source.PackingFamily = source.DedicatedPage ? source.Identity : SourceFamily(source.Identity);
                source.PackingPriority = SourcePriority(source);
            }
            AssignFamilyAreas(sources);
            sources.Sort(CompareSources);
            var catalog = new SpriteAtlasCatalog { SourceHash = hash, Layouts = new SpriteAtlasLayout[s_pageSizes.Length] };
            var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { catalogPath };
            for (var index = 0; index < s_pageSizes.Length; index++)
            {
                catalog.Layouts[index] = BakeLayout(sources, s_pageSizes[index], outputRoot, generated);
            }
            var texts = new List<SpriteAtlasText>();
            foreach (var relative in paths)
            {
                var extension = Path.GetExtension(relative);
                if (!extension.Equals(".xml", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".reanim", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var destination = outputRoot + "/Texts/" + relative + ".txt";
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllBytes(destination, File.ReadAllBytes(Path.Combine(root, relative)));
                generated.Add(destination);
                texts.Add(new SpriteAtlasText { SourceIdentity = relative, ResourcePath = ResourcePath(destination) });
            }
            catalog.Texts = texts.ToArray();
            RemoveObsoleteOutputs(outputRoot, generated);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImports(catalog, outputRoot);
            var temporaryCatalog = catalogPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryCatalog, JsonUtility.ToJson(catalog, false), new UTF8Encoding(false));
                File.Replace(temporaryCatalog, catalogPath, null);
                AssetDatabase.ImportAsset(catalogPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                if (File.Exists(temporaryCatalog))
                {
                    File.Delete(temporaryCatalog);
                }
            }
            Debug.Log($"{nameof(SpriteAtlasBaker)}: Generated {sources.Count} logical images in {catalog.Layouts.Length} lossless atlas layouts.");
            return catalog;
        }

        /// <summary>Collects original image and rendering-text paths in deterministic ordinal order.</summary>
        /// <param name="root">The original asset root.</param>
        /// <returns>The original relative source paths included in the fingerprint and generated content.</returns>
        private static List<string> CollectSources(string root)
        {
            var result = new List<string>();
            foreach (var directory in new[] { "images", "reanim", "particles", "properties" })
            {
                var path = Path.Combine(root, directory);
                if (!Directory.Exists(path))
                {
                    continue;
                }
                foreach (var source in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(source).ToLowerInvariant();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".reanim" || extension == ".xml" || extension == ".txt")
                    {
                        result.Add(Path.GetRelativePath(root, source).Replace('\\', '/'));
                    }
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>Fingerprints source bytes and all settings that affect packing or geometry.</summary>
        /// <param name="root">The original asset root.</param>
        /// <param name="paths">The deterministically ordered original relative paths.</param>
        /// <returns>A stable SHA256 fingerprint.</returns>
        private static string Fingerprint(string root, List<string> paths)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(Encoding.UTF8.GetBytes("PvZAtlasBakeV4;256,512,1024,2048;RGBA32;Pad2;Grid8;Halo1;Pieces32;AtomicFamiliesLargestFirst;BattleUiAndMowers;DedicatedBoardBackgrounds;ExactFirstImport;AtomicCatalog"));
            foreach (var path in paths)
            {
                hash.AppendData(Encoding.UTF8.GetBytes(path));
                hash.AppendData(File.ReadAllBytes(Path.Combine(root, path)));
            }
            return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
        }

        /// <summary>Checks that every expected generated file exists before trusting a matching source fingerprint.</summary>
        /// <param name="outputRoot">The generated resource directory.</param>
        /// <param name="catalog">The previously generated catalog.</param>
        /// <returns>True when all referenced page and text outputs exist.</returns>
        private static bool CompleteOutputExists(string outputRoot, SpriteAtlasCatalog catalog)
        {
            foreach (var layout in catalog.Layouts)
            {
                foreach (var page in layout.Pages)
                {
                    if (!File.Exists(AssetPath(outputRoot, page) + ".png"))
                    {
                        return false;
                    }
                }
            }
            foreach (var text in catalog.Texts)
            {
                if (!File.Exists(AssetPath(outputRoot, text.ResourcePath) + ".txt"))
                {
                    return false;
                }
            }
            return catalog.Layouts.Length == s_pageSizes.Length;
        }

        /// <summary>Decodes original color images once, preserving PNG precedence and automatic JPEG masks.</summary>
        /// <param name="root">The original asset directory.</param>
        /// <param name="paths">The complete deterministic source list.</param>
        /// <returns>The original decoded sources with conservative visible geometry.</returns>
        private static List<Source> DecodeSources(string root, List<string> paths)
        {
            var selected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var relative in paths)
            {
                var extension = Path.GetExtension(relative).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    continue;
                }
                var key = relative.Substring(0, relative.Length - extension.Length);
                if (!selected.ContainsKey(key) || extension == ".png")
                {
                    selected[key] = relative;
                }
            }
            var result = new List<Source>();
            foreach (var relative in paths)
            {
                var extension = Path.GetExtension(relative).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    continue;
                }
                var source = Decode(root, relative);
                var folder = relative.StartsWith("reanim/", StringComparison.OrdinalIgnoreCase) ? "IMAGE_REANIM_" : "IMAGE_";
                var stem = relative.Substring(0, relative.Length - extension.Length);
                if (selected[stem] == relative)
                {
                    source.Aliases.Add(folder + Path.GetFileNameWithoutExtension(relative));
                    source.Aliases.Add(stem);
                }
                source.Geometry = SpriteGeometryUtility.BuildVisibleGeometry(source.Pixels, source.Width, source.Height);
                result.Add(source);
            }
            return result;
        }

        /// <summary>Decodes a source and applies its original separately stored JPEG mask when present.</summary>
        /// <param name="root">The original resource directory.</param>
        /// <param name="relative">The selected original relative color image path.</param>
        /// <returns>The original source pixels and dimensions.</returns>
        /// <exception cref="InvalidDataException">The image or automatic alpha mask cannot be decoded.</exception>
        private static Source Decode(string root, string relative)
        {
            var path = Path.Combine(root, relative);
            var texture = DecodeTexture(path);
            try
            {
                var source = new Source { Identity = relative, Width = texture.width, Height = texture.height, Pixels = texture.GetPixels32() };
                var maskPath = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "_.png");
                if (Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase) && File.Exists(maskPath))
                {
                    ApplyMask(source, maskPath);
                }
                return source;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Decodes a temporary readable texture used only during offline generation.</summary>
        /// <param name="path">The original absolute image path.</param>
        /// <returns>The temporary readable decoded texture.</returns>
        /// <exception cref="InvalidDataException">The original image cannot be decoded.</exception>
        private static Texture2D DecodeTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path), false))
                {
                    throw new InvalidDataException("Could not decode atlas source: " + path);
                }
                return texture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
        }

        /// <summary>Applies an original red-channel alpha mask to decoded offline source pixels.</summary>
        /// <param name="source">The color pixels and dimensions to preserve.</param>
        /// <param name="maskPath">The original absolute mask path.</param>
        /// <exception cref="InvalidDataException">The mask dimensions differ from the original color artwork.</exception>
        private static void ApplyMask(Source source, string maskPath)
        {
            var mask = DecodeTexture(maskPath);
            try
            {
                if (mask.width != source.Width || mask.height != source.Height)
                {
                    throw new InvalidDataException("Original atlas alpha-grid dimensions differ: " + maskPath);
                }
                var alpha = mask.GetPixels32();
                for (var index = 0; index < alpha.Length; index++)
                {
                    source.Pixels[index].a = alpha[index].r;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        /// <summary>Resolves manifest aliases and explicit alpha grids without allocating standalone runtime textures.</summary>
        /// <param name="root">The original asset root.</param>
        /// <param name="sources">The decoded original sources, extended with explicit color/mask pairs.</param>
        private static void AddManifestAliasesAndMasks(string root, List<Source> sources)
        {
            var byStem = new Dictionary<string, Source>(StringComparer.OrdinalIgnoreCase);
            foreach (var source in sources)
            {
                var extension = Path.GetExtension(source.Identity);
                var stem = source.Identity.Substring(0, source.Identity.Length - extension.Length);
                if (!byStem.ContainsKey(stem) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    byStem[stem] = source;
                }
            }
            var automaticPairs = sources.ToArray();
            foreach (var color in automaticPairs)
            {
                var extension = Path.GetExtension(color.Identity);
                if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    && byStem.TryGetValue(color.Identity.Substring(0, color.Identity.Length - extension.Length) + "_", out var automaticMask)
                    && Path.GetExtension(automaticMask.Identity).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    color.Aliases.Add(color.Identity + "|" + automaticMask.Identity);
                }
            }
            var manifest = FindManifest(root);
            if (manifest != null)
            {
                var document = new XmlDocument { XmlResolver = null };
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using var reader = XmlReader.Create(manifest, settings);
                document.Load(reader);
                foreach (XmlNode group in document.SelectNodes("/ResourceManifest/Resources"))
                {
                    var folder = "images";
                    var prefix = "IMAGE_";
                    foreach (XmlNode node in group.ChildNodes)
                    {
                        if (node.Name == "SetDefaults")
                        {
                            folder = node.Attributes?["path"]?.Value.Replace('\\', '/').TrimEnd('/') ?? folder;
                            prefix = node.Attributes?["idprefix"]?.Value ?? prefix;
                        }
                        if (node.Name != "Image" || node.Attributes?["path"] == null || node.Attributes?["id"] == null)
                        {
                            continue;
                        }
                        var stem = folder + "/" + node.Attributes["path"]!.Value.Replace('\\', '/');
                        if (!byStem.TryGetValue(stem, out var source))
                        {
                            continue;
                        }
                        var alphaName = node.Attributes["alphagrid"]?.Value;
                        if (alphaName != null && byStem.TryGetValue(folder + "/" + alphaName, out var mask))
                        {
                            source = AddExplicitPair(root, sources, source, mask);
                        }
                        var alias = prefix + node.Attributes["id"]!.Value;
                        foreach (var entry in sources)
                        {
                            entry.Aliases.RemoveAll(candidate => candidate.Equals(alias, StringComparison.OrdinalIgnoreCase));
                        }
                        source.Aliases.Add(alias);
                    }
                }
            }
            AddKnownPair(root, sources, byStem, "images/pool_base", "images/pool_base_");
            AddKnownPair(root, sources, byStem, "images/pool_shading", "images/pool_shading_");
        }

        /// <summary>Adds a known day-pool alpha pair absent from resources.xml alphagrid attributes.</summary>
        /// <param name="root">The original resource root.</param>
        /// <param name="sources">The decoded sources and explicit pairs.</param>
        /// <param name="byStem">The original image lookup without file extensions.</param>
        /// <param name="colorStem">The original color-image stem.</param>
        /// <param name="maskStem">The original alpha-grid stem.</param>
        private static void AddKnownPair(string root, List<Source> sources, Dictionary<string, Source> byStem, string colorStem, string maskStem)
        {
            if (byStem.TryGetValue(colorStem, out var color) && byStem.TryGetValue(maskStem, out var mask))
            {
                AddExplicitPair(root, sources, color, mask);
            }
        }

        /// <summary>Creates one independently indexed original-color and explicit-alpha combination.</summary>
        /// <param name="root">The original source directory.</param>
        /// <param name="sources">The retained original sources and existing pairs.</param>
        /// <param name="color">The original color source.</param>
        /// <param name="mask">The original mask source whose red channel supplies alpha.</param>
        /// <returns>The shared explicit-pair source.</returns>
        private static Source AddExplicitPair(string root, List<Source> sources, Source color, Source mask)
        {
            var identity = color.Identity + "|" + mask.Identity;
            foreach (var existing in sources)
            {
                if (existing.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }
            var source = new Source { Identity = identity, Width = color.Width, Height = color.Height, Pixels = (Color32[])color.Pixels.Clone() };
            ApplyMask(source, Path.Combine(root, mask.Identity));
            source.Geometry = SpriteGeometryUtility.BuildVisibleGeometry(source.Pixels, source.Width, source.Height);
            sources.Add(source);
            return source;
        }

        /// <summary>Finds the source resource manifest while preserving original filename casing.</summary>
        /// <param name="root">The original source root.</param>
        /// <returns>The manifest path, or null for synthetic sources without a resource manifest.</returns>
        private static string? FindManifest(string root)
        {
            var directory = Path.Combine(root, "properties");
            if (!Directory.Exists(directory))
            {
                return null;
            }
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                if (Path.GetFileName(path).Equals("resources.xml", StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }
            return null;
        }

        /// <summary>Groups frequently related native artwork before placing larger rectangles first.</summary>
        /// <param name="first">The first decoded source.</param>
        /// <param name="second">The second decoded source.</param>
        /// <returns>The deterministic source packing order.</returns>
        private static int CompareSources(Source first, Source second)
        {
            var group = first.PackingPriority.CompareTo(second.PackingPriority);
            if (group != 0)
            {
                return group;
            }
            var area = second.PackingFamilyArea.CompareTo(first.PackingFamilyArea);
            if (area != 0)
            {
                return area;
            }
            var family = StringComparer.OrdinalIgnoreCase.Compare(first.PackingFamily, second.PackingFamily);
            if (family != 0)
            {
                return family;
            }
            var size = Math.Max(second.Width, second.Height).CompareTo(Math.Max(first.Width, first.Height));
            return size != 0 ? size : StringComparer.Ordinal.Compare(first.Identity, second.Identity);
        }

        /// <summary>Prioritizes widely shared early-adventure actors and shadows before infrequently sampled large artwork.</summary>
        /// <param name="source">The decoded source with its resolved native family.</param>
        /// <returns>Zero for shared battle artwork, one for other actors, two for static images, or three for dedicated backgrounds.</returns>
        private static int SourcePriority(Source source)
        {
            if (source.DedicatedPage)
            {
                return 3;
            }
            switch (source.PackingFamily.ToLowerInvariant())
            {
                case "peashooter":
                case "sunflower":
                case "zombie":
                case "wallnut":
                case "potatomine":
                case "cherrybomb":
                case "snowpea":
                case "repeater":
                case "chomper":
                case "blank":
                case "seedbank":
                case "sunbank":
                case "shovelbank":
                case "shovel":
                case "button":
                case "lawnmower":
                case "poolcleaner":
                case "roofcleaner":
                    return 0;
            }
            if (source.Identity.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0;
            }
            return source.Identity.StartsWith("reanim/", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        }

        /// <summary>Calculates deterministic family weights before sorting, using only actual padded visible content tiles.</summary>
        /// <param name="sources">The decoded sources with native family and frequency category assigned.</param>
        private static void AssignFamilyAreas(List<Source> sources)
        {
            var priorities = new Dictionary<int, Dictionary<string, long>>();
            foreach (var source in sources)
            {
                if (!priorities.TryGetValue(source.PackingPriority, out var families))
                {
                    families = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    priorities.Add(source.PackingPriority, families);
                }
                families.TryGetValue(source.PackingFamily, out var area);
                families[source.PackingFamily] = area + PaddedSourceArea(source);
            }
            foreach (var source in sources)
            {
                source.PackingFamilyArea = priorities[source.PackingPriority][source.PackingFamily];
            }
        }

        /// <summary>Measures a source's trimmed, losslessly tiled footprint including bilinear sampling padding.</summary>
        /// <param name="source">The original decoded pixels and conservative visible geometry.</param>
        /// <returns>The total padded content area for the default maximum page size.</returns>
        private static long PaddedSourceArea(Source source)
        {
            if (source.Geometry.Length == 0)
            {
                return 0;
            }
            var bounds = source.Geometry[0];
            foreach (var geometry in source.Geometry)
            {
                bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, geometry.xMin), Mathf.Min(bounds.yMin, geometry.yMin),
                    Mathf.Max(bounds.xMax, geometry.xMax), Mathf.Max(bounds.yMax, geometry.yMax));
            }
            var capacity = SpriteGeometryUtility.MaximumPageSize - Padding * 2;
            var area = 0L;
            for (var y = (int)bounds.yMin; y < (int)bounds.yMax; y += capacity)
            {
                for (var x = (int)bounds.xMin; x < (int)bounds.xMax; x += capacity)
                {
                    var width = Math.Min(capacity, (int)bounds.xMax - x);
                    var height = Math.Min(capacity, (int)bounds.yMax - y);
                    var tile = new Rect(x, y, width, height);
                    foreach (var geometry in source.Geometry)
                    {
                        if (SpriteGeometryUtility.Intersect(tile, geometry, out _))
                        {
                            area += (long)(width + Padding * 2) * (height + Padding * 2);
                            break;
                        }
                    }
                }
            }
            return area;
        }

        /// <summary>Groups the native filename families that are normally sampled together within one actor.</summary>
        /// <param name="identity">The original relative source identity.</param>
        /// <returns>A stable native artwork-family key.</returns>
        private static string SourceFamily(string identity)
        {
            var name = Path.GetFileNameWithoutExtension(identity);
            var separator = name.IndexOf('_');
            if (separator < 0)
            {
                return name;
            }
            if (name.StartsWith("Zombie_", StringComparison.OrdinalIgnoreCase))
            {
                var next = name.IndexOf('_', separator + 1);
                var part = next < 0 ? name.Substring(separator + 1) : name.Substring(separator + 1, next - separator - 1);
                switch (part.ToLowerInvariant())
                {
                    case "football":
                    case "paper":
                    case "digger":
                    case "gargantuar":
                    case "boss":
                    case "dolphinrider":
                    case "snorkle":
                    case "dancer":
                    case "disco":
                    case "bobsled":
                    case "balloon":
                    case "catapult":
                    case "yeti":
                    case "polevaulter":
                    case "imp":
                    case "ladder":
                    case "jackbox":
                    case "zamboni":
                    case "zambonidriver":
                    case "bungi":
                    case "pogo":
                        return "Zombie_" + part;
                    default:
                        return "Zombie";
                }
            }
            return name.Substring(0, separator);
        }

        /// <summary>Packs one complete size variant, losslessly tiling any source larger than a padded page.</summary>
        /// <param name="sources">The unchanged original source pixels.</param>
        /// <param name="maximum">The maximum page edge in this variant.</param>
        /// <param name="outputRoot">The generated Resources asset directory.</param>
        /// <param name="generated">The set of owned outputs retained after generation.</param>
        /// <returns>The complete variant metadata.</returns>
        private static SpriteAtlasLayout BakeLayout(List<Source> sources, int maximum, string outputRoot, HashSet<string> generated)
        {
            var pages = new List<Page>();
            var images = new List<SpriteAtlasImage>();
            var familyPlacements = new List<Placement>();
            string? family = null;
            var priority = -1;
            var dedicated = false;
            var capacity = maximum - Padding * 2;
            foreach (var source in sources)
            {
                if (priority != source.PackingPriority || !string.Equals(family, source.PackingFamily, StringComparison.OrdinalIgnoreCase))
                {
                    PlaceFamily(pages, maximum, familyPlacements, dedicated);
                    familyPlacements.Clear();
                    family = source.PackingFamily;
                    priority = source.PackingPriority;
                    dedicated = source.DedicatedPage;
                }
                var image = new SpriteAtlasImage { SourceIdentity = source.Identity, Aliases = source.Aliases.ToArray(), Width = source.Width,
                    Height = source.Height, VisiblePixels = source.Geometry };
                var tiles = new List<SpriteAtlasTile>();
                if (source.Geometry.Length > 0)
                {
                    var bounds = source.Geometry[0];
                    foreach (var geometry in source.Geometry)
                    {
                        bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, geometry.xMin), Mathf.Min(bounds.yMin, geometry.yMin),
                            Mathf.Max(bounds.xMax, geometry.xMax), Mathf.Max(bounds.yMax, geometry.yMax));
                    }
                    for (var y = (int)bounds.yMin; y < (int)bounds.yMax; y += capacity)
                    {
                        for (var x = (int)bounds.xMin; x < (int)bounds.xMax; x += capacity)
                        {
                            var rectangle = new RectInt(x, y, Math.Min(capacity, (int)bounds.xMax - x), Math.Min(capacity, (int)bounds.yMax - y));
                            var visible = false;
                            foreach (var geometry in source.Geometry)
                            {
                                if (SpriteGeometryUtility.Intersect(geometry, new Rect(rectangle.x, rectangle.y, rectangle.width, rectangle.height), out _))
                                {
                                    visible = true;
                                    break;
                                }
                            }
                            if (!visible)
                            {
                                continue;
                            }
                            var tile = new SpriteAtlasTile { SourcePixels = new Rect(rectangle.x, rectangle.y, rectangle.width, rectangle.height) };
                            familyPlacements.Add(new Placement { Source = source, SourcePixels = rectangle, Tile = tile });
                            tiles.Add(tile);
                        }
                    }
                }
                image.Tiles = tiles.ToArray();
                images.Add(image);
            }
            PlaceFamily(pages, maximum, familyPlacements, dedicated);
            var pagePaths = new string[pages.Count];
            for (var index = 0; index < pages.Count; index++)
            {
                var path = outputRoot + "/Layouts/" + maximum + "/page-" + index.ToString("D4") + ".png";
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                WritePage(pages[index], path);
                generated.Add(path);
                pagePaths[index] = ResourcePath(path);
            }
            return new SpriteAtlasLayout { MaximumPageSize = maximum, Pages = pagePaths, Images = images.ToArray() };
        }

        /// <summary>Packs a complete actor family together instead of scattering its small parts into unrelated old-page gaps.</summary>
        /// <param name="pages">The existing independent page bins.</param>
        /// <param name="maximum">The maximum page edge in this device variant.</param>
        /// <param name="placements">All lossless content tiles belonging to one native family.</param>
        /// <param name="dedicated">Whether the family must own independent pages so unrelated assets cannot inflate its residency.</param>
        private static void PlaceFamily(List<Page> pages, int maximum, List<Placement> placements, bool dedicated)
        {
            if (placements.Count == 0)
            {
                return;
            }
            for (var index = 0; !dedicated && index < pages.Count; index++)
            {
                if (pages[index].Dedicated)
                {
                    continue;
                }
                if (TryPlaceFamily(pages[index], maximum, placements, index, out var packed))
                {
                    pages[index] = packed!;
                    return;
                }
            }
            var firstPage = pages.Count;
            if (TryPlaceFamily(new Page(maximum) { Dedicated = dedicated }, maximum, placements, firstPage, out var newPage))
            {
                pages.Add(newPage!);
                return;
            }
            // A family larger than a complete page must span pages, but its parts remain within newly allocated family pages.
            pages.Add(new Page(maximum) { Dedicated = dedicated });
            foreach (var placement in placements)
            {
                Place(pages, maximum, placement, firstPage);
            }
        }

        /// <summary>Dry-runs all family rectangles against one page and commits metadata only after every rectangle fits.</summary>
        /// <param name="original">The unmodified candidate page.</param>
        /// <param name="maximum">The maximum page edge.</param>
        /// <param name="placements">The complete family content tiles.</param>
        /// <param name="pageIndex">The stable page index assigned if the complete group fits.</param>
        /// <param name="packed">Receives the committed page clone, or null when the complete family does not fit.</param>
        /// <returns>True when the entire family can share this page.</returns>
        private static bool TryPlaceFamily(Page original, int maximum, List<Placement> placements, int pageIndex, out Page? packed)
        {
            packed = null;
            var candidate = new Page(maximum) { Width = original.Width, Height = original.Height, Dedicated = original.Dedicated };
            candidate.Free.Clear();
            candidate.Free.AddRange(original.Free);
            candidate.Placements.AddRange(original.Placements);
            var destinations = new RectInt[placements.Count];
            for (var index = 0; index < placements.Count; index++)
            {
                var placement = placements[index];
                var width = placement.SourcePixels.width + Padding * 2;
                var height = placement.SourcePixels.height + Padding * 2;
                if (!TryAllocate(candidate, width, height, out destinations[index]))
                {
                    return false;
                }
            }
            for (var index = 0; index < placements.Count; index++)
            {
                var placement = placements[index];
                placement.Destination = destinations[index];
                placement.Tile.Page = pageIndex;
                candidate.Placements.Add(placement);
            }
            packed = candidate;
            return true;
        }

        /// <summary>Allocates a padded rectangle inside a single deterministic guillotine-packed page.</summary>
        /// <param name="page">The page whose free-space state may be updated.</param>
        /// <param name="width">The requested padded width.</param>
        /// <param name="height">The requested padded height.</param>
        /// <param name="destination">Receives the allocated bottom-left page rectangle.</param>
        /// <returns>True when the rectangle fits without rotating or scaling.</returns>
        private static bool TryAllocate(Page page, int width, int height, out RectInt destination)
        {
            destination = default;
            var selected = -1;
            var bestWaste = int.MaxValue;
            for (var index = 0; index < page.Free.Count; index++)
            {
                var free = page.Free[index];
                var waste = free.width * free.height - width * height;
                if (free.width >= width && free.height >= height && waste < bestWaste)
                {
                    selected = index;
                    bestWaste = waste;
                }
            }
            if (selected < 0)
            {
                return false;
            }
            var space = page.Free[selected];
            page.Free.RemoveAt(selected);
            if (space.width > width)
            {
                page.Free.Add(new RectInt(space.x + width, space.y, space.width - width, height));
            }
            if (space.height > height)
            {
                page.Free.Add(new RectInt(space.x, space.y + height, space.width, space.height - height));
            }
            destination = new RectInt(space.x, space.y, width, height);
            page.Width = Math.Max(page.Width, space.x + width);
            page.Height = Math.Max(page.Height, space.y + height);
            return true;
        }

        /// <summary>Finds a deterministic best-fit free rectangle, creating another page when necessary.</summary>
        /// <param name="pages">The existing page bins.</param>
        /// <param name="maximum">The maximum page edge.</param>
        /// <param name="placement">The original content tile receiving its padded destination.</param>
        /// <param name="firstPage">The first page belonging to the oversized family.</param>
        private static void Place(List<Page> pages, int maximum, Placement placement, int firstPage)
        {
            var width = placement.SourcePixels.width + Padding * 2;
            var height = placement.SourcePixels.height + Padding * 2;
            var selectedPage = -1;
            var selectedFree = -1;
            var bestWaste = int.MaxValue;
            for (var pageIndex = firstPage; pageIndex < pages.Count; pageIndex++)
            {
                var page = pages[pageIndex];
                for (var freeIndex = 0; freeIndex < page.Free.Count; freeIndex++)
                {
                    var free = page.Free[freeIndex];
                    var waste = free.width * free.height - width * height;
                    if (free.width >= width && free.height >= height && waste < bestWaste)
                    {
                        selectedPage = pageIndex;
                        selectedFree = freeIndex;
                        bestWaste = waste;
                    }
                }
            }
            if (selectedPage < 0)
            {
                selectedPage = pages.Count;
                pages.Add(new Page(maximum) { Dedicated = pages[firstPage].Dedicated });
                selectedFree = 0;
            }
            var selected = pages[selectedPage];
            var space = selected.Free[selectedFree];
            selected.Free.RemoveAt(selectedFree);
            if (space.width > width)
            {
                selected.Free.Add(new RectInt(space.x + width, space.y, space.width - width, height));
            }
            if (space.height > height)
            {
                selected.Free.Add(new RectInt(space.x, space.y + height, space.width, space.height - height));
            }
            placement.Destination = new RectInt(space.x, space.y, width, height);
            placement.Tile.Page = selectedPage;
            selected.Width = Math.Max(selected.Width, space.x + width);
            selected.Height = Math.Max(selected.Height, space.y + height);
            selected.Placements.Add(placement);
        }

        /// <summary>Writes one exact RGBA page, using neighboring original pixels in internal tile halos.</summary>
        /// <param name="page">The packed page and original source placements.</param>
        /// <param name="path">The generated PNG asset path.</param>
        private static void WritePage(Page page, string path)
        {
            var width = Mathf.NextPowerOfTwo(Math.Max(4, page.Width));
            var height = Mathf.NextPowerOfTwo(Math.Max(4, page.Height));
            var pixels = new Color32[width * height];
            foreach (var placement in page.Placements)
            {
                var original = placement.Source;
                var source = placement.SourcePixels;
                var destination = placement.Destination;
                for (var y = 0; y < destination.height; y++)
                {
                    var sourceY = Math.Max(0, Math.Min(original.Height - 1, source.y + y - Padding));
                    for (var x = 0; x < destination.width; x++)
                    {
                        var sourceX = Math.Max(0, Math.Min(original.Width - 1, source.x + x - Padding));
                        pixels[(destination.y + y) * width + destination.x + x] = original.Pixels[sourceY * original.Width + sourceX];
                    }
                }
                var tile = placement.Tile;
                tile.TextureUv = new Rect((destination.x + Padding) / (float)width, (destination.y + Padding) / (float)height,
                    source.width / (float)width, source.height / (float)height);
                tile.PageWidth = width;
                tile.PageHeight = height;
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Applies exact uncompressed import settings without resizing source assets or creating readable copies.</summary>
        /// <param name="catalog">The generated complete layout metadata.</param>
        /// <param name="outputRoot">The generated Resources directory.</param>
        private static void ConfigureTextureImports(SpriteAtlasCatalog catalog, string outputRoot)
        {
            foreach (var layout in catalog.Layouts)
            {
                foreach (var page in layout.Pages)
                {
                    var path = AssetPath(outputRoot, page) + ".png";
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        throw new InvalidOperationException("Generated atlas page was not imported: " + path);
                    }
                    SpriteAtlasTextureImport.Configure(importer, layout.MaximumPageSize);
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>Removes only obsolete generated files, pairing asset deletion with Unity metadata deletion.</summary>
        /// <param name="outputRoot">The directory owned exclusively by this generator.</param>
        /// <param name="generated">The complete set of current owned output paths.</param>
        private static void RemoveObsoleteOutputs(string outputRoot, HashSet<string> generated)
        {
            foreach (var path in Directory.GetFiles(outputRoot, "*", SearchOption.AllDirectories))
            {
                var normalized = path.Replace('\\', '/');
                if (normalized.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || generated.Contains(normalized))
                {
                    continue;
                }
                AssetDatabase.DeleteAsset(normalized);
            }
        }

        /// <summary>Converts a generated asset path into an extensionless Resources key.</summary>
        /// <param name="assetPath">The generated Unity asset path.</param>
        /// <returns>The resource key after the final Resources directory.</returns>
        private static string ResourcePath(string assetPath)
        {
            var normalized = assetPath.Replace('\\', '/');
            var start = normalized.LastIndexOf("/Resources/", StringComparison.Ordinal) + "/Resources/".Length;
            return normalized.Substring(start, normalized.Length - start - Path.GetExtension(normalized).Length);
        }

        /// <summary>Converts a generated Resources key back to its owned asset path without an extension.</summary>
        /// <param name="outputRoot">The generated Resources directory.</param>
        /// <param name="resourcePath">The extensionless generated Resources key.</param>
        /// <returns>The matching extensionless asset path.</returns>
        private static string AssetPath(string outputRoot, string resourcePath)
        {
            var normalized = outputRoot.Replace('\\', '/');
            var end = normalized.LastIndexOf("/Resources/", StringComparison.Ordinal) + "/Resources/".Length;
            return normalized.Substring(0, end) + resourcePath;
        }
    }
}
