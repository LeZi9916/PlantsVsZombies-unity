#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;
using PlantsVsZombies.Diagnostics;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Regresses native bank capacity, empty chooser artwork, and introduction handoff coordinates.</summary>
    public sealed class SeedBankLayoutTests
    {
        /// <summary>Owns the inactive production coordinator without bootstrapping progress or audio.</summary>
        private GameObject _object = null!;
        /// <summary>Routes the same capacity, packet bounds, and introduction frames used by the player.</summary>
        private PvZGame _owner = null!;
        /// <summary>Draws the production chooser bank through retained Canvas and scene meshes.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Owns the synthetic packet image renderer.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>Contains only isolated test resources.</summary>
        private string _root = "";
        /// <summary>Owns decoded source textures used to distinguish occupied slots from empty silhouettes.</summary>
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        /// <summary>Receives the real Canvas and scene camera output.</summary>
        private RenderTexture _target = null!;
        /// <summary>Reads the frame in original logical coordinates.</summary>
        private Texture2D _pixels = null!;

        /// <summary>Creates a ten-slot board with four legal selected cards and isolated test artwork.</summary>
        /// <returns>A frame boundary before creating graphics resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZSeedBank-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "reanim"));
            _object = new GameObject("Inactive native seed bank regression");
            _object.SetActive(false);
            _owner = _object.AddComponent<PvZGame>();
            _owner._root = _root;
            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8, AdventurePacketSlots = 10 }, 9151);
            var seeds = new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.CherryBomb, PlantKind.Wallnut };
            _owner._game.Adventure.AvailableSeeds = Array.AsReadOnly(seeds);
            Assert.That(_owner._game.ChooseAdventureSeeds(seeds), Is.True);
            _owner._game.ResetAdventureWaveSchedule(100000, false, false);
            _owner._deck.Clear();
            _owner._deck.AddRange(seeds);
            _owner._pendingSeeds = new AdventureSeedChooserState(seeds);
            _owner.Textures._textures.Add("SeedBank.png", Texture(446, 87, Color.red));
            _owner.Textures._textures.Add("SeedPacketSilhouette.png", Texture(50, 70, Color.green));
            _owner.Textures._textures.Add("seeds.png", Texture(450, 70, Color.blue));
            _owner.Textures._textures.Add("packet_plants.png", Texture(650, 70, Color.clear));
            _renderer = new ReanimationRenderer(_root);
            _owner._animations = _renderer;
            _presenter = new GamePresenter(_owner);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
        }

        /// <summary>Releases only the isolated coordinator, graphics, and generated fixture directory.</summary>
        /// <returns>A frame boundary for deferred renderer destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.SceneCamera.targetTexture = null;
            _owner.Textures._textures.Clear();
            _presenter.Dispose();
            _renderer.Dispose();
            UnityEngine.Object.DestroyImmediate(_object);
            foreach (var texture in _textures)
            {
                UnityEngine.Object.Destroy(texture);
            }

            _textures.Clear();
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Uses the purchased ten-slot layout while the bank contains only four packets.</summary>
        [Test]
        public void FourSelectedCardsKeepTheTenSlotPacketStride()
        {
            Assert.That(_owner.Layout.SeedBankCapacity, Is.EqualTo(10));
            for (var index = 0; index < 4; index++)
            {
                Assert.That(_owner.Layout.AdventureSeedRect(index), Is.EqualTo(new Rect(79 + index * 51, 8, 50, 70)));
                Assert.That(_owner.Layout.SeedRect(index), Is.EqualTo(_owner.Layout.AdventureSeedRect(index)));
                _owner.RequiredPendingSeeds.GetPosition(_owner.RequiredPendingSeeds[index], 0, 0, _owner.Layout.SeedBankCapacity, out var x, out var y);
                Assert.That((x, y), Is.EqualTo((79 + index * 51, 8)));
            }
        }

        /// <summary>Reads independent mode capacity even when its adventure backing board has a different fixed count.</summary>
        [Test]
        public void ModePacketsUseTheActualModeBankCapacity()
        {
            _owner.IndependentModes._modeSession = new ModeSession(1, packetSlots: 10, unlockedBasicSeeds: 4);
            _owner._game = _owner.IndependentModes._modeSession.Game;
            _owner._deck.Clear();
            _owner._deck.AddRange(_owner._game.Adventure.AvailableSeeds);
            Assert.That(_owner.Layout.SeedBankCapacity, Is.EqualTo(10));
            Assert.That(_owner._deck.Count, Is.EqualTo(4));
            Assert.That(_owner.IndependentModes.ModeSeedRect(3), Is.EqualTo(new Rect(232, 8, 50, 70)));
        }

        /// <summary>Preserves the original first-adventure fixed one-to-four packet counts.</summary>
        /// <param name="level">The tutorial adventure level with an original fixed packet count.</param>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TutorialFixedPacketCountsRemainUnchanged(int level)
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = level, AdventurePacketSlots = 10 }, 9151);
            Assert.That(_owner.Layout.SeedBankCapacity, Is.EqualTo(level));
            Assert.That(_owner.Layout.AdventureSeedRect(0).x, Is.EqualTo(85));
        }

        /// <summary>Retains the original ten-pixel return curve position through the final intro frame and first gameplay frame.</summary>
        [Test]
        public void IntroHandoffKeepsBankAndInputCoordinatesContinuous()
        {
            var clock = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = 8 }));
            while (!clock.WaitingForSeeds)
            {
                clock.Tick();
            }

            clock.BeginReturn();
            while (clock.NativeTime < clock.Plan.End - AdventureIntroPresentation.NativeUpdate)
            {
                clock.Tick();
            }

            _owner.Opening._adventureIntro = clock;
            _owner.Opening._adventureIntroFrame = clock.Frame;
            var lastIntro = _owner.Layout.AdventureSeedRect(1);
            Assert.That(_owner.Opening.AdventureIntroActive, Is.True);
            Assert.That(_owner.Layout.AdventureSeedBankOrigin, Is.EqualTo(new Vector2(10, 0)));
            clock.Tick();
            _owner.Opening._adventureIntroFrame = clock.Frame;
            Assert.That(_owner.Opening.AdventureIntroActive, Is.False);
            Assert.That(_owner.Layout.AdventureSeedRect(1), Is.EqualTo(lastIntro));
            Assert.That(lastIntro.x, Is.EqualTo(140));
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(_owner._game, 9990);
            var cooldowns = (int[])typeof(GameSimulation).GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner._game)!;
            Array.Clear(cooldowns, 0, cooldowns.Length);
            GameMemberReflection.Invoke(_owner, "HandleAdventureClick", new object[] { lastIntro.center });
            Assert.That(_owner._selected, Is.EqualTo(1), "The first gameplay click uses the retained final bank origin.");
        }

        /// <summary>Renders all ten original chooser positions when four slots are occupied and six remain empty.</summary>
        /// <returns>Camera and Canvas frame boundaries before reading the pixels.</returns>
        [UnityTest]
        public IEnumerator TenSlotChooserDrawsFourCardsAndSixOriginalSilhouettes()
        {
            DrawChooserBank();
            yield return null;
            yield return null;
            Capture();
            for (var index = 0; index < 10; index++)
            {
                AssertColor(Pixel(79 + index * 51 + 25, 40), index < 4 ? Color.blue : Color.green);
            }

            AssertColor(Pixel(595, 40), Color.red);
            Assert.That(_owner.RequiredPendingSeeds.Choose(PlantKind.PotatoMine, 181, 128, _owner.Layout.SeedBankCapacity), Is.True);
            DrawChooserBank();
            yield return null;
            yield return null;
            Capture();
            AssertColor(Pixel(79 + 4 * 51 + 25, 40), Color.green);
            _owner.RequiredPendingSeeds.Advance(.25f);
            DrawChooserBank();
            yield return null;
            yield return null;
            Capture();
            AssertColor(Pixel(79 + 4 * 51 + 25, 40), Color.blue);
        }

        /// <summary>Draws one actual chooser-bank traversal at the shared original logical viewport.</summary>
        private void DrawChooserBank()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1));
            _renderer.BeginObjectFrame();
            typeof(GamePresenter).GetMethod("DrawChooserBank", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, new object[] { _owner.Layout.SeedBankCapacity });
            _renderer.EndObjectFrame();
            CanvasUI.EndFrame();
        }

        /// <summary>Creates one decoded synthetic source texture and retains its lifetime for this test.</summary>
        /// <param name="width">The original source width.</param>
        /// <param name="height">The original source height.</param>
        /// <param name="color">The color distinguishing this source from adjacent artwork.</param>
        /// <returns>The uploaded source texture.</returns>
        private Texture2D Texture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _textures.Add(texture);
            return texture;
        }

        /// <summary>Reads the real camera output after a complete Canvas frame.</summary>
        private void Capture()
        {
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

        /// <summary>Reads a color using the same logical-to-screen conversion as production input.</summary>
        /// <param name="x">The logical original horizontal coordinate.</param>
        /// <param name="y">The logical original downward coordinate.</param>
        /// <returns>The captured color at that logical point.</returns>
        private Color Pixel(float x, float y)
        {
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var pixelX = Mathf.RoundToInt((Screen.width - 800f * scale) * .5f + x * scale);
            var pixelY = Mathf.RoundToInt((Screen.height - 600f * scale) * .5f + y * scale);
            return _pixels.GetPixel(pixelX, _target.height - pixelY - 1);
        }

        /// <summary>Checks source colors with tolerance for native Canvas and scene texture sampling.</summary>
        /// <param name="actual">The captured camera color.</param>
        /// <param name="expected">The source color expected at the original packet position.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.05f), "red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.05f), "green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.05f), "blue");
        }
    }
}
