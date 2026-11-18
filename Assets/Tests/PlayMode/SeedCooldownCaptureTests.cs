#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Tests.Shared;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares retained cooldown captures with the original uncached packet composition.</summary>
    public sealed class SeedCooldownCaptureTests
    {
        /// <summary>The original-format resources belonging only to this fixture.</summary>
        private string _root = "";
        /// <summary>The inactive production host supplying real packet context and drawing methods.</summary>
        private GameObject _host = null!;
        /// <summary>The production host's simulation and source-image caches.</summary>
        private PvZGame _owner = null!;
        /// <summary>The production presenter under test.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>The production native image and composition renderer.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>The retained delegate entering the optimized production packet method.</summary>
        private Action<PlantKind, Rect> _drawPacket = null!;
        /// <summary>The original card-art method shared by the independent uncached reference.</summary>
        private Action<PlantKind, Rect, bool> _drawCard = null!;
        /// <summary>The exact production card signature, including its optional dialog template parameter.</summary>
        private Action<PlantKind, Rect, bool, PlantKind?> _drawCardWithTemplate = null!;
        /// <summary>Resolves original resource names without using the changed card composition implementation.</summary>
        private Func<PlantKind, string> _animationName = null!;
        /// <summary>The old uncached imitation callback retained for every reference frame.</summary>
        private Func<bool> _drawReferenceImitater = null!;
        /// <summary>The retained original imitation capture key names used without per-frame formatting.</summary>
        private PresentationKeyCache _referenceKeys = null!;
        /// <summary>The immediately consumed static atlas for a reference imitation, or null for native artwork.</summary>
        private SpriteImage? _referenceImitaterAtlas;
        /// <summary>The atlas crop immediately consumed by the old imitation callback.</summary>
        private Rect _referenceImitaterSource;
        /// <summary>The resource immediately consumed by the old native imitation callback.</summary>
        private string _referenceImitaterName = string.Empty;
        /// <summary>The native origin immediately consumed by the old imitation callback.</summary>
        private Vector2 _referenceImitaterOrigin;
        /// <summary>The native scale immediately consumed by the old imitation callback.</summary>
        private float _referenceImitaterScale;
        /// <summary>The production price method, outside the cached artwork itself.</summary>
        private Action<PlantKind, Rect> _drawCost = null!;
        /// <summary>The complete production price signature, including its settled-bank default.</summary>
        private Action<PlantKind, Rect, bool> _drawCostWithMode = null!;
        /// <summary>The uncaptured reference mask callback retained across every frame.</summary>
        private Func<bool> _drawReferenceMask = null!;
        /// <summary>The direct source-image callback used to isolate capture caching from simulation queries.</summary>
        private Func<bool> _drawDirectMask = null!;
        /// <summary>The immutable source handle used by the context-free capture allocation regression.</summary>
        private SpriteImage _directMaskImage = null!;
        /// <summary>The packet context reused by the uncached reference.</summary>
        private readonly AdventureSeedBankContext _context = new AdventureSeedBankContext();
        /// <summary>The kind currently submitted by both production and reference methods.</summary>
        private PlantKind _kind;
        /// <summary>The exact native rectangle submitted by both methods.</summary>
        private Rect _card = new Rect(100f, 140f, 50f, 70f);
        /// <summary>The inherited tint currently applied to both composition paths.</summary>
        private Color _tint = Color.white;
        /// <summary>The additional transform outside the native capture.</summary>
        private Matrix4x4 _transform = Matrix4x4.identity;
        /// <summary>The synthetic source textures owned by the fixture.</summary>
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        /// <summary>The real graphics target including Canvas prices and scene artwork.</summary>
        private RenderTexture _target = null!;
        /// <summary>The reusable graphics readback texture.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The fixed native cooldown counters modified without advancing unrelated gameplay.</summary>
        private int[] _cooldowns = null!;

        /// <summary>Creates translucent native thumbnails, static packet art and a real eight-slot game context.</summary>
        /// <returns>A frame boundary initializing Unity graphics.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            CanvasUI.Dispose();
            _root = Path.Combine(Application.temporaryCachePath, "PvZCooldown-" + Guid.NewGuid().ToString("N"));
            var reanim = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var images = Directory.CreateDirectory(Path.Combine(_root, "images")).FullName;
            var red = Texture(22, 28, new Color(1f, .1f, .15f, .65f));
            var green = Texture(16, 20, new Color(.1f, 1f, .2f, .75f));
            var blue = Texture(25, 30, new Color(.1f, .2f, 1f, .7f));
            File.WriteAllBytes(Path.Combine(reanim, "red.png"), red.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(reanim, "green.png"), green.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(reanim, "blue.png"), blue.EncodeToPNG());
            var marker = "<track><name>anim_idle</name><t><f>0</f></t><t/></track>";
            File.WriteAllText(Path.Combine(reanim, "PeaShooter.reanim"),
                "<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_head_idle</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>"
                + "<track><name>anim_stem</name><t><f>0</f><x>4</x></t><t/><t/><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_red</i><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>head</name><t><i>IMAGE_REANIM_green</i><x>8</x><y>8</y><f>-1</f></t><t/><t><f>0</f></t><t/></track>");
            File.Copy(Path.Combine(reanim, "PeaShooter.reanim"), Path.Combine(reanim, "PeaShooterSingle.reanim"));
            File.WriteAllText(Path.Combine(reanim, "Sunflower.reanim"), "<fps>12</fps>" + marker
                + "<track><name>body</name><t><i>IMAGE_REANIM_blue</i><x>4</x><y>2</y><f>0</f></t><t/></track>");
            var optionBytes = red.EncodeToPNG();
            foreach (var name in new[] { "options_menuback.jpg", "options_menuback_.png", "options_sliderslot.png",
                "options_sliderknob2.png", "options_checkbox0.png", "options_checkbox1.png", "options_backtogamebutton0.png",
                "options_backtogamebutton2.png", "button_left.png", "button_middle.png", "button_right.png" })
            {
                File.WriteAllBytes(Path.Combine(images, name), optionBytes);
            }
            _host = new GameObject("Inactive cooldown capture regression");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = _root;
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8, AdventurePacketSlots = 8 }, 7529);
            // NUnit reuses the fixture instance, while each case creates a new production presenter.
            // Match its fresh identity generation so the independent oracle overwrites the same logical surface.
            _referenceKeys = new PresentationKeyCache();
            _referenceKeys.SetSession(_owner._game);
            var seeds = new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.CabbagePult, PlantKind.Imitater };
            _owner.RequiredGame.Adventure.AvailableSeeds = Array.AsReadOnly(seeds);
            _owner.RequiredGame.Adventure.ImitaterTarget = PlantKind.Peashooter;
            Assert.That(_owner.RequiredGame.ChooseAdventureSeeds(seeds), Is.True);
            _owner.RequiredGame.ResetAdventureWaveSchedule(100000, false, false);
            _owner._deck.Clear();
            _owner._deck.AddRange(seeds);
            SimulationInspection.GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(_owner.RequiredGame, 9990);
            _cooldowns = (int[])SimulationInspection.GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner.RequiredGame)!;
            _owner._optionsView = new OptionsView(_root);
            _renderer = new ReanimationRenderer(_root);
            _owner._animations = _renderer;
            _presenter = new GamePresenter(_owner);
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "seeds.png", Texture(450, 70, new Color(.9f, .8f, .5f, .9f)));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "packet_plants.png", Texture(650, 70, new Color(.6f, .3f, .8f, .45f)));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            _drawPacket = (Action<PlantKind, Rect>)typeof(GamePresenter).GetMethod("DrawAdventurePacket", flags)!.CreateDelegate(typeof(Action<PlantKind, Rect>), _presenter);
            _drawCardWithTemplate = (Action<PlantKind, Rect, bool, PlantKind?>)typeof(GamePresenter).GetMethod("DrawAdventureCard", flags)!.CreateDelegate(typeof(Action<PlantKind, Rect, bool, PlantKind?>), _presenter);
            _drawCard = DrawCardWithoutTemplate;
            _drawCostWithMode = (Action<PlantKind, Rect, bool>)typeof(GamePresenter).GetMethod("DrawAdventurePacketCost", flags)!.CreateDelegate(typeof(Action<PlantKind, Rect, bool>), _presenter);
            _drawCost = DrawCurrentPacketCost;
            _animationName = (Func<PlantKind, string>)typeof(GamePresenter).GetMethod("AdventurePlantAnimation", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(typeof(Func<PlantKind, string>));
            _drawReferenceImitater = DrawReferenceImitater;
            _drawReferenceMask = DrawReferenceMask;
            _drawDirectMask = DrawDirectMask;
            _directMaskImage = SpriteImage.FromTexture(red);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
        }

        /// <summary>Releases only fixture-owned game, source and capture resources.</summary>
        /// <returns>A frame boundary completing deferred graphics destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.SceneCamera.targetTexture = null;
            _owner.Textures._textures.Clear();
            _owner.Textures.ReleaseAll();
            _presenter.Dispose();
            _renderer.Dispose();
            _owner.RequiredOptionsView.Dispose();
            UnityEngine.Object.DestroyImmediate(_host);
            foreach (var texture in _textures)
            {
                UnityEngine.Object.Destroy(texture);
            }
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Checks the full-to-empty cooldown progression for native thumbnail and static atlas packets.</summary>
        /// <returns>Frame boundaries for each cached and reference composition.</returns>
        [UnityTest]
        public IEnumerator ChangingMaskHeightMatchesUncachedNativeComposition()
        {
            foreach (var kind in new[] { PlantKind.Peashooter, PlantKind.CabbagePult })
            {
                _kind = kind;
                foreach (var ticks in new[] { 100000, 500, 250, 3, 1, 0, 500 })
                {
                    _cooldowns[(int)_kind] = ticks;
                    yield return AssertMatchesReference();
                }
            }
        }

        /// <summary>Invalidates baked pixels for inherited tint, opacity and native viewport changes.</summary>
        /// <returns>Frame boundaries for shifted and resized packet compositions.</returns>
        [UnityTest]
        public IEnumerator TintViewportAndOuterTransformChangesMatchReference()
        {
            _kind = PlantKind.Peashooter;
            _cooldowns[(int)_kind] = 500;
            var cards = new[] { new Rect(100f, 140f, 50f, 70f), new Rect(122.25f, 151.5f, 50f, 70f),
                new Rect(120f, 130f, 75f, 105f), new Rect(110f, 145f, 50f, 70f) };
            for (var variant = 0; variant < cards.Length; variant++)
            {
                _card = cards[variant];
                _tint = new Color(1f - variant * .1f, .8f, .7f, 1f - variant * .15f);
                _transform = Matrix4x4.TRS(new Vector3(variant * 3.25f, variant * 2.5f), Quaternion.Euler(0f, 0f, variant * 2f), Vector3.one);
                yield return AssertMatchesReference();
            }
        }

        /// <summary>Preserves nested imitation pixels after target changes and repeated warm draws reuse the same inner surface identity.</summary>
        /// <returns>Frame boundaries for warm frames and every uncached-reference comparison.</returns>
        [UnityTest]
        public IEnumerator ImitaterTargetChangesPreserveNestedCapturePixels()
        {
            _kind = PlantKind.Imitater;
            foreach (var target in new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.CabbagePult,
                PlantKind.KernelPult, PlantKind.Peashooter })
            {
                _owner.RequiredGame.Adventure.ImitaterTarget = target;
                foreach (var ticks in new[] { 0, 0, 500, 380, 0, 0, 260 })
                {
                    _cooldowns[(int)_kind] = ticks;
                    // Warm the production path first: the base and dark mask share an imitation capture key.
                    // A read-only outer capture must not change the inner surface's observable final pixels.
                    Draw(false);
                    AssertImitationSourceReady();
                    yield return null;
                    yield return null;
                    yield return AssertMatchesReference();
                }
            }
        }

        /// <summary>Checks the original 200-tick wait, the 0.8 animation cue and morph after the full 26 FPS clip.</summary>
        [Test]
        public void ImitaterMorphWaitsForTheNativeAnimationAndEmitsItsCloudOnce()
        {
            var game = _owner.RequiredGame;
            Assert.That(game.TryPlant(PlantKind.Imitater, 2, 2), Is.True);
            var plant = game.Plants[0];
            Assert.That(plant.PresentationRate, Is.InRange(25f, 30f));
            for (var tick = 1; tick <= 320; tick++)
            {
                game.Update(.01f);
                Assert.That(game.Presentation.Explosions.Count, Is.EqualTo(tick < 296 ? 0 : 1));
                if (tick < 320)
                {
                    Assert.That(game.Plants[0].Kind, Is.EqualTo(PlantKind.Imitater));
                }
            }

            var effect = game.Presentation.Explosions[0];
            Assert.That(effect.Particle, Is.EqualTo("ImitaterMorph"));
            Assert.That(effect.Row, Is.EqualTo(2));
            Assert.That(effect.X, Is.EqualTo(2.5f));
            Assert.That(game.Plants[0].Kind, Is.EqualTo(PlantKind.Peashooter));
            Assert.That(game.Plants[0].IsImitated, Is.True);
        }

        /// <summary>Recreates correct pixels after a retained surface has been released or the renderer playback reset.</summary>
        /// <returns>Frame boundaries before reading reused capture attachments.</returns>
        [UnityTest]
        public IEnumerator ReleasedAndResetCooldownSurfacesDoNotDisplayOldPixels()
        {
            _kind = PlantKind.Peashooter;
            _cooldowns[(int)_kind] = 500;
            yield return AssertMatchesReference();
            _renderer.ReleaseNativeSurfaceIdentity("seed-cooldown-0");
            _tint = new Color(.55f, .8f, 1f, .65f);
            yield return AssertMatchesReference();
            _renderer.ResetNativePlayback();
            _tint = Color.white;
            yield return AssertMatchesReference();
        }

        /// <summary>Changes the live crop without rerendering static pixels or adding to the original context allocations.</summary>
        [Test]
        public void WarmCooldownHeightChangesReuseCaptureWithoutAdditionalAllocationEvents()
        {
            _kind = PlantKind.Peashooter;
            for (var warm = 0; warm < 32; warm++)
            {
                _cooldowns[(int)_kind] = 300 + warm;
                Draw(true);
            }
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 2048,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            allocations.Start();
            for (var frame = 0; frame < 64; frame++)
            {
                _cooldowns[(int)_kind] = 300 + frame;
                Draw(true);
            }
            allocations.Stop();
            var referenceEvents = allocations.Count;
            for (var warm = 0; warm < 32; warm++)
            {
                _cooldowns[(int)_kind] = 300 + warm;
                Draw(false);
            }
            var captures = _renderer.NativeCaptureRenderCount;
            Assert.That(captures, Is.GreaterThan(0), "The fixture must exercise actual cooldown composition before measuring reuse.");
            allocations.Reset();
            allocations.Start();
            for (var frame = 0; frame < 64; frame++)
            {
                _cooldowns[(int)_kind] = 300 + frame;
                Draw(false);
            }
            allocations.Stop();
            var cachedEvents = allocations.Count;
            Assert.That(_renderer.NativeCaptureRenderCount, Is.EqualTo(captures), "Changing only the visible crop must reuse cached pixels.");
            Assert.That(cachedEvents, Is.LessThanOrEqualTo(referenceEvents),
                "Caching must add no allocation events to the original production packet-context path.");
            TestContext.WriteLine("Warm packet allocation events: uncached reference = " + referenceEvents
                + ", cached production = " + cachedEvents + " (64 identical traversals each).");
        }

        /// <summary>Checks moving imitation cards retain original pixels while old coordinate captures stop accumulating.</summary>
        /// <returns>Frame boundaries for independent pixel captures and delayed surface retirement.</returns>
        [UnityTest]
        public IEnumerator MovingImitaterCardsKeepPixelsAndBoundSurfacePopulation()
        {
            _kind = PlantKind.Imitater;
            _owner.RequiredGame.Adventure.ImitaterTarget = PlantKind.Peashooter;
            _cooldowns[(int)_kind] = 0;
            var original = _card;
            for (var direction = 0; direction < 2; direction++)
            {
                for (var step = 0; step < 8; step++)
                {
                    var offset = direction == 0 ? step : 7 - step;
                    _card = new Rect(original.x + offset * 7f, original.y + offset * 3f, original.width, original.height);
                    yield return AssertMatchesReference();
                    Assert.That(_renderer.NativeSurfaceCount, Is.LessThanOrEqualTo(5),
                        "Only current/recent coordinate captures and the independent reference may remain live.");
                }
            }
            for (var frame = 0; frame < 4; frame++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                _presenter.BeginImitaterCaptureFrame();
                _presenter.EndImitaterCaptureFrame();
                yield return null;
            }
            _renderer.ReleaseNativeSurfaceIdentity("reference-cooldown");
            Assert.That(_renderer.NativeSurfaceCount, Is.Zero, "An imitation surface omitted by every screen must retire after its complete-frame grace period.");
            _card = original;
        }

        /// <summary>Retains the no-mask imitation image without adding capture renders or context allocation events.</summary>
        [Test]
        public void WarmNoMaskImitaterReusesItsInnerCaptureWithoutAdditionalAllocations()
        {
            _kind = PlantKind.Imitater;
            _owner.RequiredGame.Adventure.ImitaterTarget = PlantKind.Peashooter;
            _cooldowns[(int)_kind] = 0;
            for (var warm = 0; warm < 32; warm++)
            {
                Draw(true);
            }
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 2048,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            allocations.Start();
            for (var frame = 0; frame < 32; frame++)
            {
                Draw(true);
            }
            allocations.Stop();
            var referenceEvents = allocations.Count;
            for (var warm = 0; warm < 32; warm++)
            {
                Draw(false);
            }
            AssertImitationSourceReady();
            var captures = _renderer.NativeCaptureRenderCount;
            Assert.That(captures, Is.GreaterThan(0));
            allocations.Reset();
            allocations.Start();
            for (var frame = 0; frame < 32; frame++)
            {
                Draw(false);
            }
            allocations.Stop();
            var cachedEvents = allocations.Count;
            Assert.That(_renderer.NativeCaptureRenderCount, Is.EqualTo(captures), "A warm no-mask imitation must not rerender its inner image.");
            Assert.That(cachedEvents, Is.LessThanOrEqualTo(referenceEvents), "Imitation caching must add no events to the old packet-context path.");
            TestContext.WriteLine("Warm no-mask imitation allocation events: uncached reference = " + referenceEvents
                + ", cached production = " + cachedEvents + " (32 traversals each).");
        }

        /// <summary>Isolates the retained capture path and requires zero allocations after complete warmup.</summary>
        [Test]
        public void WarmContextFreeCaptureCropChangesAllocateNoManagedMemory()
        {
            for (var warm = 0; warm < 32; warm++)
            {
                DrawDirectCached(1f + warm);
            }
            var captures = _renderer.NativeCaptureRenderCount;
            Assert.That(captures, Is.GreaterThan(0));
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 2048,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            allocations.Start();
            for (var frame = 0; frame < 64; frame++)
            {
                DrawDirectCached(1f + frame);
            }
            allocations.Stop();
            Assert.That(_renderer.NativeCaptureRenderCount, Is.EqualTo(captures), "Live crop changes must reuse the context-free capture.");
            Assert.That(allocations.Count, Is.Zero, "The cache itself must allocate no warmed managed frame state.");
        }

        /// <summary>Submits a retained external-image capture without entering any simulation or Canvas context.</summary>
        /// <param name="height">The live visible top height, independent of the fixed source pixels.</param>
        private void DrawDirectCached(float height)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            _renderer.DrawNativeCachedClipped("direct-mask", _card, _drawDirectMask, 37L, topCrop: height);
            _renderer.EndObjectFrame();
        }

        /// <summary>Submits one immutable external image into the context-free capture fixture.</summary>
        /// <returns>Whether the image was submitted to the native capture renderer.</returns>
        private bool DrawDirectMask()
        {
            return _renderer.DrawNativeImage(_directMaskImage, _card, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Reads both production and independent reference paths using the same source state.</summary>
        /// <returns>Frame boundaries before comparing the two completed screen targets.</returns>
        private IEnumerator AssertMatchesReference()
        {
            Color32[] expected;
            Color32[] actual;
            if (_kind == PlantKind.Imitater)
            {
                // Read production first: the old inner capture deliberately uses the same logical key,
                // so drawing the reference first would invalidate the cache and hide warm-hit mistakes.
                Draw(false);
                yield return null;
                yield return null;
                actual = Capture();
                _renderer.ReleaseNativeSurfaceIdentity("reference-cooldown");
                Draw(true);
                yield return null;
                yield return null;
                expected = Capture();
            }
            else
            {
                _renderer.ReleaseNativeSurfaceIdentity("reference-cooldown");
                Draw(true);
                yield return null;
                yield return null;
                expected = Capture();
                Draw(false);
                yield return null;
                yield return null;
                actual = Capture();
            }
            var maximum = 0;
            for (var index = 0; index < expected.Length; index++)
            {
                maximum = Math.Max(maximum, Math.Abs(expected[index].r - actual[index].r));
                maximum = Math.Max(maximum, Math.Abs(expected[index].g - actual[index].g));
                maximum = Math.Max(maximum, Math.Abs(expected[index].b - actual[index].b));
                maximum = Math.Max(maximum, Math.Abs(expected[index].a - actual[index].a));
            }
            if (maximum > 1)
            {
                var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "seed-cooldown-differences"));
                Directory.CreateDirectory(output);
                var prefix = TestContext.CurrentContext.Test.MethodName + "-" + _kind + "-" + (int)_card.x + "-" + (int)_card.y;
                WriteDifferenceImage(Path.Combine(output, prefix + "-actual.png"), actual);
                WriteDifferenceImage(Path.Combine(output, prefix + "-expected.png"), expected);
                var changed = 0;
                var first = -1;
                for (var index = 0; index < expected.Length; index++)
                {
                    if (!expected[index].Equals(actual[index]))
                    {
                        changed++;
                        if (first < 0)
                        {
                            first = index;
                        }
                    }
                }
                TestContext.WriteLine("Cooldown difference: card=" + _card + ", target=" + _owner.RequiredGame.Adventure.ImitaterTarget
                    + ", ticks=" + _cooldowns[(int)_kind] + ", changed=" + changed + ", first=" + first
                    + ", actual=" + (first < 0 ? "none" : actual[first].ToString())
                    + ", expected=" + (first < 0 ? "none" : expected[first].ToString()) + ", output=" + output);
            }
            Assert.That(maximum, Is.LessThanOrEqualTo(1), "Retained masks must match uncached native composition for " + _kind);
        }

        /// <summary>Writes only failing pixel evidence outside the test project's imported Assets directory.</summary>
        /// <param name="path">The ignored evidence path.</param>
        /// <param name="pixels">The already captured comparison pixels.</param>
        private void WriteDifferenceImage(string path, Color32[] pixels)
        {
            var image = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        /// <summary>Submits one packet with a real Canvas price and production viewport transform.</summary>
        /// <param name="reference">Whether to force the original uncached cooldown composition.</param>
        private void Draw(bool reference)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _presenter.BeginImitaterCaptureFrame();
            CanvasUI.BeginFrame();
            _referenceKeys.BeginFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
            CanvasUI.Matrix *= _transform;
            CanvasUI.Color = _tint;
            _renderer.BeginObjectFrame();
            try
            {
                if (reference)
                {
                    DrawReferencePacket();
                }
                else
                {
                    _drawPacket(_kind, _card);
                }
            }
            finally
            {
                _renderer.EndObjectFrame();
                CanvasUI.EndFrame();
                _presenter.EndImitaterCaptureFrame();
            }
        }

        /// <summary>Preserves the original packet implementation as the uncached visual oracle.</summary>
        private void DrawReferencePacket()
        {
            var context = _owner.RequiredGame.AdventurePacketContext(_kind, destination: _context);
            context.Picked = _owner._selected >= 0 && _owner._selected < _owner._deck.Count && _owner._deck[_owner._selected] == _kind;
            var visual = AdventureSeedBankPresentation.Sample(context);
            var previous = CanvasUI.Color;
            try
            {
                var shade = visual.BaseColor / 255f;
                CanvasUI.Color = new Color(previous.r * shade, previous.g * shade, previous.b * shade, previous.a);
                DrawReferenceCard(_kind, _card);
                if (visual.DrawMask)
                {
                    CanvasUI.Color = new Color(previous.r * visual.MaskColor / 255f, previous.g * visual.MaskColor / 255f,
                        previous.b * visual.MaskColor / 255f, previous.a);
                    _renderer.DrawNativeClippedIdentity("reference-cooldown", _card, _drawReferenceMask,
                        topCrop: Math.Min(_card.height, visual.MaskHeight));
                }
            }
            finally
            {
                CanvasUI.Color = previous;
            }
            if (visual.DrawCost)
            {
                _drawCost(_kind, _card);
            }
        }

        /// <summary>Draws the reference's complete packet artwork before its visible top crop is applied.</summary>
        /// <returns>True after the packet artwork was submitted.</returns>
        private bool DrawReferenceMask()
        {
            DrawReferenceCard(_kind, _card);
            return true;
        }

        /// <summary>Calls the exact production signature while keeping the independent reference's settled-template default.</summary>
        /// <param name="kind">The original packet kind.</param>
        /// <param name="card">The original packet rectangle.</param>
        /// <param name="price">Whether the price is drawn.</param>
        private void DrawCardWithoutTemplate(PlantKind kind, Rect card, bool price)
        {
            _drawCardWithTemplate(kind, card, price, null);
        }

        /// <summary>Preserves the production packet-cost default without binding a delegate to an omitted optional parameter.</summary>
        /// <param name="kind">The original packet kind.</param>
        /// <param name="card">The original packet rectangle.</param>
        private void DrawCurrentPacketCost(PlantKind kind, Rect card)
        {
            _drawCostWithMode(kind, card, true);
        }

        /// <summary>Rejects missing native resources or failed inner captures before comparing imitation pixels.</summary>
        private void AssertImitationSourceReady()
        {
            Assert.That(_renderer.LastError ?? string.Empty, Does.Not.Contain("not found"),
                "The fixture must draw real source tracks instead of only comparing packet backgrounds.");
            var field = typeof(ReanimationRenderer).GetField("_nativeSurfaces", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var surfaces = (IDictionary)field.GetValue(_renderer)!;
            var key = _referenceKeys.Get("imitater-card-", (int)_card.x, (int)_card.y);
            Assert.That(surfaces.Contains(key), Is.True, "The source imitation capture must have been created.");
            var surface = surfaces[key]!;
            var ready = surface.GetType().GetField("Ready", BindingFlags.Instance | BindingFlags.Public)!;
            Assert.That((bool)ready.GetValue(surface)!, Is.True, "The native thumbnail or atlas image must produce a ready capture.");
        }

        /// <summary>Draws imitation card artwork through the original uncached inner capture, independent of production changes.</summary>
        /// <param name="kind">The packet kind selecting ordinary or imitation artwork.</param>
        /// <param name="card">The original native packet rectangle.</param>
        private void DrawReferenceCard(PlantKind kind, Rect card)
        {
            if (kind != PlantKind.Imitater)
            {
                _drawCard(kind, card, false);
                return;
            }
            var bank = _owner.Textures.LoadImage("seeds.png");
            if (bank != null)
            {
                // Imitater uses native seed-frame zero; ordinary plants use frame two.
                // The synthetic sheet has a transparent outer border, so the wrong frame differs at the left edge.
                _renderer.DrawNativeImage(bank, card, new Rect(0f, 0f, 1f / 9f, 1f));
            }
            var target = _owner.RequiredGame.Adventure.RequiredImitaterTarget;
            var id = (int)target;
            var cell = AlmanacPortraitLayout.PlantAtlasCell(id);
            _referenceImitaterAtlas = cell >= 0 ? _owner.Textures.LoadImage("packet_plants.png") : null;
            if (_referenceImitaterAtlas != null)
            {
                _referenceImitaterSource = new Rect(cell / 13f, 0f, 1f / 13f, 1f);
            }
            else
            {
                AlmanacPortraitLayout.PlantThumbnailTransform(id, new Rect(0f, 0f, 50f, 70f), out var origin, out var scale);
                _referenceImitaterOrigin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                _referenceImitaterScale = scale * card.width / 50f;
                _referenceImitaterName = _animationName(target);
            }
            _renderer.DrawNativeClippedIdentity(_referenceKeys.Get("imitater-card-", (int)card.x, (int)card.y),
                card, _drawReferenceImitater, imitationFilter: AdventureImitaterRules.SourceFilter(target));
        }

        /// <summary>Submits the currently selected old imitation artwork without any retained inner-capture optimization.</summary>
        /// <returns>Whether native artwork or its static atlas image was submitted.</returns>
        private bool DrawReferenceImitater()
        {
            if (_referenceImitaterAtlas != null)
            {
                return _renderer.DrawNativeImage(_referenceImitaterAtlas, _card, _referenceImitaterSource);
            }
            return _renderer.DrawNativeThumbnail(_referenceImitaterName, _referenceImitaterOrigin, _referenceImitaterScale);
        }

        /// <summary>Reads the completed native screen target while restoring the caller's active target.</summary>
        /// <returns>The complete screen pixel array.</returns>
        private Color32[] Capture()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                _pixels.Apply();
                return _pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Creates source artwork with a stable transparent pixel border.</summary>
        /// <param name="width">The source width.</param>
        /// <param name="height">The source height.</param>
        /// <param name="color">The interior source color and opacity.</param>
        /// <returns>The uploaded texture owned by this fixture.</returns>
        private Texture2D Texture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = x == 0 || y == 0 || x == width - 1 || y == height - 1 ? Color.clear : color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _textures.Add(texture);
            return texture;
        }
    }
}
