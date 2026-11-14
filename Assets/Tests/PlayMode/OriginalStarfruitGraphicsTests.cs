#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original Starfruit projectile origins, straight flight, rotation, and shadow artwork.</summary>
    public sealed class OriginalStarfruitGraphicsTests
    {
        /// <summary>Checks the five shared native origins and a real rotating diagonal star above the original roof shadow.</summary>
        /// <returns>A frame boundary for original artwork and real graphics initialization.</returns>
        [UnityTest]
        public IEnumerator StarsShareOriginalOriginAndRetainStraightRotatingFlightAboveRoofShadow()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Starfruit graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            var previousMatrix = SpriteDrawState.Matrix;
            var previousColor = SpriteDrawState.Color;
            var previousClip = SpriteDrawState.Clip;
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 41 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var plant = new Plant { Kind = PlantKind.Starfruit, Row = 2, Column = 3 };
                typeof(GameSimulation).GetMethod("FireAdventureStarfruit", flags)!
                    .Invoke(owner._game, new object[] { plant });
                Assert.That(owner._game.Peas, Has.Count.EqualTo(5));
                foreach (var shot in owner._game.Peas)
                {
                    Assert.That(40f + shot.X * 80f, Is.EqualTo(305f));
                    Assert.That(shot.PresentationLaunchY, Is.EqualTo(305f));
                    Assert.That(shot.PresentationStarShadowY, Is.EqualTo(350f),
                        "PC 47BB30 adds 67 to the row baseline, subtracts 12 on the roof, then adds 15 for a star.");
                    Assert.That(Math.Abs(shot.PresentationSpin), Is.InRange(.05f, .1f));
                }

                var star = owner._game.Peas[3];
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureProjectile", flags)!;
                var move = typeof(GameSimulation).GetMethod("AdvanceAdventureProjectiles", flags)!;
                SpriteDrawState.Matrix = Matrix4x4.identity;
                SpriteDrawState.Color = Color.white;
                SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                for (var sample = 0; sample < 2; sample++)
                {
                    BeginFrame(animations);
                    draw.Invoke(presenter, new object[] { star });
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var actual = pixels.GetPixels32();
                    Assert.That(Array.Exists(actual, pixel => pixel.r < 245 || pixel.g < 245 || pixel.b < 245), Is.True);
                    var ticks = sample * 10f;
                    var nativeX = 305f + (float)(Math.Cos(Math.PI / 6d) * 3.33f) * ticks;
                    var nativeY = 305f + 3.33f * .5f * ticks;
                    var shadowY = 350f + 3.33f * .5f * ticks
                        + (Math.Max(0f, 440f - nativeX) - 135f) * .25f;
                    BeginFrame(animations);
                    Assert.That(animations.DrawResourceSprite("IMAGE_PEA_SHADOWS", new Vector2(nativeX + 17.5f, shadowY + 4.5f), 1f), Is.True);
                    Assert.That(animations.DrawResourceSprite("IMAGE_PROJECTILE_STAR", new Vector2(nativeX + 19f, nativeY + 19f), 1f,
                        star.PresentationSpin * ticks * Mathf.Rad2Deg), Is.True);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    Assert.That(MaximumPixelDifference(actual, pixels.GetPixels32()), Is.LessThanOrEqualTo(1),
                        "The 38x38 original star rotates about its center while only its shadow follows the roof slope.");
                    if (sample == 0)
                    {
                        move.Invoke(owner._game, new object[] { .1f, false });
                        star.PresentationPreviousX = star.X;
                        star.PresentationPreviousRow = star.FlightRow;
                    }
                }
            }
            finally
            {
                SpriteDrawState.Matrix = previousMatrix;
                SpriteDrawState.Color = previousColor;
                SpriteDrawState.Clip = previousClip;
                presenter.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Begins a native sprite frame over a white reference board.</summary>
        /// <param name="animations">The original artwork renderer receiving the frame.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Reads original sprite output from the real graphics device.</summary>
        /// <param name="animations">The renderer owning the submitted sprites.</param>
        /// <param name="target">The fixture's uniquely owned render target.</param>
        /// <param name="pixels">The reusable readable output texture.</param>
        private static void Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer)
                .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            pool.RenderTo(target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, 800f, 600f), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Measures the largest channel difference while allowing subpixel float rounding at the image edges.</summary>
        /// <param name="actual">The production presenter's rendered pixels.</param>
        /// <param name="expected">The independent original-artwork reference pixels.</param>
        /// <returns>The largest absolute eight-bit color channel difference.</returns>
        private static int MaximumPixelDifference(Color32[] actual, Color32[] expected)
        {
            var maximum = 0;
            for (var index = 0; index < actual.Length; index++)
            {
                maximum = Math.Max(maximum, Math.Abs(actual[index].r - expected[index].r));
                maximum = Math.Max(maximum, Math.Abs(actual[index].g - expected[index].g));
                maximum = Math.Max(maximum, Math.Abs(actual[index].b - expected[index].b));
                maximum = Math.Max(maximum, Math.Abs(actual[index].a - expected[index].a));
            }

            return maximum;
        }
    }
}
