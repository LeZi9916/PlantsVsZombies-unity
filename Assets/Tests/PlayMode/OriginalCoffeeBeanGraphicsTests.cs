#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original flying coffee artwork and mushroom wake matrices.</summary>
    public sealed class OriginalCoffeeBeanGraphicsTests
    {
        /// <summary>Checks real planted coffee uses the original .8-scale idle/crumble body above the roof support and pumpkin.</summary>
        /// <returns>Real camera frames comparing independent original source compositions.</returns>
        [UnityTest]
        public IEnumerator CoffeeIdleAndCrumbleUseTheOriginalFlyingActorArtwork()
        {
            foreach (var level in new[] { 9, 29, 49 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                var game = fixture.Owner.RequiredGame;
                game.Plants.Clear();
                var row = level == 29 ? 2 : 1;
                if (level != 9)
                {
                    Invoke(game, "PlaceAdventurePlant", level == 49 ? PlantKind.FlowerPot : PlantKind.LilyPad, row, 4);
                }

                Invoke(game, "PlaceAdventurePlant", PlantKind.PuffShroom, row, 4);
                Invoke(game, "PlaceAdventurePlant", PlantKind.Pumpkin, row, 4);
                var coffee = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.CoffeeBean, row, 4)!;
                Assert.That(coffee.Layer, Is.EqualTo(PlantLayer.Effect));
                coffee.PresentationRate = 12f;
                coffee.Age = .35f;
                Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Coffeebean", "anim_crumble", out var clip), Is.True);
                Assert.That(clip.Count, Is.EqualTo(15));
                foreach (var crumbling in new[] { false, true })
                {
                    coffee.CoffeePresentation!.Crumbling = crumbling;
                    coffee.CoffeePresentation.AnimationTicks = 30;
                    fixture.Animations.ResetNativePlayback();
                    yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", coffee));
                    var actual = fixture.Pixels.GetPixels32();
                    yield return fixture.Capture(null, () =>
                    {
                        // PC 471D60: cell + plant height(-20) + (12,10), body scale .8.
                        var y = level == 49 ? 160f : level == 29 ? 240f : 170f;
                        fixture.Animations.DrawNativePlayback("Coffeebean", new Vector2(372f, y), .8f,
                            crumbling ? .3f : .35f, crumbling ? "anim_crumble" : "anim_idle", !crumbling, crumbling ? 22f : 12f);
                    });
                    Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original coffee actor " + level + "/" + crumbling);
                }
            }
        }

        /// <summary>Checks native wake matrices keep feet at Y+80 and retain the Sun-shroom sleep-to-idle normalized phase.</summary>
        /// <returns>Real source-art frames around squish, stretch and final wake.</returns>
        [UnityTest]
        public IEnumerator MushroomWakeUsesOriginalSquishStretchAndIdlePhase()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            fixture.Owner.RequiredGame.Plants.Clear();
            var plant = new Plant { Id = 91, Kind = PlantKind.PuffShroom, Row = 1, Column = 4,
                Asleep = true, Health = 300f, Age = 1.4f, PresentationRate = 12f };
            fixture.Owner.RequiredGame.Plants.Add(plant);
            foreach (var remaining in new[] { 70, 60, 35, 10 })
            {
                plant.WakeTimer = remaining * .01f;
                fixture.Animations.ResetNativePlayback();
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    // Original TodCurves.EaseSinWave: smooth-step time before one signed sine cycle.
                    var t = (70f - remaining) / 70f;
                    var smooth = t * t * (3f - 2f * t);
                    var wave = (float)Math.Sin(smooth * (float)(Math.PI * 2d));
                    var sy = (float)(1d - .2d * wave);
                    fixture.Animations.DrawNativePlayback("PuffShroom", new Vector2(360f, 180f + 80f - sy * 80f),
                        1f, 1.4f, "anim_sleep", true, 12f, verticalScale: sy);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original wake matrix at counter " + remaining);
            }

            plant.Kind = PlantKind.SunShroom;
            plant.WakeTimer = 0f;
            plant.Asleep = false;
            plant.AwakeByCoffee = true;
            plant.PresentationCoffeeWakeAge = 2f;
            plant.Age = 2.01f;
            fixture.Animations.ResetNativePlayback();
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            var awake = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                // Ten sleeping frames at twelve FPS give phase .4 at wake age two; idle has twelve frames.
                fixture.Animations.DrawNativePlayback("SunShroom", new Vector2(360f, 175f), 1f,
                    .4f + plant.Age - 2f, "anim_idle", true, 12f);
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(awake));
        }

        /// <summary>Creates an original production actor for the artwork comparison.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The original production action.</param>
        /// <param name="arguments">The action's original arguments.</param>
        /// <returns>The original action result.</returns>
        /// <exception cref="TargetInvocationException">The production action throws an exception.</exception>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
