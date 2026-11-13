#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original random-range curve and box skew required by PoolSparkly.xml.</summary>
    public sealed class PoolSparklyDefinitionTests
    {
        /// <summary>Checks PC 0x526BD0 curve 7 shapes random interpolation within one bracket, independently of emitter age.</summary>
        /// <param name="random">The fixed random interpolation supplied to the original range.</param>
        /// <param name="expected">The PC inverse-quad-S result scaled from zero to 290.</param>
        [TestCase(0f, 0f)]
        [TestCase(.25f, 108.75f)]
        [TestCase(.5f, 145f)]
        [TestCase(.75f, 181.25f)]
        [TestCase(1f, 290f)]
        public void NativeRangeCurveShapesRandomRatherThanEmitterAge(float random, float expected)
        {
            var type = typeof(OriginalParticleRenderer).GetNestedType("Curve", BindingFlags.NonPublic)!;
            var curve = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public)!
                .Invoke(null, new object[] { "[0 FastInOutWeak 290]", 0f })!;
            foreach (var age in new[] { 0f, .5f, 1f })
            {
                var actual = (float)type.GetMethod("Value")!.Invoke(curve, new object[] { age, random })!;
                Assert.That(actual, Is.EqualTo(expected).Within(.0001f));
            }
        }

        /// <summary>Checks PC 0x52BBF6..0x52BC30 applies both skews using the unmodified original box position.</summary>
        [Test]
        public void NativeEmitterSkewUsesTheOriginalBoxCoordinates()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZPoolSkew-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "particles"));
            Directory.CreateDirectory(Path.Combine(root, "reanim"));
            File.WriteAllText(Path.Combine(root, "particles", "Skew.xml"),
                "<Emitter><Image>IMAGE_UNUSED</Image><SpawnMinActive>1</SpawnMinActive>"
                + "<SystemDuration>100</SystemDuration><ParticleDuration>100</ParticleDuration><ParticleAlpha>0</ParticleAlpha>"
                + "<EmitterBoxX>10</EmitterBoxX><EmitterBoxY>20</EmitterBoxY>"
                + "<EmitterSkewX>-.3</EmitterSkewX><EmitterSkewY>.25</EmitterSkewY></Emitter>");
            using var renderer = new ReanimationRenderer(root);
            var particles = new OriginalParticleRenderer(root, renderer);
            try
            {
                particles.BeginFrame();
                Assert.That(particles.Draw("skew", "Skew", 0f, Vector2.zero, 1), Is.True);
                particles.EndFrame();
                var systems = (IDictionary)typeof(OriginalParticleRenderer).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(particles)!;
                var system = systems["skew"]!;
                var emitters = (IList)system.GetType().GetField("Emitters")!.GetValue(system)!;
                var emitter = emitters[0]!;
                var active = (IList)emitter.GetType().GetField("Particles")!.GetValue(emitter)!;
                var particle = active[0]!;
                var position = (Vector2)particle.GetType().GetField("Position")!.GetValue(particle)!;
                Assert.That(position.x, Is.EqualTo(4f).Within(.0001f));
                Assert.That(position.y, Is.EqualTo(22.5f).Within(.0001f));
            }
            finally
            {
                particles.Clear();
                Directory.Delete(root, true);
            }
        }
    }
}
