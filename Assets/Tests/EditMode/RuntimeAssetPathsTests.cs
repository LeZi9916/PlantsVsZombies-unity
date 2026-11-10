#nullable enable

using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Infrastructure;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects loose resource loading for both supported StreamingAssets layouts.</summary>
    public sealed class RuntimeAssetPathsTests
    {
        /// <summary>The unique temporary directory owned by the current test.</summary>
        private string _directory = null!;

        /// <summary>Creates an isolated resource layout for each test.</summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "PvZAssetPaths-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        /// <summary>Removes only the resource layout created by this fixture.</summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>Uses the current layout when images are stored directly in StreamingAssets.</summary>
        [Test]
        public void ResolveRootUsesCurrentLayout()
        {
            Directory.CreateDirectory(Path.Combine(_directory, "images"));
            Directory.CreateDirectory(Path.Combine(_directory, "reanim"));
            Assert.That(RuntimeAssetPaths.ResolveRoot(_directory), Is.EqualTo(_directory));
        }

        /// <summary>Preserves compatibility with the old nested PvZ resource directory.</summary>
        [Test]
        public void ResolveRootPrefersCompleteLegacyLayout()
        {
            var legacy = Path.Combine(_directory, "PvZ");
            Directory.CreateDirectory(Path.Combine(_directory, "images"));
            Directory.CreateDirectory(Path.Combine(legacy, "images"));
            Assert.That(RuntimeAssetPaths.ResolveRoot(_directory), Is.EqualTo(legacy));
        }

        /// <summary>Ignores an empty legacy directory that has no image resources.</summary>
        [Test]
        public void ResolveRootIgnoresEmptyLegacyDirectory()
        {
            Directory.CreateDirectory(Path.Combine(_directory, "PvZ"));
            Assert.That(RuntimeAssetPaths.ResolveRoot(_directory), Is.EqualTo(_directory));
        }

        /// <summary>Rejects invalid resource roots before any filesystem access.</summary>
        /// <param name="path">The invalid resource directory.</param>
        [TestCase("")]
        [TestCase(" ")]
        public void ResolveRootRejectsEmptyPath(string path)
        {
            Assert.Throws<ArgumentException>(() => RuntimeAssetPaths.ResolveRoot(path));
        }
    }
}
