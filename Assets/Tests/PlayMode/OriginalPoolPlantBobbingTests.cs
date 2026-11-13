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
    /// <summary>Checks original plant artwork moves with the PC pool-square phase rather than its body animation clock.</summary>
    public sealed class OriginalPoolPlantBobbingTests
    {
        /// <summary>Checks original support, body, armor, aquatic, small, and flying artwork through production presentation.</summary>
        /// <returns>A frame allowing the actual rendering pipeline to initialize before original artwork is submitted.</returns>
        [UnityTest]
        public IEnumerator WaterPlantsShareTheOriginalCellBobbingWhileFlyingCoffeeStaysLevel()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original pool plant bobbing fixture");
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
                var rendererField = typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!;
                var cases = new[]
                {
                    PlantKind.LilyPad, PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut,
                    PlantKind.Pumpkin, PlantKind.TangleKelp, PlantKind.SeaShroom, PlantKind.Cattail, PlantKind.CoffeeBean
                };
                foreach (var level in new[] { 22, 32 })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
                    Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                    var dayShadow = owner.Textures.RequiredLoadImage("plantshadow.png");
                    var nightShadow = owner.Textures.RequiredLoadImage("plantshadow2.png");
                    foreach (var row in new[] { 1, 2, 3 })
                    {
                        foreach (var column in new[] { 0, 1 })
                        {
                            foreach (var kind in cases)
                            {
                                owner._game.Plants.Clear();
                                var plant = new Plant
                                {
                                    Id = 1, Kind = kind, Row = row, Column = column,
                                    Health = 8000f, Age = .5f, IsSmall = column == 1,
                                    Layer = kind == PlantKind.LilyPad ? PlantLayer.Support
                                        : kind == PlantKind.Pumpkin ? PlantLayer.Armor
                                        : kind == PlantKind.CoffeeBean ? PlantLayer.Effect : PlantLayer.Main
                                };
                                owner._game.Plants.Add(plant);
                                if (kind != PlantKind.LilyPad && kind != PlantKind.TangleKelp
                                    && kind != PlantKind.SeaShroom && kind != PlantKind.Cattail)
                                {
                                    owner._game.Plants.Add(new Plant
                                    {
                                        Id = 2, Kind = PlantKind.LilyPad, Row = row, Column = column,
                                        Layer = PlantLayer.Support, Health = 300f
                                    });
                                }

                                List<Vector3[]>? baseline = null;
                                foreach (var ticks in new[] { 0, 50, 150 })
                                {
                                    owner._game.Adventure._ticks = ticks;
                                    ObjectSpriteRenderer.BeginSceneFrame();
                                    animations.BeginObjectFrame();
                                    draw.Invoke(presenter, new object[] { plant });
                                    animations.EndObjectFrame();
                                    var renderer = (ObjectSpriteRenderer)rendererField.GetValue(animations)!;
                                    var sprites = renderer.SubmittedSprites;
                                    var body = new List<Vector3[]>();
                                    for (var index = 0; index < renderer.SubmittedObjectCount; index++)
                                    {
                                        var sprite = sprites[index];
                                        var image = sprite.SourceImage;
                                        if (!ReferenceEquals(image, dayShadow) && !ReferenceEquals(image, nightShadow))
                                        {
                                            body.Add((Vector3[])sprite.Vertices.Clone());
                                        }
                                    }

                                    Assert.That(body.Count, Is.GreaterThan(0), "Original plant body artwork must be submitted: " + kind);
                                    if (baseline == null)
                                    {
                                        baseline = body;
                                        continue;
                                    }

                                    var water = row == 2 || row == 3;
                                    var phase = row * Math.PI + column * Math.PI / 4d;
                                    var expected = water && kind != PlantKind.CoffeeBean
                                        ? (Math.Sin(phase + ticks * Math.PI * 2d / 200d) - Math.Sin(phase)) * 2d : 0d;
                                    Assert.That(body.Count, Is.EqualTo(baseline.Count));
                                    for (var sprite = 0; sprite < body.Count; sprite++)
                                    {
                                        for (var vertex = 0; vertex < body[sprite].Length; vertex++)
                                        {
                                            var context = kind + " level " + level + " row " + row + " column " + column + " tick " + ticks;
                                            Assert.That(body[sprite][vertex].x, Is.EqualTo(baseline[sprite][vertex].x).Within(.0001f), context);
                                            Assert.That(baseline[sprite][vertex].y - body[sprite][vertex].y,
                                                Is.EqualTo(expected).Within(.0001f), context);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                presenter.Dispose();
                animations.Dispose();
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}
