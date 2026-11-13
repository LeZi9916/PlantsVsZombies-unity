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
    /// <summary>Checks Jalapeno's planted fuse through the production presenter against unchanged original artwork.</summary>
    public sealed class OriginalJalapenoTests
    {
        /// <summary>Compares early, middle, late, and final pre-ignition poses with the PC anim_explode layer and shake.</summary>
        /// <returns>A frame boundary for graphics initialization.</returns>
        [UnityTest]
        public IEnumerator PlantedFuseUsesOriginalExplodeFramesAndNativeShake()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Jalapeno graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow.png", null!);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!;
                var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!;
                var plant = (Plant)place.Invoke(owner._game, new object[] { PlantKind.Jalapeno, 1, 4 })!;
                var state = plant.JalapenoPresentation!;
                Assert.That(animations.TryGetNativeAnimationInfo("Jalapeno", "anim_explode", out var info), Is.True);
                Assert.That(info.Start, Is.EqualTo(12));
                Assert.That(info.Count, Is.EqualTo(13));
                var previousTick = 0;
                Color32[]? first = null;
                foreach (var tick in new[] { 10, 40, 80, 99 })
                {
                    owner._game.Update((tick - previousTick) * .01f);
                    previousTick = tick;
                    Assert.That(owner._game.Plants, Does.Contain(plant));
                    Begin(animations);
                    draw.Invoke(presenter, new object[] { plant });
                    animations.EndObjectFrame();
                    var actual = Read(animations, target, pixels);
                    Save(pixels, "jalapeno-fuse-" + tick + ".png");

                    Begin(animations);
                    // PC 46C615 selects anim_explode/PlayOnceAndHold; 473200 adds two unscaled shake offsets.
                    // The authored source contains the swelling expression and stem, without an invented spark or flame.
                    var seconds = state.NormalizedTime * 13f / state.Rate;
                    Assert.That(animations.DrawNativePlayback("Jalapeno",
                        new Vector2(360f + state.ShakeX, 180f + state.ShakeY), 1f,
                        seconds, "anim_explode", false, state.Rate), Is.True);
                    animations.EndObjectFrame();
                    var expected = Read(animations, target, pixels);
                    Assert.That(DifferentPixels(actual, expected), Is.Zero,
                        "The production fuse must render the unmodified original pose at tick " + tick + ".");
                    if (first == null)
                    {
                        first = actual;
                    }
                    else
                    {
                        Assert.That(DifferentPixels(first, actual), Is.GreaterThan(100), "The original fuse must visibly advance before ignition.");
                    }
                }

                owner._game.Update(.01f);
                Assert.That(owner._game.Plants, Has.No.Member(plant));
                Assert.That(owner._game.Presentation.JalapenoFires, Has.Count.EqualTo(1));
            }
            finally
            {
                presenter.Dispose();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Begins an independent white-background frame in native logical pixels.</summary>
        /// <param name="animations">The production renderer being sampled.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Reads the real GPU rendering of the current original source commands.</summary>
        /// <param name="animations">The owner of the submitted sprite pool.</param>
        /// <param name="target">The native-resolution render target.</param>
        /// <param name="pixels">The reusable readable texture.</param>
        /// <returns>The captured original artwork over white.</returns>
        private static Color32[] Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            pool.RenderTo(target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, 800f, 600f), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            return pixels.GetPixels32();
        }

        /// <summary>Counts exact changed pixels between production and original reference captures.</summary>
        /// <param name="left">The production capture.</param>
        /// <param name="right">The original-artwork reference capture.</param>
        /// <returns>The number of changed pixels.</returns>
        private static int DifferentPixels(Color32[] left, Color32[] right)
        {
            var count = 0;
            for (var index = 0; index < left.Length; index++)
            {
                if (!left[index].Equals(right[index]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Saves optional diagnostics only into the explicitly selected test-output directory.</summary>
        /// <param name="pixels">The actual production capture.</param>
        /// <param name="name">The diagnostic filename.</param>
        private static void Save(Texture2D pixels, string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name), pixels.EncodeToPNG());
            }
        }
    }
}
