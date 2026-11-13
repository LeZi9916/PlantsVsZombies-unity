#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Verifies actual generated PNG imports and deserialized catalog UVs reach every band of a full two-kilopixel GPU page.</summary>
    public sealed class ImportedSpriteAtlasGraphicsTests
    {
        /// <summary>Bakes a large synthetic original, imports real PNG pages, reloads the serialized catalog, and verifies GPU pixels.</summary>
        /// <returns>A frame allowing the production render pipeline to initialize before the capture.</returns>
        [UnityTest]
        public IEnumerator GeneratedImportedPagePreservesTopMiddleAndBottomTexels()
        {
            if (SystemInfo.maxTextureSize < 2048)
            {
                Assert.Ignore("This regression specifically verifies a full 2048-pixel imported page; smaller layouts have separate selection tests.");
            }
            yield return null;
            var id = Guid.NewGuid().ToString("N");
            var source = Path.Combine(Application.temporaryCachePath, "PvZImportedAtlas-" + id);
            var assets = "Assets/OriginalPvZ/ImportedAtlasTests-" + id;
            var output = assets + "/Resources/PvZRendering/Layouts/Fixture-" + id;
            var catalogResource = "PvZRendering/Layouts/Fixture-" + id + "/catalog";
            Directory.CreateDirectory(Path.Combine(source, "images"));
            Texture2D? original = null;
            Texture2D? readback = null;
            RenderTexture? target = null;
            SpriteAssetProvider? provider = null;
            ObjectSpriteRenderer? renderer = null;
            try
            {
                const int size = 1200;
                original = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var sourcePixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    var color = y < size / 3 ? new Color32(255, 0, 0, 255)
                        : y < size * 2 / 3 ? new Color32(0, 255, 0, 255) : new Color32(0, 0, 255, 255);
                    for (var x = 0; x < size; x++)
                    {
                        sourcePixels[y * size + x] = color;
                    }
                }
                original.SetPixels32(sourcePixels);
                original.Apply(false, false);
                File.WriteAllBytes(Path.Combine(source, "images", "bands.png"), original.EncodeToPNG());
                var baker = Type.GetType("PlantsVsZombies.Editor.SpriteAtlasBaker, PlantsVsZombies.Editor", true)!;
                var bake = baker.GetMethod("Bake", BindingFlags.Public | BindingFlags.Static)!;
                bake.Invoke(null, new object[] { source, output, true });
                var catalogAsset = Resources.Load<TextAsset>(catalogResource);
                Assert.That(catalogAsset, Is.Not.Null);
                var catalog = JsonUtility.FromJson<SpriteAtlasCatalog>(catalogAsset!.text);
                provider = new SpriteAssetProvider(catalog, 2048, LoadImportedPage, ReleaseImportedPage);
                Assert.That(provider.MaxPageSize, Is.EqualTo(2048));
                var image = provider.LoadImage("images/bands.png");
                Assert.That(image.SourceTexture, Is.Null);
                Assert.That(image.Width, Is.EqualTo(size));
                Assert.That(image.Height, Is.EqualTo(size));
                renderer = new ObjectSpriteRenderer();
                target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
                target.Create();
                readback = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                renderer.BeginBatch();
                renderer.DrawQuad(image, 64, 64, new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.white, 0);
                renderer.RenderTo(target);
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                    readback.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = previous;
                }
                AssertColor(readback.GetPixel(32, 8), Color.red);
                AssertColor(readback.GetPixel(32, 32), Color.green);
                AssertColor(readback.GetPixel(32, 56), Color.blue);
            }
            finally
            {
                renderer?.Dispose();
                provider?.Dispose();
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }
                UnityEngine.Object.Destroy(original);
                UnityEngine.Object.Destroy(readback);
                AssetDatabase.DeleteAsset(assets);
                if (Directory.Exists(source))
                {
                    Directory.Delete(source, true);
                }
            }
        }

        /// <summary>Loads and validates the actual imported GPU page rather than a directly constructed test texture.</summary>
        /// <param name="path">The generated extensionless Resources page key.</param>
        /// <returns>The imported PNG texture when present.</returns>
        private static Texture2D? LoadImportedPage(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture!.format, Is.EqualTo(TextureFormat.RGBA32));
            Assert.That(texture.isReadable, Is.False);
            Assert.That(texture.width, Is.EqualTo(2048));
            Assert.That(texture.height, Is.EqualTo(2048));
            return texture;
        }

        /// <summary>Releases imported pages only after the test's completed GPU readback.</summary>
        /// <param name="texture">The imported texture owned by the provider acquisition.</param>
        private static void ReleaseImportedPage(Texture2D texture)
        {
            Resources.UnloadAsset(texture);
        }

        /// <summary>Checks a captured primary-color pixel without depending on color-space conversion between intermediate values.</summary>
        /// <param name="actual">The captured GPU pixel.</param>
        /// <param name="expected">The original source primary color.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.02f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.02f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.02f));
            Assert.That(actual.a, Is.GreaterThan(.98f));
        }
    }
}
#endif
