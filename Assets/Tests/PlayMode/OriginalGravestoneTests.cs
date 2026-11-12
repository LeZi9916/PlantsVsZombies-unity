#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks real original gravestone and soil atlas rendering through the production presenter.</summary>
    public sealed class OriginalGravestoneTests
    {
        /// <summary>Owns the inactive coordinator without loading a user profile.</summary>
        private GameObject _host = null!;
        /// <summary>Provides real original textures and the isolated night board.</summary>
        private PvZGame _owner = null!;
        /// <summary>Submits original image pixels to the actual board camera.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Runs the production gravestone drawing operation.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives the actual scene camera and interleaved Canvas pixels.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives camera readback for comparison with independent original atlas crops.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the camera target owned by the surrounding test runner.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Scales the original 800 by 600 viewport to the current test surface.</summary>
        private float _scale;
        /// <summary>Stores the screen letterbox offset of the original viewport.</summary>
        private Vector2 _offset;

        /// <summary>Creates a real-asset graphics fixture with no game lifecycle or persistent data.</summary>
        /// <returns>A frame boundary allowing render pipeline initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "images")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            Assert.That(File.Exists(Path.Combine(root, "images", "Tombstones.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "images", "Tombstone_mounds.png")), Is.True);
            yield return null;
            _host = new GameObject("Inactive original gravestone graphics fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = root;
            _animations = new ReanimationRenderer(root);
            _owner._animations = _animations;
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            _presenter = new GamePresenter(_owner);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
        }

        /// <summary>Releases only textures, objects, and camera state owned by this graphics fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction.</returns>
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

        /// <summary>Checks preview suppression, original eighty-six by ninety-one geometry, soil, and all atlas row rules.</summary>
        /// <returns>Frame boundaries for the real production renderer and independent original atlas comparisons.</returns>
        [UnityTest]
        public IEnumerator PreviewHidesGravesAndEnabledBoardUsesNativeStoneAndSoilCells()
        {
            var grave = MatureGrave(0, 7, true);
            _owner.RequiredGame.Adventure.GravesEnabled = false;
            yield return Capture(() => DrawProduction(grave));
            var hidden = _pixels.GetPixels32();
            yield return Capture(() => { });
            AssertPixelsMatch(hidden, _pixels.GetPixels32(), "The zombie preview must not draw a gravestone or its soil.");

            _owner.RequiredGame.Adventure.GravesEnabled = true;
            var stone = _owner.Textures.RequiredLoad("Tombstones.png");
            var mound = _owner.Textures.RequiredLoad("Tombstone_mounds.png");
            Assert.That(stone, Is.Not.Null);
            Assert.That(mound, Is.Not.Null);
            Assert.That(stone.width, Is.EqualTo(430));
            Assert.That(stone.height, Is.EqualTo(364));
            Assert.That(mound.width, Is.EqualTo(430));
            Assert.That(mound.height, Is.EqualTo(364));

            // Original GridItem::DrawGraveStone chooses top row before special state,
            // then special row zero, or ordinary rows two and three by cell-look parity.
            var rows = new[] { 0, 1, 2, 3 };
            var looks = new[] { 7, 6, 14, 17 };
            var special = new[] { true, true, false, false };
            var sourceRows = new[] { 1, 0, 2, 3 };
            for (var index = 0; index < rows.Length; index++)
            {
                grave = MatureGrave(rows[index], looks[index], special[index]);
                yield return Capture(() => DrawProduction(grave));
                var actual = _pixels.GetPixels32();
                Save("grave-original-row-" + sourceRows[index] + ".png");
                var sourceColumn = looks[index] % 5;
                var sourceRow = sourceRows[index];
                yield return Capture(() => DrawOriginalReference(grave, stone, mound, sourceColumn, sourceRow, true));
                var expected = _pixels.GetPixels32();
                AssertPixelsMatch(actual, expected,
                    "The production grave must use both unscaled original 86x91 atlas cells for row " + sourceRow + ".");
                yield return Capture(() => DrawOriginalReference(grave, stone, mound, sourceColumn, sourceRow, false));
                Assert.That(DifferentPixels(expected, _pixels.GetPixels32()), Is.GreaterThan(20),
                    "The original soil must contribute visible pixels separately from the stone.");
                Assert.That(DifferentPixels(hidden, actual), Is.GreaterThan(100), "The enabled grave must produce actual camera artwork.");
            }
        }

        /// <summary>Checks original zombie body pixels stay underground and emerge through the native clipping boundary.</summary>
        /// <returns>Frame boundaries for actual original zombie graphics and camera readback.</returns>
        [UnityTest]
        public IEnumerator ZombieRisesFromGraveWithNativeClipping()
        {
            _owner.Textures._textures["plantshadow2.png"] = null!;
            var zombie = new Zombie
            {
                Id = 711,
                Kind = ZombieKind.Normal,
                Row = 1,
                X = AdventureZombieNativeRules.GridX(495f),
                Health = 270f,
                StartingHealth = 270f,
                GraveRiseTicks = 150,
                PresentationNativePhase = 14,
                PresentationAnimationAge = .2f
            };
            var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Action drawZombie = () => draw.Invoke(_presenter, new object[] { zombie });
            yield return Capture(() => { });
            var empty = _pixels.GetPixels32();
            yield return Capture(drawZombie);
            AssertPixelsMatch(empty, _pixels.GetPixels32(), "A newly created grave zombie must remain entirely underground.");

            zombie.GraveRiseTicks = 10;
            yield return Capture(drawZombie);
            var emerging = _pixels.GetPixels32();
            Save("grave-zombie-emerging.png");
            var visible = DifferentPixels(empty, emerging);
            Assert.That(visible, Is.GreaterThan(30), "The final fifty updates must reveal actual original zombie artwork.");
            for (var y = 0; y < _pixels.height; y++)
            {
                var logicalY = (_pixels.height - y - 1f - _offset.y) / _scale;
                if (logicalY <= 255f)
                {
                    continue;
                }

                for (var x = 0; x < _pixels.width; x++)
                {
                    var pixel = emerging[y * _pixels.width + x];
                    Assert.That(Math.Abs(255 - pixel.r) + Math.Abs(255 - pixel.g) + Math.Abs(255 - pixel.b), Is.LessThanOrEqualTo(12),
                        "The original phase-14 clipping must hide body pixels below native Y253.");
                }
            }

            zombie.GraveRiseTicks = 0;
            zombie.PresentationNativePhase = -1;
            yield return Capture(drawZombie);
            Save("grave-zombie-emerged.png");
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.GreaterThan(visible),
                "The complete original walking body becomes visible once the grave phase ends.");
        }

        /// <summary>Checks the imported original ZombieRise effect resolves all original emitter artwork.</summary>
        /// <returns>Frame boundaries for the original particle evaluator and camera readback.</returns>
        [UnityTest]
        public IEnumerator OriginalZombieRiseParticlesResolveTheirNativeArtwork()
        {
            var particles = new OriginalParticleRenderer(_owner._root, _animations);
            yield return Capture(() => { });
            var empty = _pixels.GetPixels32();
            yield return Capture(() =>
            {
                particles.BeginFrame();
                Assert.That(particles.Draw("grave-rise-fixture", "ZombieRise", .4f, new Vector2(555f, 260f), 711), Is.True);
                particles.EndFrame();
            });
            Assert.That(particles.LastError, Is.Null.Or.Empty);
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.GreaterThan(20),
                "Original grave-rise dirt and arm emitters must submit visible native artwork.");
            Save("grave-zombie-rise-particles.png");
        }

        /// <summary>Checks the real 2-5 board selects dirt-only Whack particles instead of the grave ambush's detached arm.</summary>
        /// <returns>Frame boundaries for original particle submission through the production board presenter.</returns>
        [UnityTest]
        public IEnumerator WhackRiseUsesOriginalDirtWithoutZombieArm()
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 15 }, 1570935);
            _owner._game.Update(2.03f);
            Assert.That(_owner._game.Presentation.ZombieRiseEffects.Count, Is.GreaterThan(0));
            _owner._game.Zombies.Clear();
            _owner._game.Mowers.Clear();
            _owner._game.Suns.Clear();
            _owner._game.Adventure._graves.Clear();
            _owner._choosingSeeds = true;
            foreach (var effect in _owner._game.Presentation.ZombieRiseEffects)
            {
                Assert.That(effect.Whack, Is.True);
                effect.Age = .4f;
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var drawBoard = typeof(GamePresenter).GetMethod("DrawAdventureBoard", flags)!;
            yield return Capture(() => drawBoard.Invoke(_presenter, null));
            var textures = (Dictionary<string, Texture2D>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(_animations)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(_animations)!;
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
            Assert.That(textures.ContainsKey("IMAGE_DIRTSMALL"), Is.True);
            Assert.That(textures.ContainsKey("IMAGE_DIRTBIG"), Is.True);
            Assert.That(textures.ContainsKey("IMAGE_ZOMBIEARM"), Is.False,
                "The original ZombieRise arm emitter belongs to ordinary grave ambushes, not Whack-a-Zombie.");
            Assert.That(pool.SubmittedObjectCount, Is.GreaterThan(0));
            var textureId = Shader.PropertyToID("_MainTex");
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var texture = sprites[index].Properties.GetTexture(textureId);
                Assert.That(texture == textures["IMAGE_DIRTSMALL"] || texture == textures["IMAGE_DIRTBIG"], Is.True,
                    "The isolated original Whack rise effect must submit only its two native dirt images.");
            }
        }

        /// <summary>Checks a real hammer kill renders its native Pow image through the production overlay layer.</summary>
        /// <returns>Frame boundaries for actual original impact artwork and camera readback.</returns>
        [UnityTest]
        public IEnumerator OriginalWhackPowParticlesResolveTheirNativeArtwork()
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 15 }, 1570935);
            var zombie = new Zombie
            {
                Id = 711,
                Kind = ZombieKind.Normal,
                Row = 2,
                X = 4f,
                Health = 270f,
                StartingHealth = 270f
            };
            _owner._game.Zombies.Add(zombie);
            Assert.That(_owner._game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.True);
            Assert.That(_owner._game.Presentation.Explosions.Count, Is.EqualTo(1));
            _owner._game.Presentation.Explosions[0].Ticks = 12;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var drawOverlay = typeof(GamePresenter).GetMethod("DrawAdventureHammerHits", flags)!;
            yield return Capture(() => { });
            var empty = _pixels.GetPixels32();
            yield return Capture(() =>
            {
                drawOverlay.Invoke(_presenter, null);
            });
            var particles = (OriginalParticleRenderer)typeof(GamePresenter).GetField("_hammerParticles", flags)!.GetValue(_presenter)!;
            Assert.That(particles.LastError, Is.Null.Or.Empty);
            var textures = (Dictionary<string, Texture2D>)typeof(ReanimationRenderer).GetField("_texturesById", flags)!.GetValue(_animations)!;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(_animations)!;
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
            Assert.That(pool.SubmittedObjectCount, Is.EqualTo(1));
            Assert.That(sprites[0].Properties.GetTexture(Shader.PropertyToID("_MainTex")), Is.EqualTo(textures["IMAGE_POW"]));
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.GreaterThan(20),
                "The original Pow must contribute visible hammer impact pixels.");
        }

        /// <summary>Creates an isolated fully emerged grave with fixed original appearance and no live particle tail.</summary>
        /// <param name="row">The lawn row containing the grave.</param>
        /// <param name="look">The original twenty-way cell-look value.</param>
        /// <param name="special">Whether this grave has the original special atlas state.</param>
        /// <returns>A grave whose finished appearance can be compared directly with the original image atlas.</returns>
        private static AdventureCellTimer MatureGrave(int row, int look, bool special)
        {
            var pose = new AdventureGravePresentation(look, 3, -2, 0) { Special = special };
            for (var tick = 0; tick < 200; tick++)
            {
                pose.Tick();
            }

            return new AdventureCellTimer { Row = row, Column = 3, Remaining = float.MaxValue, GravePresentation = pose };
        }

        /// <summary>Invokes the actual production helper with its normal scene-object submission path.</summary>
        /// <param name="grave">The isolated original grave to draw.</param>
        private void DrawProduction(AdventureCellTimer grave)
        {
            var field = typeof(GamePresenter).GetField("_renderingBoardObjects", BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(_presenter, true);
            try
            {
                typeof(GamePresenter).GetMethod("DrawAdventureGrave", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(_presenter, new object[] { grave });
            }
            finally
            {
                field.SetValue(_presenter, false);
            }
        }

        /// <summary>Draws literal original final-pose atlas rectangles independently of the production grave geometry helper.</summary>
        /// <param name="grave">The cell providing only the board row and column.</param>
        /// <param name="stone">The original five by four gravestone atlas.</param>
        /// <param name="mound">The original five by four soil atlas.</param>
        /// <param name="column">The expected original atlas column.</param>
        /// <param name="row">The expected original atlas row.</param>
        /// <param name="includeMound">Whether to overlay the original soil for comparison.</param>
        private void DrawOriginalReference(AdventureCellTimer grave, Texture stone, Texture mound, int column, int row, bool includeMound)
        {
            var x = 40f + grave.Column * 80f - 4f + 3f;
            // Local PC 0x458C84 offsets both layers by -9; 0x458CA3..B6 draws the full mound source height.
            var y = 80f + grave.Row * 100f - 2f - 9f;
            _animations.DrawNativeImage(stone, new Rect(x, y, 86f, 91f),
                new Rect(column / 5f, 1f - (row + 1f) / 4f, 1f / 5f, 1f / 4f));
            if (includeMound)
            {
                _animations.DrawNativeImage(mound, new Rect(x, y, 86f, 91f),
                    new Rect(column / 5f, 1f - (row + 1f) / 4f, 1f / 5f, 1f / 4f));
            }
        }

        /// <summary>Captures the actual board scene camera after submitting original artwork over a white background.</summary>
        /// <param name="draw">The production or independent reference drawing operation.</param>
        /// <returns>Frame boundaries allowing Canvas and the board camera to complete their submissions.</returns>
        private IEnumerator Capture(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0f), Quaternion.identity, new Vector3(_scale, _scale, 1f));
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

        /// <summary>Checks that actual camera pixels match a separate original-artwork reference frame.</summary>
        /// <param name="actual">Pixels produced by the production drawing helper.</param>
        /// <param name="expected">Pixels produced by literal original atlas crops.</param>
        /// <param name="message">The behavior being checked.</param>
        private static void AssertPixelsMatch(Color32[] actual, Color32[] expected, string message)
        {
            Assert.That(DifferentPixels(actual, expected), Is.LessThan(4), message);
        }

        /// <summary>Counts camera pixels whose RGB differences exceed rounding tolerance.</summary>
        /// <param name="first">The first captured frame.</param>
        /// <param name="second">The second captured frame.</param>
        /// <returns>The count of visibly different pixels.</returns>
        private static int DifferentPixels(Color32[] first, Color32[] second)
        {
            var different = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 6)
                {
                    different++;
                }
            }

            return different;
        }

        /// <summary>Writes optional diagnostic camera captures only into an explicitly selected output directory.</summary>
        /// <param name="name">The diagnostic image filename.</param>
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
