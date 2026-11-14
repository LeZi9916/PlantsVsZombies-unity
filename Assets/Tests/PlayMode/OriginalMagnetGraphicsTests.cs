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
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original Magnet-shroom body clips, captured metal coordinates, flight, and held-metal draw ordering.</summary>
    public sealed class OriginalMagnetGraphicsTests
    {
        /// <summary>Checks all original magnetic art is available and production drawing matches the original sucking, charging and ready poses.</summary>
        /// <returns>A frame boundary for real original artwork initialization.</returns>
        [UnityTest]
        public IEnumerator SuckingMetalFlightChargingAndReadyMatchOriginalArtwork()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original magnetic artwork fixture");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow.png", null!);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow2.png", null!);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(owner._game, new object[] { PlantKind.MagnetShroom, 2, 3 })!;
                var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", flags)!
                    .Invoke(owner._game, new object[] { ZombieKind.Buckethead, 2, AdventureZombieNativeRules.GridX(500f), 0 })!;
                var update = typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", flags)!;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!;
                update.Invoke(owner._game, new object[] { .01f });
                var state = plant.MagnetPresentation!;
                Assert.That(state.NativePhase, Is.EqualTo(26));
                Assert.That(zombie.ArmorHealth, Is.Zero);
                Assert.That(animations.TryGetNativeAnimationInfo("MagnetShroom", "anim_shooting", out var shooting), Is.True);
                Assert.That(shooting.Count, Is.EqualTo(26));
                foreach (var kind in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 20, 21 })
                {
                    Assert.That(animations.TryGetNativeSpriteSize(AdventureMagnetRules.Image(kind), out var size), Is.True,
                        "Original magnetic metal resource " + kind + " must exist.");
                    Assert.That(size.x > 0 && size.y > 0, Is.True);
                }

                Assert.That(animations.TryGetNativeSpriteSize("IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE1", out _), Is.True);
                SpriteDrawState.Color = Color.white;
                // Original Zombie.reanim frame44: bucket translation(1.5,-8), scale(.8,.8), original image62x67.
                // PC 481FE0/550430 includes its transformed image center and the unscaled minus-half-pixel offset.
                var initialX = 515f + (1.5f + 31f * .8f) - .5f;
                var initialY = 242f + (-8f + 33.5f * .8f) - .5f;
                initialX -= 31;
                initialY -= 33;
                for (var sample = 0; sample < 4; sample++)
                {
                    BeginFrame(animations);
                    draw.Invoke(presenter, new object[] { plant });
                    typeof(GamePresenter).GetMethod("DrawAdventureMagnetItemsOnTop", flags)!.Invoke(presenter, null);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var actual = pixels.GetPixels32();
                    Assert.That(Array.Exists(actual, pixel => pixel.r < 240 || pixel.g < 240 || pixel.b < 240), Is.True,
                        "The actual Magnet-shroom must submit the original body and metal artwork.");
                    if (state.Item != null)
                    {
                        Assert.That(state.Item.X, Is.EqualTo(initialX).Within(.0001f));
                        Assert.That(state.Item.Y, Is.EqualTo(initialY).Within(.0001f));
                    }

                    BeginFrame(animations);
                    DrawReference(animations, state, initialX, initialY);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    Assert.That(actual, Is.EqualTo(pixels.GetPixels32()),
                        "Original magnetic body, item position and draw order must match at phase " + state.NativePhase + ", sample " + sample);
                    if (sample == 0)
                    {
                        Tick(owner._game, update, 60);
                    }
                    else if (sample == 1)
                    {
                        while (state.NativePhase == 26)
                        {
                            Tick(owner._game, update, 1);
                        }

                        PrimeTransition(animations, presenter, draw, plant);
                        Tick(owner._game, update, 30);
                    }
                    else if (sample == 2)
                    {
                        while (state.NativePhase == 27)
                        {
                            Tick(owner._game, update, 1);
                        }

                        Assert.That(state.Item, Is.Null);
                        PrimeTransition(animations, presenter, draw, plant);
                        Tick(owner._game, update, 30);
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

        /// <summary>Draws the independent original body and bucket oracle from the supplied native source point.</summary>
        /// <param name="animations">The original resource renderer.</param>
        /// <param name="state">The phase and native timing selected by production.</param>
        /// <param name="initialX">The independently calculated original bucket source x.</param>
        /// <param name="initialY">The independently calculated original bucket source y.</param>
        private static void DrawReference(ReanimationRenderer animations, AdventureMagnetState state, float initialX, float initialY)
        {
            var item = state.Item;
            var x = initialX;
            var y = initialY;
            var onTop = false;
            if (item != null)
            {
                for (var tick = 0; tick < item.AgeTicks; tick++)
                {
                    var dx = 280f + item.DestinationX - x;
                    var dy = 280f + item.DestinationY - y;
                    if ((float)Math.Sqrt(dx * dx + dy * dy) < 20f)
                    {
                        break;
                    }

                    x += dx * .05f;
                    y += dy * .05f;
                }

                var remainingX = 280f + item.DestinationX - x;
                var remainingY = 280f + item.DestinationY - y;
                onTop = (float)Math.Sqrt(remainingX * remainingX + remainingY * remainingY) > 20f;
                if (!onTop)
                {
                    animations.DrawResourceSprite("IMAGE_REANIM_ZOMBIE_BUCKET1", new Vector2(x + 31f, y + 33.5f), .8f);
                }
            }

            var clip = state.NativePhase == 26 ? "anim_shooting" : state.NativePhase == 27 ? "anim_nonactive_idle2" : "anim_idle";
            var rate = state.NativePhase == 26 ? 12f : state.NativePhase == 27 ? 2f : state.AnimationRate;
            Assert.That(animations.DrawNativePlayback("MagnetShroom", new Vector2(280f, 280f), 1f,
                state.AnimationSeconds, clip, state.NativePhase != 26, rate), Is.True);
            if (item != null && onTop)
            {
                animations.DrawResourceSprite("IMAGE_REANIM_ZOMBIE_BUCKET1", new Vector2(x + 31f, y + 33.5f), .8f);
            }
        }

        /// <summary>Records the zero-time original clip handoff before checking a later fully blended pose.</summary>
        /// <param name="animations">The retained production renderer.</param>
        /// <param name="presenter">The actual plant presenter.</param>
        /// <param name="draw">The production plant draw operation.</param>
        /// <param name="plant">The Magnet-shroom whose clip has just changed.</param>
        private static void PrimeTransition(ReanimationRenderer animations, GamePresenter presenter, MethodInfo draw, Plant plant)
        {
            BeginFrame(animations);
            draw.Invoke(presenter, new object[] { plant });
            animations.EndObjectFrame();
        }

        /// <summary>Advances native plant abilities without moving the independent source zombie.</summary>
        /// <param name="game">The active board.</param>
        /// <param name="update">The real production plant update.</param>
        /// <param name="count">The number of native hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, MethodInfo update, int count)
        {
            for (var tick = 0; tick < count; tick++)
            {
                update.Invoke(game, new object[] { .01f });
            }
        }

        /// <summary>Starts a native artwork frame on an opaque white background.</summary>
        /// <param name="animations">The renderer receiving original sprites.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Renders through the actual graphics device and reads all original board pixels.</summary>
        /// <param name="animations">The original resource renderer.</param>
        /// <param name="target">The unique fixture render target.</param>
        /// <param name="pixels">The reusable readable texture.</param>
        private static void Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            var renderer = (ObjectSpriteRenderer)typeof(ReanimationRenderer)
                .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            renderer.RenderTo(target);
            var old = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, 800f, 600f), 0, 0);
            pixels.Apply();
            RenderTexture.active = old;
        }
    }
}
