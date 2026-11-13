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
    /// <summary>Checks every production plant shadow against the local PC 0x473AD0 drawing oracle.</summary>
    public sealed class OriginalPlantShadowTests
    {
        /// <summary>Checks original day/night artwork, species geometry, exclusions, pots, and growth through the actual presenter.</summary>
        /// <returns>A frame boundary allowing the real rendering pipeline to initialize.</returns>
        [UnityTest]
        public IEnumerator EveryPlantUsesItsOriginalGroundShadow()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original plant shadow fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                owner._animations = animations;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", flags)!;
                // Native draw arguments, before 0x527D20 adds the unscaled image center.
                var exceptions = new Dictionary<int, (float X, float Y, float Scale)>
                {
                    { 6, (-21f, 57f, 1f) }, { 8, (-3f, 42f, .5f) }, { 9, (-3f, 42f, .5f) },
                    { 10, (-3f, 47f, 1.3f) }, { 13, (-9f, 55f, 1f) }, { 17, (-3f, 56f, 1f) },
                    { 23, (-3f, 54f, 1.3f) }, { 25, (-3f, 57f, 1f) }, { 26, (-8f, 50f, 1f) },
                    { 30, (-3f, 46f, 1.4f) }, { 32, (-3f, 47f, 1f) }, { 33, (-4f, 46f, 1f) },
                    { 34, (0f, 47f, 1f) }, { 35, (-3f, 81f, 1f) }, { 37, (-7f, 52f, .5f) },
                    { 39, (-3f, 47f, 1f) }, { 42, (-3f, 47f, 1.3f) }, { 44, (-3f, 47f, 1f) }
                };
                var excluded = new HashSet<int> { 11, 16, 19, 21, 24, 29, 43, 46, 47 };
                var heights = new Dictionary<int, float> { { 8, 5f }, { 13, -14f }, { 30, 15f }, { 33, 26f }, { 35, -20f } };
                foreach (var level in new[] { 8, 12, 22, 42 })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    var shadow = owner.Textures.RequiredLoad(level == 12 ? "plantshadow2.png" : "plantshadow.png");
                    var otherShadow = owner.Textures.RequiredLoad(level == 12 ? "plantshadow.png" : "plantshadow2.png");
                    Assert.That(new Vector2(shadow.width, shadow.height), Is.EqualTo(new Vector2(86f, 36f)));
                    for (var id = 0; id < 50; id++)
                    {
                        foreach (var small in new[] { false, true })
                        {
                            owner._game.Plants.Clear();
                            var plant = new Plant
                            {
                                Id = 1, Kind = (PlantKind)id, Column = 1, Row = level == 22 ? 2 : 0,
                                Health = 8000f, IsSmall = small, Age = .5f,
                                PresentationAwakeAge = id == 9 && small ? 120.5f : 0f
                            };
                            owner._game.Plants.Add(plant);
                            var expected = exceptions.TryGetValue(id, out var species) ? species : (-3f, 51f, 1f);
                            if (id == 9 && small)
                            {
                                expected = (-3f, 42f, .75f);
                            }
                            var height = heights.TryGetValue(id, out var value) ? value : 0f;
                            var cellY = 80f;
                            if (level == 22)
                            {
                                cellY = 250f;
                                height += id == 35 ? 0f : 1.41421356f;
                            }

                            if (level == 42)
                            {
                                cellY += 70f;
                                if (id != 33)
                                {
                                    owner._game.Plants.Add(new Plant { Id = 2, Kind = PlantKind.FlowerPot, Row = 0, Column = 1, Layer = PlantLayer.Support });
                                    height -= id == 13 ? 0f : id == 6 || id == 25 ? 10f : 5f;
                                }
                            }

                            ObjectSpriteRenderer.BeginSceneFrame();
                            animations.BeginObjectFrame();
                            draw.Invoke(presenter, new object[] { plant });
                            animations.EndObjectFrame();
                            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
                            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
                            var submitted = FindShadow(pool, sprites, shadow);
                            Assert.That(FindShadow(pool, sprites, otherShadow), Is.Null, "Wrong terrain shadow: " + plant.Kind);
                            if (excluded.Contains(id))
                            {
                                Assert.That(submitted, Is.Null, "No separate native shadow: " + plant.Kind);
                                continue;
                            }

                            Assert.That(submitted, Is.Not.Null, "Missing native shadow: " + plant.Kind);
                            var bounds = new Rect(120f + expected.Item1 + 43f * (1f - expected.Item3),
                                cellY + height + expected.Item2 + 18f * (1f - expected.Item3), 86f * expected.Item3, 36f * expected.Item3);
                            AssertBounds(submitted!, bounds, plant.Kind + " level " + level + " small " + small);
                        }
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

        /// <summary>Checks the native continuous growth scale and the overlapping coffee shadow exclusion.</summary>
        [Test]
        public void GrowthAndFlyingCoffeeKeepTheOriginalShadowRules()
        {
            var sun = new Plant { Kind = PlantKind.SunShroom };
            foreach (var phase in new[] { 0f, .25f, .5f, .75f, 1f })
            {
                var actual = AdventurePlantShadowRules.Bounds(sun, new Vector2(120f, 80f), 0f, new Vector2(86f, 36f), phase);
                var scale = .5f + phase * .5f;
                Assert.That(actual.width, Is.EqualTo(86f * scale));
                Assert.That(actual.height, Is.EqualTo(36f * scale));
                Assert.That(actual.center, Is.EqualTo(new Vector2(160f, 140f)));
            }

            var coffee = new Plant { Kind = PlantKind.CoffeeBean, Row = 1, Column = 2, Layer = PlantLayer.Effect };
            var support = new Plant { Kind = PlantKind.FlowerPot, Row = 1, Column = 2, Layer = PlantLayer.Support };
            var plants = new List<Plant> { coffee, support };
            Assert.That(AdventurePlantShadowRules.HasShadow(coffee, plants), Is.True);
            plants.Add(new Plant { Kind = PlantKind.Pumpkin, Row = 1, Column = 2, Layer = PlantLayer.Armor });
            Assert.That(AdventurePlantShadowRules.HasShadow(coffee, plants), Is.False);
            plants[2] = new Plant { Kind = PlantKind.FumeShroom, Row = 1, Column = 2, Layer = PlantLayer.Main };
            Assert.That(AdventurePlantShadowRules.HasShadow(coffee, plants), Is.False);
        }

        /// <summary>Finds the actual submitted sprite using one original shadow resource.</summary>
        /// <param name="pool">The renderer that submitted the production frame.</param>
        /// <param name="sprites">The reusable sprite list owned by that renderer.</param>
        /// <param name="image">The original day or night image being sought.</param>
        /// <returns>The matching shadow sprite, or null if the species has none.</returns>
        private static ObjectSprite? FindShadow(ObjectSpriteRenderer pool, List<ObjectSprite> sprites, Texture2D image)
        {
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (ReferenceEquals(sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")), image))
                {
                    return sprites[index];
                }
            }

            return null;
        }

        /// <summary>Compares actual scene geometry with the independently transcribed PC bounds.</summary>
        /// <param name="sprite">The original image sprite emitted by the production presenter.</param>
        /// <param name="expected">The PC helper's expected rectangle.</param>
        /// <param name="context">The species, level, and body scale identifying this case.</param>
        private static void AssertBounds(ObjectSprite sprite, Rect expected, string context)
        {
            Assert.That(sprite.Vertices[0].x, Is.EqualTo(expected.x).Within(.0001f), context + " left");
            Assert.That(-sprite.Vertices[0].y, Is.EqualTo(expected.y).Within(.0001f), context + " top");
            Assert.That(sprite.Vertices[2].x - sprite.Vertices[0].x, Is.EqualTo(expected.width).Within(.0001f), context + " width");
            Assert.That(sprite.Vertices[0].y - sprite.Vertices[2].y, Is.EqualTo(expected.height).Within(.0001f), context + " height");
        }
    }
}
