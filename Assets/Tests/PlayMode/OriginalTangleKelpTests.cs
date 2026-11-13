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
    /// <summary>Checks the original Kelp grab artwork, native attachment origin, held frame, and sinking waterline.</summary>
    public sealed class OriginalTangleKelpTests
    {
        /// <summary>Compares production attachments with unchanged original grab artwork for ordinary and snorkel bodies.</summary>
        /// <returns>The frame required to initialize the real graphics device.</returns>
        [UnityTest]
        public IEnumerator GrabUsesOriginalAttachmentOffsetsHeldClipAndWaterline()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Kelp graphics fixture");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var grab = typeof(GamePresenter).GetMethod("DrawAdventureTangleKelpGrab", flags)!;
                Assert.That(animations.TryGetNativeAnimationInfo("Tanglekelp", "anim_grab", out var info), Is.True);
                Assert.That(info.Count, Is.EqualTo(5), "The reference must retain the original five-frame grab clip.");

                foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.Snorkel })
                {
                    var snorkel = kind == ZombieKind.Snorkel;
                    var initialAltitude = snorkel ? 10f : -40f;
                    var zombie = new Zombie
                    {
                        Id = snorkel ? 82 : 81, Kind = kind, Row = 2, X = 4f,
                        Health = 270f, StartingHealth = 270f, PresentationInPool = true,
                        PresentationAltitude = initialAltitude, PresentationNativePhase = snorkel ? 61 : 0,
                        PresentationAnimationAge = .2f,
                        PoolState = snorkel ? null : new AdventureZombiePoolState { HasDuckyTube = true, Altitude = initialAltitude }
                    };
                    var state = new AdventureTangleKelpState
                    {
                        TargetZombieId = zombie.Id, OffsetX = snorkel ? -43 : -13, OffsetY = snorkel ? 55 : 15
                    };
                    zombie.TangleKelpGrab = state;
                    owner._game.Zombies.Add(zombie);
                    var waterline = snorkel ? 338f : 340f;
                    Color32[]? held = null;
                    foreach (var countdown in new[] { 90, 75, 60, 40 })
                    {
                        state.Countdown = countdown;
                        zombie.PresentationNativeHeight = countdown <= 50 ? 3 : 0;
                        zombie.PresentationAltitude = initialAltitude - (countdown <= 50 ? 50 - countdown : 0);
                        if (zombie.PoolState != null)
                        {
                            zombie.PoolState.Altitude = zombie.PresentationAltitude;
                        }

                        Begin(animations);
                        grab.Invoke(presenter, new object[] { zombie, zombie.X });
                        animations.EndObjectFrame();
                        var actual = Read(animations, target, pixels);
                        Save(pixels, "kelp-" + kind + "-" + countdown + ".png");
                        AssertWaterline(animations, waterline);

                        Begin(animations);
                        // PC 0x46E430 attaches the complete reanimation after the body at mPos + offset.
                        // For row 2: mPosX = 80 * 4 - 32, mPosY = 80 + 85 * 2 - 30.
                        SpriteDrawState.Clip = new Rect(0f, 0f, 800f, waterline);
                        Assert.That(animations.DrawNativePlayback("Tanglekelp",
                            new Vector2(288f + (snorkel ? -43f : -13f),
                                220f - zombie.PresentationAltitude + (snorkel ? 55f : 15f)),
                            1f, (100 - countdown) * .01f, "anim_grab", false, 24f), Is.True);
                        SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                        animations.EndObjectFrame();
                        var expected = Read(animations, target, pixels);
                        Assert.That(DifferentPixels(actual, expected), Is.Zero,
                            kind + " countdown " + countdown + " must use original offsets, 24 fps once/hold, and the body waterline.");

                        if (countdown == 75)
                        {
                            held = actual;
                        }
                        else if (countdown == 60)
                        {
                            Assert.That(DifferentPixels(actual, held!), Is.Zero, "The completed five-frame clip must hold its final pose.");
                        }

                        if (countdown == 90)
                        {
                            Begin(animations);
                            draw.Invoke(presenter, new object[] { zombie });
                            animations.EndObjectFrame();
                            var attached = Read(animations, target, pixels);
                            zombie.TangleKelpGrab = null;
                            Begin(animations);
                            draw.Invoke(presenter, new object[] { zombie });
                            animations.EndObjectFrame();
                            var body = Read(animations, target, pixels);
                            zombie.TangleKelpGrab = state;
                            Assert.That(DifferentPixels(attached, body), Is.GreaterThan(20),
                                "The real zombie presenter must retain and display the original grab attachment.");
                            Assert.That(owner._game.Zombies, Does.Contain(zombie));
                        }
                        else if (countdown == 40)
                        {
                            Begin(animations);
                            draw.Invoke(presenter, new object[] { zombie });
                            animations.EndObjectFrame();
                            AssertWaterline(animations, waterline);
                        }
                    }

                    owner._game.Zombies.Remove(zombie);
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

        /// <summary>Begins a white-background frame in native board coordinates.</summary>
        /// <param name="animations">The renderer receiving production and reference commands.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Reads real GPU output from the renderer's retained scene commands.</summary>
        /// <param name="animations">The renderer owning the submitted sprite pool.</param>
        /// <param name="target">The native-resolution graphics target.</param>
        /// <param name="pixels">The reusable readable pixel buffer.</param>
        /// <returns>The complete white-background graphics output.</returns>
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

        /// <summary>Checks attachment clipping and that a sinking body restores clipping for every original track.</summary>
        /// <param name="animations">The renderer whose source commands are checked.</param>
        /// <param name="bottom">The independently calculated native waterline.</param>
        private static void AssertWaterline(ReanimationRenderer animations, float bottom)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            var count = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage?.SourceTexture == Texture2D.whiteTexture)
                {
                    continue;
                }

                count++;
                Assert.That(sprite.ClipEnabled, Is.True);
                Assert.That(sprite.Clip.yMax, Is.LessThanOrEqualTo(bottom + .01f));
            }

            Assert.That(count, Is.GreaterThan(0), "The original grab must submit authored artwork.");
        }

        /// <summary>Counts differing graphics pixels without constructing a second image oracle.</summary>
        /// <param name="left">The production or held-frame image.</param>
        /// <param name="right">The independently placed original artwork.</param>
        /// <returns>The exact number of changed pixels.</returns>
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

        /// <summary>Saves optional captures only into the explicitly selected ignored test-output directory.</summary>
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
