#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the original Dave mouth and blink artwork submitted by the reanimation renderer.</summary>
    public sealed class CrazyDaveGraphicsTests
    {
        /// <summary>Checks the native hammer pose, offset, scale, and draw order at Dave's base hand pose.</summary>
        /// <returns>A frame boundary for the renderer.</returns>
        [UnityTest]
        public IEnumerator HeldHammerUsesOriginalHandAttachment()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "CrazyDave.reanim")) || !File.Exists(Path.Combine(root, "reanim", "Hammer.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            using var renderer = new ReanimationRenderer(root);
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            renderer.BeginObjectFrame();
            try
            {
                Assert.That(renderer.DrawNativePlayback("CrazyDave", Vector2.zero, 1f, 0f, "anim_idle_handing", true, 12f, crazyDaveHeldItem: "HAMMER"), Is.True);
            }
            finally
            {
                renderer.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
            var sprites = pool.SubmittedSprites;
            var handIndex = -1;
            var hammerIndex = -1;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var texture = sprites[index].SourceImage;
                if (texture == textures["IMAGE_REANIM_CRAZYDAVE_HANDINGHAND"])
                {
                    handIndex = index;
                }
                if (texture == textures["IMAGE_REANIM_HAMMER_3"])
                {
                    hammerIndex = index;
                }
            }

            Assert.That(handIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(hammerIndex, Is.EqualTo(handIndex + 1));
            // The last whack frame begins at (57.9, -19.3); the base hand pose is identity.
            Assert.That(sprites[hammerIndex].Vertices[0].x, Is.EqualTo(148.85f).Within(.01f));
            Assert.That(sprites[hammerIndex].Vertices[0].y, Is.EqualTo(-416.05f).Within(.01f));
        }

        /// <summary>Checks the native wall-nut attachment pose, placement, layering, and independent animation.</summary>
        /// <returns>A frame boundary for the renderer.</returns>
        [UnityTest]
        public IEnumerator HeldWallnutUsesOriginalAnimatedAttachment()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "CrazyDave.reanim")) || !File.Exists(Path.Combine(root, "reanim", "Wallnut.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            using var renderer = new ReanimationRenderer(root);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var firstX = 0f;
            for (var frame = 0; frame < 2; frame++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                CanvasUI.BeginFrame();
                CanvasUI.Matrix = Matrix4x4.identity;
                CanvasUI.Color = Color.white;
                renderer.BeginObjectFrame();
                try
                {
                    Assert.That(renderer.DrawNativePlayback("CrazyDave", Vector2.zero, 1f, 0f, "anim_idle_handing", true, 12f,
                        crazyDaveHeldItem: "WALLNUT", crazyDaveHeldItemAge: frame * .5f), Is.True);
                }
                finally
                {
                    renderer.EndObjectFrame();
                    CanvasUI.EndFrame();
                }

                var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
                var sprites = pool.SubmittedSprites;
                var handIndex = -1;
                var wallnutIndex = -1;
                for (var index = 0; index < pool.SubmittedObjectCount; index++)
                {
                    var texture = sprites[index].SourceImage;
                    if (texture == textures["IMAGE_REANIM_CRAZYDAVE_HANDINGHAND"])
                    {
                        handIndex = index;
                    }
                    if (texture == textures["IMAGE_REANIM_WALLNUT_BODY"])
                    {
                        wallnutIndex = index;
                    }
                }

                Assert.That(handIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(wallnutIndex, Is.EqualTo(handIndex + 1));
                if (frame == 0)
                {
                    firstX = sprites[wallnutIndex].Vertices[0].x;
                    Assert.That(firstX, Is.EqualTo(108.28f).Within(.01f));
                    Assert.That(sprites[wallnutIndex].Vertices[0].y, Is.EqualTo(-397.32f).Within(.01f));
                }
                else
                {
                    Assert.That(sprites[wallnutIndex].Vertices[0].x, Is.Not.EqualTo(firstX).Within(.01f));
                }
            }
        }

        /// <summary>Checks the original money bag replacement and its hand attachment.</summary>
        /// <returns>A frame boundary for the renderer.</returns>
        [UnityTest]
        public IEnumerator HeldMoneyBagUsesOriginalImageAndHandPlacement()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "ZenGarden_fertilizer.reanim"))
                || !File.Exists(Path.Combine(root, "images", "moneybag.png")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            using var renderer = new ReanimationRenderer(root);
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            renderer.BeginObjectFrame();
            try
            {
                Assert.That(renderer.DrawNativePlayback("CrazyDave", Vector2.zero, 1f, 0f, "anim_idle_handing", true, 12f,
                    crazyDaveHeldItem: "MONEYBAG"), Is.True);
            }
            finally
            {
                renderer.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
            var sprites = pool.SubmittedSprites;
            var handIndex = -1;
            var bagIndex = -1;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var texture = sprites[index].SourceImage;
                if (texture == textures["IMAGE_REANIM_CRAZYDAVE_HANDINGHAND"])
                {
                    handIndex = index;
                }

                if (texture == textures["IMAGE_MONEYBAG"])
                {
                    bagIndex = index;
                }
            }

            Assert.That(handIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(bagIndex, Is.EqualTo(handIndex + 1));
            Assert.That(sprites[bagIndex].Vertices[0].x, Is.EqualTo(95.4f).Within(.01f));
            Assert.That(sprites[bagIndex].Vertices[0].y, Is.EqualTo(-409.9f).Within(.01f));
        }

        /// <summary>Checks that Dave's tree-food bag uses the original first frame and placement.</summary>
        /// <returns>A frame boundary for the renderer.</returns>
        [UnityTest]
        public IEnumerator HeldTreeFoodUsesOriginalBagTrack()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "TreeFood.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            using var renderer = new ReanimationRenderer(root);
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            renderer.BeginObjectFrame();
            try
            {
                Assert.That(renderer.DrawNativePlayback("CrazyDave", Vector2.zero, 1f, 0f, "anim_idle_handing", true, 12f,
                    crazyDaveHeldItem: "TREE_FOOD"), Is.True);
            }
            finally
            {
                renderer.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
            var sprites = pool.SubmittedSprites;
            var treeFoodIndex = -1;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (sprites[index].SourceImage == textures["IMAGE_REANIM_TREEFOOD"])
                {
                    treeFoodIndex = index;
                }
            }

            Assert.That(treeFoodIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(sprites[treeFoodIndex].Vertices[0].x, Is.EqualTo(100.6f).Within(.01f));
            Assert.That(sprites[treeFoodIndex].Vertices[0].y, Is.EqualTo(-414.9f).Within(.01f));
        }

        /// <summary>Checks mouth substitution, hidden open eyes, and attached blink artwork.</summary>
        /// <returns>A frame boundary for the renderer.</returns>
        [UnityTest]
        public IEnumerator IdleMouthAndBlinkUseOriginalArt()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "reanim", "CrazyDave.reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            using var renderer = new ReanimationRenderer(root);
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            renderer.BeginObjectFrame();
            try
            {
                Assert.That(renderer.DrawNativePlayback("CrazyDave", Vector2.zero, 1f, 0f, "anim_idle", true, 12f,
                    visible: track => track != "Dave_eye", overrideTrack: "Dave_mouths", overrideImage: "IMAGE_REANIM_CRAZYDAVE_MOUTH5"), Is.True);
                Assert.That(renderer.DrawNativeCrazyDaveBlink(Vector2.zero, 1f, "anim_idle", 0f, 12f, true, .1f), Is.True);
            }
            finally
            {
                renderer.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var textures = (Dictionary<string, SpriteImage>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
            var sprites = pool.SubmittedSprites;
            var actual = new HashSet<SpriteImage?>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                actual.Add(sprites[index].SourceImage);
            }

            Assert.That(actual, Does.Contain(textures["IMAGE_REANIM_CRAZYDAVE_MOUTH5"]));
            Assert.That(actual, Does.Contain(textures["IMAGE_REANIM_CRAZYDAVE_BLINK1"]));
            // The parent hides its open eye; only the blink layer's squashing Dave_eye may draw it.
            Assert.That(actual, Does.Contain(textures["IMAGE_REANIM_CRAZYDAVE_EYE"]));
            var eyeSprites = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (sprites[index].SourceImage == textures["IMAGE_REANIM_CRAZYDAVE_EYE"])
                {
                    eyeSprites++;
                }
            }

            Assert.That(eyeSprites, Is.EqualTo(1));
        }
    }
}
