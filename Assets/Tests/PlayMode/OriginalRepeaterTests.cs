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
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the original Repeater head clock when one pea launches during the next windup's first update.</summary>
    public sealed class OriginalRepeaterTests
    {
        /// <summary>Runs a real two-pea attack and compares its restarted head geometry with the original authored shooting clip.</summary>
        /// <returns>A frame boundary allowing the original artwork renderer to initialize.</returns>
        [UnityTest]
        public IEnumerator FirstPeaDoesNotSkipTheSecondHeadWindup()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original Repeater presentation fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner.Textures._textures["plantshadow.png"] = null!;
                var game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
                owner._game = game;
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
                game.ResetAdventureWaveSchedule(100000, false, false);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(game, new object[] { PlantKind.Repeater, 0, 1 })!;
                plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.Repeater, 26, 0);
                plant.ActionTimer = .26f;
                game.Zombies.Add(new Zombie
                {
                    Id = 91, Kind = ZombieKind.Normal, Row = 0,
                    X = AdventureZombieNativeRules.GridX(600f), Health = 10000f, StartingHealth = 10000f,
                    FrozenRemaining = 1000f
                });
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!;
                Action drawPlant = () => draw.Invoke(presenter, new object[] { plant });
                DrawFrame(animations, drawPlant);
                game.Update(.01f);
                Assert.That(plant.PresentationHeadAge[0], Is.Zero);
                DrawFrame(animations, drawPlant);
                game.Update(.24f);
                Assert.That(game.Peas, Is.Empty);
                DrawFrame(animations, drawPlant);

                // PC 0x472C70 launches the first pea, then 0x46D240 restarts the head on this same tick.
                game.Update(.01f);
                Assert.That(game.Peas, Has.Count.EqualTo(1));
                Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(26));
                Assert.That(plant.PresentationHeadAge[0], Is.Zero, "Projectile feedback must not replace the new head clock with 0.25 seconds.");
                Assert.That(plant.PresentationShotAge, Is.Zero);
                DrawFrame(animations, drawPlant);
                game.Update(.21f);
                Assert.That(plant.PresentationHeadAge[0], Is.EqualTo(.21f).Within(.00001f));
                DrawFrame(animations, drawPlant);
                var actual = Snapshot(animations);
                DrawFrame(animations, () =>
                {
                    Assert.That(animations.DrawNativeAttachedHeads("PeaShooter", new Vector2(120f, 80f), 1f,
                        plant.Age, plant.PresentationRate, new[]
                        {
                            new HeadPlayback { Anchor = "anim_stem", Animation = "anim_shooting", Seconds = .21f, Rate = 45f, Loop = false }
                        }), Is.True, animations.LastError);
                });
                var expected = Snapshot(animations);
                Assert.That(actual, Has.Count.EqualTo(expected.Count));
                for (var index = 0; index < actual.Count; index++)
                {
                    Assert.That(actual[index].Texture, Is.SameAs(expected[index].Texture));
                    for (var corner = 0; corner < 4; corner++)
                    {
                        Assert.That(Vector3.Distance(actual[index].Vertices[corner], expected[index].Vertices[corner]), Is.LessThan(.002f),
                            "The second windup must display the original shooting frame after its twenty-tick blend.");
                    }
                }

                game.Update(.03f);
                Assert.That(game.Peas, Has.Count.EqualTo(1));
                Assert.That(plant.PresentationHeadAge[0], Is.EqualTo(.24f).Within(.00001f));
                game.Update(.01f);
                Assert.That(game.Peas, Has.Count.EqualTo(2));
                Assert.That(plant.PresentationHeadAge[0], Is.EqualTo(.25f).Within(.00001f));
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

        /// <summary>Publishes one production animation frame while retaining the actor's blend identity.</summary>
        /// <param name="animations">The original artwork renderer.</param>
        /// <param name="draw">The production or independent native drawing operation.</param>
        private static void DrawFrame(ReanimationRenderer animations, Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            try
            {
                draw();
            }
            finally
            {
                animations.EndObjectFrame();
            }
        }

        /// <summary>Copies the real submitted texture identities and mesh corners for an independent pose comparison.</summary>
        /// <param name="animations">The renderer that submitted the original plant frame.</param>
        /// <returns>The original frame's ordered image quads and their actual transformed vertices.</returns>
        private static List<(Texture? Texture, Vector3[] Vertices)> Snapshot(ReanimationRenderer animations)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
            var result = new List<(Texture? Texture, Vector3[] Vertices)>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                result.Add((sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")), (Vector3[])sprites[index].Vertices.Clone()));
            }

            return result;
        }
    }
}
