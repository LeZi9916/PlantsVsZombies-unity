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
    /// <summary>Checks planted Plantern light uses original XML and pixels, native placement, lifetime, and foreground ordering.</summary>
    public sealed class OriginalPlanternGraphicsTests
    {
        /// <summary>Checks the twenty original rotating pulses persist above fog, stay below money, and leave with the plant.</summary>
        /// <returns>The graphics initialization frame.</returns>
        [UnityTest]
        public IEnumerator PlantedPlanternShinesWithOriginalParticlesAboveFogUntilRemoved()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "particles", "LanternShine.xml")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Plantern graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var options = new OptionsView(root);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._optionsView = options;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 37 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(owner._game, new object[] { PlantKind.Plantern, 1, 5 })!;
                plant.Age = .25f;
                var pool = Submit(animations, presenter, false);
                var first = Read(pool, target, pixels);
                AssertLights(pool, 20);
                Assert.That(Array.Exists(first, pixel => pixel.r < 240 || pixel.g < 240 || pixel.b < 240), Is.True);
                var reference = new OriginalParticleRenderer(root, animations);
                BeginNativeFrame(animations);
                reference.BeginFrame();
                Assert.That(reference.Draw("original-reference", "LanternShine", .25f, new Vector2(480f, 205f), plant.Id), Is.True);
                reference.EndFrame();
                animations.EndObjectFrame();
                CanvasUI.EndFrame();
                Assert.That(Read(pool, target, pixels), Is.EqualTo(first), "Production must use PC mX+40,mY+40 and the unchanged original emitter.");
                reference.Clear();

                plant.Age = 3.5f;
                pool = Submit(animations, presenter, false);
                AssertLights(pool, 20);
                var later = Read(pool, target, pixels);
                Assert.That(later, Is.Not.EqualTo(first), "Both original looping emitters must continue rotating and pulsing.");
                for (var tick = 0; tick < 100; tick++)
                {
                    owner._game.AdvanceAdventureFog();
                }

                owner._game.RequiredSpawnAdventureCoin(AdventureCoinKind.Gold, 7f, 1);
                pool = Submit(animations, presenter, true);
                var lastFog = -1;
                var firstLight = int.MaxValue;
                var lastLight = -1;
                var firstMoney = int.MaxValue;
                for (var index = 0; index < pool.SubmittedObjectCount; index++)
                {
                    var name = pool.SubmittedSprites[index].SourceImage?.SourceIdentity ?? "";
                    if (name.EndsWith("fog.png", StringComparison.OrdinalIgnoreCase))
                    {
                        lastFog = index;
                    }
                    else if (name.EndsWith("LanternShine.png", StringComparison.OrdinalIgnoreCase))
                    {
                        firstLight = Math.Min(firstLight, index);
                        lastLight = index;
                    }
                    else if (name.IndexOf("coin_gold_dollar", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        firstMoney = Math.Min(firstMoney, index);
                    }
                }

                Assert.That(lastFog, Is.GreaterThanOrEqualTo(0));
                Assert.That(firstLight, Is.GreaterThan(lastFog));
                Assert.That(lastLight, Is.LessThan(firstMoney));
                Assert.That(firstMoney, Is.LessThan(int.MaxValue));
                owner._game.Plants.Remove(plant);
                pool = Submit(animations, presenter, false);
                AssertLights(pool, 0);
                Read(pool, target, pixels);
                Assert.That(Array.TrueForAll(pixels.GetPixels32(), pixel => pixel.r == 255 && pixel.g == 255 && pixel.b == 255), Is.True,
                    "The original attached shine must disappear immediately when its plant is removed.");
            }
            finally
            {
                presenter.Dispose();
                options.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Submits the production light pass or complete board with its real fog and pickup ordering.</summary>
        /// <param name="animations">The original artwork renderer.</param>
        /// <param name="presenter">The real board presenter.</param>
        /// <param name="wholeBoard">Whether to include the surrounding fog and money passes.</param>
        /// <returns>The actual renderer retaining submitted original source commands.</returns>
        private static ObjectSpriteRenderer Submit(ReanimationRenderer animations, GamePresenter presenter, bool wholeBoard)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            BeginNativeFrame(animations);
            if (wholeBoard)
            {
                typeof(GamePresenter).GetMethod("DrawAdventureBoard", flags)!.Invoke(presenter, null);
            }
            else
            {
                (typeof(GamePresenter).GetField("_poolRenderer", flags)!.GetValue(presenter) as OriginalPoolRenderer)?.Hide();
                typeof(GamePresenter).GetMethod("BeginAdventurePresentation", flags)!.Invoke(presenter, null);
                typeof(GamePresenter).GetMethod("DrawAdventurePlanternLights", flags)!.Invoke(presenter, null);
                typeof(GamePresenter).GetMethod("EndAdventurePresentation", flags)!.Invoke(presenter, null);
            }

            animations.EndObjectFrame();
            CanvasUI.EndFrame();
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
        }

        /// <summary>Gives production and reference passes identical native coordinates, clipping, tint, and white backgrounds.</summary>
        /// <param name="animations">The renderer receiving the native-resolution pass.</param>
        private static void BeginNativeFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            // BeginSceneFrame otherwise restores Screen dimensions, which can clip an 800-by-600 reference target.
            SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Checks the original XML's two ten-particle emitters keep ordinary alpha blending.</summary>
        /// <param name="pool">The submitted original source command list.</param>
        /// <param name="expected">The expected number of original shine sprites.</param>
        private static void AssertLights(ObjectSpriteRenderer pool, int expected)
        {
            var count = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage?.SourceIdentity.EndsWith("LanternShine.png", StringComparison.OrdinalIgnoreCase) == true)
                {
                    count++;
                    Assert.That(sprite.Mode, Is.Zero, "The original XML does not request additive blending.");
                    Assert.That(sprite.Colors[0].a, Is.InRange(.299f, .501f));
                }
            }

            Assert.That(count, Is.EqualTo(expected));
        }

        /// <summary>Renders the original source commands on the real GPU and reads native-resolution pixels.</summary>
        /// <param name="pool">The renderer containing the actual source commands.</param>
        /// <param name="target">The reusable graphics target.</param>
        /// <param name="pixels">The readable output texture.</param>
        /// <returns>The actual rendered color pixels.</returns>
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
