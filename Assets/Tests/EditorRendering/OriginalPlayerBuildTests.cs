#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Editor;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks default build staging without changing project StreamingAssets.</summary>
    public sealed class OriginalPlayerBuildTests
    {
        /// <summary>Verifies only HQ images and their metadata are excluded and restored after a build exception.</summary>
        [Test]
        public void StagingKeepsOriginalsAndRestoresHqAfterFailure()
        {
            var directory = Path.Combine(Application.temporaryCachePath, "PvZBuildStaging-" + Guid.NewGuid().ToString("N"));
            var root = Path.Combine(directory, "StreamingAssets");
            var images = Path.Combine(root, "images");
            var staging = Path.Combine(directory, "excluded");
            Directory.CreateDirectory(images);
            var original = Path.Combine(images, "background.jpg");
            var hq = Path.Combine(images, "background.jpgHQ.png");
            File.WriteAllText(original, "original");
            File.WriteAllText(hq, "high quality");
            File.WriteAllText(hq + ".meta", "asset GUID");

            try
            {
                Assert.That(OriginalPlayerBuild.HasHighQualityFiles(root), Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    OriginalPlayerBuild.WithOriginalStreamingAssets<bool>(root, staging, () =>
                    {
                        Assert.That(File.Exists(original), Is.True);
                        Assert.That(File.Exists(hq), Is.False);
                        Assert.That(File.Exists(hq + ".meta"), Is.False);
                        Assert.That(OriginalPlayerBuild.HasHighQualityFiles(root), Is.False);
                        throw new InvalidOperationException("Simulated build failure");
                    }));
                Assert.That(File.ReadAllText(hq), Is.EqualTo("high quality"));
                Assert.That(File.ReadAllText(hq + ".meta"), Is.EqualTo("asset GUID"));
                Assert.That(Directory.Exists(staging), Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
