#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks submitted production money artwork against original PC collection drawing operations.</summary>
    public sealed class OriginalCoinGraphicsTests
    {
        /// <summary>Checks original static currency faces, glow, diamond pivot, shrinking, and fading during real collection.</summary>
        /// <returns>A frame boundary allowing render pipeline initialization before submitting real original textures.</returns>
        [UnityTest]
        public IEnumerator FlyingMoneyUsesOriginalFacesGlowDiamondPivotAndOpacity()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original coin graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._choosingSeeds = true;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureBoard", flags)!;
                foreach (var kind in new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold, AdventureCoinKind.Diamond })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    owner._game.Mowers.Clear();
                    var coin = owner._game.RequiredSpawnAdventureCoin(kind, 5f, 2f);
                    Assert.That(owner._game.CollectAdventureCoin(coin), Is.True);
                    for (var frame = 0; frame < 2; frame++)
                    {
                        if (frame == 1)
                        {
                            for (var tick = 0; tick < 150 && coin.PresentationScale >= .85f; tick++)
                            {
                                owner._game.Update(.01f);
                            }

                            Assert.That(coin.IsBeingCollected, Is.True);
                            Assert.That(coin.PresentationScale, Is.LessThan(.85f), "A native pickup flight must reach its shrinking phase.");
                            Assert.That(coin.PresentationAlpha, Is.LessThan(1f));
                        }

                        BeginFrame(animations);
                        try
                        {
                            draw.Invoke(presenter, null);
                        }
                        finally
                        {
                            animations.EndObjectFrame();
                            CanvasUI.EndFrame();
                        }

                        var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
                        var textures = (Dictionary<string, Texture2D>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(animations)!;
                        var artwork = new HashSet<Texture>();
                        foreach (var entry in textures)
                        {
                            if (entry.Key.StartsWith("IMAGE_REANIM_COIN", StringComparison.Ordinal)
                                || entry.Key.StartsWith("IMAGE_REANIM_DIAMOND", StringComparison.Ordinal))
                            {
                                artwork.Add(entry.Value);
                            }
                        }

                        var actual = Snapshot(pool, artwork);
                        Assert.That(actual.Count, Is.GreaterThan(0), "The real board presenter must submit original money artwork.");
                        BeginFrame(animations);
                        try
                        {
                            CanvasUI.Color = new Color(1f, 1f, 1f, coin.PresentationAlpha);
                            var x = coin.PresentationPreviousX;
                            var y = coin.PresentationPreviousY;
                            var scale = coin.PresentationScale;
                            if (kind == AdventureCoinKind.Diamond)
                            {
                                // Original PC 0x43CC10 shifts the attachment by (18,13)*(1-scale).
                                animations.DrawNative("Diamond", new Vector2(x - 3f + 18f * (1f - scale), y + 4f + 13f * (1f - scale)), scale, coin.Age);
                            }
                            else
                            {
                                // PC 0x43CEE0 draws static full dollar faces, never the rotating coin reanimation.
                                var gold = kind == AdventureCoinKind.Gold;
                                var face = textures[gold ? "IMAGE_REANIM_COIN_GOLD_DOLLAR" : "IMAGE_REANIM_COIN_SILVER_DOLLAR"];
                                Assert.That(face.width, Is.EqualTo(gold ? 45 : 43));
                                Assert.That(face.height, Is.EqualTo(gold ? 45 : 40));
                                var glow = textures["IMAGE_REANIM_COINGLOW"];
                                Assert.That(glow.width, Is.EqualTo(80));
                                Assert.That(glow.height, Is.EqualTo(80));
                                DrawCentered(animations, glow, new Vector2(x + 26f, y + 28f), scale);
                                DrawCentered(animations, face, new Vector2(x + 8f + face.width * .5f, y + 10f + face.height * .5f), scale);
                            }
                        }
                        finally
                        {
                            animations.EndObjectFrame();
                            CanvasUI.EndFrame();
                        }

                        var expected = Snapshot(pool, artwork);
                        Assert.That(actual.Count, Is.EqualTo(expected.Count), "Flying silver/gold must contain only the original static face and glow; diamonds retain their original animation tracks.");
                        for (var index = 0; index < actual.Count; index++)
                        {
                            Assert.That(actual[index].Texture, Is.SameAs(expected[index].Texture), "The real original currency texture must be used.");
                            for (var vertex = 0; vertex < 4; vertex++)
                            {
                                Assert.That(Vector3.Distance(actual[index].Vertices[vertex], expected[index].Vertices[vertex]), Is.LessThan(.0001f), kind + " native flight position, scale, and pivot");
                                Assert.That(actual[index].Colors[vertex].a, Is.EqualTo(expected[index].Colors[vertex].a).Within(.0001f), kind + " original flight opacity");
                            }
                        }
                    }
                }
            }
            finally
            {
                presenter.Dispose();
                animations.Dispose();
                foreach (var texture in owner.Textures._textures.Values)
                {
                    UnityEngine.Object.Destroy(texture);
                }

                owner.Textures._textures.Clear();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Starts a production scene-object frame using original logical board coordinates.</summary>
        /// <param name="animations">The renderer receiving the original artwork.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            animations.BeginObjectFrame();
        }

        /// <summary>Submits the literal native image geometry independently of the production money branch.</summary>
        /// <param name="animations">The renderer receiving the reference quad.</param>
        /// <param name="texture">The original currency face or glow.</param>
        /// <param name="center">The original unscaled center in board pixels.</param>
        /// <param name="scale">The native current collection scale.</param>
        private static void DrawCentered(ReanimationRenderer animations, Texture texture, Vector2 center, float scale)
        {
            animations.DrawNativeImage(texture, new Rect(center.x - texture.width * .5f * scale,
                center.y - texture.height * .5f * scale, texture.width * scale, texture.height * scale), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Copies submitted original money geometry before the reusable scene pool is overwritten.</summary>
        /// <param name="pool">The actual scene pool used by the production renderer.</param>
        /// <param name="artwork">The original currency textures whose quads are selected.</param>
        /// <returns>Independent texture, vertex, and opacity snapshots in production submission order.</returns>
        private static List<(Texture Texture, Vector3[] Vertices, Color[] Colors)> Snapshot(ObjectSpriteRenderer pool, HashSet<Texture> artwork)
        {
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pool)!;
            var result = new List<(Texture, Vector3[], Color[])>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = sprites[index];
                var texture = sprite.Properties.GetTexture(Shader.PropertyToID("_MainTex"));
                if (artwork.Contains(texture))
                {
                    result.Add((texture, (Vector3[])sprite.Vertices.Clone(), (Color[])sprite.Colors.Clone()));
                }
            }

            return result;
        }
    }
}
