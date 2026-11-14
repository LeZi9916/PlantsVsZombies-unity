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
    /// <summary>Checks native storm tile geometry, real rain reanimations and brightness ordering through the production presenter.</summary>
    public sealed class OriginalStormGraphicsTests
    {
        /// <summary>Compares original rain artwork against native PC tile rectangles, including the street-preview camera pan.</summary>
        /// <returns>Actual camera frame and texture-readback boundaries.</returns>
        [UnityTest]
        public IEnumerator RainUsesOriginalTilesAndOriginalOneShotArtwork()
        {
            using var fixture = new StormFixture();
            var game = fixture.Owner.RequiredGame;
            var context = new AdventureIntroContext { Level = 40, NativeBackground = 3, IsConveyor = true, IsStormyNight = true };
            var intro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            fixture.Owner._currentView = PvZGame.ScreenView.Board;
            fixture.Owner.Dave._crazyDaveMessageIndex = -1;
            fixture.Owner.Opening._adventureIntro = intro;
            fixture.Owner.Opening._adventureIntroFrame = intro.Frame;
            fixture.Owner.Opening._adventureIntroEventHandler = fixture.Owner.Opening.HandleAdventureIntroEvent;
            game.BeginAdventureStormIntro();
            fixture.Owner.Opening.UpdateAdventureOpening(.5f);
            Assert.That(game.Adventure.Storm.EffectTicks, Is.Zero, "Rain cannot advance before the introduction has been presented.");
            fixture.Owner.Opening._adventureIntroDrawn = true;
            for (var tick = 0; tick < 37; tick++)
            {
                fixture.Owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(game.Adventure.Storm.Countdown, Is.EqualTo(113));
            Assert.That(game.Adventure.Storm.MainTicks, Is.Zero);
            Assert.That(game.Adventure.Storm.EffectTicks, Is.EqualTo(37));
            game.TogglePause();
            fixture.Owner.Opening.UpdateAdventureOpening(.05f);
            Assert.That(game.Adventure.Storm.Countdown, Is.EqualTo(108));
            Assert.That(game.Adventure.Storm.EffectTicks, Is.EqualTo(37));
            game.TogglePause();
            while (intro.NativeTime < 4250)
            {
                intro.Tick();
            }

            fixture.Owner.Opening._adventureIntroFrame = intro.Frame;
            var boardX = intro.Frame.BoardX;
            Assert.That(boardX, Is.LessThan(0));
            yield return fixture.Capture("DrawAdventureStormRain");
            var actual = fixture.Pixels.GetPixels32();
            var rainTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                Assert.That(ImageConversion.LoadImage(rainTexture, File.ReadAllBytes(Path.Combine(fixture.Root, "particles", "Rain.png"))), Is.True);
                Assert.That(rainTexture.width, Is.EqualTo(100));
                yield return fixture.Capture(null, () =>
                {
                    // PC 0x431E20: integer offsets and center-preserving scale, independently expressed as image rectangles.
                    var alignment = (boardX > 0 ? (boardX + 100) / 100 : boardX / 100) * -100;
                    for (var layer = 0; layer < 2; layer++)
                    {
                        var scale = layer == 0 ? 1f : 1.5f;
                        var x = (int)Math.Round(-100f * (37 % (layer == 0 ? 100 : 161)) / (layer == 0 ? 100 : 161), MidpointRounding.AwayFromZero);
                        var y = (int)Math.Round(-100f + 100f * (37 % (layer == 0 ? 20 : 33)) / (layer == 0 ? 20 : 33), MidpointRounding.AwayFromZero);
                        for (var column = 0; column < 9; column++)
                        {
                            for (var row = 0; row < 7; row++)
                            {
                                fixture.Animations.DrawNativeImage(rainTexture,
                                    new Rect((x + column * 100) * scale + alignment - (scale - 1f) * 50f,
                                        (y + row * 100) * scale - (scale - 1f) * 50f, 100f * scale, 100f * scale), new Rect(0f, 0f, 1f, 1f));
                            }
                        }
                    }
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Rain must use native PC rectangles at both scales and the original camera alignment.");
            }
            finally
            {
                UnityEngine.Object.Destroy(rainTexture);
            }

            game.AdvanceAdventureStormTick(true);
            yield return fixture.Capture("DrawAdventureStormRain");
            Assert.That(Changed(actual, fixture.Pixels.GetPixels32()), Is.GreaterThan(100), "Rain must scroll with the independent native effect counter.");
            fixture.Owner.Opening._adventureIntro = null;
            fixture.Owner.Opening._adventureIntroFrame = null;
            Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Rain_circle", null, out var circle), Is.True);
            Assert.That(circle.Count, Is.EqualTo(19));
            Assert.That(circle.SourceRate, Is.EqualTo(12f));
            Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Rain_splash", null, out var splash), Is.True);
            Assert.That(splash.Count, Is.EqualTo(8));
            Assert.That(splash.SourceRate, Is.EqualTo(12f));
            yield return fixture.Capture(null);
            var blank = fixture.Pixels.GetPixels32();
            yield return fixture.Capture("DrawAdventureRainSplashes");
            var drops = fixture.Pixels.GetPixels32();
            Assert.That(Changed(blank, drops), Is.GreaterThan(50), "The original water circles and ground splashes must produce actual visible artwork.");
            for (var tick = 0; tick < 10; tick++)
            {
                game.AdvanceAdventureStormTick(true);
            }

            yield return fixture.Capture("DrawAdventureRainSplashes");
            Assert.That(Changed(drops, fixture.Pixels.GetPixels32()), Is.GreaterThan(50), "The original rain reanimations must advance rather than remain static.");
            fixture.Owner.Opening.HandleAdventureIntroEvent(AdventureIntroEvent.IntroFinished);
            Assert.That(game.Adventure.Storm.Phase, Is.EqualTo(5));
            Assert.That(game.Adventure.Storm.Countdown, Is.EqualTo(400));
        }

        /// <summary>Checks full black intervals, bright native UI/pickups and preview zombies beneath the storm masks.</summary>
        /// <returns>Actual production-board camera frame boundaries.</returns>
        [UnityTest]
        public IEnumerator StormDarkensTheWorldAndRetainsOriginalForegroundLayers()
        {
            using var fixture = new StormFixture();
            var game = fixture.Owner.RequiredGame;
            game.Mowers.Clear();
            fixture.Owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
            game.RequiredSpawnAdventureCoin(AdventureCoinKind.Gold, 4.5f, 2);
            game.Adventure.Storm.Countdown = 300;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var dark = fixture.Pixels.GetPixels32();
            game.Adventure.Storm.Countdown = 299;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var light = fixture.Pixels.GetPixels32();
            var blackWorld = 0;
            var preservedUi = 0;
            var preservedCoin = 0;
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var marginX = (Screen.width - 800f * scale) * .5f;
            var marginY = (Screen.height - 600f * scale) * .5f;
            for (var index = 0; index < dark.Length; index++)
            {
                var x = (index % fixture.Target.width - marginX) / scale;
                var y = (fixture.Target.height - 1 - index / fixture.Target.width - marginY) / scale;
                if (x > 150f && x < 600f && y > 140f && y < 250f && dark[index].r == 0 && dark[index].g == 0 && dark[index].b == 0)
                {
                    blackWorld++;
                }

                if (Difference(dark[index], light[index]) <= 3 && dark[index].r + dark[index].g + dark[index].b > 100)
                {
                    if (y > 5f && y < 70f && x > 50f && x < 500f)
                    {
                        preservedUi++;
                    }
                    else if (y > 250f && y < 380f && x > 330f && x < 510f)
                    {
                        preservedCoin++;
                    }
                }
            }

            Assert.That(blackWorld, Is.GreaterThan((int)(40000f * scale * scale)), "Native non-flash intervals are opaque black rather than a blue translucent tint.");
            Assert.That(Changed(dark, light), Is.GreaterThan((int)(100000f * scale * scale)));
            Assert.That(preservedUi, Is.GreaterThan((int)(1000f * scale * scale)), "The conveyor and shovel must be redrawn at normal brightness.");
            Assert.That(preservedCoin, Is.GreaterThan((int)(30f * scale * scale)), "Native pickups are above Weather layer 500003.");

            var context = new AdventureIntroContext { Level = 40, NativeBackground = 3, IsConveyor = true, IsStormyNight = true };
            var intro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            for (var tick = 0; tick < 425; tick++)
            {
                intro.Tick();
            }

            fixture.Owner.Opening._adventureIntro = intro;
            fixture.Owner.Opening._adventureIntroFrame = intro.Frame;
            fixture.Owner.Opening._adventureIntroDrawn = true;
            game.Adventure.Storm.Countdown = 300;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var empty = fixture.Pixels.GetPixels32();
            fixture.Owner.Opening._adventurePreview.Add(AdventureIntroPresentation.PlaceStreetZombie(context,
                (int)ZombieKind.Normal, 4, 3, maximum => 0, maximum => 0f, (kind, track) => track == "anim_idle"));
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(empty), "Street-preview zombies must also be hidden by the original opaque storm interval.");
            game.Adventure.Storm.Countdown = 225;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var preview = fixture.Pixels.GetPixels32();
            fixture.Owner.Opening._adventurePreview.Clear();
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            Assert.That(Changed(preview, fixture.Pixels.GetPixels32()), Is.GreaterThan(50), "Street-preview actors become visible during the original flash window.");
        }

        /// <summary>Counts visible RGB changes without printing complete camera buffers on failure.</summary>
        /// <param name="first">The reference pixels.</param>
        /// <param name="second">The pixels being compared.</param>
        /// <returns>The number of visibly different pixels.</returns>
        private static int Changed(Color32[] first, Color32[] second)
        {
            var count = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Difference(first[index], second[index]) > 12)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Measures visible RGB distance.</summary>
        /// <param name="first">The reference pixel.</param>
        /// <param name="second">The pixel being compared.</param>
        /// <returns>The summed channel distance.</returns>
        private static int Difference(Color32 first, Color32 second)
        {
            return Math.Abs(first.r - second.r) + Math.Abs(first.g - second.g) + Math.Abs(first.b - second.b);
        }

        /// <summary>Owns an inactive production host and a real graphics target without touching saves or the main scene.</summary>
        private sealed class StormFixture : IDisposable
        {
            /// <summary>Gets the read-only runtime resource directory.</summary>
            internal string Root { get; }
            /// <summary>Gets the inactive game host.</summary>
            internal PvZGame Owner { get; }
            /// <summary>Gets the original animation renderer.</summary>
            internal ReanimationRenderer Animations { get; }
            /// <summary>Gets the real scene camera target.</summary>
            internal RenderTexture Target { get; }
            /// <summary>Gets the reusable camera readback image.</summary>
            internal Texture2D Pixels { get; }
            /// <summary>Owns the complete production board presenter.</summary>
            private readonly GamePresenter _presenter;
            /// <summary>Owns the options state required by board visibility guards.</summary>
            private readonly OptionsView _options;

            /// <summary>Creates an original 4-10 board using only supplied runtime resources.</summary>
            internal StormFixture()
            {
                Root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
                if (!File.Exists(Path.Combine(Root, "particles", "Rain.png")))
                {
                    Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
                }

                var host = new GameObject("Inactive original storm graphics fixture");
                host.SetActive(false);
                Owner = host.AddComponent<PvZGame>();
                Animations = new ReanimationRenderer(Root);
                _presenter = new GamePresenter(Owner);
                _options = new OptionsView(Root);
                Owner._root = Root;
                Owner._animations = Animations;
                Owner._optionsView = _options;
                Owner._game = new GameSimulation(new GameRules { AdventureLevel = 40 }, 91);
                typeof(GamePresenter).GetField("_renderingBoardObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_presenter, true);
                Target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                Target.Create();
                Pixels = new Texture2D(Target.width, Target.height, TextureFormat.RGBA32, false);
                ObjectSpriteRenderer.SceneCamera.targetTexture = Target;
            }

            /// <summary>Captures a production pass or an independently submitted native geometry oracle.</summary>
            /// <param name="method">The production presenter pass, or null for an oracle-only capture.</param>
            /// <param name="oracle">The optional independent native geometry submission.</param>
            /// <param name="wholeBoard">Whether the production method manages its own board presentation scope.</param>
            /// <returns>Camera frame boundaries before reading actual rendered pixels.</returns>
            internal IEnumerator Capture(string? method, Action? oracle = null, bool wholeBoard = false)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                CanvasUI.BeginFrame();
                var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
                CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                    Quaternion.identity, new Vector3(scale, scale, 1f));
                CanvasUI.Color = Color.white;
                Animations.BeginObjectFrame();
                Animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                if (!wholeBoard)
                {
                    if (Owner.Opening.AdventureIntroActive)
                    {
                        CanvasUI.Matrix *= Matrix4x4.Translate(new Vector3(Owner.Opening.RequiredAdventureIntroFrame.BoardX, Owner.Opening.RequiredAdventureIntroFrame.BoardY, 0f));
                    }

                    typeof(GamePresenter).GetMethod("BeginAdventurePresentation", flags)!.Invoke(_presenter, null);
                }

                if (method != null)
                {
                    typeof(GamePresenter).GetMethod(method, flags)!.Invoke(_presenter, null);
                }

                oracle?.Invoke();
                if (!wholeBoard)
                {
                    typeof(GamePresenter).GetMethod("EndAdventurePresentation", flags)!.Invoke(_presenter, null);
                }

                Animations.EndObjectFrame();
                CanvasUI.EndFrame();
                yield return null;
                yield return null;
                var previous = RenderTexture.active;
                RenderTexture.active = Target;
                Pixels.ReadPixels(new Rect(0f, 0f, Target.width, Target.height), 0, 0);
                Pixels.Apply();
                RenderTexture.active = previous;
            }

            /// <summary>Releases fixture-owned graphics without saving the inactive game.</summary>
            public void Dispose()
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = null;
                _presenter.Dispose();
                Animations.Dispose();
                _options.Dispose();
                Owner.Textures.ReleaseAll();
                Target.Release();
                UnityEngine.Object.Destroy(Target);
                UnityEngine.Object.Destroy(Pixels);
                UnityEngine.Object.Destroy(Owner.gameObject);
            }
        }
    }
}
