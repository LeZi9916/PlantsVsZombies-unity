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
    /// <summary>Checks original dancer preview scale, entrance, pointing gesture, and summoned backup emergence.</summary>
    public sealed class OriginalDancerGraphicsTests
    {
        /// <summary>Owns an inactive coordinator without game lifecycle or profile loading.</summary>
        private GameObject _host = null!;
        /// <summary>Provides the isolated board and original resources to production presentation.</summary>
        private PvZGame _owner = null!;
        /// <summary>Submits original dancer reanimation tracks to the actual camera.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Runs the production street and battle actor drawing paths.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives actual scene and Canvas pixels.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives readable camera output for artwork and scale comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the surrounding runner's scene camera target.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Maps the original logical viewport onto the current test screen.</summary>
        private Matrix4x4 _viewport;
        /// <summary>Converts measured camera dimensions back to original source pixels.</summary>
        private float _scale;
        /// <summary>Identifies the letterbox borders excluded from actor measurements.</summary>
        private Vector2 _offset;

        /// <summary>Creates an original-artwork fixture without starting gameplay or touching persistent data.</summary>
        /// <returns>A frame boundary allowing graphics pipeline initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            _host = new GameObject("Inactive original dancer graphics fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = root;
            _animations = new ReanimationRenderer(root);
            _owner._animations = _animations;
            _owner._game = Fixture();
            _owner.Textures._textures["plantshadow2.png"] = null!;
            _presenter = new GamePresenter(_owner);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
            _viewport = Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0f), Quaternion.identity, new Vector3(_scale, _scale, 1f));
        }

        /// <summary>Releases only resources and scene objects owned by this isolated fixture.</summary>
        /// <returns>A frame boundary allowing deferred Unity destruction to finish.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                _presenter.Dispose();
                _animations.Dispose();
                _owner.Textures._textures.Clear();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
                UnityEngine.Object.Destroy(_host);
            }

            yield return null;
        }

        /// <summary>Checks original dancer street and almanac sizes, the almanac body pivot, and the Little Trouble override.</summary>
        /// <returns>Frame boundaries for real production previews, original source silhouettes, and the native almanac pivot oracle.</returns>
        [UnityTest]
        public IEnumerator StreetDancersAnimateAtOriginalScaleIncludingLittleTrouble()
        {
            using var almanac = new AlmanacView(_owner._root, _animations);
            var portrait = typeof(AlmanacView).GetMethod("DrawNativePortrait", BindingFlags.Instance | BindingFlags.NonPublic)!;
            foreach (var kind in new[] { ZombieKind.Dancing, ZombieKind.BackupDancer })
            {
                var asset = kind == ZombieKind.Dancing ? "Zombie_disco" : "Zombie_backup";
                foreach (var small in new[] { false, true })
                {
                    _animations.ResetNativePlayback();
                    var context = new AdventureIntroContext { Level = 19, NativeBackground = 1, IsLittleTrouble = small };
                    var preview = AdventureIntroPresentation.PlaceStreetZombie(context, (int)kind, 0, 1,
                        maximum => 0, maximum => 0f,
                        (nativeKind, track) => _animations.TryGetNativeAnimationInfo(asset, track, out _));
                    var expectedScale = small ? .5f : .8f;
                    Assert.That(preview.Scale, Is.EqualTo(expectedScale));
                    Assert.That(preview.Animation, Is.EqualTo("anim_armraise"));
                    _owner.Opening._adventureIntro = new AdventureIntroClock(new AdventureIntroPlan { Context = context });
                    _owner.Opening._adventureIntroFrame = new AdventureIntroFrame { PreviewVisible = true };
                    _owner.Opening._adventurePreview.Clear();
                    _owner.Opening._adventurePreview.Add(preview);
                    _owner.Opening._adventureIntroVisualAge = 0f;
                    yield return Capture(DrawStreetActors);
                    var first = _pixels.GetPixels32();
                    var actualBounds = ArtworkBounds();

                    // Measure the unchanged original reanimation at 1x rather than repeating the production scale calculation.
                    yield return Capture(() =>
                    {
                        Assert.That(_animations.DrawNativePlayback(asset, new Vector2(230f, 200f), 1f, 0f,
                            "anim_armraise", true, 12f), Is.True, _animations.LastError);
                    });
                    var sourceBounds = ArtworkBounds();
                    Assert.That(actualBounds.width, Is.EqualTo(sourceBounds.width * expectedScale).Within(3f));
                    Assert.That(actualBounds.height, Is.EqualTo(sourceBounds.height * expectedScale).Within(3f));
                    _owner.Opening._adventureIntroVisualAge = .2f;
                    yield return Capture(DrawStreetActors);
                    Assert.That(DifferentPixels(first, _pixels.GetPixels32()), Is.GreaterThan(20),
                        "Both original street dancers must visibly advance their raised-arm animation.");
                }

                yield return Capture(() =>
                {
                    Assert.That((bool)portrait.Invoke(almanac, new object[]
                    {
                        false, (int)kind, new Vector2(559f, 175f), 1f, .2f, false
                    })!, Is.True);
                });
                var detail = _pixels.GetPixels32();
                var detailBounds = ArtworkBounds();
                yield return Capture(() =>
                {
                    Assert.That(_animations.DrawNativePlayback(asset, new Vector2(230f, 200f), 1f, .2f,
                        "anim_armraise", true, 12f), Is.True, _animations.LastError);
                });
                var almanacSourceBounds = ArtworkBounds();
                Assert.That(detailBounds.width, Is.EqualTo(almanacSourceBounds.width * .8f).Within(3f));
                Assert.That(detailBounds.height, Is.EqualTo(almanacSourceBounds.height * .8f).Within(3f));
                yield return Capture(() =>
                {
                    // PC 0x40BF40/0x547FF0: (559,175) plus (30,120)*(1-.8) is the source origin (565,199).
                    Assert.That(_animations.DrawNativePlayback(asset, new Vector2(565f, 199f), .8f, .2f,
                        "anim_armraise", true, 12f), Is.True, _animations.LastError);
                });
                Assert.That(DifferentPixels(detail, _pixels.GetPixels32()), Is.LessThan(5),
                    "The almanac dancer must preserve the original body pivot and twelve-fps arm-raise pose.");
            }
        }

        /// <summary>Checks production entrance and summoning animate before a real summoned backup emerges through its native ground clip.</summary>
        /// <returns>Frame boundaries for actual original entrance, pointing, and emerging backup artwork.</returns>
        [UnityTest]
        public IEnumerator EntranceAndSummoningAnimateBeforeBackupsEmerge()
        {
            var game = _owner.RequiredGame;
            var dancer = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Dancing, 2, 6.5f, 0 })!;
            Assert.That(dancer.PresentationNativePhase, Is.EqualTo(40));
            Assert.That(game.AdventurePresentationZombieAnimation(dancer), Is.EqualTo("anim_moonwalk"));
            yield return Capture(() => DrawBattleActor(dancer));
            var entrance = _pixels.GetPixels32();
            Assert.That(ArtworkBounds().height, Is.GreaterThan(40f));
            game.Update(.15f);
            yield return Capture(() => DrawBattleActor(dancer));
            Assert.That(DifferentPixels(entrance, _pixels.GetPixels32()), Is.GreaterThan(20),
                "The original moonwalk entrance must produce advancing camera artwork.");

            Assert.That(dancer.DancerState, Is.Not.Null);
            dancer.DancerState!.PhaseTicks = 1;
            game.Update(.01f);
            Assert.That(dancer.PresentationNativePhase, Is.EqualTo(41));
            Assert.That(game.AdventurePresentationZombieAnimation(dancer), Is.EqualTo("anim_point"));
            yield return Capture(() => DrawBattleActor(dancer));
            var pointing = _pixels.GetPixels32();
            game.Update(.2f);
            yield return Capture(() => DrawBattleActor(dancer));
            Assert.That(DifferentPixels(pointing, _pixels.GetPixels32()), Is.GreaterThan(20),
                "The pointing gesture must visibly animate before its nine source frames finish summoning.");
            for (var tick = 0; tick < 50 && !game.Zombies.Exists(zombie => zombie.Kind == ZombieKind.BackupDancer); tick++)
            {
                game.Update(.01f);
            }

            var backup = game.Zombies.Find(zombie => zombie.Kind == ZombieKind.BackupDancer);
            Assert.That(backup, Is.Not.Null, "The production pointing gesture must summon a real backup dancer.");
            Assert.That(backup!.PresentationNativePhase, Is.EqualTo(50));
            Assert.That(backup.DancerState!.PhaseTicks, Is.EqualTo(150));
            yield return Capture(() => { });
            var empty = _pixels.GetPixels32();
            yield return Capture(() => DrawBattleActor(backup));
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.Zero,
                "A newly summoned backup starts completely underground.");
            yield return Capture(() => DrawBattleActor(dancer));
            var heldPoint = _pixels.GetPixels32();
            game.Update(.2f);
            yield return Capture(() => DrawBattleActor(dancer));
            Assert.That(DifferentPixels(heldPoint, _pixels.GetPixels32()), Is.LessThan(5),
                "The original pointing gesture holds its final frame instead of looping during the summon hold.");
            game.Update(1f);
            yield return Capture(() => DrawBattleActor(backup));
            Assert.That(DifferentPixels(empty, _pixels.GetPixels32()), Is.GreaterThan(20),
                "The original linear emergence must reveal actual backup pixels before its counter expires.");
            var emerging = ArtworkBounds();
            game.Update(.3f);
            yield return Capture(() => DrawBattleActor(backup));
            Assert.That(backup.PresentationNativePhase, Is.Not.EqualTo(50));
            Assert.That(ArtworkBounds().height, Is.GreaterThan(emerging.height + 5f),
                "The complete dancer must rise above the partially clipped emerging silhouette.");
        }

        /// <summary>Creates a legal isolated night board whose wave scheduling remains outside this graphics fixture.</summary>
        /// <returns>The playing board used for production actor presentation.</returns>
        private static GameSimulation Fixture()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Brings the real street coordinates into the camera without modifying preview placement or size.</summary>
        /// <exception cref="TargetInvocationException">The production operation throws.</exception>
        private void DrawStreetActors()
        {
            CanvasUI.Matrix = _viewport * Matrix4x4.Translate(new Vector3(-600f, 60f, 0f));
            typeof(GamePresenter).GetMethod("DrawAdventureOpeningActors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_presenter, null);
        }

        /// <summary>Draws one actual board zombie through the production battle presentation.</summary>
        /// <param name="zombie">The live board actor whose original phase is being checked.</param>
        /// <exception cref="TargetInvocationException">The production operation throws.</exception>
        private void DrawBattleActor(Zombie zombie)
        {
            typeof(GamePresenter).GetMethod("DrawAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_presenter, new object[] { zombie });
        }

        /// <summary>Captures an original logical draw operation over a plain Canvas background.</summary>
        /// <param name="draw">The actual production or original-reference operation to capture.</param>
        /// <returns>Frame boundaries publishing native Canvas and scene geometry.</returns>
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

        /// <summary>Measures the visible original actor silhouette without depending on its production placement formula.</summary>
        /// <returns>The artwork bounds expressed in original source-pixel dimensions.</returns>
        private Rect ArtworkBounds()
        {
            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);
            var pixels = _pixels.GetPixels32();
            for (var y = 0; y < _pixels.height; y++)
            {
                for (var x = 0; x < _pixels.width; x++)
                {
                    var point = new Vector2((x - _offset.x) / _scale, (_pixels.height - y - 1f - _offset.y) / _scale);
                    var pixel = pixels[y * _pixels.width + x];
                    if (point.x >= 0f && point.x < 800f && point.y >= 0f && point.y < 600f
                        && 765 - pixel.r - pixel.g - pixel.b > 24)
                    {
                        minimum = Vector2.Min(minimum, point);
                        maximum = Vector2.Max(maximum, point);
                    }
                }
            }

            Assert.That(minimum.x, Is.LessThan(float.MaxValue), "Original dancer tracks must reach the actual camera output.");
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x + 1f / _scale, maximum.y + 1f / _scale);
        }

        /// <summary>Counts materially changed RGB pixels in two actual camera captures.</summary>
        /// <param name="first">The first completed camera image.</param>
        /// <param name="second">The second completed camera image.</param>
        /// <returns>The number of pixels whose combined RGB difference exceeds readback tolerance.</returns>
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
