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
            var images = Directory.CreateDirectory(Path.Combine(_root, "images")).FullName;
            var optionImage = Texture(2, 2, Color.white).EncodeToPNG();
            foreach (var name in new[] { "options_menuback.jpg", "options_menuback_.png", "options_sliderslot.png", "options_sliderknob2.png",
                "options_checkbox0.png", "options_checkbox1.png", "options_backtogamebutton0.png", "options_backtogamebutton2.png",
                "button_left.png", "button_middle.png", "button_right.png" })
            {
                File.WriteAllBytes(Path.Combine(images, name), optionImage);
            }

            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8, AdventurePacketSlots = 10 }, 9151);
            var seeds = new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.CherryBomb, PlantKind.Wallnut };
            _owner._game.Adventure.AvailableSeeds = Array.AsReadOnly(seeds);
            Assert.That(_owner._game.ChooseAdventureSeeds(seeds), Is.True);
            _owner._game.ResetAdventureWaveSchedule(100000, false, false);
            _owner._deck.Clear();
            _owner._deck.AddRange(seeds);
            _owner._pendingSeeds = new AdventureSeedChooserState(seeds);
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "SeedBank.png", Texture(446, 87, Color.red));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "SeedPacketSilhouette.png", Texture(50, 70, Color.green));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "seeds.png", Texture(450, 70, Color.blue));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "packet_plants.png", Texture(650, 70, Color.clear));
            File.WriteAllBytes(Path.Combine(_root, "reanim", "marker.png"), Texture(8, 8, Color.blue).EncodeToPNG());
            var marker = "<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_marker</i><f>0</f></t><t/></track>";
            File.WriteAllText(Path.Combine(_root, "reanim", "PuffShroom.reanim"), marker);
            File.WriteAllText(Path.Combine(_root, "reanim", "Pot.reanim"), marker);
            File.WriteAllText(Path.Combine(_root, "reanim", "CoffeeBean.reanim"), marker);
            File.WriteAllText(Path.Combine(_root, "reanim", "GraveBuster.reanim"), marker);
            // Finish creating the source tree before any view acquires its immutable shared image index.
            _owner._optionsView = new OptionsView(_root);
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
            _owner.Textures.ReleaseAll();
            _presenter.Dispose();
            _renderer.Dispose();
            _owner.RequiredOptionsView.Dispose();
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

        /// <summary>Places the original shovel beside the configured bank rather than the number of selected cards.</summary>
        /// <param name="capacity">The purchased seed bank capacity.</param>
        /// <param name="x">The PC 0x416BC0 holder's absolute horizontal coordinate.</param>
        [TestCase(6, 456f)]
        [TestCase(7, 516f)]
        [TestCase(8, 532f)]
        [TestCase(9, 568f)]
        [TestCase(10, 609f)]
        public void ShovelPositionUsesOriginalBankCapacity(int capacity, float x)
        {
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 8, AdventurePacketSlots = capacity }, 9151);
            Assert.That(_owner.Layout.AdventureShovelButton, Is.EqualTo(new Rect(x, 0f, 70f, 72f)));
        }

        /// <summary>Keeps the original slot machine exception while both bowling challenges use conveyor capacity.</summary>
        /// <param name="mode">The original supported challenge identifier.</param>
        /// <param name="x">The absolute original shovel holder coordinate.</param>
        [TestCase(17, 609f)]
        [TestCase(18, 600f)]
        [TestCase(33, 609f)]
        public void ShovelPositionPreservesOriginalChallengeExceptions(int mode, float x)
        {
            _owner.IndependentModes._modeSession = new ModeSession(mode, packetSlots: 6);
            _owner._game = _owner.IndependentModes.RequiredModeSession.Game;
            Assert.That(_owner.Layout.AdventureShovelButton, Is.EqualTo(new Rect(x, 0f, 70f, 72f)));
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

        /// <summary>Uses PC 0x49F100's conveyor artwork throughout its native introduction slide.</summary>
        /// <returns>Frames rendering every adventure conveyor mode and the partial slide of level 2-10.</returns>
        [UnityTest]
        public IEnumerator ConveyorIntroductionsUseTheMovingConveyorWithoutSeedSlots()
        {
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "ConveyorBelt_backdrop.png", Texture(516, 87, Color.green));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "ConveyorBelt.png", Texture(512, 84, Color.yellow));
            foreach (var number in new[] { 5, 10, 20, 25, 30, 40, 45, 50 })
            {
                _owner._game = new GameSimulation(new GameRules { AdventureLevel = number }, 9151);
                var level = _owner.RequiredGame.Adventure.Level;
                var clock = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
                {
                    Level = number,
                    NativeBackground = (int)level.Terrain,
                    IsConveyor = level.HasConveyor,
                    IsStormyNight = level.Mode == AdventureMode.Storm,
                    IsFinalBoss = level.Mode == AdventureMode.Boss,
                    ChooseSeedsOverride = level.ChoosesSeeds
                }));
                _owner.Opening._adventureIntro = clock;
                var target = clock.Plan.BankOnStart + (number == 20 ? 120 : 260);
                while (clock.NativeTime < target)
                {
                    clock.Tick();
                }

                _owner.Opening._adventureIntroFrame = clock.Frame;
                Assert.That(_owner.RequiredGame.UsesNativeAdventureConveyor, Is.True);
                Assert.That(_owner.Opening.AdventureIntroActive, Is.True);
                yield return Render(() => typeof(GamePresenter).GetMethod("DrawAdventureOpeningInterface",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null));
                AssertColor(Pixel(20, 10), Color.white);
                AssertColor(Pixel(110, 10), Color.green);
                if (number == 20)
                {
                    Assert.That(clock.Frame.SeedBankY, Is.InRange(-86, -1));
                    AssertColor(Pixel(110, 50), Color.white);
                    while (clock.NativeTime < clock.Plan.BankOnEnd + 10)
                    {
                        clock.Tick();
                    }

                    _owner.Opening._adventureIntroFrame = clock.Frame;
                    yield return Render(() => typeof(GamePresenter).GetMethod("DrawAdventureOpeningInterface",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null));
                    AssertColor(Pixel(110, 50), Color.green);
                }
                else
                {
                    AssertColor(Pixel(110, 70), Color.yellow);
                }
            }
        }

        /// <summary>Preserves PC 0x425B40's bank translation across the last opening and first playing frame.</summary>
        /// <returns>Actual camera frames covering every adventure conveyor introduction and its gameplay handoff.</returns>
        [UnityTest]
        public IEnumerator ConveyorHandoffKeepsNativeArtworkPacketAndInputCoordinates()
        {
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "ConveyorBelt_backdrop.png", Texture(516, 87, Color.green));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "ConveyorBelt.png", Texture(512, 84, Color.yellow));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var opening = typeof(GamePresenter).GetMethod("DrawAdventureOpeningInterface", flags)!;
            var playing = typeof(GamePresenter).GetMethod("DrawAdventureSeedBankContents", flags)!;
            var add = typeof(GameSimulation).GetMethod("AddAdventurePacket", flags)!;
            foreach (var number in new[] { 5, 10, 20, 25, 30, 40, 45, 50 })
            {
                _owner._game = new GameSimulation(new GameRules { AdventureLevel = number }, 9151);
                _owner.AdventureBoard._selectedAdventurePacket = -1;
                var level = _owner.RequiredGame.Adventure.Level;
                var clock = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
                {
                    Level = number,
                    NativeBackground = (int)level.Terrain,
                    IsConveyor = level.HasConveyor,
                    IsStormyNight = level.Mode == AdventureMode.Storm,
                    IsFinalBoss = level.Mode == AdventureMode.Boss,
                    ChooseSeedsOverride = level.ChoosesSeeds
                }));
                _owner.Opening._adventureIntro = clock;
                while (clock.NativeTime < clock.Plan.End - AdventureIntroPresentation.NativeUpdate)
                {
                    clock.Tick();
                }

                _owner.Opening._adventureIntroFrame = clock.Frame;
                // Bowling and the boss legitimately prefill packets; this geometry fixture supplies two explicit slots.
                _owner.RequiredGame.Adventure._packets.Clear();
                var first = (AdventurePacket)add.Invoke(_owner.RequiredGame, new object?[] { PlantKind.Peashooter, null, null })!;
                first.ConveyorOffsetX = 0;
                var last = (AdventurePacket)add.Invoke(_owner.RequiredGame, new object?[] { PlantKind.Peashooter, null, null })!;
                last.ConveyorOffsetX = 440;
                Assert.That(_owner.Opening.AdventureIntroActive, Is.True);
                yield return Render(() => opening.Invoke(_presenter, null));
                AssertNativeConveyorGeometry();
                var packet = _owner.Layout.AdventureSeedRect(0, true);
                var openingPixels = new[] { Pixel(96, 40), Pixel(126, 40), Pixel(155, 70), Pixel(595, 40), Pixel(604, 40) };
                clock.Tick();
                _owner.Opening._adventureIntroFrame = clock.Frame;
                Assert.That(_owner.Opening.AdventureIntroActive, Is.False);
                yield return Render(() => playing.Invoke(_presenter, null));
                AssertNativeConveyorGeometry();
                Assert.That(_owner.Layout.AdventureSeedRect(0, true), Is.EqualTo(packet));
                var playingPixels = new[] { Pixel(96, 40), Pixel(126, 40), Pixel(155, 70), Pixel(595, 40), Pixel(604, 40) };
                for (var index = 0; index < openingPixels.Length; index++)
                {
                    AssertColor(playingPixels[index], openingPixels[index]);
                }

                GameMemberReflection.Invoke(_owner, "HandleAdventureClick", new object[] { packet.center });
                Assert.That(_owner.AdventureBoard._selectedAdventurePacket, Is.EqualTo(first.Id),
                    "The first playing click must use the same native packet coordinates as the opening draw.");
            }

            _owner.Opening._adventureIntro = null;
            _owner.Opening._adventureIntroFrame = null;
            _owner.AdventureBoard._selectedAdventurePacket = -1;
            yield return Render(() => playing.Invoke(_presenter, null));
            AssertNativeConveyorGeometry();
        }

        /// <summary>Checks absolute PC bank-local geometry after its retained ten-pixel translation.</summary>
        private void AssertNativeConveyorGeometry()
        {
            // PC 0x44778C ends at bank X=10; 0x425CC4 adds it before 0x49F100.
            // Backdrop (83,0), belt (90,63), packet (91,8), and clip (90,0,501,600) are bank-local.
            Assert.That(_owner.Layout.AdventureConveyorOrigin, Is.EqualTo(new Vector2(10f, 0f)));
            Assert.That(_owner.Layout.AdventureConveyorClip, Is.EqualTo(new Rect(100f, 0f, 501f, 87f)));
            Assert.That(_owner.Layout.AdventureSeedRect(0, true), Is.EqualTo(new Rect(101f, 8f, 50f, 70f)));
            AssertColor(Pixel(91, 40), Color.white);
            AssertColor(Pixel(96, 40), Color.green);
            AssertColor(Pixel(126, 40), Color.blue);
            AssertColor(Pixel(155, 70), Color.yellow);
            AssertColor(Pixel(595, 40), Color.blue);
            AssertColor(Pixel(604, 40), Color.green);
            Assert.That(_owner.Layout.AdventurePacketContains(1, new Vector2(600f, 40f)), Is.True);
            Assert.That(_owner.Layout.AdventurePacketContains(1, new Vector2(604f, 40f)), Is.False);
        }

        /// <summary>Keeps the shovel at PC 0x4244A0's native offset and below actors crossing the holder.</summary>
        /// <returns>Frame boundaries before checking the actual world and interface composition.</returns>
        [UnityTest]
        public IEnumerator ShovelBankUsesNativeOffsetAndStaysBehindLawnActors()
        {
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "ShovelBank.png", Texture(70, 72, Color.red));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "Shovel.png", Texture(20, 60, Color.green));
            yield return Render(() =>
            {
                typeof(GamePresenter).GetMethod("DrawAdventureBoard", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null);
                _renderer.DrawNativeImage(Texture2D.whiteTexture, new Rect(600, 10, 10, 10), new Rect(0, 0, 1, 1));
                typeof(GamePresenter).GetMethod("DrawAdventureInterface", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null);
            });
            AssertColor(Pixel(603, 5), Color.green);
            AssertColor(Pixel(606, 15), Color.white);
            AssertColor(Pixel(629, 30), Color.red);
        }

        /// <summary>Checks real PC artwork and both shovel input paths across adventure conveyor levels and purchased capacities.</summary>
        /// <returns>Camera frames comparing production composition with the original absolute-coordinate oracle.</returns>
        [UnityTest]
        public IEnumerator OriginalConveyorShovelAlignsWithBeltAndHitArea()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "images", "ConveyorBelt_backdrop.png")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            var backdrop = LoadOriginalImage(root, "ConveyorBelt_backdrop.png");
            var belt = LoadOriginalImage(root, "ConveyorBelt.png");
            var holder = LoadOriginalImage(root, "ShovelBank.png");
            var shovel = LoadOriginalImage(root, "Shovel.png");
            Assert.That(backdrop.width, Is.EqualTo(516));
            Assert.That(holder.width, Is.EqualTo(70));
            yield return Render(() =>
            {
                // PC 0x49F100 uses bank (10,0), while 0x416BC0 directly returns holder (609,0).
                CanvasUI.DrawTexture(new Rect(93f, 0f, backdrop.width, backdrop.height), backdrop);
                CanvasUI.DrawTextureWithTexCoords(new Rect(100f, 63f, belt.width, belt.height / 6f), belt, new Rect(0f, 5f / 6f, 1f, 1f / 6f));
                CanvasUI.DrawTexture(new Rect(609f, 0f, holder.width, holder.height), holder);
                CanvasUI.DrawTexture(new Rect(602f, -3f, shovel.width, shovel.height), shovel);
            });
            var expected = _pixels.GetPixels32();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var drawShovel = typeof(GamePresenter).GetMethod("DrawAdventureShovelBank", flags)!;
            var drawBank = typeof(GamePresenter).GetMethod("DrawAdventureSeedBankContents", flags)!;
            foreach (var capacity in new[] { 6, 10 })
            {
                foreach (var level in new[] { 5, 10, 20, 25, 30, 40, 45, 50 })
                {
                    _owner._game = new GameSimulation(new GameRules { AdventureLevel = level, AdventurePacketSlots = capacity }, 9151);
                    _owner.RequiredGame.Adventure._packets.Clear();
                    _owner._shovel = false;
                    Assert.That(_owner.Layout.AdventureShovelButton, Is.EqualTo(new Rect(609f, 0f, 70f, 72f)));
                    yield return Render(() =>
                    {
                        drawShovel.Invoke(_presenter, null);
                        drawBank.Invoke(_presenter, null);
                    });
                    var actual = _pixels.GetPixels32();
                    var differences = 0;
                    for (var index = 0; index < actual.Length; index++)
                    {
                        if (Math.Abs(actual[index].r - expected[index].r) + Math.Abs(actual[index].g - expected[index].g)
                            + Math.Abs(actual[index].b - expected[index].b) > 12)
                        {
                            differences++;
                        }
                    }

                    Assert.That(differences, Is.LessThan(5), "Original conveyor/shovel composition: level " + level + ", purchased slots " + capacity);
                    _owner.AdventureBoard.HandleAdventureClick(new Vector2(605f, 35f));
                    Assert.That(_owner._shovel, Is.False, "The old overlap strip must not pick up the shovel.");
                    _owner.AdventureBoard.HandleAdventureClick(new Vector2(675f, 35f));
                    Assert.That(_owner._shovel, Is.True, "The visible holder's right edge must pick up the shovel.");
                    if (level == 5)
                    {
                        _owner._shovel = false;
                        _owner.Opening.HandleBowlingShovelLesson(new Vector2(675f, 35f));
                        Assert.That(_owner._shovel, Is.True, "The opening shovel lesson must share the holder's original hit area.");
                    }
                }
            }
        }

        /// <summary>Checks native price ink bounds independently of the presenter's bitmap baseline calculation.</summary>
        /// <returns>Frame boundaries before comparing priced and unpriced production cards.</returns>
        [UnityTest]
        public IEnumerator PacketPricesUseTheOriginalFontAndNativeAnchor()
        {
            foreach (var scale in new[] { 1f, 2f })
            {
                foreach (var kind in new[] { PlantKind.Sunflower, PlantKind.Peashooter })
                {
                    var card = new Rect(100, 160, 50f * scale, 70f * scale);
                    var draw = typeof(GamePresenter).GetMethod("DrawAdventureCard", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    yield return Render(() =>
                    {
                        draw.Invoke(_presenter, new object[] { kind, card, false });
                    });
                    var width = (int)card.width;
                    var height = (int)(80f * scale);
                    var withoutPrice = new Color[width * height];
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            withoutPrice[y * width + x] = Pixel(card.x + x, card.y + y);
                        }
                    }

                    yield return Render(() =>
                    {
                        draw.Invoke(_presenter, new object[] { kind, card, true });
                    });
                    var minX = width;
                    var minY = height;
                    var maxX = -1;
                    var maxY = -1;
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var before = withoutPrice[y * width + x];
                            var after = Pixel(card.x + x, card.y + y);
                            if (Mathf.Abs(before.r - after.r) + Mathf.Abs(before.g - after.g) + Mathf.Abs(before.b - after.b) <= .1f)
                            {
                                continue;
                            }

                            minX = Math.Min(minX, x);
                            minY = Math.Min(minY, y);
                            maxX = Math.Max(maxX, x);
                            maxY = Math.Max(maxY, y);
                        }
                    }

                    // Original Pico129 mask ink with PC 0x49D260's right edge 32 and baseline 54 + ascent 12.
                    var expectedLeft = kind == PlantKind.Sunflower ? 21f : 16f;
                    Assert.That(minX / scale, Is.EqualTo(expectedLeft).Within(1f), kind + " left ink edge");
                    Assert.That((maxX + 1f) / scale, Is.EqualTo(32f).Within(1f), kind + " right ink edge");
                    Assert.That(minY / scale, Is.EqualTo(59f).Within(1f), kind + " top ink edge");
                    Assert.That((maxY + 1f) / scale, Is.EqualTo(66f).Within(1f), kind + " bottom ink edge");
                    Assert.That((maxY - minY + 1f) / scale, Is.EqualTo(7f).Within(1f), kind + " complete original glyph height");
                }
            }
        }

        /// <summary>Applies PC 0x4445FA's packet-type height before resolving the imitation artwork.</summary>
        /// <returns>Frame boundaries before checking each real held-cursor draw.</returns>
        [UnityTest]
        public IEnumerator HeldPlantUsesPacketHeightBeforeResolvingImitater()
        {
            var kinds = new[] { PlantKind.PuffShroom, PlantKind.FlowerPot, PlantKind.CoffeeBean, PlantKind.GraveBuster,
                PlantKind.Imitater, PlantKind.Imitater, PlantKind.Imitater };
            var targets = new[] { PlantKind.PuffShroom, PlantKind.FlowerPot, PlantKind.CoffeeBean, PlantKind.GraveBuster,
                PlantKind.PuffShroom, PlantKind.CoffeeBean, PlantKind.GraveBuster };
            var origins = new[] { new Vector2(365, 245), new Vector2(365, 266), new Vector2(377, 262), new Vector2(365, 230),
                new Vector2(365, 240), new Vector2(377, 252), new Vector2(365, 240) };
            _owner.AdventureBoard._adventurePointer = new Vector2(400, 300);
            _owner._selected = 0;
            for (var index = 0; index < kinds.Length; index++)
            {
                _owner._deck.Clear();
                _owner._deck.Add(kinds[index]);
                _owner.RequiredGame.Adventure.ImitaterTarget = targets[index];
                yield return Render(() =>
                {
                    typeof(GamePresenter).GetMethod("DrawAdventurePlantCursor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null);
                });
                var origin = origins[index];
                Assert.That(Pixel(origin.x + 2, origin.y + 2).r, Is.LessThan(.8f), kinds[index] + " / " + targets[index]);
                AssertColor(Pixel(origin.x + 2, origin.y - 2), Color.white);
                AssertColor(Pixel(origin.x - 2, origin.y + 2), Color.white);
            }
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

        /// <summary>Preserves PC 0x4229F0's moving head above the level-progress lettering.</summary>
        /// <returns>Camera and Canvas frame boundaries before checking overlapping original coordinates.</returns>
        [UnityTest]
        public IEnumerator ProgressMeterHeadCoversTheLevelProgressLettering()
        {
            _owner.RequiredGame.Presentation.ProgressMeter.SetBossHealth(100, 200);
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "FlagMeter.png", Texture(158, 54, Color.red));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "FlagMeterParts.png", Texture(75, 25, Color.green));
            SpriteSourceTestUtilities.SetImage(_owner.Textures, "FlagMeterLevelProgress.png", Texture(86, 11, Color.blue));
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1));
            _renderer.BeginObjectFrame();
            typeof(GamePresenter).GetMethod("DrawAdventureProgressAndName", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_presenter, null);
            _renderer.EndObjectFrame();
            CanvasUI.EndFrame();
            yield return null;
            yield return null;
            Capture();
            AssertColor(Pixel(645, 592), Color.blue);
            AssertColor(Pixel(675, 592), Color.green);
        }

        /// <summary>Submits an isolated production operation over white and reads its actual camera output.</summary>
        /// <param name="draw">The production world, interface, or cursor operation to exercise.</param>
        /// <returns>Frame boundaries before capturing retained scene and Canvas geometry.</returns>
        private IEnumerator Render(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f),
                Quaternion.identity, new Vector3(scale, scale, 1));
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(0, 0, 800, 600), Texture2D.whiteTexture);
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

            yield return null;
            yield return null;
            Capture();
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

        /// <summary>Loads one original loose image into this fixture's cache without modifying the supplied artwork.</summary>
        /// <param name="root">The read-only original resource directory.</param>
        /// <param name="name">The original image filename relative to its images directory.</param>
        /// <returns>The decoded source texture retained until fixture teardown.</returns>
        private Texture2D LoadOriginalImage(string root, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _textures.Add(texture);
            Assert.That(texture.LoadImage(File.ReadAllBytes(Path.Combine(root, "images", name))), Is.True, name);
            texture.wrapMode = TextureWrapMode.Clamp;
            SpriteSourceTestUtilities.SetImage(_owner.Textures, name, texture);
            return texture;
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
