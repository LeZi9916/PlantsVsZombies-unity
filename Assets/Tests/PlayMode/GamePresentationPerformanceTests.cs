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
using PlantsVsZombies.Diagnostics;

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
                + ImageTrack("anim_hair", "green", 10, 10)
                + "<track><name>Zombie_flaghand</name><t><x>30</x><y>0</y><f>0</f></t><t/></track>");
            File.WriteAllText(Path.Combine(reanim, "Zombie_FlagPole.reanim"),
                "<fps>12</fps>" + ImageTrack("flagpole", "green", 30, 0));
            File.WriteAllText(Path.Combine(reanim, "Zombie_polevaulter.reanim"),
                "<fps>12</fps>" + Marker("anim_walk")
                + ImageTrack("Zombie_polevaulter_body1", "blue", 0, 0)
                + ImageTrack("anim_head1", "red", 0, 10)
                + ImageTrack("Zombie_polevaulter_innerarm_upper", "green", 10, 10)
                + ImageTrack("Zombie_polevaulter_innerarm_lower", "yellow", 20, 10)
                + ImageTrack("Zombie_polevaulter_innerhand", "green", 10, 20)
                + ImageTrack("Zombie_polevaulter_pole", "red", 20, 20)
                + ImageTrack("Zombie_polevaulter_pole2", "yellow", 30, 20));
            var particles = Directory.CreateDirectory(Path.Combine(_root, "particles")).FullName;
            File.WriteAllText(Path.Combine(particles, "Burst.xml"),
                "<Emitter><Image>IMAGE_REANIM_green</Image><SpawnMinActive>1</SpawnMinActive>"
                + "<ParticleDuration>100</ParticleDuration><SystemDuration>100</SystemDuration></Emitter>");
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

        /// <summary>Prevents a directly drawn flag attachment from translating all subsequently submitted lawn rows.</summary>
        [Test]
        public void DirectFlagAttachmentPreservesFollowingPlantPlacement()
        {
            _zombie.Kind = ZombieKind.Flag;
            BeginFrame();
            var matrix = SpriteDrawState.Matrix;
            var color = SpriteDrawState.Color;
            _actors.DrawZombie(_zombie, "Zombie", "anim_walk", new Vector2(4f, 4f), 1f, 0f, 12f, true,
                "flag-body", "flag-combat");
            Assert.That(SpriteDrawState.Matrix, Is.EqualTo(matrix), "The native flag callback must restore its caller's placement matrix.");
            Assert.That(SpriteDrawState.Color, Is.EqualTo(color));
            DrawKernel(new Vector2(44f, 24f));
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(45, 25), Color.red);
            AssertColor(Pixel(55, 25), Color.yellow);
        }

        /// <summary>Matches original DropPole prefix hiding after decapitation, while preserving the remaining body.</summary>
        [Test]
        public void HeadlessPolevaulterHidesBothArmSegmentsAndBothPoles()
        {
            _zombie.Kind = ZombieKind.PoleVaulter;
            _zombie.Headless = true;
            BeginFrame();
            _actors.DrawZombie(_zombie, "Zombie_polevaulter", "anim_walk", new Vector2(4f, 4f), 1f, 0f, 12f, true,
                "pole-body", "pole-combat");
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(5, 5), Color.blue);
            AssertColor(Pixel(5, 15), Color.black);
            AssertColor(Pixel(15, 15), Color.black);
            AssertColor(Pixel(25, 15), Color.black);
            AssertColor(Pixel(15, 25), Color.black);
            AssertColor(Pixel(25, 25), Color.black);
            AssertColor(Pixel(35, 25), Color.black);
        }

        /// <summary>Reuses capture cameras and mesh pools across repeated feedback identities and level playback resets.</summary>
        [Test]
        public void ReleasedCaptureSurfacesReuseResourcesAcrossFeedbackAndPlaybackReset()
        {
            Func<bool> draw = () => _renderer.DrawResourceSprite("IMAGE_REANIM_red", new Vector2(8f, 8f), 1f);
            for (var actor = 0; actor < 32; actor++)
            {
                var key = "feedback-" + actor;
                BeginFrame();
                Assert.That(_renderer.DrawNativeClipped(key, new Rect(0f, 0f, 32f, 32f), draw), Is.True);
                _renderer.EndObjectFrame();
                _renderer.ReleaseNativeSurface(key);
                Assert.That(_renderer.NativeSurfaceCount, Is.Zero);
                Assert.That(_renderer.InactiveNativeSurfaceCount, Is.EqualTo(1));
            }

            Assert.That(_renderer.NativeSurfaceCreationCount, Is.EqualTo(1), "Completed feedback must reuse its capture GameObjects.");
            Assert.That(_renderer.NativeCaptureRendererCount, Is.EqualTo(1));
            BeginFrame();
            Assert.That(_renderer.DrawNativeClipped("next-level", new Rect(0f, 0f, 32f, 32f), draw), Is.True);
            _renderer.EndObjectFrame();
            _renderer.ResetNativePlayback();
            Assert.That(_renderer.NativeSurfaceCount, Is.Zero);
            Assert.That(_renderer.InactiveNativeSurfaceCount, Is.EqualTo(1));
            Assert.That(_renderer.NativeSurfaceCreationCount, Is.EqualTo(1));
        }

        /// <summary>Redraws reused frozen captures instead of displaying the former actor's cached pixels.</summary>
        [Test]
        public void ReusedFrozenCaptureClearsThePreviousActorsReadyState()
        {
            BeginFrame();
            Assert.That(_renderer.DrawNativeClipped("red-capture", new Rect(0f, 0f, 32f, 32f),
                () => _renderer.DrawResourceSprite("IMAGE_REANIM_red", new Vector2(8f, 8f), 1f), freeze: true), Is.True);
            _renderer.EndObjectFrame();
            _renderer.ReleaseNativeSurface("red-capture");
            BeginFrame();
            Assert.That(_renderer.DrawNativeClipped("blue-capture", new Rect(0f, 0f, 32f, 32f),
                () => _renderer.DrawResourceSprite("IMAGE_REANIM_blue", new Vector2(8f, 8f), 1f), freeze: true), Is.True);
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(8, 8), Color.blue);
            Assert.That(_renderer.NativeSurfaceCreationCount, Is.EqualTo(1));
        }

        /// <summary>Preserves two composites when a logical capture is retired before the current scene frame renders.</summary>
        [Test]
        public void CapturesSubmittedThisFrameAreNotRewrittenForAnotherActor()
        {
            BeginFrame();
            Assert.That(_renderer.DrawNativeClipped("first-capture", new Rect(0f, 0f, 32f, 32f),
                () => _renderer.DrawResourceSprite("IMAGE_REANIM_red", new Vector2(8f, 8f), 1f)), Is.True);
            _renderer.ReleaseNativeSurface("first-capture");
            Assert.That(_renderer.DrawNativeClipped("second-capture", new Rect(44f, 0f, 32f, 32f),
                () => _renderer.DrawResourceSprite("IMAGE_REANIM_blue", new Vector2(52f, 8f), 1f)), Is.True);
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(8, 8), Color.red);
            AssertColor(Pixel(52, 8), Color.blue);
            Assert.That(_renderer.NativeSurfaceCreationCount, Is.EqualTo(2));
            Assert.That(_renderer.NativeCaptureRendererCount, Is.EqualTo(1), "Independent capture textures share one synchronous mesh pool.");
        }

        /// <summary>Uses a separate nested capture pool while preserving both inner composition and earlier scene artwork.</summary>
        [Test]
        public void NestedCapturesSharePoolsByDepthWithoutOverwritingOuterGeometry()
        {
            BeginFrame();
            DrawKernel(new Vector2(4f, 4f));
            Assert.That(_renderer.DrawNativeClipped("outer-capture", new Rect(40f, 0f, 40f, 32f), () =>
            {
                var drawn = _renderer.DrawResourceSprite("IMAGE_REANIM_blue", new Vector2(48f, 8f), 1f);
                drawn |= _renderer.DrawNativeClipped("inner-capture", new Rect(56f, 0f, 24f, 32f),
                    () => _renderer.DrawResourceSprite("IMAGE_REANIM_green", new Vector2(64f, 8f), 1f));
                return drawn;
            }), Is.True);
            _renderer.EndObjectFrame();
            Capture();
            AssertColor(Pixel(5, 5), Color.red);
            AssertColor(Pixel(48, 8), Color.blue);
            AssertColor(Pixel(64, 8), Color.green);
            Assert.That(_renderer.NativeCaptureRendererCount, Is.EqualTo(2));
            Assert.That(_renderer.NativeSurfaceCount, Is.EqualTo(2));
        }

        /// <summary>Preserves complete plant rows when a large wave interleaves nested zombie captures and particle submissions.</summary>
        [Test]
        public void ZombieFeedbackAndParticlesPreservePreviouslySubmittedPlantRows()
        {
            var particles = new OriginalParticleRenderer(_root, _renderer);
            for (var frame = 0; frame < 3; frame++)
            {
                BeginFrame();
                particles.BeginFrame();
                _renderer.BeginObjectFrame();
                for (var row = 0; row < 3; row++)
                {
                    for (var column = 0; column < 3; column++)
                    {
                        DrawKernel(new Vector2(4f + column * 24f, 4f + row * 20f));
                    }

                    for (var actor = 0; actor < 4; actor++)
                    {
                        _zombie.Id = row * 4 + actor + 100;
                        _zombie.PresentationFlash = .5f;
                        _actors.DrawZombie(_zombie, "Zombie", "anim_walk", new Vector2(100f, 4f + row * 20f),
                            1f, .1f, 12f, true, "burst-body-" + _zombie.Id, "burst-feedback-" + _zombie.Id);
                        Assert.That(particles.Draw("burst-" + _zombie.Id, "Burst", .1f, new Vector2(88f, 4f + row * 20f), _zombie.Id), Is.True);
                    }
                }

                _renderer.EndObjectFrame();
                particles.EndFrame();
                _renderer.EndObjectFrame();
                Capture();
                for (var row = 0; row < 3; row++)
                {
                    for (var column = 0; column < 3; column++)
                    {
                        AssertColor(Pixel(5 + column * 24, 5 + row * 20), Color.red);
                        AssertColor(Pixel(15 + column * 24, 5 + row * 20), Color.yellow);
                    }
                }
            }
        }

        /// <summary>Bounds idle resources after a transient burst and destroys both live and idle captures on disposal.</summary>
        [Test]
        public void CapturePoolBoundsIdleResourcesAndDisposesAllAttachments()
        {
            Func<bool> draw = () => _renderer.DrawResourceSprite("IMAGE_REANIM_red", new Vector2(8f, 8f), 1f);
            BeginFrame();
            for (var actor = 0; actor < 24; actor++)
            {
                Assert.That(_renderer.DrawNativeClipped("burst-" + actor, new Rect(0f, 0f, 32f, 32f), draw), Is.True);
            }

            _renderer.EndObjectFrame();
            _renderer.ResetNativePlayback();
            Assert.That(_renderer.NativeSurfaceCount, Is.Zero);
            Assert.That(_renderer.InactiveNativeSurfaceCount, Is.EqualTo(16));
            _renderer.Dispose();
            Assert.That(_renderer.NativeSurfaceCount, Is.Zero);
            Assert.That(_renderer.InactiveNativeSurfaceCount, Is.Zero);
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

        /// <summary>Uses each original defensive damage image only on its specified body or pumpkin front track.</summary>
        [Test]
        public void DefensiveDamageReplacesOnlyTheOriginalBodyTrack()
        {
            WriteDefensiveFixture();
            var kinds = new[] { PlantKind.Wallnut, PlantKind.TallNut, PlantKind.Pumpkin };
            var names = new[] { "Wallnut", "Tallnut", "Pumpkin" };
            BeginFrame();
            for (var species = 0; species < kinds.Length; species++)
            {
                var maximum = kinds[species] == PlantKind.TallNut ? 8000 : 4000;
                for (var stage = 0; stage < 3; stage++)
                {
                    var plant = new Plant
                    {
                        Id = 500 + species * 10 + stage,
                        Kind = kinds[species],
                        Health = stage == 0 ? maximum : stage == 1 ? maximum * 2 / 3 - 1 : maximum / 3 - 1
                    };
                    var origin = new Vector2(4f + species * 40f, 4f + stage * 20f);
                    _actors.DrawPlant(plant, names[species], origin, 1f, 0f, "anim_idle", true, 12f, null,
                        _keys.Get("plant-", plant.Id), new Rect(0f, 0f, 128f, 64f), 0, 0f, _keys.Get("producer-plant-", plant.Id));
                }
            }

            _renderer.EndObjectFrame();
            Capture();
            for (var species = 0; species < kinds.Length; species++)
            {
                AssertColor(Pixel(15 + species * 40, 5), new Color(.25f, .25f, .25f));
                AssertColor(Pixel(15 + species * 40, 25), Color.green);
                AssertColor(Pixel(15 + species * 40, 45), Color.blue);
                for (var stage = 0; stage < 3; stage++)
                {
                    AssertColor(Pixel(5 + species * 40, 5 + stage * 20), Color.yellow);
                }
            }
        }

        /// <summary>Routes the real shovel hover through the presenter and preserves the unselected adjacent plant's pixels.</summary>
        [Test]
        public void ShovelHoverHighlightsOnlyTheCurrentProductionTarget()
        {
            WriteDefensiveFixture();
            var ownerObject = new GameObject("Inactive shovel pixel regression");
            ownerObject.SetActive(false);
            GamePresenter? presenter = null;
            var previous = SpriteDrawState.Matrix;
            try
            {
                var owner = ownerObject.AddComponent<PvZGame>();
                owner._root = _root;
                owner._animations = _renderer;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 9151);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                owner._game.ResetAdventureWaveSchedule(100000, false, false);
                var first = new Plant { Id = 601, Kind = PlantKind.Wallnut, Layer = PlantLayer.Main, Row = 0, Column = 0, Health = 4000 };
                var second = new Plant { Id = 602, Kind = PlantKind.Wallnut, Layer = PlantLayer.Main, Row = 0, Column = 1, Health = 4000 };
                owner._game.Plants.Add(first);
                owner._game.Plants.Add(second);
                owner._shovel = true;
                presenter = new GamePresenter(owner);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
                for (var frame = 0; frame < 2; frame++)
                {
                    owner.AdventureBoard._adventurePointer = new Vector2(80f + frame * 80f, 120f);
                    Assert.That(owner.Layout.AdventureShovelTarget(owner.AdventureBoard._adventurePointer), Is.SameAs(frame == 0 ? first : second));
                    BeginFrame();
                    SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(-36f, -76f));
                    draw.Invoke(presenter, new object[] { first });
                    draw.Invoke(presenter, new object[] { second });
                    _renderer.EndObjectFrame();
                    Capture();
                    var highlighted = Pixel(frame == 0 ? 15 : 95, 5);
                    var ordinary = Pixel(frame == 0 ? 95 : 15, 5);
                    AssertColor(ordinary, new Color(.25f, .25f, .25f));
                    Assert.That(highlighted.r, Is.GreaterThan(ordinary.r + .08f), "The current shovel target receives the original source-colored additive pass.");
                    Assert.That(highlighted.g, Is.EqualTo(highlighted.r).Within(.02f));
                    Assert.That(highlighted.b, Is.EqualTo(highlighted.r).Within(.02f));
                }
            }
            finally
            {
                SpriteDrawState.Matrix = previous;
                presenter?.Dispose();
                UnityEngine.Object.DestroyImmediate(ownerObject);
            }
        }

        /// <summary>Writes isolated defensive body and damage colors using original resource and track identifiers.</summary>
        private void WriteDefensiveFixture()
        {
            var reanim = Path.Combine(_root, "reanim");
            WriteImage(reanim, "defensive_intact", new Color(.25f, .25f, .25f));
            foreach (var name in new[] { "Wallnut_cracked1", "Tallnut_cracked1", "Pumpkin_damage1" })
            {
                WriteImage(reanim, name, Color.green);
            }

            foreach (var name in new[] { "Wallnut_cracked2", "Tallnut_cracked2", "Pumpkin_damage3" })
            {
                WriteImage(reanim, name, Color.blue);
            }

            File.WriteAllText(Path.Combine(reanim, "Wallnut.reanim"),
                "<fps>12</fps>" + Marker("anim_idle")
                + ImageTrack("anim_face", "defensive_intact", 10, 0) + ImageTrack("unreplaced_leaf", "yellow", 0, 0));
            File.WriteAllText(Path.Combine(reanim, "Tallnut.reanim"),
                "<fps>12</fps>" + ImageTrack("anim_idle", "defensive_intact", 10, 0) + ImageTrack("unreplaced_leaf", "yellow", 0, 0));
            File.WriteAllText(Path.Combine(reanim, "Pumpkin.reanim"),
                "<fps>12</fps>" + Marker("anim_idle")
                + ImageTrack("Pumpkin_front", "defensive_intact", 10, 0) + ImageTrack("Pumpkin_back", "yellow", 0, 0));
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
                GameMemberReflection.SetField(owner, "_presenter", presenter);
                GameMemberReflection.SetField(owner, "_smokePointer", new Vector2(-1000f, -1000f));
                GameMemberReflection.SetField(owner, "_zombiePartAudioLoading", true);
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

        /// <summary>Checks the real board presenter lets a zombie overlap the native bank instead of hiding behind it.</summary>
        /// <returns>Frame boundaries for Canvas and scene camera rendering before reading the overlap pixel.</returns>
        [UnityTest]
        public IEnumerator PlayingSeedBankIsBehindZombieArtwork()
        {
            WriteOptionsFixture();
            var ownerObject = new GameObject("Inactive board layer regression");
            ownerObject.SetActive(false);
            var bank = new Texture2D(446, 87, TextureFormat.RGBA32, false);
            var colors = new Color32[446 * 87];
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32(255, 0, 0, 255);
            }

            bank.SetPixels32(colors);
            bank.Apply();
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            OptionsView? options = null;
            GamePresenter? presenter = null;
            PvZGame? owner = null;
            try
            {
                owner = ownerObject.AddComponent<PvZGame>();
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 8 }, randomSeed: 12345);
                owner._root = _root;
                owner._deck.Clear();
                owner.Textures._textures.Add("SeedBank.png", bank);
                owner._animations = _renderer;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._smokeMode = true;
                owner.AdventureBoard._adventurePointer = new Vector2(-1000f, -1000f);
                options = new OptionsView(_root);
                owner._optionsView = options;
                foreach (var mower in owner._game.Mowers)
                {
                    mower.Available = false;
                    mower.Active = false;
                }

                owner._game.Zombies.Add(new Zombie
                {
                    Id = 333,
                    Kind = ZombieKind.Normal,
                    Row = 0,
                    X = 1f,
                    Health = 200f,
                    StartingHealth = 200f
                });
                presenter = new GamePresenter(owner);
                ObjectSpriteRenderer.SceneCamera.targetTexture = target;
                presenter.Present();
                yield return null;
                yield return null;
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                RenderTexture.active = previous;
                var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
                var x = Mathf.RoundToInt((Screen.width - 800f * scale) * .5f + 65f * scale);
                var y = Mathf.RoundToInt((Screen.height - 600f * scale) * .5f + 44f * scale);
                AssertColor(pixels.GetPixel(x, target.height - y - 1), Color.blue);
                var bankX = Mathf.RoundToInt((Screen.width - 800f * scale) * .5f + 200f * scale);
                var bankY = Mathf.RoundToInt((Screen.height - 600f * scale) * .5f + 20f * scale);
                AssertColor(pixels.GetPixel(bankX, target.height - bankY - 1), Color.red);
            }
            finally
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = null;
                if (owner != null)
                {
                    owner.Textures._textures.Clear();
                }

                presenter?.Dispose();
                options?.Dispose();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(bank);
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
