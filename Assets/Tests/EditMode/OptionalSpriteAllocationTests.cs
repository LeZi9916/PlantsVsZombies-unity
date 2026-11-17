#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Game;
using Unity.Profiling;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects optional-artwork misses without making absence survive a provider's source-index lifetime.</summary>
    public sealed class OptionalSpriteAllocationTests
    {
        /// <summary>Retains a native alpha-grid cache key when an optional source pair is repeatedly absent.</summary>
        [Test]
        public void WarmMissingAlphaGridPairsProduceNoAllocationEvents()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZAlphaCache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var host = new GameObject("Inactive alpha cache regression");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            owner._root = root;
            try
            {
                var cache = owner.Textures;
                for (var warm = 0; warm < 8; warm++)
                {
                    Assert.That(cache.LoadAlphaGrid("pool_base.jpg", "pool_base_.png"), Is.Null);
                }
                using var recorder = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 4096,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                Assert.That(recorder.Valid, Is.True);
                recorder.Start();
                var sentinel = new byte[1024];
                recorder.Stop();
                GC.KeepAlive(sentinel);
                Assert.That(recorder.Count, Is.GreaterThan(0));
                recorder.Reset();
                recorder.Start();
                for (var i = 0; i < 1000; i++)
                {
                    cache.LoadAlphaGrid("pool_base.jpg", "pool_base_.png");
                }
                recorder.Stop();
                Assert.That(recorder.Count, Is.Zero);
            }
            finally
            {
                owner.Textures.ReleaseAll();
                UnityEngine.Object.DestroyImmediate(host);
                Directory.Delete(root, true);
            }
        }

        /// <summary>Checks repeated misses avoid formatting fallback paths or throwing resource exceptions.</summary>
        [Test]
        public void WarmMissingImagesProduceNoAllocationEvents()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZOptionalImages-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "images"));
            try
            {
                using var provider = SpriteAssetProvider.ForRoot(root);
                Assert.That(provider.TryLoadImage("IMAGE_ABSENT", out _), Is.False);
                Assert.That(provider.ContainsImage("images/absent.png"), Is.False);
                using var recorder = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 4096,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                Assert.That(recorder.Valid, Is.True);
                recorder.Start();
                var positiveControl = new byte[1024];
                recorder.Stop();
                GC.KeepAlive(positiveControl);
                Assert.That(recorder.Count, Is.GreaterThan(0));
                recorder.Reset();
                var found = false;
                recorder.Start();
                for (var i = 0; i < 1000; i++)
                {
                    found |= provider.TryLoadImage("IMAGE_ABSENT", out _);
                    found |= provider.ContainsImage("images/absent.png");
                }
                recorder.Stop();
                Assert.That(found, Is.False);
                Assert.That(recorder.Count, Is.Zero);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        /// <summary>Allows a newly indexed resource directory to observe files added after an earlier provider was disposed.</summary>
        [Test]
        public void ReopeningProviderObservesNewArtworkAfterNegativeLookup()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZOptionalImages-" + Guid.NewGuid().ToString("N"));
            var imageDirectory = Directory.CreateDirectory(Path.Combine(root, "images")).FullName;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                using (var first = SpriteAssetProvider.ForRoot(root))
                {
                    Assert.That(first.ContainsImage("images/added.png"), Is.False);
                }
                File.WriteAllBytes(Path.Combine(imageDirectory, "added.png"), texture.EncodeToPNG());
                using var reopened = SpriteAssetProvider.ForRoot(root);
                Assert.That(reopened.TryLoadImage("images/added.png", out var image), Is.True);
                Assert.That(image!.Width, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                Directory.Delete(root, true);
            }
        }
    }
}
