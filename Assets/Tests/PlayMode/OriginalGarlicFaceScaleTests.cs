#nullable enable
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks PC 4816F0/4814C0 image replacement dimensions and the unchanged native head center.</summary>
    public sealed class OriginalGarlicFaceScaleTests
    {
        /// <summary>Checks each native body family, small bodies and mirroring use the original gross-out image size.</summary>
        /// <returns>Production original-resource captures before, during and after the garlic face override.</returns>
        [UnityTest]
        public IEnumerator GrossOutRetainsNativeImageDimensionsAndHeadCenter()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var load = fixture.Animations.GetType().GetMethod("LoadTexture", flags)!;
            var face = (SpriteImage)load.Invoke(fixture.Animations, new object[] { "IMAGE_REANIM_ZOMBIE_HEAD_GROSSOUT" })!;
            var id = 900;
            foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.Newspaper, ZombieKind.PoleVaulter, ZombieKind.Dancing, ZombieKind.BackupDancer })
            {
                var headName = kind == ZombieKind.Dancing || kind == ZombieKind.BackupDancer
                    ? "IMAGE_REANIM_ZOMBIE_DANCER__HEAD" : "IMAGE_REANIM_ZOMBIE_HEAD";
                var head = (SpriteImage)load.Invoke(fixture.Animations, new object[] { headName })!;
                foreach (var small in new[] { false, true })
                {
                    var zombie = new Zombie { Id = ++id, Kind = kind, Row = 1, X = 5f, Health = 270f,
                        StartingHealth = 270f, UsedAbility = true, PresentationNativePhase = kind == ZombieKind.Dancing || kind == ZombieKind.BackupDancer ? 44 : 0,
                        PresentationNativeVelocity = .24f, Speed = .3f, IsSmall = small, PresentationWalkingBackwards = small };
                    SimulationFixture.Zombies(fixture.Owner.RequiredGame).Clear();
                    SimulationFixture.Zombies(fixture.Owner.RequiredGame).Add(zombie);
                    yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
                    var pool = (ObjectSpriteRenderer)fixture.Animations.GetType().GetField("_objectRenderer", flags)!.GetValue(fixture.Animations)!;
                    var original = Corners(pool, head);
                    zombie.PresentationGarlicCounter = 70;
                    yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
                    var actual = Corners(pool, face);
                    var center = (original[0] + original[2]) * .5f;
                    var horizontal = (original[1] - original[0]) * ((float)face.Width / head.Width);
                    var vertical = (original[3] - original[0]) * ((float)face.Height / head.Height);
                    var expected = new[] { center - horizontal * .5f - vertical * .5f,
                        center + horizontal * .5f - vertical * .5f, center + horizontal * .5f + vertical * .5f,
                        center - horizontal * .5f + vertical * .5f };
                    for (var corner = 0; corner < 4; corner++)
                    {
                        Assert.That(actual[corner].x, Is.EqualTo(expected[corner].x).Within(.0001f), kind + " native face x");
                        Assert.That(actual[corner].y, Is.EqualTo(expected[corner].y).Within(.0001f), kind + " native face y");
                    }
                    zombie.PresentationGarlicCounter = -1;
                    yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
                    var restored = Corners(pool, head);
                    for (var corner = 0; corner < 4; corner++)
                    {
                        Assert.That(restored[corner].x, Is.EqualTo(original[corner].x).Within(.0001f));
                        Assert.That(restored[corner].y, Is.EqualTo(original[corner].y).Within(.0001f));
                    }
                }
            }
        }

        /// <summary>Copies the original untrimmed source corners from the actual production batch.</summary>
        /// <param name="pool">The production scene batch owner.</param>
        /// <param name="image">The exact original head or replacement image.</param>
        /// <returns>The four native source corners before atlas trimming.</returns>
        private static Vector3[] Corners(ObjectSpriteRenderer pool, SpriteImage image)
        {
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (ReferenceEquals(sprite.SourceImage, image))
                {
                    return new[] { sprite.Vertices[0], sprite.Vertices[1], sprite.Vertices[2], sprite.Vertices[3] };
                }
            }
            Assert.Fail("The native head image was not submitted: " + image.SourceIdentity);
            return Array.Empty<Vector3>();
        }
    }
}
