#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Editor;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Regresses lossless oversize tiling, internal bilinear halos, explicit alpha grids, and reproducible generation.</summary>
    public sealed class SpriteAtlasBakerTests
    {
        /// <summary>The unique external source directory created for each synthetic fixture.</summary>
        private string _source = string.Empty;
        /// <summary>The unique ignored Unity asset directory owned only by this fixture.</summary>
        private string _assets = string.Empty;
        /// <summary>The generated Resources directory owned only by this fixture.</summary>
        private string _output = string.Empty;

        /// <summary>Creates isolated source and output directories without touching production artwork.</summary>
        [SetUp]
        public void SetUp()
        {
            var id = Guid.NewGuid().ToString("N");
            _source = Path.Combine(Application.temporaryCachePath, "PvZAtlasBake-" + id);
            _assets = "Assets/OriginalPvZ/AtlasBakeTests-" + id;
            _output = _assets + "/Resources/PvZRendering/Layouts/Fixture-" + id;
            Directory.CreateDirectory(Path.Combine(_source, "images"));
            Directory.CreateDirectory(Path.Combine(_source, "properties"));
        }

        /// <summary>Removes only uniquely generated fixture directories and their paired Unity metadata.</summary>
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_assets);
            if (Directory.Exists(_source))
            {
                Directory.Delete(_source, true);
            }
        }

        /// <summary>Checks oversized original artwork is tiled without scaling and internal halos sample neighboring original pixels.</summary>
        [Test]
        public void OversizedImageKeepsEveryTexelAndNeighboringTileHalo()
        {
            var pixels = new Color32[520 * 20];
            for (var y = 0; y < 20; y++)
            {
                for (var x = 0; x < 520; x++)
                {
                    pixels[y * 520 + x] = new Color32((byte)(x % 256), (byte)(x / 256), (byte)y, (byte)(x % 253 + 1));
                }
            }
            WriteImage("wide.png", 520, 20, pixels);
            var catalog = SpriteAtlasBaker.Bake(_source, _output, true);
            Assert.That(catalog.Layouts.Length, Is.EqualTo(4));
            foreach (var layout in catalog.Layouts)
            {
                var image = FindImage(layout, "images/wide.png");
                Assert.That(image.Width, Is.EqualTo(520));
                Assert.That(image.Height, Is.EqualTo(20));
                var covered = 0;
                foreach (var tile in image.Tiles)
                {
                    Assert.That(tile.PageWidth, Is.LessThanOrEqualTo(layout.MaximumPageSize));
                    Assert.That(tile.PageHeight, Is.LessThanOrEqualTo(layout.MaximumPageSize));
                    Assert.That(tile.SourcePixels.width, Is.LessThanOrEqualTo(layout.MaximumPageSize - 4));
                    var page = ReadPage(layout.Pages[tile.Page]);
                    try
                    {
                        var pagePixels = page.GetPixels32();
                        var left = Mathf.RoundToInt(tile.TextureUv.x * page.width);
                        var bottom = Mathf.RoundToInt(tile.TextureUv.y * page.height);
                        for (var y = 0; y < (int)tile.SourcePixels.height; y++)
                        {
                            for (var x = 0; x < (int)tile.SourcePixels.width; x++)
                            {
                                var originalX = (int)tile.SourcePixels.x + x;
                                var originalY = (int)tile.SourcePixels.y + y;
                                Assert.That(pagePixels[(bottom + y) * page.width + left + x], Is.EqualTo(pixels[originalY * 520 + originalX]));
                                covered++;
                            }
                        }
                        var haloOriginalX = Math.Max(0, (int)tile.SourcePixels.x - 1);
                        Assert.That(pagePixels[bottom * page.width + left - 1], Is.EqualTo(pixels[(int)tile.SourcePixels.y * 520 + haloOriginalX]));
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(page);
                    }
                }
                Assert.That(covered, Is.EqualTo(520 * 20));
                if (layout.MaximumPageSize == 256)
                {
                    Assert.That(image.Tiles.Length, Is.EqualTo(3));
                }
            }
        }

        /// <summary>Checks resource-manifest alpha grids become baked logical pairs with unchanged original text copies.</summary>
        [Test]
        public void ManifestAlphaGridAndLawnStringsArePackagedWithoutSourceTextureAllocation()
        {
            WriteImage("ink.png", 2, 2, new[] { new Color32(20, 30, 40, 255), new Color32(20, 30, 40, 255),
                new Color32(20, 30, 40, 255), new Color32(20, 30, 40, 255) });
            WriteImage("mask.png", 2, 2, new[] { new Color32(0, 0, 0, 255), new Color32(64, 64, 64, 255),
                new Color32(128, 128, 128, 255), new Color32(255, 255, 255, 255) });
            File.WriteAllText(Path.Combine(_source, "properties", "Resources.xml"),
                "<ResourceManifest><Resources id=\"Fixture\"><SetDefaults path=\"images\" idprefix=\"IMAGE_\"/>"
                + "<Image id=\"NOTE\" path=\"ink\" alphagrid=\"mask\"/></Resources></ResourceManifest>");
            const string strings = "[GREETING]\nHello 世界\n";
            File.WriteAllText(Path.Combine(_source, "properties", "LawnStrings.txt"), strings);
            var catalog = SpriteAtlasBaker.Bake(_source, _output, true);
            var image = FindImage(catalog.Layouts[0], "images/ink.png|images/mask.png");
            Assert.That(image.Aliases, Does.Contain("IMAGE_NOTE"));
            var tile = image.Tiles[0];
            var page = ReadPage(catalog.Layouts[0].Pages[tile.Page]);
            try
            {
                var left = Mathf.RoundToInt(tile.TextureUv.x * page.width);
                var bottom = Mathf.RoundToInt(tile.TextureUv.y * page.height);
                var decoded = page.GetPixels32();
                Assert.That(decoded[bottom * page.width + left].a, Is.Zero);
                Assert.That(decoded[bottom * page.width + left + 1], Is.EqualTo(new Color32(20, 30, 40, 64)));
                Assert.That(decoded[(bottom + 1) * page.width + left].a, Is.EqualTo(128));
                Assert.That(decoded[(bottom + 1) * page.width + left + 1].a, Is.EqualTo(255));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(page);
            }
            var textFound = false;
            foreach (var text in catalog.Texts)
            {
                if (text.SourceIdentity == "properties/LawnStrings.txt")
                {
                    textFound = true;
                    var asset = Resources.Load<TextAsset>(text.ResourcePath);
                    Assert.That(asset, Is.Not.Null);
                    Assert.That(asset!.text, Is.EqualTo(strings));
                }
            }
            Assert.That(textFound, Is.True);
            var cached = SpriteAtlasBaker.Bake(_source, _output);
            Assert.That(cached.SourceHash, Is.EqualTo(catalog.SourceHash));
        }

        /// <summary>Checks related actor parts stay together instead of filling gaps left in unrelated earlier pages.</summary>
        [Test]
        public void CompleteActorFamilySharesOnePageWhenItFitsAnEmptyPage()
        {
            WriteSolidImage("AAA.png", 190, 190);
            WriteSolidImage("Foo_body.png", 100, 100);
            WriteSolidImage("Foo_leaf.png", 20, 20);
            var catalog = SpriteAtlasBaker.Bake(_source, _output, true);
            var layout = catalog.Layouts[0];
            Assert.That(layout.MaximumPageSize, Is.EqualTo(256));
            var background = FindImage(layout, "images/AAA.png");
            var body = FindImage(layout, "images/Foo_body.png");
            var leaf = FindImage(layout, "images/Foo_leaf.png");
            Assert.That(body.Tiles[0].Page, Is.EqualTo(leaf.Tiles[0].Page));
            Assert.That(body.Tiles[0].Page, Is.Not.EqualTo(background.Tiles[0].Page));
        }

        /// <summary>Checks large hot families are placed before smaller families fragment the common battle page.</summary>
        [Test]
        public void CommonActorsShadowsAndBattleUiShareACompatiblePage()
        {
            WriteSolidImage("Zombie_body.png", 140, 140);
            WriteSolidImage("Zombie_head.png", 120, 100);
            WriteSolidImage("Peashooter_head.png", 100, 60);
            WriteSolidImage("Sunflower_face.png", 100, 60);
            WriteSolidImage("plantshadow.png", 60, 20);
            WriteSolidImage("SeedBank.png", 90, 15);
            WriteSolidImage("Shovel.png", 12, 12);
            WriteSolidImage("LawnMower_body.png", 12, 12);
            var catalog = SpriteAtlasBaker.Bake(_source, _output, true);
            foreach (var layout in catalog.Layouts)
            {
                var zombie = FindImage(layout, "images/Zombie_body.png");
                var page = zombie.Tiles[0].Page;
                foreach (var name in new[] { "Zombie_head.png", "Peashooter_head.png", "Sunflower_face.png", "plantshadow.png", "SeedBank.png", "Shovel.png", "LawnMower_body.png" })
                {
                    var image = FindImage(layout, "images/" + name);
                    Assert.That(image.Tiles.Length, Is.EqualTo(1));
                    Assert.That(image.Tiles[0].Page, Is.EqualTo(page), name + " must share the common battle page even in the 256-pixel variant.");
                }
            }
        }

        /// <summary>Checks large board backgrounds own tightly sized pages and cannot inherit unrelated atlas residency.</summary>
        [Test]
        public void LargeBoardBackgroundUsesAnIndependentTightlySizedPage()
        {
            WriteSolidImage("background1.png", 1400, 600);
            WriteSolidImage("unrelated.png", 600, 600);
            var catalog = SpriteAtlasBaker.Bake(_source, _output, true);
            var layout = catalog.Layouts[catalog.Layouts.Length - 1];
            var background = FindImage(layout, "images/background1.png");
            var unrelated = FindImage(layout, "images/unrelated.png");
            Assert.That(background.Tiles.Length, Is.EqualTo(1));
            Assert.That(background.Tiles[0].Page, Is.Not.EqualTo(unrelated.Tiles[0].Page));
            Assert.That(background.Tiles[0].PageWidth, Is.EqualTo(2048));
            Assert.That(background.Tiles[0].PageHeight, Is.EqualTo(1024));
            foreach (var image in layout.Images)
            {
                if (image.SourceIdentity == background.SourceIdentity)
                {
                    continue;
                }
                foreach (var tile in image.Tiles)
                {
                    Assert.That(tile.Page, Is.Not.EqualTo(background.Tiles[0].Page));
                }
            }
        }

        /// <summary>Writes an opaque test source whose full content participates in rectangle packing.</summary>
        /// <param name="name">The original images filename.</param>
        /// <param name="width">The original width.</param>
        /// <param name="height">The original height.</param>
        private void WriteSolidImage(string name, int width, int height)
        {
            var pixels = new Color32[width * height];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = new Color32(80, 120, 160, 255);
            }
            WriteImage(name, width, height, pixels);
        }

        /// <summary>Checks an unsuccessful source rebuild invalidates the published catalog instead of exposing stale mappings.</summary>
        [Test]
        public void FailedGenerationLeavesNoUsableCatalogReferencingPartiallyReplacedPages()
        {
            WriteImage("color.png", 2, 2, new[] { new Color32(20, 30, 40, 255), new Color32(20, 30, 40, 255),
                new Color32(20, 30, 40, 255), new Color32(20, 30, 40, 255) });
            WriteImage("mask.png", 2, 2, new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            File.WriteAllText(Path.Combine(_source, "properties", "Resources.xml"),
                "<ResourceManifest><Resources id=\"Fixture\"><SetDefaults path=\"images\" idprefix=\"IMAGE_\"/>"
                + "<Image id=\"MASKED\" path=\"color\" alphagrid=\"mask\"/></Resources></ResourceManifest>");
            SpriteAtlasBaker.Bake(_source, _output, true);
            WriteImage("mask.png", 2, 1, new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            Assert.Throws<InvalidDataException>(() => SpriteAtlasBaker.Bake(_source, _output, true));
            var catalog = JsonUtility.FromJson<SpriteAtlasCatalog>(File.ReadAllText(_output + "/catalog.json"));
            Assert.That(catalog.Version != SpriteAtlasCatalog.CurrentVersion || catalog.Layouts == null || catalog.Layouts.Length == 0, Is.True);
        }

        /// <summary>Writes a synthetic original PNG retaining exact source texels.</summary>
        /// <param name="name">The original images filename.</param>
        /// <param name="width">The original width.</param>
        /// <param name="height">The original height.</param>
        /// <param name="pixels">The exact bottom-left RGBA pixels.</param>
        private void WriteImage(string name, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(_source, "images", name), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Finds one original identity in a generated layout.</summary>
        /// <param name="layout">The complete generated layout.</param>
        /// <param name="identity">The original relative image path or explicit pair.</param>
        /// <returns>The generated logical source metadata.</returns>
        /// <exception cref="InvalidOperationException">The generated layout omits the requested original source.</exception>
        private static SpriteAtlasImage FindImage(SpriteAtlasLayout layout, string identity)
        {
            foreach (var image in layout.Images)
            {
                if (image.SourceIdentity == identity)
                {
                    return image;
                }
            }
            throw new InvalidOperationException("Generated source is missing: " + identity);
        }

        /// <summary>Decodes a generated PNG directly so exact texels can be tested with Read/Write disabled on its import.</summary>
        /// <param name="resource">The generated extensionless Resources path.</param>
        /// <returns>The caller-owned readable decoded PNG.</returns>
        private Texture2D ReadPage(string resource)
        {
            var resources = _output.Substring(0, _output.LastIndexOf("/Resources/", StringComparison.Ordinal) + "/Resources/".Length);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(resources + resource + ".png")), Is.True);
            return texture;
        }
    }
}
