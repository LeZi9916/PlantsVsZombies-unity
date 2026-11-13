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
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks real original Puff-shroom shadow and muzzle artwork through the production presenter.</summary>
    public sealed class OriginalPuffShroomTests
    {
        /// <summary>Stores the explicit original resource directory selected by the test operator.</summary>
        private string _root = "";
        /// <summary>Owns the inactive coordinator used only by this graphics fixture.</summary>
        private GameObject _host = null!;
        /// <summary>Supplies real board and texture state to the production presenter.</summary>
        private PvZGame _owner = null!;
        /// <summary>Submits the actual native-format animation and original sprite resources.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Runs the production plant and projectile drawing paths.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives the actual scene camera and interleaved Canvas output.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives the last complete rendered frame for pixel comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the camera target owned by the surrounding Unity test runner.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Stores the logical-to-screen scale used by the original 800x600 viewport.</summary>
        private float _scale;
        /// <summary>Stores the viewport's horizontal and vertical letterbox offsets.</summary>
        private Vector2 _offset;

        /// <summary>Creates a uniquely owned real-asset graphics fixture without starting game lifecycle or disk-backed profiles.</summary>
        /// <returns>A frame boundary allowing the render pipeline to finish initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(_root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            _host = new GameObject("Inactive original Puff-shroom graphics fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _animations = new ReanimationRenderer(_root);
            _presenter = new GamePresenter(_owner);
            _owner._root = _root;
            _owner._animations = _animations;
            _owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
        }

        /// <summary>Releases only textures, sprites, and objects owned by this explicit graphics fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction to finish.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                foreach (var texture in _owner.Textures._textures.Values)
                {
                    UnityEngine.Object.Destroy(texture);
                }

                _owner.Textures._textures.Clear();
                _presenter.Dispose();
                _animations.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
                UnityEngine.Object.Destroy(_host);
            }

            yield return null;
        }

        /// <summary>Checks the original night shadow remains inside its native half-size rectangle.</summary>
        /// <returns>Frame boundaries for actual production plant rendering and pixel readback.</returns>
        [UnityTest]
        public IEnumerator NightPuffUsesOriginalHalfSizeShadow()
        {
            yield return CheckShadow(11, "plantshadow2.png", "puff-night-shadow.png");
        }

        /// <summary>Checks the original daytime shadow uses the day artwork at the same native dimensions.</summary>
        /// <returns>Frame boundaries for actual production plant rendering and pixel readback.</returns>
        [UnityTest]
        public IEnumerator DayPuffUsesOriginalHalfSizeShadow()
        {
            yield return CheckShadow(9, "plantshadow.png", "puff-day-shadow.png");
        }

        /// <summary>Checks original spore artwork leaves the native muzzle and grows over thirty native ticks.</summary>
        /// <returns>Frame boundaries for actual production projectile rendering and pixel readback.</returns>
        [UnityTest]
        public IEnumerator OriginalSporeUsesNativeMuzzleAndGrowth()
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            var plant = new Plant { Id = 81, Kind = PlantKind.PuffShroom, Row = 0, Column = 1, Health = 300f };
            var fire = typeof(GameSimulation).GetMethod("FireAdventurePea", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var shot = (Pea)fire.Invoke(_owner._game, new object[] { plant, 0, false, 20f })!;
            var draw = typeof(GamePresenter).GetMethod("DrawAdventureProjectile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            yield return Capture(() => draw.Invoke(_presenter, new object[] { shot }));
            var initial = ArtworkBounds();
            Save("puff-spore-initial.png");
            Assert.That(initial.width, Is.InRange(4f, 13f), "The original new spore should start at scale0.3.");
            Assert.That(initial.height, Is.InRange(3f, 12f));
            Assert.That(initial.center.x, Is.EqualTo(176f).Within(4f));
            Assert.That(initial.center.y, Is.EqualTo(133.5f).Within(4f), "The generic pea Y=cell+30 places the puff too high.");

            shot.PresentationNativeAge = .3f;
            shot.PresentationAge = .3f;
            yield return Capture(() => draw.Invoke(_presenter, new object[] { shot }));
            var grown = ArtworkBounds();
            Save("puff-spore-grown.png");
            Assert.That(grown.width, Is.GreaterThan(initial.width * 2f));
            Assert.That(grown.height, Is.GreaterThan(initial.height * 2f));
            Assert.That(grown.center.x, Is.EqualTo(176f).Within(4f));
            Assert.That(grown.center.y, Is.EqualTo(133.5f).Within(4f));
            Assert.That(grown.xMin, Is.GreaterThanOrEqualTo(158f));
            Assert.That(grown.xMax, Is.LessThanOrEqualTo(194f));
            Assert.That(grown.yMin, Is.GreaterThanOrEqualTo(118f));
            Assert.That(grown.yMax, Is.LessThanOrEqualTo(149f));
        }

        /// <summary>Checks the production presenter displays the original shooting frames before the first spore launches.</summary>
        /// <returns>Frame boundaries for native attack playback and camera comparisons.</returns>
        [UnityTest]
        public IEnumerator FirstAttackDisplaysOriginalWindupBeforeLaunching()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            _owner._game = game;
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var plant = (Plant)place.Invoke(game, new object[] { PlantKind.PuffShroom, 0, 1 })!;
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.PuffShroom, 0, 0);
            game.Zombies.Add(new Zombie
            {
                Id = 91,
                Kind = ZombieKind.Normal,
                Row = 0,
                X = AdventureZombieNativeRules.GridX(264f),
                Health = 270f,
                StartingHealth = 270f,
                FrozenRemaining = 1000f
            });
            game.Update(.01f);
            Assert.That(plant.PresentationShotAge, Is.Zero, "The first attack begins at the first original shooting frame.");
            Assert.That(game.Peas.Count, Is.Zero);
            game.Update(.14f);
            Assert.That(game.Peas.Count, Is.Zero, "Original shooting frames must be visible before projectile creation.");

            // Suppress the shared ground shadow so this comparison isolates the original body animation.
            _owner.Textures._textures["plantshadow2.png"] = null!;
            var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            yield return Capture(() => draw.Invoke(_presenter, new object[] { plant }));
            var actual = _pixels.GetPixels32();
            Save("puff-first-attack-windup.png");
            var origin = new Vector2(120f, 80f + AlmanacPortraitLayout.PlantHeightOffset(8));
            yield return Capture(() => _animations.DrawNativePlayback("Puffshroom", origin, 1f,
                plant.PresentationShotAge, "anim_shooting", false, 35f));
            Assert.That(ChangedPixels(actual, _pixels.GetPixels32()), Is.LessThan(10),
                "Production must select the original shooting clip at its original 35 fps before the first spore.");
            yield return Capture(() => _animations.DrawNativePlayback("Puffshroom", origin, 1f,
                plant.Age, "anim_idle", true, plant.PresentationRate));
            Assert.That(ChangedPixels(actual, _pixels.GetPixels32()), Is.GreaterThan(30),
                "The first windup must visibly differ from the idle body pose.");
            game.Update(.14f);
            Assert.That(game.Peas.Count, Is.EqualTo(1), "The first spore follows the full twenty-eight-tick body windup.");
        }

        /// <summary>Checks the Fume-shroom body animation and its instant area damage use the original attack clock.</summary>
        /// <returns>Frame boundaries for native attack playback and camera comparison.</returns>
        [UnityTest]
        public IEnumerator FumeAttackAnimatesBeforeInstantAreaDamage()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            _owner._game = game;
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var plant = (Plant)place.Invoke(game, new object[] { PlantKind.FumeShroom, 0, 1 })!;
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.FumeShroom, 0, 0);
            var zombie = new Zombie
            {
                Id = 92,
                Kind = ZombieKind.Normal,
                Row = 0,
                X = AdventureZombieNativeRules.GridX(264f),
                Health = 270f,
                StartingHealth = 270f,
                FrozenRemaining = 1000f
            };
            game.Zombies.Add(zombie);
            game.Update(.01f);
            game.Update(.14f);
            Assert.That(zombie.Health, Is.EqualTo(270f));
            Assert.That(game.Peas.Count, Is.Zero);

            _owner.Textures._textures["plantshadow2.png"] = null!;
            var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            yield return Capture(() => draw.Invoke(_presenter, new object[] { plant }));
            var actual = _pixels.GetPixels32();
            var origin = new Vector2(120f, 80f + AlmanacPortraitLayout.PlantHeightOffset(10));
            yield return Capture(() => _animations.DrawNativePlayback("Fumeshroom", origin, 1f,
                plant.PresentationShotAge, "anim_shooting", false, 35f));
            Assert.That(ChangedPixels(actual, _pixels.GetPixels32()), Is.LessThan(10),
                "Fume-shroom must display its original body shooting clip before damage.");

            game.Update(.35f);
            Assert.That(zombie.Health, Is.EqualTo(250f), "The original firing tick immediately damages the entire row area.");
            Assert.That(game.Peas.Count, Is.Zero, "Fume-shroom does not create a travelling projectile.");
        }

        /// <summary>Checks the production Squash shows its original look, launch, moving jump, and retained flattened body.</summary>
        /// <returns>Frames comparing each production attack stage with the original authored reanimation.</returns>
        [UnityTest]
        public IEnumerator SquashUsesOriginalLookLaunchFlightAndFlattenedArtwork()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
            _owner._game = game;
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            _owner.Textures._textures["plantshadow.png"] = null!;
            _owner.Textures._textures["plantshadow2.png"] = null!;
            var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.Squash, 1, 4 })!;
            game.Zombies.Add(new Zombie
            {
                Id = 1000, Kind = ZombieKind.Normal, Row = 1,
                X = AdventureZombieNativeRules.GridX(390), Health = 270f, StartingHealth = 270f,
                FrozenRemaining = 100f
            });
            game.Update(.01f);
            game.Update(.2f);
            Assert.That(plant.SquashPresentation!.Animation, Is.EqualTo("anim_lookright"));
            yield return CompareOriginalSquashPose(plant);
            game.Update(.6f);
            game.Update(.21f);
            Assert.That(plant.SquashPresentation.Animation, Is.EqualTo("anim_jumpup"));
            yield return CompareOriginalSquashPose(plant);
            game.Update(.24f);
            game.Update(.3f);
            Assert.That(plant.SquashPresentation.NativePhase, Is.EqualTo(5));
            Assert.That(plant.SquashPresentation.NativeY, Is.EqualTo(68));
            yield return CompareOriginalSquashPose(plant);
            game.Update(.2f);
            game.Update(.05f);
            Assert.That(plant.SquashPresentation.Animation, Is.EqualTo("anim_jumpdown"));
            Assert.That(plant.SquashPresentation.NativeY, Is.EqualTo(128));
            yield return CompareOriginalSquashPose(plant);
            game.Update(.15f);
            Assert.That(plant.SquashPresentation.NativePhase, Is.EqualTo(7));
            Assert.That(game.Plants, Has.Member(plant));
            yield return CompareOriginalSquashPose(plant);
        }

        /// <summary>Compares one actual Squash body with its direct original animation at the native moving origin.</summary>
        /// <param name="plant">The live Squash whose current native state is being rendered.</param>
        /// <returns>Frames rendering actual production and unmodified original source artwork.</returns>
        private IEnumerator CompareOriginalSquashPose(Plant plant)
        {
            var state = plant.SquashPresentation!;
            _animations.ResetNativePlayback();
            yield return Capture(() => typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_presenter, new object[] { plant }));
            var actual = _pixels.GetPixels32();
            yield return Capture(() => Assert.That(_animations.DrawNativePlayback("Squash",
                new Vector2(state.NativeX, state.NativeY), 1f, state.SampleSeconds(0f),
                state.Animation, false, state.Rate), Is.True, _animations.LastError));
            Assert.That(ChangedPixels(actual, _pixels.GetPixels32()), Is.LessThan(10),
                "Squash must retain the original " + state.Animation + " artwork through native phase " + state.NativePhase + ".");
        }

        /// <summary>Checks the production presenter displays each original Scaredy-shroom fear and recovery body.</summary>
        /// <returns>Frame boundaries for actual original artwork rendering and reference comparisons.</returns>
        [UnityTest]
        public IEnumerator ScaredyUsesOriginalLoweringScaredIdleAndRaisingClips()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            _owner._game = game;
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            _owner.Textures._textures["plantshadow.png"] = null!;
            _owner.Textures._textures["plantshadow2.png"] = null!;
            var state = new AdventureScaredyShroomState();
            var plant = new Plant
            {
                Id = 83,
                Kind = PlantKind.ScaredyShroom,
                Row = 0,
                Column = 1,
                Health = 300f,
                PresentationRate = 12f,
                ScaredyShroomPresentation = state
            };
            game.Plants.Add(plant);
            var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var origin = new Vector2(120f, 80f + AlmanacPortraitLayout.PlantHeightOffset(13));
            state.Tick(true, false, 0f);
            for (var phase = 0; phase < 3; phase++)
            {
                for (var tick = 0; tick < 30; tick++)
                {
                    state.Tick(phase != 2, false, .5f);
                }

                Assert.That(state.Animation, Is.EqualTo(phase == 0 ? "anim_scared" : phase == 1 ? "anim_scaredidle" : "anim_grow"));
                yield return Capture(() => draw.Invoke(_presenter, new object[] { plant }));
                var actual = _pixels.GetPixels32();
                Save("scaredy-body-" + phase + ".png");
                yield return Capture(() => _animations.DrawNativePlayback("ScaredyShroom", origin, 1f,
                    state.SampleSeconds(0f), state.Animation, state.Loop, state.Rate));
                Assert.That(ChangedPixels(actual, _pixels.GetPixels32()), Is.LessThan(10),
                    "Production must select the original " + state.Animation + " artwork and native rate.");

                if (phase == 0)
                {
                    for (var tick = 0; tick < 100; tick++)
                    {
                        state.Tick(true, false, 0f);
                    }
                }
                else if (phase == 1)
                {
                    state.Tick(false, false, .5f);
                }
            }
        }

        /// <summary>Counts visible camera pixel differences while tolerating negligible channel rounding.</summary>
        /// <param name="first">The production camera frame.</param>
        /// <param name="second">The reference camera frame.</param>
        /// <returns>The number of pixels with a visible difference.</returns>
        private static int ChangedPixels(Color32[] first, Color32[] second)
        {
            var changed = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 12)
                {
                    changed++;
                }
            }

            return changed;
        }

        /// <summary>Compares a real plant draw with and without only its cached shadow to isolate visible ground pixels.</summary>
        /// <param name="level">The day or night adventure terrain used by the fixture.</param>
        /// <param name="shadowName">The original shadow resource expected on that terrain.</param>
        /// <param name="screenshot">The optional ignored diagnostic screenshot name.</param>
        /// <returns>Frame boundaries for real Canvas, original animation, and camera readback.</returns>
        private IEnumerator CheckShadow(int level, string shadowName, string screenshot)
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            var plant = new Plant
            {
                Id = 71,
                Kind = PlantKind.PuffShroom,
                Row = 0,
                Column = 1,
                Health = 300f,
                Age = .5f,
                PresentationRate = 12f,
                Asleep = level == 9
            };
            _owner._game.Plants.Add(plant);
            var method = typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Action draw = () => method.Invoke(_presenter, new object[] { plant });
            yield return Capture(draw);
            yield return Capture(draw);
            var withShadow = _pixels.GetPixels32();
            Save(screenshot);
            Assert.That(_owner.Textures._textures.TryGetValue(shadowName, out var shadow), Is.True, "Production must use the terrain's original shadow image.");
            Assert.That(shadow, Is.Not.Null);
            _owner.Textures._textures[shadowName] = null!;
            yield return Capture(draw);
            _owner.Textures._textures[shadowName] = shadow;
            var withoutShadow = _pixels.GetPixels32();
            var expected = new Rect(138.5f, 136f, 43f, 18f);
            var tolerance = new Rect(expected.x - 2f, expected.y - 2f, expected.width + 4f, expected.height + 4f);
            var shadowPixels = 0;
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var index = y * _pixels.width + x;
                    var first = withShadow[index];
                    var second = withoutShadow[index];
                    var difference = Math.Abs(first.r - second.r) + Math.Abs(first.g - second.g) + Math.Abs(first.b - second.b);
                    if (difference > 12)
                    {
                        var logical = ToLogical(x, y);
                        Assert.That(tolerance.Contains(logical), Is.True,
                            "Puff shadow extends outside original43x18 rectangle at " + logical + ".");
                        shadowPixels++;
                    }
                }
            }

            Assert.That(shadowPixels, Is.GreaterThan(10), "The original shadow must produce visible ground pixels.");
        }

        /// <summary>Submits one production draw over a flat test background and captures its actual camera pixels.</summary>
        /// <param name="draw">The production presenter operation under test.</param>
        /// <returns>Frame boundaries allowing the Canvas and scene camera to publish their geometry.</returns>
        private IEnumerator Capture(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0), Quaternion.identity, new Vector3(_scale, _scale, 1f));
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(0, 0, 800, 600), Texture2D.whiteTexture);
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
                _pixels.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Finds the nonwhite original projectile artwork in logical board coordinates.</summary>
        /// <returns>The bounding rectangle of visible original spore pixels.</returns>
        private Rect ArtworkBounds()
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var pixels = _pixels.GetPixels32();
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var point = ToLogical(x, y);
                    var pixel = pixels[y * _pixels.width + x];
                    if (point.x >= 0 && point.x < 800 && point.y >= 0 && point.y < 600
                        && 765 - pixel.r - pixel.g - pixel.b > 24)
                    {
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }
                }
            }

            Assert.That(min.x, Is.LessThan(float.MaxValue), "The original projectile must produce visible camera pixels.");
            return Rect.MinMaxRect(min.x, min.y, max.x + 1f / _scale, max.y + 1f / _scale);
        }

        /// <summary>Maps a bottom-left readback pixel into the original top-left logical board viewport.</summary>
        /// <param name="x">The readback pixel X coordinate.</param>
        /// <param name="y">The readback pixel Y coordinate.</param>
        /// <returns>The corresponding original logical board position.</returns>
        private Vector2 ToLogical(int x, int y)
        {
            return new Vector2((x - _offset.x) / _scale, (_pixels.height - y - 1 - _offset.y) / _scale);
        }

        /// <summary>Writes optional full-resolution diagnostics into the explicitly selected ignored output directory.</summary>
        /// <param name="name">The diagnostic screenshot filename.</param>
        private void Save(string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name), _pixels.EncodeToPNG());
            }
        }
    }
}
