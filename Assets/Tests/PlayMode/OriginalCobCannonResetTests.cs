#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks PC 47318F's twenty-update cannon return blend even when no render observes the shooting endpoint.</summary>
    public sealed class OriginalCobCannonResetTests
    {
        /// <summary>Checks a skipped shooting endpoint returns from the authored final firing pose with the elapsed native blend.</summary>
        /// <returns>Actual original-resource frames for the production actor and independent native playback oracle.</returns>
        [UnityTest]
        public IEnumerator ResetUsesCompletedShootingPoseAndNativeElapsedBlend()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var game = fixture.Owner.RequiredGame;
            var cannon = new Plant { Id = 800, Kind = PlantKind.CobCannon, Row = 1, Column = 3,
                ColumnSpan = 2, Layer = PlantLayer.Main, Health = 300f,
                CannonPhase = CobCannonPhase.Firing, CannonReloadRemaining = 1f, PhaseAge = 2.5f };
            SimulationFixture.Plants(game).Add(cannon);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
            var advance = SimulationInspection.GetMethod("AdvanceAdventurePlants", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var tick = 0; tick < 110; tick++)
            {
                advance.Invoke(game, new object[] { .01f });
            }
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Reloading));
            Assert.That(cannon.PhaseAge, Is.EqualTo(.1f).Within(.00001f));
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
            var frames = new List<(float Age, Color32[] Pixels)> { (cannon.PhaseAge, fixture.Pixels.GetPixels32()) };
            foreach (var updates in new[] { 5, 10 })
            {
                for (var tick = 0; tick < updates; tick++)
                {
                    advance.Invoke(game, new object[] { .01f });
                }
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
                frames.Add((cannon.PhaseAge, fixture.Pixels.GetPixels32()));
            }
            var output = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "cannon-native-return.png"), fixture.Pixels.EncodeToPNG());
            }
            // Native PlayReanim sees the completed source pose even if the GPU did not draw its last updates.
            // StartBlend(20) starts when the simulation switches clips, not at the first later rendered frame.
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativePlayback("CobCannon",
                new Vector2(280f, 180f), 1f, 3.5f, "anim_shooting", false, 12f, instanceKey: "cannon-reset-oracle"), Is.True));
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativePlayback("CobCannon",
                new Vector2(280f, 180f), 1f, 0f, "anim_unarmed_idle", true, 12f, instanceKey: "cannon-reset-oracle", blendTicks: 20), Is.True));
            foreach (var frame in frames)
            {
                yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativePlayback("CobCannon",
                    new Vector2(280f, 180f), 1f, frame.Age, "anim_unarmed_idle", true, 12f,
                    instanceKey: "cannon-reset-oracle", blendTicks: 20), Is.True));
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(frame.Pixels),
                    "The native twenty-tick return must use the completed firing pose and elapsed logic time at age " + frame.Age);
            }
        }
    }
}
