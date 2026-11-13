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
    /// <summary>Checks the original Plantern emitter's native bounce and random lifetime phases.</summary>
    public sealed class PlanternParticleTests
    {
        /// <summary>Checks PC 526C50 returns both ends of a particle pulse to the first authored value.</summary>
        [Test]
        public void BounceReturnsToTheFirstEndpointInsteadOfRamping()
        {
            var type = typeof(OriginalParticleRenderer).GetNestedType("Curve", BindingFlags.NonPublic)!;
            var curve = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public)!
                .Invoke(null, new object[] { "1 Bounce [.7 1]", 0f })!;
            var times = new[] { 0f, .25f, .5f, .75f, 1f };
            var expected = new[] { 1f, .85f, .7f, .85f, 1f };
            for (var index = 0; index < times.Length; index++)
            {
                var actual = (float)type.GetMethod("Value")!.Invoke(curve, new object[] { times[index], 0f })!;
                Assert.That(actual, Is.EqualTo(expected[index]).Within(.0001f));
            }
        }

        /// <summary>Checks PC 52B7E4 randomizes integer age, performs one birth update, and does not rerandomize loops.</summary>
        [Test]
        public void RandomStartUsesIntegerAgesWithoutPrewarmingOrRerandomizingLoops()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZPlanternParticles-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "particles"));
            Directory.CreateDirectory(Path.Combine(root, "reanim"));
            File.WriteAllText(Path.Combine(root, "particles", "RandomStart.xml"),
                "<Emitter><Image>IMAGE_UNUSED</Image><SpawnMinActive>20</SpawnMinActive>"
                + "<SystemLoops>1</SystemLoops><ParticleLoops>1</ParticleLoops><RandomStartTime>1</RandomStartTime>"
                + "<ParticleDuration>10</ParticleDuration><ParticleAlpha>0</ParticleAlpha>"
                + "<LaunchAngle>0</LaunchAngle><LaunchSpeed>100</LaunchSpeed></Emitter>");
            using var renderer = new ReanimationRenderer(root);
            var particles = new OriginalParticleRenderer(root, renderer);
            try
            {
                particles.BeginFrame();
                Assert.That(particles.Draw("plantern", "RandomStart", 0f, Vector2.zero, 7529), Is.True);
                particles.EndFrame();
                var systems = (IDictionary)typeof(OriginalParticleRenderer).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(particles)!;
                var system = systems["plantern"]!;
                var emitters = (IList)system.GetType().GetField("Emitters")!.GetValue(system)!;
                var active = (IList)emitters[0]!.GetType().GetField("Particles")!.GetValue(emitters[0])!;
                Assert.That(active.Count, Is.EqualTo(20));
                var ages = new float[active.Count];
                var varied = false;
                for (var index = 0; index < active.Count; index++)
                {
                    var particle = active[index]!;
                    var type = particle.GetType();
                    var age = (float)type.GetField("Age")!.GetValue(particle)!;
                    ages[index] = age;
                    varied |= age != ages[0];
                    Assert.That(age, Is.InRange(1f, 10f));
                    Assert.That(age, Is.EqualTo((float)(int)age));
                    Assert.That((float)type.GetField("Time")!.GetValue(particle)!, Is.EqualTo((age - 1f) / 9f).Within(.00001f));
                    Assert.That((Vector2)type.GetField("Position")!.GetValue(particle)!, Is.EqualTo(new Vector2(0f, 1f)));
                }

                Assert.That(varied, Is.True, "Original random starts must stagger the glow pulses immediately.");
                particles.BeginFrame();
                Assert.That(particles.Draw("plantern", "RandomStart", .1f, Vector2.zero, 7529), Is.True);
                particles.EndFrame();
                for (var index = 0; index < active.Count; index++)
                {
                    Assert.That((float)active[index]!.GetType().GetField("Age")!.GetValue(active[index])!, Is.EqualTo(ages[index]));
                }

                Assert.That(particles.CreatedParticleCount, Is.EqualTo(20));
            }
            finally
            {
                particles.Clear();
                Directory.Delete(root, true);
            }
        }
    }
}
