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
    /// <summary>Checks the original disarmed digger's surprise attachment and clipped emergence artwork.</summary>
    public sealed class OriginalDiggerGraphicsTests
    {
        /// <summary>Checks the original question mark lifetime and the stationary no-axe landing pose beneath its ground clip.</summary>
        /// <returns>A frame boundary for real original artwork and graphics initialization.</returns>
        [UnityTest]
        public IEnumerator DisarmedSurpriseAndClippedEmergenceMatchOriginalArtwork()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original digger graphics fixture");
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
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var effects = typeof(GamePresenter).GetMethod("DrawAdventureDiggerEffects", flags)!;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var update = typeof(GameSimulation).GetMethod("AdvanceAdventureDigger", flags)!;
                var zombie = new Zombie
                {
                    Id = 81, Kind = ZombieKind.Digger, Row = 2, X = 4f, PresentationPreviousX = 4f,
                    Health = 270f, StartingHealth = 270f, ArmorHealth = 100f, PresentationMaxArmor = 100f,
                    UsedAbility = true, PresentationNativePhase = 34, PresentationWalkingBackwards = true,
                    PresentationInPool = false, DiggerState = new AdventureDiggerState { PhaseTicks = 151 }
                };
                owner._game.Zombies.Add(zombie);
                SpriteDrawState.Matrix = Matrix4x4.identity;
                SpriteDrawState.Color = Color.white;
                SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                BeginFrame(animations);
                effects.Invoke(presenter, new object[] { zombie });
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var blank = pixels.GetPixels32();
                update.Invoke(owner._game, new object[] { zombie, .01f });
                Assert.That(zombie.DiggerState!.PhaseTicks, Is.EqualTo(150));
                Assert.That(zombie.DiggerState.SurpriseAge, Is.Zero);
                zombie.DiggerState.SurpriseAge = .5f;
                BeginFrame(animations);
                effects.Invoke(presenter, new object[] { zombie });
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var surprise = pixels.GetPixels32();
                Assert.That(surprise, Is.Not.EqualTo(blank), "The original question-mark artwork must be visible during the axe-loss pause.");
                BeginFrame(animations);
                // PC native origin is (288,250); the surprise attachment ignores the body's mirrored draw.
                Assert.That(animations.DrawNativePlayback("Zombie_surprise", new Vector2(311f, 343f), 1f,
                    .5f, null, false, 12f), Is.True);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(surprise, Is.EqualTo(pixels.GetPixels32()),
                    "PC 543D90 places the unmirrored attachment at native origin+(23,93-altitude).");
                zombie.DiggerState.SurpriseAge = 1.25f;
                BeginFrame(animations);
                effects.Invoke(presenter, new object[] { zombie });
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(pixels.GetPixels32(), Is.EqualTo(blank), "The original fifteen-frame question mark must finish after 1.25 seconds.");

                zombie.DiggerState.SurpriseAge = -1f;
                zombie.DiggerState.PhaseTicks = 100;
                zombie.PresentationNativePhase = 35;
                zombie.AbilityPhase = 1;
                zombie.PresentationAltitude = -42f;
                zombie.PresentationWalkingBackwards = null;
                BeginFrame(animations);
                draw.Invoke(presenter, new object[] { zombie });
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var emergence = pixels.GetPixels32();
                Assert.That(emergence, Is.Not.EqualTo(blank), "The original held landing frame must rise visibly from the soil.");
                BeginFrame(animations);
                // PC holds landing at zero FPS until counter30 and clips at nativeY+113 while counter>20.
                SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 363f);
                Assert.That(animations.DrawNativePlayback("Zombie_digger", new Vector2(303f, 284f), 1f,
                    0f, "anim_landing", false, 0f,
                    track => !string.Equals(track, "zombie_digger_pickaxe", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(track, "zombie_digger_dirt", StringComparison.OrdinalIgnoreCase)), Is.True);
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(emergence, Is.EqualTo(pixels.GetPixels32()),
                    "The no-axe rise must keep the hardhat, hide the pickaxe, hold the landing frame, and cancel altitude from its ground clip.");
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

        /// <summary>Begins a native sprite frame over the same white reference background.</summary>
        /// <param name="animations">The original artwork renderer receiving the frame.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Renders submitted sprites through the real graphics device and reads their pixels.</summary>
        /// <param name="animations">The renderer owning the submitted sprites.</param>
        /// <param name="target">The fixture's uniquely owned render target.</param>
        /// <param name="pixels">The reusable readable image receiving the rendered frame.</param>
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
