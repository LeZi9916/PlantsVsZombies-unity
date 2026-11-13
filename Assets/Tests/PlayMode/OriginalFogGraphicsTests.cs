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
    /// <summary>Checks original street-preview fog, native lawn coverage and foreground pickup ordering through the production presenter.</summary>
    public sealed class OriginalFogGraphicsTests
    {
        /// <summary>Compares the original fog's real camera pixels during preview and gameplay.</summary>
        /// <returns>Camera frame boundaries before reading the original artwork.</returns>
        [UnityTest]
        public IEnumerator OriginalFogCoversStreetPreviewAndKeepsPickupsInFront()
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
                ObjectSpriteRenderer.SceneCamera.targetTexture = target;
                yield return Capture(owner, animations, presenter, target, pixels);
                var clear = pixels.GetPixels32();
                var context = new AdventureIntroContext { Level = 31, NativeBackground = 3, ChooseSeedsOverride = true };
                var intro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
                owner.Opening._adventureIntro = intro;
                owner._game.BeginAdventureFogIntro();
                for (var tick = 0; tick < 425; tick++)
                {
                    intro.Tick();
                    owner._game.AdvanceAdventureFog(true);
                }

                owner.Opening._adventureIntroFrame = intro.Frame;
                owner._choosingSeeds = true;
                Assert.That(intro.NativeTime, Is.EqualTo(4250));
                Assert.That(intro.WaitingForSeeds, Is.True);
                Assert.That(intro.Frame.BoardX, Is.EqualTo(-380));
                Assert.That(owner._game.Adventure.Fog.Offset, Is.EqualTo(585f));
                yield return Capture(owner, animations, presenter, target, pixels);
                var preview = pixels.GetPixels32();
                var changed = 0;
                var changedLeft = 0;
                var scale = Mathf.Min(target.width / 800f, target.height / 600f);
                var margin = (target.width - 800f * scale) * .5f;
                for (var index = 0; index < preview.Length; index++)
                {
                    if (Difference(clear[index], preview[index]) > 12)
                    {
                        changed++;
                        if (index % target.width < margin + 665f * scale)
                        {
                            changedLeft++;
                        }
                    }
                }

                Assert.That(changed, Is.GreaterThan((int)(1000f * scale * scale)), "The held original fog grid must remain visible at the right of the street preview.");
                Assert.That(changedLeft, Is.Zero, "The original held offset places the first preview fog tile at screen x=1050-380=670.");

                yield return Capture(owner, animations, presenter, target, pixels, true);
                var emptyStreet = pixels.GetPixels32();
                foreach (var row in new[] { 1, 3 })
                {
                    owner.Opening._adventurePreview.Add(AdventureIntroPresentation.PlaceStreetZombie(context,
                        (int)ZombieKind.Normal, 4, row, maximum => 0, maximum => 0f, (kind, track) => track == "anim_idle"));
                }

                yield return Capture(owner, animations, presenter, target, pixels, true);
                var foggedZombies = pixels.GetPixels32();
                yield return Capture(owner, animations, presenter, target, pixels, true, true);
                var unobscuredZombies = pixels.GetPixels32();
                var occluded = 0;
                for (var index = 0; index < foggedZombies.Length; index++)
                {
                    if (Difference(emptyStreet[index], foggedZombies[index]) <= 12
                        && Difference(emptyStreet[index], unobscuredZombies[index]) > 60)
                    {
                        occluded++;
                    }
                }

                Assert.That(occluded, Is.GreaterThan((int)(30f * scale * scale)),
                    "Native preview zombies at layer 300000 must be obscured by street fog at layer 500000.");

                owner.Opening._adventurePreview.Clear();
                owner.Opening._adventureIntro = null;
                owner.Opening._adventureIntroFrame = null;
                owner._choosingSeeds = false;
                owner._game.Adventure.Fog.Advance(owner._game.AdventureFogLeftColumn, 0f, false, owner._game.Plants);
                yield return Capture(owner, animations, presenter, target, pixels);
                var playing = pixels.GetPixels32();
                changed = 0;
                changedLeft = 0;
                for (var index = 0; index < playing.Length; index++)
                {
                    if (Difference(clear[index], playing[index]) > 12)
                    {
                        changed++;
                        if (index % target.width < margin + 460f * scale)
                        {
                            changedLeft++;
                        }
                    }
                }

                Assert.That(changed, Is.GreaterThan((int)(10000f * scale * scale)), "The original seven-row fog artwork must cover the right lawn during gameplay.");
                Assert.That(changedLeft, Is.Zero, "Stage 4-1 starts its original settled fog grid at native x=465.");

                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.Mowers.Clear();
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                owner._game.Adventure.Fog.BeginIntro(owner._game.AdventureFogLeftColumn);
                yield return Capture(owner, animations, presenter, target, pixels, true);
                var empty = pixels.GetPixels32();
                var kinds = new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold, AdventureCoinKind.Diamond,
                    AdventureCoinKind.PlantGift, AdventureCoinKind.Chocolate };
                var drops = new AdventureCoin[kinds.Length];
                for (var index = 0; index < kinds.Length; index++)
                {
                    drops[index] = owner._game.RequiredSpawnAdventureCoin(kinds[index], 7.5f, index);
                }

                yield return Capture(owner, animations, presenter, target, pixels, true);
                var clearDrops = pixels.GetPixels32();
                owner._game.Adventure.Fog.Advance(owner._game.AdventureFogLeftColumn, 0f, false, owner._game.Plants);
                yield return Capture(owner, animations, presenter, target, pixels, true);
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

        /// <summary>Submits the fog pass or complete production board and reads the scene camera output.</summary>
        /// <param name="owner">The inactive game host supplying the real opening clock and board translation.</param>
        /// <param name="animations">The renderer owning the original artwork.</param>
        /// <param name="presenter">The production board presenter.</param>
        /// <param name="target">The shared scene camera target.</param>
        /// <param name="pixels">The reusable camera readback image.</param>
        /// <param name="wholeBoard">Whether to submit the complete production board, including pickup and fog ordering.</param>
        /// <param name="unobscuredPreview">Whether to redraw the same preview after fog as an independently visible occlusion reference.</param>
        /// <returns>Frame boundaries before reading the visible fog pixels.</returns>
        private static IEnumerator Capture(PvZGame owner, ReanimationRenderer animations, GamePresenter presenter, RenderTexture target,
            Texture2D pixels, bool wholeBoard = false, bool unobscuredPreview = false)
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
                if (unobscuredPreview)
                {
                    var matrix = CanvasUI.Matrix;
                    CanvasUI.Matrix = matrix * Matrix4x4.Translate(new Vector3(owner.Opening.RequiredAdventureIntroFrame.BoardX,
                        owner.Opening.RequiredAdventureIntroFrame.BoardY, 0f));
                    typeof(GamePresenter).GetMethod("DrawAdventureOpeningActors", flags)!.Invoke(presenter, null);
                    CanvasUI.Matrix = matrix;
                }
            }
            else
            {
                // The pool owns independent MeshRenderers outside the scene-sprite frame reset.
                // A fog-only capture must retire water left by the preceding complete-board capture.
                (typeof(GamePresenter).GetField("_poolRenderer", flags)!.GetValue(presenter) as OriginalPoolRenderer)?.Hide();
                if (owner.Opening.AdventureIntroActive)
                {
                    CanvasUI.Matrix *= Matrix4x4.Translate(new Vector3(owner.Opening.RequiredAdventureIntroFrame.BoardX,
                        owner.Opening.RequiredAdventureIntroFrame.BoardY, 0f));
                }

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
