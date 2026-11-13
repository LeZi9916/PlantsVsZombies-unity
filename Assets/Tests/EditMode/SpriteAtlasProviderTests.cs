#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks device selection, cropped atlas geometry, recoverable whole-layout fallback, and external compatibility.</summary>
    public sealed class SpriteAtlasProviderTests
    {
        /// <summary>The temporary textures owned only by these tests.</summary>
        private readonly List<Texture2D> _textures = new List<Texture2D>();

        /// <summary>Destroys generated test textures after every test.</summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var texture in _textures)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            _textures.Clear();
        }

        /// <summary>Checks the hardware limit is an upper bound rather than the desired allocation size.</summary>
        /// <param name="hardwareMaximum">The simulated graphics device's maximum texture edge.</param>
        /// <param name="expected">The expected memory-bounded baked page size.</param>
        [TestCase(256, 256)]
        [TestCase(768, 512)]
        [TestCase(1024, 1024)]
        [TestCase(16384, 2048)]
        public void DeviceSelectionUsesSupportedBakedPagesAndSixteenMiBBudget(int hardwareMaximum, int expected)
        {
            var layouts = new[] { new SpriteAtlasLayout { MaximumPageSize = 2048 }, new SpriteAtlasLayout { MaximumPageSize = 256 },
                new SpriteAtlasLayout { MaximumPageSize = 1024 }, new SpriteAtlasLayout { MaximumPageSize = 512 } };
            var selected = SpriteGeometryUtility.SelectLayout(layouts, hardwareMaximum);
            Assert.That(layouts[selected].MaximumPageSize, Is.EqualTo(expected));
            Assert.That(SpriteGeometryUtility.SelectLayout(layouts, 128), Is.EqualTo(-1));
            var budgetLimited = SpriteGeometryUtility.SelectLayout(layouts, 16384, 1024L * 1024);
            Assert.That(layouts[budgetLimited].MaximumPageSize, Is.EqualTo(512));
        }

        /// <summary>Checks source identity and dimensions do not allocate or load a GPU source texture.</summary>
        [Test]
        public void LogicalImageLoadsNoPageUntilItsVisibleRegionsAreRequested()
        {
            var calls = 0;
            var page = CreateTexture(4, 4);
            var catalog = Catalog(Layout(256, "small", false));
            using var provider = new SpriteAssetProvider(catalog, 1024, _ =>
            {
                calls++;
                return page;
            }, IgnoreReleasedTexture);
            Assert.That(provider.MaxPageSize, Is.EqualTo(256));
            var image = provider.LoadImage("IMAGE_FIXTURE");
            Assert.That(image.Width, Is.EqualTo(8));
            Assert.That(image.Height, Is.EqualTo(4));
            Assert.That(image.SourceIdentity, Is.EqualTo("images/fixture.png"));
            Assert.That(image.SourceTexture, Is.Null);
            Assert.That(calls, Is.Zero);
            Assert.That(image.GetRegionCount(new Rect(0, 0, 1, 1)), Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(1));
        }

        /// <summary>Checks arbitrary original sheet crops map independently to multiple losslessly tiled pages.</summary>
        [Test]
        public void PartialSourceUvCrossingTilesPreservesOriginalCoordinates()
        {
            var first = CreateTexture(4, 4);
            var second = CreateTexture(4, 4);
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", true)), 256,
                path => path.EndsWith("0", StringComparison.Ordinal) ? first : second, IgnoreReleasedTexture);
            var image = provider.LoadImage("images/fixture.png");
            var source = new Rect(.25f, .25f, .5f, .5f);
            Assert.That(image.GetRegionCount(source), Is.EqualTo(2));
            Assert.That(image.TryGetRegion(source, 0, out var left), Is.True);
            Assert.That(image.TryGetRegion(source, 1, out var right), Is.True);
            Assert.That(left.SourceUv, Is.EqualTo(new Rect(.25f, .25f, .25f, .5f)));
            Assert.That(right.SourceUv, Is.EqualTo(new Rect(.5f, .25f, .25f, .5f)));
            Assert.That(left.TextureUv, Is.EqualTo(new Rect(.5f, .25f, .5f, .5f)));
            Assert.That(right.TextureUv, Is.EqualTo(new Rect(0f, .25f, .5f, .5f)));
            Assert.That(left.Texture, Is.SameAs(first));
            Assert.That(right.Texture, Is.SameAs(second));
            Assert.That(image.TryGetRegion(source, 2, out _), Is.False);
        }

        /// <summary>Checks an observed resource-loading failure switches the complete image mapping to a smaller layout.</summary>
        [Test]
        public void MissingPageRetriesAnEntireSmallerLayoutWithoutMixingPageCoordinates()
        {
            var calls = new List<string>();
            var page = CreateTexture(4, 4);
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", false), Layout(512, "large", false)), 1024,
                path =>
                {
                    calls.Add(path);
                    return path.StartsWith("large", StringComparison.Ordinal) ? null : page;
                }, IgnoreReleasedTexture);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            var version = provider.LayoutVersion;
            Assert.That(image.GetRegionCount(new Rect(0, 0, 1, 1)), Is.EqualTo(1));
            Assert.That(provider.MaxPageSize, Is.EqualTo(256));
            Assert.That(provider.LayoutVersion, Is.GreaterThan(version));
            Assert.That(calls, Is.EqualTo(new[] { "large0", "small0" }));
            Assert.That(image.TryGetRegion(new Rect(0, 0, 1, 1), 0, out var region), Is.True);
            Assert.That(region.Texture, Is.SameAs(page));
        }

        /// <summary>Checks graphics allocation exhaustion is not advertised as a recoverable missing-resource condition.</summary>
        [Test]
        public void OutOfMemoryIsNotCaughtOrRetriedAsAnOrdinaryLayoutFailure()
        {
            var calls = 0;
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", false), Layout(512, "large", false)), 1024,
                _ =>
                {
                    calls++;
                    throw new OutOfMemoryException("Injected allocation failure");
                }, IgnoreReleasedTexture);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            Assert.Throws<OutOfMemoryException>(() => image.GetRegionCount(new Rect(0, 0, 1, 1)));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(provider.MaxPageSize, Is.EqualTo(512));
        }

        /// <summary>Checks unexpected importer resizing is rejected before any UVs from that page are submitted.</summary>
        [Test]
        public void UnexpectedPageDimensionsFailRatherThanSilentlyResizingOriginalArtwork()
        {
            var page = CreateTexture(2, 2);
            var released = 0;
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", false)), 256, _ => page, _ => released++);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            Assert.Throws<InvalidOperationException>(() => image.GetRegionCount(new Rect(0, 0, 1, 1)));
            Assert.That(released, Is.EqualTo(1));
            Assert.That(provider.LoadedPageCount, Is.Zero);
        }

        /// <summary>Checks external compatibility wrappers retain out-of-range UV sampling and allocate only once.</summary>
        [Test]
        public void ExternalTextureWrapperIsCachedAndPreservesReversedOutOfRangeSources()
        {
            var texture = CreateTexture(4, 4);
            var image = SpriteImage.FromTexture(texture);
            Assert.That(SpriteImage.FromTexture(texture), Is.SameAs(image));
            var source = new Rect(2, 3, -4, -6);
            Assert.That(image.TryGetRegion(source, 0, out var region), Is.True);
            Assert.That(region.SourceUv, Is.EqualTo(new Rect(-2, -3, 4, 6)));
            Assert.That(region.TextureUv, Is.EqualTo(region.SourceUv));
            Assert.That(region.Texture, Is.SameAs(texture));
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100; index++)
            {
                SpriteImage.FromTexture(texture);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        /// <summary>Checks even a very faint source texel and its filtering halo survive conservative disjoint geometry.</summary>
        [Test]
        public void VisibleGeometryKeepsFaintPixelsAndOnePixelHaloWithoutOverlappingPieces()
        {
            var pixels = new Color32[24 * 16];
            pixels[7 * 24 + 7] = new Color32(12, 45, 67, 1);
            pixels[12 * 24 + 20] = new Color32(255, 0, 0, 255);
            var rectangles = SpriteGeometryUtility.BuildVisibleGeometry(pixels, 24, 16);
            Assert.That(rectangles.Length, Is.LessThanOrEqualTo(32));
            for (var y = 6; y <= 8; y++)
            {
                for (var x = 6; x <= 8; x++)
                {
                    var coverage = 0;
                    foreach (var rectangle in rectangles)
                    {
                        if (rectangle.Contains(new Vector2(x + .5f, y + .5f)))
                        {
                            coverage++;
                        }
                    }
                    Assert.That(coverage, Is.EqualTo(1), "The source texel and filtering halo must be retained exactly once.");
                }
            }
            for (var first = 0; first < rectangles.Length; first++)
            {
                for (var second = first + 1; second < rectangles.Length; second++)
                {
                    Assert.That(SpriteGeometryUtility.Intersect(rectangles[first], rectangles[second], out _), Is.False);
                }
            }
            Assert.That(SpriteGeometryUtility.BuildVisibleGeometry(new Color32[64], 8, 8), Is.Empty);
        }

        /// <summary>Checks external original resource aliases respect SetDefaults and extensionless PNG precedence.</summary>
        [Test]
        public void LooseResourceManifestAliasesResolveWithoutOriginalGpuDuplicates()
        {
            var root = Path.Combine(Application.temporaryCachePath, "PvZAtlasProvider-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "images"));
            Directory.CreateDirectory(Path.Combine(root, "properties"));
            var texture = CreateTexture(4, 4);
            File.WriteAllBytes(Path.Combine(root, "images", "atlas.png"), texture.EncodeToPNG());
            File.WriteAllText(Path.Combine(root, "properties", "Resources.xml"),
                "<ResourceManifest><Resources id=\"Fixture\"><SetDefaults path=\"images\" idprefix=\"IMAGE_\"/>"
                + "<Image id=\"CLIPATLAS\" path=\"atlas\"/></Resources></ResourceManifest>");
            File.WriteAllText(Path.Combine(root, "properties", "LawnStrings.txt"), "[HELLO]\nworld");
            try
            {
                using var provider = SpriteAssetProvider.ForRoot(root);
                Assert.That(provider.ContainsImage("images/atlas"), Is.True);
                Assert.That(provider.TryLoadImage("IMAGE_CLIPATLAS", out var image), Is.True);
                Assert.That(image, Is.SameAs(provider.LoadImage("images/atlas.png")));
                Assert.That(provider.TryLoadImage("images/optional.png", out _), Is.False);
                Assert.That(provider.TryReadText("properties/LawnStrings.txt", out var text), Is.True);
                Assert.That(text, Is.EqualTo("[HELLO]\nworld"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        /// <summary>Checks out-of-range source rectangles preserve Clamp extension across multiple atlas pages.</summary>
        [Test]
        public void BakedClampExtensionRetainsFullVirtualSourceRectangleAndFixedEdgeTexels()
        {
            var first = CreateTexture(4, 4);
            var second = CreateTexture(4, 4);
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", true)), 256,
                path => path.EndsWith("0", StringComparison.Ordinal) ? first : second, IgnoreReleasedTexture);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            var source = new Rect(1.5f, 1f, -2f, -1f);
            Assert.That(image.GetRegionCount(source), Is.EqualTo(4));
            Assert.That(image.TryGetRegion(source, 0, out var left), Is.True);
            Assert.That(left.SourceUv, Is.EqualTo(new Rect(-.5f, 0f, .5f, 1f)));
            Assert.That(left.TextureUv, Is.EqualTo(new Rect(.125f, 0f, 0f, 1f)));
            Assert.That(image.TryGetRegion(source, 3, out var right), Is.True);
            Assert.That(right.SourceUv, Is.EqualTo(new Rect(1f, 0f, .5f, 1f)));
            Assert.That(right.TextureUv, Is.EqualTo(new Rect(.875f, 0f, 0f, 1f)));
            var expanded = new Rect(-1, -1, 3, 3);
            var count = image.GetRegionCount(expanded);
            var coveredArea = 0f;
            for (var index = 0; index < count; index++)
            {
                Assert.That(image.TryGetRegion(expanded, index, out var region), Is.True);
                coveredArea += region.SourceUv.width * region.SourceUv.height;
                for (var previous = 0; previous < index; previous++)
                {
                    image.TryGetRegion(expanded, previous, out var other);
                    Assert.That(SpriteGeometryUtility.Intersect(region.SourceUv, other.SourceUv, out _), Is.False);
                }
            }
            Assert.That(coveredArea, Is.EqualTo(9f));
        }

        /// <summary>Checks Android jar URLs survive normalization and both production layouts share one ownership key.</summary>
        [Test]
        public void RootNormalizationPreservesJarUrlsAndCanonicalizesProductionProviderOwnership()
        {
            const string jar = "jar:file:///data/app/example/base.apk!/assets/PvZ";
            Assert.That(SpriteAssetProvider.NormalizeRoot(jar + "/"), Is.EqualTo(jar));
            var current = SpriteAssetProvider.NormalizeRoot(Application.streamingAssetsPath);
            Assert.That(SpriteAssetProvider.NormalizeProviderRoot(current + "/PvZ"), Is.EqualTo(current));
            Assert.That(SpriteAssetProvider.NormalizeProviderRoot(current), Is.EqualTo(current));
            var local = Path.Combine(Application.temporaryCachePath, "resources", "..", "images");
            Assert.That(SpriteAssetProvider.NormalizeRoot(local), Is.EqualTo(Path.GetFullPath(local).Replace('\\', '/')));
        }

        /// <summary>Checks unused page storage is evicted after published frames retire and reloaded when sampled again.</summary>
        [Test]
        public void InactivePagesAreEvictedAndLogicalImagesRemainReloadable()
        {
            var first = CreateTexture(4, 4);
            var second = CreateTexture(4, 4);
            var replacement = CreateTexture(4, 4);
            var layout = Layout(256, "small", false);
            layout.Pages = new[] { "small0", "small1" };
            var other = new SpriteAtlasImage { SourceIdentity = "images/other.png", Width = 4, Height = 4,
                VisiblePixels = new[] { new Rect(0, 0, 4, 4) }, Tiles = new[] { new SpriteAtlasTile { Page = 1, PageWidth = 4, PageHeight = 4,
                    SourcePixels = new Rect(0, 0, 4, 4), TextureUv = new Rect(0, 0, 1, 1) } } };
            layout.Images = new[] { layout.Images[0], other };
            var released = new List<Texture2D>();
            var loads = 0;
            var firstPageLoads = 0;
            using var provider = new SpriteAssetProvider(Catalog(layout), 256, path =>
            {
                loads++;
                if (path == "small0")
                {
                    firstPageLoads++;
                    return firstPageLoads == 1 ? first : replacement;
                }
                return second;
            }, released.Add);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            image.GetRegionCount(new Rect(0, 0, 1, 1));
            for (var frame = 0; frame < 3; frame++)
            {
                SpriteDrawState.BeginFrame();
            }
            provider.LoadImage("images/other.png").GetRegionCount(new Rect(0, 0, 1, 1));
            Assert.That(released, Is.EqualTo(new[] { first }));
            Assert.That(provider.LoadedPageCount, Is.EqualTo(1));
            image.GetRegionCount(new Rect(0, 0, 1, 1));
            Assert.That(loads, Is.EqualTo(3));
            Assert.That(image.TryGetRegion(new Rect(0, 0, 1, 1), 0, out var reloaded), Is.True);
            Assert.That(reloaded.Texture, Is.SameAs(replacement));
        }

        /// <summary>Checks warmed indexed reads reuse prepared regions without allocating iterators or rescanning allocations.</summary>
        [Test]
        public void WarmedMultiRegionReadsAndSourceCropChangesAllocateNoManagedMemory()
        {
            var page = CreateTexture(4, 4);
            using var provider = new SpriteAssetProvider(Catalog(Layout(256, "small", true)), 256, _ => page, IgnoreReleasedTexture);
            var image = provider.LoadImage("IMAGE_FIXTURE");
            var full = new Rect(0, 0, 1, 1);
            var crop = new Rect(.25f, .25f, .5f, .5f);
            for (var warm = 0; warm < 4; warm++)
            {
                image.GetRegionCount(full);
                image.GetRegionCount(crop);
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var repeat = 0; repeat < 100; repeat++)
            {
                var source = repeat % 2 == 0 ? full : crop;
                var count = image.GetRegionCount(source);
                for (var index = 0; index < count; index++)
                {
                    image.TryGetRegion(source, index, out _);
                }
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(image.TryGetRegion(crop, 0, out var region), Is.True);
            Assert.That(region.SourceUv, Is.EqualTo(new Rect(.25f, .25f, .25f, .5f)));
        }

        /// <summary>Checks differently mapped images retain independent prepared arrays across intervening submissions.</summary>
        [Test]
        public void AlternatingImagesRetainIndependentCachedPageReferences()
        {
            var first = CreateTexture(4, 4);
            var second = CreateTexture(4, 4);
            var layout = Layout(256, "small", false);
            layout.Pages = new[] { "small0", "small1" };
            var other = new SpriteAtlasImage { SourceIdentity = "images/other.png", Width = 4, Height = 4,
                VisiblePixels = new[] { new Rect(0, 0, 4, 4) }, Tiles = new[] { new SpriteAtlasTile { Page = 1, PageWidth = 4, PageHeight = 4,
                    SourcePixels = new Rect(0, 0, 4, 4), TextureUv = new Rect(0, 0, 1, 1) } } };
            layout.Images = new[] { layout.Images[0], other };
            using var provider = new SpriteAssetProvider(Catalog(layout), 256,
                path => path == "small0" ? first : second, IgnoreReleasedTexture);
            var original = provider.LoadImage("IMAGE_FIXTURE");
            var alternate = provider.LoadImage("images/other.png");
            var source = new Rect(0, 0, 1, 1);
            original.GetRegionCount(source);
            alternate.GetRegionCount(source);
            original.GetRegionCount(source);
            Assert.That(original.TryGetRegion(source, 0, out var region), Is.True);
            Assert.That(region.Texture, Is.SameAs(first));
            Assert.That(alternate.TryGetRegion(source, 0, out region), Is.True);
            Assert.That(region.Texture, Is.SameAs(second));
            Assert.That(provider.PreparedRegionCacheCount, Is.EqualTo(2));
        }

        /// <summary>Checks an already cached image replaces its page references after a different image forces whole-layout fallback.</summary>
        [Test]
        public void CachedImageRegionsInvalidateAfterAnotherImageForcesLayoutFallback()
        {
            var large = CreateTexture(4, 4);
            var smallFirst = CreateTexture(4, 4);
            var smallSecond = CreateTexture(4, 4);
            var largeLayout = Layout(512, "large", false);
            var smallLayout = Layout(256, "small", false);
            foreach (var layout in new[] { largeLayout, smallLayout })
            {
                var prefix = layout.MaximumPageSize == 512 ? "large" : "small";
                layout.Pages = new[] { prefix + "0", prefix + "1" };
                var other = new SpriteAtlasImage { SourceIdentity = "images/other.png", Width = 4, Height = 4,
                    VisiblePixels = new[] { new Rect(0, 0, 4, 4) }, Tiles = new[] { new SpriteAtlasTile { Page = 1, PageWidth = 4, PageHeight = 4,
                        SourcePixels = new Rect(0, 0, 4, 4), TextureUv = new Rect(0, 0, 1, 1) } } };
                layout.Images = new[] { layout.Images[0], other };
            }
            using var provider = new SpriteAssetProvider(Catalog(largeLayout, smallLayout), 512, path =>
            {
                if (path == "large0")
                {
                    return large;
                }
                if (path == "large1")
                {
                    return null;
                }
                return path == "small0" ? smallFirst : smallSecond;
            }, IgnoreReleasedTexture);
            var original = provider.LoadImage("IMAGE_FIXTURE");
            var otherImage = provider.LoadImage("images/other.png");
            var source = new Rect(0, 0, 1, 1);
            original.GetRegionCount(source);
            Assert.That(original.TryGetRegion(source, 0, out var before), Is.True);
            Assert.That(before.Texture, Is.SameAs(large));
            otherImage.GetRegionCount(source);
            Assert.That(provider.MaxPageSize, Is.EqualTo(256));
            original.GetRegionCount(source);
            Assert.That(original.TryGetRegion(source, 0, out var after), Is.True);
            Assert.That(after.Texture, Is.SameAs(smallFirst));
            Assert.That(otherImage.TryGetRegion(source, 0, out var alternate), Is.True);
            Assert.That(alternate.Texture, Is.SameAs(smallSecond));
        }

        /// <summary>Checks warmed cross-image and cross-crop cache switching remains allocation-free over complete frame changes.</summary>
        [Test]
        public void WarmedCrossImageAndCropRegionReadsAllocateNoManagedMemory()
        {
            var texture = CreateTexture(4, 4);
            var layout = ManyImageLayout(50);
            using var provider = new SpriteAssetProvider(Catalog(layout), 256, _ => texture, IgnoreReleasedTexture);
            var images = new SpriteImage[layout.Images.Length];
            for (var index = 0; index < images.Length; index++)
            {
                images[index] = provider.LoadImage(layout.Images[index].SourceIdentity);
            }
            var sources = new[] { new Rect(0, 0, 1, 1), new Rect(.25f, .25f, .5f, .5f), new Rect(1.5f, 1f, -2f, -1f) };
            ReadImageCrops(images, sources);
            ReadImageCrops(images, sources);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 4; frame++)
            {
                SpriteDrawState.BeginFrame();
                ReadImageCrops(images, sources);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(provider.PreparedRegionCacheCount, Is.EqualTo(150));
        }

        /// <summary>Checks the cache remains bounded and reuses evicted arrays when a larger warmed working set cycles through it.</summary>
        [Test]
        public void PreparedRegionCacheRetainsAtMostFiveHundredTwelveEntries()
        {
            var texture = CreateTexture(4, 4);
            var layout = ManyImageLayout(600);
            using var provider = new SpriteAssetProvider(Catalog(layout), 256, _ => texture, IgnoreReleasedTexture);
            var images = new SpriteImage[layout.Images.Length];
            for (var index = 0; index < images.Length; index++)
            {
                images[index] = provider.LoadImage(layout.Images[index].SourceIdentity);
            }
            var sources = new[] { new Rect(0, 0, 1, 1), new Rect(.25f, .25f, .5f, .5f) };
            ReadImageCrops(images, sources);
            ReadImageCrops(images, sources);
            var before = GC.GetAllocatedBytesForCurrentThread();
            ReadImageCrops(images, sources);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(provider.PreparedRegionCacheCount, Is.EqualTo(512));
        }

        /// <summary>Reads all visible cropped regions while alternating among logical images.</summary>
        /// <param name="images">The already loaded logical image handles.</param>
        /// <param name="sources">The previously warmed original source rectangles.</param>
        private static void ReadImageCrops(SpriteImage[] images, Rect[] sources)
        {
            foreach (var source in sources)
            {
                foreach (var image in images)
                {
                    var count = image.GetRegionCount(source);
                    for (var index = 0; index < count; index++)
                    {
                        image.TryGetRegion(source, index, out _);
                    }
                }
            }
        }

        /// <summary>Creates independent logical identities sharing the same two physical source pages.</summary>
        /// <param name="count">The number of independent logical image identities.</param>
        /// <returns>A complete layout for cache switching and bounded replacement regressions.</returns>
        private static SpriteAtlasLayout ManyImageLayout(int count)
        {
            var layout = Layout(256, "small", true);
            var prototype = layout.Images[0];
            var images = new SpriteAtlasImage[count];
            for (var index = 0; index < count; index++)
            {
                images[index] = new SpriteAtlasImage { SourceIdentity = "images/cache-" + index + ".png", Width = prototype.Width,
                    Height = prototype.Height, VisiblePixels = prototype.VisiblePixels, Tiles = prototype.Tiles };
            }
            layout.Images = images;
            return layout;
        }

        /// <summary>Leaves injected textures owned by the fixture rather than the provider.</summary>
        /// <param name="texture">The page whose final destruction belongs to fixture teardown.</param>
        private static void IgnoreReleasedTexture(Texture2D texture)
        {
        }

        /// <summary>Creates a small texture owned by this test fixture.</summary>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <returns>The temporary texture.</returns>
        private Texture2D CreateTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _textures.Add(texture);
            return texture;
        }

        /// <summary>Creates a complete injected catalog for layout-selection regressions.</summary>
        /// <param name="layouts">The independently mapped compatible layouts.</param>
        /// <returns>The test catalog.</returns>
        private static SpriteAtlasCatalog Catalog(params SpriteAtlasLayout[] layouts)
        {
            return new SpriteAtlasCatalog { Layouts = layouts };
        }

        /// <summary>Creates an eight-by-four logical source mapped to one or two small test pages.</summary>
        /// <param name="maximum">The selected layout's maximum permitted page size.</param>
        /// <param name="pagePrefix">The injected page identity prefix.</param>
        /// <param name="split">Whether the original image spans two independent pages.</param>
        /// <returns>A complete test layout with exact source and page UV metadata.</returns>
        private static SpriteAtlasLayout Layout(int maximum, string pagePrefix, bool split)
        {
            var first = new SpriteAtlasTile { Page = 0, SourcePixels = new Rect(0, 0, split ? 4 : 8, 4),
                TextureUv = new Rect(0, 0, 1, 1), PageWidth = 4, PageHeight = 4 };
            var second = new SpriteAtlasTile { Page = 1, SourcePixels = new Rect(4, 0, 4, 4),
                TextureUv = new Rect(0, 0, 1, 1), PageWidth = 4, PageHeight = 4 };
            return new SpriteAtlasLayout
            {
                MaximumPageSize = maximum,
                Pages = split ? new[] { pagePrefix + "0", pagePrefix + "1" } : new[] { pagePrefix + "0" },
                Images = new[] { new SpriteAtlasImage { SourceIdentity = "images/fixture.png", Aliases = new[] { "IMAGE_FIXTURE" },
                    Width = 8, Height = 4, VisiblePixels = new[] { new Rect(0, 0, 8, 4) }, Tiles = split ? new[] { first, second } : new[] { first } } }
            };
        }
    }
}
