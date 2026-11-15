#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Compares bounded track-basis reuse with the original uncached affine constructor.</summary>
    public sealed class ReanimationBasisCacheTests
    {
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
