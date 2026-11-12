#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original screen emitters, Doom preparation and crater artwork through the production renderer.</summary>
    public sealed class OriginalScreenEffectGraphicsTests
    {
        /// <summary>Stores the explicitly selected read-only original resource directory.</summary>
        private string _root = "";
        /// <summary>Renders the read-only original particle definitions and artwork.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>Owns the original particle systems whose native ages the tests advance.</summary>
        private OriginalParticleRenderer _particles = null!;
        /// <summary>Receives the actual game camera output.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives the actual camera pixels for color comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the surrounding runner's camera render target.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Restores the surrounding runner's camera clear color.</summary>
        private Color _previousClearColor;
        /// <summary>Maps the original fixed viewport to the actual test surface.</summary>
        private Matrix4x4 _viewport;

        /// <summary>Creates a real-artwork fixture without starting gameplay or accessing a profile.</summary>
        /// <returns>A frame boundary allowing render pipeline initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(_root, "particles")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            Assert.That(File.Exists(Path.Combine(_root, "particles", "IceTrap.xml")), Is.True);
            Assert.That(File.Exists(Path.Combine(_root, "particles", "Doom.xml")), Is.True);
            yield return null;
            _renderer = new ReanimationRenderer(_root);
            _particles = new OriginalParticleRenderer(_root, _renderer);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            _previousClearColor = ObjectSpriteRenderer.SceneCamera.backgroundColor;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            ObjectSpriteRenderer.SceneCamera.backgroundColor = Color.black;
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _viewport = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f,
                (Screen.height - 600f * scale) * .5f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        /// <summary>Releases only render targets and resources owned by this isolated fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_renderer != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                ObjectSpriteRenderer.SceneCamera.backgroundColor = _previousClearColor;
                _particles.Clear();
                _renderer.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
            }

            yield return null;
        }

        /// <summary>Checks the original cyan half-opacity flash covers every viewport corner and fades within thirty ticks.</summary>
        /// <returns>Frame boundaries for the original ice emitter's visible, fading, and expired samples.</returns>
        [UnityTest]
        public IEnumerator IceTrapUsesOriginalFullscreenColorAndLifetime()
        {
            yield return Capture("IceTrap", 0f);
            AssertViewport(new Color(.25f, .5f, .5f), .025f);
            yield return Capture("IceTrap", .15f);
            AssertViewport(new Color(.125f, .25f, .25f), .035f);
            yield return Capture("IceTrap", .4f);
            AssertViewport(Color.black, .01f);
        }

        /// <summary>Checks DoomFlash starts white, retains its original red-purple fade, and ends after fifty ticks.</summary>
        /// <returns>Frame boundaries for the original Doom emitter's visible, colored, and expired samples.</returns>
        [UnityTest]
        public IEnumerator DoomFlashUsesOriginalWhiteAndColoredFadeAcrossViewport()
        {
            yield return Capture("Doom", 0f);
            AssertViewport(Color.white, .01f);
            yield return Capture("Doom", .25f);
            AssertViewport(new Color(.4f, .25f, .35f), .035f);
            yield return Capture("Doom", .5f);
            AssertViewport(Color.black, .01f);
        }

        /// <summary>Checks the original Doom stem and base retain their native placement, dimensions, and bounded shake.</summary>
        /// <returns>Frame boundaries for isolated original-artwork captures at selected native ages.</returns>
        [UnityTest]
        public IEnumerator DoomStemAndBaseKeepOriginalCentersDimensionsAndBoundedShake()
        {
            var temporaryRoot = Path.Combine(Application.temporaryCachePath, "PvZDoomGeometry-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(temporaryRoot, "particles"));
            var original = new XmlDocument { XmlResolver = null };
            original.LoadXml("<Particles>" + File.ReadAllText(Path.Combine(_root, "particles", "Doom.xml")) + "</Particles>");
            var stem = original.SelectSingleNode("/Particles/Emitter[Name='DoomStem']");
            var ring = original.SelectSingleNode("/Particles/Emitter[Name='DoomBase']");
            Assert.That(stem, Is.Not.Null);
            Assert.That(ring, Is.Not.Null);
            File.WriteAllText(Path.Combine(temporaryRoot, "particles", "DoomStemAndBase.xml"), stem!.OuterXml + ring!.OuterXml);
            var particles = new OriginalParticleRenderer(temporaryRoot, _renderer);
            var origin = new Vector2(400f, 400f);
            try
            {
                // PC 0x52B6C0 updates each newborn immediately, with previous particle time -1.
                // The untouched XML uses 150-tick particles, Position Y35->15, and Shake2->0.
                for (var tick = 0; tick < 150; tick++)
                {
                    ObjectSpriteRenderer.BeginSceneFrame();
                    SpriteDrawState.Matrix = _viewport;
                    _renderer.BeginObjectFrame();
                    particles.BeginFrame();
                    try
                    {
                        Assert.That(particles.Draw("doom-stem-base", "DoomStemAndBase", (tick + .99f) * .01f, origin, 711),
                            Is.True, particles.LastError);
                    }
                    finally
                    {
                        particles.EndFrame();
                        _renderer.EndObjectFrame();
                    }

                    var time = tick / 149f;
                    var expectedStem = new Vector2(0f, -75f + 35f - 20f * time);
                    var stemState = ParticleState(particles, "DoomStem");
                    var ringState = ParticleState(particles, "DoomBase");
                    Assert.That(stemState.Time, Is.EqualTo(time).Within(.00001f));
                    Assert.That(ringState.Time, Is.EqualTo(time).Within(.00001f));
                    AssertParticleCenter(stemState.Position, expectedStem, 2f * (1f - time), "DoomStem tick " + tick);
                    AssertParticleCenter(ringState.Position, new Vector2(-4f, 10f), 2f * (1f - time), "DoomBase tick " + tick);
                    if (tick < 149)
                    {
                        // Mesh interpolation retains the previous native position until the next update fraction.
                        var positionTime = Math.Max(0f, tick - .01f) / 149f;
                        var previousTime = Math.Max(0f, tick - 1f) / 149f;
                        var shakeBound = 2f * (1f - previousTime);
                        var scale = 1.15f - .25f * time;
                        var stretch = .6f + .4f * time;
                        AssertParticleQuad("IMAGE_DOOMSHROOM_EXPLOSION_STEM",
                            origin + new Vector2(0f, -75f + 35f - 20f * positionTime),
                            new Vector2(136f * scale, 229f * scale * stretch), shakeBound);
                        AssertParticleQuad("IMAGE_DOOMSHROOM_EXPLOSION_BASE", origin + new Vector2(-4f, 10f),
                            new Vector2(157f, 85f), shakeBound);
                    }

                    if (tick == 0 || tick == 50 || tick == 100)
                    {
                        yield return ReadFrame();
                        AssertVisible(_pixels.GetPixels32());
                        var output = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
                        if (!string.IsNullOrEmpty(output))
                        {
                            Directory.CreateDirectory(output);
                            File.WriteAllBytes(Path.Combine(output, "doom-stem-base-" + tick.ToString("D3") + ".png"), _pixels.EncodeToPNG());
                        }
                    }
                }
            }
            finally
            {
                particles.Clear();
                Directory.Delete(temporaryRoot, true);
            }
        }

        /// <summary>Reads the native particle state rather than inferring its motion from the renderer under test.</summary>
        /// <param name="particles">The production evaluator containing the isolated original emitters.</param>
        /// <param name="emitterName">The original XML emitter whose one particle is required.</param>
        /// <returns>The particle's actual relative position and last evaluated native time.</returns>
        /// <exception cref="AssertionException">The original particle or emitter is absent.</exception>
        private static (Vector2 Position, float Time) ParticleState(OriginalParticleRenderer particles, string emitterName)
        {
            var systems = (IDictionary)typeof(OriginalParticleRenderer)
                .GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(particles)!;
            var system = systems["doom-stem-base"]!;
            var emitters = (IList)system.GetType().GetField("Emitters")!.GetValue(system)!;
            foreach (var emitter in emitters)
            {
                var definition = emitter!.GetType().GetField("Definition")!.GetValue(emitter)!;
                if ((string)definition.GetType().GetField("Name")!.GetValue(definition)! != emitterName)
                {
                    continue;
                }

                var live = (IList)emitter.GetType().GetField("Particles")!.GetValue(emitter)!;
                Assert.That(live.Count, Is.EqualTo(1), emitterName);
                var particle = live[0]!;
                return ((Vector2)particle.GetType().GetField("Position")!.GetValue(particle)!,
                    (float)particle.GetType().GetField("Time")!.GetValue(particle)!);
            }

            throw new AssertionException("Missing original emitter: " + emitterName);
        }

        /// <summary>Checks that an original particle's motion differs from its authored baseline only by its current shake.</summary>
        /// <param name="actual">The evaluated or rendered center.</param>
        /// <param name="baseline">The independent original XML position curve.</param>
        /// <param name="shake">The largest original jitter allowed on either axis at this age.</param>
        /// <param name="context">The emitter and age identifying the assertion.</param>
        private static void AssertParticleCenter(Vector2 actual, Vector2 baseline, float shake, string context)
        {
            Assert.That(actual.x, Is.EqualTo(baseline.x).Within(shake + .002f), context + " X must not perform a random walk.");
            Assert.That(actual.y, Is.EqualTo(baseline.y).Within(shake + .002f), context + " Y must retain the initial position field.");
        }

        /// <summary>Checks actual original texture quads against independent native atlas-cell dimensions and placement.</summary>
        /// <param name="resource">The original native texture resource identifying the submitted quad.</param>
        /// <param name="center">The independent original center before bounded shake.</param>
        /// <param name="size">The original atlas-cell dimensions after XML scale and stretch.</param>
        /// <param name="shake">The allowed native shake displacement on each axis.</param>
        /// <exception cref="AssertionException">The original texture quad was not submitted.</exception>
        private void AssertParticleQuad(string resource, Vector2 center, Vector2 size, float shake)
        {
            var textures = (IDictionary)typeof(ReanimationRenderer)
                .GetField("_texturesById", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
            var texture = (Texture)textures[resource]!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer)
                .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
            var sprites = (IList)typeof(ObjectSpriteRenderer)
                .GetField("_sprites", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pool)!;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = (ObjectSprite)sprites[index]!;
                if (sprite.Properties.GetTexture(Shader.PropertyToID("_MainTex")) != texture)
                {
                    continue;
                }

                var first = _viewport.inverse.MultiplyPoint3x4(new Vector3(sprite.Vertices[0].x, -sprite.Vertices[0].y, 0f));
                var last = _viewport.inverse.MultiplyPoint3x4(new Vector3(sprite.Vertices[2].x, -sprite.Vertices[2].y, 0f));
                AssertParticleCenter((first + last) * .5f, center, shake, resource);
                Assert.That(last.x - first.x, Is.EqualTo(size.x).Within(.002f), resource + " native width");
                Assert.That(last.y - first.y, Is.EqualTo(size.y).Within(.002f), resource + " native height");
                return;
            }

            throw new AssertionException("Missing original particle quad: " + resource);
        }

        /// <summary>Checks production presentation moves both the lawn and HUD by the native board offset and restores the viewport.</summary>
        /// <returns>Frame boundaries for stationary, independent translated, and production shake captures.</returns>
        [UnityTest]
        public IEnumerator ProductionBoardAndHudShareShakeAndRestoreViewport()
        {
            var host = new GameObject("Inactive original board shake fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var options = new OptionsView(_root);
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = _root;
                owner._animations = _renderer;
                owner._optionsView = options;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._smokeMode = true;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._deck.Clear();
                presenter.SetPointerPosition(new Vector2(-1000f, -1000f));
                presenter.Present();
                yield return ReadFrame();
                var stationary = _pixels.GetPixels32();

                // Independent PC widget translation: half of the twelve-tick bounce reaches (3, -4).
                var drawBoard = typeof(GamePresenter).GetMethod("DrawBoard", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var drawInterface = typeof(GamePresenter).GetMethod("DrawInterface", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ObjectSpriteRenderer.BeginSceneFrame();
                SpriteDrawState.Clip = GameViewport.ScreenRect;
                CanvasUI.BeginFrame();
                CanvasUI.Matrix = _viewport * Matrix4x4.Translate(new Vector3(3f, -4f, 0f));
                _renderer.BeginObjectFrame();
                try
                {
                    drawBoard.Invoke(presenter, null);
                    drawInterface.Invoke(presenter, null);
                }
                finally
                {
                    _renderer.EndObjectFrame();
                    CanvasUI.EndFrame();
                }

                yield return ReadFrame();
                var translated = _pixels.GetPixels32();
                AssertRegionChanged(stationary, translated, new Rect(0f, 0f, 700f, 85f));
                AssertRegionChanged(stationary, translated, new Rect(100f, 120f, 600f, 420f));

                owner._game.Presentation.ScreenShake.Start(3, -4);
                for (var tick = 0; tick < 6; tick++)
                {
                    owner._game.Presentation.ScreenShake.Tick(false);
                }

                presenter.Present();
                Assert.That(CanvasUI.Matrix, Is.EqualTo(_viewport));
                yield return ReadFrame();
                var production = _pixels.GetPixels32();
                var differences = 0;
                for (var index = 0; index < production.Length; index++)
                {
                    if (ColorDifference(production[index], translated[index]) > 12)
                    {
                        differences++;
                    }
                }

                Assert.That(differences, Is.LessThan(5), "Production must apply the native offset to the complete lawn and HUD.");
            }
            finally
            {
                presenter.Dispose();
                options.Dispose();
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

        /// <summary>Checks the original crater atlas cell, native placement, terrain changes and final fade.</summary>
        /// <returns>Frame boundaries comparing production craters with independently selected original image cells.</returns>
        [UnityTest]
        public IEnumerator ProductionCraterDrawsOneOriginalCellForTerrainAndAging()
        {
            var presenter = CreateBoardPresenter(out var host, out var owner);
            try
            {
                // PC GridItem::DrawCrater 0x458EA0: the source image always contains two columns.
                var cases = new (int Level, int Row, int Column, float Remaining, string Image, int Cell, float X, float Y, float Alpha)[]
                {
                    (Level: 9, Row: 1, Column: 4, Remaining: 180f, Image: "crater.png", Cell: 0, X: 352f, Y: 220f, Alpha: 1f),
                    (19, 1, 4, 180f, "crater.png", 1, 352f, 220f, 1f),
                    (9, 1, 4, 80f, "crater_fading.png", 0, 352f, 220f, 1f),
                    (19, 1, 4, .12f, "crater_fading.png", 1, 352f, 220f, 122f / 255f),
                    (49, 1, 3, 180f, "crater_roof_left.png", 0, 288f, 219f, 1f),
                    (49, 1, 6, 80f, "crater_roof_center.png", 1, 530f, 186f, 1f),
                    (29, 2, 4, 180f, "crater_water_day.png", 0, 352f, 290f, 1f),
                    (39, 2, 4, 80f, "crater_water_night.png", 1, 352f, 290f, 1f)
                };
                foreach (var sample in cases)
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = sample.Level }, 7529);
                    owner._game.Adventure._ticks = 23;
                    var crater = new AdventureCellTimer { Row = sample.Row, Column = sample.Column, Remaining = sample.Remaining };
                    yield return Capture(() => InvokePresenter(presenter, "DrawAdventureCrater", crater));
                    var production = _pixels.GetPixels32();
                    AssertVisible(production);
                    var texture = owner.Textures.Load(sample.Image);
                    Assert.That(texture, Is.Not.Null, sample.Image);
                    var y = sample.Y;
                    if (sample.Level == 29 || sample.Level == 39)
                    {
                        var phase = sample.Row * Mathf.PI + sample.Column * Mathf.PI * .25f + 23 * Mathf.PI * 2f / 200f;
                        y += (float)Math.Sin(phase) * 2f;
                    }

                    yield return Capture(() =>
                    {
                        SpriteDrawState.Color = new Color(1f, 1f, 1f, sample.Alpha);
                        Assert.That(_renderer.DrawNativeImage(texture!, new Rect(sample.X, y, texture!.width * .5f, texture.height),
                            new Rect(sample.Cell * .5f, 0f, .5f, 1f)), Is.True);
                    });
                    AssertEquivalent(production, "The original crater must draw exactly one native atlas cell: " + sample.Image);
                }
            }
            finally
            {
                DestroyBoardPresenter(presenter, host, owner);
            }
        }

        /// <summary>Checks the Doom-shroom uses its authored exploding pose and independent head shake before detonation.</summary>
        /// <returns>Frame boundaries comparing live production poses with independently drawn original tracks.</returns>
        [UnityTest]
        public IEnumerator DoomPreparationUsesOriginalExplodingAnimationAndHeadShake()
        {
            var presenter = CreateBoardPresenter(out var host, out var owner);
            try
            {
                var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
                owner._game = game;
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
                game.ResetAdventureWaveSchedule(100000, false, false);
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(game, new object[] { PlantKind.DoomShroom, 1, 4 })!;
                var shadow = owner.Textures.Load("plantshadow.png");
                Assert.That(shadow, Is.Not.Null);
                Color32[]? previous = null;
                foreach (var age in new[] { .25f, .75f })
                {
                    game.Update(previous == null ? .25f : .5f);
                    Assert.That(game.Plants.Contains(plant), Is.True);
                    Assert.That(game.Presentation.Explosions, Is.Empty);
                    var state = plant.DoomShroomPresentation!;
                    Assert.That(state, Is.Not.Null);
                    yield return Capture(() => InvokePresenter(presenter, "DrawAdventurePlant", plant));
                    var production = _pixels.GetPixels32();
                    AssertVisible(production);
                    if (previous != null)
                    {
                        AssertRegionChanged(previous, production, new Rect(340f, 140f, 140f, 140f));
                    }

                    previous = production;
                    yield return Capture(() =>
                    {
                        Assert.That(_renderer.DrawNativeImage(shadow!, new Rect(361f, 245f, 76f, 24f), new Rect(0f, 0f, 1f, 1f)), Is.True);
                        // PC UpdateDoomShroom 0x46F1A0: anim_explode at 23 fps, no pose blend.
                        _renderer.DrawNativePlayback("DoomShroom", new Vector2(360f, 180f), 1f, age, "anim_explode", false, 23f,
                            visible: track => !track.StartsWith("DoomShroom_head", StringComparison.OrdinalIgnoreCase));
                        for (var head = 0; head < 3; head++)
                        {
                            var track = "DoomShroom_head" + (head + 1);
                            // Independently move each original head's entire draw origin instead of using the production track-offset API.
                            var origin = new Vector2(360f + state.HeadShakeX[head], 180f + state.HeadShakeY[head]);
                            _renderer.DrawNativePlayback("DoomShroom", origin, 1f, age, "anim_explode", false, 23f,
                                visible: name => string.Equals(name, track, StringComparison.OrdinalIgnoreCase));
                        }
                    });
                    AssertEquivalent(production, "The live Doom-shroom must use the original preparing clip, timing and three independent head offsets.");
                }
            }
            finally
            {
                DestroyBoardPresenter(presenter, host, owner);
            }
        }

        /// <summary>Creates an inactive production presenter using only the explicitly selected original resources.</summary>
        /// <param name="host">Receives the inactive Unity object owned by this fixture.</param>
        /// <param name="owner">Receives the coordinator whose board is selected by each sample.</param>
        /// <returns>The production presenter configured to submit board images through the native scene renderer.</returns>
        private GamePresenter CreateBoardPresenter(out GameObject host, out PvZGame owner)
        {
            host = new GameObject("Inactive original Doom and crater fixture");
            host.SetActive(false);
            owner = host.AddComponent<PvZGame>();
            owner._root = _root;
            owner._animations = _renderer;
            var presenter = new GamePresenter(owner);
            typeof(GamePresenter).GetField("_renderingBoardObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(presenter, true);
            return presenter;
        }

        /// <summary>Releases the inactive production fixture and its original image cache.</summary>
        /// <param name="presenter">The fixture presenter owning retained interface data.</param>
        /// <param name="host">The inactive Unity object owned by the fixture.</param>
        /// <param name="owner">The coordinator whose local texture cache must be released.</param>
        private static void DestroyBoardPresenter(GamePresenter presenter, GameObject host, PvZGame owner)
        {
            presenter.Dispose();
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

        /// <summary>Invokes an existing private production drawing operation without expanding its runtime visibility.</summary>
        /// <param name="presenter">The production presenter under test.</param>
        /// <param name="name">The private drawing method.</param>
        /// <param name="arguments">The drawing method arguments.</param>
        /// <exception cref="TargetInvocationException">The production drawing method throws.</exception>
        private static void InvokePresenter(GamePresenter presenter, string name, params object[] arguments)
        {
            typeof(GamePresenter).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(presenter, arguments);
        }

        /// <summary>Captures one production or independent native image operation over the black camera clear color.</summary>
        /// <param name="draw">The native drawing operation.</param>
        /// <returns>Frame boundaries allowing scene rendering and camera readback.</returns>
        private IEnumerator Capture(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = _viewport;
            CanvasUI.Color = Color.white;
            _renderer.BeginObjectFrame();
            try
            {
                draw();
            }
            finally
            {
                _renderer.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            yield return ReadFrame();
        }

        /// <summary>Requires actual original artwork rather than a blank successful render.</summary>
        /// <param name="pixels">The production camera output.</param>
        private static void AssertVisible(Color32[] pixels)
        {
            var visible = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.r + pixel.g + pixel.b > 12)
                {
                    visible++;
                }
            }

            Assert.That(visible, Is.GreaterThan(30));
        }

        /// <summary>Compares a production image with the current independently submitted reference image.</summary>
        /// <param name="production">The production camera pixels.</param>
        /// <param name="message">The original drawing behavior that must match.</param>
        private void AssertEquivalent(Color32[] production, string message)
        {
            var reference = _pixels.GetPixels32();
            var differences = 0;
            for (var index = 0; index < production.Length; index++)
            {
                if (ColorDifference(production[index], reference[index]) > 12)
                {
                    differences++;
                }
            }

            Assert.That(differences, Is.LessThan(5), message);
        }

        /// <summary>Captures native particles at a deliberately offscreen origin to isolate the fullscreen emitter.</summary>
        /// <param name="name">The unchanged original emitter definition to evaluate.</param>
        /// <param name="age">The native effect age in seconds.</param>
        /// <returns>Frame boundaries allowing scene rendering and camera readback.</returns>
        private IEnumerator Capture(string name, float age)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Matrix = _viewport * Matrix4x4.Translate(new Vector3(7f, -5f, 0f));
            _renderer.BeginObjectFrame();
            _particles.BeginFrame();
            try
            {
                Assert.That(_particles.Draw("original-screen-effect", name, age, new Vector2(-2000f, 2000f), 711,
                    fullScreenMatrix: _viewport), Is.True, _particles.LastError);
            }
            finally
            {
                _particles.EndFrame();
                _renderer.EndObjectFrame();
            }

            yield return ReadFrame();
        }

        /// <summary>Reads the actual scene camera after submitted Canvas and scene geometry have rendered.</summary>
        /// <returns>Frame boundaries allowing scene rendering and camera readback.</returns>
        private IEnumerator ReadFrame()
        {
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

        /// <summary>Requires a visible native displacement in the selected original board region.</summary>
        /// <param name="stationary">The original unshifted camera pixels.</param>
        /// <param name="translated">The independently translated camera pixels.</param>
        /// <param name="region">The board or HUD region in original screen coordinates.</param>
        private void AssertRegionChanged(Color32[] stationary, Color32[] translated, Rect region)
        {
            var changed = 0;
            var topLeft = _viewport.MultiplyPoint3x4(new Vector3(region.xMin, region.yMin, 0f));
            var bottomRight = _viewport.MultiplyPoint3x4(new Vector3(region.xMax, region.yMax, 0f));
            for (var y = Mathf.CeilToInt(topLeft.y); y < bottomRight.y; y++)
            {
                for (var x = Mathf.CeilToInt(topLeft.x); x < bottomRight.x; x++)
                {
                    var index = (_pixels.height - 1 - y) * _pixels.width + x;
                    if (ColorDifference(stationary[index], translated[index]) > 12)
                    {
                        changed++;
                    }
                }
            }

            Assert.That(changed, Is.GreaterThan(30), "The original artwork must visibly move in " + region);
        }

        /// <summary>Measures the absolute RGB difference between two camera pixels.</summary>
        /// <param name="first">The first rendered pixel.</param>
        /// <param name="second">The second rendered pixel.</param>
        /// <returns>The combined red, green, and blue channel difference.</returns>
        private static int ColorDifference(Color32 first, Color32 second)
        {
            return Math.Abs(first.r - second.r) + Math.Abs(first.g - second.g) + Math.Abs(first.b - second.b);
        }

        /// <summary>Checks the original flash color at all viewport corners and the center.</summary>
        /// <param name="expected">The independently expected blend over the black camera clear color.</param>
        /// <param name="tolerance">The allowed channel difference for color conversion and native tick boundaries.</param>
        private void AssertViewport(Color expected, float tolerance)
        {
            foreach (var point in new[]
            {
                new Vector3(2f, 2f), new Vector3(798f, 2f), new Vector3(2f, 598f),
                new Vector3(798f, 598f), new Vector3(400f, 300f)
            })
            {
                var screen = _viewport.MultiplyPoint3x4(point);
                var actual = _pixels.GetPixel(Mathf.FloorToInt(screen.x), _pixels.height - 1 - Mathf.FloorToInt(screen.y));
                Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), "Original fullscreen red at " + point);
                Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), "Original fullscreen green at " + point);
                Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), "Original fullscreen blue at " + point);
            }
        }
    }
}
