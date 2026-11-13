#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Editor;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks exact NPOT extents, original tile association, and portable device limits in offline packing.</summary>
    public sealed class SpriteAtlasRectanglePackerTests
    {
        /// <summary>Checks a padded full-size background does not inherit power-of-two empty texels.</summary>
        [Test]
        public void SingleRectangleKeepsItsExactNonPowerOfTwoExtent()
        {
            var sizes = new[] { new Vector2Int(1404, 604) };
            Assert.That(SpriteAtlasRectanglePacker.TryPack(sizes, 2048, out var rectangles, out var extent), Is.True);
            Assert.That(extent, Is.EqualTo(new Vector2Int(1404, 604)));
            Assert.That(rectangles[0], Is.EqualTo(new RectInt(0, 0, 1404, 604)));
            Assert.That(SpriteAtlasRectanglePacker.TryPack(sizes, 1024, out _, out _), Is.False);
        }

        /// <summary>Checks varied input order remains associated with unrotated, nonoverlapping source tiles.</summary>
        [Test]
        public void MixedRectangleTrialsPreserveEveryInputSizeAndItsOriginalIndex()
        {
            var sizes = new[] { new Vector2Int(19, 71), new Vector2Int(144, 144), new Vector2Int(104, 64),
                new Vector2Int(124, 104), new Vector2Int(104, 64), new Vector2Int(94, 19), new Vector2Int(64, 24) };
            Assert.That(SpriteAtlasRectanglePacker.TryPack(sizes, 256, out var rectangles, out var extent), Is.True);
            Assert.That(extent.x, Is.LessThanOrEqualTo(256));
            Assert.That(extent.y, Is.LessThanOrEqualTo(256));
            for (var index = 0; index < sizes.Length; index++)
            {
                Assert.That(rectangles[index].size, Is.EqualTo(sizes[index]));
                Assert.That(rectangles[index].xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(rectangles[index].yMin, Is.GreaterThanOrEqualTo(0));
                for (var other = 0; other < index; other++)
                {
                    Assert.That(rectangles[index].Overlaps(rectangles[other]), Is.False);
                }
            }
        }

        /// <summary>Checks an overflowing device page requests lossless multi-page fallback rather than shrinking source tiles.</summary>
        [Test]
        public void OversizedGroupsFailWithoutScalingOrPartiallyPublishingPositions()
        {
            var sizes = new[] { new Vector2Int(256, 256), new Vector2Int(12, 8) };
            Assert.That(SpriteAtlasRectanglePacker.TryPack(sizes, 256, out var rectangles, out var extent), Is.False);
            Assert.That(rectangles, Is.Empty);
            Assert.That(extent, Is.EqualTo(Vector2Int.zero));
        }
    }
}
