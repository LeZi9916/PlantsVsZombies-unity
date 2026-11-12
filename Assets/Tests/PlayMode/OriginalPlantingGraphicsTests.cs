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
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original dirt and water planting emitters through successful player planting and the production presenter.</summary>
    public sealed class OriginalPlantingGraphicsTests
    {
        /// <summary>Stores the explicitly selected read-only original resource directory.</summary>
        private string _root = "";
        /// <summary>Owns an inactive coordinator without game lifecycle or profile access.</summary>
        private GameObject _host = null!;
        /// <summary>Provides the real simulation and resource state to the presenter.</summary>
        private PvZGame _owner = null!;
        /// <summary>Submits the original particle atlas pixels to native scene objects.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Runs the production planting effect drawing operation.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives actual Canvas and scene camera output.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives readable camera output for independent emitter comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the surrounding runner's scene camera target.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Maps the original logical viewport onto the current test screen.</summary>
        private Matrix4x4 _viewport;

        /// <summary>Creates a real-resource graphics fixture without starting gameplay or touching persistent data.</summary>
        /// <returns>A frame boundary allowing the render pipeline to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(_root, "particles")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            Assert.That(File.Exists(Path.Combine(_root, "particles", "Planting.xml")), Is.True);
            Assert.That(File.Exists(Path.Combine(_root, "particles", "PlantingPool.xml")), Is.True);
            yield return null;
            _host = new GameObject("Inactive original planting graphics fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = _root;
            _animations = new ReanimationRenderer(_root);
            _owner._animations = _animations;
            _presenter = new GamePresenter(_owner);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var offset = new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f, 0f);
            _viewport = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        /// <summary>Releases only scene objects and resources owned by this isolated graphics fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction to finish.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                _presenter.Dispose();
                _animations.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
                UnityEngine.Object.Destroy(_host);
            }

            yield return null;
        }

        /// <summary>Checks a real sunflower planting draws the original dirt emitter, moves, and expires.</summary>
        /// <returns>Frame boundaries for actual original dirt pixels and an independent XML emitter comparison.</returns>
        [UnityTest]
        public IEnumerator LandPlantingUsesOriginalDirtEmitterAndExpires()
        {
            yield return CheckPlanting(false);
        }

        /// <summary>Checks a real lily-pad planting draws the original water emitter at its native additional offset.</summary>
        /// <returns>Frame boundaries for actual original water pixels and an independent XML emitter comparison.</returns>
        [UnityTest]
        public IEnumerator PoolPlantingUsesOriginalWaterEmitterAndExpires()
        {
            yield return CheckPlanting(true);
        }

        /// <summary>Compares the production effect with the unchanged original XML at the original PC planting coordinates.</summary>
        /// <param name="pool">Whether the plant is placed in a pool row.</param>
        /// <returns>Frame boundaries for visible, advancing, and expired production particles.</returns>
        private IEnumerator CheckPlanting(bool pool)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = pool ? 29 : 19 }, 7529);
            _owner._game = game;
            Assert.That(game.ChooseAdventureSeeds(new[]
            {
                PlantKind.Sunflower, PlantKind.Peashooter, PlantKind.Wallnut, PlantKind.PotatoMine,
                PlantKind.SnowPea, pool ? PlantKind.LilyPad : PlantKind.PuffShroom
            }), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            yield return Capture(() => { });
            var empty = _pixels.GetPixels32();
            var row = pool ? 2 : 1;
            Assert.That(game.TryPlant(pool ? PlantKind.LilyPad : PlantKind.Sunflower, row, 3), Is.True);
            Assert.That(game.Presentation.PlantingEffects.Count, Is.EqualTo(1));
            game.Update(.05f);
            var effect = game.Presentation.PlantingEffects[0];
            Assert.That(effect.Pool, Is.EqualTo(pool));
            yield return Capture(DrawProduction);
            var first = _pixels.GetPixels32();
            Assert.That(DifferentPixels(empty, first), Is.GreaterThan(20), "Successful planting must produce actual original particle pixels.");

            var reference = new OriginalParticleRenderer(_root, _animations);
            var asset = pool ? "PlantingPool" : "Planting";
            // PC Board::PlayPlantingEffects 0x418AB0: column origin+41, row origin+74, plus 15 for LilyPad.
            var origin = new Vector2(321f, pool ? 339f : 254f);
            yield return Capture(() =>
            {
                reference.BeginFrame();
                Assert.That(reference.Draw("planting-original-reference", asset, effect.Age, origin, effect.Seed), Is.True,
                    reference.LastError);
                reference.EndFrame();
            });
            Assert.That(DifferentPixels(first, _pixels.GetPixels32()), Is.LessThan(5),
                "Production must use the original emitter XML, atlas frames, native placement, and effect age.");

            game.Update(.1f);
            yield return Capture(DrawProduction);
            var moved = _pixels.GetPixels32();
            Assert.That(DifferentPixels(empty, moved), Is.GreaterThan(10));
            Assert.That(DifferentPixels(first, moved), Is.GreaterThan(20), "Original velocities and gravity must visibly advance the burst.");
            game.Update(.5f);
            Assert.That(game.Presentation.PlantingEffects, Is.Empty);
            yield return Capture(DrawProduction);
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.Zero,
                "Expired planting effects must leave no retained particle artwork.");
            reference.Clear();
        }

        /// <summary>Runs the same particle presentation boundaries and planting draw operation as the production board.</summary>
        private void DrawProduction()
        {
            InvokePresenter("BeginAdventurePresentation");
            try
            {
                InvokePresenter("DrawAdventurePlantingEffects");
            }
            finally
            {
                InvokePresenter("EndAdventurePresentation");
            }
        }

        /// <summary>Invokes a production drawing hook without expanding its runtime visibility.</summary>
        /// <param name="name">The private presenter operation to invoke.</param>
        /// <exception cref="TargetInvocationException">The production operation throws.</exception>
        private void InvokePresenter(string name)
        {
            typeof(GamePresenter).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null);
        }

        /// <summary>Captures one original logical scene operation over the same plain Canvas background.</summary>
        /// <param name="draw">The production or original-reference operation to capture.</param>
        /// <returns>Frame boundaries allowing real scene and Canvas geometry to render.</returns>
        private IEnumerator Capture(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = _viewport;
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
            _animations.BeginObjectFrame();
            try
            {
                draw();
            }
            finally
            {
                _animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            yield return null;
            yield return null;
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Counts materially changed RGB pixels between two actual camera captures.</summary>
        /// <param name="first">The first completed camera image.</param>
        /// <param name="second">The second completed camera image.</param>
        /// <returns>The number of pixels whose combined RGB difference exceeds the readback tolerance.</returns>
        private static int DifferentPixels(Color32[] first, Color32[] second)
        {
            var count = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 12)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
