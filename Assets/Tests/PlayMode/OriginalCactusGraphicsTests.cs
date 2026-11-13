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
    /// <summary>Checks the original cactus's authored rising and lowering artwork through the production plant presenter.</summary>
    public sealed class OriginalCactusGraphicsTests
    {
        /// <summary>Renders the original low, growing, extended, shrinking and restored cactus body poses.</summary>
        /// <returns>Camera frame boundaries before each original-artwork readback.</returns>
        [UnityTest]
        public IEnumerator OriginalCactusUsesAuthoredRiseHighShotAndLoweringFrames()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "Cactus.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original cactus fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 91);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(owner._game, new object[] { PlantKind.Cactus, 1, 3 })!;
                plant.PresentationRate = 12f;
                Tick(owner._game, plant, 1);
                ObjectSpriteRenderer.SceneCamera.targetTexture = target;
                yield return Capture(animations, presenter, plant, target, pixels);
                var low = pixels.GetPixels32();
                var lowTop = HighestGreen(low, target.width, target.height);

                owner._game.Zombies.Add(new Zombie
                {
                    Id = 91,
                    Kind = ZombieKind.Balloon,
                    Row = 1,
                    X = AdventureZombieNativeRules.GridX(600f),
                    Health = 270f,
                    StartingHealth = 270f,
                    Airborne = true,
                    PresentationNativePhase = 73
                });
                Tick(owner._game, plant, 1);
                Assert.That(plant.CactusPresentation!.Animation, Is.EqualTo("anim_rise"));
                yield return Capture(animations, presenter, plant, target, pixels);
                Tick(owner._game, plant, 60);
                yield return Capture(animations, presenter, plant, target, pixels);
                var rising = pixels.GetPixels32();
                Assert.That(Changed(low, rising), Is.GreaterThan(100), "The authored rise must visibly animate between the original low and high bodies.");

                Tick(owner._game, plant, 65);
                Assert.That(plant.CactusPresentation.Animation, Is.EqualTo("anim_shootinghigh"));
                yield return Capture(animations, presenter, plant, target, pixels);
                Tick(owner._game, plant, 22);
                yield return Capture(animations, presenter, plant, target, pixels);
                var high = pixels.GetPixels32();
                var highTop = HighestGreen(high, target.width, target.height);
                Assert.That(highTop, Is.LessThan(lowTop - 30f), "The original high body must extend upward without shifting the plant's ground origin.");
                Assert.That(Changed(rising, high), Is.GreaterThan(100));

                owner._game.Zombies.Clear();
                Tick(owner._game, plant, 18);
                Assert.That(plant.CactusPresentation.Animation, Is.EqualTo("anim_lower"));
                yield return Capture(animations, presenter, plant, target, pixels);
                Tick(owner._game, plant, 55);
                yield return Capture(animations, presenter, plant, target, pixels);
                var lowering = pixels.GetPixels32();
                Assert.That(Changed(high, lowering), Is.GreaterThan(100), "Losing the air target must play the original lowering frames.");
                Tick(owner._game, plant, 54);
                yield return Capture(animations, presenter, plant, target, pixels);
                Tick(owner._game, plant, 20);
                yield return Capture(animations, presenter, plant, target, pixels);
                Assert.That(plant.CactusPresentation.Animation, Is.EqualTo("anim_idle"));
                Assert.That(HighestGreen(pixels.GetPixels32(), target.width, target.height), Is.EqualTo(lowTop).Within(15f));
            }
            finally
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = null;
                presenter.Dispose();
                animations.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Advances only the production native cactus branch without spawning unrelated combat objects.</summary>
        /// <param name="game">The active simulation.</param>
        /// <param name="plant">The cactus being animated.</param>
        /// <param name="ticks">The number of native hundred-Hz updates.</param>
        private static void Tick(GameSimulation game, Plant plant, int ticks)
        {
            var update = typeof(GameSimulation).GetMethod("AdvanceAdventureNativeTimedPlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var tick = 0; tick < ticks; tick++)
            {
                update.Invoke(game, new object[] { plant });
            }
        }

        /// <summary>Submits the production plant body and its original shadow over a white reference board.</summary>
        /// <param name="animations">The renderer supplying the original cactus artwork.</param>
        /// <param name="presenter">The actual production plant presenter.</param>
        /// <param name="plant">The cactus whose current authored pose is being inspected.</param>
        /// <param name="target">The shared scene camera target.</param>
        /// <param name="pixels">The reusable readback image.</param>
        /// <returns>Camera frame boundaries before inspecting pixels.</returns>
        private static IEnumerator Capture(ReanimationRenderer animations, GamePresenter presenter, Plant plant, RenderTexture target, Texture2D pixels)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            CanvasUI.Color = Color.white;
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GamePresenter).GetMethod("BeginAdventurePresentation", flags)!.Invoke(presenter, null);
            typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!.Invoke(presenter, new object[] { plant });
            typeof(GamePresenter).GetMethod("EndAdventurePresentation", flags)!.Invoke(presenter, null);
            animations.EndObjectFrame();
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Finds the original cactus's highest green artwork pixel in the native board coordinate system.</summary>
        /// <param name="pixels">The actual scene camera output.</param>
        /// <param name="width">The physical readback width.</param>
        /// <param name="height">The physical readback height.</param>
        /// <returns>The uppermost green pixel's native y coordinate, or 600 when no cactus was rendered.</returns>
        private static float HighestGreen(Color32[] pixels, int width, int height)
        {
            var scale = Mathf.Min(width / 800f, height / 600f);
            var offsetX = (width - 800f * scale) * .5f;
            var offsetY = (height - 600f * scale) * .5f;
            var top = 600f;
            for (var index = 0; index < pixels.Length; index++)
            {
                var x = (index % width - offsetX) / scale;
                var y = (height - 1 - index / width - offsetY) / scale;
                var color = pixels[index];
                if (x >= 260f && x <= 450f && y >= 0f && y <= 300f
                    && color.g > color.r + 10 && color.g > color.b + 5 && color.g > 40)
                {
                    top = Mathf.Min(top, y);
                }
            }

            return top;
        }

        /// <summary>Counts visible body pixels that differ between two original authored poses.</summary>
        /// <param name="first">The previous camera output.</param>
        /// <param name="second">The current camera output.</param>
        /// <returns>The number of RGB changes above the channel-rounding tolerance.</returns>
        private static int Changed(Color32[] first, Color32[] second)
        {
            var count = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 20)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
