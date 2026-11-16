#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the changed original plant artwork, animation poses, target cursor and Foley on a real graphics device.</summary>
    public sealed class OriginalPlantParityGraphicsTests
    {
        /// <summary>Checks both damaged garlic images, hidden stems, and the zombie's original gross-out head pixels.</summary>
        /// <returns>Original-resource camera readback frames.</returns>
        [UnityTest]
        public IEnumerator GarlicDamageAndGrossOutUseOriginalImages()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var game = fixture.Owner.RequiredGame;
            game.ChooseDefaultAdventureSeeds();
            var plant = new Plant { Id = 71, Kind = PlantKind.Garlic, Row = 1, Column = 3,
                Layer = PlantLayer.Main, Age = .25f, PresentationRate = 12f, Health = 400f };
            foreach (var health in new[] { 400f, 200f, 100f })
            {
                plant.Health = health;
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                var actual = fixture.Pixels.GetPixels32();
                var replacement = health == 400f ? null : health == 200f ? "IMAGE_REANIM_GARLIC_BODY2" : "IMAGE_REANIM_GARLIC_BODY3";
                yield return fixture.Capture(null, () =>
                {
                    fixture.DrawPass("DrawPlantShadow", plant, new Vector2(280f, 180f), 0f, true);
                    Assert.That(fixture.Animations.DrawNativePlayback("Garlic", new Vector2(280f, 180f), 1f, .25f,
                        "anim_idle", true, 12f, track => health >= 133f || !track.StartsWith("Garlic_stem", StringComparison.OrdinalIgnoreCase),
                        "anim_face", replacement), Is.True);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "PC garlic body/stem damage stage at health " + health);
            }
            Save(fixture, "garlic-damaged.png");
            var zombie = new Zombie { Id = 81, Kind = ZombieKind.Normal, Row = 1, X = 5f, Health = 270f,
                StartingHealth = 270f, PresentationNativeVelocity = .24f, Speed = .3f, PresentationAnimationAge = .2f };
            SimulationFixture.Zombies(game).Add(zombie);
            SimulationFixture.SynchronizeZombieAnimation(game, zombie);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            var normal = fixture.Pixels.GetPixels32();
            zombie.PresentationGarlicCounter = 70;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            Assert.That(fixture.Pixels.GetPixels32(), Is.Not.EqualTo(normal));
            Save(fixture, "garlic-grossout.png");
            zombie.PresentationGarlicCounter = -1;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(normal), "Finishing the original reaction must restore the ordinary head and jaw.");
        }

        /// <summary>Checks the Cattail body uses the authored thirty-FPS shooting pose and its projectile uses the native cactus image.</summary>
        /// <returns>Original-resource body and projectile comparisons.</returns>
        [UnityTest]
        public IEnumerator CattailShootingAndSpikeMatchNativeResources()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(21);
            var plant = new Plant { Id = 72, Kind = PlantKind.Cattail, Row = 2, Column = 3,
                Layer = PlantLayer.Main, Health = 300f, PresentationRate = 12f, PresentationShotAge = .31f };
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                var bob = AdventurePresentationRules.WaterBobbingOffset(PlantKind.Cattail, 2, 3, true, 0);
                Assert.That(fixture.Animations.DrawNativePlayback("Cattail", new Vector2(280f, 250f + bob),
                    1f, .31f, "anim_shooting", false, 30f), Is.True);
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            Save(fixture, "cattail-shooting.png");
            var shot = new Pea { SourceKind = PlantKind.Cattail, Homing = true, X = 3.25f, FlightRow = 2f,
                PresentationHomingY = 247f, PresentationPreviousHomingY = 247f, VelocityX = 2.5f };
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureProjectile", shot));
            actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawResourceSprite("IMAGE_PROJECTILECACTUS",
                new Vector2(315f, 255f), 1f), Is.True));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            Save(fixture, "cattail-original-spike.png");
        }

        /// <summary>Checks the ready cob flashes independently and the firing body continues beyond projectile release.</summary>
        /// <returns>Original-resource ready and firing pose comparisons.</returns>
        [UnityTest]
        public IEnumerator CannonFlashesOnlyItsCobAndFinishesShooting()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var game = fixture.Owner.RequiredGame;
            var cannon = new Plant { Id = 73, Kind = PlantKind.CobCannon, Row = 1, Column = 3,
                ColumnSpan = 2, Layer = PlantLayer.Main, Health = 300f, CannonPhase = CobCannonPhase.Ready };
            game.Adventure._ticks = 0;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
            var bright = fixture.Pixels.GetPixels32();
            game.Adventure._ticks = 37;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
            var dark = fixture.Pixels.GetPixels32();
            Assert.That(dark, Is.Not.EqualTo(bright));
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativePlayback("CobCannon",
                new Vector2(280f, 180f), 1f, 0f, "anim_idle", true, 12f,
                colorTrack: "CobCannon_cob", trackColor: new Color(55f / 255f, 55f / 255f, 55f / 255f, 1f)), Is.True));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(dark));
            Save(fixture, "cob-ready-flash.png");
            cannon.Id = 74;
            cannon.CannonPhase = CobCannonPhase.Firing;
            cannon.CannonReloadRemaining = 1f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", cannon));
            var firing = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativePlayback("CobCannon",
                new Vector2(280f, 180f), 1f, 2.5f, "anim_shooting", false, 12f), Is.True));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(firing));
            Save(fixture, "cob-post-release-shooting.png");
        }

        /// <summary>Checks the original seventy-by-sixty-three crosshair is centered on the pointer at half alpha.</summary>
        /// <returns>Camera readback frames for the native cursor comparison.</returns>
        [UnityTest]
        public IEnumerator CannonCrosshairKeepsNativeSizeAndOpacity()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            fixture.Owner.RequiredGame.Adventure.SelectedCobCannonId = 73;
            fixture.Owner.AdventureBoard._adventurePointer = new Vector2(493f, 327f);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureCobCannonTarget"));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                var old = CanvasUI.Color;
                CanvasUI.Color = new Color(1f, 1f, 1f, 127f / 255f);
                Assert.That(fixture.Animations.DrawNativeImage(fixture.Owner.Textures.RequiredLoadImage("CobCannon_target.png"),
                    new Rect(458f, 295.5f, 70f, 63f), new Rect(0f, 0f, 1f, 1f)), Is.True);
                CanvasUI.Color = old;
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            Save(fixture, "cob-original-crosshair.png");
        }

        /// <summary>Checks real cannon clicks reach the shared native cursor through survival and independent-mode interface dispatch.</summary>
        /// <returns>Actual interface frames compared with the original centered seventy-by-sixty-three cursor at alpha 127.</returns>
        [UnityTest]
        public IEnumerator ModeCannonClickDisplaysTheOriginalCrosshairAndCancelRemovesIt()
        {
            foreach (var mode in new[] { 1, 13, 15, 31 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(ModeCatalog.Get(mode).BoardLevel);
                var owner = fixture.Owner;
                var session = new ModeSession(mode, AdventureSeedAccess.DevelopmentAll());
                owner.IndependentModes._modeSession = session;
                owner._game = session.Game;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._selected = -1;
                owner._choosingSeeds = false;
                Assert.That(session.Game.ChooseDefaultAdventureSeeds(), Is.True);
                session.Phase = ModePhase.Playing;
                session.Game.ResetAdventureWaveSchedule(100000, false, false);
                SimulationFixture.Plants(session.Game).Clear();
                var cannon = new Plant { Id = 73, Kind = PlantKind.CobCannon, Row = 2, Column = 3,
                    ColumnSpan = 2, Layer = PlantLayer.Main, Health = 300f, CannonPhase = CobCannonPhase.Ready };
                SimulationFixture.Plants(session.Game).Add(cannon);
                var click = new Vector2(320f, 80f + 2f * owner.Layout.AdventurePitch + owner.Layout.AdventureSlope(3) + 40f);
                owner.AdventureBoard._adventurePointer = click;
                Assert.That(owner.IndependentModes.TryHandleModeClick(click), Is.True);
                Assert.That(session.Game.Adventure.SelectedCobCannonId, Is.EqualTo(cannon.Id));
                Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Aiming));
                owner.AdventureBoard._adventurePointer = new Vector2(493f, 327f);
                yield return fixture.Capture("DrawInterface");
                var actual = fixture.Pixels.GetPixels32();
                Assert.That(session.Game.CancelAdventureCobCannonAim(), Is.True);
                yield return fixture.Capture("DrawInterface", () =>
                {
                    var previous = CanvasUI.Color;
                    CanvasUI.Color = new Color(1f, 1f, 1f, 127f / 255f);
                    Assert.That(fixture.Animations.DrawNativeImage(owner.Textures.RequiredLoadImage("CobCannon_target.png"),
                        new Rect(458f, 295.5f, 70f, 63f), new Rect(0f, 0f, 1f, 1f)), Is.True);
                    CanvasUI.Color = previous;
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Mode " + mode + " must draw the native cursor after an actual cannon click.");
                yield return fixture.Capture("DrawInterface");
                Assert.That(fixture.Pixels.GetPixels32(), Is.Not.EqualTo(actual), "Cancelling aim must remove the cursor.");
            }
        }

        /// <summary>Checks purple packet pixels and base-price-plus versus live bank-price labels.</summary>
        /// <returns>Original-resource card readback frames.</returns>
        [UnityTest]
        public IEnumerator UpgradeCardUsesPurpleFrameAndChooserPlus()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var session = new ModeSession(13, AdventureSeedAccess.DevelopmentAll());
            fixture.Owner._game = session.Game;
            var card = new Rect(280f, 180f, 50f, 70f);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureCard", PlantKind.GatlingPea, card, false, null!));
            var actual = fixture.Pixels.GetPixels32();
            Save(fixture, "upgrade-purple-packet.png");
            yield return fixture.Capture(null, () => Assert.That(fixture.Animations.DrawNativeImage(
                fixture.Owner.Textures.RequiredLoadImage("seeds.png"), card, new Rect(1f / 9f, 0f, 1f / 9f, 1f)), Is.True));
            var expected = fixture.Pixels.GetPixels32();
            foreach (var x in new[] { 290f, 305f, 320f })
            {
                var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
                var pixelX = Mathf.RoundToInt((Screen.width - 800f * scale) * .5f + x * scale);
                var pixelY = Screen.height - 1 - Mathf.RoundToInt((Screen.height - 600f * scale) * .5f + 187f * scale);
                Assert.That(actual[pixelY * Screen.width + pixelX], Is.EqualTo(expected[pixelY * Screen.width + pixelX]));
            }
            var presenter = new GamePresenter(fixture.Owner);
            try
            {
                var label = typeof(GamePresenter).GetMethod("AdventurePacketCostLabel", BindingFlags.Instance | BindingFlags.NonPublic)!;
                SimulationFixture.Plants(session.Game).Add(new Plant { Id = 75, Kind = PlantKind.GatlingPea, Health = 300f });
                Assert.That(label.Invoke(presenter, new object[] { PlantKind.GatlingPea, false }), Is.EqualTo("250+"));
                Assert.That(label.Invoke(presenter, new object[] { PlantKind.GatlingPea, true }), Is.EqualTo("300"));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        /// <summary>Checks garlic, charge, cannon launch and spike launch recordings decode and reach production voices.</summary>
        /// <returns>Original audio decoding and production playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator ChangedPlantFoleyReachProductionVoices()
        {
            yield return null;
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var audioHost = new GameObject("Original plant parity voices");
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
            var effects = fixture.Owner.Sounds;
            effects._audioSource = source;
            effects._soundVolume = .7f;
            effects._soundVoices = voices;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GameSoundEffects).GetField("_zombiePartAudioLoading", flags)!.SetValue(effects, true);
            var play = typeof(GameSoundEffects).GetMethod("PlayAdventureZombieFoley", flags)!;
            try
            {
                foreach (var foley in new[] { 72, 64, 76, 3 })
                {
                    var clips = new List<AudioClip>();
                    var seen = new HashSet<string>();
                    foreach (var sample in AdventureZombieFoleyData.Definitions[foley].Samples)
                    {
                        if (!seen.Add(sample))
                        {
                            continue;
                        }
                        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(fixture.Root, "sounds", sample + ".ogg")).AbsoluteUri, AudioType.OGGVORBIS);
                        yield return request.SendWebRequest();
                        Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), sample + ": " + request.error);
                        var clip = DownloadHandlerAudioClip.GetContent(request);
                        var data = new float[clip.samples * clip.channels];
                        Assert.That(clip.GetData(data, 0), Is.True);
                        Assert.That(Array.Exists(data, value => Math.Abs(value) > .001f), Is.True, sample);
                        effects._presentationSounds[sample] = clip;
                        clips.Add(clip);
                    }
                    voices.StopAll();
                    play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = foley, Tick = 100 } });
                    yield return null;
                    Assert.That(Array.Exists(audioHost.GetComponents<AudioSource>(), voice => voice.isPlaying && clips.Contains(voice.clip)), Is.True);
                }
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

        /// <summary>Optionally saves the actual real-device readback into the caller's ignored diagnostic directory.</summary>
        /// <param name="fixture">The completed camera capture.</param>
        /// <param name="name">The descriptive screenshot filename.</param>
        private static void Save(OriginalStormGraphicsTests.StormFixture fixture, string name)
        {
            var output = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, name), fixture.Pixels.EncodeToPNG());
            }
        }
    }
}
