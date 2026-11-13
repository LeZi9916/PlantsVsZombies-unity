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
    /// <summary>Checks the PC continuous ice trail against original unscaled tiles, cap overlap, and melting alpha.</summary>
    public sealed class OriginalIceTrailTests
    {
        /// <summary>Compares production ice with right-aligned original full-size tiles on flat and roof rows.</summary>
        /// <returns>A frame boundary for real graphics initialization.</returns>
        [UnityTest]
        public IEnumerator TrailKeepsOriginalTilePixelsCapOverlapAndMeltingAlpha()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "images")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original ice trail graphics fixture");
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
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureIceTrail", flags)!;
                var tile = owner.Textures.RequiredLoadImage("ice.png");
                var cap = owner.Textures.RequiredLoadImage("ice_cap.png");
                Assert.That(new Vector2(tile.Width, tile.Height), Is.EqualTo(new Vector2(150f, 72f)));
                Assert.That(new Vector2(cap.Width, cap.Height), Is.EqualTo(new Vector2(25f, 72f)));
                foreach (var level in new[] { 8, 42 })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    // Native GridToPixelY(column 8, row 1) + 20: flat 180+20; roof 155+20.
                    var y = level == 42 ? 175f : 200f;
                    foreach (var minX in new[] { 25, 337, 499 })
                    {
                        foreach (var remaining in new[] { 3000, 5 })
                        {
                            var trail = new AdventureIceTrail { Id = 1, Row = 1, MinX = minX, RemainingTicks = remaining };
                            Begin(animations);
                            draw.Invoke(presenter, new object[] { trail });
                            animations.EndObjectFrame();
                            var actual = Read(animations, target, pixels);
                            Save(pixels, "ice-trail-" + level + "-" + minX + "-" + remaining + ".png");

                            Begin(animations);
                            // PC Board::DrawIce 421600 anchors source tiles at the right board edge.
                            // Draw complete 150x72 images backwards and clip the leftmost image instead of
                            // reproducing the production first-tile width/source-UV calculation.
                            SpriteDrawState.Color = new Color(1f, 1f, 1f, remaining == 5 ? 127f / 255f : 1f);
                            SpriteDrawState.Clip = new Rect(minX + 13f, 0f, 800f - minX - 13f, 600f);
                            for (var x = 650; x + 150 > minX + 13; x -= 150)
                            {
                                Assert.That(animations.DrawNativeImage(tile, new Rect(x, y, 150f, 72f), new Rect(0f, 0f, 1f, 1f)), Is.True);
                            }

                            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                            Assert.That(animations.DrawNativeImage(cap, new Rect(minX, y, 25f, 72f), new Rect(0f, 0f, 1f, 1f)), Is.True);
                            SpriteDrawState.Color = Color.white;
                            animations.EndObjectFrame();
                            var expected = Read(animations, target, pixels);
                            Assert.That(DifferentPixels(actual, expected), Is.Zero,
                                "Original ice pixels, cap overlap, roof height, or integer melting alpha changed at level "
                                + level + ", left edge " + minX + ", remaining updates " + remaining + ".");
                        }
                    }
                }
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

        /// <summary>Begins an independent white-background frame in native board pixels.</summary>
        /// <param name="animations">The renderer receiving production and reference commands.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Captures real graphics output from the retained native sprite commands.</summary>
        /// <param name="animations">The renderer owning the submitted sprite pool.</param>
        /// <param name="target">The native-resolution render target.</param>
        /// <param name="pixels">The readable output texture.</param>
        /// <returns>The complete original-artwork graphics capture.</returns>
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

        /// <summary>Counts exact changed pixels in the independent original-artwork comparison.</summary>
        /// <param name="left">The production ice trail capture.</param>
        /// <param name="right">The independently tiled original-artwork capture.</param>
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

        /// <summary>Saves optional captures only to the explicitly selected ignored test-output directory.</summary>
        /// <param name="pixels">The captured original artwork.</param>
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
