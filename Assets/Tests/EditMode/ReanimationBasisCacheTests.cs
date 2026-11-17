#nullable enable
using System;
using System.Reflection;
using System.Collections;
using System.IO;
using PlantsVsZombies.Rendering;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Compares bounded track-basis reuse with the original uncached affine constructor.</summary>
    public sealed class ReanimationBasisCacheTests
    {
        /// <summary>Checks loading prepares a visible actor's prior-pose dictionary without changing its current pose or exact sample.</summary>
        [Test]
        public void PreparationUpgradesActivePlaybackAndPreservesItsPose()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZActivePose-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                using var renderer = new ReanimationRenderer(root);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var prepare = typeof(ReanimationRenderer).GetMethod("PrepareNativeBlend", flags)!;
                var identity = PresentationIdentity.Create("actor", 71, 0, 1);
                var blend = prepare.Invoke(renderer, new object[] { identity, "anim_walk", 0f, 10 })!;
                var type = blend.GetType();
                var current = (IDictionary)type.GetField("Current")!.GetValue(blend)!;
                var from = (IDictionary)type.GetField("From")!.GetValue(blend)!;
                var frameType = typeof(ReanimationRenderer).GetNestedType("Frame", BindingFlags.NonPublic)!;
                var frame = Activator.CreateInstance(frameType)!;
                frameType.GetField("X")!.SetValue(frame, 123f);
                current.Add(37, frame);
                from.Add(12, frame);
                typeof(ReanimationRenderer).GetField("_maximumPreparedTracks", flags)!.SetValue(renderer, 300);
                Assert.That(renderer.PreparePresentationCapacityStep(0, 0, 1), Is.False);
                Assert.That(current.Count, Is.EqualTo(1));
                Assert.That(from.Count, Is.EqualTo(1));
                Assert.That(frameType.GetField("X")!.GetValue(current[37]), Is.EqualTo(123f));
                var method = current.GetType().GetMethod("EnsureCapacity")!;
                var currentCapacity = (int)method.Invoke(current, new object[] { 0 })!;
                var fromCapacity = (int)method.Invoke(from, new object[] { 0 })!;
                Assert.That(currentCapacity, Is.GreaterThanOrEqualTo(600));
                Assert.That(fromCapacity, Is.GreaterThanOrEqualTo(600));
                prepare.Invoke(renderer, new object[] { identity, "anim_eat", 1f, 10 });
                Assert.That(from.Contains(37), Is.True);
                Assert.That((int)method.Invoke(from, new object[] { 0 })!, Is.EqualTo(fromCapacity));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        /// <summary>Checks translation-only reuse, skew, mirrored scale and alternating actor poses against the uncached oracle.</summary>
        [Test]
        public void CachedBasisMatchesUncachedConstructorAcrossExactPoseChanges()
        {
            var owner = typeof(ReanimationRenderer);
            var frameType = owner.GetNestedType("Frame", BindingFlags.NonPublic)!;
            var trackType = owner.GetNestedType("Track", BindingFlags.NonPublic)!;
            var uncached = owner.GetMethod("PartMatrix", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { frameType.MakeByRefType() }, null)!;
            var cached = owner.GetMethod("PartMatrix", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { frameType.MakeByRefType(), trackType }, null)!;
            var track = Activator.CreateInstance(trackType, true)!;
            var random = new System.Random(7529);
            for (var pose = 0; pose < 120; pose++)
            {
                var frame = Activator.CreateInstance(frameType)!;
                var angleX = (float)(random.NextDouble() * 1440d - 720d);
                var angleY = pose % 2 == 0 ? angleX : (float)(random.NextDouble() * 1440d - 720d);
                frameType.GetField("Kx")!.SetValue(frame, angleX);
                frameType.GetField("Ky")!.SetValue(frame, angleY);
                frameType.GetField("ScaleX")!.SetValue(frame, pose % 3 == 0 ? -1.25f : .75f);
                frameType.GetField("ScaleY")!.SetValue(frame, pose % 5 == 0 ? 0f : 1.375f);
                for (var translation = 0; translation < 4; translation++)
                {
                    frameType.GetField("X")!.SetValue(frame, (float)(random.NextDouble() * 800d));
                    frameType.GetField("Y")!.SetValue(frame, (float)(random.NextDouble() * 600d));
                    var expected = (Matrix4x4)uncached.Invoke(null, new[] { frame })!;
                    var actual = (Matrix4x4)cached.Invoke(null, new[] { frame, track })!;
                    for (var index = 0; index < 16; index++)
                    {
                        Assert.That(actual[index], Is.EqualTo(expected[index]), "Every affine element must retain the uncached result.");
                    }
                }
            }
        }
    }
}
