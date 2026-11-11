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
            var textures = (Dictionary<string, Texture2D>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(renderer)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(renderer)!;
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
            var actual = new HashSet<Texture>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                actual.Add(sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")));
            }

            Assert.That(actual, Does.Contain(textures["IMAGE_REANIM_CRAZYDAVE_MOUTH5"]));
            Assert.That(actual, Does.Contain(textures["IMAGE_REANIM_CRAZYDAVE_BLINK1"]));
            Assert.That(actual.Contains(textures["IMAGE_REANIM_CRAZYDAVE_EYE"]), Is.False);
        }
    }
}
