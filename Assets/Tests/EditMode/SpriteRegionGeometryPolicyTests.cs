#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects separate scene and native-capture geometry policies without changing source pixels or allocating warmed lookups.</summary>
    public sealed class SpriteRegionGeometryPolicyTests
    {
        /// <summary>The odd original source width exposing normalized-coordinate round trips.</summary>
        private const int SourceWidth = 17;
        /// <summary>The odd original source height exposing normalized-coordinate round trips.</summary>
        private const int SourceHeight = 19;
        /// <summary>The imported page supplied by this isolated metadata fixture.</summary>
        private Texture2D _page = null!;
        /// <summary>The injected provider retaining the two policy-specific caches.</summary>
        private SpriteAssetProvider _provider = null!;
        /// <summary>The odd-sized logical source whose visible cells form a disjoint L shape.</summary>
        private SpriteImage _image = null!;

        /// <summary>Creates one page containing a complete odd-sized source and two conservative scene rectangles.</summary>
        [SetUp]
        public void SetUp()
        {
            _page = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[]
                {
                    new SpriteAtlasLayout
                    {
                        MaximumPageSize = 256,
                        Pages = new[] { "odd-source-page" },
                        Images = new[]
                        {
                            new SpriteAtlasImage
                            {
                                SourceIdentity = "images/odd-source.png",
                                Width = SourceWidth,
                                Height = SourceHeight,
                                VisiblePixels = new[] { new Rect(0f, 0f, 8f, 19f), new Rect(8f, 8f, 9f, 11f) },
                                Tiles = new[]
                                {
                                    new SpriteAtlasTile
                                    {
                                        Page = 0,
                                        PageWidth = 32,
                                        PageHeight = 32,
                                        SourcePixels = new Rect(0f, 0f, SourceWidth, SourceHeight),
                                        TextureUv = new Rect(3f / 32f, 5f / 32f, SourceWidth / 32f, SourceHeight / 32f)
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _provider = new SpriteAssetProvider(catalog, 256, _ => _page, IgnorePageRelease);
            _image = _provider.LoadImage("images/odd-source.png");
        }

        /// <summary>Releases only this fixture's injected page and metadata cache.</summary>
        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
            UnityEngine.Object.DestroyImmediate(_page);
        }

        /// <summary>Keeps public scene reads tight even after the same source and crop were prepared as one capture tile.</summary>
        [Test]
        public void AlternatingPoliciesRetainSeparateCachesAndPublicDefaults()
        {
            var source = new Rect(0f, 0f, 1f, 1f);
            for (var iteration = 0; iteration < 4; iteration++)
            {
                Assert.That(_image.GetRegionCount(source, false), Is.EqualTo(1));
                Assert.That(_image.TryGetRegion(source, 0, false, out var capture), Is.True);
                Assert.That(capture.SourcePixels, Is.EqualTo(new Rect(0f, 0f, 17f, 19f)));
                Assert.That(capture.SourcePixels.xMax, Is.EqualTo(17f));
                Assert.That(capture.SourcePixels.yMax, Is.EqualTo(19f));
                Assert.That(_image.GetRegionCount(source), Is.EqualTo(2));
                Assert.That(_image.TryGetRegion(source, 0, out var first), Is.True);
                Assert.That(_image.TryGetRegion(source, 1, out var second), Is.True);
                Assert.That(first.SourcePixels, Is.EqualTo(new Rect(0f, 0f, 8f, 19f)));
                Assert.That(second.SourcePixels, Is.EqualTo(new Rect(8f, 8f, 9f, 11f)));
                Assert.That(second.SourcePixels.xMax, Is.EqualTo(17f));
                Assert.That(second.SourcePixels.yMax, Is.EqualTo(19f));
                Assert.That(_image.TryGetRegion(source, 1, false, out _), Is.False);
            }

            Assert.That(_provider.PreparedRegionCacheCount, Is.EqualTo(2));
        }

        /// <summary>Preserves exact metadata edges and paired atlas sampling when odd-sized sources are cropped or reversed.</summary>
        [Test]
        public void OddSourcePartialAndInvertedCropsPreservePixelCoordinates()
        {
            var forward = new Rect(4f / 17f, 3f / 19f, 9f / 17f, 12f / 19f);
            var inverted = new Rect(13f / 17f, 15f / 19f, -9f / 17f, -12f / 19f);
            foreach (var tight in new[] { false, true })
            {
                var expected = ReadRegions(forward, tight);
                var reversed = ReadRegions(inverted, tight);
                Assert.That(expected.Length, Is.EqualTo(tight ? 2 : 1));
                Assert.That(reversed.Length, Is.EqualTo(expected.Length));
                for (var index = 0; index < expected.Length; index++)
                {
                    AssertRect(reversed[index].SourcePixels, expected[index].SourcePixels);
                    AssertRect(reversed[index].SourceUv, expected[index].SourceUv);
                    AssertRect(reversed[index].TextureUv, expected[index].TextureUv);
                    Assert.That(reversed[index].Texture, Is.SameAs(_page));
                }

                if (tight)
                {
                    AssertRect(expected[0].SourcePixels, new Rect(4f, 3f, 4f, 12f));
                    AssertRect(expected[1].SourcePixels, new Rect(8f, 8f, 5f, 7f));
                }
                else
                {
                    AssertRect(expected[0].SourcePixels, new Rect(4f, 3f, 9f, 12f));
                    AssertRect(expected[0].TextureUv, new Rect(7f / 32f, 8f / 32f, 9f / 32f, 12f / 32f));
                }
            }
        }

        /// <summary>Retains disjoint virtual edge and corner geometry with constant edge-texel sampling in both policies.</summary>
        [Test]
        public void InvertedVirtualClampExtensionsPreservePixelsAndPolicyCoverage()
        {
            var forward = new Rect(-1f / 17f, -2f / 19f, 19f / 17f, 23f / 19f);
            var inverted = new Rect(18f / 17f, 21f / 19f, -19f / 17f, -23f / 19f);
            foreach (var tight in new[] { false, true })
            {
                var regions = ReadRegions(forward, tight);
                var reversed = ReadRegions(inverted, tight);
                Assert.That(regions.Length, Is.EqualTo(tight ? 10 : 9));
                Assert.That(reversed.Length, Is.EqualTo(regions.Length));
                var area = 0f;
                for (var index = 0; index < regions.Length; index++)
                {
                    var region = regions[index];
                    var pixels = region.SourcePixels;
                    AssertRect(reversed[index].SourcePixels, pixels);
                    AssertRect(reversed[index].TextureUv, region.TextureUv);
                    Assert.That(pixels.width, Is.GreaterThan(0f));
                    Assert.That(pixels.height, Is.GreaterThan(0f));
                    area += pixels.width * pixels.height;
                    if (pixels.xMax <= 0f || pixels.xMin >= SourceWidth)
                    {
                        Assert.That(region.TextureUv.width, Is.Zero);
                        var edge = pixels.xMax <= 0f ? 3.5f : 19.5f;
                        Assert.That(region.TextureUv.x, Is.EqualTo(edge / 32f).Within(.000001f));
                    }

                    if (pixels.yMax <= 0f || pixels.yMin >= SourceHeight)
                    {
                        Assert.That(region.TextureUv.height, Is.Zero);
                        var edge = pixels.yMax <= 0f ? 5.5f : 23.5f;
                        Assert.That(region.TextureUv.y, Is.EqualTo(edge / 32f).Within(.000001f));
                    }

                    for (var other = 0; other < index; other++)
                    {
                        Assert.That(SpriteGeometryUtility.Intersect(pixels, regions[other].SourcePixels, out _), Is.False,
                            "Virtual source rectangles must not double blend a shared strip or corner.");
                    }
                }

                Assert.That(area, Is.EqualTo(tight ? 337f : 437f).Within(.001f));
            }
        }

        /// <summary>Leaves externally owned textures unchanged when a caller selects either source geometry policy.</summary>
        [Test]
        public void ExternalTexturesPreserveTheirSingleRegionInBothPolicies()
        {
            var texture = new Texture2D(17, 19, TextureFormat.RGBA32, false);
            try
            {
                var image = SpriteImage.FromTexture(texture);
                var source = new Rect(2f, 2f, -3f, -4f);
                foreach (var tight in new[] { false, true })
                {
                    Assert.That(image.GetRegionCount(source, tight), Is.EqualTo(1));
                    Assert.That(image.TryGetRegion(source, 0, tight, out var region), Is.True);
                    Assert.That(region.Texture, Is.SameAs(texture));
                    Assert.That(region.SourcePixels, Is.EqualTo(new Rect(-17f, -38f, 51f, 76f)));
                    Assert.That(region.SourceUv, Is.EqualTo(new Rect(-1f, -2f, 3f, 4f)));
                    Assert.That(region.TextureUv, Is.EqualTo(region.SourceUv));
                    Assert.That(image.TryGetRegion(source, 1, tight, out _), Is.False);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Keeps alternating tight and full-tile region reads allocation-free across complete warmed scene frames.</summary>
        [Test]
        public void WarmedAlternatingPoliciesAcrossOneHundredFramesDoNotAllocate()
        {
            var requests = new[] { new Rect(0f, 0f, 1f, 1f), new Rect(4f / 17f, 3f / 19f, 9f / 17f, 12f / 19f),
                new Rect(18f / 17f, 21f / 19f, -19f / 17f, -23f / 19f) };
            for (var frame = 0; frame < 8; frame++)
            {
                ReadFrame(requests);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 100; frame++)
            {
                ReadFrame(requests);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(_provider.PreparedRegionCacheCount, Is.EqualTo(6));
        }

        /// <summary>Reads every requested policy region during one complete allocation-sensitive frame.</summary>
        /// <param name="requests">The source rectangles warmed before allocation measurement.</param>
        private void ReadFrame(Rect[] requests)
        {
            SpriteDrawState.BeginFrame();
            for (var policy = 0; policy < 2; policy++)
            {
                var tight = policy == 0;
                foreach (var source in requests)
                {
                    var count = _image.GetRegionCount(source, tight);
                    for (var index = 0; index < count; index++)
                    {
                        _image.TryGetRegion(source, index, tight, out _);
                    }
                }
            }
        }

        /// <summary>Copies region values before another policy or source crop replaces the prepared lookup.</summary>
        /// <param name="source">The requested original source rectangle.</param>
        /// <param name="tight">Whether conservative scene geometry is selected.</param>
        /// <returns>The complete value snapshot of every source and atlas rectangle.</returns>
        private SpriteImageRegion[] ReadRegions(Rect source, bool tight)
        {
            var count = _image.GetRegionCount(source, tight);
            var regions = new SpriteImageRegion[count];
            for (var index = 0; index < count; index++)
            {
                Assert.That(_image.TryGetRegion(source, index, tight, out regions[index]), Is.True);
            }

            return regions;
        }

        /// <summary>Compares requested floating source crops while exact metadata endpoints are checked separately.</summary>
        /// <param name="actual">The prepared source or atlas rectangle.</param>
        /// <param name="expected">The independently expected rectangle.</param>
        private static void AssertRect(Rect actual, Rect expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.00001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(.00001f));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(.00001f));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(.00001f));
        }

        /// <summary>Leaves the injected page lifetime with the fixture rather than unloading an imported production asset.</summary>
        /// <param name="page">The page released from the provider's test cache.</param>
        private static void IgnorePageRelease(Texture2D page)
        {
        }
    }
}
