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
                        var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(animations)!;
                        var artwork = new HashSet<SpriteImage>();
                        foreach (var entry in textures)
                        {
                            if (entry.Key.StartsWith("IMAGE_REANIM_COIN", StringComparison.Ordinal)
                                || entry.Key.StartsWith("IMAGE_REANIM_DIAMOND", StringComparison.Ordinal))
                            {
                                artwork.Add(entry.Value);
                            }
                        }

                        var actual = Snapshot(pool, artwork);
                        var submittedSources = new List<string>();
                        for (var index = 0; index < pool.SubmittedObjectCount; index++)
                        {
                            submittedSources.Add(pool.SubmittedSprites[index].SourceImage?.SourceIdentity ?? "(missing source)");
                        }

                        Assert.That(actual.Count, Is.GreaterThan(0), "The real board presenter must submit original money artwork. "
                            + kind + " frame " + frame + "; loaded=" + string.Join(",", textures.Keys)
                            + "; submitted=" + string.Join(",", submittedSources) + "; clip=" + SpriteDrawState.Clip
                            + "; culled=" + pool.LastFrameStatistics.CulledSpriteCount + "; error=" + animations.LastError);
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
                                Assert.That(face.Width, Is.EqualTo(gold ? 45 : 43));
                                Assert.That(face.Height, Is.EqualTo(gold ? 45 : 40));
                                var glow = textures["IMAGE_REANIM_COINGLOW"];
                                Assert.That(glow.Width, Is.EqualTo(80));
                                Assert.That(glow.Height, Is.EqualTo(80));
                                DrawCentered(animations, glow, new Vector2(x + 26f, y + 28f), scale);
                                DrawCentered(animations, face, new Vector2(x + 8f + face.Width * .5f, y + 10f + face.Height * .5f), scale);
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
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks native airborne dollar faces switch to the original animated coin attachment only after landing.</summary>
        /// <returns>A graphics initialization frame before submitting the production board.</returns>
        [UnityTest]
        public IEnumerator DroppedSilverAndGoldUseAirborneFacesThenAnimatedGroundArtwork()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original falling coin graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
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
                foreach (var kind in new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    owner._game.ResetAdventureWaveSchedule(100000, false, false);
                    owner._game.Mowers.Clear();
                    var coin = owner._game.RequiredSpawnAdventureCoin(kind, 5f, 2f);
                    Assert.That(coin.HitGround, Is.False);
                    var airborne = DrawMoneySnapshot(animations, presenter, draw);
                    Assert.That(airborne.Count, Is.EqualTo(2), "PC 43CC10 hides the rotating attachment until landing.");
                    var face = kind == AdventureCoinKind.Gold ? "Coin_gold_dollar.png" : "Coin_silver_dollar.png";
                    Assert.That(airborne.Exists(sprite => sprite.Texture.SourceIdentity.EndsWith(face, StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(airborne.Exists(sprite => sprite.Texture.SourceIdentity.EndsWith("CoinGlow.png", StringComparison.OrdinalIgnoreCase)), Is.True);

                    for (var tick = 0; tick < 100 && !coin.HitGround; tick++)
                    {
                        owner._game.Update(.01f);
                    }

                    Assert.That(coin.HitGround, Is.True);
                    coin.PresentationPreviousX = coin.PresentationX;
                    coin.PresentationPreviousY = coin.PresentationY;
                    var grounded = DrawMoneySnapshot(animations, presenter, draw);
                    Assert.That(grounded.Count, Is.GreaterThan(0));
                    coin.Age += .17f;
                    var animated = DrawMoneySnapshot(animations, presenter, draw);
                    Assert.That(SameMoneyPose(grounded, animated), Is.False, "A landed original coin must keep rotating while its physical position stays fixed.");
                    Assert.That(coin.PresentationX, Is.EqualTo(coin.PresentationPreviousX));
                    Assert.That(coin.PresentationY, Is.EqualTo(coin.GroundY));
                }
            }
            finally
            {
                presenter.Dispose();
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Captures only the original currency commands submitted through the complete production board presenter.</summary>
        /// <param name="animations">The renderer retaining original currency images and scene commands.</param>
        /// <param name="presenter">The production board presenter.</param>
        /// <param name="draw">The production board draw entry point.</param>
        /// <returns>Independent money source images, transformed vertices, and tint values.</returns>
        private static List<(SpriteImage Texture, Vector3[] Vertices, Color[] Colors)> DrawMoneySnapshot(ReanimationRenderer animations, GamePresenter presenter, MethodInfo draw)
        {
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

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(animations)!;
            var artwork = new HashSet<SpriteImage>();
            foreach (var entry in textures)
            {
                if (entry.Key.StartsWith("IMAGE_REANIM_COIN", StringComparison.Ordinal))
                {
                    artwork.Add(entry.Value);
                }
            }

            return Snapshot(pool, artwork);
        }

        /// <summary>Compares submitted money sources and geometry independently of unrelated board artwork.</summary>
        /// <param name="first">The first production money frame.</param>
        /// <param name="second">The later production money frame at the same physical position.</param>
        /// <returns>Whether every money image and transformed corner stayed unchanged.</returns>
        private static bool SameMoneyPose(List<(SpriteImage Texture, Vector3[] Vertices, Color[] Colors)> first,
            List<(SpriteImage Texture, Vector3[] Vertices, Color[] Colors)> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (var index = 0; index < first.Count; index++)
            {
                if (!ReferenceEquals(first[index].Texture, second[index].Texture))
                {
                    return false;
                }

                for (var corner = 0; corner < 4; corner++)
                {
                    if (Vector3.Distance(first[index].Vertices[corner], second[index].Vertices[corner]) > .0001f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Starts a production scene-object frame using original logical board coordinates.</summary>
        /// <param name="animations">The renderer receiving the original artwork.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            // This fixture compares unscaled native geometry rather than the physical camera viewport.
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
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
        private static void DrawCentered(ReanimationRenderer animations, SpriteImage texture, Vector2 center, float scale)
        {
            animations.DrawNativeImage(texture, new Rect(center.x - texture.Width * .5f * scale,
                center.y - texture.Height * .5f * scale, texture.Width * scale, texture.Height * scale), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Copies submitted original money geometry before the reusable scene pool is overwritten.</summary>
        /// <param name="pool">The actual scene pool used by the production renderer.</param>
        /// <param name="artwork">The original currency textures whose quads are selected.</param>
        /// <returns>Independent texture, vertex, and opacity snapshots in production submission order.</returns>
        private static List<(SpriteImage Texture, Vector3[] Vertices, Color[] Colors)> Snapshot(ObjectSpriteRenderer pool, HashSet<SpriteImage> artwork)
        {
            var sprites = pool.SubmittedSprites;
            var result = new List<(SpriteImage, Vector3[], Color[])>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = sprites[index];
                var texture = sprite.SourceImage;
                if (texture != null && artwork.Contains(texture))
                {
                    result.Add((texture, (Vector3[])sprite.Vertices.Clone(), (Color[])sprite.Colors.Clone()));
                }
            }

            return result;
        }
    }
}
