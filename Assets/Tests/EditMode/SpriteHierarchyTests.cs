#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects stable source-sprite identities and model-based lifetime independently of batch geometry.</summary>
    public sealed class SpriteHierarchyTests
    {
        /// <summary>Preserves hidden actors while their models remain live and resets bindings for another session.</summary>
        [Test]
        public void ModelLivenessPreservesHiddenNodesAndSessionResetDiscardsThem()
        {
            using var hierarchy = new SpriteHierarchy();
            var session = new object();
            var asset = new object();
            hierarchy.BeginFrame(session);
            GameObject actor;
            GameObject sprite;
            using (hierarchy.BeginActor("Plants", 1, "Peashooter"))
            {
                actor = SpriteHierarchy.CurrentSprite!.gameObject;
                Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.SameAs(actor));
                using (hierarchy.BeginSprite(asset, 3, "Head"))
                {
                    sprite = SpriteHierarchy.CurrentSprite!.gameObject;
                    Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.SameAs(sprite));
                }

                Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.SameAs(actor));
            }

            Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.Null);
            Assert.That(actor.GetComponentsInChildren<Renderer>(true), Is.Empty);
            sprite.SetActive(false);
            actor.SetActive(false);
            hierarchy.EndFrame();
            hierarchy.BeginFrame(session);
            hierarchy.MarkAlive("Plants", 1, "Peashooter");
            hierarchy.EndFrame();
            Assert.That(hierarchy.ActorCount, Is.EqualTo(1));
            using (hierarchy.BeginActor("Plants", 1, "Peashooter"))
            {
                Assert.That(SpriteHierarchy.CurrentSprite!.gameObject, Is.SameAs(actor));
                using (hierarchy.BeginSprite(asset, 3, "Head"))
                {
                    Assert.That(SpriteHierarchy.CurrentSprite!.gameObject, Is.SameAs(sprite));
                    Assert.That(sprite.activeSelf, Is.False);
                }
            }

            hierarchy.BeginFrame(new object());
            Assert.That(hierarchy.ActorCount, Is.Zero);
            Assert.That(actor == null, Is.True);
            using (hierarchy.BeginActor("Plants", 1, "Peashooter"))
            {
                Assert.That(SpriteHierarchy.CurrentSprite!.gameObject.activeSelf, Is.True);
            }
        }

        /// <summary>Keeps repeated attached-head tracks distinct while all passes of one source reuse its node.</summary>
        [Test]
        public void SourceAndAttachmentIdentitiesRemainStableAcrossClipsAndPasses()
        {
            using var hierarchy = new SpriteHierarchy();
            var asset = new object();
            hierarchy.BeginFrame(this);
            using var actor = hierarchy.BeginActor("Plants", 2, "Threepeater");
            Transform body;
            using (hierarchy.BeginSprite(asset, 4, "Mouth"))
            {
                body = SpriteHierarchy.CurrentSprite!;
            }

            using (hierarchy.BeginSprite(asset, 4, "Mouth"))
            {
                Assert.That(SpriteHierarchy.CurrentSprite, Is.SameAs(body));
            }

            Transform firstHead;
            using (hierarchy.BeginAttachment(1))
            {
                using (hierarchy.BeginSprite(asset, 4, "Mouth"))
                {
                    firstHead = SpriteHierarchy.CurrentSprite!;
                    Assert.That(firstHead, Is.Not.SameAs(body));
                }
            }

            using (hierarchy.BeginAttachment(2))
            {
                using (hierarchy.BeginSprite(asset, 4, "Mouth"))
                {
                    Assert.That(SpriteHierarchy.CurrentSprite, Is.Not.SameAs(firstHead));
                }
            }

            using (hierarchy.BeginAttachment(1))
            {
                using (hierarchy.BeginSprite(asset, 4, "Mouth"))
                {
                    Assert.That(SpriteHierarchy.CurrentSprite, Is.SameAs(firstHead));
                }
            }
        }

        /// <summary>Invalidates capture revisions for child and ancestor visibility changes without rewriting activeSelf.</summary>
        [Test]
        public void VisibilityChangesInvalidateCapturesAndAbsentModelsRetire()
        {
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            GameObject root;
            GameObject sprite;
            using (hierarchy.BeginActor("Zombies", 8, "Normal"))
            {
                root = SpriteHierarchy.CurrentSprite!.gameObject;
                using (hierarchy.BeginNamedSprite("Shadow"))
                {
                    sprite = SpriteHierarchy.CurrentSprite!.gameObject;
                }
            }

            var revision = hierarchy.VisibilityRevision;
            sprite.SetActive(false);
            hierarchy.RefreshVisibility();
            Assert.That(hierarchy.VisibilityRevision, Is.GreaterThan(revision));
            revision = hierarchy.VisibilityRevision;
            root.SetActive(false);
            hierarchy.RefreshVisibility();
            Assert.That(hierarchy.VisibilityRevision, Is.GreaterThan(revision));
            hierarchy.BeginFrame(this);
            hierarchy.EndFrame();
            Assert.That(hierarchy.ActorCount, Is.Zero);
            Assert.That(root == null, Is.True);
        }

        /// <summary>Preserves skewed animation axes and restores the correct metadata destination after attachment scopes.</summary>
        [Test]
        public void PoseInspectionPreservesFullAffineMatrixAndSourceIdentity()
        {
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            var asset = new object();
            using var actor = hierarchy.BeginActor("Plants", 1, "Peashooter");
            using var body = hierarchy.BeginSprite(asset, 1, "Body");
            var bodyInfo = SpriteHierarchy.CurrentSprite!.GetComponent<SpriteHierarchySprite>();
            var matrix = Matrix4x4.identity;
            matrix.m00 = .8f;
            matrix.m01 = .4f;
            matrix.m10 = .3f;
            matrix.m11 = .6f;
            matrix.m03 = 42f;
            matrix.m13 = 15f;
            var image = SpriteImage.FromTexture(Texture2D.whiteTexture);
            using (hierarchy.BeginAttachment(1))
            {
                using (hierarchy.BeginSprite(asset, 1, "Head"))
                {
                    SpriteHierarchy.RecordCurrentSprite(image, 12f, 15f, new Rect(0f, 0f, .5f, 1f), matrix, Color.red, 0);
                    var headInfo = SpriteHierarchy.CurrentSprite!.GetComponent<SpriteHierarchySprite>();
                    Assert.That(headInfo.DestinationTransform, Is.EqualTo(matrix));
                    Assert.That(SpriteHierarchy.CurrentSprite.parent.name, Is.EqualTo("Head 1"));
                }
            }

            Assert.That(bodyInfo.HasPose, Is.False);
            Assert.That(SpriteHierarchy.CurrentSpriteObject, Is.SameAs(bodyInfo.gameObject));
            SpriteHierarchy.RecordCurrentSprite(image, 20f, 30f, new Rect(0f, 0f, 1f, 1f), matrix, Color.green, 2);
            Assert.That(bodyInfo.HasPose, Is.True);
            Assert.That(bodyInfo.SourceIdentity, Is.EqualTo(image.SourceIdentity));
            Assert.That(bodyInfo.DestinationTransform, Is.EqualTo(matrix));
            Assert.That(bodyInfo.LogicalSize, Is.EqualTo(new Vector2(20f, 30f)));
            Assert.That(bodyInfo.Tint, Is.EqualTo(Color.green));
            Assert.That(bodyInfo.RenderMode, Is.EqualTo(2));
        }

        /// <summary>Prevents hierarchy bookkeeping from allocating during warmed actor and sprite presentation.</summary>
        [Test]
        public void WarmedActorAndSpriteScopesDoNotAllocateManagedMemory()
        {
            using var hierarchy = new SpriteHierarchy();
            var session = new object();
            var asset = new object();
            for (var index = 0; index < 8; index++)
            {
                DrawHierarchyFrame(hierarchy, session, asset);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100; index++)
            {
                DrawHierarchyFrame(hierarchy, session, asset);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(hierarchy.ActorCount, Is.EqualTo(1));
        }

        /// <summary>Exercises one allocation-sensitive model-liveness frame with an attached source sprite.</summary>
        /// <param name="hierarchy">The registry retaining reusable identities.</param>
        /// <param name="session">The stable simulation session.</param>
        /// <param name="asset">The stable parsed animation identity.</param>
        private static void DrawHierarchyFrame(SpriteHierarchy hierarchy, object session, object asset)
        {
            hierarchy.BeginFrame(session);
            hierarchy.MarkAlive("Plants", 1, "Peashooter");
            using (hierarchy.BeginActor("Plants", 1, "Peashooter"))
            {
                using (hierarchy.BeginAttachment(1))
                {
                    using (hierarchy.BeginSprite(asset, 2, "Head"))
                    {
                    }
                }
            }

            hierarchy.EndFrame();
        }
    }
}
