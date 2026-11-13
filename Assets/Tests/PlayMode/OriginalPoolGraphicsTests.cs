#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the real original pool meshes, day/night artwork, sparkle playback, and freeze/resume pixels.</summary>
    public sealed class OriginalPoolGraphicsTests
    {
        /// <summary>Reads the original production pool output across time and the exact shared ice pause.</summary>
        /// <returns>Real camera frame boundaries before inspecting the submitted water pixels.</returns>
        [UnityTest]
        public IEnumerator OriginalWaterAnimatesAndIceHoldsEverySurfaceLayer()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "images", "pool_caustic_effect.jpg")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original pool graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var begin = typeof(GamePresenter).GetMethod("BeginAdventurePresentation", flags)!;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePool", flags)!;
                var end = typeof(GamePresenter).GetMethod("EndAdventurePresentation", flags)!;
                ObjectSpriteRenderer.SceneCamera.targetTexture = target;
                foreach (var level in new[] { 22, 32 })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    owner._game.ResetAdventureWaveSchedule(100000, false, false);
                    owner._game.Adventure.PoolEffectTicks = 117;
                    yield return Capture(animations, presenter, begin, draw, end, target, pixels);
                    var first = pixels.GetPixels32();
                    var pool = (OriginalPoolRenderer)typeof(GamePresenter).GetField("_poolRenderer", flags)!.GetValue(presenter)!;
                    var meshes = (Mesh[])typeof(OriginalPoolRenderer).GetField("_meshes", flags)!.GetValue(pool)!;
                    Assert.That(meshes, Has.Length.EqualTo(3));
                    foreach (var mesh in meshes)
                    {
                        Assert.That(mesh.vertexCount, Is.EqualTo(96));
                        Assert.That(mesh.triangles, Has.Length.EqualTo(450));
                    }

                    owner._game.Adventure.PoolEffectTicks = 217;
                    yield return Capture(animations, presenter, begin, draw, end, target, pixels);
                    Assert.That(Changed(first, pixels.GetPixels32()), Is.GreaterThan(500), "The original pool must move on both day and night boards.");
                    Save(pixels, "pool-original-moving-" + level + ".png");
                    owner._game.NotifyAdventureIceTrap();
                    yield return Capture(animations, presenter, begin, draw, end, target, pixels);
                    var frozen = pixels.GetPixels32();
                    owner._game.Update(.1f);
                    Assert.That(owner._game.Adventure.PoolEffectTicks, Is.EqualTo(217));
                    yield return Capture(animations, presenter, begin, draw, end, target, pixels);
                    Assert.That(Changed(frozen, pixels.GetPixels32()), Is.Zero, "Ice holds the base, shading, caustic texture and daytime sparkles.");
                    owner._game.Update(3f);
                    Assert.That(owner._game.Adventure.IceTrapTicks, Is.Zero);
                    Assert.That(owner._game.Adventure.PoolEffectTicks, Is.GreaterThan(217));
                    yield return Capture(animations, presenter, begin, draw, end, target, pixels);
                    Assert.That(Changed(frozen, pixels.GetPixels32()), Is.GreaterThan(50), "The original water resumes after the shared ice countdown ends.");
                }
            }
            finally
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = null;
                presenter.Dispose();
                animations.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Submits the production water call and reads the shared scene camera after rendering all three meshes.</summary>
        /// <param name="animations">The renderer owning the scene's other original sprites.</param>
        /// <param name="presenter">The production presenter owning its original water renderer.</param>
        /// <param name="begin">The production particle presentation frame entry.</param>
        /// <param name="draw">The actual production water drawing operation.</param>
        /// <param name="end">The production particle presentation frame exit.</param>
        /// <param name="target">The shared scene camera target.</param>
        /// <param name="pixels">The reusable camera readback image.</param>
        /// <returns>Frame boundaries before reading the visible scene pixels.</returns>
        private static IEnumerator Capture(ReanimationRenderer animations, GamePresenter presenter, MethodInfo begin,
            MethodInfo draw, MethodInfo end, RenderTexture target, Texture2D pixels)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            SpriteDrawState.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
            begin.Invoke(presenter, null);
            draw.Invoke(presenter, null);
            end.Invoke(presenter, null);
            animations.EndObjectFrame();
            yield return null;
            yield return null;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Counts visible changes with tolerance for harmless channel rounding.</summary>
        /// <param name="first">The preceding camera output.</param>
        /// <param name="second">The current camera output.</param>
        /// <returns>The number of pixels whose RGB sum differs by more than twelve byte values.</returns>
        private static int Changed(Color32[] first, Color32[] second)
        {
            var changes = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 12)
                {
                    changes++;
                }
            }

            return changes;
        }

        /// <summary>Saves optional diagnostics only beneath the explicitly selected ignored test output directory.</summary>
        /// <param name="pixels">The captured original-artwork pool frame.</param>
        /// <param name="name">The diagnostic filename.</param>
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
