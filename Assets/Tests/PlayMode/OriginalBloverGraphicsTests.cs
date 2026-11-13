#nullable enable

using System;
using System.Collections;
using System.IO;
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
    /// <summary>Checks the planted Blover's original authored blow and continuing loop through the production GPU renderer.</summary>
    public sealed class OriginalBloverGraphicsTests
    {
        /// <summary>Compares complete native-resolution body pixels with the unchanged original clips before and after wind starts.</summary>
        /// <returns>A frame boundary for the original artwork and graphics device.</returns>
        [UnityTest]
        public IEnumerator PlantedBloverUsesOriginalBlowAndContinuingWindAnimation()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "Blover.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            yield return null;
            var host = new GameObject("Inactive original Blover graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 91);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow2.png", null);
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!;
                var advance = typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", flags)!;
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(owner._game, new object[] { PlantKind.Blover, 0, 4 })!;
                Assert.That(animations.TryGetNativeAnimationInfo("Blover", "anim_blow", out var blow), Is.True);
                Assert.That(blow.Count, Is.EqualTo(20));
                Assert.That(animations.TryGetNativeAnimationInfo("Blover", "anim_loop", out var loop), Is.True);
                Assert.That(loop.Count, Is.EqualTo(10));
                Color32[]? first = null;
                var tick = 0;
                foreach (var sample in new[] { 0, 25, 49, 50, 99, 100, 125, 199 })
                {
                    while (tick < sample)
                    {
                        advance.Invoke(owner._game, new object[] { .01f });
                        tick++;
                    }

                    Assert.That(owner._game.Plants, Has.Member(plant));
                    Begin(animations);
                    draw.Invoke(presenter, new object[] { plant });
                    End(animations);
                    var actual = Read(animations, target, pixels);
                    Assert.That(Array.Exists(actual, color => color.r < 240 || color.g < 240 || color.b < 240), Is.True);
                    if (sample == 0)
                    {
                        first = actual;
                    }
                    else if (sample == 25)
                    {
                        Assert.That(actual, Is.Not.EqualTo(first), "The planted body must immediately animate through the original windup.");
                    }

                    Begin(animations);
                    var looping = sample >= 100;
                    Assert.That(animations.DrawNativePlayback("Blover", new Vector2(360f, 80f), 1f,
                        (looping ? sample - 100 : sample) * .01f, looping ? "anim_loop" : "anim_blow", looping, 20f), Is.True);
                    End(animations);
                    Assert.That(Read(animations, target, pixels), Is.EqualTo(actual),
                        "PC Blover must use its unchanged authored clip at twenty fps and native lawn origin on update " + sample + ".");
                }
            }
            finally
            {
                presenter.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Gives production and reference passes the same native clip, tint, transform, and white background.</summary>
        /// <param name="animations">The renderer receiving one complete native-resolution pass.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Completes one symmetrical native object and Canvas frame.</summary>
        /// <param name="animations">The renderer publishing the completed pass.</param>
        private static void End(ReanimationRenderer animations)
        {
            animations.EndObjectFrame();
            CanvasUI.EndFrame();
        }

        /// <summary>Renders the submitted original artwork and reads every native-resolution GPU pixel.</summary>
        /// <param name="animations">The renderer containing the completed pass.</param>
        /// <param name="target">The actual graphics capture target.</param>
        /// <param name="pixels">The reusable readable output texture.</param>
        /// <returns>The complete rendered pixels, including the common white background.</returns>
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
    }
}
