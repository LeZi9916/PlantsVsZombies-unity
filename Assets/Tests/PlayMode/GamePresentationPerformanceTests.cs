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
    /// <summary>Protects actor callback reuse, presentation identities, and native track filtering.</summary>
    public sealed class GamePresentationPerformanceTests
    {
        /// <summary>Contains synthetic native animation definitions and their small source images.</summary>
        private string _root = "";
        /// <summary>Samples the fixture animations through the production renderer.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>Retains the production actor callbacks for all measured draws.</summary>
        private AdventureActorRenderer _actors = null!;
        /// <summary>Retains actor identities across consecutive presentation frames.</summary>
        private PresentationKeyCache _keys = null!;
        /// <summary>Exercises the kernel and butter visibility callback.</summary>
        private Plant _kernel = null!;
        /// <summary>Exercises independently sampled attached plant heads.</summary>
        private Plant _peashooter = null!;
        /// <summary>Exercises native zombie combat composition and costume filtering.</summary>
        private Zombie _zombie = null!;
        /// <summary>Reuses the attached-head poses consumed synchronously by the actor renderer.</summary>
        private HeadPlayback[] _heads = null!;
        /// <summary>Receives synthetic scene pixels for behavior checks.</summary>
        private RenderTexture _target = null!;
        /// <summary>Reads colors from the synthetic target without screenshots or source assets.</summary>
        private Texture2D _pixels = null!;

        /// <summary>Creates original-format fixture assets after the render pipeline has initialized.</summary>
        /// <returns>A frame boundary before allocating graphics resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZPresentation-" + Guid.NewGuid().ToString("N"));
            var reanim = Path.Combine(_root, "reanim");
            Directory.CreateDirectory(reanim);
            WriteImage(reanim, "red", Color.red);
            WriteImage(reanim, "green", Color.green);
            WriteImage(reanim, "blue", Color.blue);
            WriteImage(reanim, "yellow", Color.yellow);
            File.WriteAllText(Path.Combine(reanim, "KernelPult.reanim"),
                "<fps>12</fps>" + Marker("anim_idle")
                + ImageTrack("body", "red", 0, 0)
                + ImageTrack("Cornpult_kernal", "yellow", 10, 0)
                + ImageTrack("Cornpult_butter", "green", 20, 0));
            File.WriteAllText(Path.Combine(reanim, "PeaShooter.reanim"),
                "<fps>12</fps>"
                + "<track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_head_idle</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>"
                + "<track><name>anim_stem</name><t><f>0</f></t><t/><t/><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_red</i><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>head</name><t><i>IMAGE_REANIM_green</i><x>10</x><f>-1</f></t><t/><t><f>0</f></t><t/></track>");
            File.WriteAllText(Path.Combine(reanim, "Zombie.reanim"),
                "<fps>12</fps>" + Marker("anim_walk")
                + ImageTrack("Zombie_body", "blue", 0, 0)
                + ImageTrack("anim_head", "red", 0, 10)
                + ImageTrack("anim_hair", "green", 10, 10));
            _renderer = new ReanimationRenderer(_root);
            _actors = new AdventureActorRenderer(_renderer);
            _keys = new PresentationKeyCache();
            _kernel = new Plant { Id = 1, Kind = PlantKind.KernelPult };
            _peashooter = new Plant { Id = 2, Kind = PlantKind.Peashooter };
            _zombie = new Zombie { Id = 3, Kind = ZombieKind.Normal, Health = 200f };
            _heads = new[]
            {
                new HeadPlayback { Animation = "anim_head_idle", Anchor = "anim_stem", Rate = 12f, Loop = true }
            };
            _target = new RenderTexture(128, 64, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        }

        /// <summary>Releases native rendering resources and the uniquely generated fixture directory.</summary>
        /// <returns>A frame boundary allowing Unity to finish deferred destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer?.Dispose();
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }

            UnityEngine.Object.Destroy(_pixels);
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }

            yield return null;
        }

        /// <summary>Retains live keys across frames while allowing absent actor identities to be collected.</summary>
        [Test]
        public void ReusesLiveKeysAndRetiresAbsentIdentities()
        {
            _keys.BeginFrame();
            var first = _keys.Get("plant-", 123);
            var part = _keys.Get("effect-", 123, 2);
            Assert.That(first, Is.EqualTo("plant-123"));
            Assert.That(part, Is.EqualTo("effect-123-2"));
            Assert.That(_keys.Get("plant-", 123), Is.SameAs(first));
            _keys.BeginFrame();
            Assert.That(_keys.Get("plant-", 123), Is.SameAs(first));
            Assert.That(_keys.Get("effect-", 123, 2), Is.SameAs(part));
            _keys.BeginFrame();
            _keys.BeginFrame();
            var replacement = _keys.Get("plant-", 123);
            Assert.That(replacement, Is.EqualTo(first));
            Assert.That(replacement, Is.Not.SameAs(first));
        }

        /// <summary>Uses the current plant's butter selection even when two actors share one retained predicate.</summary>
        [Test]
        public void KeepsPlantTrackFiltersLocalToEachDraw()
        {
            BeginFrame();
            DrawKernel(new Vector2(4f, 4f));
            _kernel.PresentationButter = true;
            DrawKernel(new Vector2(44f, 4f));
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(5, 5), Color.red);
            AssertColor(Pixel(15, 5), Color.yellow);
            AssertColor(Pixel(25, 5), Color.black);
            AssertColor(Pixel(45, 5), Color.red);
            AssertColor(Pixel(55, 5), Color.black);
            AssertColor(Pixel(65, 5), Color.green);
        }

        /// <summary>Preserves missing-head filtering when the retained zombie callback is reused immediately.</summary>
        [Test]
        public void KeepsZombieHeadVisibilityLocalToEachDraw()
        {
            BeginFrame();
            _actors.DrawZombie(_zombie, "Zombie", "anim_walk", new Vector2(4f, 4f), 1f, 0f, 12f, true, "actor-3", "native-zombie-combat-3");
            _zombie.Id = 4;
            _zombie.Headless = true;
            _actors.DrawZombie(_zombie, "Zombie", "anim_walk", new Vector2(44f, 4f), 1f, 0f, 12f, true, "actor-4", "native-zombie-combat-4");
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(5, 5), Color.blue);
            AssertColor(Pixel(5, 15), Color.red);
            AssertColor(Pixel(15, 15), Color.green);
            AssertColor(Pixel(45, 5), Color.blue);
            AssertColor(Pixel(45, 15), Color.black);
            AssertColor(Pixel(55, 15), Color.black);
        }

        /// <summary>Renders independent head artwork through the same reusable plant callback.</summary>
        [Test]
        public void PreservesAttachedPlantHeadArtwork()
        {
            BeginFrame();
            DrawPeashooter();
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(5, 25), Color.red);
            AssertColor(Pixel(15, 25), Color.green);
        }

        /// <summary>Prevents actor closures, head arrays, and repeated identity formatting after warmup.</summary>
        [Test]
        public void WarmPlantPresentationDoesNotAllocateManagedMemory()
        {
            for (var i = 0; i < 8; i++)
            {
                PresentPlants();
            }

            var screenRenderer = ScreenRenderer();
            var retained = screenRenderer.PooledObjectCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                PresentPlants();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(screenRenderer.PooledObjectCount, Is.EqualTo(retained));
            Assert.That(_renderer.LastError, Is.Null);
        }

        /// <summary>Prevents callback and surface-key garbage in the repeatedly drawn combat zombie path.</summary>
        [Test]
        public void WarmZombiePresentationDoesNotAllocateManagedMemory()
        {
            for (var i = 0; i < 8; i++)
            {
                PresentZombie();
            }

            var screenRenderer = ScreenRenderer();
            var retained = screenRenderer.PooledObjectCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                PresentZombie();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(screenRenderer.PooledObjectCount, Is.EqualTo(retained));
            Assert.That(_renderer.LastError, Is.Null);
        }

        /// <summary>Protects the real input and simulation update traversal after startup without loading audio or disk-backed progress.</summary>
        [Test]
        public void WarmGameUpdateWithoutLoadedAudioDoesNotAllocateManagedMemory()
        {
            WriteOptionsFixture();
            var ownerObject = new GameObject("Inactive game update regression");
            ownerObject.SetActive(false);
            OptionsView? options = null;
            SelectorView? selector = null;
            GamePresenter? presenter = null;
            try
            {
                var owner = ownerObject.AddComponent<PvZGame>();
                owner._game = new GameSimulation(new GameRules
                {
                    FirstZombieDelay = float.MaxValue,
                    SkySunInterval = float.MaxValue
                }, randomSeed: 12345);
                options = new OptionsView(_root);
                selector = new SelectorView(_root);
                owner._optionsView = options;
                owner._selectorView = selector;
                owner._animations = _renderer;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._smokeMode = true;
                presenter = new GamePresenter(owner);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PvZGame).GetField("_presenter", flags)!.SetValue(owner, presenter);
                typeof(PvZGame).GetField("_smokePointer", flags)!.SetValue(owner, new Vector2(-1000f, -1000f));
                typeof(PvZGame).GetField("_zombiePartAudioLoading", flags)!.SetValue(owner, true);
                var method = typeof(PvZGame).GetMethod("Update", flags)!;
                var update = (Action)Delegate.CreateDelegate(typeof(Action), owner, method);
                for (var i = 0; i < 8; i++)
                {
                    update();
                }

                var elapsed = owner._game.Elapsed;
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 100; i++)
                {
                    update();
                }

                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero);
                Assert.That(owner._game.Elapsed, Is.GreaterThan(elapsed));
            }
            finally
            {
                presenter?.Dispose();
                options?.Dispose();
                selector?.Dispose();
                UnityEngine.Object.Destroy(ownerObject);
            }
        }

        /// <summary>Provides only the small decoded option textures required to construct a real closed modal view.</summary>
        private void WriteOptionsFixture()
        {
            var images = Path.Combine(_root, "images");
            Directory.CreateDirectory(images);
            var bytes = File.ReadAllBytes(Path.Combine(_root, "reanim", "red.png"));
            var names = new[]
            {
                "options_menuback.jpg", "options_menuback_.png", "options_sliderslot.png", "options_sliderknob2.png",
                "options_checkbox0.png", "options_checkbox1.png", "options_backtogamebutton0.png", "options_backtogamebutton2.png",
                "button_left.png", "button_middle.png", "button_right.png"
            };
            foreach (var name in names)
            {
                File.WriteAllBytes(Path.Combine(images, name), bytes);
            }
        }

        /// <summary>Begins one complete presentation traversal with stable actor identities.</summary>
        private void BeginFrame()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _keys.BeginFrame();
            _renderer.BeginObjectFrame();
        }

        /// <summary>Draws the two plant paths exercised by the steady-state allocation regression.</summary>
        private void PresentPlants()
        {
            BeginFrame();
            DrawKernel(new Vector2(4f, 4f));
            DrawPeashooter();
            _renderer.EndObjectFrame();
        }

        /// <summary>Draws one combat zombie using its stable key and a retained callback.</summary>
        private void PresentZombie()
        {
            BeginFrame();
            _actors.DrawZombie(_zombie, "Zombie", "anim_walk", new Vector2(4f, 4f), 1f, .1f, 12f, true,
                _keys.Get("actor-", _zombie.Id), _keys.Get("native-zombie-combat-", _zombie.Id));
            _renderer.EndObjectFrame();
        }

        /// <summary>Draws the fixture kernel catapult at a caller-selected origin.</summary>
        /// <param name="origin">The top-left logical origin of the plant.</param>
        private void DrawKernel(Vector2 origin)
        {
            _actors.DrawPlant(_kernel, "KernelPult", origin, 1f, .1f, "anim_idle", true, 12f, null,
                _keys.Get("plant-", _kernel.Id), new Rect(0f, 0f, 128f, 64f), 20, 0f, _keys.Get("producer-plant-", _kernel.Id));
        }

        /// <summary>Draws the fixture attached-head plant with a reused playback array.</summary>
        private void DrawPeashooter()
        {
            _actors.DrawPlant(_peashooter, "PeaShooter", new Vector2(4f, 24f), 1f, .1f, "anim_idle", true, 12f, _heads,
                _keys.Get("plant-", _peashooter.Id), new Rect(0f, 0f, 128f, 64f), 20, 0f, _keys.Get("producer-plant-", _peashooter.Id));
        }

        /// <summary>Finds the scene pool for native-pixel assertions outside all allocation measurements.</summary>
        /// <returns>The pool used to submit the fixture actor geometry.</returns>
        private ObjectSpriteRenderer ScreenRenderer()
        {
            var field = typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (ObjectSpriteRenderer)field.GetValue(_renderer)!;
        }

        /// <summary>Renders the actor geometry and reads its synthetic target while preserving the active target.</summary>
        private void Capture()
        {
            ScreenRenderer().RenderTo(_target);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, 128f, 64f), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Reads one synthetic scene pixel in top-left logical coordinates.</summary>
        /// <param name="x">The horizontal target coordinate.</param>
        /// <param name="y">The downward target coordinate.</param>
        /// <returns>The rendered source color.</returns>
        private Color Pixel(int x, int y)
        {
            return _pixels.GetPixel(x, _pixels.height - y - 1);
        }

        /// <summary>Checks primary source colors with tolerance for the native surface's texture filtering.</summary>
        /// <param name="actual">The captured scene color.</param>
        /// <param name="expected">The expected source color over the black background.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.05f), "red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.05f), "green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.05f), "blue");
        }

        /// <summary>Builds a two-frame native animation marker.</summary>
        /// <param name="name">The marker identifying the requested clip.</param>
        /// <returns>The original XML fragment for a visible two-frame clip.</returns>
        private static string Marker(string name)
        {
            return "<track><name>" + name + "</name><t><f>0</f></t><t/></track>";
        }

        /// <summary>Builds a two-frame image track with a stable synthetic position.</summary>
        /// <param name="name">The native track name used by production visibility rules.</param>
        /// <param name="image">The synthetic source image name.</param>
        /// <param name="x">The horizontal source offset.</param>
        /// <param name="y">The downward source offset.</param>
        /// <returns>The original XML fragment for the image track.</returns>
        private static string ImageTrack(string name, string image, int x, int y)
        {
            return "<track><name>" + name + "</name><t><i>IMAGE_REANIM_" + image + "</i><x>" + x
                + "</x><y>" + y + "</y><f>0</f></t><t/></track>";
        }

        /// <summary>Writes a small source image without including any original game assets in the fixture.</summary>
        /// <param name="directory">The unique fixture's reanimation directory.</param>
        /// <param name="name">The source image name referenced by native image IDs.</param>
        /// <param name="color">The primary source color used by pixel checks.</param>
        private static void WriteImage(string directory, string name, Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color[16];
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
