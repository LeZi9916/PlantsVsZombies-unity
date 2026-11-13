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
    /// <summary>Checks original fog coverage, preview suppression and foreground pickup ordering through the production presenter.</summary>
    public sealed class OriginalFogGraphicsTests
    {
        /// <summary>Compares the original fog's real camera pixels during preview and gameplay.</summary>
        /// <returns>Camera frame boundaries before reading the original artwork.</returns>
        [UnityTest]
        public IEnumerator OriginalFogIsHiddenInPreviewAndUsesNativeLawnCoverage()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "images", "fog.png")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original fog fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var options = new OptionsView(root);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._optionsView = options;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
                typeof(GamePresenter).GetField("_renderingBoardObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(presenter, true);
                for (var tick = 0; tick < 100; tick++)
                {
                    owner._game.AdvanceAdventureFog();
                }

                ObjectSpriteRenderer.SceneCamera.targetTexture = target;
                owner._choosingSeeds = true;
                yield return Capture(animations, presenter, target, pixels);
                var preview = pixels.GetPixels32();
                owner._choosingSeeds = false;
                yield return Capture(animations, presenter, target, pixels);
                var playing = pixels.GetPixels32();
                var changed = 0;
                var changedLeft = 0;
                var scale = Mathf.Min(target.width / 800f, target.height / 600f);
                var margin = (target.width - 800f * scale) * .5f;
                for (var index = 0; index < playing.Length; index++)
                {
                    if (Math.Abs(preview[index].r - playing[index].r) + Math.Abs(preview[index].g - playing[index].g)
                        + Math.Abs(preview[index].b - playing[index].b) > 12)
                    {
                        changed++;
                        if (index % target.width < margin + 460f * scale)
                        {
                            changedLeft++;
                        }
                    }
                }

                Assert.That(changed, Is.GreaterThan((int)(10000f * scale * scale)), "The original seven-row fog artwork must cover the right lawn during gameplay.");
                Assert.That(changedLeft, Is.Zero, "Stage 4-1 starts its original fog grid at native x=465, without covering the left lawn.");

                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.Mowers.Clear();
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                owner._game.Adventure.Fog.BeginIntro(owner._game.AdventureFogLeftColumn);
                yield return Capture(animations, presenter, target, pixels, true);
                var empty = pixels.GetPixels32();
                var kinds = new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold, AdventureCoinKind.Diamond,
                    AdventureCoinKind.PlantGift, AdventureCoinKind.Chocolate };
                var drops = new AdventureCoin[kinds.Length];
                for (var index = 0; index < kinds.Length; index++)
                {
                    drops[index] = owner._game.RequiredSpawnAdventureCoin(kinds[index], 7.5f, index);
                }

                yield return Capture(animations, presenter, target, pixels, true);
                var clearDrops = pixels.GetPixels32();
                owner._game.Adventure.Fog.Advance(owner._game.AdventureFogLeftColumn, 0f, false, owner._game.Plants);
                yield return Capture(animations, presenter, target, pixels, true);
                var fogDrops = pixels.GetPixels32();
                for (var index = 0; index < drops.Length; index++)
                {
                    var bounds = owner.Layout.AdventureCoinRect(drops[index]);
                    bounds.xMin -= 20f;
                    bounds.xMax += 20f;
                    bounds.yMin -= 20f;
                    bounds.yMax += 20f;
                    var preserved = 0;
                    for (var pixel = 0; pixel < fogDrops.Length; pixel++)
                    {
                        var x = (pixel % target.width - margin) / scale;
                        var y = (target.height - 1 - pixel / target.width - (target.height - 600f * scale) * .5f) / scale;
                        if (bounds.Contains(new Vector2(x, y)) && Difference(empty[pixel], clearDrops[pixel]) > 30
                            && Difference(clearDrops[pixel], fogDrops[pixel]) <= 3)
                        {
                            preserved++;
                        }
                    }

                    Assert.That(preserved, Is.GreaterThan((int)(30f * scale * scale)),
                        kinds[index] + " must retain its opaque original artwork above fog, as native Coin layer 600001 exceeds Fog layer 500000.");
                }
            }
            finally
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = null;
                presenter.Dispose();
                animations.Dispose();
                options.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Submits only the production fog pass over a white board and reads the scene camera output.</summary>
        /// <param name="animations">The renderer owning the original artwork.</param>
        /// <param name="presenter">The production board presenter.</param>
        /// <param name="target">The shared scene camera target.</param>
        /// <param name="pixels">The reusable camera readback image.</param>
        /// <param name="wholeBoard">Whether to submit the complete production board, including pickup and fog ordering.</param>
        /// <returns>Frame boundaries before reading the visible fog pixels.</returns>
        private static IEnumerator Capture(ReanimationRenderer animations, GamePresenter presenter, RenderTexture target, Texture2D pixels, bool wholeBoard = false)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            CanvasUI.Color = Color.white;
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            if (wholeBoard)
            {
                typeof(GamePresenter).GetMethod("DrawAdventureBoard", flags)!.Invoke(presenter, null);
            }
            else
            {
                typeof(GamePresenter).GetMethod("BeginAdventurePresentation", flags)!.Invoke(presenter, null);
                typeof(GamePresenter).GetMethod("DrawAdventureFog", flags)!.Invoke(presenter, null);
                typeof(GamePresenter).GetMethod("EndAdventurePresentation", flags)!.Invoke(presenter, null);
            }

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

        /// <summary>Measures visible RGB change without treating alpha readback differences as artwork changes.</summary>
        /// <param name="first">The reference camera pixel.</param>
        /// <param name="second">The camera pixel being compared.</param>
        /// <returns>The sum of the three absolute RGB channel differences.</returns>
        private static int Difference(Color32 first, Color32 second)
        {
            return Math.Abs(first.r - second.r) + Math.Abs(first.g - second.g) + Math.Abs(first.b - second.b);
        }
    }
}
