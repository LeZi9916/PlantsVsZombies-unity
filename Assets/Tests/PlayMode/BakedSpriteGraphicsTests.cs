#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares real imported atlas GPU pixels against the unchanged original image files.</summary>
    public sealed class BakedSpriteGraphicsTests
    {
        /// <summary>Checks imported page completeness, source mapping, and semi-transparent edges.</summary>
        /// <returns>Frame boundaries required to initialize the real graphics device.</returns>
        [UnityTest]
        public IEnumerator ImportedAtlasMatchesOriginalArtwork()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || Resources.Load<TextAsset>("PvZRendering/catalog") == null)
            {
                Assert.Ignore("Provide original artwork and generate the isolated project's production rendering catalog.");
            }
            yield return null;
            using var provider = SpriteAssetProvider.ForRoot(Application.streamingAssetsPath);
            using var renderer = new ObjectSpriteRenderer();
            var target = new RenderTexture(320, 160, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(320, 160, TextureFormat.RGBA32, false);
            target.Create();
            try
            {
                foreach (var identity in new[] { "reanim/PEASHOOTER_HEAD.png", "reanim/ZOMBIE_HEAD.png", "images/background1.jpg" })
                {
                    var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        Assert.That(source.LoadImage(File.ReadAllBytes(Path.Combine(root!, identity))), Is.True);
                        source.filterMode = FilterMode.Bilinear;
                        source.wrapMode = TextureWrapMode.Clamp;
                        var image = provider.LoadImage(identity);
                        var uv = new Rect(0f, 0f, 1f, 1f);
                        renderer.BeginBatch();
                        renderer.DrawQuad(image, 120f, 120f, uv, Matrix4x4.Translate(new Vector3(10f, 10f)), Color.white, 0);
                        renderer.DrawQuad(source, 120f, 120f, uv, Matrix4x4.Translate(new Vector3(170f, 10f)), Color.white, 0);
                        renderer.RenderTo(target);
                        var previous = RenderTexture.active;
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0f, 0f, 320f, 160f), 0, 0);
                        pixels.Apply();
                        RenderTexture.active = previous;
                        var colors = pixels.GetPixels32();
                        var maximum = 0;
                        for (var y = 10; y < 130; y++)
                        {
                            for (var x = 10; x < 130; x++)
                            {
                                var packed = colors[y * 320 + x];
                                var original = colors[y * 320 + x + 160];
                                maximum = Math.Max(maximum, Math.Abs(packed.r - original.r));
                                maximum = Math.Max(maximum, Math.Abs(packed.g - original.g));
                                maximum = Math.Max(maximum, Math.Abs(packed.b - original.b));
                                maximum = Math.Max(maximum, Math.Abs(packed.a - original.a));
                            }
                        }
                        Assert.That(maximum, Is.LessThanOrEqualTo(3), identity + " imported GPU page pixels must preserve the original image, including filtering edges.");
                    }
                    finally
                    {
                        UnityEngine.Object.Destroy(source);
                    }
                }
            }
            finally
            {
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
            }
        }
    }
}
