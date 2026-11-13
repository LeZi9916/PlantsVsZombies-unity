#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises complete production board presentation with read-only original artwork and repeated wave-sized captures.</summary>
    public sealed class OriginalBoardStressTests
    {
        /// <summary>Checks real native foot dust remains visible and is covered by the following football body layer.</summary>
        /// <returns>Frame boundaries before comparing original-artwork dust and body captures.</returns>
        [UnityTest]
        public IEnumerator OriginalFootballFootDustDrawsBelowTheBody()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime asset directory.");
            }

            Assert.That(File.Exists(Path.Combine(root!, "particles", "Dust_Foot.xml")), Is.True);
            Assert.That(File.Exists(Path.Combine(root!, "images", "Dust_puffs.png")), Is.True);
            yield return null;
            var host = new GameObject("Inactive original football foot-dust fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root!);
            var presenter = new GamePresenter(owner);
            var options = new OptionsView(root!);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            var camera = ObjectSpriteRenderer.SceneCamera;
            var previousTarget = camera.targetTexture;
            try
            {
                owner._root = root!;
                owner._animations = animations;
                owner._optionsView = options;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._smokeMode = true;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._deck.Clear();
                var zombie = new Zombie
                {
                    Id = 999,
                    Kind = ZombieKind.Football,
                    Row = 1,
                    X = AdventureZombieNativeRules.GridX(300f),
                    Speed = AdventureZombieNativeRules.GridSpeed(.67f),
                    Health = 270f,
                    StartingHealth = 270f,
                    ArmorHealth = 1400f,
                    PresentationMaxArmor = 1400f,
                    PresentationAnimation = "anim_walk",
                    PresentationAnimationAge = .3f
                };
                var footstep = new AdventureZombieFootstepEffect { Id = 444, Row = 1, Seed = 1977, X = 385f, Y = 170f, Age = .05f };
                var frames = new Color32[4][];
                camera.targetTexture = target;
                for (var pass = 0; pass < frames.Length; pass++)
                {
                    owner._game.Zombies.Clear();
                    owner._game.Presentation.FootstepEffects.Clear();
                    if (pass >= 2)
                    {
                        owner._game.Zombies.Add(zombie);
                    }

                    if (pass == 1 || pass == 3)
                    {
                        owner._game.Presentation.FootstepEffects.Add(footstep);
                    }

                    presenter.Present();
                    yield return null;
                    yield return null;
                    Read(target, pixels);
                    frames[pass] = pixels.GetPixels32();
                    Save(pixels, "football-foot-dust-" + pass + ".png");
                }

                var visibleDust = 0;
                var bodyOverlap = 0;
                var coveredDust = 0;
                for (var index = 0; index < frames[0].Length; index++)
                {
                    if (Difference(frames[1][index], frames[0][index]) <= 12)
                    {
                        continue;
                    }

                    visibleDust++;
                    if (Difference(frames[2][index], frames[0][index]) <= 90)
                    {
                        continue;
                    }

                    bodyOverlap++;
                    if (Difference(frames[3][index], frames[2][index]) <= 3)
                    {
                        coveredDust++;
                    }
                }

                Assert.That(visibleDust, Is.GreaterThan(15), "The actual original Dust_Foot definition and atlas must produce visible pixels.");
                Assert.That(bodyOverlap, Is.GreaterThan(8), "The fixture must overlap the real football body with original dust pixels.");
                Assert.That(coveredDust, Is.GreaterThanOrEqualTo(Math.Max(8, bodyOverlap / 2)), "Opaque body pixels must cover the lower dust layer.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                presenter.Dispose();
                animations.Dispose();
                options.Dispose();
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(pixels);
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks actual original shadow quads and body placement against the native image helper's top-left semantics.</summary>
        /// <returns>Frame boundaries before exercising the real render pipeline requests.</returns>
        [UnityTest]
        public IEnumerator OriginalZombieShadowUsesNativeImageTopLeft()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime asset directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original shadow fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root!);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            var white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            try
            {
                owner._root = root!;
                owner._animations = animations;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var drawZombie = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var bodyOrigin = typeof(GamePresenter).GetMethod("AdventureZombieOrigin", flags)!;
                var cases = new List<(string Name, int Level, bool Small, bool Frozen, bool Chilled, float NativeX, ZombieKind Kind)>
                {
                    ("normal-day", 8, false, false, false, 200.25f, ZombieKind.Normal),
                    ("normal-night", 12, false, false, false, 200.25f, ZombieKind.Normal),
                    ("small-day", 8, true, false, false, 200.25f, ZombieKind.Normal),
                    ("frozen-night", 12, false, true, false, 200.25f, ZombieKind.Normal),
                    ("chilled-day", 8, false, false, true, 200.25f, ZombieKind.Normal),
                    ("moving-day", 8, false, false, false, 325.75f, ZombieKind.Normal),
                    ("pole-day", 8, false, false, false, 200.25f, ZombieKind.PoleVaulter),
                    ("football-day", 8, false, false, false, 200.25f, ZombieKind.Football),
                    ("small-football-day", 8, true, false, false, 200.25f, ZombieKind.Football),
                    ("paper-day", 8, false, false, false, 200.25f, ZombieKind.Newspaper),
                    ("imp-day", 8, false, false, false, 200.25f, ZombieKind.Imp),
                    ("gargantuar-day", 8, false, false, false, 200.25f, ZombieKind.Gargantuar),
                    ("landed-digger-day", 8, false, false, false, 200.25f, ZombieKind.Digger),
                    ("yeti-day", 8, false, false, false, 200.25f, ZombieKind.Yeti),
                    ("bobsled-day", 8, false, false, false, 200.25f, ZombieKind.Bobsled),
                    ("balloon-ground-day", 8, false, false, false, 200.25f, ZombieKind.Balloon),
                    ("pogo-ground-day", 8, false, false, false, 200.25f, ZombieKind.Pogo),
                    ("dancer-night", 12, false, false, false, 200.25f, ZombieKind.Dancing)
                };
                // These values combine the separately verified PC GetPosYBasedOnRow/GetDrawPos/DrawShadow chains.
                var species = new Dictionary<ZombieKind, (float BodyX, float BodyY, float ShadowX, float ShadowY, float ShadowScale)>
                {
                    { ZombieKind.Normal, (0f, 0f, 23f, 0f, 1f) },
                    { ZombieKind.PoleVaulter, (-6f, -11f, 30f, 0f, 1f) },
                    { ZombieKind.Football, (0f, -16f, 41f, 0f, 1f) },
                    { ZombieKind.Newspaper, (0f, 0f, 29f, 4f, 1f) },
                    { ZombieKind.Imp, (0f, 0f, 25f, 7f, .6f) },
                    { ZombieKind.Gargantuar, (0f, -8f, 27f, -1f, 1.5f) },
                    { ZombieKind.Digger, (0f, 0f, 17f, 5f, 1f) },
                    { ZombieKind.Yeti, (0f, -20f, 3f, 0f, 1f) },
                    { ZombieKind.Bobsled, (0f, -12f, 20f, 1f, 1f) },
                    { ZombieKind.Balloon, (0f, -13f, 23f, 0f, 1f) },
                    { ZombieKind.Pogo, (0f, 0f, 23f, 0f, 1f) },
                    { ZombieKind.Dancing, (0f, 0f, 23f, 0f, 1f) }
                };
                foreach (var item in cases)
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = item.Level }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    var scale = item.Small ? .5f : item.Kind == ZombieKind.Dancing ? .8f : 1f;
                    var zombie = new Zombie
                    {
                        Id = 1,
                        Kind = item.Kind,
                        Row = 1,
                        X = AdventureZombieNativeRules.GridX(item.NativeX, scale),
                        IsSmall = item.Small,
                        Health = 270f,
                        ArmorHealth = item.Kind == ZombieKind.Football ? 1400f : 0f,
                        PresentationMaxArmor = item.Kind == ZombieKind.Football ? 1400f : 0f,
                        ShieldHealth = item.Kind == ZombieKind.Newspaper ? 150f : 0f,
                        PresentationMaxShield = item.Kind == ZombieKind.Newspaper ? 150f : 0f,
                        UsedAbility = true,
                        AbilityPhase = item.Kind == ZombieKind.Digger ? 2 : 0,
                        PresentationWalkingBackwards = false,
                        FrozenRemaining = item.Frozen ? 1f : 0f,
                        ChillRemaining = item.Chilled ? 1f : 0f,
                        PresentationAnimationAge = .3f
                    };
                    owner._game.Zombies.Add(zombie);
                    ObjectSpriteRenderer.BeginSceneFrame();
                    animations.BeginObjectFrame();
                    animations.DrawNativeImage(white, new Rect(0, 0, Screen.width, Screen.height), new Rect(0, 0, 1, 1));
                    drawZombie.Invoke(presenter, new object[] { zombie });
                    animations.EndObjectFrame();
                    var image = owner.Textures.RequiredLoadImage(item.Level == 12 ? "plantshadow2.png" : "plantshadow.png");
                    Assert.That(image.Width, Is.EqualTo(86));
                    Assert.That(image.Height, Is.EqualTo(36));
                    var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
                    var sprites = pool.SubmittedSprites;
                    ObjectSprite? shadow = null;
                    for (var index = 0; index < pool.SubmittedObjectCount; index++)
                    {
                        if (ReferenceEquals(sprites[index].SourceImage, image))
                        {
                            shadow = sprites[index];
                            break;
                        }
                    }

                    Assert.That(shadow, Is.Not.Null, "The actual presenter must submit the original shadow artwork.");
                    var offsets = species[item.Kind];
                    var shadowScale = scale * offsets.ShadowScale;
                    var shadowX = item.Kind == ZombieKind.Football ? 20f + 21f * scale : offsets.ShadowX;
                    // 0x54FF00 supplies NativeX+20*s-20+speciesX and NativeY+speciesY+92 to 0x527D20.
                    // The helper first adds the UNSCALED 43/18 half dimensions, then scales around that center.
                    var expectedLeft = item.NativeX + 20f * scale - 20f + shadowX + 43f - 43f * shadowScale;
                    var expectedTop = 150f + offsets.ShadowY + 92f + 18f - 18f * shadowScale;
                    Assert.That(shadow!.Vertices[0].x, Is.EqualTo(expectedLeft).Within(.0001f), item.Name + " native shadow left");
                    Assert.That(-shadow.Vertices[0].y, Is.EqualTo(expectedTop).Within(.0001f), item.Name + " native shadow top");
                    Assert.That(shadow.Vertices[2].x - shadow.Vertices[0].x, Is.EqualTo(86f * shadowScale).Within(.0001f));
                    Assert.That(shadow.Vertices[0].y - shadow.Vertices[2].y, Is.EqualTo(36f * shadowScale).Within(.0001f));
                    var origin = (Vector2)bodyOrigin.Invoke(presenter, new object[] { zombie, zombie.X })!;
                    var footballCorrection = item.Kind == ZombieKind.Football ? 20f * (1f - scale) : 0f;
                    Assert.That(origin.x, Is.EqualTo(item.NativeX + offsets.BodyX + 15f + 30f * (1f - scale)).Within(.0001f), item.Name + " body X");
                    Assert.That(origin.y, Is.EqualTo(150f + offsets.BodyY - 8f + 120f * (1f - scale) + footballCorrection).Within(.0001f), item.Name + " body Y");
                    pool.RenderTo(target);
                    Read(target, pixels);
                    Save(pixels, "shadow-" + item.Name + ".png");
                }
            }
            finally
            {
                presenter.Dispose();
                animations.Dispose();
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(white);
                UnityEngine.Object.Destroy(pixels);
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks every planted cell after repeated capture churn without allowing simulation damage or sprite overlap.</summary>
        /// <returns>Frame boundaries allowing the actual scene camera and Canvas to render each submitted board.</returns>
        [UnityTest]
        public IEnumerator OriginalBoardKeepsPlantColumnsAcrossCaptureBursts()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime asset directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original board stress fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root!);
            var options = new OptionsView(root!);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            var camera = ObjectSpriteRenderer.SceneCamera;
            var previousTarget = camera.targetTexture;
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            try
            {
                owner._root = root!;
                owner._animations = animations;
                owner._optionsView = options;
                owner._font = font;
                owner._modeTitleBitmap = CanvasBitmapFont.LoadFromResources("HouseofTerror28");
                owner._currentView = PvZGame.ScreenView.Board;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(1000000, false, false);
                typeof(GameSimulation).GetProperty(nameof(GameSimulation.Elapsed))!.SetValue(owner._game, 100f);
                owner.WaveNotices._waveNoticeGame = owner._game;
                owner._deck.Clear();
                owner._deck.AddRange(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.SnowPea, PlantKind.Imitater });
                owner._game.Adventure.ImitaterTarget = PlantKind.Peashooter;
                var cooldowns = (int[])typeof(GameSimulation).GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner._game)!;
                Array.Fill(cooldowns, 500);
                presenter.SetPointerPosition(new Vector2(-1000f, -1000f));
                camera.targetTexture = target;
                presenter.Present();
                yield return null;
                Read(target, pixels);
                var background = pixels.GetPixels32();
                var kinds = new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Repeater, PlantKind.Wallnut, PlantKind.TallNut, PlantKind.SnowPea };
                for (var row = 0; row < 5; row++)
                {
                    for (var column = 0; column < 6; column++)
                    {
                        owner._game.Plants.Add(new Plant
                        {
                            Id = row * 6 + column + 1,
                            Kind = kinds[column],
                            Layer = PlantLayer.Main,
                            Row = row,
                            Column = column,
                            Health = 4000f,
                            Age = .5f,
                            PresentationRate = 12f,
                            PresentationAwakeAge = .5f
                        });
                    }
                }

                presenter.Present();
                yield return null;
                Read(target, pixels);
                var planted = pixels.GetPixels32();
                var masks = CreateMasks(background, planted, pixels.width, pixels.height);
                Save(pixels, "00-planted.png");
                for (var frame = 0; frame < 24; frame++)
                {
                    owner.WaveNotices._hugeWaveNoticeStart = 100f - frame * .08f;
                    owner.WaveNotices._finalWaveNoticeStart = 100f - frame * .08f;
                    owner._game.Zombies.Clear();
                    for (var index = 0; index < 100; index++)
                    {
                        owner._game.Zombies.Add(new Zombie
                        {
                            Id = 1000 + frame * 100 + index,
                            Kind = index == 1 ? ZombieKind.Flag : index % 3 == 0 ? ZombieKind.PoleVaulter : index % 3 == 1 ? ZombieKind.Buckethead : ZombieKind.Normal,
                            Row = index % 5,
                            X = 11f + index * .005f,
                            Health = 270f,
                            ArmorHealth = index % 3 == 1 ? 1100f : 0f,
                            PresentationMaxArmor = index % 3 == 1 ? 1100f : 0f,
                            PresentationFlash = frame % 3 == 1 ? 0f : .05f,
                            PresentationArmorFlash = frame % 3 == 1 ? 0f : .05f,
                            UsedAbility = true
                        });
                    }

                    foreach (var plant in owner._game.Plants)
                    {
                        plant.PresentationEatenFlashTicks = frame % 3 == 0 ? 30 : 0;
                    }

                    presenter.Present();
                    yield return null;
                    Assert.That(animations.NativeCaptureRendererCount, Is.LessThanOrEqualTo(2),
                        "Sequential actor captures must share cameras and meshes; only nested card capture needs a second pool.");
                    Read(target, pixels);
                    var current = pixels.GetPixels32();
                    for (var cell = 0; cell < masks.Count; cell++)
                    {
                        var visible = 0;
                        foreach (var pixel in masks[cell])
                        {
                            if (Difference(current[pixel], background[pixel]) > 40)
                            {
                                visible++;
                            }
                        }

                        Assert.That(visible, Is.GreaterThan(masks[cell].Count / 4),
                            "Plant vanished at row " + cell / 6 + ", column " + cell % 6 + ", frame " + frame + ".");
                    }

                    if (frame % 6 == 0 || frame == 23)
                    {
                        Save(pixels, "stress-" + frame + ".png");
                        Debug.Log("Original board stress frame " + frame + ": plants=" + owner._game.Plants.Count
                            + ", capture textures=" + animations.NativeSurfaceCount + ", created=" + animations.NativeSurfaceCreationCount
                            + ", capture renderer pools=" + animations.NativeCaptureRendererCount);
                    }
                }

                Assert.That(owner._game.Plants.Count, Is.EqualTo(30));
            }
            finally
            {
                camera.targetTexture = previousTarget;
                presenter.Dispose();
                options.Dispose();
                animations.Dispose();
                owner.Textures.ReleaseAll();
                owner._modeTitleBitmap?.Dispose();
                owner.WaveNotices._bundledWaveAnimations?.Dispose();
                UnityEngine.Object.Destroy(font);
                UnityEngine.Object.Destroy(pixels);
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Builds one original-artwork difference mask per planted cell.</summary>
        /// <param name="background">The board before planting.</param>
        /// <param name="planted">The same board with static plant artwork.</param>
        /// <param name="width">The captured pixel width.</param>
        /// <param name="height">The captured pixel height.</param>
        /// <returns>The non-background plant pixels grouped by row and column.</returns>
        private static List<List<int>> CreateMasks(Color32[] background, Color32[] planted, int width, int height)
        {
            var masks = new List<List<int>>();
            var scale = Math.Min(width / 800f, height / 600f);
            var offsetX = (width - 800f * scale) * .5f;
            var offsetY = (height - 600f * scale) * .5f;
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 6; column++)
                {
                    var mask = new List<int>();
                    var left = (int)(offsetX + (40f + column * 80f) * scale);
                    var top = (int)(offsetY + (65f + row * 100f) * scale);
                    var right = (int)(left + 80f * scale);
                    var bottom = (int)(top + 95f * scale);
                    for (var y = top; y < bottom; y++)
                    {
                        for (var x = left; x < right; x++)
                        {
                            var pixel = (height - y - 1) * width + x;
                            if (Difference(planted[pixel], background[pixel]) > 70)
                            {
                                mask.Add(pixel);
                            }
                        }
                    }

                    Assert.That(mask.Count, Is.GreaterThan(10), "The original fixture plant must have visible artwork.");
                    masks.Add(mask);
                }
            }

            return masks;
        }

        /// <summary>Measures the channel difference used to distinguish a plant from its original lawn background.</summary>
        /// <param name="first">The current color.</param>
        /// <param name="second">The reference background color.</param>
        /// <returns>The sum of absolute RGB channel differences.</returns>
        private static int Difference(Color32 first, Color32 second)
        {
            return Math.Abs(first.r - second.r) + Math.Abs(first.g - second.g) + Math.Abs(first.b - second.b);
        }

        /// <summary>Reads the actual scene camera target without changing the active capture surface.</summary>
        /// <param name="target">The complete scene target.</param>
        /// <param name="pixels">The reusable readable pixel buffer.</param>
        private static void Read(RenderTexture target, Texture2D pixels)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Writes optional diagnostic screenshots into the explicitly selected ignored output directory.</summary>
        /// <param name="pixels">The complete original board capture.</param>
        /// <param name="name">The current diagnostic frame name.</param>
        private static void Save(Texture2D pixels, string name)
        {
            var output = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, name), pixels.EncodeToPNG());
            }
        }
    }
}
