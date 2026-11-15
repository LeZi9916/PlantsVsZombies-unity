#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects source-track table reuse across actor, asset, attachment, and scope changes.</summary>
    public sealed class SpriteHierarchyTrackCacheTests
    {
        /// <summary>Reuses grown numeric tables while retaining distinct nodes for another asset or attachment.</summary>
        [Test]
        public void NumericTracksRetainIdentityAcrossTableGrowthAndGroupSwitches()
        {
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            var firstAsset = new object();
            var secondAsset = new object();
            using var actor = hierarchy.BeginActor("Plants", 12, "Threepeater");
            var low = Node(hierarchy, firstAsset, 2);
            var high = Node(hierarchy, firstAsset, 129);
            Assert.That(high, Is.Not.SameAs(low));
            Assert.That(Node(hierarchy, firstAsset, 2), Is.SameAs(low));
            var other = Node(hierarchy, secondAsset, 2);
            Assert.That(other, Is.Not.SameAs(low));
            GameObject attached;
            using (hierarchy.BeginAttachment(1))
            {
                attached = Node(hierarchy, firstAsset, 2);
                Assert.That(attached, Is.Not.SameAs(low));
                Assert.That(attached.transform.parent.name, Is.EqualTo("Head 1"));
            }
            Assert.That(Node(hierarchy, firstAsset, 2), Is.SameAs(low));
            using (hierarchy.BeginAttachment(1))
            {
                Assert.That(Node(hierarchy, firstAsset, 2), Is.SameAs(attached));
                Assert.That(Node(hierarchy, secondAsset, 2), Is.Not.SameAs(other));
            }
            Assert.That(Node(hierarchy, firstAsset, 129), Is.SameAs(high));
        }

        /// <summary>Preserves equality-based source identities and supports sparse indices without huge retained arrays.</summary>
        [Test]
        public void EquivalentSourceKeysAndSparseIndicesKeepOriginalDictionarySemantics()
        {
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            using var actor = hierarchy.BeginActor("Zombies", 5, "Normal");
            var firstName = new string(new[] { 'B', 'o', 'd', 'y' });
            var secondName = new string(new[] { 'B', 'o', 'd', 'y' });
            var first = Node(hierarchy, firstName, 0);
            Assert.That(Node(hierarchy, secondName, 0), Is.SameAs(first));
            var negative = Node(hierarchy, firstName, -1);
            var sparse = Node(hierarchy, firstName, int.MaxValue);
            Assert.That(Node(hierarchy, secondName, -1), Is.SameAs(negative));
            Assert.That(Node(hierarchy, secondName, int.MaxValue), Is.SameAs(sparse));
            Assert.That(Node(hierarchy, firstName, 0), Is.SameAs(first));
        }

        /// <summary>Keeps visibility inspection and saved metadata context correct after nested actors alter their own tables.</summary>
        [Test]
        public void NestedActorsRestoreMetadataAndVisibilityStillTraversesCachedNodes()
        {
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            var asset = new object();
            using (hierarchy.BeginActor("Zombies", 1, "Normal"))
            {
                using (hierarchy.BeginSprite(asset, 4, "Body"))
                {
                    var outer = SpriteHierarchy.CurrentSpriteObject!;
                    var metadata = outer.GetComponent<SpriteHierarchySprite>();
                    using (hierarchy.BeginActor("Zombies", 2, "Normal"))
                    {
                        Assert.That(Node(hierarchy, asset, 4), Is.Not.SameAs(outer));
                        using (hierarchy.BeginAttachment(10))
                        {
                            Node(hierarchy, new object(), 64);
                        }
                    }
                    Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.SameAs(outer));
                    SpriteHierarchy.RecordCurrentSprite(SpriteImage.FromTexture(Texture2D.whiteTexture),
                        12f, 8f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.red, 0);
                    Assert.That(metadata.Tint, Is.EqualTo(Color.red));
                    var revision = hierarchy.VisibilityRevision;
                    outer.SetActive(false);
                    hierarchy.RefreshVisibility();
                    Assert.That(hierarchy.VisibilityRevision, Is.GreaterThan(revision));
                    Assert.That(Node(hierarchy, asset, 4), Is.SameAs(outer));
                    Assert.That(outer.activeSelf, Is.False);
                }
            }
            Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.Null);
        }

        /// <summary>Gets one scoped node while preserving the caller's current hierarchy context.</summary>
        /// <param name="hierarchy">The active actor registry.</param>
        /// <param name="asset">The parsed source identity.</param>
        /// <param name="index">The original track index.</param>
        /// <returns>The stable source node retained by the registry.</returns>
        private static GameObject Node(SpriteHierarchy hierarchy, object asset, int index)
        {
            using var sprite = hierarchy.BeginSprite(asset, index, "Track");
            return SpriteHierarchy.CurrentSpriteObject!;
        }
    }
}
