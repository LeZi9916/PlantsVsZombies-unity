#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares original giant actions, durability art, free charred catapult and flattened plant pixels.</summary>
    public sealed class OriginalGargantuarGraphicsTests
    {
        /// <summary>Checks flattened cached plants use PC's .25 scale, sixty-pixel argument and original pot order.</summary>
        /// <returns>Real camera frames comparing static unchanged source artwork.</returns>
        [UnityTest]
        public IEnumerator CrushedPlantsKeepOriginalCachedPictureAndQuarterHeight()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            var game = fixture.Owner.RequiredGame;
            game.Plants.Clear();
            var pot = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, 3)!;
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Peashooter, 1, 3)!;
            Invoke(game, "CrushAdventurePlantCell", 1, 3, false);
            Assert.That(game.Presentation.SquishedPlants.Count, Is.EqualTo(2));
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureSquishedPlants", 1));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                var previous = SpriteDrawState.Matrix;
                try
                {
                    SpriteDrawState.Matrix = previous * Matrix4x4.Translate(new Vector3(280f, 266f, 0f))
                        * Matrix4x4.Scale(new Vector3(1f, .25f, 1f));
                    fixture.Animations.DrawNative("Pot", Vector2.zero, 1f, 0f, null);
                    SpriteDrawState.Matrix = previous * Matrix4x4.Translate(new Vector3(280f, 255f, 0f))
                        * Matrix4x4.Scale(new Vector3(1f, .25f, 1f));
                    fixture.Animations.DrawNative("PeaShooterSingle", Vector2.zero, 1f, 0f, null);
                }
                finally
                {
                    SpriteDrawState.Matrix = previous;
                }
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            Assert.That(Array.Exists(actual, pixel => pixel.r < 200 || pixel.g < 200 || pixel.b < 200), Is.True);
        }

        /// <summary>Calls a production simulation entry point without replacing behavior.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The original private method name.</param>
        /// <param name="arguments">The production arguments.</param>
        /// <returns>The production method result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
