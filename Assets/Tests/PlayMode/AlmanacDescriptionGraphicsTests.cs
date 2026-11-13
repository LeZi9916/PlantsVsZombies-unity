#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks that original almanac attributes and detailed prose reach the actual Canvas pixels.</summary>
    public sealed class AlmanacDescriptionGraphicsTests
    {
        /// <summary>Verifies original rich descriptions render below the independently visible short header.</summary>
        /// <returns>Frames required to build dynamic font meshes and read the actual camera output.</returns>
        [UnityTest]
        public IEnumerator OriginalAttributesAndDetailedDescriptionRenderInsideTheCard()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resources.");
            }
            yield return null;
            CanvasUI.Dispose();
            using var animations = new ReanimationRenderer(root!);
            using var view = new AlmanacView(root!, animations);
            var camera = ObjectSpriteRenderer.SceneCamera;
            var previousTarget = camera.targetTexture;
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            target.Create();
            camera.targetTexture = target;
            try
            {
                foreach (var plants in new[] { true, false })
                {
                    view.Open();
                    Assert.That(view.HandleClick(plants ? new Vector2(208f, 366f) : new Vector2(592f, 369f)), Is.True);
                    ObjectSpriteRenderer.BeginSceneFrame();
                    CanvasUI.BeginFrame();
                    var bounds = GameViewport.ScreenRect;
                    CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(bounds.x, bounds.y), Quaternion.identity, new Vector3(GameViewport.Scale, GameViewport.Scale, 1f));
                    animations.BeginObjectFrame();
                    view.Draw(0f);
                    animations.EndObjectFrame();
                    CanvasUI.EndFrame();
                    yield return null;
                    yield return null;
                    Read(target, pixels);
                    var withDescription = pixels.GetPixels32();
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var body = (string)typeof(AlmanacView).GetField("_formattedDescription", flags)!.GetValue(view)!;
                    var height = (float)typeof(AlmanacView).GetField("_descriptionHeight", flags)!.GetValue(view)!;
                    var rect = (Rect)typeof(AlmanacView).GetField("_descriptionRect", flags)!.GetValue(view)!;
                    Assert.That(body.Length, Is.GreaterThan(30), "The real resource must include attributes and prose.");
                    var label = Array.Find(Resources.FindObjectsOfTypeAll<CanvasTextGraphic>(), x => x.gameObject.activeInHierarchy && x.text == body);
                    Assert.That(label, Is.Not.Null);
                    Assert.That(label!.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0), "The real description must survive geometry clipping.");
                    Save(pixels, plants ? "plant-top" : "zombie-top");
                    label.enabled = false;
                    yield return null;
                    yield return null;
                    Read(target, pixels);
                    var withoutDescription = pixels.GetPixels32();
                    var visible = 0;
                    for (var y = 0; y < pixels.height; y++)
                    {
                        for (var x = 0; x < pixels.width; x++)
                        {
                            if (!rect.Contains(GameViewport.ScreenToLogical(new Vector2(x + .5f, y + .5f))))
                            {
                                continue;
                            }
                            var index = y * pixels.width + x;
                            var a = withDescription[index];
                            var b = withoutDescription[index];
                            if (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 15)
                            {
                                visible++;
                            }
                        }
                    }
                    label.enabled = true;
                    Assert.That(height, Is.GreaterThan(60f), "The original multiline body must retain its line height.");
                    Assert.That(visible, Is.GreaterThan(80), "Original attributes and detailed prose must produce visible card pixels: " + (plants ? "plant" : "zombie"));
                    if (height > rect.height)
                    {
                        Assert.That(view.DetailPageCount, Is.GreaterThan(1));
                        for (var step = 0; step <= Mathf.CeilToInt(height / 5f); step++)
                        {
                            Assert.That(view.HandleScroll(rect.center, 1f), Is.True);
                        }
                        Assert.That(view.DetailPage, Is.EqualTo(view.DetailPageCount - 1), "Scrolling must reach the full original description.");
                        ObjectSpriteRenderer.BeginSceneFrame();
                        CanvasUI.BeginFrame();
                        CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(bounds.x, bounds.y), Quaternion.identity, new Vector3(GameViewport.Scale, GameViewport.Scale, 1f));
                        animations.BeginObjectFrame();
                        view.Draw(0f);
                        animations.EndObjectFrame();
                        CanvasUI.EndFrame();
                        yield return null;
                        yield return null;
                        Read(target, pixels);
                        var scrolled = pixels.GetPixels32();
                        var changed = 0;
                        var bottomVisible = 0;
                        for (var index = 0; index < scrolled.Length; index++)
                        {
                            var point = GameViewport.ScreenToLogical(new Vector2(index % pixels.width + .5f, index / pixels.width + .5f));
                            if (!rect.Contains(point) || point.x >= rect.xMax - 8f)
                            {
                                continue;
                            }
                            if (!scrolled[index].Equals(withDescription[index]))
                            {
                                changed++;
                            }
                            var a = scrolled[index];
                            var b = withoutDescription[index];
                            if (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 15)
                            {
                                bottomVisible++;
                            }
                        }
                        Assert.That(changed, Is.GreaterThan(80), "Scrolling must expose a different part of the original body.");
                        Assert.That(bottomVisible, Is.GreaterThan(80), "The bottom of the rich description must remain visible after scrolling.");
                        Save(pixels, plants ? "plant-bottom" : "zombie-bottom");
                    }
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                CanvasUI.Dispose();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
            }
        }

        /// <summary>Reads one real camera frame while restoring the caller's render target.</summary>
        /// <param name="target">The camera texture to inspect.</param>
        /// <param name="pixels">The readable diagnostic texture.</param>
        private static void Read(RenderTexture target, Texture2D pixels)
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Saves optional card evidence outside tracked source assets.</summary>
        /// <param name="pixels">The actual composited card pixels.</param>
        /// <param name="name">The diagnostic snapshot label.</param>
        private static void Save(Texture2D pixels, string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_ALMANAC_GRAPHICS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            }
        }
    }
}
