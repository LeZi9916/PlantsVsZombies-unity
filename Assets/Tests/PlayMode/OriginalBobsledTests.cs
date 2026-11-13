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
    /// <summary>Checks the production sled team submits original vehicle artwork and renders its damage and fade on the GPU.</summary>
    public sealed class OriginalBobsledTests
    {
        /// <summary>Checks the original four-member vehicle, three damage images, boarding, and broken-shell alpha.</summary>
        /// <returns>The graphics initialization frame.</returns>
        [UnityTest]
        public IEnumerator NativeSledArtworkSurroundsFourRidersAndFadesAfterBreaking()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original sled graphics fixture");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 28 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                for (var position = 0; position < 4; position++)
                {
                    owner._game.Zombies.Add(new Zombie
                    {
                        Id = 81 + position, Kind = ZombieKind.Bobsled, Row = 1,
                        X = AdventureZombieNativeRules.GridX(340f + position * 50f),
                        Health = 270f, StartingHealth = 270f, ArmorHealth = position == 0 ? 300f : 0f,
                        PresentationInPool = false, PresentationHasSled = true, PresentationNativePhase = 17,
                        PresentationNativeVelocity = .6f, Speed = .75f,
                        PresentationAltitude = AdventureBobsledRules.InitialAltitude(position),
                        BobsledState = new AdventureBobsledState { LeaderId = 81, Position = position }
                    });
                }

                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombieTeam", flags)!;
                Color32[]? previous = null;
                for (var stage = 0; stage < 3; stage++)
                {
                    owner._game.Zombies[0].ArmorHealth = stage == 0 ? 300f : stage == 1 ? 199f : 99f;
                    var pool = Submit(animations, presenter, draw, owner._game.Zombies[0]);
                    Assert.That(Find(pool, "Zombie_bobsled" + (stage + 1) + ".png"), Is.Not.Null);
                    Assert.That(Find(pool, "Zombie_bobsled_inside.png"), Is.Not.Null);
                    var heads = 0;
                    for (var index = 0; index < pool.SubmittedObjectCount; index++)
                    {
                        if (pool.SubmittedSprites[index].SourceImage?.SourceIdentity.EndsWith("Zombie_bobsled_newhead.png", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            heads++;
                        }
                    }

                    Assert.That(heads, Is.EqualTo(4), "Every linked body must be submitted exactly once.");
                    var current = Read(pool, target, pixels);
                    if (previous != null)
                    {
                        var differences = 0;
                        for (var index = 0; index < current.Length; index++)
                        {
                            if (!current[index].Equals(previous[index]))
                            {
                                differences++;
                            }
                        }

                        Assert.That(differences, Is.GreaterThan(20), "Original sled damage stages must change rendered pixels.");
                    }

                    previous = current;
                }

                foreach (var member in owner._game.Zombies)
                {
                    member.PresentationNativePhase = 18;
                    owner._game.AdventurePresentationZombieAnimation(member);
                    member.PresentationAnimationAge = .9f;
                    member.PresentationAltitude = 18f;
                }

                var boarded = Submit(animations, presenter, draw, owner._game.Zombies[0]);
                Assert.That(Find(boarded, "Zombie_bobsled3.png"), Is.Not.Null);
                Assert.That(Find(boarded, "Zombie_bobsled_inside.png"), Is.Not.Null);
                Read(boarded, target, pixels);
                foreach (var member in owner._game.Zombies)
                {
                    member.PresentationNativePhase = 19;
                    member.BobsledState!.PhaseTicks = 15;
                    member.PresentationAltitude = 0f;
                }

                var crashing = Submit(animations, presenter, draw, owner._game.Zombies[0]);
                var debris = Find(crashing, "Zombie_bobsled4.png");
                Assert.That(debris, Is.Not.Null);
                Assert.That(debris!.Colors[0].a, Is.EqualTo(127f / 255f).Within(.001f));
                Assert.That(Find(crashing, "Zombie_bobsled_inside.png"), Is.Null);
                Read(crashing, target, pixels);
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

        /// <summary>Submits the full team through the production presenter.</summary>
        /// <param name="animations">The original animation renderer.</param>
        /// <param name="presenter">The board presenter containing native vehicle composition.</param>
        /// <param name="draw">The production team drawing operation.</param>
        /// <param name="leader">The front member supplying the shared vehicle.</param>
        /// <returns>The actual renderer retaining submitted source commands.</returns>
        private static ObjectSpriteRenderer Submit(ReanimationRenderer animations, GamePresenter presenter, MethodInfo draw, Zombie leader)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
            draw.Invoke(presenter, new object[] { leader });
            animations.EndObjectFrame();
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
        }

        /// <summary>Finds one original source image in the actual submitted frame.</summary>
        /// <param name="pool">The renderer retaining the source image commands.</param>
        /// <param name="filename">The required unchanged original filename.</param>
        /// <returns>The submitted sprite, or null when the production frame omits it.</returns>
        private static ObjectSprite? Find(ObjectSpriteRenderer pool, string filename)
        {
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage?.SourceIdentity.EndsWith(filename, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>Renders the production frame on the real graphics device and reads its pixels.</summary>
        /// <param name="pool">The renderer containing the native sled team.</param>
        /// <param name="target">The reusable graphics output target.</param>
        /// <param name="pixels">The readable output texture.</param>
        /// <returns>The GPU's native-resolution color pixels.</returns>
        private static Color32[] Read(ObjectSpriteRenderer pool, RenderTexture target, Texture2D pixels)
        {
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
