#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.VerifiedData;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original Vasebreaker artwork, input and sound through the production presentation paths.</summary>
    public sealed class OriginalVasebreakerTests
    {
        /// <summary>Owns the inactive coordinator without loading persistent progress.</summary>
        private GameObject _host = null!;
        /// <summary>Provides the real board controllers and original resources.</summary>
        private PvZGame _owner = null!;
        /// <summary>Submits the original images and authored hammer animation.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Executes the production drawing operations.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Receives real GPU output at the native logical resolution.</summary>
        private RenderTexture _target = null!;
        /// <summary>Reads the rendered pixels for independent comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Resolves private presentation operations without widening runtime visibility.</summary>
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>Creates an isolated original-resource fixture with no game lifecycle or user save writes.</summary>
        /// <returns>A graphics initialization frame.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "images", "Scary_Pot.png")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            yield return null;
            _host = new GameObject("Inactive original Vasebreaker fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = root;
            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._optionsView = new OptionsView(root);
            _animations = new ReanimationRenderer(root);
            _owner._animations = _animations;
            _presenter = new GamePresenter(_owner);
            _owner._presenter = _presenter;
            _target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            SetBoard(0);
        }

        /// <summary>Releases only resources owned by this original-resource fixture.</summary>
        /// <returns>A frame boundary for deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null)
            {
                _presenter.Dispose();
                _animations.Dispose();
                _owner.Textures.ReleaseAll();
                _owner.RequiredOptionsView.Dispose();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
                UnityEngine.Object.Destroy(_host);
            }

            yield return null;
        }

        /// <summary>Checks all Vasebreaker entries omit conveyor artwork and use the same native vase input target.</summary>
        [Test]
        public void AllVasebreakerBoardsOmitConveyorsAndShareGridSelection()
        {
            foreach (var mode in new[] { 0, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 })
            {
                SetBoard(mode);
                Capture(() => Invoke(mode == 0 ? "DrawAdventureSeedBankContents" : "DrawModeSeedBank"));
                Assert.That(VisiblePixels(_pixels.GetPixels32(), new RectInt(0, 0, 500, 80)), Is.Zero,
                    "Vasebreaker " + mode + " must leave the lawn visible above its vases instead of drawing a conveyor or replacement bar.");
                var vase = _owner.RequiredGame.Adventure.Vases[0];
                var point = new Vector2(40f + vase.Column * 80f + 40f, 80f + vase.Row * 100f + 50f);
                _owner.AdventureBoard._adventurePointer = point;
                Assert.That(_owner.AdventureBoard.HighlightedVase, Is.SameAs(vase), "Mode " + mode);
                Assert.That(_owner.AdventureBoard.VaseAtPoint(new Vector2(39f, point.y)), Is.Null);
                if (mode == 0)
                {
                    _owner.AdventureBoard.HandleAdventureClick(point);
                }
                else
                {
                    Assert.That(_owner.IndependentModes.TryHandleModeClick(point), Is.True);
                }

                Assert.That(_owner.RequiredGame.Presentation.VaseMalletColumn, Is.EqualTo(vase.Column));
                Assert.That(_owner.RequiredGame.Presentation.VaseMalletRow, Is.EqualTo(vase.Row));
                Assert.That(_owner.RequiredGame.Presentation.VaseMalletTicks, Is.EqualTo(20));
                Assert.That(_owner.AdventureBoard.HighlightedVase, Is.Null, "The original single active mallet blocks another hover target.");
                if (mode == 0)
                {
                    _owner.AdventureBoard.HandleAdventureClick(point);
                }
                else
                {
                    _owner.IndependentModes.TryHandleModeClick(point);
                }

                Assert.That(_owner.RequiredGame.Presentation.VaseSwingSoundSerial, Is.EqualTo(1), "Clicks during an active swing must be ignored.");
            }
        }

        /// <summary>Checks every puzzle draws and collects its reward at the original dropped-card position.</summary>
        [Test]
        public void VaseRewardsUseNativeGroundCardPositionsAndPickupInput()
        {
            foreach (var mode in new[] { 0, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 })
            {
                SetBoard(mode);
                var vase = SeededVase();
                var spawn = new Vector2(60f + vase.Column * 80f, 80f + vase.Row * 100f);
                ClickBoard(mode, new Vector2(80f + vase.Column * 80f, 130f + vase.Row * 100f));
                AdvanceBoard(mode, .2f);
                Assert.That(_owner.RequiredGame.Adventure.Packets, Has.Count.EqualTo(1));
                var packet = _owner.RequiredGame.Adventure.Packets[0];
                Assert.That(packet.IsVaseDrop, Is.True);
                Assert.That(_owner.Layout.AdventureSeedRect(0, true), Is.EqualTo(new Rect(spawn.x, spawn.y, 50f, 70f)));
                Capture(() => Invoke("DrawAdventureVasePackets"));
                var actual = _pixels.GetPixels32();
                Assert.That(VisiblePixels(actual, new RectInt(0, 0, 800, 600)), Is.GreaterThan(100));
                Capture(() => DrawLiteralSeedPacket(packet, 255));
                AssertMatch(actual, _pixels.GetPixels32(), "The reward uses the original fixed-size card at the vase's usable-coin origin.");
                Capture(() => Invoke(mode == 0 ? "DrawAdventureSeedBankContents" : "DrawModeSeedBank"));
                Assert.That(VisiblePixels(_pixels.GetPixels32(), new RectInt(0, 0, 500, 80)), Is.Zero,
                    "A vase reward must not enter the seed-bank row.");
                var point = spawn + new Vector2(25f, 35f);
                Assert.That(_owner.Layout.AdventurePacketContains(0, point), Is.True);
                Assert.That(_owner.Layout.AdventurePacketContains(0, new Vector2(33f, 43f)), Is.False);
                ClickBoard(mode, point);
                Assert.That(_owner.AdventureBoard._selectedAdventurePacket, Is.EqualTo(packet.Id));
                Assert.That(packet.IsHeld, Is.True);
                Assert.That(_owner.Layout.AdventurePacketContains(0, point), Is.False);
                Capture(() => Invoke("DrawAdventureVasePackets"));
                var held = _pixels.GetPixels32();
                Capture(() => DrawLiteralSeedPacket(packet, 128));
                AssertMatch(held, _pixels.GetPixels32(), "The original collected card remains as a gray-128 ghost at its ground position.");
                var blocked = _owner.RequiredGame.Adventure.Vases[0];
                ClickBoard(mode, new Vector2(80f + blocked.Column * 80f, 130f + blocked.Row * 100f));
                Assert.That(packet.IsHeld, Is.True, "An unopened vase rejects planting without releasing the carried seed.");
                Assert.That(_owner.AdventureBoard._selectedAdventurePacket, Is.EqualTo(packet.Id));
                _owner.Router.CancelBoardSelection();
                Assert.That(packet.IsHeld, Is.False);
                Assert.That(_owner.Layout.AdventurePacketContains(0, point), Is.True);
            }
        }

        /// <summary>Checks the eleventh simultaneous vase reward stays visible and selectable without conveyor limits.</summary>
        [Test]
        public void VaseRewardsBeyondTenRemainVisibleAndSelectable()
        {
            SetBoard(52);
            for (var index = 0; index < 11; index++)
            {
                var vase = SeededVase();
                Assert.That(_owner.RequiredGame.SmashModeVase(vase.Row, vase.Column), Is.True);
                AdvanceBoard(52, .2f);
            }

            Assert.That(_owner.RequiredGame.Adventure.Packets, Has.Count.EqualTo(11));
            Capture(() => Invoke("DrawAdventureVasePackets"));
            var actual = _pixels.GetPixels32();
            Capture(() =>
            {
                foreach (var packet in _owner.RequiredGame.Adventure.Packets)
                {
                    DrawLiteralSeedPacket(packet, 255);
                }
            });
            AssertMatch(actual, _pixels.GetPixels32(), "All ground drops must be drawn, independently of the conveyor's ten-card capacity.");
            var last = _owner.RequiredGame.Adventure.Packets[10];
            var point = _owner.Layout.AdventureSeedRect(10, true).center;
            Assert.That(_owner.Layout.AdventurePacketContains(10, point), Is.True);
            ClickBoard(52, point);
            Assert.That(_owner.AdventureBoard._selectedAdventurePacket, Is.EqualTo(last.Id));
            Assert.That(last.IsHeld, Is.True);
        }

        /// <summary>Checks the original imminent-expiry flash and its opaque gray card during the final fade counter.</summary>
        [Test]
        public void VaseRewardWarningAndFinalFadeUseOriginalGrayness()
        {
            var vase = SeededVase();
            _owner.RequiredGame.SmashAdventureVase(vase.Row, vase.Column);
            _owner.RequiredGame.Update(.2f);
            var packet = _owner.RequiredGame.Adventure.Packets[0];
            packet.GroundTicks = 1201;
            Capture(() => Invoke("DrawAdventureVasePackets"));
            var warning = _pixels.GetPixels32();
            Capture(() => DrawLiteralSeedPacket(packet, 192));
            AssertMatch(warning, _pixels.GetPixels32(), "The last three grounded seconds blink using the original gray-192 card.");
            packet.GroundTicks = 1500;
            packet.FadeTicks = 8;
            Capture(() => Invoke("DrawAdventureVasePackets"));
            AssertMatch(warning, _pixels.GetPixels32(), "PC small-card grayness resets alpha, so the final fifteen ticks remain opaque before removal.");
        }

        /// <summary>Finds an unopened original seed vase without modifying its recipe.</summary>
        /// <returns>The first plant-containing vase in creation order.</returns>
        private AdventureVase SeededVase()
        {
            foreach (var vase in _owner.RequiredGame.Adventure.Vases)
            {
                if (vase._seed.HasValue)
                {
                    return vase;
                }
            }

            throw new AssertionException("The original Vasebreaker recipe has no seed vase.");
        }

        /// <summary>Drives the actual adventure or independent pointer entry point.</summary>
        /// <param name="mode">Zero for adventure, or the original independent mode identifier.</param>
        /// <param name="point">The original logical pointer coordinate.</param>
        private void ClickBoard(int mode, Vector2 point)
        {
            _owner.AdventureBoard._adventurePointer = point;
            if (mode == 0)
            {
                _owner.AdventureBoard.HandleAdventureClick(point);
            }
            else
            {
                Assert.That(_owner.IndependentModes.TryHandleModeClick(point), Is.True);
            }
        }

        /// <summary>Advances the public update loop belonging to the current puzzle.</summary>
        /// <param name="mode">Zero for adventure, or the original independent mode identifier.</param>
        /// <param name="seconds">The native simulation time to advance.</param>
        private void AdvanceBoard(int mode, float seconds)
        {
            if (mode == 0)
            {
                _owner.RequiredGame.Update(seconds);
            }
            else
            {
                _owner.IndependentModes.RequiredModeSession.Update(seconds);
            }
        }

        /// <summary>Draws the original seed background and portrait using independent fixed-size pickup geometry.</summary>
        /// <param name="packet">The reward whose seed selects the original portrait atlas cell.</param>
        /// <param name="grayness">The literal PC card brightness to apply.</param>
        private void DrawLiteralSeedPacket(AdventurePacket packet, int grayness)
        {
            var background = _owner.Textures.LoadImage("seeds.png")!;
            var cell = AlmanacPortraitLayout.PlantAtlasCell((int)packet.Kind);
            var matrix = Matrix4x4.Translate(new Vector3((int)packet.PresentationX, (int)packet.PresentationY, 0f));
            var tint = new Color(grayness / 255f, grayness / 255f, grayness / 255f, 1f);
            Pool.DrawQuad(background, 50f, 70f, new Rect(2f / 9f, 0f, 1f / 9f, 1f), matrix, tint, 0);
            if (cell >= 0)
            {
                var portraits = _owner.Textures.LoadImage("packet_plants.png")!;
                Pool.DrawQuad(portraits, 50f, 70f, new Rect(cell / 13f, 0f, 1f / 13f, 1f), matrix, tint, 0);
                return;
            }

            AlmanacPortraitLayout.PlantThumbnailTransform((int)packet.Kind, new Rect(0f, 0f, 50f, 70f), out var origin, out var scale);
            Assert.That(VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[(int)packet.Kind].ReanimationId, out var definition), Is.True);
            var animation = Path.GetFileNameWithoutExtension(definition!.Path.Replace('\\', '/'));
            origin += new Vector2((int)packet.PresentationX, (int)packet.PresentationY);
            var previous = SpriteDrawState.Color;
            try
            {
                SpriteDrawState.Color = tint;
                Assert.That(_animations.DrawNative(animation, origin, scale, 0f), Is.True);
            }
            finally
            {
                SpriteDrawState.Color = previous;
            }
        }

        /// <summary>Compares the exact original foreground atlas cell and its additional alpha-196 additive pass.</summary>
        [Test]
        public void HoverUsesOriginalForegroundAndAdditiveSelection()
        {
            var vase = _owner.RequiredGame.Adventure.Vases[0];
            vase.Row = 4;
            vase.Column = 8;
            _owner.RequiredGame.Adventure._vases.Clear();
            _owner.RequiredGame.Adventure._vases.Add(vase);
            var source = _owner.Textures.LoadImage("Scary_Pot.png")!;
            Assert.That(source.Width, Is.EqualTo(240));
            Assert.That(source.Height, Is.EqualTo(202));
            var destination = new Rect(675f, 465f, 80f, 101f);
            _owner.AdventureBoard._adventurePointer = new Vector2(0f, 0f);
            Capture(() => Invoke("DrawAdventureVase", vase));
            var normal = _pixels.GetPixels32();
            Capture(() => DrawLiteralVase(source, destination, false));
            AssertMatch(normal, _pixels.GetPixels32(), "The native foreground is the 80x101 cel at grid(-5,-15), atlas row one.");
            _owner.AdventureBoard._adventurePointer = new Vector2(799f, 599f);
            Assert.That(_owner.AdventureBoard.HighlightedVase, Is.SameAs(vase), "Native grid conversion includes the bottom-right board edge.");
            Capture(() => Invoke("DrawAdventureVase", vase));
            var highlighted = _pixels.GetPixels32();
            Save("vase-original-highlight.png");
            Capture(() => DrawLiteralVase(source, destination, true));
            AssertMatch(highlighted, _pixels.GetPixels32(), "Hover adds the same native foreground cel with original alpha 196.");
            Assert.That(DifferentPixels(normal, highlighted), Is.GreaterThan(100));
            _owner._shovel = true;
            Assert.That(_owner.AdventureBoard.HighlightedVase, Is.Null);
            Capture(() => Invoke("DrawAdventureVase", vase));
            AssertMatch(normal, _pixels.GetPixels32(), "A held shovel must not select a vase.");
        }

        /// <summary>Checks the mallet uses the authored pot-opening clip at its fixed clicked cell.</summary>
        [Test]
        public void PotOpeningHammerUsesFixedCellAndOriginalFortyFpsClip()
        {
            var vase = _owner.RequiredGame.Adventure.Vases[0];
            var origin = new Vector2(40f + vase.Column * 80f, 80f + vase.Row * 100f);
            _owner.AdventureBoard.HandleAdventureClick(origin + new Vector2(40f, 50f));
            _owner.RequiredGame.Update(.08f);
            _owner.AdventureBoard._adventurePointer = new Vector2(20f, 20f);
            Capture(() => Invoke("DrawAdventureVaseHammer"));
            var actual = _pixels.GetPixels32();
            Save("vase-original-mallet.png");
            Assert.That(VisiblePixels(actual, new RectInt(0, 0, 800, 600)), Is.GreaterThan(30));
            Capture(() => Assert.That(_animations.DrawNativePlayback("Hammer", origin, 1f,
                _owner.RequiredGame.Presentation.VaseMalletAge + _owner.RequiredGame.AdventurePresentationRemainder, "anim_open_pot", false, 40f), Is.True));
            AssertMatch(actual, _pixels.GetPixels32(), "The vase hammer stays at the cell origin and samples eight authored frames at 40 FPS.");
            _owner.RequiredGame.Update(.12f);
            Capture(() => Invoke("DrawAdventureVaseHammer"));
            Assert.That(VisiblePixels(_pixels.GetPixels32(), new RectInt(0, 0, 800, 600)), Is.Zero,
                "The native twentieth update removes the hammer instead of retaining a cursor-following pose.");
        }

        /// <summary>Checks the stage-clear transition submits the original ScreenFlash particle with its authored warm tint.</summary>
        [Test]
        public void StageClearUsesOriginalScreenFlashParticle()
        {
            // Fullscreen particles use the real display viewport rather than the fixture's 800x600 surface.
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _owner.RequiredGame.Adventure._vases.Clear();
            _owner.RequiredGame.Update(.01f);
            _owner.RequiredGame.Update(2f);
            _owner.RequiredGame.Update(.1f);
            Assert.That(_owner.RequiredGame.Presentation.VaseScreenFlashSerial, Is.EqualTo(1));
            Capture(() =>
            {
                Invoke("BeginAdventurePresentation");
                Invoke("DrawAdventureVaseScreenFlash");
                Invoke("EndAdventurePresentation");
            }, Color.black);
            var particles = (OriginalParticleRenderer)typeof(GamePresenter).GetField("_adventureParticles", PrivateInstance)!.GetValue(_presenter)!;
            Assert.That(particles.LastError, Is.Null.Or.Empty);
            var center = _pixels.GetPixel(Screen.width / 2, Screen.height / 2);
            Assert.That(center.r, Is.GreaterThan(.1f), "The native fullscreen particle must contribute actual camera pixels.");
            Assert.That(center.g, Is.EqualTo(center.r).Within(.02f));
            Assert.That(center.b / center.r, Is.EqualTo(.8f).Within(.08f), "ScreenFlash.xml authors a warm white RGB(1,1,.8) flash.");
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var offset = new Vector2((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f);
            foreach (var corner in new[] { new Vector2(2f, 2f), new Vector2(798f, 2f), new Vector2(2f, 598f), new Vector2(798f, 598f) })
            {
                var point = offset + corner * scale;
                var color = _pixels.GetPixel((int)point.x, Screen.height - 1 - (int)point.y);
                Assert.That(color.r, Is.EqualTo(center.r).Within(.02f), "The flash must cover every corner of the game viewport.");
                Assert.That(color.g, Is.EqualTo(center.g).Within(.02f));
                Assert.That(color.b, Is.EqualTo(center.b).Within(.02f));
            }

            Save("vase-original-stage-flash.png");
        }

        /// <summary>Decodes the original samples and checks real start and impact playback through production audio.</summary>
        /// <returns>Audio decoding and playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator OriginalSwingBonkAndVaseBreakingReachProductionAudio()
        {
            var audioHost = new GameObject("Original vase audio voices");
            var source = audioHost.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            using var voices = new AudioVoicePool(audioHost, source, 8);
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audioHost.AddComponent<AudioListener>();
            }

            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            var effects = _owner.Sounds;
            effects._audioSource = source;
            effects._soundVolume = .7f;
            effects._soundVoices = voices;
            typeof(GameSoundEffects).GetField("_zombiePartAudioLoading", PrivateInstance)!.SetValue(effects, true);
            try
            {
                foreach (var name in new[] { "swing", "bonk", "vase_breaking" })
                {
                    using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(_owner._root, "sounds", name + ".ogg")).AbsoluteUri, AudioType.OGGVORBIS);
                    yield return request.SendWebRequest();
                    Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), name + ": " + request.error);
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    var data = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(data, 0), Is.True);
                    Assert.That(Array.Exists(data, value => Math.Abs(value) > .001f), Is.True, name);
                    effects._presentationSounds[name] = clip;
                }

                var vase = _owner.RequiredGame.Adventure.Vases[0];
                Assert.That(_owner.RequiredGame.SmashAdventureVase(vase.Row, vase.Column), Is.True);
                effects.UpdateAdventureVaseAudio();
                yield return null;
                AssertVoice(audioHost, effects._presentationSounds["swing"]);
                Assert.That(source.isPlaying, Is.False, "Bonk is deferred until the pot opens.");
                voices.StopAll();
                _owner.RequiredGame.Update(.20f);
                effects.UpdateAdventureVaseAudio();
                yield return null;
                Assert.That(source.isPlaying, Is.True, "The impact must play the original unpitched Bonk one-shot.");
                AssertVoice(audioHost, effects._presentationSounds["vase_breaking"]);
                voices.StopAll();
                source.Stop();
                _owner.RequiredGame.Presentation.VaseBreakSoundSerial += 2;
                effects.UpdateAdventureVaseAudio();
                yield return null;
                var breaking = effects._presentationSounds["vase_breaking"];
                Assert.That(Array.FindAll(audioHost.GetComponents<AudioSource>(), voice => voice.isPlaying && voice.clip == breaking),
                    Has.Length.EqualTo(0), "PC Foley 35 retains its ten-update admission window for simultaneous pot openings.");
                Assert.That(source.isPlaying, Is.True, "Each opened pot still plays the direct, unpitched Bonk sample.");
                voices.StopAll();
                source.Stop();
                effects.UpdateAdventureVaseAudio();
                yield return null;
                Assert.That(Array.Exists(audioHost.GetComponents<AudioSource>(), voice => voice.isPlaying), Is.False,
                    "Already-consumed sound serials must not replay on another host update.");
            }
            finally
            {
                voices.StopAll();
                AudioListener.pause = previousPause;
                foreach (var clip in effects._presentationSounds.Values)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                effects._presentationSounds.Clear();
                UnityEngine.Object.Destroy(audioHost);
            }
        }

        /// <summary>Switches only the in-memory fixture between adventure and independent Vasebreaker boards.</summary>
        /// <param name="mode">Zero for adventure 4-5, or an independent original PC mode identifier.</param>
        private void SetBoard(int mode)
        {
            _owner.IndependentModes._modeSession = mode == 0 ? null : new ModeSession(mode, randomSeed: 7529);
            _owner._game = mode == 0
                ? new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529)
                : _owner.IndependentModes.RequiredModeSession.Game;
            _owner._selected = -1;
            _owner._shovel = false;
            _owner.AdventureBoard._selectedAdventurePacket = -1;
            _owner.AdventureBoard._adventurePointer = Vector2.zero;
        }

        /// <summary>Draws the original foreground cel and literal separate native additive pass.</summary>
        /// <param name="image">The unchanged original Scary_Pot atlas.</param>
        /// <param name="destination">The literal native foreground rectangle.</param>
        /// <param name="highlight">Whether the additional alpha-196 source-colored pass is present.</param>
        private void DrawLiteralVase(SpriteImage image, Rect destination, bool highlight)
        {
            var matrix = Matrix4x4.Translate(new Vector3(destination.x, destination.y, 0f));
            Pool.DrawQuad(image, destination.width, destination.height, new Rect(0f, 0f, 1f / 3f, .5f), matrix, Color.white, 0);
            if (highlight)
            {
                Pool.DrawQuad(image, destination.width, destination.height, new Rect(0f, 0f, 1f / 3f, .5f), matrix,
                    new Color(1f, 1f, 1f, 196f / 255f), 2);
            }
        }

        /// <summary>Gets the original renderer's submitted sprite pool for independent GPU reference draws.</summary>
        private ObjectSpriteRenderer Pool => (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", PrivateInstance)!.GetValue(_animations)!;

        /// <summary>Submits an isolated native-resolution production or reference frame and reads real GPU output.</summary>
        /// <param name="draw">The production or reference drawing operation.</param>
        /// <param name="background">The optional solid reference background.</param>
        private void Capture(Action draw, Color? background = null)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Color = background ?? Color.white;
            SpriteDrawState.Clip = new Rect(0f, 0f, _target.width, _target.height);
            _animations.BeginObjectFrame();
            _animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, _target.width, _target.height), new Rect(0f, 0f, 1f, 1f));
            SpriteDrawState.Color = Color.white;
            typeof(GamePresenter).GetField("_renderingBoardObjects", PrivateInstance)!.SetValue(_presenter, true);
            try
            {
                draw();
            }
            finally
            {
                typeof(GamePresenter).GetField("_renderingBoardObjects", PrivateInstance)!.SetValue(_presenter, false);
                _animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            Pool.RenderTo(_target);
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
            _pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Invokes an existing production drawing entry point.</summary>
        /// <param name="name">The private presenter method name.</param>
        /// <param name="arguments">The method arguments.</param>
        private void Invoke(string name, params object[] arguments)
        {
            typeof(GamePresenter).GetMethod(name, PrivateInstance)!.Invoke(_presenter, arguments);
        }

        /// <summary>Checks that a decoded original sample is playing on a real pooled source.</summary>
        /// <param name="host">The object owning production audio voices.</param>
        /// <param name="clip">The decoded original sample expected on a voice.</param>
        private static void AssertVoice(GameObject host, AudioClip clip)
        {
            Assert.That(Array.Exists(host.GetComponents<AudioSource>(), voice => voice.isPlaying && voice.clip == clip), Is.True, clip.name);
        }

        /// <summary>Compares rendered production pixels with independent original artwork.</summary>
        /// <param name="actual">The production pixels.</param>
        /// <param name="expected">The independent reference pixels.</param>
        /// <param name="message">The original behavior being checked.</param>
        private static void AssertMatch(Color32[] actual, Color32[] expected, string message)
        {
            Assert.That(DifferentPixels(actual, expected), Is.LessThan(4), message);
        }

        /// <summary>Counts visible RGB differences beyond render rounding tolerance.</summary>
        /// <param name="first">The first complete camera readback.</param>
        /// <param name="second">The second complete camera readback.</param>
        /// <returns>The number of visibly different pixels.</returns>
        private static int DifferentPixels(Color32[] first, Color32[] second)
        {
            var count = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 6)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Counts original artwork over a white native-resolution reference background.</summary>
        /// <param name="pixels">The complete bottom-left-origin camera readback.</param>
        /// <param name="area">The top-left-origin logical area to inspect.</param>
        /// <returns>The number of visibly nonwhite pixels.</returns>
        private static int VisiblePixels(Color32[] pixels, RectInt area)
        {
            var count = 0;
            for (var y = area.yMin; y < area.yMax; y++)
            {
                for (var x = area.xMin; x < area.xMax; x++)
                {
                    var color = pixels[(599 - y) * 800 + x];
                    if (color.r + color.g + color.b < 714)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Writes optional diagnostic screenshots only into the explicitly selected output directory.</summary>
        /// <param name="name">The diagnostic filename.</param>
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
