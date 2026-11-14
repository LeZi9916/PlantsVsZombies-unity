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
    /// <summary>Checks the real Split Pea muzzles and level projectile paths against original artwork.</summary>
    public sealed class OriginalSplitPeaGraphicsTests
    {
        /// <summary>Checks both original muzzle positions and fixed projectile height while crossing a roof slope.</summary>
        /// <returns>A frame boundary for real original artwork and graphics initialization.</returns>
        [UnityTest]
        public IEnumerator BothMuzzlesMatchOriginalAnchorAndKeepTheirHeightAcrossRoofSlope()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Split Pea graphics fixture");
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
                var plant = new Plant { Kind = PlantKind.SplitPea, Row = 2, Column = 3, Age = .625f, PresentationRate = 20f };
                owner._game.Plants.Add(new Plant { Kind = PlantKind.FlowerPot, Row = 2, Column = 3 });
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var fire = typeof(GameSimulation).GetMethod("FireAdventurePea", flags)!;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureProjectile", flags)!;
                var move = typeof(GameSimulation).GetMethod("AdvanceAdventureProjectiles", flags)!;
                var forward = (Pea)fire.Invoke(owner._game, new object[] { plant, plant.Row, false, 20f })!;
                var backward = (Pea)fire.Invoke(owner._game, new object[] { plant, plant.Row, true, 20f })!;
                Assert.That(animations.TryGetNativeTrackPosition("SplitPea", "anim_idle", "anim_idle",
                    plant.Age, plant.PresentationRate, true, out var anchor), Is.True);
                var frontCenter = new Vector2(280f + (int)anchor.x + 24f + 14f, 280f + (int)anchor.y - 33f - 5f + 14f);
                var rearCenter = frontCenter - new Vector2(88f, 0f);
                SpriteDrawState.Matrix = Matrix4x4.identity;
                SpriteDrawState.Color = Color.white;
                SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                for (var sample = 0; sample < 2; sample++)
                {
                    BeginFrame(animations);
                    draw.Invoke(presenter, new object[] { forward });
                    draw.Invoke(presenter, new object[] { backward });
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var actual = pixels.GetPixels32();
                    Assert.That(Array.Exists(actual, pixel => pixel.g > pixel.r + 10 && pixel.g > pixel.b + 10), Is.True,
                        "Both production projectiles must submit real green original pea artwork.");
                    BeginFrame(animations);
                    Assert.That(animations.DrawResourceSprite("IMAGE_PROJECTILEPEA", frontCenter, 1f), Is.True);
                    Assert.That(animations.DrawResourceSprite("IMAGE_PROJECTILEPEA", rearCenter, 1f), Is.True);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    Assert.That(actual, Is.EqualTo(pixels.GetPixels32()),
                        "PC 4752E0 uses body-anchor muzzles 88 pixels apart; pea Y remains fixed after launch.");
                    if (sample == 0)
                    {
                        move.Invoke(owner._game, new object[] { .5f, false });
                        // Freeze the current native sample rather than rendering the previous interpolation endpoint.
                        forward.PresentationPreviousX = forward.X;
                        backward.PresentationPreviousX = backward.X;
                        frontCenter.x += 166.5f;
                        rearCenter.x -= 166.5f;
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

        /// <summary>Begins a native sprite frame over the same white reference background.</summary>
        /// <param name="animations">The original artwork renderer receiving the frame.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Renders submitted sprites through the real graphics device and reads their pixels.</summary>
        /// <param name="animations">The renderer owning the submitted sprites.</param>
        /// <param name="target">The fixture's uniquely owned render target.</param>
        /// <param name="pixels">The reusable readable image receiving the rendered frame.</param>
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
    }
}
