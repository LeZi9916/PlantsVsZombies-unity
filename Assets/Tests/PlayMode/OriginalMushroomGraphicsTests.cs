#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original mushroom sleep artwork and free Z animation.</summary>
    public sealed class OriginalMushroomGraphicsTests
    {
        /// <summary>Checks all ten source sleeping bodies and independently placed original Z artwork, including roof and small bodies.</summary>
        /// <returns>Real camera frame boundaries comparing original source compositions.</returns>
        [UnityTest]
        public IEnumerator SleepingBodiesAndZUseTheOriginalClipsAndIndependentOffsets()
        {
            var samples = new[] { (PlantKind.PuffShroom, "PuffShroom", 9, 1f),
                (PlantKind.SunShroom, "SunShroom", 9, 1f), (PlantKind.FumeShroom, "FumeShroom", 9, 1f),
                (PlantKind.HypnoShroom, "HypnoShroom", 9, 1f), (PlantKind.ScaredyShroom, "ScaredyShroom", 9, 1f),
                (PlantKind.IceShroom, "IceShroom", 9, 1f), (PlantKind.DoomShroom, "DoomShroom", 9, 1f),
                (PlantKind.SeaShroom, "SeaShroom", 29, 1f), (PlantKind.MagnetShroom, "MagnetShroom", 9, 1f),
                (PlantKind.GloomShroom, "GloomShroom", 9, 1f), (PlantKind.PuffShroom, "PuffShroom", 49, .5f) };
            foreach (var sample in samples)
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(sample.Item3);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
                var plant = new Plant { Id = 91, Kind = sample.Item1, Row = 1, Column = 2, Asleep = true,
                    Health = 300f, Age = .37f, PresentationRate = 12f, IsSmall = sample.Item4 < 1f,
                    SleepingPresentation = new AdventureSleepingState { Rate = 7f, AnimationSeconds = .4f } };
                fixture.Owner.RequiredGame.Plants.Clear();
                fixture.Owner.RequiredGame.Plants.Add(plant);
                if (sample.Item3 == 49)
                {
                    fixture.Owner.RequiredGame.Plants.Add(new Plant { Kind = PlantKind.FlowerPot,
                        Layer = PlantLayer.Support, Row = 1, Column = 2, Health = 300f });
                }

                yield return fixture.Capture(null, () =>
                {
                    fixture.DrawPass("DrawAdventurePlant", plant);
                    fixture.DrawPass("DrawAdventureSleepingPlant", plant);
                });
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    var cellY = sample.Item3 == 49 ? 215f : sample.Item3 == 29 ? 165f : 180f;
                    var bodyOffset = sample.Item1 == PlantKind.PuffShroom ? 5f
                        : sample.Item1 == PlantKind.ScaredyShroom ? -14f : sample.Item1 == PlantKind.SeaShroom ? 28f : 0f;
                    if (sample.Item3 == 49)
                    {
                        bodyOffset -= 5f;
                    }

                    fixture.Animations.DrawNativePlayback(sample.Item2,
                        new Vector2(200f + 40f * (1f - sample.Item4), cellY + bodyOffset + 80f * (1f - sample.Item4)),
                        sample.Item4, .37f, "anim_sleep", true, 12f);
                    var zX = sample.Item1 == PlantKind.FumeShroom ? 262f : 250f;
                    var zY = cellY + (sample.Item1 == PlantKind.ScaredyShroom ? 20f : sample.Item1 == PlantKind.GloomShroom ? 28f : 40f);
                    // PC 46CB90 creates reanimation 68 (Z), scale one, independently of the body's pot/small offsets.
                    fixture.Animations.DrawNativePlayback("Z", new Vector2(zX, zY), 1f, .4f, null, true, 7f);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), sample.Item1 + " original sleeping composition");
            }
        }

        /// <summary>Checks captured sleeping plants retain the free cell-positioned Z even after their body leaves normal plant order.</summary>
        /// <returns>Real board camera frames comparing the independently retained native sleeping animation.</returns>
        [UnityTest]
        public IEnumerator LiftedSleepingPlantKeepsItsOriginalFreeZOnTheBoard()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(9);
            fixture.Owner.RequiredGame.Plants.Clear();
            var plant = new Plant { Id = 91, Kind = PlantKind.PuffShroom, Row = 1, Column = 2,
                Asleep = true, BungeeState = 2, Health = 300f,
                SleepingPresentation = new AdventureSleepingState { Rate = 7f, AnimationSeconds = .4f } };
            fixture.Owner.RequiredGame.Plants.Add(plant);
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var actual = fixture.Pixels.GetPixels32();
            plant.Asleep = false;
            yield return fixture.Capture("DrawAdventureBoard", () =>
                fixture.Animations.DrawNativePlayback("Z", new Vector2(250f, 220f), 1f, .4f, null, true, 7f), true);
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
        }

    }
}
