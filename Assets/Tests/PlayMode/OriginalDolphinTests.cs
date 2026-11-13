#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
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
    /// <summary>Checks every original dolphin rider phase against unchanged artwork in the source track order.</summary>
    public sealed class OriginalDolphinTests
    {
        /// <summary>Compares entrance, ride, jump, swimming, eating, walking, dragged, and death pixels with the PC draw contract.</summary>
        /// <returns>A frame boundary for the real graphics device.</returns>
        [UnityTest]
        public IEnumerator NativePhasesKeepOriginalOffsetsWaterTracksAndDeathArtwork()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            var definition = new XmlDocument { XmlResolver = null };
            definition.LoadXml("<root>" + File.ReadAllText(Path.Combine(root, "reanim", "Zombie_dolphinrider.reanim")) + "</root>");
            var tracks = new List<string>();
            foreach (XmlNode track in definition.SelectNodes("/root/track")!)
            {
                tracks.Add(track.SelectSingleNode("name")!.InnerText);
            }

            yield return null;
            var host = new GameObject("Inactive original dolphin graphics fixture");
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
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow.png", null!);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow2.png", null!);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var waterImages = new HashSet<SpriteImage>();
                var load = typeof(ReanimationRenderer).GetMethod("LoadTexture", flags)!;
                foreach (XmlNode image in definition.SelectNodes("/root/track[name='Zombie_dolphinrider_whitewater' or name='Zombie_dolphinrider_dolphininwater']/t/i")!)
                {
                    if (!string.IsNullOrEmpty(image.InnerText))
                    {
                        waterImages.Add((SpriteImage)load.Invoke(animations, new object[] { image.InnerText })!);
                    }
                }

                Assert.That(waterImages.Count, Is.GreaterThan(0));
                var samples = new (int Phase, string Clip, float Seconds, float Rate, float Altitude,
                    float OffsetX, float OffsetY, float Bottom, bool Eating, int Height)[]
                {
                    (51, "anim_walkdolphin", .37f, 12f, 0f, 0f, 0f, 600f, false, 0),
                    (52, "anim_jumpinpool", .30f * 45f / 16f, 16f, 0f, 0f, 0f, 600f, false, 0),
                    (52, "anim_jumpinpool", .60f * 45f / 16f, 16f, 0f, 0f, 0f, 333f, false, 0),
                    (52, "anim_jumpinpool", .70f * 45f / 16f, 16f, 0f, 0f, 0f, 600f, false, 0),
                    (52, "anim_jumpinpool", .80f * 45f / 16f, 16f, 0f, 0f, 0f, 343f, false, 0),
                    (53, "anim_ride", .38f, 12f, 0f, 70f, 0f, 343f, false, 0),
                    (54, "anim_dolphinjump", .03f * 23f / 10f, 10f, 20f, 90f, 0f, 343f, false, 0),
                    (54, "anim_dolphinjump", .25f * 23f / 10f, 10f, 20f, 90f, 0f, 600f, false, 0),
                    (54, "anim_dolphinjump", .60f * 23f / 10f, 10f, 20f, 90f, 0f, 326f, false, 0),
                    (54, "anim_dolphinjump", .90f * 23f / 10f, 10f, 20f, 90f, 0f, 600f, false, 0),
                    (55, "anim_swim", .30f, 12f, 0f, 0f, 50f, 600f, false, 0),
                    (55, "anim_eat", .30f, 36f, 0f, 0f, 50f, 600f, true, 0),
                    (56, "anim_walk", .37f, 12f, 0f, 0f, 0f, 600f, false, 0),
                    (56, "anim_eat", .30f, 36f, 0f, 0f, 0f, 600f, true, 0),
                    (53, "anim_ride", .38f, 12f, -20f, 70f, 0f, 348f, false, 3),
                    (55, "anim_swim", .30f, 12f, -20f, 0f, 50f, 347f, false, 3)
                };
                var id = 100;
                foreach (var sample in samples)
                {
                    var zombie = new Zombie
                    {
                        Id = ++id, Kind = ZombieKind.DolphinRider, Row = 2, X = 4f,
                        Health = 500f, StartingHealth = 500f, UsedAbility = sample.Phase >= 54,
                        PresentationInPool = sample.Phase >= 53 && sample.Phase <= 55,
                        PresentationNativePhase = sample.Phase, PresentationNativeHeight = sample.Height,
                        PresentationAltitude = sample.Altitude, PresentationAnimation = sample.Clip,
                        PresentationAnimationAge = sample.Seconds, PresentationMovementRate = sample.Rate,
                        _presentationMovementAnimation = sample.Clip, Eating = sample.Eating
                    };
                    owner._game.Zombies.Add(zombie);
                    Assert.That(owner._game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo(sample.Clip));
                    Assert.That(owner._game.AdventurePresentationZombieRate(zombie), Is.EqualTo(sample.Rate));
                    Begin(animations);
                    draw.Invoke(presenter, new object[] { zombie });
                    animations.EndObjectFrame();
                    var actual = Read(animations, target, pixels);
                    Save(pixels, "dolphin-" + id + "-phase-" + sample.Phase + ".png");
                    var waterAlpha = sample.Phase == 53 && sample.Height == 0
                        ? WaterTrackAlpha(animations, waterImages) : null;

                    Begin(animations);
                    // PC GetDrawPos 549740: base source origin is (mPosX+15,mPosY-8), then unscaled phase offsets.
                    // Row 2 at grid X 4 has mPos=(288,220); do not fit or rescale any original source frame.
                    DrawReference(animations, tracks, sample.Clip,
                        new Vector2(303f + sample.OffsetX, 212f + sample.OffsetY - sample.Altitude),
                        sample.Seconds, sample.Rate, sample.Phase == 52 || sample.Phase == 54,
                        sample.Phase == 53, sample.Bottom, sample.Height == 3);
                    animations.EndObjectFrame();
                    var expected = Read(animations, target, pixels);
                    Save(pixels, "dolphin-" + id + "-expected.png");
                    Assert.That(DifferentPixels(actual, expected), Is.Zero,
                        "Original dolphin pixels differ at phase " + sample.Phase + ", time " + sample.Seconds + ", height " + sample.Height + ".");
                    if (waterAlpha != null)
                    {
                        // PC SetupWaterTrack preserves these source colors even when body cold and hit feedback are active.
                        zombie.ChillRemaining = 1f;
                        zombie.PresentationFlash = .1f;
                        Begin(animations);
                        draw.Invoke(presenter, new object[] { zombie });
                        animations.EndObjectFrame();
                        AssertOriginalWaterColor(animations, waterImages, waterAlpha);
                        zombie.ChillRemaining = 0f;
                        zombie.PresentationFlash = 0f;
                    }

                    owner._game.Zombies.Remove(zombie);
                }

                var dying = new Zombie
                {
                    Id = ++id, Kind = ZombieKind.DolphinRider, Row = 2, X = 4f,
                    Health = 500f, StartingHealth = 500f, UsedAbility = true,
                    PresentationInPool = true, PresentationNativePhase = 55,
                    PresentationAnimation = "anim_swim", PresentationAnimationAge = .3f
                };
                typeof(GameSimulation).GetMethod("AdventurePresentationZombieDeath", flags)!.Invoke(owner._game, new object[] { dying, 0U });
                Assert.That(owner._game.Presentation.Corpses, Has.Count.EqualTo(1));
                var corpse = owner._game.Presentation.Corpses[0];
                Assert.That(corpse.Animation, Is.EqualTo("anim_death"), "The original dolphin source has no anim_waterdeath layer.");
                Assert.That(dying.PresentationNativePhase, Is.EqualTo(1));
                corpse.Age = .4f;
                Begin(animations);
                typeof(GamePresenter).GetMethod("DrawAdventureCorpses", flags)!.Invoke(presenter, new object[] { 2 });
                animations.EndObjectFrame();
                var death = Read(animations, target, pixels);
                Save(pixels, "dolphin-water-death.png");
                Begin(animations);
                DrawReference(animations, tracks, "anim_death", new Vector2(303f, 262f), .4f, corpse.Rate,
                    true, false, 339f, false);
                animations.EndObjectFrame();
                Assert.That(DifferentPixels(death, Read(animations, target, pixels)), Is.Zero,
                    "The original swimming death retains its +50 image offset, altitude, water tracks, and 44-pixel clip height.");
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

        /// <summary>Checks a real cached jump transitions to the rebased swim pose before an immediate original meal blend.</summary>
        /// <returns>A frame boundary for initializing the real graphics device.</returns>
        [UnityTest]
        public IEnumerator LandingAndImmediateMealNeverReuseTheDisplacedJumpPose()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "Zombie_dolphinrider.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive dolphin landing graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            using var reference = new ReanimationRenderer(root);
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
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow.png", null!);
                SpriteSourceTestUtilities.SetImage(owner.Textures, "plantshadow2.png", null!);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var land = typeof(GameSimulation).GetMethod("AdvanceAdventureDolphin", flags)!;
                foreach (var eating in new[] { false, true })
                {
                    var zombie = new Zombie
                    {
                        Id = eating ? 902 : 901,
                        Kind = ZombieKind.DolphinRider,
                        Row = 2,
                        X = AdventureZombieNativeRules.GridX(400f),
                        Health = 500f,
                        StartingHealth = 500f,
                        PresentationInPool = true,
                        PresentationNativePhase = 54,
                        PresentationNativeHeight = 0,
                        PresentationAltitude = 10f,
                        _presentationInitialized = true,
                        DolphinState = new AdventureDolphinState()
                    };
                    zombie.PresentationPreviousX = zombie.X;
                    owner._game.Zombies.Add(zombie);
                    owner._game.AdventurePresentationZombieAnimation(zombie);
                    zombie.PresentationAnimationAge = 2.299f;
                    Begin(animations);
                    draw.Invoke(presenter, new object[] { zombie });
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    zombie._presentationPreviousWalkCycle = .99d;
                    zombie._presentationWalkPhase = 0f;
                    zombie._presentationWalkLoops = 1;
                    land.Invoke(owner._game, new object[] { zombie, .01f });
                    Assert.That(zombie.PresentationNativePhase, Is.EqualTo(55));
                    Assert.That(zombie.PresentationPreviousX, Is.EqualTo(zombie.X));
                    zombie.Eating = eating;
                    var clip = owner._game.AdventurePresentationZombieAnimation(zombie);
                    var rate = owner._game.AdventurePresentationZombieRate(zombie);
                    Assert.That(clip, Is.EqualTo(eating ? "anim_eat" : "anim_swim"));
                    Assert.That(zombie.DolphinState!.AnimationBlendTicks, Is.EqualTo(eating ? 20 : 0));
                    var origin = new Vector2(AdventureZombieNativeRules.ReanimationSourceX(zombie.X), 262f);
                    var referenceKey = eating ? "independent-eating-landing" : "independent-swimming-landing";
                    Begin(reference);
                    // PC 541C70 invokes StartWalkAnim before EatPlant can call the twenty-tick StartEating blend.
                    reference.DrawNativePlayback("Zombie_dolphinrider", origin, 1f, 0f, "anim_swim", true, 12f,
                        instanceKey: referenceKey, blendTicks: 0);
                    reference.EndObjectFrame();
                    foreach (var seconds in new[] { 0f, .1f, .2f })
                    {
                        zombie.PresentationAnimationAge = seconds;
                        Begin(animations);
                        draw.Invoke(presenter, new object[] { zombie });
                        animations.EndObjectFrame();
                        var actual = Read(animations, target, pixels);
                        Save(pixels, "dolphin-cached-landing-" + eating + "-" + (int)(seconds * 100f) + ".png");
                        Begin(reference);
                        reference.DrawNativePlayback("Zombie_dolphinrider", origin, 1f, seconds, clip, true, rate,
                            instanceKey: referenceKey, blendTicks: eating ? 20 : 0);
                        reference.EndObjectFrame();
                        var expected = Read(reference, target, pixels);
                        Assert.That(DifferentPixels(actual, expected), Is.Zero,
                            "Landing must discard the old jump placement before " + clip + " at native tick " + (int)(seconds * 100f) + ".");
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

        /// <summary>Draws original tracks in XML order with literal PC water-track clipping exceptions.</summary>
        /// <param name="animations">The original source animation renderer.</param>
        /// <param name="tracks">The unchanged source track order.</param>
        /// <param name="clip">The original phase layer.</param>
        /// <param name="origin">The independently computed PC source origin.</param>
        /// <param name="seconds">The native clip clock.</param>
        /// <param name="rate">The original phase frame rate.</param>
        /// <param name="once">Whether the original phase plays once and holds.</param>
        /// <param name="fullLastFrame">Whether the original ride loop includes its complete final frame.</param>
        /// <param name="bottom">The independently calculated PC waterline or screen bottom.</param>
        /// <param name="dragged">Whether kelp restores water-track clipping.</param>
        private static void DrawReference(ReanimationRenderer animations, List<string> tracks, string clip, Vector2 origin,
            float seconds, float rate, bool once, bool fullLastFrame, float bottom, bool dragged)
        {
            foreach (var track in tracks)
            {
                var water = track == "Zombie_dolphinrider_whitewater" || track == "Zombie_dolphinrider_dolphininwater";
                SpriteDrawState.Clip = bottom >= 600f || water && !dragged
                    ? new Rect(0f, 0f, 800f, 600f)
                    : Rect.MinMaxRect(origin.x - 215f, origin.y - 70f, origin.x + 305f, bottom);
                // Marker layers and hidden source tracks intentionally submit no image in this phase.
                animations.DrawNativePlayback("Zombie_dolphinrider", origin, 1f, seconds, clip, !once, rate,
                    visible: name => name == track, fullLastFrame: fullLastFrame);
            }

            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
        }

        /// <summary>Retains the original alpha of water images identified directly from the source XML tracks.</summary>
        /// <param name="animations">The renderer with the original untinted production commands.</param>
        /// <param name="waterImages">The original images referenced by the two PC water tracks.</param>
        /// <returns>Source-image alpha values for the same authored ride frame.</returns>
        private static Dictionary<SpriteImage, float> WaterTrackAlpha(ReanimationRenderer animations, HashSet<SpriteImage> waterImages)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            var alpha = new Dictionary<SpriteImage, float>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage != null && waterImages.Contains(sprite.SourceImage))
                {
                    alpha[sprite.SourceImage] = sprite.Colors[0].a;
                }
            }

            Assert.That(alpha.Count, Is.GreaterThan(0));
            return alpha;
        }

        /// <summary>Checks PC water-track override exclusions while ordinary body cold and hit processing remains active.</summary>
        /// <param name="animations">The renderer with chilled and flashing production commands.</param>
        /// <param name="waterImages">The source XML's two water-track image sets.</param>
        /// <param name="originalAlpha">The original source alpha sampled before changing body feedback.</param>
        private static void AssertOriginalWaterColor(ReanimationRenderer animations, HashSet<SpriteImage> waterImages,
            Dictionary<SpriteImage, float> originalAlpha)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            var water = 0;
            var bodyFeedback = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage != null && waterImages.Contains(sprite.SourceImage))
                {
                    water++;
                    Assert.That(sprite.Colors[0], Is.EqualTo(new Color(1f, 1f, 1f, originalAlpha[sprite.SourceImage])));
                    Assert.That(sprite.AddColor, Is.EqualTo(Color.clear));
                }
                else if (sprite.Colors[0].b > sprite.Colors[0].r && sprite.AddColor.b > 0f)
                {
                    bodyFeedback++;
                }
            }

            Assert.That(water, Is.GreaterThan(0), "The unchanged original water artwork must still be submitted.");
            Assert.That(bodyFeedback, Is.GreaterThan(0), "Only the PC water-track flags exclude cold tint and hit addition.");
        }

        /// <summary>Begins a white-background frame in native board coordinates.</summary>
        /// <param name="animations">The renderer receiving the source commands.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Reads real GPU output from the submitted original sprite commands.</summary>
        /// <param name="animations">The renderer owning the sprite pool.</param>
        /// <param name="target">The native-resolution graphics target.</param>
        /// <param name="pixels">The reusable readable output texture.</param>
        /// <returns>The complete real graphics capture.</returns>
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

        /// <summary>Counts exact differing pixels between production and independently placed original source tracks.</summary>
        /// <param name="left">The production capture.</param>
        /// <param name="right">The original source reference capture.</param>
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

        /// <summary>Saves optional original-artwork diagnostics only into the explicitly selected test-output directory.</summary>
        /// <param name="pixels">The production capture.</param>
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
