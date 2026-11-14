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
    /// <summary>Checks the original Pogo zombie's bounce, high jump, and disarmed eating artwork.</summary>
    public sealed class OriginalPogoGraphicsTests
    {
        /// <summary>Checks three original PC poses through the production zombie presenter against unchanged source artwork.</summary>
        /// <returns>A frame boundary for original artwork and real graphics initialization.</returns>
        [UnityTest]
        public IEnumerator BounceHighJumpAndDisarmedEatingMatchOriginalFramesAndHeight()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Pogo graphics fixture");
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
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 1 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!;
                // PC 540F10: 80-update bounce, 9-pixel deflection, heights40/90 for phases20/27.
                // At counter5 the normal altitude is18.375; at counter40 phase27 reaches99.
                // Counter7 restarts the eleven-frame anim_pogo at40 FPS; its other poses hold frame165.
                var samples = new[]
                {
                    (Phase: 20, Counter: 5, Altitude: 18.375f, Seconds: .02f, Eating: false),
                    (Phase: 27, Counter: 40, Altitude: 99f, Seconds: 11f / 40f, Eating: false),
                    (Phase: 0, Counter: 0, Altitude: 0f, Seconds: .25f, Eating: true)
                };
                SpriteDrawState.Matrix = Matrix4x4.identity;
                SpriteDrawState.Color = Color.white;
                var id = 81;
                foreach (var sample in samples)
                {
                    var animation = sample.Eating ? "anim_eat" : "anim_pogo";
                    var rate = sample.Eating ? 36f : 40f;
                    var zombie = new Zombie
                    {
                        Id = id++, Kind = ZombieKind.Pogo, Row = 2, X = 4f, PresentationPreviousX = 4f,
                        Health = 500f, StartingHealth = 500f, PresentationNativePhase = sample.Phase,
                        PresentationNativeHeight = 0, PresentationInPool = false, PresentationAltitude = sample.Altitude,
                        UsedAbility = sample.Eating, Eating = sample.Eating, PresentationAnimation = animation,
                        // The native update publishes the retained Pogo clock before presentation.
                        PresentationAnimationAge = sample.Seconds,
                        PogoState = new AdventurePogoState
                        {
                            PhaseTicks = sample.Counter, AnimationSeconds = sample.Seconds,
                            AnimationBlendTicks = sample.Eating ? 20 : 0
                        }
                    };
                    Assert.That(owner._game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo(animation));
                    Assert.That(owner._game.AdventurePresentationZombieRate(zombie), Is.EqualTo(rate));
                    BeginFrame(animations);
                    draw.Invoke(presenter, new object[] { zombie });
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var actual = pixels.GetPixels32();
                    Assert.That(Array.Exists(actual, pixel => pixel.r < 240 || pixel.g < 240 || pixel.b < 240), Is.True,
                        "Each original Pogo pose must submit visible source artwork.");
                    BeginFrame(animations);
                    // PC 547FF0/549740: nativeX+15, rowFloor-38-altitude; loss of the pole restores a standing body.
                    Assert.That(animations.DrawNativePlayback("Zombie_pogo", new Vector2(303f, 242f - sample.Altitude), 1f,
                        sample.Seconds, animation, sample.Eating, rate), Is.True);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var expected = pixels.GetPixels32();
                    SaveComparison(actual, expected, sample.Phase);
                    var origin = (Vector2)typeof(GamePresenter)
                        .GetMethod("AdventureZombieOrigin", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(presenter, new object[] { zombie, zombie.X })!;
                    Assert.That(actual, Is.EqualTo(expected),
                        $"Pogo phase {sample.Phase}, counter {sample.Counter}, animation {animation}, rate {rate}, origin {origin}, altitude {sample.Altitude}, seconds {sample.Seconds}: "
                        + "original held/restarted bounce frames, body height, and disarmed eating must match.");
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

        /// <summary>Checks the real street preview keeps bouncing while the seed chooser waits, without board movement or bounce sounds.</summary>
        /// <returns>One initialization frame for the original artwork renderer.</returns>
        [UnityTest]
        public IEnumerator StreetPreviewKeepsOriginalPogoBounceWhileChoosingSeeds()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Pogo street fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            using var options = new OptionsView(root);
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
                owner._optionsView = options;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 43 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._currentView = PvZGame.ScreenView.Board;
                var context = new AdventureIntroContext { Level = 43, NativeBackground = 4 };
                var clock = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
                for (var tick = 0; tick < 1000 && !clock.WaitingForSeeds; tick++)
                {
                    clock.Tick();
                }

                Assert.That(clock.WaitingForSeeds, Is.True);
                owner.Opening._adventureIntro = clock;
                owner.Opening._adventureIntroFrame = clock.Frame;
                owner.Opening._adventureIntroDrawn = true;
                owner.Opening._adventureIntroEventHandler = _ =>
                {
                };
                owner._choosingSeeds = true;
                var preview = AdventureIntroPresentation.PlaceStreetZombie(context, (int)ZombieKind.Pogo, 0, 1,
                    bound => bound == 80 ? 79 : 0, _ => 0f,
                    (nativeKind, clip) => animations.TryGetNativeAnimationInfo("Zombie_pogo", clip, out _));
                owner.Opening._adventurePreview.Add(preview);
                var nativeTime = clock.NativeTime;
                var soundCount = owner._game.Presentation.ZombieSounds.Count;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureOpeningActors", BindingFlags.Instance | BindingFlags.NonPublic)!;
                SpriteDrawState.Color = Color.white;
                BeginFrame(animations);
                SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-600f, 0f, 0f));
                draw.Invoke(presenter, null);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var first = pixels.GetPixels32();
                owner.Opening.UpdateAdventureOpening(.4f);
                BeginFrame(animations);
                SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-600f, 0f, 0f));
                draw.Invoke(presenter, null);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var peak = pixels.GetPixels32();
                var changedPixels = 0;
                for (var index = 0; index < peak.Length; index++)
                {
                    if (!peak[index].Equals(first[index]))
                    {
                        changedPixels++;
                    }
                }

                Assert.That(changedPixels, Is.GreaterThan(20), "The real seed-chooser preview must keep bouncing rather than holding anim_pogo forever.");
                Assert.That(clock.NativeTime, Is.EqualTo(nativeTime), "The chooser remains stalled while the original preview zombie keeps updating.");
                Assert.That(preview.WorldX, Is.EqualTo(825f), "Street Pogo updates must not apply combat walking.");
                Assert.That(owner._game.Presentation.ZombieSounds.Count, Is.EqualTo(soundCount), "Original UI zombies do not request the on-board Pogo Foley.");
                // PC 53DD90 starts counter80 at the held frame; 546B40 still calls 540F10 for wave -2/-3.
                // Forty updates later the original altitude is49, and the eleven-frame clip still holds its end.
                BeginFrame(animations);
                SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-600f, 0f, 0f));
                Assert.That(animations.DrawNativePlayback("Zombie_pogo", new Vector2(preview.WorldX, preview.WorldY - 49f),
                    preview.Scale, 11f / 40f, "anim_pogo", false, 40f), Is.True);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(peak, Is.EqualTo(pixels.GetPixels32()), "The street bounce must use the original peak and held pose.");
                owner.Opening.UpdateAdventureOpening(.35f);
                BeginFrame(animations);
                SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-600f, 0f, 0f));
                draw.Invoke(presenter, null);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var landing = pixels.GetPixels32();
                BeginFrame(animations);
                SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-600f, 0f, 0f));
                Assert.That(animations.DrawNativePlayback("Zombie_pogo", new Vector2(preview.WorldX, preview.WorldY - 18.375f),
                    preview.Scale, .02f, "anim_pogo", false, 40f), Is.True);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(landing, Is.EqualTo(pixels.GetPixels32()), "Counter7 restarts the original body clip, so counter5 samples two updates of forty-FPS animation.");
                var altitude = preview.Altitude;
                owner._game.TogglePause();
                owner.Opening.UpdateAdventureOpening(.5f);
                Assert.That(preview.Altitude, Is.EqualTo(altitude));
                owner._game.TogglePause();
                owner.Opening.UpdateAdventureOpening(.8f);
                Assert.That(preview.Altitude, Is.EqualTo(altitude).Within(.0001f), "An eighty-update UI bounce repeats without walking or plant targeting.");
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

        /// <summary>Begins an original sprite frame and isolates body artwork above the unrelated ground shadow.</summary>
        /// <param name="animations">The original artwork renderer receiving the frame.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 330f);
        }

        /// <summary>Saves optional original-artwork comparisons to the operator's ignored evidence directory.</summary>
        /// <param name="actual">The production presenter's complete rendered frame.</param>
        /// <param name="expected">The independent original-reanimation frame.</param>
        /// <param name="phase">The native Pogo phase identifying this sample.</param>
        private static void SaveComparison(Color32[] actual, Color32[] expected, int phase)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            directory = Path.Combine(directory, "pogo-graphics");
            Directory.CreateDirectory(directory);
            var image = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels32(actual);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, $"phase-{phase}-actual.png"), image.EncodeToPNG());
                image.SetPixels32(expected);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, $"phase-{phase}-reference.png"), image.EncodeToPNG());
                var difference = new Color32[actual.Length];
                for (var index = 0; index < difference.Length; index++)
                {
                    var delta = Math.Max(Math.Abs(actual[index].r - expected[index].r), Math.Abs(actual[index].g - expected[index].g));
                    delta = Math.Max(delta, Math.Abs(actual[index].b - expected[index].b));
                    delta = Math.Max(delta, Math.Abs(actual[index].a - expected[index].a));
                    var channel = (byte)Math.Min(255, delta * 8);
                    difference[index] = new Color32(channel, 0, channel, 255);
                }

                image.SetPixels32(difference);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, $"phase-{phase}-difference.png"), image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(image);
            }
        }

        /// <summary>Reads submitted original sprites from the real graphics device.</summary>
        /// <param name="animations">The renderer owning the submitted sprite batch.</param>
        /// <param name="target">The fixture's uniquely owned render target.</param>
        /// <param name="pixels">The reusable readable output image.</param>
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
