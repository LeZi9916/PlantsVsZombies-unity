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
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the original rare death cannot rotate wrapped leg axes through a full turn during body blending.</summary>
    public sealed class OriginalZombieDeathBlendTests
    {
        /// <summary>Checks production corpse geometry follows the original wrapped leg and foot directions throughout its blend.</summary>
        /// <returns>A frame boundary allowing original texture and mesh rendering to initialize.</returns>
        [UnityTest]
        public IEnumerator SuperlongDeathKeepsOriginalLegDirectionsThroughCorpseBlend()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original zombie death blend fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner.Textures._textures["plantshadow2.png"] = null!;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var living = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var dead = typeof(GamePresenter).GetMethod("DrawAdventureCorpses", flags)!;
                var initialize = typeof(GameSimulation).GetMethod("InitializeAdventureZombieCorpse", flags)!;
                var load = typeof(ReanimationRenderer).GetMethod("LoadTexture", flags)!;
                Assert.That(animations.TryGetNativeAnimationInfo("Zombie", "anim_superlongdeath", out var death), Is.True, animations.LastError);
                Assert.That(death.Count, Is.EqualTo(137));
                var textures = new[]
                {
                    (Texture2D)load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_INNERLEG_LOWER" })!,
                    (Texture2D)load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_INNERLEG_FOOT" })!,
                    (Texture2D)load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_OUTERLEG_FOOT" })!
                };
                var zombie = new Zombie
                {
                    Id = 81,
                    Kind = ZombieKind.Normal,
                    Row = 2,
                    X = AdventureZombieNativeRules.GridX(200f),
                    Health = 270f,
                    StartingHealth = 270f,
                    PresentationAnimation = "anim_walk"
                };
                owner._game.Zombies.Add(zombie);
                Begin(animations);
                living.Invoke(presenter, new object[] { zombie });
                End(animations);

                var corpse = (AdventureZombieCorpse)initialize.Invoke(owner._game, new object[]
                {
                    new AdventureZombieCorpse { Zombie = zombie, Animation = "anim_superlongdeath", Rate = 14f }
                })!;
                owner._game.Zombies.Clear();
                owner._game.Presentation.Corpses.Add(corpse);
                var actual = new Vector2[5];
                for (var tick = 0; tick <= 20; tick++)
                {
                    corpse.Age = tick * .01f;
                    Begin(animations);
                    try
                    {
                        dead.Invoke(presenter, new object[] { zombie.Row });
                        var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
                        var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
                        ReadDirections(pool, sprites, textures, actual);

                        // Original superlongdeath starts at -358.4/-357.2 degrees on both inner-leg axes,
                        // and -341.5 on the outer foot's y axis. PC 0x481180 takes those new angles directly.
                        Assert.That(animations.DrawZombiePlayback(new Vector2(500f, 250f), 1f, corpse.Age,
                            ZombieVariant.Normal, "anim_superlongdeath", false, 14f), Is.True, animations.LastError);
                        var expected = new Vector2[5];
                        ReadDirections(pool, sprites, textures, expected);
                        for (var axis = 0; axis < actual.Length; axis++)
                        {
                            Assert.That(Vector2.Dot(actual[axis], expected[axis]), Is.GreaterThan(.99999f),
                                "The original death leg axis " + axis + " must retain its authored direction at blend tick " + tick + ".");
                        }
                    }
                    finally
                    {
                        End(animations);
                    }
                }
            }
            finally
            {
                presenter.Dispose();
                animations.Dispose();
                foreach (var texture in owner.Textures._textures.Values)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }

                owner.Textures._textures.Clear();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Reads the actual submitted mesh axes of both wrapped inner limbs and the outer foot.</summary>
        /// <param name="pool">The production renderer containing the completed draw commands.</param>
        /// <param name="sprites">The production renderer's retained mesh objects.</param>
        /// <param name="textures">The three exact original limb textures.</param>
        /// <param name="directions">The five normalized mesh axes receiving the trajectory sample.</param>
        private static void ReadDirections(ObjectSpriteRenderer pool, List<ObjectSprite> sprites, Texture2D[] textures, Vector2[] directions)
        {
            for (var limb = 0; limb < textures.Length; limb++)
            {
                ObjectSprite? match = null;
                for (var index = pool.SubmittedObjectCount - 1; index >= 0; index--)
                {
                    if (ReferenceEquals(sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")), textures[limb]))
                    {
                        match = sprites[index];
                        break;
                    }
                }

                Assert.That(match, Is.Not.Null, "The original death must submit its leg texture " + textures[limb].name + ".");
                var vertices = match!.Vertices;
                if (limb < 2)
                {
                    directions[limb * 2] = ((Vector2)(vertices[1] - vertices[0])).normalized;
                    directions[limb * 2 + 1] = ((Vector2)(vertices[3] - vertices[0])).normalized;
                }
                else
                {
                    directions[4] = ((Vector2)(vertices[3] - vertices[0])).normalized;
                }
            }
        }

        /// <summary>Begins one production presentation frame while retaining the live zombie's animation identity.</summary>
        /// <param name="animations">The renderer sampling the original body tracks.</param>
        private static void Begin(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.white;
            animations.BeginObjectFrame();
        }

        /// <summary>Publishes one production presentation frame and keeps the actor's current pose for its next blend.</summary>
        /// <param name="animations">The renderer owning the completed original body draw.</param>
        private static void End(ReanimationRenderer animations)
        {
            animations.EndObjectFrame();
            CanvasUI.EndFrame();
        }
    }
}
