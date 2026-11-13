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
    /// <summary>Checks original damage images, real particle source artwork, and production vehicle GPU output.</summary>
    public sealed class OriginalZomboniTests
    {
        /// <summary>Checks production vehicle rendering uses original damage panels and original engine smoke, including its wheelie attachment.</summary>
        /// <returns>A frame boundary for the real graphics device.</returns>
        [UnityTest]
        public IEnumerator DamagePanelsAndSmokeUseOriginalArtworkInProductionPresenter()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original ice vehicle graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var particles = new OriginalParticleRenderer(root, animations);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                typeof(GamePresenter).GetField("_adventureParticles", flags)!.SetValue(presenter, particles);
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var zombie = new Zombie
                {
                    Id = 91, Kind = ZombieKind.Zomboni, Row = 1, X = 4f, StartingHealth = 1350f,
                    PresentationInPool = false, PresentationAnimation = "anim_drive", PresentationAnimationAge = .3f,
                    ZomboniState = new AdventureZomboniState()
                };
                owner._game.Zombies.Add(zombie);
                Color32[]? previous = null;
                foreach (var health in new[] { 1350f, 899f, 449f })
                {
                    zombie.Health = health;
                    Begin(animations, particles);
                    draw.Invoke(presenter, new object[] { zombie });
                    End(animations, particles);
                    var output = Read(animations, target, pixels);
                    var stage = health == 1350f ? "" : health == 899f ? "_damage1" : "_damage2";
                    AssertSource(animations, "Zombie_zamboni_1" + stage + ".png");
                    AssertSource(animations, "Zombie_zamboni_2" + stage + ".png");
                    if (previous != null)
                    {
                        Assert.That(Differences(output, previous), Is.GreaterThan(100), "An original damage stage must change visible vehicle pixels.");
                    }

                    previous = output;
                    Save(pixels, "zamboni-health-" + health + ".png");
                }

                zombie.Age = 1f;
                zombie.ZomboniState!.SmokeStartAge = 0f;
                Begin(animations, particles);
                draw.Invoke(presenter, new object[] { zombie });
                End(animations, particles);
                AssertSource(animations, "ZamboniSmoke.png");
                var smoke = Read(animations, target, pixels);
                Assert.That(Differences(smoke, previous!), Is.GreaterThan(20));
                Save(pixels, "zamboni-original-engine-smoke.png");

                particles.Clear();
                zombie.ZomboniState.SmokeStartAge = -1f;
                zombie.ZomboniState.FlatTires = true;
                zombie.ZomboniState.DeathTicks = 210;
                zombie.PresentationNativePhase = 1;
                zombie.PresentationAnimation = "anim_wheelie1";
                zombie.PresentationAnimationAge = .7f;
                Begin(animations, particles);
                draw.Invoke(presenter, new object[] { zombie });
                End(animations, particles);
                AssertSource(animations, "ZamboniSmoke.png");
                AssertNoSource(animations, "Zombie_seaweed");
                Read(animations, target, pixels);
                Save(pixels, "zamboni-original-wheelie-smoke.png");

                particles.Clear();
                owner._game.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle
                {
                    Id = 99, Row = 1, Seed = 91, X = 368f, Y = 210f, Age = .3f, Particle = "ZamboniExplosion"
                });
                Begin(animations, particles);
                typeof(GamePresenter).GetMethod("DrawAdventureExplosionRow", flags)!.Invoke(presenter, new object[] { 1 });
                End(animations, particles);
                AssertSource(animations, "Zombie_zamboni_1_damage1.png");
                var explosion = Read(animations, target, pixels);
                Assert.That(Array.Exists(explosion, pixel => pixel.r < 240 || pixel.g < 240 || pixel.b < 240), Is.True);
                Save(pixels, "zamboni-original-explosion.png");
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

        /// <summary>Starts an original native-coordinate object and particle frame over white.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <param name="particles">The retained original particle systems.</param>
        private static void Begin(ReanimationRenderer animations, OriginalParticleRenderer particles)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            particles.BeginFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Completes a submitted production frame.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <param name="particles">The retained original particle systems.</param>
        private static void End(ReanimationRenderer animations, OriginalParticleRenderer particles)
        {
            particles.EndFrame();
            animations.EndObjectFrame();
        }

        /// <summary>Checks the actual submitted source command uses one specific unchanged original resource.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <param name="filename">The required original image filename.</param>
        private static void AssertSource(ReanimationRenderer animations, string filename)
        {
            var pool = Pool(animations);
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (pool.SubmittedSprites[index].SourceImage?.SourceIdentity.EndsWith(filename, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return;
                }
            }

            Assert.Fail("The production frame did not submit original " + filename + ".");
        }

        /// <summary>Rejects unrelated artwork in a native vehicle particle attachment.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <param name="name">The unrelated image name fragment.</param>
        private static void AssertNoSource(ReanimationRenderer animations, string name)
        {
            var pool = Pool(animations);
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                Assert.That(pool.SubmittedSprites[index].SourceImage?.SourceIdentity.IndexOf(name, StringComparison.OrdinalIgnoreCase) ?? -1,
                    Is.LessThan(0), "The wheelie requires ZamboniSmoke instead of another attachment effect.");
            }
        }

        /// <summary>Gets the production object renderer retaining original source command metadata.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <returns>The actual submitted object renderer.</returns>
        private static ObjectSpriteRenderer Pool(ReanimationRenderer animations)
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
        }

        /// <summary>Renders the submitted production frame on the real GPU and reads its pixels.</summary>
        /// <param name="animations">The source animation renderer.</param>
        /// <param name="target">The GPU capture target.</param>
        /// <param name="pixels">The reusable readable output texture.</param>
        /// <returns>The rendered native-resolution pixels.</returns>
        private static Color32[] Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            Pool(animations).RenderTo(target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, 800f, 600f), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            return pixels.GetPixels32();
        }

        /// <summary>Counts pixels changed between original vehicle damage stages.</summary>
        /// <param name="left">The newly rendered stage.</param>
        /// <param name="right">The previously rendered stage.</param>
        /// <returns>The number of changed pixels.</returns>
        private static int Differences(Color32[] left, Color32[] right)
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

        /// <summary>Stores optional diagnostic screenshots only in the explicit ignored output directory.</summary>
        /// <param name="pixels">The GPU output buffer.</param>
        /// <param name="filename">The diagnostic filename.</param>
        private static void Save(Texture2D pixels, string filename)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, filename), pixels.EncodeToPNG());
            }
        }
    }
}
